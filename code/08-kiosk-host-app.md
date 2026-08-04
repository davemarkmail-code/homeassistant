# The kiosk host application

A small native Windows app whose only job is to display a Home Assistant
dashboard fullscreen on a dedicated panel, and to behave like an **appliance**
rather than a browser someone left open.

This started as PowerShell launching Microsoft Edge in kiosk mode. That worked,
mostly — until Edge decided to show an update prompt, or the window lost focus,
or a stray keypress opened a tab. A purpose-built host solved all of it.

---

## Why bother at all?

If you just want a dashboard on a screen, a browser in kiosk mode is fine. The
reasons to build a host:

**It survives.** No update banners, no crash-restore bar, no address bar to
reach, no way to navigate away.

**It knows which monitor it belongs on.** Mine drives a widescreen secondary
panel. A browser reopens wherever Windows feels like; the app pins itself to a
named display and reasserts that if the monitor layout changes.

**It starts itself and hides.** Launches with Windows, no console window, a
tray icon for the rare occasions you need it.

**It reconnects.** If Home Assistant restarts or the network blips, it retries
rather than sitting on an error page.

---

## Stack

- **WPF on .NET 8** — chosen over WinUI 3 deliberately. For a 24/7 borderless
  single-monitor kiosk that must publish as a self-contained executable with no
  admin rights, WPF meets every requirement with less friction. MVVM,
  dependency injection and Serilog throughout; the service layer is
  framework-agnostic if you'd rather use something else.
- **WebView2** for rendering — the Chromium engine already present on Windows
  10 (2004+) and 11.
- **Self-contained publish** — a single ~70MB exe with the runtime bundled, so
  nobody has to install .NET. An Inno Setup installer wraps it with Start Menu
  and startup shortcuts, and needs no admin rights.

---

## Structure

```
src/XeonDashboard/
├── App.xaml.cs                  host builder, DI, Serilog, single-instance lock
├── Views/
│   ├── DashboardWindow.xaml     borderless WebView2 host
│   └── SettingsWindow.xaml      first-run setup and live monitor picker
├── ViewModels/
│   ├── DashboardViewModel.cs    navigation policy, reconnect logic
│   └── SettingsViewModel.cs
├── Services/
│   ├── MonitorService.cs        enumerate displays, find one by name
│   ├── AppController.cs         lifecycle coordination
│   ├── TrayIconService.cs       tray icon and menu
│   ├── SettingsService.cs       settings.json read/write
│   ├── StartupRegistrationService.cs   the "start with Windows" toggle
│   ├── StartupCheckService.cs   first-run detection
│   └── WebView2RuntimeService.cs       detect and provision the runtime
└── Models/
```

Every service sits behind an interface (`IMonitorService`, `ITrayIconService`
and so on), which keeps the WPF layer thin and the logic testable.

---

## The design decisions that mattered

**Native resolution, no scaling.** The app is a browser viewport; it cannot
re-flow your dashboard. If the dashboard looks wrong on the panel, fix it *in
Home Assistant* — card sizes, column counts, a panel view. Trying to solve
layout in the host is a trap. The settings screen says this out loud so anyone
you share the build with sees it.

**Find the monitor by name, not by index.** Display indices shuffle when you
plug something in. Matching on the monitor's reported name is stable. Provide a
fallback to the primary display, or nobody else can run your build.

**Single-instance lock.** Two copies fighting over the same fullscreen window is
a miserable bug to diagnose.

**Keep the tray icon.** The temptation with an appliance is to make it
completely inescapable. Resist it. You will need to reach settings, and you will
need to quit it, and doing that over remote desktop with no visible UI is
horrible.

**The rotation lives in the dashboard, not the app.** Worth stating plainly
because I confused myself about it later: the host navigates once to a single URL
and stays there. All the page cycling is done inside Home Assistant (see
`06-page-rotation-controller.js`). Keeping the host dumb means the dashboard
works identically in any browser, and the host has nothing to go wrong.

---

## Things I'd tell my past self

**The kiosk is a separate browser session.** When the panel looks stuck but your
desktop browser is fine, reload the kiosk. I lost an hour to this: my desk
session showed healthy state while the panel sat frozen, and I went hunting for
a server fault that didn't exist.

**Signed-URL assets expire.** Camera stream URLs in Home Assistant are signed
and time-limited. A page held open permanently will keep retrying with stale
tokens after a server restart, which logs authentication warnings that look
alarming and are entirely harmless. Either ignore them, filter them in `logger:`,
or have the host refresh the page when the connection returns.

**Long-lived pages accumulate problems.** Anything you leave up for weeks will
find edge cases: expired tokens, leaked timers, memory growth. A nightly refresh
costs nothing and prevents a whole category of mystery.

---

## If you'd rather not write an app

Perfectly reasonable. Good alternatives:

- **A Raspberry Pi with Chromium in kiosk mode** and a systemd unit. Cheapest
  route, well-trodden, plenty of guides.
- **An old tablet** running the Home Assistant companion app with the screen kept
  awake. Zero effort, and you get touch.
- **Fully Kiosk Browser** on Android — genuinely excellent, with motion wake,
  screensaver and remote admin built in.

I built the host because the panel is a Windows machine that also does other
things, and because I wanted it to behave like a fitted appliance. If your
display is dedicated hardware, one of the above will get you there faster.
