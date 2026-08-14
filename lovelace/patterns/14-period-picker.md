# 14 — Period picker

A segmented control that switches a chart or a set of figures between Today,
Yesterday, 7 days, 30 days and 12 months.

```
┌────────┬───────────┬────────┬─────────┬───────────┐
│ Today  │ Yesterday │ 7 Days │ 30 Days │ 12 Months │
└────────┴───────────┴────────┴─────────┴───────────┘
     ▲ active, highlighted
```

**Needs:** an `input_select` helper. No bridge, no extra HACS.

---

## The idea

The picker doesn't know anything about charts. It writes to an `input_select`, and
everything else on the page reads that. One piece of state, any number of consumers —
charts, figure tiles, headings.

**Create the helper** (Settings → Devices & Services → Helpers → Dropdown):

```
input_select.solar_period
  options: Today, Yesterday, 7 Days, 30 Days, 12 Months
```

---

## The buttons

One button per option, each writing its own value:

```yaml
type: custom:button-card
name: 7 Days
entity: input_select.solar_period
tap_action:
  action: call-service
  service: input_select.select_option
  target: { entity_id: input_select.solar_period }
  data: { option: 7 Days }
triggers_update:
  - input_select.solar_period
styles:
  card:
    - height: 46px
    - border-radius: 12px
    - background: >
        [[[ return states['input_select.solar_period'].state === '7 Days'
              ? 'rgba(77,152,255,0.18)' : 'rgba(255,255,255,0.05)'; ]]]
    - border: >
        [[[ return states['input_select.solar_period'].state === '7 Days'
              ? '1px solid rgba(77,152,255,0.6)' : '1px solid rgba(255,255,255,0.10)'; ]]]
```

Repeat with the option name changed. Yes, it's repetitive — but it's readable, and
each button is independently editable.

### Cycling instead

If space is tight, one button that steps through:

```yaml
tap_action:
  action: call-service
  service: input_select.select_next
  target: { entity_id: input_select.solar_period }
  data: { cycle: true }
```

Good for a phone or a tight header. Less good when someone needs to see the options.

---

## Consuming it

Anything that should react adds the helper to `triggers_update` and branches on its
value:

```yaml
triggers_update:
  - input_select.solar_period
  - sensor.solar_today
  - sensor.solar_7d
custom_fields:
  figure: |
    [[[
      const p = states['input_select.solar_period'].state;
      const map = {
        'Today':     'sensor.solar_today',
        'Yesterday': 'input_number.yesterday_solar',
        '7 Days':    'sensor.solar_7d',
        '30 Days':   'sensor.solar_30d',
        '12 Months': 'sensor.solar_12m'
      };
      const e = states[map[p]];
      return `<div style="font-size:26px;font-weight:700">
                ${e ? Number(e.state).toFixed(1) : '—'} kWh</div>`;
    ]]]
```

**List every sensor you might read in `triggers_update`**, not just the currently
selected one. Otherwise the tile won't refresh when the underlying value changes —
only when you press a button. That's a confusing bug to chase.

---

## Driving a chart

Charting cards take a fixed range, so the usual approach is one card per period with
`conditional` wrappers:

```yaml
type: conditional
conditions:
  - entity: input_select.solar_period
    state: 7 Days
card:
  type: custom:apexcharts-card
  graph_span: 7d
  # …
```

Heavier than it sounds — each hidden card still exists in the config, though only the
matching one renders. See [15-history-charts](15-history-charts.md).

---

## Gotchas

**`input_select` is global state.** Every device looking at that dashboard shares it.
Change the period on your phone and the wall tablet changes too. Usually fine, occasionally
surprising — if you want them independent, you need one helper per surface.

**Option strings must match exactly.** The button writes a string and the template
compares strings. A trailing space or a different capitalisation gives you a picker
where nothing appears selected and nothing updates.

**Give it a sensible default.** An `input_select` that has never been set reads
`unknown`, so guard your lookups (`map[p] || map['Today']`) or the tile renders blank
on first load.

**Nested button-cards need `display: grid` on their wrapper** if you're laying the
buttons out inside another button-card's grid, or they won't fill their cells. See
[../../docs/05-gotchas.md](../../docs/05-gotchas.md).
