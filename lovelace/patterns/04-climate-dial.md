# 04 — Climate dial

Current temperature, target, and controls for heating or air conditioning.

```
┌────────────────────────────────────────────┐
│ 🌡  Office Air Conditioner                 │
│ ────────────────────────────────────────── │
│   ╭───╮   CONTROL TYPE   CURRENT   SET TO  │
│   │ ❄ │   IR CONTROL     24.9°C      20    │
│   ╰───╯   use on or off  tapo sensor target│
│ ┌──────┬────────┬────────┬──────┬────────┐ │
│ │ POWER│ TEMP - │ TEMP + │ MODE │  FAN   │ │
│ └──────┴────────┴────────┴──────┴────────┘ │
└────────────────────────────────────────────┘
```

**Needs:** a `climate` entity, or a `switch` plus a separate temperature sensor if
you're driving something over IR. No bridge.

---

## The grid

Status fills, buttons pin to the bottom:

```yaml
styles:
  grid:
    - grid-template-areas: >
        "status status status status status"
        "power tempdown tempup fanmode fan"
    - grid-template-columns: repeat(5, minmax(0,1fr))
    - grid-template-rows: 1fr 52px
    - column-gap: 8px
    - row-gap: 8px
```

`1fr` on the status row is what pins the buttons to the bottom regardless of tile
height — see [02-device-status](02-device-status.md), same principle.

---

## The status block

```yaml
custom_fields:
  status: |
    [[[
      const c   = states['climate.air_conditioner'];
      const on  = states['switch.air_conditioner'].state === 'on';
      const cur = states['sensor.room_temperature'].state;
      const tgt = c.attributes.temperature;

      const cell = (label, value, sub, colour) => `
        <div>
          <div style="font-size:11px;opacity:.5;letter-spacing:.5px">${label}</div>
          <div style="font-size:22px;font-weight:700;color:${colour}">${value}</div>
          <div style="font-size:10px;opacity:.45">${sub}</div>
        </div>`;

      return `<div style="height:100%;display:grid;
                   grid-template-columns:86px minmax(0,1fr) 118px 105px;
                   align-items:center;gap:12px;text-align:left">
        <div style="width:68px;height:68px;border-radius:50%;display:flex;
             align-items:center;justify-content:center;
             background:${on ? 'rgba(77,152,255,0.14)' : 'rgba(255,255,255,0.04)'}">
          <ha-icon icon="mdi:air-conditioner"
                   style="--mdc-icon-size:34px;color:${on ? '#4d98ff' : '#6b7280'}"></ha-icon>
        </div>
        ${cell('CONTROL TYPE', on ? 'ON' : 'OFF', 'use on or off', on ? '#4d98ff' : '#9ba9b2')}
        ${cell('CURRENT', cur + '°C', 'room sensor', '#e0a63c')}
        ${cell('SET TO', tgt, 'target', '#4d98ff')}
      </div>`;
    ]]]
```

### A spinning fan, cheaply

Both this and the purifier tile use the same trick — a CSS keyframe injected inline:

```javascript
return `<style>@keyframes dmFanSpin { to { transform: rotate(360deg) } }</style>
  <ha-icon icon="mdi:fan" style="
    --mdc-icon-size:34px;
    animation:${on ? 'dmFanSpin 1.9s linear infinite' : 'none'}"></ha-icon>`;
```

Cheap, no dependencies, and the animation stops when the device is off — which makes
the tile readable at a glance from across the room.

**Don't put this on a card that also uses `backdrop-filter`.** An animating ancestor
can break the blur. See [../../docs/05-gotchas.md](../../docs/05-gotchas.md).

---

## Buttons

```yaml
tempup:
  card:
    type: custom:button-card
    name: Temp +
    icon: mdi:plus
    tap_action:
      action: call-service
      service: climate.set_temperature
      target: { entity_id: climate.air_conditioner }
      data:
        temperature: >
          [[[ return Number(states['climate.air_conditioner'].attributes.temperature) + 1; ]]]
```

Reading the current target and adding one keeps a single source of truth. Don't track
the setpoint in a helper — it will drift out of sync with reality.

---

## IR-controlled units

If you're driving a unit over IR (Broadlink and similar), HA is **sending commands
blind**. There's no feedback, so:

- The `climate` entity reflects what HA *thinks*, not what the unit is doing
- If someone uses the physical remote, HA doesn't know
- Temperature must come from a **separate sensor** — never from the climate entity

That's why the example above reads `sensor.room_temperature` for CURRENT and the
climate entity only for the target. Label it clearly on the tile so the difference is
obvious to whoever's reading it.

For the same reason, a plain **on/off** control is more honest than a full mode
selector on IR kit — fewer states to get out of step.

---

## Gotchas

**`temperature` vs `current_temperature`.** The first is the target, the second is what
the device reports. On IR setups `current_temperature` is often missing or wrong.

**`hvac_action` is the useful one for showing activity** — `heating`, `cooling`,
`idle` — but plenty of integrations don't provide it. Check before relying on it;
falling back to comparing current against target is usually good enough.

**Guard against `unavailable`.** A climate entity that drops out gives you
`attributes.temperature === undefined`, which renders as literal "undefined" in the
tile.

**Round your display.** `24.900000000000002` happens more often than you'd think.
