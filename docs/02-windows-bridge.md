# The Windows → Home Assistant bridge

Getting Windows-only data into Home Assistant as real sensors.

---

## The pattern in 20 lines

Everything below is elaboration on this:

```powershell
# 1. Collect something Windows knows and HA doesn't
$track = Get-CurrentlyPlayingTrack        # your logic here

# 2. Push it into HA as a sensor
$body = @{
    state      = $track.Title
    attributes = @{
        artist         = $track.Artist
        artwork        = $track.ArtUrl
        playback_state = $track.Status
        icon           = 'mdi:music-note'
    }
} | ConvertTo-Json -Depth 6

Invoke-RestMethod `
    -Uri     "$HaUrl/api/states/sensor.office_now_playing" `
    -Method  Post `
    -Headers @{ Authorization = "Bearer $Token" } `
    -ContentType 'application/json' `
    -Body    ([Text.Encoding]::UTF8.GetBytes($body))
```

That's it. The entity appears in HA immediately — no YAML, no restart, no integration.

> **Encode the body as UTF-8 bytes**, as above. Hand `Invoke-RestMethod` a plain
> string and non-ASCII characters (accented artist names, em-dashes, `°`) arrive
> mangled.

---

## Anatomy

```
@Resources/
├── Get-NowPlaying.ps1          collectors — each writes ONE file
├── Get-NextMeeting.ps1
├── Get-Comms.ps1
├── NowPlaying.txt              their output
├── Meeting.txt
└── HomeAssistant/
    ├── Run-Bridge.ps1          the loop
    ├── Publish-Sensors.ps1     reads files → POSTs to HA
    ├── Start-Bridge.vbs        launcher (hidden window)
    └── *.dat                   encrypted tokens — never commit
```

### The loop

```powershell
$resources = Split-Path $PSScriptRoot -Parent
$last = @{}

function Should-Run($key, $seconds) {
    if (-not $last.ContainsKey($key) -or
        ((Get-Date) - $last[$key]).TotalSeconds -ge $seconds) {
        $last[$key] = Get-Date
        return $true
    }
    return $false
}

while ($true) {
    if (Should-Run 'nowplaying'  5)   { & "$resources\Get-NowPlaying.ps1" }
    if (Should-Run 'meeting'    60)   { & "$resources\Get-NextMeeting.ps1" }
    if (Should-Run 'tickets'   300)   { & "$resources\Get-TicketSummary.ps1" }

    & "$PSScriptRoot\Publish-Sensors.ps1"
    Start-Sleep -Seconds 5
}
```

**Stagger your intervals.** Now-playing needs 5 seconds to feel live; a ticket queue
is fine at 5 minutes. Hitting a third-party API every 5 seconds will get you
rate-limited and tells you nothing new.

**Use `$PSScriptRoot` everywhere.** Any absolute path in a collector is a landmine
that goes off the day you move the folder. See [05-gotchas.md](05-gotchas.md).

**Guard against double-starts** with a named mutex, so a second launch exits quietly
instead of double-posting:

```powershell
$mutex = New-Object System.Threading.Mutex($false, 'Global\OfficeDashboardBridge')
if (-not $mutex.WaitOne(0)) { exit }
```

---

## File formats

Pipe-delimited for flat data — trivial to write, trivial to parse, readable in Notepad:

```
Artist — Album|Track Title|0:22 / 3:45|10|Playing|AppId|https://…/art.jpg
```

```powershell
$f = (Get-Content $path -Raw).Split('|')
$title = $f[1]; $elapsed = $f[2]; $state = $f[4]
```

JSON when it's nested (ticket counts, breakdowns). Both are fine — just be consistent
and always write with `-Encoding UTF8`.

---

## Tokens: don't put them in the script

Create a long-lived token in HA (Profile → Security → Long-lived access tokens), then
encrypt it to disk with DPAPI so it's readable only by your Windows account:

```powershell
# Once, interactively:
$token = Read-Host 'Paste HA token' -AsSecureString
$bytes = [Text.Encoding]::UTF8.GetBytes(
            [Runtime.InteropServices.Marshal]::PtrToStringAuto(
              [Runtime.InteropServices.Marshal]::SecureStringToBSTR($token)))
$entropy = [Text.Encoding]::UTF8.GetBytes('YourApp.v1')
$enc = [Security.Cryptography.ProtectedData]::Protect(
          $bytes, $entropy, 'CurrentUser')
[IO.File]::WriteAllBytes("$PSScriptRoot\ha-token.dat", $enc)
```

```powershell
# At runtime:
$enc = [IO.File]::ReadAllBytes($TokenPath)
$entropy = [Text.Encoding]::UTF8.GetBytes('YourApp.v1')
$token = [Text.Encoding]::UTF8.GetString(
    [Security.Cryptography.ProtectedData]::Unprotect(
        $enc, $entropy, 'CurrentUser'))
```

Three things to know:

- **`CurrentUser` scope binds it to your Windows account.** Copying the `.dat` to
  another machine or user will fail to decrypt. That's the point, but it will surprise
  you when you migrate.
- **The entropy string is a salt, not a path.** Change it and existing files stop
  decrypting. Don't "tidy" it during a refactor.
- **Never commit `*.dat`.** Encrypted or not.

---

## Running it at login

A `.vbs` launcher runs PowerShell with no console window:

```vbscript
Option Explicit
Dim shell, command
Set shell = CreateObject("WScript.Shell")
command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden " & _
          "-File ""C:\path\to\Run-Bridge.ps1"""
shell.Run command, 0, False
```

Drop it in `shell:startup` (Win+R → `shell:startup`). You can put the `.vbs` itself
there rather than a shortcut — simpler, and one less indirection to break.

**Use an absolute path inside the `.vbs`.** It's tempting to make it self-locating via
`WScript.ScriptFullName`, but then copying it into Startup breaks it, because it would
resolve relative to the Startup folder.

---

## Logging that doesn't destroy its own evidence

The obvious approach is wrong:

```powershell
# DON'T
[IO.File]::WriteAllText($log, "$(Get-Date -f s) bridge online")
```

That overwrites on every cycle. At a 5-second loop, any error is gone within 5 seconds
— exactly when you need it. Append, and cap the size:

```powershell
Add-Content $log "$(Get-Date -f s) $message"
if ((Get-Item $log).Length -gt 1MB) {
    $keep = Get-Content $log -Tail 500
    Set-Content $log $keep
}
```

---

## Health checking

Because HA won't tell you the feed died (see
[01-architecture.md](01-architecture.md)), publish a heartbeat whose value always
changes:

```powershell
$body = @{
    state = 'online'
    attributes = @{ last_run = (Get-Date -Format o); uptime_minutes = $mins }
} | ConvertTo-Json
```

Then alert on it in HA:

```yaml
- alias: Office bridge died
  trigger:
    - platform: state
      entity_id: sensor.office_bridge_status
      to: ~
      for: "00:05:00"      # no state change at all for 5 minutes
  action:
    - service: notify.mobile_app_phone
      data: { message: "Office dashboard bridge has stopped" }
```

---

## Troubleshooting

| Symptom | Likely cause |
|---|---|
| Sensor frozen, file also stale | Bridge not running. Check Startup and Task Scheduler paths |
| Sensor frozen, **file updating** | Publisher failing — token expired, HA moved, or it can't parse the file |
| One tile stale, rest fine | That collector is erroring. Run it by hand and read the output |
| Everything worked until you moved the folder | An absolute path somewhere. Grep for `C:\` |
| Garbled characters | Body not UTF-8 encoded before POST |
| Sensor exists but `unknown` | Posted with no `state`, or state longer than 255 characters |

**Run collectors directly when debugging.** No token, no HA, no loop — just run the
script and look at the file it wrote. That isolates collection from publishing in one
step, and is the single most useful habit with this design.
