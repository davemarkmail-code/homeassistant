# Home Assistant Office Dashboard — a working reference

A wall-mounted Home Assistant dashboard for an ultrawide panel, plus the Windows
bridge that feeds it data Home Assistant can't reach on its own.

This is a **pick-and-mix dump**, not a product. Nothing here is packaged, versioned
or supported. Take the bits you want, ignore the rest, break it however you like.

It exists because most of this was worked out the hard way and almost none of it is
written down anywhere else.

---

## What's actually here

| Piece | What it does | Worth stealing if you… |
|---|---|---|
| **Windows→HA bridge** | PowerShell collectors write text files; a publisher POSTs them to HA as sensors | …want Windows-only data (now playing, Outlook, Teams, ticket systems) inside HA |
| **Lovelace dashboard** | Full-screen `custom:button-card` layout for a 2560×720 panel | …are building a kiosk dashboard and want a grid that actually fills the screen |
| **Glass design system** | `backdrop-filter` over a textured backdrop, with a tint you can swap | …want frosted-glass tiles that don't look flat |
| **Kiosk app** | WPF/WebView2 appliance — targets a monitor by name, auto-reconnects, hides the cursor | …are tired of browser kiosk mode nearly working |
| **Gotchas** | The traps that cost real hours | …are doing any of the above |

If you read one file, make it **[docs/05-gotchas.md](docs/05-gotchas.md)**. It's the
part you can't get from documentation elsewhere.

---

## → [INDEX.md](INDEX.md) — the full contents page

**Start there.** It lists every file, has an "I want to…" table, and a keyword block
so you can search for the thing you're stuck on rather than reading top to bottom.

There's a second index inside
**[lovelace/patterns/](lovelace/patterns/README.md)** — a cookbook of **22 individual
dashboard tiles**, each standalone. Solar flow diagram, camera grid, climate dial,
now-playing, confirmation dialogs, drilldown views and so on. Take one, take all of
them, take none.

---

## Documentation

1. **[01-architecture.md](docs/01-architecture.md)** — how the pieces fit together
2. **[02-windows-bridge.md](docs/02-windows-bridge.md)** — the PowerShell → HA REST sensor pattern
3. **[03-lovelace-design.md](docs/03-lovelace-design.md)** — grid standard, button-card templates, the glass recipe
4. **[04-kiosk-setup.md](docs/04-kiosk-setup.md)** — running it full-screen on a dedicated panel
5. **[05-gotchas.md](docs/05-gotchas.md)** — the hard-won stuff
6. **[06-kiosk-app.md](docs/06-kiosk-app.md)** — the native kiosk appliance

Plus **[lovelace/patterns/](lovelace/patterns/README.md)** — 22 tile recipes with
their own index.

---

## The short version

Home Assistant is excellent at talking to devices and hopeless at knowing what's
happening on a Windows PC. There's no integration that tells HA what Apple Music is
playing, what your next Outlook meeting is, or how many tickets are in your work
queue.

So a PowerShell loop runs on the PC. Every few seconds it collects that data, writes
it to plain text files, and POSTs it into Home Assistant's REST API as sensors. HA
treats them like any other entity — templates, automations, dashboards, history.

The dashboard is then a single full-screen `custom:button-card` per view, laid out on
a CSS grid, with nested button-cards as tiles. That sounds odd if you're used to
stacking normal HA cards, but it's the only way I found to get pixel-accurate control
on a non-standard aspect ratio.

---

## Requirements

- Home Assistant (any recent version) with a long-lived access token
- [button-card](https://github.com/custom-cards/button-card) via HACS — everything leans on this
- Windows PC for the bridge, PowerShell 5.1+ (built in)
- Optional: [kiosk-mode](https://github.com/NemesisRE/kiosk-mode) to hide the HA header and sidebar

---

## Before you use any of this

**Nothing here contains working credentials, and you shouldn't add any to a repo.**

Every config file is a `.example`. Tokens are stored encrypted on disk outside the
project (see [02-windows-bridge.md](docs/02-windows-bridge.md)). The `.gitignore`
blocks `*.dat`, real config files and live data dumps — check it still does before
you commit anything of your own.

IPs, hostnames, tenant URLs, device serials and entity IDs in these docs are
placeholders. Swap in your own.

---

## Licence

MIT — see [LICENSE](LICENSE). Do what you like with it.

## Credit where it's due

Built with a lot of trial and error, and a fair amount of help from Claude. The
gotchas file in particular is a record of things that were only worked out by
measuring the DOM until the truth fell out.
