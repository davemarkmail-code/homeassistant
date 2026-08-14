# 03 — Camera grid

Several camera snapshots in one tile, each opening larger on tap, with lock status
and actions above them.

```
┌──────────────────────────────────────┐
│ 🛡  Security & Cameras               │  header      42px
│ ──────────────────────────────────── │  line         2px
│  🔓          LOCK BATTERY   KEYPAD   │  status     112px
│  UNLOCKED        84%          OK     │
│ ┌────────┐┌────────┐┌────────┐       │  buttons     42px
│ │  OPEN  ││ UNLOCK ││  LOCK  │       │
│ └────────┘└────────┘└────────┘       │
│  FRONT DOOR        DRIVE             │  labels      18px
│ ┌──────────────┐┌──────────────┐     │  cameras    135px
│ │              ││              │     │
│ └──────────────┘└──────────────┘     │
│  GARDEN            EXTRA CAMERA      │  labels      18px
│ ┌──────────────┐┌──────────────┐     │  cameras    135px
│ │              ││              │     │
│ └──────────────┘└──────────────┘     │
└──────────────────────────────────────┘
```

**Needs:** camera entities, and optionally a lock plus its battery/status sensors.
No HACS resources beyond button-card. No bridge.

---

## The grid

Eight rows, six columns. Labels and images alternate so each caption sits directly
above its picture:

```yaml
styles:
  grid:
    - grid-template-areas: >
        "header header header header header header"
        "line line line line line line"
        "status status status status status status"
        "open open unlock unlock lock lock"
        "fronttitle fronttitle fronttitle drivetitle drivetitle drivetitle"
        "front front front drive drive drive"
        "gardentitle gardentitle gardentitle futuretitle futuretitle futuretitle"
        "garden garden garden future future future"
    - grid-template-columns: repeat(6, minmax(0,1fr))
    - grid-template-rows: 42px 2px 112px 42px 18px 135px 18px 135px
    - column-gap: 8px
    - row-gap: 4px
```

Six columns is the trick: the button row splits 2/2/2 while the cameras split 3/3.
Trying to do both with four columns means fighting the grid.

---

## Snapshots without `picture-entity`

You can't nest a `picture-entity` card cleanly inside a button-card grid. Point an
`<img>` at the camera's proxy URL instead:

```yaml
custom_fields:
  front: |
    [[[
      const cam = states['camera.front_door'];
      const url = cam.attributes.entity_picture;
      return `<div style="width:100%;height:100%;border-radius:10px;overflow:hidden">
                <img src="${url}"
                     style="width:100%;height:100%;object-fit:cover;display:block">
              </div>`;
    ]]]
```

`entity_picture` is a signed proxy URL that HA refreshes for you, so this works
without exposing the camera itself.

**`object-fit: cover` is doing real work.** It fills the box and crops, rather than
distorting. Your grid cell will almost never match the camera's aspect ratio, and
`cover` is the difference between "looks deliberate" and "looks stretched".

Style the wrapper so the image has something to fill:

```yaml
styles:
  custom_fields:
    front:
      - height: 135px
      - overflow: hidden
      - border-radius: 10px
```

### Refreshing

Snapshots update when HA updates `entity_picture`. To force a cadence:

```yaml
triggers_update:
  - camera.front_door
  - camera.drive
```

Don't push this hard. Each refresh is a fetch, and four cameras on a short timer is a
lot of traffic for a tile you glance at. This will be the slowest thing on the view —
if a page feels sluggish to load, suspect the cameras before your CSS.

---

## Tap to open larger

```yaml
front:
  card:
    type: custom:button-card
    tap_action:
      action: more-info
      entity: camera.front_door
```

`more-info` on a camera gives you HA's live stream dialog for free. If you'd rather
have a full page, see [13-drilldown-views](13-drilldown-views.md) — a hidden view per
camera with a back button.

---

## A placeholder for the camera you haven't bought yet

```yaml
future: |
  [[[
    return `<div style="width:100%;height:100%;display:flex;flex-direction:column;
                        align-items:center;justify-content:center;
                        border:1px dashed rgba(255,255,255,0.14);border-radius:10px;
                        background:rgba(255,255,255,0.02)">
              <ha-icon icon="mdi:camera-plus-outline"
                       style="--mdc-icon-size:38px;opacity:.35"></ha-icon>
              <div style="font-size:13px;font-weight:700;opacity:.55;margin-top:6px">
                COMING SOON</div>
            </div>`;
  ]]]
```

Better than an empty cell — the grid keeps its shape and it reads as intentional
rather than broken.

---

## Lock status and actions

```yaml
status: |
  [[[
    const locked = states['lock.front_door'].state === 'locked';
    const col  = locked ? '#4ade80' : '#ff5656';
    const icon = locked ? 'mdi:lock' : 'mdi:lock-open-variant';
    return `<div style="display:flex;align-items:center;gap:18px">
              <ha-icon icon="${icon}" style="--mdc-icon-size:44px;color:${col}"></ha-icon>
              <div style="font-size:20px;font-weight:700;color:${col}">
                ${locked ? 'LOCKED' : 'UNLOCKED'}</div>
            </div>`;
  ]]]
```

```yaml
unlock:
  card:
    type: custom:button-card
    template: dm_small_button
    name: Unlock
    icon: mdi:lock-open-variant
    tap_action:
      action: call-service
      service: lock.unlock
      target: { entity_id: lock.front_door }
      confirmation:
        text: Unlock the front door?
```

**Put a confirmation on anything that unlocks a door.** A wall panel is tappable by
anyone walking past it, including people who don't live there.

---

## Gotchas

**Don't set an aspect ratio on the image.** Fix the row height in the grid and let
`object-fit: cover` handle the rest. Trying to preserve the camera's aspect ratio
inside a fixed grid is a fight you won't win.

**Labels get their own rows on purpose.** Putting the caption inside the image cell
means it either overlaps the picture or shrinks it. An 18px label row costs nothing.

**Watch what's behind the tile.** If you're using a glass or textured background, a
bright patch behind a mostly-transparent area of this tile is very visible. See
[../../docs/05-gotchas.md](../../docs/05-gotchas.md).

**Cameras keep rendering when the view isn't visible.** If you have several views each
with cameras, they all keep fetching. Worth knowing if you're chasing network noise.
