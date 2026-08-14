# 11 — Header bar

The strip across the top of every page: page navigation, a clock, and the two buttons
you don't want anyone pressing by accident.

Probably the most directly reusable thing in this repo. Nothing about it is specific
to solar, or to a wall panel, or to any integration — it's the chrome that makes a
set of views feel like one application instead of a pile of dashboards.

---

## Needs

**Resources:** `custom:button-card`.

**Entities:** none required. The clock needs no entity at all, though there's a wrinkle
below about making it tick. The power buttons need whatever you want them to call —
`button.press`, `script.turn_on`, a shell command, or nothing.

---

## Why not just use the built-in tabs?

The HA header gives you view tabs for free, and for a desk browser that's fine. Three
reasons this exists instead:

- **Touch targets.** Header tabs are small. At arm's length on a wall panel, a 72px
  chip is the difference between working and not.
- **It disappears under kiosk-mode.** If you hide the header to reclaim vertical space
  — which is the whole point on a 720px-tall panel — you also delete your only way to
  change page. Ask how I know.
- **You can put other things in it.** A clock, a power menu, a now-playing strip. The
  built-in header isn't yours to fill.

If you're hiding the header, **build this first**. Otherwise the day kiosk-mode
actually loads properly, you'll be stranded on whichever view you happen to be on.

---

## The chip template

Every element in the bar is the same template with a different label. Define it once:

```yaml
button_card_templates:
  topchip:
    show_icon: true
    show_name: true
    show_label: false
    styles:
      card:
        - height: 72px
        - padding: 0 20px
        - border-radius: 16px
        - border: 1px solid rgba(255,255,255,0.34)
        - box-shadow: >-
            inset 1px 1px 0 rgba(255,255,255,0.34),
            inset -1px -1px 0 rgba(0,0,0,0.40)
        - background: >-
            linear-gradient(135deg,
              rgba(255,255,255,0.155) 0%,
              rgba(255,255,255,0.05)  26%,
              rgba(255,255,255,0.012) 52%,
              rgba(255,255,255,0)     78%),
            linear-gradient(0deg,
              rgba(14,17,24,0.26), rgba(14,17,24,0.26))
      grid:
        - grid-template-areas: '"i n"'
        - grid-template-columns: auto 1fr
        - align-items: center
        - column-gap: 10px
      icon:
        - width: 22px
      name:
        - font-size: 15px
        - font-weight: 600
        - letter-spacing: 0.06em
        - text-transform: uppercase
        - justify-self: start
```

That double-gradient is doing real work. The 135° pass is a diagonal sheen from
bright top-left to nothing by 78%, and the flat pass underneath darkens the whole
thing so the sheen has something to sit on. Combined with the asymmetric inset shadow
— light top-left, dark bottom-right — you get a chip that reads as physically raised
without a drop shadow. See [`glass.yaml`](../glass.yaml).

---

## Navigation

```yaml
custom_fields:
  nav:
    card:
      type: custom:button-card
      show_icon: false
      show_name: false
      styles:
        card:
          - background: none
          - border: none
          - box-shadow: none
          - padding: 0
        grid:
          - grid-template-areas: '"lbl a b c d"'
          - grid-template-columns: auto repeat(4, 1fr)
          - column-gap: 10px
          - align-items: center
      custom_fields:
        lbl: |
          [[[
            return '<div style="font-size:11px;letter-spacing:.14em;' +
                   'color:#8d9499;text-transform:uppercase;padding-right:6px">' +
                   'Page navigation</div>';
          ]]]
        a:
          card:
            type: custom:button-card
            template: topchip
            name: Work
            icon: mdi:briefcase-outline
            tap_action: { action: navigate, navigation_path: /office/work }
            styles:
              card:
                - border-color: >-
                    [[[ return window.location.pathname.endsWith('/work')
                        ? '#25c2a0' : 'rgba(255,255,255,0.34)' ]]]
              name:
                - color: >-
                    [[[ return window.location.pathname.endsWith('/work')
                        ? '#25c2a0' : '#e6ebee' ]]]
        # b, c, d identical with their own name / icon / path
```

**Marking the active page** is the only fiddly part. There's no "current view"
variable in button-card, so read the URL directly: `window.location.pathname`. It's
available inside `[[[ ]]]` like any browser global.

Two caveats. It won't re-evaluate on navigation alone — see the ticking problem below,
same fix. And if a view is reachable by more than one path, `endsWith` will miss;
compare against the full path in that case.

---

## Clock

The entire clock:

```yaml
clock:
  card:
    type: custom:button-card
    template: dm_clock
    entity: sensor.some_frequently_updating_entity
    name: |
      [[[
        return new Date().toLocaleTimeString('en-GB',
          { hour: '2-digit', minute: '2-digit' });
      ]]]
    label: |
      [[[
        return new Date().toLocaleDateString('en-GB',
          { weekday: 'long', day: '2-digit', month: 'long' }).toUpperCase();
      ]]]
```

**The `entity` line is the trick, and it's the thing people miss.** Nothing about a
clock needs an entity — `new Date()` doesn't care. But a button-card only
re-evaluates its templates when something tells it to, and "the wall clock advanced"
isn't an event it knows about. Bind it to any entity that changes often and you get a
free repaint on every change.

Cleaner alternative if you'd rather not borrow an unrelated sensor:

```yaml
    entity: sensor.time          # from the `time_date` integration
```

`sensor.time` updates once a minute, exactly, which is precisely what a
hours-and-minutes clock wants. Add to `configuration.yaml`:

```yaml
sensor:
  - platform: time_date
    display_options: [ 'time', 'date' ]
```

Or use `update_timer: 30s` on the card and skip the entity entirely. All three work;
`sensor.time` is the tidiest.

**Locale matters.** `en-GB` gives 24-hour time and day-before-month. Swap to your own
locale string rather than hand-rolling the formatting — `toLocaleDateString` handles
ordinals, month names and week-start correctly for free.

---

## Power buttons

```yaml
power:
  card:
    type: custom:button-card
    show_icon: false
    show_name: false
    styles:
      card: [ background: none, border: none, box-shadow: none, padding: 0 ]
      grid:
        - grid-template-areas: '"r s"'
        - grid-template-columns: 1fr 1fr
        - column-gap: 10px
    custom_fields:
      r:
        card:
          type: custom:button-card
          template: topchip
          name: Restart
          icon: mdi:restart
          tap_action:
            action: navigate
            navigation_path: /office/power-confirm?do=restart
      s:
        card:
          type: custom:button-card
          template: topchip
          name: Shutdown
          icon: mdi:power
          tap_action:
            action: navigate
            navigation_path: /office/power-confirm?do=shutdown
```

**Note these navigate rather than act.** Neither button does anything directly — they
open a confirmation view. On a wall panel at hip height, a bare shutdown button will
eventually get leant on. See [12-confirm-dialog](12-confirm-dialog.md).

---

## Putting the bar together

In the view's top-level card:

```yaml
styles:
  grid:
    - grid-template-areas: >-
        "nowplaying nav power clock"
        "gap gap gap gap"
        "... the rest of your view ..."
    - grid-template-columns: 620px 1fr auto 260px
    - grid-template-rows: 72px 10px minmax(0,1fr)
    - column-gap: 10px
```

The header row is a fixed `72px`. Everything below is `minmax(0,1fr)`.

**Use `minmax(0,1fr)`, not `1fr`.** A bare `1fr` row won't shrink below its content's
intrinsic size, so one oversized tile pushes the whole view taller than the screen and
you get a scrollbar on a panel that should never scroll. `minmax(0,1fr)` lets it
compress. This costs an hour to work out the first time.

---

## Gotchas

**Templates don't re-run on navigation.** Change page and the active-chip highlight
may not update, because nothing told the card to repaint. Binding to `sensor.time`
fixes it within a minute; `update_timer: 5s` fixes it faster. Slightly unsatisfying
either way — there's no navigation event button-card can hook.

**Don't hide the sidebar as well as the header.** `kiosk_mode` can hide both. Hiding
the header is fine once you have this bar. Hiding both leaves no route out of the
dashboard at all if a template error blanks your nav — you'd be reinstalling to get
back in. Leave the sidebar, or keep a browser bookmark to `/config/lovelace`.

**Kiosk-mode loads lazily.** Like any HACS resource, `kiosk-mode.js` has to load and
register before it does anything. On a mobile app a pull-to-refresh isn't enough — you
need a genuine force-quit. Until then the header stays visible and your layout is
~56px shorter than you designed for, which shows up as the bottom row clipping.

---

## Adapting it

**More or fewer pages.** Change `grid-template-areas` and the column count. Four fits
comfortably at 2560px wide; on a tablet, three is the practical limit before labels
start truncating.

**Icon-only chips** for narrow screens: `show_name: false` and drop to
`grid-template-columns: repeat(N, 1fr)`. You lose the labels but gain roughly 90px per
chip.

**Highlight colour.** `#25c2a0` here. Whatever you pick, apply it to both the border
and the label — border alone is too subtle to read at a distance, label alone looks
like a rendering bug.

**Nav on a tablet.** Same pattern, smaller: 62px chips and 12px labels. See
[13-drilldown-views](13-drilldown-views.md) for keeping detail pages consistent with it.
