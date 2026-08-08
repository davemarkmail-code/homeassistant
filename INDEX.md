# Index — find what you need

Everything here is standalone. Nothing depends on anything else unless it says so.
**Ctrl+F this page** for what you're trying to do.

---

## Quick answers

| I want to… | Go to |
|---|---|
| Get Windows data into HA as a sensor | [02-windows-bridge.md](docs/02-windows-bridge.md) · [`Publish-Sensors.ps1`](bridge/Publish-Sensors.ps1) |
| Show what's playing on my PC in HA | [`Get-NowPlaying.ps1`](bridge/collectors/Get-NowPlaying.ps1) |
| Pull a price / number from an API on a timer | [`Get-Bitcoin.ps1`](bridge/collectors/Get-Bitcoin.ps1) |
| Pull an RSS feed into HA | [`Get-NewsFeed.ps1`](bridge/collectors/Get-NewsFeed.ps1) |
| Store an API token safely on Windows | [`Set-Token.ps1`](bridge/Set-Token.ps1) |
| Run something hidden at login | [`Start-Bridge.vbs`](bridge/Start-Bridge.vbs) |
| Schedule several collectors on different intervals | [`Run-Bridge.ps1`](bridge/Run-Bridge.ps1) |
| Make a dashboard fill an ultrawide exactly | [`view-skeleton.yaml`](lovelace/view-skeleton.yaml) · [03-lovelace-design.md](docs/03-lovelace-design.md) |
| Make tiles look like frosted glass | [`glass.yaml`](lovelace/glass.yaml) |
| Reuse one tile style everywhere | [`button-card-templates.yaml`](lovelace/button-card-templates.yaml) |
| Run it full-screen on a wall panel | [04-kiosk-setup.md](docs/04-kiosk-setup.md) |
| Use a proper kiosk app instead of a browser | [06-kiosk-app.md](docs/06-kiosk-app.md) · [`kiosk/`](kiosk/) |
| Work out why my thing is broken | [05-gotchas.md](docs/05-gotchas.md) |

---

## Search by keyword

**backdrop-filter, blur, frosted, glass, glassmorphism** → [glass.yaml](lovelace/glass.yaml), [03-lovelace-design.md](docs/03-lovelace-design.md), [05-gotchas.md](docs/05-gotchas.md)
**button-card, custom_fields, templates, nested cards** → [button-card-templates.yaml](lovelace/button-card-templates.yaml), [03-lovelace-design.md](docs/03-lovelace-design.md)
**grid, grid-template-areas, 1fr, minmax, layout, gaps, alignment** → [view-skeleton.yaml](lovelace/view-skeleton.yaml), [03-lovelace-design.md](docs/03-lovelace-design.md)
**gap at bottom, card won't stretch, display block** → [05-gotchas.md](docs/05-gotchas.md)
**flicker, flashing, opacity, animation** → [05-gotchas.md](docs/05-gotchas.md)
**kiosk, fullscreen, ultrawide, 2560x720, WebView2, panel, wall display** → [04-kiosk-setup.md](docs/04-kiosk-setup.md), [06-kiosk-app.md](docs/06-kiosk-app.md)
**WPF, .NET 8, appliance, tray icon, single instance, monitor by name, auto-reconnect** → [06-kiosk-app.md](docs/06-kiosk-app.md)
**REST API, POST, /api/states, push sensor, custom sensor, create entity** → [02-windows-bridge.md](docs/02-windows-bridge.md), [Publish-Sensors.ps1](bridge/Publish-Sensors.ps1)
**PowerShell, collector, loop, interval, scheduling, mutex** → [Run-Bridge.ps1](bridge/Run-Bridge.ps1)
**token, DPAPI, ProtectedData, secret, long-lived access token** → [Set-Token.ps1](bridge/Set-Token.ps1)
**Startup folder, shell:startup, run at login, hidden window, vbs** → [Start-Bridge.vbs](bridge/Start-Bridge.vbs), [04-kiosk-setup.md](docs/04-kiosk-setup.md)
**now playing, media session, Apple Music, Spotify, GSMTC, WinRT, artwork** → [Get-NowPlaying.ps1](bridge/collectors/Get-NowPlaying.ps1)
**RSS, XML feed, news, headlines, media:thumbnail** → [Get-NewsFeed.ps1](bridge/collectors/Get-NewsFeed.ps1)
**stale sensor, last_updated, heartbeat, dead feed, monitoring** → [05-gotchas.md](docs/05-gotchas.md), [Publish-Sensors.ps1](bridge/Publish-Sensors.ps1)
**$PSScriptRoot, absolute path, moved folder, broke after move** → [05-gotchas.md](docs/05-gotchas.md)
**UTF-8, mangled characters, accents, encoding** → [02-windows-bridge.md](docs/02-windows-bridge.md)
**logging, log overwritten, lost errors** → [Run-Bridge.ps1](bridge/Run-Bridge.ps1), [05-gotchas.md](docs/05-gotchas.md)

---

## By file

### `docs/`
| File | Contents |
|---|---|
| [01-architecture.md](docs/01-architecture.md) | System diagram, why files sit between collectors and HA, push-vs-poll consequences |
| [02-windows-bridge.md](docs/02-windows-bridge.md) | The 20-line core pattern, loop design, file formats, token storage, logging, health checks, troubleshooting table |
| [03-lovelace-design.md](docs/03-lovelace-design.md) | One-card-per-view model, grid standard, templates, the five ingredients of glass, sizing for a wall |
| [04-kiosk-setup.md](docs/04-kiosk-setup.md) | kiosk-mode, browser choice, client-side rendering, slow chart views, view rotation |
| [05-gotchas.md](docs/05-gotchas.md) | Eleven traps, each with symptom → cause → fix |
| [06-kiosk-app.md](docs/06-kiosk-app.md) | The WPF/WebView2 kiosk appliance — what it does, config reference, build, browser-only alternative |

### `kiosk/`
| File | Contents |
|---|---|
| [README.md](kiosk/README.md) | What to copy in, what to exclude, why it's safe to publish |
| `src/` `build.ps1` `Installer/` | The WPF app itself — self-contained portable exe, no .NET needed by end users |

### `bridge/`
| File | What it demonstrates | Runs alone? |
|---|---|---|
| [README.md](bridge/README.md) | Ten-minute setup, adding your own collector, troubleshooting | — |
| [`Run-Bridge.ps1`](bridge/Run-Bridge.ps1) | Staggered-interval loop, single-instance mutex, self-trimming log | Needs collectors |
| [`Publish-Sensors.ps1`](bridge/Publish-Sensors.ps1) | Files → HA sensors, UTF-8 handling, 255-char guard, heartbeat | Needs a token |
| [`Set-Token.ps1`](bridge/Set-Token.ps1) | Encrypting a token to disk with DPAPI | Yes |
| [`Start-Bridge.vbs`](bridge/Start-Bridge.vbs) | Launching PowerShell with no window | Yes |

### `bridge/collectors/`
| File | Source | Auth |
|---|---|---|
| [`Get-NowPlaying.ps1`](bridge/collectors/Get-NowPlaying.ps1) | Windows media session — works with any player | None |
| [`Get-Bitcoin.ps1`](bridge/collectors/Get-Bitcoin.ps1) | CoinGecko — the "fetch a number" template | None |
| [`Get-NewsFeed.ps1`](bridge/collectors/Get-NewsFeed.ps1) | Any RSS/Atom feed | None |

> **Not included, deliberately:** collectors for Microsoft Graph (calendar, mail,
> Teams), OAuth ticket systems and vendor cloud APIs. They're all the same shape as
> `Get-Bitcoin.ps1` — fetch, format, write one file — but each needs its own app
> registration and tenant config, so a generic copy would mislead more than it helps.
> [02-windows-bridge.md](docs/02-windows-bridge.md) covers the auth pattern.

### `lovelace/`
| File | Contents |
|---|---|
| [`view-skeleton.yaml`](lovelace/view-skeleton.yaml) | A complete working grid view to build from |
| [`button-card-templates.yaml`](lovelace/button-card-templates.yaml) | Tile, header chip, small button and plain-text templates |
| [`glass.yaml`](lovelace/glass.yaml) | The full glass recipe with the runtime tint switcher |

---

## If you take nothing else

Three things here are genuinely hard to find written down anywhere:

1. **Nested button-cards won't stretch inside a `display:block` wrapper** — even with
   `height:100%` set in two places. Presents as a mysterious gap at the bottom of every
   tile. → [05-gotchas.md](docs/05-gotchas.md)
2. **An opacity animation on an ancestor silently disables `backdrop-filter`** on
   everything inside it. Presents as glass that flickers on every re-render. →
   [05-gotchas.md](docs/05-gotchas.md)
3. **`backdrop-filter` over a flat colour is mathematically invisible.** You need
   texture behind it before blur does anything at all. → [glass.yaml](lovelace/glass.yaml)

---

## Before you run anything

There are **no working credentials anywhere in this repo**. All addresses are
placeholders (`homeassistant.local`, `your-tenant.example.com`), all tokens are
created locally by you, and `.gitignore` blocks `*.dat`, real config files, live data
dumps and logs.

Start with [bridge/README.md](bridge/README.md). Run one collector by hand and look at
the file it writes — that's the whole system in miniature.
