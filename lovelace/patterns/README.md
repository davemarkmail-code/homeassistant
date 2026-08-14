# Tile patterns — a cookbook

Twenty-two tiles, each written up on its own. Take one, take all of them, take none.
Nothing here depends on anything else unless it says so at the top.

**Ctrl+F this page** for the thing you're trying to build.

Every pattern assumes [`custom:button-card`](https://github.com/custom-cards/button-card).
Most assume nothing else. Where a pattern needs an extra HACS resource it says so in
its **Needs** line, and there's usually a note on doing it without.

---

## Quick answers

| I want a tile that… | Pattern |
|---|---|
| Shows energy moving between solar, house, battery and grid | [01-solar-flow](01-solar-flow.md) |
| Shows a device's state, a few stats and some buttons | [02-device-status](02-device-status.md) |
| Shows several camera snapshots that open larger | [03-camera-grid](03-camera-grid.md) |
| Shows a temperature dial with a setpoint | [04-climate-dial](04-climate-dial.md) |
| Shows ink / toner / drum levels as bars | [05-printer-consumables](05-printer-consumables.md) |
| Shows CPU, memory and disk with progress bars | [06-system-stats](06-system-stats.md) |
| Shows network up/down speeds | [07-network-throughput](07-network-throughput.md) |
| Rotates through news headlines with images | [08-news-carousel](08-news-carousel.md) |
| Shows my next meeting and a join link | [09-next-meeting](09-next-meeting.md) |
| Shows a ticket / work queue with counts | [10-ticket-queue](10-ticket-queue.md) |
| Gives me a header with page navigation and a clock | [11-header-bar](11-header-bar.md) |
| Asks "are you sure?" before doing something destructive | [12-confirm-dialog](12-confirm-dialog.md) |
| Opens a detail page and comes back again | [13-drilldown-views](13-drilldown-views.md) |
| Lets me pick Today / 7 days / 30 days for a chart | [14-period-picker](14-period-picker.md) |
| Draws a filled history chart in brand colours | [15-history-charts](15-history-charts.md) |
| Shows my current electricity rate and standing charge | [16-tariff-tile](16-tariff-tile.md) |
| Shows an air purifier's speed and filter life | [17-purifier-tile](17-purifier-tile.md) |
| Toggles a light and shows brightness | [18-light-tile](18-light-tile.md) |
| Shows whether a cloud service is up | [19-service-status](19-service-status.md) |
| Shows the weather without looking like everyone else's | [20-weather-tile](20-weather-tile.md) |
| Shows what's playing with artwork | [21-now-playing](21-now-playing.md) |
| Switches a receiver / TV between sources | [22-source-select](22-source-select.md) |

---

## Search by keyword

**flow diagram, energy flow, animated, SVG, dashed line, power routing** → [01-solar-flow](01-solar-flow.md)
**vacuum, mower, robot, battery %, consumables, dock, start, pause** → [02-device-status](02-device-status.md)
**camera, snapshot, live view, still image, lock, unlock, doorbell** → [03-camera-grid](03-camera-grid.md)
**thermostat, hvac_action, setpoint, heating, boost, arc, dial** → [04-climate-dial](04-climate-dial.md)
**printer, ink, toner, drum, belt, fuser, CMYK, progress bar** → [05-printer-consumables](05-printer-consumables.md)
**CPU, memory, disk, uptime, bar, percentage, threshold colour** → [06-system-stats](06-system-stats.md)
**download, upload, Mbps, throughput, bandwidth, speed** → [07-network-throughput](07-network-throughput.md)
**RSS, news, headlines, carousel, rotate, image, index sensor** → [08-news-carousel](08-news-carousel.md)
**calendar, meeting, next event, join link, Teams, countdown** → [09-next-meeting](09-next-meeting.md)
**tickets, SLA, queue, breached, triage, helpdesk, counts** → [10-ticket-queue](10-ticket-queue.md)
**header, nav, page navigation, active tab, clock, date, restart, shutdown** → [11-header-bar](11-header-bar.md)
**confirm, are you sure, countdown, cancel, destructive, guard** → [12-confirm-dialog](12-confirm-dialog.md)
**drilldown, detail page, sub-view, back button, navigate, hidden view** → [13-drilldown-views](13-drilldown-views.md)
**period, today, yesterday, 7 days, input_select, segmented control** → [14-period-picker](14-period-picker.md)
**chart, graph, plotly, apexcharts, fill, area, dotted, dual axis** → [15-history-charts](15-history-charts.md)
**tariff, Octopus, rate, p/kWh, standing charge, import, export, agile** → [16-tariff-tile](16-tariff-tile.md)
**purifier, fan, filter life, air quality, PM2.5, speed** → [17-purifier-tile](17-purifier-tile.md)
**light, brightness, dimmer, toggle, colour, lamp** → [18-light-tile](18-light-tile.md)
**service status, operational, outage, uptime, cloud, incident** → [19-service-status](19-service-status.md)
**weather, forecast, temperature, humidity, feels like, condition icon** → [20-weather-tile](20-weather-tile.md)
**now playing, media, artwork, artist, album, progress, Apple Music, Spotify** → [21-now-playing](21-now-playing.md)
**source, input, receiver, HDMI, AV, select, source_list** → [22-source-select](22-source-select.md)

---

## How each pattern is laid out

Every file follows the same shape, so you can skim to the bit you want:

| Section | What's in it |
|---|---|
| **What it is** | One paragraph and a description of what you're looking at |
| **Needs** | Entities and HACS resources. If it's just button-card, it says so |
| **The tile** | Complete YAML you can paste. No `...` gaps |
| **How it works** | Only the two or three techniques that aren't obvious |
| **Gotchas** | Things that cost real time to work out |
| **Adapting it** | The bits most likely to need changing for your setup |

---

## Before you paste anything

**Entity IDs are placeholders.** Every pattern uses names like
`sensor.example_cpu_percent`. Yours will differ. Swap them.

**These tiles are sized for a 2560×720 ultrawide.** Heights, font sizes and icon
sizes assume that. On a normal monitor or a tablet they'll look oversized until you
scale them — see [03-lovelace-design.md](../../docs/03-lovelace-design.md) for the
sizing model.

**The grid wrapper matters more than the tile.** Most "why is there a gap at the
bottom" problems are the containing view, not the tile. Start with
[`view-skeleton.yaml`](../view-skeleton.yaml).

**Set wrappers to `display:grid`, never `display:block`.** A nested button-card will
not stretch inside a block wrapper even with `height:100%` set. This is the single
most expensive trap in this repo — [05-gotchas.md](../../docs/05-gotchas.md).

---

## If you take nothing else

Three techniques recur across almost every pattern here, and they're what make the
difference between a tile that looks hand-built and one that looks like a stock card:

1. **One button-card per view, with `custom_fields` for each tile.** Not thirty cards
   in a grid. One card, one grid definition, named areas. It's why the alignment holds.
2. **JavaScript templates return HTML, not just text.** `[[[ ]]]` blocks can build a
   whole block of markup with inline styles, computed colours and conditional sections.
   Most of these patterns are one template function.
3. **Colour carries the state.** No badges, no "ON" text. The icon and the number
   change colour and that's the entire status indicator.

---

## Credit

These were built iteratively against a real dashboard over several months, mostly in
conversation with Claude (Anthropic). The awkward bits — the grid stretching problem,
the backdrop-filter interactions, the template escaping — were worked out by breaking
them repeatedly and writing down what fixed it. Corrections and additions welcome.

MIT. Do what you like with them.
