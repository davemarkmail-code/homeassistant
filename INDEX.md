# Index — start here

Everything in this repo, and where to find it. **Ctrl+F this page** for what you're
trying to do.

Two levels of navigation:

- **This page** — the whole repo: docs, bridge scripts, Lovelace, kiosk app
- **[lovelace/patterns/README.md](lovelace/patterns/README.md)** — a cookbook of 22
  individual dashboard tiles, each standalone

---

## I want to…

### Get data into Home Assistant

| | Go to |
|---|---|
| Push data from a Windows PC into HA as a sensor | [02-windows-bridge](docs/02-windows-bridge.md) · [`Publish-Sensors.ps1`](bridge/Publish-Sensors.ps1) |
| Show what's playing on my PC | [`Get-NowPlaying.ps1`](bridge/collectors/Get-NowPlaying.ps1) |
| Pull a price or number from an API on a timer | [`Get-Bitcoin.ps1`](bridge/collectors/Get-Bitcoin.ps1) |
| Pull an RSS feed | [`Get-NewsFeed.ps1`](bridge/collectors/Get-NewsFeed.ps1) |
| Store an API token safely on Windows | [`Set-Token.ps1`](bridge/Set-Token.ps1) |
| Run a script hidden at login | [`Start-Bridge.vbs`](bridge/Start-Bridge.vbs) |
| Schedule several collectors on different intervals | [`Run-Bridge.ps1`](bridge/Run-Bridge.ps1) |
| Know if my feed has silently died | [02-windows-bridge](docs/02-windows-bridge.md) |

### Build the dashboard

| | Go to |
|---|---|
| Make a dashboard fill a non-standard screen exactly | [`view-skeleton.yaml`](lovelace/view-skeleton.yaml) · [03-lovelace-design](docs/03-lovelace-design.md) |
| Make tiles look like frosted glass | [`glass.yaml`](lovelace/glass.yaml) |
| Define a tile style once and reuse it | [`button-card-templates.yaml`](lovelace/button-card-templates.yaml) |
| Build a specific tile (22 recipes) | **[lovelace/patterns/](lovelace/patterns/README.md)** |
| Size things for a wall panel rather than a desk | [03-lovelace-design](docs/03-lovelace-design.md) |

### Run it on a screen

| | Go to |
|---|---|
| Full-screen in a browser, hide HA's header | [04-kiosk-setup](docs/04-kiosk-setup.md) |
| Use a proper kiosk app instead of a browser | [06-kiosk-app](docs/06-kiosk-app.md) · [`kiosk/`](kiosk/) |
| Understand why a chart-heavy view takes 2 minutes | [15-history-charts](lovelace/patterns/15-history-charts.md) |

### Work out why something's broken

| | Go to |
|---|---|
| A tile has a gap at the bottom and won't stretch | [05-gotchas](docs/05-gotchas.md) |
| My glass flickers | [05-gotchas](docs/05-gotchas.md) |
| `backdrop-filter` appears to do nothing | [`glass.yaml`](lovelace/glass.yaml) |
| A sensor looks stale but might be fine | [05-gotchas](docs/05-gotchas.md) |
| It all broke after I moved a folder | [05-gotchas](docs/05-gotchas.md) |
| A view is blank with no errors | [04-kiosk-setup](docs/04-kiosk-setup.md) |

---

## Search by keyword

**backdrop-filter, blur, frosted, glass, glassmorphism** → [glass.yaml](lovelace/glass.yaml), [03-lovelace-design](docs/03-lovelace-design.md), [05-gotchas](docs/05-gotchas.md)
**button-card, custom_fields, templates, nested cards, display block** → [button-card-templates.yaml](lovelace/button-card-templates.yaml), [05-gotchas](docs/05-gotchas.md)
**grid, grid-template-areas, 1fr, minmax, gaps, alignment** → [view-skeleton.yaml](lovelace/view-skeleton.yaml)
**kiosk, fullscreen, ultrawide, 2560x720, WebView2, wall display** → [04-kiosk-setup](docs/04-kiosk-setup.md), [06-kiosk-app](docs/06-kiosk-app.md)
**WPF, .NET 8, tray icon, single instance, monitor by name** → [06-kiosk-app](docs/06-kiosk-app.md)
**REST API, POST, /api/states, push sensor, create entity** → [02-windows-bridge](docs/02-windows-bridge.md)
**PowerShell, collector, loop, interval, mutex, $PSScriptRoot** → [Run-Bridge.ps1](bridge/Run-Bridge.ps1), [05-gotchas](docs/05-gotchas.md)
**token, DPAPI, ProtectedData, secret, long-lived access token** → [Set-Token.ps1](bridge/Set-Token.ps1)
**Startup folder, shell:startup, run at login, hidden window, vbs** → [Start-Bridge.vbs](bridge/Start-Bridge.vbs)
**now playing, media session, Apple Music, Spotify, artwork** → [Get-NowPlaying.ps1](bridge/collectors/Get-NowPlaying.ps1), [21-now-playing](lovelace/patterns/21-now-playing.md)
**stale sensor, last_updated, heartbeat, dead feed** → [05-gotchas](docs/05-gotchas.md)
**UTF-8, mangled characters, accents, encoding** → [02-windows-bridge](docs/02-windows-bridge.md)
**energy flow, animated SVG, power routing, solar diagram** → [01-solar-flow](lovelace/patterns/01-solar-flow.md)
**chart, apexcharts, plotly, recorder, slow to load, blank view** → [15-history-charts](lovelace/patterns/15-history-charts.md)
**input_select, segmented control, today, 7 days, period** → [14-period-picker](lovelace/patterns/14-period-picker.md)
**camera, snapshot, entity_picture, object-fit** → [03-camera-grid](lovelace/patterns/03-camera-grid.md)
**confirm, are you sure, countdown, destructive action** → [12-confirm-dialog](lovelace/patterns/12-confirm-dialog.md)
**sub-view, drilldown, back button, hidden view, navigate** → [13-drilldown-views](lovelace/patterns/13-drilldown-views.md)

---

## Every file

### `docs/` — how and why
| File | Contents |
|---|---|
| [01-architecture.md](docs/01-architecture.md) | System diagram, why files sit between collectors and HA, push-vs-poll consequences |
| [02-windows-bridge.md](docs/02-windows-bridge.md) | The core pattern, loop design, file formats, token storage, logging, health checks, troubleshooting |
| [03-lovelace-design.md](docs/03-lovelace-design.md) | One-card-per-view model, grid standard, templates, glass, sizing for a wall |
| [04-kiosk-setup.md](docs/04-kiosk-setup.md) | kiosk-mode, browser choice, client-side rendering, slow views, rotation |
| [05-gotchas.md](docs/05-gotchas.md) | Eleven traps, each symptom → cause → fix |
| [06-kiosk-app.md](docs/06-kiosk-app.md) | The WPF/WebView2 appliance — config reference, build, browser-only alternative |

### `bridge/` — Windows → HA
| File | Demonstrates | Runs alone? |
|---|---|---|
| [README.md](bridge/README.md) | Ten-minute setup, adding a collector, troubleshooting | — |
| [`Run-Bridge.ps1`](bridge/Run-Bridge.ps1) | Staggered loop, single-instance mutex, self-trimming log | Needs collectors |
| [`Publish-Sensors.ps1`](bridge/Publish-Sensors.ps1) | Files → HA sensors, UTF-8, 255-char guard, heartbeat | Needs a token |
| [`Set-Token.ps1`](bridge/Set-Token.ps1) | DPAPI token encryption | Yes |
| [`Start-Bridge.vbs`](bridge/Start-Bridge.vbs) | PowerShell with no console window | Yes |
| [`collectors/Get-NowPlaying.ps1`](bridge/collectors/Get-NowPlaying.ps1) | Windows media session — any player | Yes |
| [`collectors/Get-Bitcoin.ps1`](bridge/collectors/Get-Bitcoin.ps1) | Simplest possible API collector | Yes |
| [`collectors/Get-NewsFeed.ps1`](bridge/collectors/Get-NewsFeed.ps1) | Any RSS/Atom feed | Yes |

### `lovelace/` — dashboard
| File | Contents |
|---|---|
| [`view-skeleton.yaml`](lovelace/view-skeleton.yaml) | A complete working grid view to build from |
| [`button-card-templates.yaml`](lovelace/button-card-templates.yaml) | Tile, header chip, button and plain-text templates |
| [`glass.yaml`](lovelace/glass.yaml) | The full glass recipe with runtime tint switching |
| [`patterns/`](lovelace/patterns/README.md) | **22 individual tile recipes** — see its own index |

### `kiosk/` — the Windows app
| File | Contents |
|---|---|
| [README.md](kiosk/README.md) | What to copy in, what to exclude, why it's safe to publish |
| `src/` `build.ps1` `Installer/` | WPF app — publishes as a self-contained portable exe |

---

## The three things worth knowing above all

Hard to find written down anywhere, and each one presents as a completely different
problem than it actually is:

1. **A nested button-card won't stretch inside a `display:block` wrapper** — even with
   `height:100%` set in two places. Presents as a mysterious gap under every tile.
   → [05-gotchas](docs/05-gotchas.md)

2. **An opacity animation on an ancestor silently disables `backdrop-filter`** on
   everything inside it. Presents as glass that flickers on every re-render.
   → [05-gotchas](docs/05-gotchas.md)

3. **`backdrop-filter` over a flat colour is mathematically invisible.** You need
   texture behind it before blur does anything at all.
   → [glass.yaml](lovelace/glass.yaml)

---

## Before you run anything

There are **no working credentials anywhere in this repo**. Addresses are placeholders
(`homeassistant.local`, `your-tenant.example.com`), tokens are created locally by you,
and `.gitignore` blocks `*.dat`, real config files, live data dumps and logs.

Entity IDs in the examples are illustrative — `sensor.solar_power`, `vacuum.robot` and
so on. Nothing will work unmodified; swap in your own.

**Suggested reading order if you're starting cold:**
[01-architecture](docs/01-architecture.md) →
[bridge/README](bridge/README.md) →
[03-lovelace-design](docs/03-lovelace-design.md) →
[patterns/](lovelace/patterns/README.md)

And read [05-gotchas](docs/05-gotchas.md) before you spend an hour on something that's
already in there.
