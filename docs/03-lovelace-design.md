# Lovelace design — grid, templates and glass

Building a dashboard that exactly fills a non-standard panel, and making it look like
frosted glass.

Everything here needs [button-card](https://github.com/custom-cards/button-card).

---

## One card per view

Each view is a single `custom:button-card` in panel mode. Tiles are nested
button-cards placed into named grid areas.

```yaml
views:
  - path: home
    panel: true
    cards:
      - type: custom:button-card
        show_icon: false
        show_name: false
        styles:
          card:
            - height: 100%
            - padding: 10px
            - box-sizing: border-box
          grid:
            - grid-template-areas: >
                "head head head head"
                "solar security vacuum mower"
                "solar security heating printer"
            - grid-template-columns: repeat(4, minmax(0,1fr))
            - grid-template-rows: 72px 10px minmax(0,1fr) 10px minmax(0,1fr)
            - column-gap: 10px
          custom_fields:
            solar:
              - height: 100%
              - display: grid          # NOT block — see gotchas
        custom_fields:
          solar:
            card:
              type: custom:button-card
              template: dm_tile
```

### Use `minmax(0,1fr)`, not `1fr`

A bare `1fr` won't shrink below its content's minimum size, so one overflowing tile
pushes the whole grid off-screen. `minmax(0,1fr)` lets it shrink and keeps the layout
locked to the viewport.

### Pick one spacing standard and apply it everywhere

```yaml
- grid-template-columns: repeat(12, minmax(0,1fr))
- grid-template-rows: 72px 10px minmax(0,1fr) 10px minmax(0,1fr)
- column-gap: 10px
```

Those `10px` rows are gutters — a spacer row is easier to reason about than `row-gap`
when some tiles span multiple rows. Whatever you choose, make every view byte-identical.
Mine drifted to `30px` on one view and it was visibly wrong long before I found why.

---

## Templates do the heavy lifting

Define the look once, reference it everywhere. `button_card_templates` sits at the top
level of the dashboard config:

```yaml
button_card_templates:
  dm_tile:
    show_icon: true
    show_name: true
    styles:
      card:
        - padding: 16px 18px
        - border-radius: 16px
        - background: rgba(255,255,255,0.045)
        - border: 1px solid rgba(255,255,255,0.13)
        - box-shadow: inset 0 1px 0 rgba(255,255,255,0.07), 0 8px 24px rgba(0,0,0,0.35)
      name:
        - font-size: 20px
        - font-weight: 600
        - color: rgb(229,229,229)
      icon:
        - width: 28px
        - color: rgba(235,240,235,0.82)
```

This matters more than it looks. Restyling 22 tiles across 11 views was **one edit**
because they shared a template. The tiles that had been styled inline instead each
needed doing by hand.

**Audit your templates occasionally.** Mine had two that differed only in icon colour
— one blue, one white — which is how half the dashboard ended up subtly inconsistent
without anyone deciding it should be.

---

## Glass

Five ingredients. Miss any one and it reads as a flat tinted box.

```yaml
styles:
  card:
    # 1. translucent fill, with a diagonal specular sweep
    - background: >
        linear-gradient(135deg,
          rgba(255,255,255,0.155) 0%,
          rgba(255,255,255,0.05) 26%,
          rgba(255,255,255,0.012) 52%,
          rgba(255,255,255,0) 78%),
        linear-gradient(0deg, rgba(14,17,24,0.26), rgba(14,17,24,0.26))

    # 2. the blur itself — brightness lifts the panel above its surroundings
    - backdrop-filter: blur(17px) saturate(122%) brightness(1.16)
    - -webkit-backdrop-filter: blur(17px) saturate(122%) brightness(1.16)

    # 3. a crisp rim
    - border: 1px solid rgba(255,255,255,0.34)

    # 4. directional edge lighting + lift
    - box-shadow: >
        inset 1px 1px 0 rgba(255,255,255,0.34),
        inset -1px -1px 0 rgba(0,0,0,0.42),
        0 14px 38px rgba(0,0,0,0.7)
```

The fifth ingredient isn't on the tile at all — it's **something worth blurring behind
it**:

```yaml
# on the view's root card
styles:
  card:
    - background-image: url(/local/glass-texture.jpg)
    - background-size: cover
    - background-position: center
    - box-shadow: 0 0 0 1px rgba(255,255,255,0.04),
                  inset 0 0 0 9999px rgba(2,5,16,0.95)   # tint overlay
```

### Why the texture is mandatory

`backdrop-filter` blurs what's behind the element. **Blur a flat colour and you get the
same flat colour.** Over a plain dark background the effect is literally invisible, and
you'll conclude it's broken when it's working perfectly.

A photo or texture gives the blur something to soften. It also means each panel blurs a
*different* region, so tiles differ from one another — and that variation is most of
what sells the illusion.

Keep the texture **dark and low-contrast**. Anything bright fights your text. Put it in
`config/www/` and reference it as `/local/…` — one file shared by every view. Don't
embed it as a data URI: at ~110 KB per view, eleven views is 1.2 MB of dashboard config.

### Tinting

The huge `inset 0 0 0 9999px rgba(...)` shadow is a full-surface colour wash over the
texture. It means one greyscale image can be tinted any colour without touching the
image.

To make the tint switchable at runtime, drive it from an `input_select` and template it:

```yaml
- box-shadow: >
    [[[ var t = states['input_select.dashboard_tint'].state;
        var m = {'Deep Blue':'10,17,44,0.88','Deepest Blue':'2,5,16,0.95'};
        return '0 0 0 1px rgba(255,255,255,0.04), inset 0 0 0 9999px rgba(' +
               (m[t] || m['Deepest Blue']) + ')'; ]]]
triggers_update:
  - input_select.dashboard_tint
```

Add a `hold_action` calling `input_select.select_next` on any tile and you can cycle
colours by touch — invaluable for judging on the real panel, since colours shift a lot
between your desk monitor and the wall display. Hardcode the winner afterwards and
remove the machinery.

### Darkness

Darken via the **tint colour**, not the opacity. Raising opacity toward 1 flattens the
texture out and you lose the blur's raw material. Dropping the colour toward black keeps
the texture modulating underneath.

---

## Sizing for a wall panel

Everything looks smaller on a wall than on your desk. Expect to size up:

| Element | Desk | Wall panel |
|---|---|---|
| Tile title | 17–18px | **20px / 600** |
| Tile icon | 22–24px | **28px** |
| Button label | 12–13px | **14–15px** |
| Button icon | 18–21px | **21–25px** |

Bump everything by the same amount so the relative hierarchy survives. And check for
truncation afterwards — larger text clips sooner, and the fix is usually a shorter
label rather than a smaller font.

**Judge sizes on the actual panel.** A screenshot on a desktop monitor is close to
useless for this.
