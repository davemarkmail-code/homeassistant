# Styling Home Assistant's own cards — and what fails inside shadow DOM

Two problems took me longer than anything else in this project, and both come
down to the same thing: Home Assistant's cards live inside **shadow DOM**, and
some CSS features simply do not cross that boundary.

If you only read one page of this booklet, make it this one. It will save you
an evening.

---

## Problem 1 — recolouring a native slider

**What I wanted:** the volume slider on a `tile` card to match my olive palette
instead of Home Assistant's default blue.

**What I tried first**, which seemed obvious:

```yaml
card_mod:
  style: |
    ha-card {
      --control-slider-color: #94a860 !important;
    }
```

No effect. The slider stayed blue.

**Why it failed.** I traced the variable up the element tree and found this:

```
HA-CONTROL-SLIDER                        --control-slider-color: #9e9e9e   ← wins
HUI-MEDIA-PLAYER-VOLUME-SLIDER-FEATURE   --control-slider-color: #94a860
HUI-CARD-FEATURE                         --control-slider-color: #94a860
HUI-CARD-FEATURES                        --control-slider-color: #94a860
HA-TILE-CONTAINER                        --control-slider-color: #94a860
HA-CARD                                  --control-slider-color: #94a860
```

My value was inheriting down perfectly — and then the slider element itself
**overrode it**, because it derives its colour from `--tile-color`, which Home
Assistant sets **inline on the card** from the entity's *state* colour.

That is also why the slider looked blue when the amp was on and grey when it was
off: I wasn't fighting a theme, I was fighting the entity state.

**The fix** — override `--tile-color`, and use `!important` so a stylesheet rule
beats an inline style:

```yaml
type: tile
entity: media_player.lounge_receiver
features:
  - type: media-player-volume-slider
card_mod:
  style: |
    ha-card {
      height: 110px !important;
      background: rgba(255,255,255,0.05) !important;
      border: 1px solid transparent !important;
      box-shadow: none !important;
      --tile-color: #94a860 !important;                 /* the one that matters */
      --feature-color: #94a860 !important;
      --control-slider-color: #94a860 !important;
      --control-slider-background: #ddd6c8 !important;
      --control-slider-background-opacity: 0.18 !important;
    }
```

**Two things worth knowing:**

- Style at the **`ha-card` level only**. My attempts to reach deeper with
  selectors like `ha-tile-info $` collapsed the card entirely and dropped
  sibling cards out of position. Twice.
- The native `tile` card is the only practical way to get a **draggable**
  volume slider. A `button-card` can fake the look but not the drag.

---

## Problem 2 — animating a progress ring

**What I wanted:** the border of the active navigation button to fill red,
clockwise, as a countdown to the dashboard auto-rotating to the next page.

**What I tried first**, the standard modern technique — a conic gradient masked
to the border, with a registered custom property so it can be animated:

```css
@property --sweep { syntax: '<percentage>'; inherits: false; initial-value: 0%; }
@keyframes sweep { from { --sweep: 0%; } to { --sweep: 100%; } }

ha-card::after {
  content: '';
  position: absolute; inset: 0;
  border-radius: 10px; padding: 2px;
  background: conic-gradient(from -90deg, #ff3b3b var(--sweep), transparent 0);
  -webkit-mask: linear-gradient(#000 0 0) content-box,
                linear-gradient(#000 0 0);
  -webkit-mask-composite: xor;
          mask-composite: exclude;
  animation: sweep 60s linear forwards;
}
```

The pseudo-element appeared. The gradient and the animation did **nothing**.

**Why it failed.** `@property` is **document-scoped**. Registering it inside a
card's shadow root is silently ignored, so `var(--sweep)` was invalid, which
made the whole `conic-gradient` invalid, which killed the background — and with
nothing to animate, the animation didn't run either.

This is worth internalising, because `@property` is exactly the tool you reach
for and it will fail without telling you why.

**The fix** — SVG stroke animation, which needs no custom properties at all:

```javascript
// inside a button-card custom_field template
'<svg style="position:absolute;inset:0;width:100%;height:100%;'
+ 'overflow:visible;pointer-events:none">'
+ '<style>@keyframes dmsw{to{stroke-dashoffset:0}}'
+ '.dmr{width:calc(100% - 2px);height:calc(100% - 2px);}</style>'
+ '<rect class="dmr" x="1" y="1" rx="9" fill="none" stroke="#ff2f2f"'
+ ' stroke-width="3" pathLength="100" stroke-dasharray="100"'
+ ' stroke-dashoffset="100"'
+ ' style="animation:dmsw ' + total + 'ms linear forwards;'
+ ' animation-delay:-' + elapsed + 'ms"/></svg>'
```

**The tricks in there, each of which mattered:**

**`pathLength="100"`** — normalises the path so `stroke-dasharray: 100` and
`stroke-dashoffset: 100` work regardless of the element's pixel size. No need to
calculate a perimeter.

**Positioning at `inset: -2px`** — an absolutely positioned child sits inside the
**padding box**, so `inset: 0` draws the line *inboard* of the card's 2px border.
Pulling it out by the border width puts the stroke *on* the border, which is what
makes it read as "the border is filling up" rather than "there's a second line".

**A negative `animation-delay`** — this is the important one. The card
re-renders periodically (my rotation engine runs on a 1-second timer), and every
re-render **restarts a CSS animation from zero**. The visible symptom was a line
that crept a little way round, snapped back, crept slightly further, snapped
back. Setting a fixed 60s duration with a negative delay equal to the time
already elapsed means every re-render lands the animation exactly where it should
be. Re-renders become invisible.

```javascript
var total   = 60000;
var rem     = nextSwapTimestamp - Date.now();
var elapsed = total - rem;
// animation: dmsw 60000ms linear forwards; animation-delay: -<elapsed>ms
```

---

## Problem 3 — a loading spinner

Trivial by comparison, but genuinely useful: when a script takes a few seconds,
show that the tap registered. Otherwise people press twice.

```javascript
// custom_field on the tile, positioned absolute top-left
'[[[ var s = states["script.lounge_streaming"];'
+ ' if (!s || s.state !== "on") return "";'
+ ' return "<div style=\'width:16px;height:16px;"'
+ ' + "border:2px solid rgba(221,214,200,0.25);"'
+ ' + "border-top-color:#ddd6c8;border-radius:50%;"'
+ ' + "animation:dmspin 0.8s linear infinite\'></div>"'
+ ' + "<style>@keyframes dmspin{to{transform:rotate(360deg)}}</style>"; ]]]'
```

Add the script to `triggers_update` so the tile re-renders when it starts and
stops. A plain CSS keyframe animation **does** work inside shadow DOM — it is
only `@property` that doesn't.

---

## The general rule

Inside a card's shadow root:

| Technique | Works? |
|---|---|
| CSS custom properties (inheriting *in*) | Yes |
| `@keyframes` | Yes |
| SVG `stroke-dashoffset` animation | Yes |
| Pseudo-elements via `::after` | Yes |
| `@property` registration | **No — document-scoped** |
| Reaching *into* a nested card's shadow root | Effectively no |

When something styles correctly but refuses to animate, suspect a custom
property that never got registered.

---

## Always pre-flight a template before saving

A single JavaScript error in one `button-card` template can blank an entire
view — and if that view is a full-screen kiosk card, you lose the whole screen
and have to fix it blind.

Evaluate the template against live state *before* you write the config:

```javascript
const inner = template.replace(/^\s*\[\[\[/, '').replace(/\]\]\]\s*$/, '');
const result = new Function('states', 'entity', 'user', 'hass', inner)(
  hass.states, null, hass.user, hass
);
// only save if result is a non-empty string
```

This caught a genuine error for me — `Identifier 'ev' has already been declared`
— because the variable I was trying to reassign sat inside a `const` declaration
list. Had I saved it, the entire energy-flow card would have gone blank.
