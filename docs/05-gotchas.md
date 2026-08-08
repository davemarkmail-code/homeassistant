# Gotchas

Things that cost real hours. Each one was diagnosed by measuring, not guessing.

If you only read one file here, this is the one.

---

## Nested button-cards don't stretch

**Symptom:** every tile has a gap at the bottom. The grid cell is the right height, the
card inside it is short.

**This is the big one.** It caused four separate bugs across one dashboard.

A nested `custom:button-card` sits inside a wrapper `div` that button-card generates for
each custom field. If that wrapper is `display: block`, the nested card is a plain inline
child — it sizes to its content and **will not stretch**, no matter what you do:

```yaml
styles:
  custom_fields:
    solar:
      - height: 100%
      - display: block      # ← the card inside will NOT fill this
```

And critically, `height: 100%` on the nested card's own `ha-card` **doesn't save you**,
because it resolves against a content-sized parent. So you can have `height: 100%` in
two places and still get a short card. That's what makes it so hard to spot — every
inspection says "this is already correct".

**Fix:**

```yaml
      - display: grid       # grid items stretch both axes by default
```

`flex` works too. `block` never will.

**Where it bites:** any tile designed as `1fr <button-row>` to pin buttons to the
bottom. The `1fr` has nothing to expand into, so the whole block collapses to the top
and the buttons float mid-card. Fix the wrapper and every button row in the view snaps
to the same baseline at once.

**Rule:** if a nested button-card should fill its cell, its wrapper is `display: grid`.
Set it when you create the tile, not when you notice the gap.

---

## Opacity animations silently kill backdrop-filter

**Symptom:** glass panels flicker or flatten intermittently, worst on views that update
frequently.

An element with `opacity` below 1 creates a **new backdrop root**. Everything inside it
loses its `backdrop-filter` — the blur samples that element instead of the page behind it.

So a fade-in like this:

```css
@keyframes viewFadeIn { from { opacity: .72 } to { opacity: 1 } }
ha-card { animation: viewFadeIn .45s ease-out both; }
```

…disables all the glass inside that card for 450ms **every time it re-renders**. On a
view with frequently-updating sensors, that's constant flickering.

`opacity` is the surprising one, but the same applies to any ancestor with `filter`,
`transform`, `perspective`, `will-change` or `contain`.

**Fix:** drop the animation, or animate something that doesn't create a backdrop root —
`filter: brightness()` fades fine without breaking descendants.

**Diagnostic:** if `backdrop-filter` computes correctly but doesn't *look* like it's
doing anything, walk up the ancestor chain checking for those five properties before
you touch the blur values.

---

## Blur over a flat background is invisible

**Symptom:** you add `backdrop-filter: blur(20px)`, see no change, and conclude it's
unsupported.

Blur averages nearby pixels. Blur a uniform colour and you get **the same uniform
colour**. Over a plain dark background the effect is mathematically present and visually
nil.

**Fix:** put texture behind it first — a photo, a noise texture, overlapping soft
gradients. *Then* apply the blur.

**Order matters.** Texture first, blur second. Do it the other way round and you'll
spend an hour tuning a blur that cannot possibly show.

**Related:** if your tiles cover ~97% of the screen with small gaps, there's almost no
visible backdrop *around* the glass either — so the only way to perceive it is through
variation *within* each panel. A photo gives you that; a smooth gradient doesn't.

---

## A stale sensor doesn't mean a dead feed

**Symptom:** `last_updated` is hours old, so you conclude the bridge has crashed.

Home Assistant only moves `last_updated` when a value **changes**. A bridge posting
`"Not playing"` every 5 seconds for two hours produces a timestamp two hours old, while
working perfectly.

**Fix:** check liveness at the source — the file's modified time — not the sensor.

```powershell
(Get-Item .\NowPlaying.txt).LastWriteTime
```

Then publish a heartbeat sensor whose value always changes, so you can tell "idle" from
"dead" without leaving HA.

---

## Absolute paths are the thing that breaks when you move a folder

**Symptom:** everything works for months, you reorganise, and one tile quietly freezes.

Scripts using `$PSScriptRoot` survive being moved. Any absolute path doesn't. In a
folder of ~30 scripts, mine had exactly **two** absolute paths — a `.vbs` launcher and
one collector — and both broke silently.

Worse, the already-running process kept working until the machine restarted, so the
failure surfaced *hours* after the change that caused it.

**Fix:** `Join-Path $PSScriptRoot 'file.txt'` everywhere. Grep for `C:\` before you
consider a refactor done.

**The exception:** a `.vbs` in the Startup folder *must* use an absolute path — a
self-locating one would resolve relative to the Startup folder.

---

## Self-overwriting logs destroy their own evidence

```powershell
[IO.File]::WriteAllText($log, "$(Get-Date -f s) bridge online")   # DON'T
```

On a 5-second loop, that's a log with a 5-second memory. Any error is gone before you
can read it — and it looks reassuring while doing so.

**Fix:** append, and trim by size. See [02-windows-bridge.md](02-windows-bridge.md).

---

## DPAPI tokens are bound to the account that made them

`ProtectedData.Protect(..., 'CurrentUser')` ties the blob to that Windows user on that
machine. Copy the `.dat` elsewhere and decryption fails.

That's the security benefit and the migration trap. Re-encrypt on the target machine
rather than copying the file.

Also: **the entropy string is a salt, not a comment.** Rename it during a tidy-up and
every existing token stops decrypting.

---

## Grid rows with fixed heights don't fill their container

**Symptom:** two tiles that should look identical are misaligned by ~20px.

```yaml
- grid-template-rows: 42px 2px 154px 38px    # sums to 236 + gaps
```

In a 276px box, that leaves 25px unused at the bottom and every row rides high. A
sibling using `1fr` for the flexible row fills correctly, and the two drift apart.

**Fix:** make the content row `1fr` so it absorbs the remaining space:

```yaml
- grid-template-rows: 42px 2px 1fr 38px
```

Then both tiles agree regardless of the container height.

---

## Fields not listed in grid-template-areas still take up space

An empty leftover `custom_field` — even one containing `''` — gets **auto-placed into an
implicit row** if it isn't named in `grid-template-areas`. Mine silently stole 16px and
held one tile out of alignment with its neighbours.

**Fix:** delete unused custom fields. Don't leave empty ones lying around.

---

## Two places to change a size

Camera tiles had their height set in **both** `grid-template-rows` **and** the field's
own `styles.custom_fields.<name>.height`. Changing one and not the other grows the
container while the content stays put — so you get a bigger gap rather than a bigger
image.

**Fix:** grep for the value before assuming you've found where it's set.

---

## Inline styles on button-card wrappers get wiped

Testing a change by setting `element.style.foo` in DevTools looks like it works, then
reverts — button-card rewrites the style attribute on its next render.

**Fix:** inject a `<style>` element into the relevant shadow root instead. It survives
re-renders and is trivial to remove.

---

## Restyle templates, not instances

Twenty-two tiles took **one** edit because they shared a `button_card_template`. The
handful styled inline each needed doing individually.

If you find yourself making the same change more than twice, stop and check whether a
template should own it.

---

## Judge colour and size on the real display

A wall panel is usually brighter and punchier than a desk monitor. Dark tints in
particular shift a lot. I tuned a backdrop that looked right on a desktop screenshot and
was noticeably too light on the panel.

**Fix:** build a runtime switcher (an `input_select` plus a template) so you can cycle
options while standing in front of the thing. Hardcode the winner and delete the
machinery afterwards.
