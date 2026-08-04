# XEON Dashboard

A native Windows appliance that displays a Home Assistant dashboard fullscreen
on a dedicated mini PC connected to a **Corsair XENEON EDGE** display. It
replaces the previous PowerShell + Microsoft Edge kiosk arrangement with a
purpose-built WebView2 host that behaves like an appliance, not a browser.

> **Status:** Stages 1–3. Working borderless dashboard host with a system-tray
> icon, single-instance locking, and a full settings window (live monitor
> picker, first-run setup, and a self-managing "start with Windows" toggle).
> See [`Docs/ROADMAP.md`](Docs/ROADMAP.md) for what comes next.

## A note on dashboard scaling

This app displays your Home Assistant dashboard at the screen's **native
resolution** — it's a browser viewport, so it can't resize or re-flow the
dashboard itself. If your dashboard looks too big or too small on the panel,
adjust it **inside Home Assistant** (card sizes, column counts, or a
kiosk/panel view). The app shows this same reminder on its Settings screen so
anyone you share the build with sees it too.

## Why WPF (not WinUI 3)

The spec proposed WinUI 3; this foundation uses **WPF on .NET 8** instead. The
reasoning is in [`Docs/ARCHITECTURE.md`](Docs/ARCHITECTURE.md#framework-choice).
In short: for a 24/7 borderless single-monitor kiosk that must publish as a
self-contained no-admin executable, WPF hits every requirement with less
friction and far more long-term stability. MVVM, DI, and Serilog are used
exactly as requested. If you'd prefer WinUI 3, the service layer is
framework-agnostic and can be reused.

## Requirements

- Windows 10 (2004+) or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download) to build
- WebView2 Runtime (ships with Windows 11 and current Windows 10; the installer
  stage will bundle the Evergreen bootstrapper as a fallback)

## Build & run

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project src/XeonDashboard/XeonDashboard.csproj
```

On a machine without a monitor literally named `XENEON EDGE`, set `MonitorName`
to an empty string in `settings.json` (see below) to fall back to the primary
display for development.

## Publish (prerequisite-free for users)

One command builds both a portable exe and an installer:

```powershell
.\build.ps1
```

- **Portable:** a single self-contained `XeonDashboard.exe` (~70 MB) — download
  and run, no .NET install needed.
- **Installer:** `Installer\Output\XEON-Dashboard-Setup.exe` — a no-admin
  setup.exe with Start Menu / startup shortcuts and clean uninstall (needs
  [Inno Setup 6](https://jrsoftware.org/isdl.php) to compile).

The .NET runtime is bundled, and the WebView2 Runtime is auto-provisioned, so
end users don't install anything by hand. Full details, including how to bundle
the WebView2 bootstrapper and the SmartScreen/code-signing note, are in
[`Docs/PACKAGING.md`](Docs/PACKAGING.md).

## Configuration

Settings live in `%LocalAppData%\XEON Dashboard\settings.json` and are created
with sensible defaults on first run. See
[`Docs/settings.sample.json`](Docs/settings.sample.json).

| Key | Meaning | Default |
| --- | --- | --- |
| `DashboardUrl` | Home Assistant URL to show | `http://homeassistant.local:8123/lovelace/0?kiosk` |
| `MonitorName` | EDID friendly name of the target display (empty = primary) | `XENEON EDGE` |
| `LaunchDelaySeconds` | Delay before startup checks | `10` |
| `ReconnectIntervalSeconds` | Retry interval when HA is unreachable | `5` |
| `HideMouse` / `HideMouseAfterSeconds` | Cursor auto-hide (later stage) | `false` / `5` |
| `AlwaysOnTop` | Keep window topmost | `false` |
| `StartWithWindows` | Auto-launch at sign-in (later stage) | `false` |
| `WebViewProfilePath` | WebView2 user-data folder (empty = default) | `` |

Logs: `%LocalAppData%\XEON Dashboard\Logs\xeon-*.log` (daily rolling, 14 days).

## Repository layout

```
XEON-Dashboard/
├── src/XeonDashboard/       # single WPF application project
│   ├── Models/              # AppSettings
│   ├── Services/            # settings, monitor detection, connectivity
│   ├── ViewModels/          # DashboardViewModel (startup state machine)
│   ├── Views/               # DashboardWindow (borderless host + WebView2)
│   ├── Helpers/             # AppPaths, Win32 interop, MonitorInfo
│   └── Assets/              # icons
├── Installer/               # packaging (later stage)
├── Docs/                    # architecture, roadmap, sample config
└── README.md
```

## Design principles

Clean architecture, maintainability, stability, and native Windows behaviour.
No simulated keystrokes, no browser window hacks. Where a Win32 call is
unavoidable (friendly monitor names, borderless framing) it is isolated in
`Helpers/NativeMethods.cs` behind a service interface.

## Licence

Intended to be released open source. Add a `LICENSE` file (MIT is a good
default for this kind of utility) before publishing.
