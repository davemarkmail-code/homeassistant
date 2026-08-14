# 15 — History charts

Charting from Home Assistant's recorder database — what to use, and what it costs.

**Needs:** [apexcharts-card](https://github.com/RomRider/apexcharts-card) or
[plotly-graph-card](https://github.com/dbuezas/lovelace-plotly-graph-card) from HACS.
Both are worth having; they're good at different things.

---

## Which card

| | apexcharts-card | plotly-graph-card |
|---|---|---|
| Bar charts, monthly totals | **better** | possible |
| Many series, live updating | fine | **better** |
| Redraw behaviour | redraws the chart | updates traces only |
| Config style | YAML, approachable | YAML, more knobs |

**The redraw difference matters on a wall panel.** A chart that redraws its whole
canvas on every state change flashes visibly. Plotly updates the traces in place, so
it's the better choice for anything live. Apex is the nicer tool for a static "last 12
months" bar chart that only changes daily.

Avoid the built-in `history-graph` card for anything you'll stare at — it redraws
aggressively and looks it.

---

## A filled area chart

```yaml
type: custom:apexcharts-card
graph_span: 24h
header:
  show: true
  title: Power today
apex_config:
  chart: { height: 320 }
  stroke: { width: 2, curve: smooth }
  fill: { type: gradient, gradient: { opacityFrom: 0.45, opacityTo: 0 } }
  legend: { show: true, position: top }
series:
  - entity: sensor.solar_power
    name: Solar
    color: '#f2cc3d'
    stroke_width: 2
  - entity: sensor.house_power
    name: House
    color: '#4d98ff'
  - entity: sensor.grid_power
    name: Grid
    color: '#ff5656'
```

A gradient fill fading to transparent reads much better on a dark dashboard than a
flat block colour, and costs one config block.

---

## Monthly totals

```yaml
type: custom:apexcharts-card
graph_span: 365d
span: { end: month }
chart_type: bar
apex_config:
  chart: { stacked: true }
series:
  - entity: sensor.solar_energy
    type: column
    group_by: { func: max, duration: 1month }
    name: Solar
```

`group_by` with `func: max` is the right choice for **total-increasing** sensors like
energy meters — you want the highest value in each bucket, not the average. Using
`avg` here is a common mistake that produces plausible-looking nonsense.

---

## The thing nobody warns you about

**Chart-heavy views are slow — much slower than they look.**

A view with two or three history charts can take **90 to 120 seconds** to paint on
first load. During that time the page is completely blank with **no console errors**.
It looks exactly like a broken config.

The bottleneck isn't your browser or your CSS — it's Home Assistant querying the
recorder database. Plotly logs `waiting for loading` a few hundred times while it
waits.

Three consequences worth internalising:

- **Don't revert a config because a chart view is blank.** Wait longer first.
- **A faster kiosk machine won't help.** This is server-side.
- **`graph_span: 365d` is expensive.** Every reload re-queries a year of history.

If a chart view is your default landing page, consider making it *not* the default —
a wall panel that shows nothing for two minutes after every restart is a poor
experience.

---

## Making it faster

**Shorten the span.** 7 days instead of 365 is dramatically cheaper.

**Use long-term statistics** where the card supports it. HA downsamples history into
hourly statistics for `total`/`measurement` sensors, and querying those is far
cheaper than raw states:

```yaml
statistics:
  type: mean
  period: hour
```

**Check your recorder retention.** The default keeps 10 days. Asking for a 365-day
chart when the recorder only holds 10 days of raw states gives you an empty chart and
a slow query — the data has to come from statistics instead.

**Put heavy charts on their own view** so they don't slow down pages you use daily.

---

## Gotchas

**Charts don't inherit your card styling.** They bring their own background and
padding. Wrap them or set `apex_config.chart.background: transparent` to blend in.

**Height needs setting explicitly** or the card sizes itself unhelpfully inside a
fixed grid.

**Colours should match your tiles.** Reusing the same hex values across charts and
tiles is what makes a dashboard feel designed rather than assembled.

**`graph_span` is not the same as the recorder's retention.** Asking for more than you
keep silently gives you a shorter chart.
