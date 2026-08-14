# 22 — Source select

Switching a receiver, TV or speaker between inputs — and the same pattern as
one-tap favourite buttons.

```
┌─────────────────────┬─────────────────────┐
│      🌙             │       🧠            │
│     Chill           │      Focus          │
├─────────────────────┼─────────────────────┤
│      🎤             │       👤            │
│   Clare Teal        │    DaveMarks        │
└─────────────────────┴─────────────────────┘
```

**Needs:** a `media_player` with a `source_list`. No bridge, no extra HACS.

---

## The service

One call does everything:

```yaml
tap_action:
  action: call-service
  service: media_player.select_source
  target:
    entity_id: media_player.living_room
  data:
    source: Chill
```

The value in `source` must match an entry in the entity's `source_list` **exactly** —
it's a string match, so spacing and capitalisation matter. Check what's actually
available in Developer Tools → States before writing the buttons.

---

## Why this matters more than it looks

For speakers, `source_list` is often where your **saved favourites** live. Sonos, for
instance, exposes each Sonos favourite as a source:

```
["Line-in", "Chill", "Clare Teal", "DaveMarks", "Focus"]
```

That's the workaround for streaming services HA can't browse. You can't pick an
arbitrary Apple Music album from HA — but you *can* save it as a favourite in the
vendor's app once, and from then on it's a one-tap button on your dashboard.

New favourites appear in `source_list` automatically. No config change needed on the
HA side beyond adding a button.

---

## A favourite button

```yaml
type: custom:button-card
entity: media_player.living_room
name: Chill
icon: mdi:weather-night
tap_action:
  action: call-service
  service: media_player.select_source
  target: { entity_id: media_player.living_room }
  data: { source: Chill }
styles:
  card:
    - height: 96px
```

Duplicate per favourite and lay them out in a `grid` card with `columns: 2`.

---

## Highlighting the active source

```yaml
styles:
  card:
    - background: >
        [[[ return states['media_player.living_room'].attributes.source === 'Chill'
              ? 'rgba(77,152,255,0.18)' : 'rgba(255,255,255,0.05)'; ]]]
    - border: >
        [[[ return states['media_player.living_room'].attributes.source === 'Chill'
              ? '1px solid rgba(77,152,255,0.6)' : '1px solid rgba(255,255,255,0.10)'; ]]]
triggers_update:
  - media_player.living_room
```

**`source` is often `null`.** Many platforms only populate it when a source was
selected *through HA* — start playback from the vendor's own app and it stays empty
even though something is clearly playing. So treat the highlight as a nice-to-have,
not a reliable indicator, and never build logic that depends on it.

---

## Building the list dynamically

If you'd rather not hard-code buttons, render them from `source_list`:

```yaml
custom_fields:
  list: |
    [[[
      const e = states['media_player.tx_nr6100'];
      const cur = e.attributes.source;
      return (e.attributes.source_list || []).map(s => `
        <div style="padding:8px 12px;margin-bottom:6px;border-radius:8px;
             background:${s === cur ? 'rgba(77,152,255,0.18)' : 'rgba(255,255,255,0.05)'}">
          ${s}
        </div>`).join('');
    ]]]
```

Note the trade-off: this **can't be tapped**. Templated HTML inside a custom field
renders fine but doesn't get button-card's tap handling, so you'd need `browser_mod`
or a nested card per item to make it interactive. Fine for display, no good as a
control. Hard-coded buttons are usually the better answer.

---

## Gotchas

**A receiver that's off is usually `unavailable`**, not `off` — and you can't select a
source on an unavailable entity. If you need to power on and switch input, that's two
steps with a delay, which is a script rather than a button. Some receivers also need
network standby enabled or they vanish from the network entirely when off.

**Sources and favourites are different things** on some platforms, even though both
surface through `source_list`. On Sonos, `Line-in` is a physical input while `Chill`
is a saved favourite — same service call, very different behaviour.

**Don't assume selecting a source starts playback.** On some devices it switches the
input and waits. If a button seems to do nothing, check whether it needs a
`media_play` afterwards.
