<#
    Publish-Sensors.ps1
    -------------------
    Reads the files written by the collectors and POSTs each one into Home
    Assistant as a sensor. This is the ONLY script that knows about HA, so it's
    the only place to change if your token or address changes.

    Entities are created on first POST — no YAML, no integration, no restart.

    TO ADAPT:
      * Change -BaseUrl to your HA address.
      * Create a token first:  .\Set-Token.ps1 -Name ha
      * Add a block per sensor at the bottom. The pattern is always the same:
            read a file -> build attributes -> Publish-State

    GOTCHA: HA only updates last_updated when a value CHANGES. Posting the same
    payload repeatedly leaves the timestamp stale, so a healthy-but-idle feed
    looks identical to a dead one. That's why the heartbeat at the bottom always
    carries a changing value — keep it.
#>

param(
    [string]$BaseUrl   = 'http://homeassistant.local:8123',
    [string]$TokenPath = (Join-Path $PSScriptRoot 'ha-token.dat'),
    [string]$Entropy   = 'MyDashboard.HomeAssistant.v1'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security

$collectors = Join-Path $PSScriptRoot 'collectors'

# ── token ────────────────────────────────────────────────────────────────────
function Unprotect-Token {
    param([string]$Path, [string]$EntropyString)
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Token file not found: $Path  (run Set-Token.ps1 first)"
    }
    $enc = [IO.File]::ReadAllBytes($Path)
    $ent = [Text.Encoding]::UTF8.GetBytes($EntropyString)
    return [Text.Encoding]::UTF8.GetString(
        [Security.Cryptography.ProtectedData]::Unprotect(
            $enc, $ent, [Security.Cryptography.DataProtectionScope]::CurrentUser))
}

$token   = Unprotect-Token $TokenPath $Entropy
$headers = @{ Authorization = "Bearer $token" }

# ── the core call ────────────────────────────────────────────────────────────
function Publish-State {
    param(
        [Parameter(Mandatory)][string]$EntityId,
        [Parameter(Mandatory)][AllowEmptyString()][string]$State,
        [hashtable]$Attributes = @{}
    )

    # HA rejects states longer than 255 chars — truncate rather than fail
    if ($State.Length -gt 255) { $State = $State.Substring(0, 252) + '...' }

    $payload = @{ state = $State; attributes = $Attributes } | ConvertTo-Json -Depth 8

    # Encode as UTF-8 BYTES. Passing a string mangles accents, em-dashes, degrees.
    $bytes = [Text.Encoding]::UTF8.GetBytes($payload)

    try {
        Invoke-RestMethod -Uri "$BaseUrl/api/states/$EntityId" `
                          -Method Post -Headers $headers `
                          -ContentType 'application/json; charset=utf-8' `
                          -Body $bytes -TimeoutSec 10 | Out-Null
    }
    catch {
        Write-Warning "$EntityId : $($_.Exception.Message)"
    }
}

# ── helpers ──────────────────────────────────────────────────────────────────
function Read-TextFile {
    param([string]$Name)
    $p = Join-Path $collectors $Name
    if (-not (Test-Path -LiteralPath $p)) { return $null }
    $raw = (Get-Content -LiteralPath $p -Raw -Encoding UTF8).Trim()
    if ([string]::IsNullOrWhiteSpace($raw)) { return $null }
    return $raw
}

function Read-JsonFile {
    param([string]$Name)
    $raw = Read-TextFile $Name
    if ($null -eq $raw) { return $null }
    try { return $raw | ConvertFrom-Json } catch { return $null }
}

# ═════════════════════════════════════════════════════════════════════════════
#  ONE BLOCK PER SENSOR — copy the shape for your own
# ═════════════════════════════════════════════════════════════════════════════

# Now playing  ── Artist|Title|0:22 / 3:45|10|Playing|AppId|artworkUrl
$np = Read-TextFile 'NowPlaying.txt'
if ($np) {
    $f = $np.Split('|')
    Publish-State 'sensor.office_now_playing' $f[1] @{
        artist         = $f[0]
        elapsed        = $f[2]
        progress       = [int]$f[3]
        playback_state = $f[4]
        source         = $f[5]
        artwork        = $f[6]
        icon           = 'mdi:music-note'
    }
}

# Bitcoin  ── price|changePct
$btc = Read-TextFile 'Bitcoin.txt'
if ($btc) {
    $f = $btc.Split('|')
    Publish-State 'sensor.office_bitcoin' $f[0] @{
        change_24h          = $f[1]
        unit_of_measurement = 'GBP'
        icon                = 'mdi:currency-btc'
    }
}

# News  ── one headline per line
$news = Read-TextFile 'NewsItems.txt'
if ($news) {
    $items = @($news -split "`n" | Where-Object { $_.Trim() })
    Publish-State 'sensor.office_news' ("{0} stories" -f $items.Count) @{
        headlines = $items
        icon      = 'mdi:newspaper'
    }
}

# Ticket summary  ── JSON object
$tickets = Read-JsonFile 'ticket-summary.json'
if ($tickets) {
    Publish-State 'sensor.office_tickets' ([string]$tickets.open) @{
        open            = [int]$tickets.open
        awaiting_triage = [int]$tickets.awaitingTriage
        breached        = [int]$tickets.breached
        icon            = 'mdi:ticket-confirmation'
    }
}

# ── Machine stats: no collector needed, read them here ───────────────────────
$cpu = [math]::Round((Get-CimInstance Win32_Processor |
        Measure-Object -Property LoadPercentage -Average).Average, 0)
$os  = Get-CimInstance Win32_OperatingSystem
$memUsedPct = [math]::Round(
    (($os.TotalVisibleMemorySize - $os.FreePhysicalMemory) / $os.TotalVisibleMemorySize) * 100, 0)

Publish-State 'sensor.office_system' ([string]$cpu) @{
    cpu_percent         = $cpu
    memory_percent      = $memUsedPct
    uptime_hours        = [math]::Round(((Get-Date) - $os.LastBootUpTime).TotalHours, 1)
    unit_of_measurement = '%'
    icon                = 'mdi:chip'
}

# ── Heartbeat: ALWAYS carries a changing value, so you can tell idle from dead ──
Publish-State 'sensor.office_bridge_status' 'online' @{
    last_run = (Get-Date).ToString('o')
    icon     = 'mdi:lan-connect'
}
