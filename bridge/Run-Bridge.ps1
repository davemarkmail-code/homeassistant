<#
    Run-Bridge.ps1
    --------------
    The loop. Runs collectors on staggered intervals, then publishes everything
    to Home Assistant. Designed to run hidden at login via Start-Bridge.vbs.

    Layout it expects:
        bridge\
          Run-Bridge.ps1          <- this file
          Publish-Sensors.ps1
          collectors\
            Get-NowPlaying.ps1    <- and friends; each writes ONE file

    TO ADAPT: edit the schedule block. Add or remove Invoke-Collector lines and
    change the intervals to suit. Anything hitting a third-party API should be
    minutes, not seconds.

    NOTE ON LOGGING: this APPENDS and trims. An earlier version overwrote the log
    every cycle, which meant any error vanished within 5 seconds — precisely when
    you needed to read it. Don't do that.
#>

param(
    [int]$IntervalSeconds = 5,
    [int]$MaxLogBytes     = 1MB
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

$collectors = Join-Path $PSScriptRoot 'collectors'
$logPath    = Join-Path $PSScriptRoot 'bridge.log'

# Single instance only — a second launch exits quietly rather than double-posting
$mutex = New-Object Threading.Mutex($false, 'Local\OfficeDashboardBridge')
if (-not $mutex.WaitOne(0, $false)) { exit 0 }

function Write-Log {
    param([string]$Message)
    Add-Content -LiteralPath $logPath -Value ((Get-Date -Format s) + ' ' + $Message)
    if ((Test-Path $logPath) -and (Get-Item $logPath).Length -gt $MaxLogBytes) {
        $keep = Get-Content -LiteralPath $logPath -Tail 500
        Set-Content -LiteralPath $logPath -Value $keep
    }
}

function Invoke-Collector {
    param([string]$Name)
    $path = Join-Path $collectors $Name
    if (Test-Path -LiteralPath $path) {
        try   { & $path *> $null }
        catch { Write-Log ("$Name FAILED: " + $_.Exception.Message) }
    }
}

Write-Log 'bridge starting'

$last = @{}
function Due {
    param([string]$Key, [double]$Seconds)
    $now = Get-Date
    if (-not $last.ContainsKey($Key) -or ($now - $last[$Key]).TotalSeconds -ge $Seconds) {
        $last[$Key] = $now
        return $true
    }
    return $false
}

try {
    while ($true) {

        # ── schedule ──────────────────────────────────────────────────────────
        Invoke-Collector 'Get-NowPlaying.ps1'                        # every cycle (~5s)

        if (Due 'minute' 60) {
            Invoke-Collector 'Get-NextMeeting.ps1'
            Invoke-Collector 'Get-Comms.ps1'
            Invoke-Collector 'Get-Bitcoin.ps1'
        }
        if (Due 'fiveMinutes' 300) {
            Invoke-Collector 'Get-TicketSummary.ps1'
        }
        if (Due 'tenMinutes' 600) {
            Invoke-Collector 'Get-ServiceStatus.ps1'
            Invoke-Collector 'Get-NewsFeed.ps1'
        }
        # ──────────────────────────────────────────────────────────────────────

        try {
            & (Join-Path $PSScriptRoot 'Publish-Sensors.ps1') *> $null
        }
        catch { Write-Log ('publish FAILED: ' + $_.Exception.Message) }

        Start-Sleep -Seconds ([Math]::Max(5, $IntervalSeconds))
    }
}
finally {
    $mutex.ReleaseMutex()
    $mutex.Dispose()
}
