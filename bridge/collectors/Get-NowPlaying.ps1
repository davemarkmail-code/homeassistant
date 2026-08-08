<#
    Get-NowPlaying.ps1
    ------------------
    Reads the Windows global media session and writes the current track to a text file.

    Works with ANY player that registers with Windows media controls — Apple Music,
    Spotify, YouTube in Edge/Chrome, VLC, Groove. No API key, no OAuth, no vendor SDK.

    Output (pipe-delimited, one line):
        Artist|Title|0:22 / 3:45|10|Playing|AppUserModelId|https://artwork.url

    TO ADAPT:
      * Change the SourceAppUserModelId filter below to target a different player,
        or remove the Where-Object entirely to take whichever session is active.
        Spotify   -> 'Spotify'
        Edge      -> 'MSEdge'
        Chrome    -> 'Chrome'
      * Artwork is looked up from the public iTunes search API and cached so it
        only fires when the track changes. Delete Get-ArtworkUrl if you don't want it.

    Requires: Windows 10/11, PowerShell 5.1+. No admin rights.
#>

$ErrorActionPreference = 'Stop'
$outputFile       = Join-Path $PSScriptRoot 'NowPlaying.txt'
$artworkCacheFile = Join-Path $PSScriptRoot 'NowPlayingArtwork.json'
$utf8NoBom        = New-Object System.Text.UTF8Encoding($false)

function Clean-Field {
    param([AllowNull()][string]$Value)
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    return (($Value -replace '[\r\n|]+', ' ') -replace '\s{2,}', ' ').Trim()
}

function Format-Time {
    param([int]$Seconds)
    if ($Seconds -lt 0) { $Seconds = 0 }
    $span = [TimeSpan]::FromSeconds($Seconds)
    if ($span.TotalHours -ge 1) { return '{0}:{1:00}:{2:00}' -f [int]$span.TotalHours, $span.Minutes, $span.Seconds }
    return '{0}:{1:00}' -f $span.Minutes, $span.Seconds
}

function Get-ArtworkUrl {
    param([string]$Artist, [string]$Title)
    if ([string]::IsNullOrWhiteSpace($Title)) { return '' }

    # Strip a trailing " — Album" if the player packs it into the artist field
    $lookupArtist = [regex]::Replace($Artist, '\s+[\u2013\u2014]\s+.*$', '').Trim()
    $cacheKey = ($lookupArtist + '|' + $Title).ToLowerInvariant()

    $cached = $null
    if (Test-Path -LiteralPath $artworkCacheFile) {
        try { $cached = Get-Content -Raw -LiteralPath $artworkCacheFile | ConvertFrom-Json } catch { $cached = $null }
    }
    if ($null -ne $cached -and [string]$cached.key -eq $cacheKey) {
        return [string]$cached.artwork      # same track — don't hit the API again
    }

    try {
        $term = [Uri]::EscapeDataString(($lookupArtist + ' ' + $Title).Trim())
        $uri  = 'https://itunes.apple.com/search?term=' + $term + '&country=GB&media=music&entity=song&limit=5'
        $result = Invoke-RestMethod -Uri $uri -TimeoutSec 8

        $candidates = @($result.results)
        $best = $candidates | Where-Object {
            ([string]$_.trackName).Trim() -ieq $Title.Trim() -and
            ([string]$_.artistName) -like ('*' + $lookupArtist + '*')
        } | Select-Object -First 1
        if ($null -eq $best) { $best = $candidates | Select-Object -First 1 }

        $artwork = if ($null -ne $best) { [string]$best.artworkUrl100 } else { '' }
        @{ key = $cacheKey; artwork = $artwork; checked = (Get-Date).ToString('o') } |
            ConvertTo-Json -Compress | Set-Content -LiteralPath $artworkCacheFile -Encoding UTF8
        return $artwork
    }
    catch { return '' }
}

function Write-Result {
    param(
        [string]$Artist, [string]$Title, [string]$Time,
        [int]$Progress, [string]$State, [string]$Source, [string]$Artwork
    )
    $line = '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f @(
        (Clean-Field $Artist),
        (Clean-Field $Title),
        (Clean-Field $Time),
        ([Math]::Max(0, [Math]::Min(100, $Progress))),
        (Clean-Field $State),
        (Clean-Field $Source),
        (Clean-Field $Artwork)
    )
    [System.IO.File]::WriteAllText($outputFile, $line + [Environment]::NewLine, $utf8NoBom)
}

try {
    # Load the WinRT media-control types into PowerShell
    Add-Type -AssemblyName System.Runtime.WindowsRuntime
    $null = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager, Windows.Media.Control, ContentType = WindowsRuntime]
    $null = [Windows.Media.Control.GlobalSystemMediaTransportControlsSession, Windows.Media.Control, ContentType = WindowsRuntime]
    $null = [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties, Windows.Media, ContentType = WindowsRuntime]

    # WinRT async ops aren't awaitable in PS directly — grab the AsTask helper
    $asTask = [System.WindowsRuntimeSystemExtensions].GetMethods() |
        Where-Object { $_.Name -eq 'AsTask' -and $_.IsGenericMethod -and $_.GetParameters().Count -eq 1 } |
        Select-Object -First 1

    function Await-WinRT {
        param($Operation, [Type]$ResultType)
        $task = $asTask.MakeGenericMethod($ResultType).Invoke($null, @($Operation))
        $task.Wait()
        return $task.Result
    }

    $manager = Await-WinRT (
        [Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager]::RequestAsync()
    ) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager])

    # ── CHANGE THIS LINE to target a different player ─────────────────────────
    $session = $manager.GetSessions() |
        Where-Object { $_.SourceAppUserModelId -match 'AppleMusic' } |
        Select-Object -First 1
    # ──────────────────────────────────────────────────────────────────────────

    if (-not $session) {
        Write-Result 'Apple Music' 'Not playing' '0:00 / 0:00' 0 'Stopped' '' ''
        exit 0
    }

    $media = Await-WinRT (
        $session.TryGetMediaPropertiesAsync()
    ) ([Windows.Media.Control.GlobalSystemMediaTransportControlsSessionMediaProperties])

    $timeline = $session.GetTimelineProperties()
    $playback = $session.GetPlaybackInfo()
    $position = [Math]::Max(0, [int]$timeline.Position.TotalSeconds)
    $duration = [Math]::Max(0, [int]$timeline.EndTime.TotalSeconds)
    $progress = if ($duration -gt 0) { [int](($position / $duration) * 100) } else { 0 }
    $timeText = '{0} / {1}' -f (Format-Time $position), (Format-Time $duration)
    $artwork  = Get-ArtworkUrl -Artist ([string]$media.Artist) -Title ([string]$media.Title)

    Write-Result $media.Artist $media.Title $timeText $progress `
                 $playback.PlaybackStatus $session.SourceAppUserModelId $artwork
}
catch {
    # Never leave a half-written file — publish a known-bad state instead
    Write-Result 'Apple Music' 'Unavailable' '0:00 / 0:00' 0 'Unavailable' '' ''
}
