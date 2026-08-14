# 21 — Now playing

Artwork, track, artist and transport controls for any media player.

```
┌────────────────────────────────────────┐
│ ┌────────┐  PLAYING                    │
│ │ artwork│  Valjean's Soliloquy        │
│ │        │  Hugh Jackman               │
│ └────────┘  Volume 42%                 │
├────────────────────────────────────────┤
│  ⏮        ⏯        🔉        🔊        │
└────────────────────────────────────────┘
```

**Needs:** any `media_player` entity. No bridge, no HACS beyond button-card.

---

## The tile

```yaml
type: custom:button-card
entity: media_player.living_room
show_name: false
show_icon: false
triggers_update:
  - media_player.living_room
styles:
  card:
    - height: 150px
    - padding: 14px
    - border-radius: 14px
    - background: rgba(255,255,255,0.05)
  grid:
    - grid-template-areas: '"c"'
    - grid-template-columns: minmax(0,1fr)
  custom_fields:
    c:
      - width: 100%
      - text-align: left
custom_fields:
  c: |
    [[[
      const e = states['media_player.living_room'];
      const a = e.attributes || {};
      const art = a.entity_picture
        ? `<img src="${a.entity_picture}" style="width:104px;height:104px;
             border-radius:10px;object-fit:cover">`
        : `<div style="width:104px;height:104px;border-radius:10px;
             background:rgba(255,255,255,0.06);display:flex;
             align-items:center;justify-content:center">
             <ha-icon icon="mdi:music" style="--mdc-icon-size:38px;opacity:.4"></ha-icon>
           </div>`;
      const title  = a.media_title || 'Nothing playing';
      const artist = a.media_artist || a.media_album_name || '';
      return `<div style="display:flex;gap:14px;align-items:center;height:100%">
                ${art}
                <div style="flex:1;min-width:0">
                  <div style="font-size:11px;opacity:.5;letter-spacing:1px">
                    ${e.state.toUpperCase()}</div>
                  <div style="font-size:19px;font-weight:700;white-space:nowrap;
                       overflow:hidden;text-overflow:ellipsis;margin-top:3px">${title}</div>
                  <div style="font-size:13px;opacity:.6;white-space:nowrap;
                       overflow:hidden;text-overflow:ellipsis">${artist}</div>
                  <div style="font-size:12px;opacity:.45;margin-top:6px">
                    Volume ${Math.round((a.volume_level || 0) * 100)}%</div>
                </div>
              </div>`;
    ]]]
```

### `min-width: 0` is not optional

That `flex: 1; min-width: 0` on the text column is what makes `text-overflow: ellipsis`
work. Without it a flex child refuses to shrink below its content width, the text
pushes the layout wide, and long track titles blow the tile apart instead of
truncating. Easy to omit, annoying to diagnose.

### Artwork is free

`entity_picture` is served by HA as a proxied URL — no API key, no direct call to the
music service. Works for Sonos, Spotify, Chromecast, AirPlay, anything that reports it.
Always provide a fallback for when it's absent; `null` renders as a broken image icon.

---

## Transport controls

Four small buttons in a grid beneath:

```yaml
type: grid
columns: 4
square: false
cards:
  - type: custom:button-card
    show_name: false
    icon: mdi:skip-previous
    tap_action:
      action: call-service
      service: media_player.media_previous_track
      target: { entity_id: media_player.living_room }
  - type: custom:button-card
    show_name: false
    icon: mdi:play-pause
    tap_action:
      action: call-service
      service: media_player.media_play_pause
      target: { entity_id: media_player.living_room }
  # volume_down / volume_up follow the same shape
```

`media_play_pause` beats separate play and pause buttons — one control, no state
tracking, and it always does the right thing.

---

## Progress bar, if you want one

```javascript
const pos = a.media_position || 0;
const dur = a.media_duration || 0;
const pct = dur ? (pos / dur) * 100 : 0;
```

```html
<div style="height:3px;background:rgba(255,255,255,0.10);border-radius:2px">
  <div style="width:${pct}%;height:100%;background:#4d98ff;border-radius:2px"></div>
</div>
```

**It won't animate.** `media_position` only updates when HA receives a new state, not
every second, so the bar jumps rather than slides. You can interpolate using
`media_position_updated_at` plus a short `update_timer`, but for a glanceable tile
it's rarely worth the extra re-rendering.

---

## Gotchas

**Check `supported_features` before adding buttons.** Not every player supports every
command — calling `media_previous_track` on something that can't do it fails silently
and the button just looks broken.

**State strings vary by platform.** `playing`, `paused`, `idle`, `off`, `standby` — and
which ones appear differs. Don't write logic that assumes only `playing` and `paused`
exist.

**Streaming services usually can't be browsed.** With Sonos, for example, HA can list
your *Sonos favourites* but not browse Apple Music or Spotify libraries — that's the
vendor's API, not an HA limitation. Save what you listen to as favourites and drive
those instead; see [22-source-select](22-source-select.md).
