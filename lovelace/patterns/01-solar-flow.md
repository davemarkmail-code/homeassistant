# 01 — Solar flow diagram

The tile that gets asked about more than everything else here combined.

Five circular nodes — solar, house, battery, grid, EV — connected by lines that light
up only when power is actually moving along them, with a dot travelling in the
direction of flow. Today's generation and export totals sit in the corners. Tap the
mode chips to switch the inverter between import, export and eco.

It looks complicated. It's one button-card and one JavaScript template.

---

## Needs

**Resources:** `custom:button-card` only. No plotly, no apexcharts, no card-mod.
The whole thing is inline SVG and HTML built by a template.

**Entities** — these are the generic shapes. Names will differ per integration:

| Purpose | Example entity |
|---|---|
| Solar generation, W | `sensor.solar_pv_power` |
| House consumption, W | `sensor.solar_load_power` |
| Battery charge rate, W | `sensor.solar_charge_power` |
| Battery discharge rate, W | `sensor.solar_discharge_power` |
| Battery state of charge, % | `sensor.solar_soc` |
| Grid import, W | `sensor.solar_import_power` |
| Grid export, W | `sensor.solar_export_power` |
| Today's generation, kWh | `sensor.solar_pv_energy_today_kwh` |
| Today's export, kWh | `sensor.solar_export_energy_today_kwh` |
| EV charger draw, W *(optional)* | `sensor.evse_active_power` |

**If you're on GivEnergy/GivTCP, check for these first:**

```
sensor.<serial>_solar_to_house      sensor.<serial>_solar_to_battery
sensor.<serial>_solar_to_grid       sensor.<serial>_battery_to_house
sensor.<serial>_grid_to_house       sensor.<serial>_grid_to_battery
```

GivTCP computes the actual routing for you. If you have them, use them — you get the
split between "solar going to the house" and "solar going to the battery" directly,
instead of inferring it from four power figures and getting it subtly wrong at the
edges. The template below works either way; there's a note at each decision point.

---

## The tile

```yaml
type: custom:button-card
show_icon: false
show_name: false
show_label: false
tap_action:
  action: navigate
  navigation_path: /your-dashboard/solar-detail

# Repaint every 10s even if nothing changed, and immediately when
# any of these move. Without triggers_update the template only
# re-evaluates on its own timer and the tile feels laggy.
update_timer: 10s
triggers_update:
  - sensor.solar_pv_power
  - sensor.solar_load_power
  - sensor.solar_soc
  - sensor.solar_charge_power
  - sensor.solar_discharge_power
  - sensor.solar_import_power
  - sensor.solar_export_power

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
    - grid-template-areas: '"content"'
    - grid-template-columns: 1fr
    - grid-template-rows: minmax(0,1fr)
  custom_fields:
    content:
      # display:grid, NOT block. See Gotchas.
      - display: grid
      - height: 100%

custom_fields:
  content: |
    [[[
      // ---- read ----------------------------------------------------
      var num = function(id) {
        var s = states[id];
        if (!s) return 0;
        var v = parseFloat(s.state);
        return isNaN(v) ? 0 : v;
      };

      var pv     = num('sensor.solar_pv_power');
      var load   = num('sensor.solar_load_power');
      var soc    = num('sensor.solar_soc');
      var chg    = num('sensor.solar_charge_power');
      var dis    = num('sensor.solar_discharge_power');
      var imp    = num('sensor.solar_import_power');
      var exp    = num('sensor.solar_export_power');
      var ev     = num('sensor.evse_active_power');

      // At 100% SOC the inverter trickles a few watts to hold the pack.
      // Showing that as "charging" makes the tile look wrong all evening.
      if (soc >= 100) chg = 0;

      // ---- deadbands -----------------------------------------------
      // Below these, treat as zero. Without this the lines flicker
      // constantly as readings wobble around 0.
      var DEAD_BATT = 300;   // battery is noisy
      var DEAD_GRID = 20;    // grid is not

      var batt = chg > DEAD_BATT ? chg : (dis > DEAD_BATT ? -dis : 0);
      var grid = exp > DEAD_GRID ? -exp : (imp > DEAD_GRID ? imp : 0);

      // ---- which lines are live ------------------------------------
      // If you have GivTCP's *_to_* sensors, replace each of these
      // with num('sensor.<serial>_solar_to_house') > 20 and so on.
      var liveSolarHouse = pv > 20 && load > 20;
      var liveSolarBatt  = pv > 20 && batt > 0;
      var liveSolarGrid  = pv > 20 && grid < 0;
      var liveBattHouse  = batt < 0;
      var liveGridHouse  = grid > 0;
      var liveEv         = ev > 100;

      // ---- format --------------------------------------------------
      var w = function(v) {
        var a = Math.abs(v);
        if (a >= 1000) return (a/1000).toFixed(1) + ' kW';
        return Math.round(a) + ' W';
      };

      var C = {
        solar: '#ffd84d',
        house: '#3f9cff',
        batt:  '#58d68d',
        grid:  '#ff5656',
        ev:    '#ff9f43',
        idle:  '#354047'
      };

      // ---- one node ------------------------------------------------
      // x/y are percentages so the whole thing scales with the tile.
      var node = function(x, y, colour, icon, label, value, active) {
        var glow = active
          ? 'box-shadow:0 0 18px ' + colour + '55;border-color:' + colour
          : 'box-shadow:none;border-color:' + C.idle;
        return '' +
          '<div style="position:absolute;left:' + x + '%;top:' + y + '%;' +
                      'transform:translate(-50%,-50%);width:104px;height:104px;' +
                      'border-radius:50%;border:2px solid;' + glow + ';' +
                      'background:#0d1116;display:flex;flex-direction:column;' +
                      'align-items:center;justify-content:center;">' +
            '<ha-icon icon="' + icon + '" style="--mdc-icon-size:26px;color:' +
              (active ? colour : C.idle) + '"></ha-icon>' +
            '<div style="font-size:9px;letter-spacing:.10em;color:#8d9499;' +
                        'margin-top:2px">' + label + '</div>' +
            '<div style="font-size:15px;font-weight:700;color:' +
              (active ? colour : C.idle) + '">' + value + '</div>' +
          '</div>';
      };

      // ---- one connecting line -------------------------------------
      // A dashed path plus a dot that rides it via animateMotion.
      // dur is tied to magnitude so heavy flow visibly moves faster.
      var link = function(id, d, colour, active, watts) {
        if (!active) {
          return '<path d="' + d + '" stroke="' + C.idle +
                 '" stroke-width="2" fill="none" opacity="0.5"/>';
        }
        var dur = Math.max(1.1, 3.4 - (Math.abs(watts) / 1800));
        return '' +
          '<path id="' + id + '" d="' + d + '" stroke="' + colour +
            '" stroke-width="2.5" fill="none" stroke-dasharray="7 7" opacity="0.85"/>' +
          '<circle r="4.5" fill="' + colour + '">' +
            '<animateMotion dur="' + dur + 's" repeatCount="indefinite">' +
              '<mpath href="#' + id + '"/>' +
            '</animateMotion>' +
          '</circle>';
      };

      // ---- layout --------------------------------------------------
      // SVG draws the lines underneath; HTML nodes sit on top.
      // Coordinates are in the 500x470 viewBox, nodes in percentages.
      var svg = '' +
        '<svg viewBox="0 0 500 470" preserveAspectRatio="none" ' +
             'style="position:absolute;inset:0;width:100%;height:100%">' +
          link('l_sh', 'M250,110 L250,235', C.solar, liveSolarHouse, pv) +
          link('l_sb', 'M250,110 C160,150 110,200 110,300', C.solar, liveSolarBatt, chg) +
          link('l_sg', 'M250,110 C340,150 390,200 390,300', C.solar, liveSolarGrid, exp) +
          link('l_bh', 'M110,300 C150,290 200,270 235,250', C.batt, liveBattHouse, dis) +
          link('l_gh', 'M390,300 C350,290 300,270 265,250', C.grid, liveGridHouse, imp) +
        '</svg>';

      var nodes = '' +
        node(50, 23, C.solar, 'mdi:solar-power-variant', 'SOLAR', w(pv), pv > 20) +
        node(50, 53, C.house, 'mdi:home-lightning-bolt', 'HOME',  w(load), load > 20) +
        node(22, 64, C.batt,  'mdi:battery-high',        'BATTERY',
             batt === 0 ? 'IDLE' : w(batt), batt !== 0) +
        node(78, 64, C.grid,  'mdi:transmission-tower',  'GRID',
             grid === 0 ? 'IDLE' : w(grid), grid !== 0) +
        node(50, 80, C.ev,    'mdi:car-electric',        'CAR',
             liveEv ? w(ev) : 'IDLE', liveEv);

      // ---- corner totals -------------------------------------------
      var gen = states['sensor.solar_pv_energy_today_kwh'];
      var expT = states['sensor.solar_export_energy_today_kwh'];
      var corners = '' +
        '<div style="position:absolute;right:6px;top:2px;text-align:right">' +
          '<div style="font-size:17px;font-weight:700;color:' + C.solar + '">TODAY ' +
            (gen ? parseFloat(gen.state).toFixed(1) : '-') + ' kWh</div>' +
        '</div>' +
        '<div style="position:absolute;right:6px;bottom:2px;text-align:right">' +
          '<div style="font-size:9px;letter-spacing:.12em;color:#8d9499">EXPORTED</div>' +
          '<div style="font-size:17px;font-weight:700;color:' + C.grid + '">TODAY ' +
            (expT ? parseFloat(expT.state).toFixed(1) : '-') + ' kWh</div>' +
        '</div>' +
        '<div style="position:absolute;left:6px;bottom:4px;font-size:17px;' +
                    'font-weight:700;color:' + C.batt + '">' +
          Math.round(soc) + '%</div>';

      return '<div style="position:relative;width:100%;height:100%">' +
             svg + nodes + corners + '</div>';
    ]]]
```

---

## How it works

**Two layers.** An absolutely-positioned SVG draws the connecting lines and fills the
tile; HTML `<div>` nodes sit on top of it. Doing the nodes in HTML rather than SVG
means you get `ha-icon`, flexbox centring and normal text rendering for free. The SVG
is only ever lines and a travelling dot.

**`preserveAspectRatio="none"`** lets the SVG stretch to whatever shape the tile ends
up. The line coordinates are in a fixed 500×470 space and get squashed to fit, which
is fine for curves but would distort text — another reason the text is HTML.

**The dot is `animateMotion` + `mpath`.** Give the path an `id`, reference it from
`<mpath href="#id">`, and a circle rides it. This is native SVG animation, no
JavaScript loop, no CSS keyframes, and it costs nothing to run.

**Speed encodes magnitude.** `dur` shortens as watts rise, so a 4 kW export visibly
races and a 200 W trickle ambles. It's the cheapest way to add information to a
diagram that's already using colour for identity.

**`update_timer` and `triggers_update` do different jobs.** The timer forces a repaint
on a schedule; the trigger list repaints immediately when a listed entity changes.
You want both — the triggers for responsiveness, the timer to catch anything that
changed without firing a state event.

---

## Gotchas

**Deadbands aren't optional.** Battery and grid readings wobble around zero
constantly. Without a threshold the lines flicker on and off every few seconds and
the tile looks broken. 300 W for battery and 20 W for grid works on a domestic
install; tune to your own noise floor. The battery needs a much wider band than the
grid because inverters idle-cycle.

**Handle 100% SOC explicitly.** A full battery still draws a trickle to hold charge.
Reported as-is, your tile shows "charging" all evening on a full pack. Force it to
zero at 100%.

**One template error blanks the entire view.** If this tile is the only card in a
panel view — which is the model this repo uses — a thrown exception inside `[[[ ]]]`
takes the whole page with it, and you get a blank screen with nothing in the console.
Test the logic before you paste it in:

```js
// paste the template body into the browser console, wrapped:
new Function('states', `<template body here>`)(
  document.querySelector('home-assistant').hass.states
);
```

If that returns a string, the template is safe to save.

**Wrapper must be `display:grid`.** In `styles.custom_fields.content`, `display:block`
will not let the content stretch even with `height:100%` set, because the percentage
resolves against a content-sized host. You get a tile with a mysterious gap at the
bottom. This is the most expensive single trap in this repo —
[05-gotchas.md](../../docs/05-gotchas.md).

**Forced export saturates the inverter.** During a forced export with strong sun, the
AC side hits its limit and battery export only ramps as solar falls. The diagram is
telling the truth; it just looks like the battery is ignoring you. Not a tile bug.

---

## Adapting it

**Fewer nodes.** No EV or no battery — delete the `node(...)` line and the `link(...)`
lines that touch it. Nothing else references them.

**Different geometry.** The node positions are percentages and the paths are viewBox
coordinates, so they're independent. Move a node by changing its `x, y`, then redraw
the curve to match. Keep the `M` start point on one node centre and the end on
another.

**No EV charger sensor?** Some chargers only expose a session energy total. Compare
consecutive readings in a template sensor to get instantaneous power, or drop the node.

**Tap to drill down.** The `tap_action` at the top opens a detail view. See
[13-drilldown-views](13-drilldown-views.md) for the back-navigation pattern.

**Mode chips.** The import/export/eco chips in the original are separate
`custom_fields` calling `select.select_option` on the inverter's mode entity. They're
not shown here because the option strings are vendor-specific — check yours with
Developer Tools → States before wiring buttons to them, and be aware some inverters
expose paused variants that look like duplicates.
