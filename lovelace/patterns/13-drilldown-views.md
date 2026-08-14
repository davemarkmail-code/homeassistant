# 13 — Drill-down views

Tap a tile, get a full page of detail, tap once to come back.

Used here for per-room vacuum controls — the vacuum tile opens a room picker, each
room opens its own page of cleaning options — but the shape is general. Anything with
more controls than fit on a tile can hang off one.

---

## Needs

**Resources:** `custom:button-card`.

**Entities:** whatever the detail page is about. The navigation itself needs none.

---

## Why full views rather than popups

There are popup cards for HA — `browser_mod`, `bubble-card` and others — and they're
good. This uses plain views instead, for three reasons:

- **No extra dependency.** Navigation is built in. One less HACS resource to break on
  a core update.
- **The whole screen.** On a wall panel a modal that covers 60% of the display, with
  the dimmed dashboard showing round the edges, looks like an error state. A full page
  looks deliberate.
- **It survives a reload.** A drill-down page has a real URL. If the panel restarts on
  a detail view it comes back to that view. A popup evaporates.

The trade-off is no transition animation and no "click outside to dismiss". For a
touch panel neither is missed.

---

## The structure

```yaml
views:
  # ---- the page that launches it ----
  - title: Home
    path: home
    type: panel
    cards: [ ... ]

  # ---- the picker ----
  - title: Vacuum
    path: vacuum-control
    type: panel
    visible: false
    cards: [ ... ]

  # ---- one page per target ----
  - title: Living Room
    path: vacuum-living
    type: panel
    visible: false
    cards: [ ... ]

  - title: Kitchen
    path: vacuum-kitchen
    type: panel
    visible: false
    cards: [ ... ]
```

**`visible: false`** keeps them out of the header tabs while leaving them reachable by
URL. Without it you get eight tabs across the top, six of which are pages nobody
navigates to directly. If you've hidden the header entirely it changes nothing
visually — set it anyway, so the dashboard still makes sense the day you unhide it.

---

## Launching

Any tile, with a navigate action:

```yaml
tap_action:
  action: navigate
  navigation_path: /office/vacuum-control?kiosk
```

**Carry your query string.** If the dashboard runs with `?kiosk`, every internal link
needs it too. Miss it once and that page — and everything reached from it — comes back
with the HA header showing, which changes the usable height and clips your bottom row.
It presents as "one page has a layout bug", which sends you looking at the grid rather
than the link.

---

## Coming back

The neat bit. Don't add a back button to the header — **make the header the back
button**:

```yaml
header:
  card:
    type: custom:button-card
    show_icon: true
    show_name: true
    show_label: true
    icon: mdi:arrow-left
    name: Living Room
    label: CARPET / VACUUM ONLY / TAP TO GO BACK
    tap_action:
      action: navigate
      navigation_path: /office/vacuum-control?kiosk
    styles:
      card:
        - height: 100%
        - padding: 0 24px
        - border-radius: 16px
        - background: rgba(255,255,255,0.045)
        - border: 1px solid rgba(255,255,255,0.13)
        - box-shadow: >-
            inset 0 1px 0 rgba(255,255,255,0.07),
            0 8px 24px rgba(0,0,0,0.35)
      grid:
        - grid-template-areas: '"i n l"'
        - grid-template-columns: 50px 1fr auto
      icon:
        - width: 31px
        - color: '#4d98ff'
      name:
        - justify-self: start
        - font-size: 22px
        - font-weight: 700
      label:
        - justify-self: end
        - font-size: 12px
        - letter-spacing: 0.10em
        - color: '#8d9499'
```

One element doing three jobs: it says where you are, it says what this page is for,
and the whole bar is a 100px-tall touch target that goes back. No separate button
competing for space, and nothing to miss with a thumb.

**Say "tap to go back" in words.** A left arrow is obvious to us and not to anyone
else in the house. The label costs nothing — it's already there for the subtitle.

---

## Passing context

Two options, and the boring one is usually right.

**One view per target.** Six rooms, six views. Verbose, but each page is independently
editable, the URLs are meaningful, and there's no shared state to get out of step.
That's what's used here.

**One view, state in a helper.** Tapping a room sets `input_select.selected_room`, and
a single detail view templates itself from that. Less YAML, but every device viewing
the dashboard shares the helper — so if a second screen is open, you can be looking at
a page that says "Kitchen" while it acts on the lounge. Fine for one panel, a real
problem for two.

Query strings sound like the answer and aren't:
`navigation_path: /office/room?name=kitchen` navigates fine, but a button-card
template can read `window.location.search` only when it happens to re-evaluate, so
the page can render with the previous room's data. If you go this route, bind the card
to a frequently-updating entity so it repaints — same trick as the clock in
[11-header-bar](11-header-bar.md).

---

## Gotchas

**Browser back works, and does something different.** The system back gesture unwinds
history; your header button navigates forward to a known page. Usually the same
result. If you've bounced between three rooms they diverge — back retraces the bounce,
the header goes straight home. Not a bug, but it's why the header should always name
its destination rather than just showing an arrow.

**Nothing preloads.** Detail views render on first navigation, so a chart-heavy one
has a visible pause the first time. On this dashboard the solar detail takes
90–120 seconds to paint because of history queries — fine for something you open
occasionally, not fine for a page reached by tapping a tile. Keep drill-downs light,
and put the heavy chart behind a second, deliberate tap.

**A template error blanks the page, including your way back.** These are panel views —
one card, whole screen. If the detail page throws, you get black, and the back button
was in that card. Recovery is the URL bar, or the sidebar if you left it on. Test
templates before saving:

```js
new Function('states', `<template body>`)(
  document.querySelector('home-assistant').hass.states
);
```

**Renaming a view path breaks every link to it silently.** There's no link checker.
Grep the config for the old path before you rename — including the query string
variants.

---

## Adapting it

**Confirmation pages** are the same pattern with one destination and a countdown —
[12-confirm-dialog](12-confirm-dialog.md).

**Tablet and phone.** Works unchanged. On small screens the full-page approach is
better than popups, because a modal on a phone is just a worse full page.

**Breadcrumbs**, if you nest more than one level: put the trail in the header's
`label` — `VACUUM › LIVING ROOM` — and point the icon at the parent. Two levels is
usually the point to stop; a wall panel isn't a filesystem.
