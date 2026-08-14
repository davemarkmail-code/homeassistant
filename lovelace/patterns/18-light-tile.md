# 18 — Light tile

A light with state, brightness, and a couple of quick actions.

```
┌──────────────────────────────────────┐
│ 💡 Desk Light                        │
│ ──────────────────────────────────── │
│  ⬤                                   │
│  ON                                  │
│  brightness 100%                     │
│ ┌──────────────┬──────────────┐      │
│ │      🎨      │      ⏻       │      │
│ └──────────────┴──────────────┘      │
└──────────────────────────────────────┘
```

**Needs:** a `light` entity. Nothing else.

The simplest pattern in the cookbook, and a good one to start with if you're new to
`button-card`.

---

## The whole thing

```yaml
type: custom:button-card
entity: light.desk_light
show_name: false
show_icon: false
triggers_update:
  - light.desk_light
styles:
  card:
    - padding: 14px
    - border-radius: 14px
    - background: rgba(255,255,255,0.05)
  grid:
    - grid-template-areas: '"status status" "colour power"'
    - grid-template-rows: 1fr 48px
    - grid-template-columns: repeat(2, minmax(0,1fr))
    - row-gap: 8px
    - column-gap: 8px
custom_fields:
  status: |
    [[[
      const l  = states['light.desk_light'];
      const on = l.state === 'on';
      const br = Math.round(((l.attributes.brightness || 0) / 255) * 100);
      const rgb = l.attributes.rgb_color;
      const dot = on
        ? (rgb ? `rgb(${rgb.join(',')})` : '#f2cc3d')
        : 'rgba(255,255,255,0.10)';

      return `<div style="display:flex;align-items:center;gap:14px;height:100%">
        <div style="width:42px;height:42px;border-radius:50%;background:${dot};
             box-shadow:${on ? '0 0 18px ' + dot : 'none'}"></div>
        <div>
          <div style="font-size:22px;font-weight:700">${on ? 'ON' : 'OFF'}</div>
          <div style="font-size:12px;opacity:.55">
            ${on ? 'brightness ' + br + '%' : 'tap to turn on'}</div>
        </div>
      </div>`;
    ]]]
```

### The dot does the work

Filling a circle with the light's **actual `rgb_color`** and adding a matching glow
means the tile shows you what colour the lamp is without any text. Costs two lines
and reads instantly from across a room.

Fall back to a warm yellow when `rgb_color` is absent — plenty of dimmable-only
lights don't report it.

---

## Brightness without a slider

Sliders are painful inside a button-card. Three practical options:

**Tap for `more-info`** — HA's own dialog has a proper brightness slider and colour
picker. One line, no maintenance, works everywhere.

```yaml
tap_action: { action: more-info }
```

**Stepped buttons** — good on a wall panel where precision doesn't matter:

```yaml
tap_action:
  action: call-service
  service: light.turn_on
  target: { entity_id: light.desk_light }
  data:
    brightness_step_pct: 20
```

`brightness_step_pct` is relative, so it needs no state tracking and can't drift.

**Preset scenes** — often what people actually want. Two or three buttons calling
`light.turn_on` with fixed `brightness_pct` and `color_temp_kelvin` values beats a
slider nobody adjusts precisely.

---

## Toggle vs explicit on/off

```yaml
service: light.toggle      # one button, fewer taps
service: light.turn_on     # explicit, safe for automations and voice
```

Use `toggle` for a hand-operated tile. Use explicit `turn_on`/`turn_off` anywhere a
command might arrive twice or where you can't see the current state.

---

## Gotchas

**Brightness is 0–255, not 0–100.** `brightness_pct` in service calls is a percentage,
but the `brightness` *attribute* you read back is 0–255. Mixing them up gives you a
tile reading "40%" when the light is at full.

**`brightness` is `null` when the light is off** — not `0`. `(null / 255) * 100` gives
`0`, which is fine, but `null.toFixed()` throws. Default it.

**Groups report the average.** A light group's brightness is the mean of its members,
so it can read 50% when one lamp is off and another is full.

**Colour attributes vary.** `rgb_color`, `hs_color`, `color_temp_kelvin`, `xy_color` —
which are present depends on the bulb. Check `supported_color_modes` rather than
assuming `rgb_color` exists.
