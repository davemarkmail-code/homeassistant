# Architecture

## Framework choice

The spec proposed **.NET 8 + WinUI 3 + WebView2 + MVVM + DI + Serilog** and
invited a counter-proposal if one is "significantly better." For this specific
appliance, the recommendation is to keep everything except the UI framework, and
use **WPF** instead of WinUI 3.

### Why not WinUI 3 here

WinUI 3 is Microsoft's forward-looking desktop UI stack and is an excellent
choice for rich, touch-first, Fluent-styled applications. But the requirements
of this project pull in the opposite direction — it wants to be a boring,
rock-solid, single-window kiosk — and that is where WinUI 3's rough edges show:

- **Borderless / kiosk framing.** WinUI 3 does not give first-class,
  precise control over a truly chromeless window pinned to a *named* monitor;
  you end up in `AppWindow` / `OverlappedPresenter` interop anyway.
- **Deployment.** The requirement is "single EXE or MSIX, no admin,
  self-contained." WinUI 3's cleanest path is packaged (MSIX) with a Windows App
  SDK runtime dependency; unpackaged self-contained deployment is possible but
  adds a bootstrapper and more moving parts.
- **Tray icon.** No native support; you take a third-party dependency
  (`H.NotifyIcon`) regardless.
- **Churn vs. stability.** A 24/7 appliance benefits from a mature, slow-moving
  platform. WPF's age is a feature here.

### Why WPF fits

- Truly borderless windows are trivial (`WindowStyle=None` + `ResizeMode=NoResize`).
- First-class `Microsoft.Web.WebView2.Wpf` control.
- `.NET 8` self-contained single-file publish with **no admin and no runtime
  prerequisite** — matches the deployment requirement directly.
- MVVM (`CommunityToolkit.Mvvm`), DI (`Microsoft.Extensions.*`), and Serilog work
  identically to the WinUI plan.
- Precise physical-pixel window placement via a small, isolated Win32 layer.

### What's identical either way

Both frameworks need Win32 interop to resolve a monitor by its EDID *friendly*
name ("XENEON EDGE") — Windows only exposes that through the CCD API. So that
cost is a wash, and everything else favours WPF for this appliance.

> If you specifically want the Fluent look or plan a touch-heavy future UI, we
> can switch the presentation layer to WinUI 3 with minimal disruption: the
> `Services/`, `Models/`, and `Helpers/` layers are UI-agnostic and would be
> reused unchanged.

## Layering

```
App (composition root)
 └─ builds the Generic Host: DI container + Serilog + lifetime
      ├─ Services/      pure logic, no UI types
      │    ISettingsService     load/save settings.json
      │    IMonitorService      resolve monitors by friendly name (CCD API)
      │    IStartupCheckService network + Home Assistant reachability
      ├─ ViewModels/    DashboardViewModel — the startup/reconnect state machine
      └─ Views/         DashboardWindow — borderless host + WebView2 lifecycle
      Helpers/          AppPaths, MonitorInfo, NativeMethods (isolated P/Invoke)
```

Rules that keep it clean and extensible:

- **Services never reference WPF types.** They are trivially testable and
  reusable if the UI framework ever changes.
- **All P/Invoke lives in `Helpers/NativeMethods.cs`** behind a service
  interface, so the "unsafe" surface is small and auditable.
- **Policy vs. mechanism.** The ViewModel decides *when* to navigate,
  reconnect, or wait. The View owns the *mechanism* (the WebView2 control and
  the window handle). They communicate through a narrow event
  (`NavigateRequested`) and a couple of notification methods.

## The monitor gate

The app must not launch if the XENEON EDGE display is absent. `App.OnStartup`
resolves the monitor first; if `IMonitorService.FindByName` returns `null`, the
app logs the reason and shuts down before any window is created — a silent
no-op, exactly as specified. This also makes "start with Windows" safe: on a
reboot where the display hasn't enumerated yet, the retry belongs to the
Windows startup task / a short internal retry (Roadmap), not a crash.

## Persistent login

Home Assistant's "keep me logged in" stores a long-lived token in browser
storage. WebView2 persists that in its **user-data folder**, so the single most
important thing for "never sign in again" is that the profile path is stable and
never a temp directory. It defaults to
`%LocalAppData%\XEON Dashboard\WebView2`.

## Single project, not many

The requested `src/` sub-folders (`Services`, `Models`, `Views`, …) are
implemented as folders/namespaces within **one** project rather than separate
assemblies. For an app this size, splitting into multiple projects is
premature — it adds build and reference overhead without a real boundary to
enforce. The namespaces already give clean separation, and extraction into
libraries later (if a plugin system for the "future features" warrants it) is
mechanical.
