# 02 — Device status tile

The workhorse. A titled panel showing what a device is doing right now, a row of
supporting numbers, and the two or three buttons you actually press.

Used here for a robot vacuum, a robot mower and a printer. Once you have the shape,
most devices fit it — the only thing that changes is which sensors go in the middle
row.

---

## Needs

**Resources:** `custom:button-card`.

**Entities:** whatever your device exposes. The examples use a vacuum, but the layout
assumes nothing:

| Slot | Example |
|---|---|
| Primary state | `sensor.vacuum_status` |
| Secondary context | `sensor.vacuum_current_room` |
| Metric 1–4 | battery %, water tank, dust bag, filter |
| Actions | `vacuum.start`, `vacuum.pause`, `vacuum.return_to_base` |

---

## The layout

Four rows, named:

```
"header  header  header"     <- icon, title, live sub-state
"line    line    line"       <- 1px rule
"status  status  status"     <- big state text + metrics row
"normal  deep    rooms"      <- action buttons
```

```yaml
type: custom:button-card
show_icon: false
show_name: false
styles:
  card:
    - height: 100%
    - padding: 14px 16px
    - border-radius: 14px
    - background: rgba(255,255,255,0.045)
    - border: 1px solid rgba(255,255,255,0.13)
    - box-shadow: none
    - box-sizing: border-box
  grid:
    - grid-template-areas: >-
        "header header header"
        "line   line   line"
        "status status status"
        "normal deep   rooms"
    - grid-template-columns: 1fr 1fr 1fr
    - grid-template-rows: 42px 2px minmax(0,1fr) 56px
    - column-gap: 10px
    - row-gap: 8px
  custom_fields:
    header: [ display: grid, height: 100% ]
    line:   [ display: grid, height: 100% ]
    status: [ display: grid, height: 100% ]
    normal: [ display: grid, height: 100% ]
    deep:   [ display: grid, height: 100% ]
    rooms:  [ display: grid, height: 100% ]
```

**Every wrapper is `display: grid`.** Not block. A nested button-card inside a block
wrapper will not stretch, even with `height: 100%` set on both, because the percentage
resolves against a content-sized host. You get a button that's the height of its label
sitting in a taller cell, and a gap you'll spend an afternoon on.

**`minmax(0,1fr)` on the status row** lets it absorb whatever's left and, crucially,
lets it *shrink*. A bare `1fr` refuses to go below its content's intrinsic height, so
a long status string pushes the tile taller than its grid cell.

---

## Header

```yaml
header: |
  [[[
    var st  = states['sensor.vacuum_status'];
    var err = states['sensor.vacuum_error'];
    var bad = err && err.state !== 'none' && err.state !== 'unknown';

    var accent = bad ? '#ff6b6b' : '#4d98ff';
    var sub    = bad ? err.state.toUpperCase()
                     : (st ? st.state.toUpperCase() : 'UNKNOWN');

    return '' +
      '<div style="display:flex;align-items:center;gap:10px;height:100%">' +
        '<ha-icon icon="mdi:robot-vacuum" ' +
          'style="--mdc-icon-size:26px;color:' + accent + '"></ha-icon>' +
        '<span style="font-size:19px;font-weight:700;color:' + accent + '">' +
          'Vacuum</span>' +
        '<span style="font-size:19px;font-weight:700;color:#7f929f">·</span>' +
        '<span style="font-size:19px;font-weight:700;color:' + accent + '">' +
          sub + '</span>' +
      '</div>';
  ]]]

line: |
  [[[
    return '<div style="height:1px;width:100%;background:' +
           'rgba(255,255,255,0.10)"></div>';
  ]]]
```

**The title carries the alert.** No separate error badge — when something's wrong the
whole header goes red and the sub-state becomes the error. One place to look.

---

## Status and metrics

```yaml
status: |
  [[[
    var num = function(id) {
      var s = states[id];
      if (!s) return null;
      var v = parseFloat(s.state);
      return isNaN(v) ? null : v;
    };
    var txt = function(id, fallback) {
      var s = states[id];
      return s ? s.state : (fallback || '—');
    };

    // --- big state ---------------------------------------------
    var state = txt('sensor.vacuum_status').toUpperCase();
    var room  = txt('sensor.vacuum_current_room', '');

    var colour = state === 'CLEANING' ? '#69a57e'
               : state === 'CHARGING' ? '#8bc5df'
               : state === 'ERROR'    ? '#ff5f65'
                                      : '#c6d2d8';

    var big = '' +
      '<div>' +
        '<div style="font-size:10px;letter-spacing:.14em;color:#7f929f">' +
          'CURRENT STATUS</div>' +
        '<div style="font-size:26px;font-weight:700;color:' + colour + '">' +
          state + '</div>' +
        (room ? '<div style="font-size:10px;letter-spacing:.10em;' +
                'color:#7f929f;margin-top:2px">ROOM ' +
                room.toUpperCase() + '</div>' : '') +
      '</div>';

    // --- metric row --------------------------------------------
    var metric = function(label, value, colour) {
      return '' +
        '<div style="text-align:center">' +
          '<div style="font-size:10px;letter-spacing:.10em;color:#7f929f">' +
            label + '</div>' +
          '<div style="font-size:19px;font-weight:700;color:' + colour + '">' +
            value + '</div>' +
        '</div>';
    };

    var batt = num('sensor.vacuum_battery');
    var battCol = batt === null ? '#9aa9b2'
                : batt < 20     ? '#ff5f65'
                : batt < 50     ? '#e0b341'
                                : '#69a57e';

    var ok = function(id, invert) {
      var s = states[id];
      if (!s) return { t: '—', c: '#9aa9b2' };
      var on = s.state === 'on';
      var good = invert ? !on : on;
      return { t: good ? 'OK' : 'CHECK', c: good ? '#69a57e' : '#ff5f65' };
    };

    var water = ok('binary_sensor.vacuum_water_shortage', true);

    var row = '' +
      '<div style="display:grid;grid-template-columns:repeat(4,1fr);' +
                  'align-items:end;margin-top:10px">' +
        metric('BATTERY', batt === null ? '—' : batt + '%', battCol) +
        metric('WATER',   water.t, water.c) +
        metric('DUST BAG', ok('binary_sensor.vacuum_dust_bag_full', true).t,
                           ok('binary_sensor.vacuum_dust_bag_full', true).c) +
        metric('FILTER',  (num('sensor.vacuum_filter_left') || 0) + 'h', '#c6d2d8') +
      '</div>';

    return '<div style="display:flex;flex-direction:column;' +
                       'justify-content:space-between;height:100%">' +
           big + row + '</div>';
  ]]]
```

**Thresholds, not raw numbers.** `OK` / `CHECK` is more useful at a glance than a
percentage you have to interpret. Keep numbers where the value itself matters
(battery), use words where only the verdict does (water, bag).

**Watch the sensor polarity.** `binary_sensor.*_water_shortage` being `on` is *bad*.
The `invert` flag above exists because half of these read one way and half the other,
and getting it backwards produces a tile that's confidently wrong.

---

## Action buttons

```yaml
normal:
  card:
    type: custom:button-card
    show_icon: false
    show_name: true
    name: NORMAL
    tap_action:
      action: call-service
      service: vacuum.start
      service_data:
        entity_id: vacuum.your_vacuum
    styles:
      card:
        - height: 100%
        - border-radius: 10px
        - background: rgba(255,255,255,0.05)
        - border: 1px solid rgba(255,255,255,0.10)
        - box-shadow: none
      name:
        - font-size: 13px
        - font-weight: 600
        - letter-spacing: 0.08em
        - color: '#c6d2d8'
```

Repeat for the other two. Three is about the limit — past that the row gets cramped
and you're better off with a drill-down page
([13-drilldown-views](13-drilldown-views.md)).

---

## Gotchas

**`unavailable` is a state, and it's a string.** `parseFloat('unavailable')` is `NaN`,
which renders as `NaN%` in 19px type. Every read above goes through a helper that
returns `null` and a fallback. Do this from the start — devices go offline.

**Vendor status strings aren't stable.** A vacuum might report `cleaning`, `Cleaning`
or `zoned cleaning` depending on integration version and what it's doing. Upper-case
before comparing, and treat your colour map as a best-effort with a sensible default,
not an exhaustive switch.

**Buttons don't confirm.** Fine for "start cleaning". Not fine for anything with
consequences — put those behind [12-confirm-dialog](12-confirm-dialog.md).

**Check what the integration actually exposes before designing the metric row.** It's
easy to design four slots and then find two of your sensors don't exist — a mower that
reports blade hours in one integration version and not the next, for instance. Build
from the entity list, not from what the device *should* report.

---

## Adapting it

**Printer.** Same shape: header is model and online state, status is the big
`READY` / `OFFLINE`, metrics become ink levels. See
[05-printer-consumables](05-printer-consumables.md) for level bars.

**Anything with a battery and a dock** — mower, vacuum, handheld — is a drop-in. Swap
entities and icons.

**No actions?** Delete the fourth row from `grid-template-areas` and the row height
from `grid-template-rows`. Nothing else references them.

**Wider tiles** get more metric columns: change `repeat(4,1fr)` in the metric row.
Six starts to look like a spreadsheet.

**Make the whole tile tappable** to a detail page by putting `tap_action` on the
outer card. Inner buttons still take precedence for their own areas, so you get
"tap a button to act, tap anywhere else to drill down" for free.
