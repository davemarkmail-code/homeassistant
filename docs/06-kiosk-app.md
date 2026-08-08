# The kiosk app

A native Windows appliance that shows a Home Assistant dashboard fullscreen on a
dedicated display. Built because browser kiosk mode is *nearly* right and the gap
is annoying: stray toolbars, wrong monitor on reboot, no auto-reconnect, and a
mouse cursor sitting in the middle of your wall panel.

Source lives in [`kiosk/`](../kiosk/). WPF on .NET 8, hosting WebView2.

---

## What it does that a browser doesn't

| Behaviour | Why it matters on a wall panel |
|---|---|
| **Targets a monitor by name** | Survives reboots and display re-ordering. `--window-position` guesses; this doesn't |
| **Launch delay** | Waits for the network before loading, so you don't boot to a connection error |
| **Auto-reconnect** | Retries on a timer if HA is down or rebooting |
| **Borderless, no chrome** | No address bar, no tab strip, no F11 to fall out of |
| **Single instance** | A second launch focuses the first rather than opening a duplicate |
| **Tray icon + settings window** | Reconfigure without editing JSON over RDP |
| **Hide cursor after N seconds** | No arrow parked over your dashboard |
| **Start with Windows** | Self-managing, no admin rights, no Task Scheduler |
| **Self-contained publish** | One `.exe`, no .NET install on the target machine |

---

## Configuration

`settings.json`, sat beside the executable:

```json
{
  "DashboardUrl": "http://homeassistant.local:8123/lovelace/kiosk?kiosk",
  "MonitorName": "XENEON EDGE",
  "LaunchDelaySeconds": 10,
  "ReconnectIntervalSeconds": 5,
  "HideMouse": false,
  "HideMouseAfterSeconds": 5,
  "AlwaysOnTop": false,
  "StartWithWindows": false,
  "ExitSilentlyIfMonitorMissing": false,
  "WebViewProfilePath": ""
}
```

| Key | Notes |
|---|---|
| `DashboardUrl` | **Include the view path.** `/lovelace/dash?kiosk` lands on the *first* view; `/lovelace/dash/home?kiosk` lands where you meant |
| `MonitorName` | As Windows reports it. Leave **empty** to use the primary display — do this when developing on a normal PC |
| `LaunchDelaySeconds` | 10 is sensible. Wi-Fi and DHCP are rarely ready the instant the shell loads |
| `ExitSilentlyIfMonitorMissing` | Set `true` on a laptop that sometimes isn't docked, so it doesn't nag |
| `WebViewProfilePath` | Its own profile keeps HA's session separate from your normal browsing |

---

## Build

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project src/XeonDashboard/XeonDashboard.csproj
```

Publish a portable exe plus an installer:

```powershell
.\build.ps1
```

- **Portable** — one self-contained `XeonDashboard.exe` (~70 MB). Copy and run; no
  .NET on the target machine.
- **Installer** — no-admin setup with Start Menu and startup shortcuts. Needs
  [Inno Setup 6](https://jrsoftware.org/isdl.php).

Requirements: Windows 10 (2004+) or 11, .NET 8 SDK to build, WebView2 Runtime at
runtime (already present on Windows 11 and current Windows 10).

---

## It won't rescale your dashboard

Worth being blunt about, because it's the most common expectation mismatch: **this
is a browser viewport.** It renders your dashboard at the panel's native resolution
and cannot reflow it.

If things look too big or too small, fix it **in Home Assistant** — card sizes,
column counts, a panel view. See [03-lovelace-design.md](03-lovelace-design.md) for
building a layout that fits a fixed resolution exactly.

---

## Don't want .NET?

Chrome or Edge in kiosk mode covers most of it:

```powershell
start msedge --kiosk "http://homeassistant.local:8123/lovelace/dash/home?kiosk" `
    --edge-kiosk-type=fullscreen --no-first-run `
    --user-data-dir="C:\kiosk-profile"
```

You lose monitor-by-name targeting, auto-reconnect, cursor hiding and single-instance
behaviour — which is precisely the list that made the app worth writing. For a panel
that's always on the primary display and rarely reboots, the one-liner is fine.

---

## Why WPF rather than WinUI 3

For a 24/7 borderless kiosk that must publish as a self-contained, no-admin
executable, WPF on .NET 8 hits every requirement with less friction and better
long-term stability. The service layer is framework-agnostic, so it can be reused if
you'd rather build the shell in something else.
