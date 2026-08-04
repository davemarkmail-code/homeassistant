# Roadmap

Built iteratively. Each stage is independently shippable and leaves the
appliance in a working state.

## Stage 1 — Foundation ✅ (this drop)

- Project structure, solution, build/publish config
- Architecture decision (WPF) documented
- Settings model + JSON persistence (`settings.json`)
- Serilog logging to `%LocalAppData%\XEON Dashboard\Logs`
- Monitor detection by EDID friendly name (CCD API); **exit silently** if absent
- Borderless, chromeless window pinned to the target monitor in physical pixels
- WebView2 with a **persistent profile** (login survives reboot)
- Startup sequence: delay → network check → HA reachability → navigate
- Basic reconnect loop + loading overlay

## Stage 2 — Tray + lifecycle ✅

- System tray icon with menu: Show Dashboard, Reload Dashboard, Settings, Exit
- Single-instance enforcement (named mutex)
- Explicit shutdown so windows can open/close without killing the app
- Stock tray icon fallback until a custom `app.ico` is supplied

## Stage 3 — Settings window ✅

- MVVM settings editor for every documented key
- Live monitor picker (detected displays + resolution, plus "Primary display")
- First-run flow: opens Settings before launching the kiosk
- Monitor-not-found handling: opens Settings (or exits silently if configured)
- "Start with Windows" toggle manages the Startup-folder shortcut automatically
- Friendly on-screen notice about dashboard scaling within Home Assistant

## Stage 4 — Input & interaction

- Global keyboard shortcuts:
  - `Ctrl+Alt+R` reload dashboard
  - `Ctrl+Alt+Shift+Q` exit
  - `Ctrl+Alt+F` toggle fullscreen/windowed (for maintenance)
- Optional mouse auto-hide after inactivity; reappear on movement

## Stage 5 — Resilience hardening

- Robust reconnect: dedicated loading page, capped/backoff retries, watchdog
- Handle monitor hot-plug (display appears/disappears at runtime)
- Startup-with-Windows via a per-user Startup task (no admin)

## Stage 6 — Packaging ✅

- Self-contained publish (bundled .NET runtime — no user install)
- Portable single-file exe publish profile
- WebView2 runtime auto-detection + silent bootstrap on first run
- No-admin Inno Setup installer (Start Menu / startup shortcuts, WebView2, clean uninstall)
- One-command `build.ps1`; see `Docs/PACKAGING.md`

## Still to come

- **Stage 4** — keyboard shortcuts (reload / exit / toggle fullscreen) and optional mouse auto-hide
- **Stage 5** — resilience hardening (watchdog, monitor hot-plug, backoff)
- Optional code-signing to remove the SmartScreen prompt

## Future features (explicitly deferred; design for extensibility)

Doorbell popup · camera overlay · weather alerts · Spotify / Apple Music
overlay · Teams notifications · Grafana dashboards · MagicMirror support ·
Halo ITSM dashboard · multiple dashboard tabs · scheduled brightness.

These are best served by an **overlay/plugin** model layered over the WebView
host — worth designing the extension seam around Stage 3–5, but no
implementation until the core is stable.
