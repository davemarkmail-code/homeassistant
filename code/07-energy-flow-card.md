# The energy flow card

A single card showing solar, house, battery, grid and car as circles on a ring,
with animated flow lines between them. It is the piece of the dashboard I look
at most, and the piece that has caused me the most trouble — because it does
arithmetic on sensor readings, and sensors lie.

![The energy flow card](../images/energy-flow-card.png)

---

## The structure

One `custom:button-card` with a `content` custom_field containing a template
that builds the whole thing as HTML. Roughly 8KB of template.

That sounds mad, and there is a real cost: **one JavaScript error blanks the
entire card**. But it buys complete control over layout, animation and colour
logic in a way that composing existing cards does not.

A helper function does the repetitive work:

```javascript
// node(x, y, icon, label, value, colour, badge, width, fontSize)
node(18, 70, battIcon, 'BATTERY', battValue, C.battery, battBolt, 120, 14)
```

Each node is an absolutely positioned circle. Flow lines are dashed borders with
a CSS animation, shown only when power is actually moving in that direction.

**Always pre-flight the template before saving.** See
`05-styling-inside-shadow-dom.md` for how, and why it matters here more than
anywhere else.

---

## Colour thresholds and deadbands

Raw sensor values make a twitchy display. Three things fixed that:

**Thresholds, not gradients.** Battery charge is green above 80%, amber
20–80%, red below 20%. Easier to read at a glance than a continuous scale.

**A deadband on discharge.** A battery reports 15–20W of self-consumption
constantly. Without a floor, the discharge animation runs all night:

```javascript
var minDischarge = forcedExport ? 20 : 300;   // watts
var discharging  = dischargePower > minDischarge;
```

The deadband **relaxes to 20W during a forced export**, because then you *want*
to see genuine flow even when it's small.

**Hide the trickle at 100%.** A full battery accepting 60W from surplus solar is
noise, not information:

```javascript
var chargePower = (soc < 100) ? rawChargePower : 0;
```

At 100% the battery node reads `IDLE` and no flow line is drawn to it.

---

## The trap: subtracting the car from the house

This is the bit worth reading even if you don't build a flow card.

The card computed the house figure like this:

```javascript
home = Math.max(0, load - ev);
```

The reasoning: the inverter's `load_power` includes everything in the house
*including* the EV charger, so to show the house alone you subtract the car.

Sound logic. It failed three separate times.

### Failure 1 — a phantom charging session

The charger's local API stalled and froze reporting **7200W** into a car that
was already full — 31.4A, a nine-hour session, its internal clock seven hours
adrift. The app agreed with it, so the unit itself had lost the plot.

Result: `max(0, 477 − 7200)` = **0**. The house appeared to be using nothing.

### Failure 2 — two pollers on one inverter

Mid-migration I briefly had two Home Assistant instances both running the
inverter add-on. Modbus doesn't tolerate that — values land in the wrong
registers. The car showed 1.3kW, the grid direction inverted, and the house
zeroed again.

**If you ever run a test instance, disable the inverter poller on one of them.**

### Failure 3 — the real one, and my own fault

Long after the charger was healthy, the house still read zero whenever the car
was plugged in.

The cause: my charger reports `charging_state: "Charging"` **whenever a cable is
attached**, whether or not any current is flowing. The card trusted that word,
inferred a session was running, and derived a plausible-looking figure — which
it then subtracted from the house.

It only went wrong during my off-peak battery charging window, because that's
when there was a big grid import for it to misattribute. I was asleep for it
every night for weeks.

### The fix

Trust **measured power**, never a state word:

```javascript
var carPower = (parseFloat(
      (states['sensor.ev_charger_power'] || {}).state) || 0);

home = Math.max(0, load - carPower);

// and for the car node itself:
carValue = (carPower > 20) ? fmt(carPower) : 'IDLE';
```

Zero watts means nothing is charging. No inference, no estimation, nothing to go
stale.

### A sanity check worth having

If your own card must estimate anything, check it against physics before
believing it. Available power is bounded:

```javascript
var available = pv + Math.max(0, -gridPower) + batteryDischarge;
var plausible = (carPower <= available + 300);   // 300W tolerance
```

A car claiming 7.2kW while the house has 800W available is not charging.

---

## The plug / bolt / cross badge

Same lesson, applied to the icon. The car node carries a small badge that is
driven by **power and connection**, not by any state word:

```javascript
var pwr = (parseFloat((states['sensor.ev_charger_power']||{}).state) || 0);
var conn = (function () {
  var s = String((states['sensor.ev_charger_connection']||{}).state||'')
            .toLowerCase();
  // careful: "disconnected" contains "connected"
  return s.indexOf('disconnect') < 0 &&
        (s.indexOf('connect') >= 0 || s.indexOf('charg') >= 0);
})();

var badge =
    pwr > 20  ? boltHtml    // drawing power  → pulsing lightning bolt
  : conn      ? plugHtml    // plugged in     → static plug
              : crossHtml;  // not plugged in → red cross
```

Three states, each unambiguous, none of them inferred.

---

## Keyframes must travel with the element

An animation defined inside one conditional string is only present when that
string is. My charging pulse lived inside the car badge, so when I later
referenced the same animation from the battery node it silently did nothing
whenever the car wasn't charging.

Either define keyframes in a `<style>` block that is **always** emitted, or give
each element its own. The former is tidier:

```javascript
var styleBlock = '<style>'
  + '@keyframes battPulse{0%,100%{opacity:1}50%{opacity:.55}}'
  + '@keyframes chargeBolt{0%,100%{opacity:1}50%{opacity:.5}}'
  + '</style>';
// include styleBlock in the output unconditionally
```

---

## A mode picker, safely

Three segmented buttons — IMPORT / EXPORT / ECO — replacing an older status
badge. Scripts are in `02-battery-and-audio-scripts.yaml`. The dashboard side:

- **Green means live**, red means not live. Only one can be green.
- **IMPORT and EXPORT have confirmations.** They cost money if mistaken.
- **ECO has no confirmation.** It is the bail-out; it must always be one tap.
- Active state is read from the inverter's own force-charge and force-export
  selects, never from a helper you set yourself — same principle as the activity
  tiles.

---

## Read your own sensors before copying anything

Every inverter integration names things differently, and sign conventions vary —
mine reports grid import as negative. Before building, dump what you actually
have in Developer Tools → Template:

```jinja
{{ states.sensor | selectattr('entity_id','search','inverter')
                 | map(attribute='entity_id') | list }}
```

Then verify the energy balances, which is the single best test of whether you've
understood your own sensors:

```
solar + grid_import + battery_discharge
    ≈ house_load + battery_charge + grid_export
```

If that doesn't add up to within a few percent, something is misread — and it is
better to find out now than to spend a night wondering why your house claims to
use no electricity.
