# 16 — Tariff tile

Current import and export rates, standing charge, and what yesterday cost or earned.

```
┌──────────────────────────────────────────┐
│ 🐙 Octopus Tariff                        │
│ ──────────────────────────────────────── │
│  IMPORT NOW   EXPORT NOW    NEXT RATE    │
│    34.3p        12.0p         8.0p       │
│                                          │
│  STANDING     COST YDAY    EARNED YDAY   │
│    53.9p        £2.62         £2.16      │
└──────────────────────────────────────────┘
```

**Needs:** rate sensors from your energy supplier integration. No bridge.

---

## Layout

Six figures in a 3×2 grid — the shape is the whole pattern:

```yaml
type: custom:button-card
show_name: false
show_icon: false
triggers_update:
  - sensor.octopus_import_rate
  - sensor.octopus_export_rate
  - sensor.octopus_next_rate
  - sensor.octopus_standing_charge
styles:
  card: [{ padding: 16px }, { border-radius: 16px }]
  grid: [{ 'grid-template-areas': '"c"' }, { 'grid-template-columns': 'minmax(0,1fr)' }]
  custom_fields: { c: [{ width: 100% }, { 'text-align': left }] }
custom_fields:
  c: |
    [[[
      const p  = id => { const e = states[id];
                         return e ? Number(e.state) : null; };
      const fmtP = v => v === null ? '—' : v.toFixed(1) + 'p';
      const fmtC = v => v === null ? '—' : '£' + v.toFixed(2);

      const cell = (label, value, colour) => `
        <div>
          <div style="font-size:11px;opacity:.55;letter-spacing:.5px">${label}</div>
          <div style="font-size:24px;font-weight:700;color:${colour}">${value}</div>
        </div>`;

      return `<div style="display:grid;grid-template-columns:repeat(3,1fr);
                   gap:18px 14px">
        ${cell('IMPORT NOW',  fmtP(p('sensor.octopus_import_rate')),   '#ff5656')}
        ${cell('EXPORT NOW',  fmtP(p('sensor.octopus_export_rate')),   '#4ade80')}
        ${cell('NEXT RATE',   fmtP(p('sensor.octopus_next_rate')),     '#4d98ff')}
        ${cell('STANDING',    fmtP(p('sensor.octopus_standing_charge')), '#e5e5e5')}
        ${cell('COST YDAY',   fmtC(p('sensor.cost_yesterday')),        '#ff5656')}
        ${cell('EARNED YDAY', fmtC(p('sensor.earned_yesterday')),      '#4ade80')}
      </div>`;
    ]]]
```

**Colour by direction, consistently.** Red for money going out, green for money coming
in, blue for informational. Once you're consistent across the dashboard, people read
it without reading it.

---

## Units are the fiddly part

Suppliers and integrations disagree about units. Some report **pence per kWh**, some
report **pounds** (`0.343` rather than `34.3`). Check in Developer Tools before
formatting, or you'll ship a tile that says `0.3p`.

```javascript
// normalise pounds to pence if the number looks too small
const raw = Number(states['sensor.octopus_import_rate'].state);
const pence = raw < 2 ? raw * 100 : raw;
```

Crude, but it survives an integration changing its mind — which does happen.

---

## Yesterday's totals

Most suppliers publish consumption a day in arrears, so "today so far" often doesn't
exist as a sensor at all. Two options:

**Use the supplier's previous-day sensors** where they exist — accurate, but always a
day behind.

**Capture your own at midnight** with a scheduled automation writing to
`input_number` helpers:

```yaml
- alias: Capture yesterday's figures
  trigger:
    - platform: time
      at: "23:59:50"
  action:
    - service: input_number.set_value
      target: { entity_id: input_number.yesterday_import }
      data: { value: "{{ states('sensor.import_today') | float(0) }}" }
```

The second approach is worth knowing about generally — it's the standard way to keep
"yesterday" values for anything that only publishes a running daily total. It's also
independent of any external service, which means it keeps working when the supplier's
API doesn't.

---

## Gotchas

**Agile-style tariffs change every 30 minutes.** `NEXT RATE` is genuinely useful there
and meaningless on a fixed tariff — worth hiding rather than showing a duplicate.

**Rate sensors go `unavailable` when the integration reauthenticates.** Guard every
lookup; an unguarded `Number(undefined)` renders as `NaN`, which looks broken in a way
a dash doesn't.

**Export rates are often a fixed number** rather than a live sensor. If your export
tariff is flat, hard-coding it and labelling it clearly is more honest than wiring up
a sensor that never changes.
