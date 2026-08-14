# 17 — Purifier tile

Fan speed, air quality and filter life for an air purifier — and a good example of
mixing a live reading with a consumable.

```
┌──────────────────────────────────────────────┐
│ 🌀 Blueair Purifier                          │
│ ──────────────────────────────────────────── │
│  ╭────╮   MANUAL      AIR QUALITY  FILTER   │
│  │ ✿  │   fan 50%     EXCELLENT     79%     │
│  ╰────╯               PM2.5 0     ▓▓▓▓▓░░   │
│ ┌────────────────────┬────────────────────┐  │
│ │    FAN CONTROL     │       POWER        │  │
│ └────────────────────┴────────────────────┘  │
└──────────────────────────────────────────────┘
```

**Needs:** a `fan` entity, plus PM2.5 and filter-life sensors if your model exposes
them. No bridge.

---

## The grid

```yaml
styles:
  grid:
    - grid-template-areas: >
        "status status"
        "control power"
    - grid-template-columns: repeat(2, minmax(0,1fr))
    - grid-template-rows: 1fr 48px
    - column-gap: 8px
    - row-gap: 8px
```

---

## Status block

Four columns: spinning icon, mode, air quality, filter life.

```yaml
custom_fields:
  status: |
    [[[
      const f    = states['fan.purifier'];
      const on   = f.state === 'on';
      const pct  = f.attributes.percentage || 0;
      const mode = f.attributes.preset_mode || (on ? 'MANUAL' : 'OFF');
      const pm   = Number(states['sensor.purifier_pm25'].state);
      const filt = Number(states['sensor.purifier_filter_life'].state);

      const aq = pm <= 12 ? ['EXCELLENT','#4ade80']
               : pm <= 35 ? ['GOOD','#e0a63c']
               : ['POOR','#ff5656'];

      const filtCol = filt > 40 ? '#4ade80' : filt > 15 ? '#e0a63c' : '#ff5656';

      return `<style>@keyframes dmFanSpin { to { transform: rotate(360deg) } }</style>
        <div style="height:100%;display:grid;
             grid-template-columns:100px 1fr 1fr 1fr;
             align-items:center;text-align:left;gap:12px">

          <div style="width:78px;height:78px;display:flex;
               align-items:center;justify-content:center">
            <ha-icon icon="mdi:flower-tulip" style="--mdc-icon-size:52px;
              color:${on ? '#4d98ff' : '#4b5563'};
              animation:${on ? 'dmFanSpin 1.9s linear infinite' : 'none'}"></ha-icon>
          </div>

          <div>
            <div style="font-size:24px;font-weight:700">${mode.toUpperCase()}</div>
            <div style="font-size:11px;opacity:.5">fan ${pct}%</div>
          </div>

          <div>
            <div style="font-size:11px;opacity:.5">AIR QUALITY</div>
            <div style="font-size:20px;font-weight:700;color:${aq[1]}">${aq[0]}</div>
            <div style="font-size:10px;opacity:.45">PM2.5 ${pm}</div>
          </div>

          <div>
            <div style="font-size:11px;opacity:.5">FILTER LIFE</div>
            <div style="font-size:20px;font-weight:700;color:${filtCol}">${filt}%</div>
            <div style="height:5px;background:rgba(255,255,255,0.08);
                 border-radius:3px;margin-top:5px">
              <div style="width:${Math.max(0,Math.min(100,filt))}%;height:100%;
                   background:${filtCol};border-radius:3px"></div>
            </div>
          </div>
        </div>`;
    ]]]
```

**The spin rate tells you nothing useful — and that's fine.** It's a binary "this is
running" signal, readable from across a room without reading any text. Don't try to
map speed to the fan percentage; it looks buggy rather than informative.

---

## Air quality thresholds

The PM2.5 bands above follow WHO guidance loosely — 12 and 35 µg/m³ are common
breakpoints. Different purifiers report different scales, so check what yours actually
outputs before trusting the colours. Some report an index rather than µg/m³, in which
case the numbers mean something completely different.

---

## Filter life is a consumable, not a reading

Worth treating differently from air quality:

- It only ever goes **down**, slowly, over months
- It's the one number here worth an **alert** rather than a glance

```yaml
- alias: Purifier filter low
  trigger:
    - platform: numeric_state
      entity_id: sensor.purifier_filter_life
      below: 15
  action:
    - service: notify.mobile_app_phone
      data: { message: "Purifier filter at {{ states('sensor.purifier_filter_life') }}%" }
```

A dashboard tile is the wrong place to *learn* your filter needs changing — you'll
walk past it for weeks. The tile is for confirming; the notification is for knowing.

---

## Controls

```yaml
power:
  card:
    type: custom:button-card
    name: Power
    icon: mdi:power
    tap_action:
      action: call-service
      service: fan.toggle
      target: { entity_id: fan.purifier }

control:
  card:
    type: custom:button-card
    name: Fan control
    icon: mdi:fan
    tap_action:
      action: more-info
      entity: fan.purifier
```

`more-info` for speed is the pragmatic choice — HA's built-in dialog gives you a
percentage slider and preset modes for free, and a slider is fiddly to build inside a
button-card.

---

## Gotchas

**Some purifiers expose a status LED as a `light` entity.** It looks like an accent
light and isn't — toggling it turns off the indicator, not the device. Check what a
`light.*` entity on a purifier actually controls before wiring it to anything.

**`percentage` and `preset_mode` can both be set** and they interact. Setting a
percentage often knocks the device out of Auto, which surprises people who expected
Auto to stick.

**Auto mode reports a changing percentage** you didn't set. That's correct behaviour,
but if you cache or display it as "your setting" it'll look like it's ignoring you.
