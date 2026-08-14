# 06 — System stats

CPU, memory and disks as labelled progress bars that change colour as they fill, with
uptime underneath.

Small tile, and the bar-drawing function in it gets reused by half the other patterns
in this folder.

---

## Needs

**Resources:** `custom:button-card`.

**Entities:** one sensor with attributes — see below. If you're using
[HA's own System Monitor](https://www.home-assistant.io/integrations/systemmonitor)
you'll have a separate entity per metric instead, which works fine; there's a note at
the end.

---

## One sensor, many attributes

The version here reads a single sensor pushed from the machine being monitored:

```json
{
  "state": "ok",
  "attributes": {
    "cpu": 25,
    "memory": 34,
    "disk_c": 17,
    "disk_d": 0,
    "uptime_minutes": 62
  }
}
```

**Why one sensor rather than five.** Three reasons, and the first is the one that
bites:

- **The 255-character limit applies to `state`, not to attributes.** Anything
  structured has to live in attributes anyway, so you may as well put the whole group
  there.
- **They arrive together.** CPU and memory sampled two seconds apart can disagree.
  One push, one timestamp, one consistent picture.
- **Entity count.** Five machines × five metrics is twenty-five entities in every
  picker you ever open. Five sensors with attributes is five.

Pushing it is a REST call — see
[02-windows-bridge.md](../../docs/02-windows-bridge.md) and
[`Publish-Sensors.ps1`](../../bridge/Publish-Sensors.ps1). The collector is about
fifteen lines of PowerShell.

---

## The tile

```yaml
type: custom:button-card
show_icon: false
show_name: false
entity: sensor.office_system
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
        "header"
        "line"
        "body"
    - grid-template-columns: 1fr
    - grid-template-rows: 34px 2px minmax(0,1fr)
    - row-gap: 8px
  custom_fields:
    header: [ display: grid, height: 100% ]
    line:   [ display: grid, height: 100% ]
    body:   [ display: grid, height: 100% ]

custom_fields:
  header: |
    [[[
      return '' +
        '<div style="display:flex;align-items:center;gap:9px;height:100%">' +
          '<ha-icon icon="mdi:chip" style="--mdc-icon-size:22px;' +
            'color:#4d98ff"></ha-icon>' +
          '<span style="font-size:17px;font-weight:700;color:#e6ebee">' +
            'System</span>' +
        '</div>';
    ]]]

  line: |
    [[[
      return '<div style="height:1px;background:rgba(255,255,255,0.10)"></div>';
    ]]]

  body: |
    [[[
      var a = (states['sensor.office_system'] || {}).attributes || {};

      var n = function(v) {
        var f = parseFloat(v);
        return isNaN(f) ? null : Math.max(0, Math.min(100, f));
      };

      // green until 60, amber to 85, red above
      var colour = function(pct) {
        if (pct === null) return '#586771';
        return pct >= 85 ? '#ff5f65' : pct >= 60 ? '#e3ad55' : '#62a77b';
      };

      // --- the reusable bit -------------------------------------
      var bar = function(pct) {
        var c = colour(pct);
        var w = pct === null ? 0 : pct;
        return '' +
          '<div style="height:6px;border-radius:3px;background:#20262a;' +
                      'overflow:hidden;margin-top:5px">' +
            '<div style="height:100%;width:' + w + '%;background:' + c + ';' +
                        'border-radius:3px"></div>' +
          '</div>';
      };

      var big = function(label, pct) {
        var c = colour(pct);
        return '' +
          '<div>' +
            '<div style="font-size:10px;letter-spacing:.12em;color:#7f929f;' +
                        'text-align:center">' + label + '</div>' +
            '<div style="font-size:30px;font-weight:700;color:' + c + ';' +
                        'text-align:center;line-height:1.05">' +
              (pct === null ? '—' : Math.round(pct) + '%') + '</div>' +
            bar(pct) +
          '</div>';
      };

      var small = function(label, pct) {
        var c = colour(pct);
        return '' +
          '<div>' +
            '<div style="display:flex;justify-content:space-between;' +
                        'align-items:baseline">' +
              '<span style="font-size:10px;letter-spacing:.08em;' +
                           'color:#9ba9b2">' + label + '</span>' +
              '<span style="font-size:12px;font-weight:700;color:' + c + '">' +
                (pct === null ? '—' : Math.round(pct) + '%') + '</span>' +
            '</div>' +
            bar(pct) +
          '</div>';
      };

      // --- uptime -----------------------------------------------
      var mins = parseInt(a.uptime_minutes, 10);
      var up = '—';
      if (!isNaN(mins)) {
        var d = Math.floor(mins / 1440);
        var h = Math.floor((mins % 1440) / 60);
        var m = mins % 60;
        up = d + 'd ' + h + 'h ' + m + 'm';
      }

      return '' +
        '<div style="display:flex;flex-direction:column;' +
                    'justify-content:space-between;height:100%">' +
          '<div style="display:grid;grid-template-columns:1fr 1fr;' +
                      'column-gap:18px">' +
            big('CPU',    n(a.cpu)) +
            big('MEMORY', n(a.memory)) +
          '</div>' +
          '<div style="display:grid;grid-template-columns:1fr 1fr;' +
                      'column-gap:18px;margin-top:10px">' +
            small('SYSTEM DRIVE C:', n(a.disk_c)) +
            small('DATA DRIVE D:',   n(a.disk_d)) +
          '</div>' +
          '<div style="text-align:center;font-size:10px;letter-spacing:.10em;' +
                      'color:#586771;margin-top:8px">UPTIME ' + up + '</div>' +
        '</div>';
    ]]]
```

---

## How it works

**The bar is two nested divs.** An outer track with `overflow:hidden` and a rounded
radius, an inner fill with a percentage width. No SVG, no library, no card
dependency. This function turns up again in
[05-printer-consumables](05-printer-consumables.md) and
[17-purifier-tile](17-purifier-tile.md) — it's worth lifting out.

**Clamp before you draw.** `Math.max(0, Math.min(100, v))` stops a bad reading
producing a fill wider than its track, which escapes the rounded corners and looks
broken. The outer `overflow:hidden` is the second line of defence.

**Colour thresholds are shared** between the number and the bar, so they always agree.
Green under 60, amber to 85, red above. Tune to taste — a NAS at 80% disk is normal,
a system drive at 80% is a Saturday afternoon.

**Two sizes, one function each.** Big for the things you glance at, small for the ones
you only care about when they're wrong. Same bar underneath both.

---

## Gotchas

**Attributes are strings surprisingly often.** Depending on how the sensor was
pushed, `a.cpu` may be `"25"` rather than `25`. Everything goes through `parseFloat`
above for that reason. `"25" >= 60` does work in JavaScript, but `"9" >= 60` is
`false` and `"9" > "60"` is `true`, so string comparison will eventually bite you.

**A missing sensor shouldn't blank the tile.** `(states[...] || {}).attributes || {}`
gives an empty object, every metric renders `—`, and the tile stays up. Reaching
straight into `.attributes` on an undefined entity throws, and in a single-card panel
view that takes the whole page with it.

**Uptime in minutes overflows fast.** A machine up 40 days is 57,600 minutes. Format
it, don't print it.

**Don't poll this hard.** A system sensor pushed every 5 seconds is 17,000 state
changes a day and a recorder database that grows noticeably. Thirty to sixty seconds
is plenty for something a human glances at, and you can exclude it from the recorder
entirely if you never want history.

---

## Adapting it

**Using HA's System Monitor integration** instead of a pushed sensor? Replace the
attribute reads with entity reads:

```js
var cpu = n((states['sensor.processor_use'] || {}).state);
var mem = n((states['sensor.memory_use_percent'] || {}).state);
```

Everything else is unchanged.

**More drives.** The small-row grid is `1fr 1fr`. Three drives, `repeat(3,1fr)`. Past
four, drop to a list.

**Temperatures.** Same `small()` helper with `°C` instead of `%` — but pass the bar a
normalised percentage rather than the raw temperature, or a 45°C CPU shows a 45% bar
and looks half-idle when it's fine.

**Multiple machines.** One tile per machine, one sensor per machine, same template.
Put the hostname in the header.

**Network throughput** is the same tile with different metrics —
[07-network-throughput](07-network-throughput.md).
