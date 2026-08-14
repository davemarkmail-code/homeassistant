# 12 — Confirm dialog

A full-screen "are you sure?" with a live countdown, for the buttons you don't want
pressed by accident.

Used here for restart and shutdown of the PC driving the wall panel. Both buttons in
the header navigate here rather than doing anything, and this page is what actually
fires — after ten seconds, unless you cancel.

---

## Needs

**Resources:** `custom:button-card`.

**Helpers:** one `timer` per action. Created in the UI under
**Settings → Devices & Services → Helpers → Timer**, or in YAML:

```yaml
timer:
  pc_restart_countdown:
    name: PC restart countdown
    duration: "00:00:10"
  pc_shutdown_countdown:
    name: PC shutdown countdown
    duration: "00:00:10"
```

**Automations:** one per timer, firing on `timer.finished`.

---

## Why a timer helper, not JavaScript

The obvious implementation is a `setTimeout` in the card template. Don't.

A `timer` entity lives in Home Assistant, not in the browser. That means:

- **It survives a reload.** Navigate away, refresh, or lose the tab, and the countdown
  keeps running — because the thing counting isn't the page.
- **You can act on it from an automation.** `timer.finished` is a normal trigger. The
  browser doesn't have to be the thing that fires the action, which matters when the
  action is "shut this machine down".
- **Cancelling is a service call.** `timer.cancel` — one line, works from anywhere,
  including your phone if you've walked away mid-countdown.
- **It's visible.** The timer shows in Developer Tools with its remaining time. A
  `setTimeout` is invisible and unkillable once started.

The browser's only job is to display the number and offer a cancel button. All the
state is server-side, which is the right split.

---

## The view

```yaml
- title: Confirm
  path: power-confirm
  type: panel
  visible: false
  cards:
    - type: custom:button-card
      show_icon: false
      show_name: false
      styles:
        card:
          - height: 100vh
          - padding: 12px 20px
          - background: 'rgba(120,20,20,0.10)'
          - border: none
          - border-radius: 0
          - box-sizing: border-box
        grid:
          - grid-template-areas: |
              "countdown"
              "gap"
              "cancel"
          - grid-template-columns: 1fr
          - grid-template-rows: minmax(0,1fr) 16px 120px
        custom_fields:
          countdown: [ display: grid, height: 100% ]
          cancel:    [ display: grid, height: 100% ]
      custom_fields:
        countdown: ...   # below
        cancel: ...      # below
```

**Tint the whole page.** A faint red wash over the background is the fastest signal
that this page is not like the others. It reads before any text does.

---

## The countdown

```yaml
countdown:
  card:
    type: custom:button-card
    show_icon: false
    show_name: true
    show_label: true
    entity: timer.pc_restart_countdown
    name: |
      [[[
        var ids = ['timer.pc_restart_countdown',
                   'timer.pc_shutdown_countdown'];

        // whichever one is running is the one we're confirming
        var t = ids.map(function(id) { return states[id]; })
                   .filter(function(s) { return s && s.state === 'active'; })[0];

        if (!t) return 'NOTHING PENDING';

        // finishes_at is an absolute ISO timestamp, not a duration
        var left = Math.max(0,
          Math.round((new Date(t.attributes.finishes_at) - new Date()) / 1000));

        var what = t.entity_id.indexOf('restart') > -1 ? 'RESTART' : 'SHUTDOWN';
        return what + ' IN ' + left;
      ]]]
    label: |
      [[[
        var ids = ['timer.pc_restart_countdown',
                   'timer.pc_shutdown_countdown'];
        var any = ids.some(function(id) {
          return states[id] && states[id].state === 'active';
        });
        return any ? 'TAP CANCEL BELOW TO STOP THIS'
                   : 'NO ACTION IS PENDING — SAFE TO GO BACK';
      ]]]
    styles:
      card:
        - background: none
        - border: none
        - box-shadow: none
      name:
        - font-size: 96px
        - font-weight: 800
        - color: '#ff5656'
        - letter-spacing: 0.02em
      label:
        - font-size: 18px
        - letter-spacing: 0.14em
        - color: '#cfc7b6'
        - margin-top: 12px
```

**`finishes_at` is an absolute timestamp**, not a remaining duration. Subtract `now`
to get seconds left. There's also `remaining`, but it only updates when the timer's
state changes — so a card bound to `remaining` shows a frozen number. `finishes_at`
plus a repaint gives you a real ticking display.

**Which repaint?** Binding `entity:` to the timer isn't enough on its own, because an
active timer doesn't emit a state change every second. Add:

```yaml
    update_timer: 1s
```

Then the template re-evaluates once a second regardless, and the number counts down
properly. Same class of problem as the clock in [11-header-bar](11-header-bar.md).

---

## The cancel button

```yaml
cancel:
  card:
    type: custom:button-card
    show_icon: true
    show_name: true
    icon: mdi:close-circle-outline
    name: CANCEL
    tap_action:
      action: multi-actions
      actions:
        - action: call-service
          service: timer.cancel
          service_data:
            entity_id: timer.pc_restart_countdown
        - action: call-service
          service: timer.cancel
          service_data:
            entity_id: timer.pc_shutdown_countdown
        - action: navigate
          navigation_path: /office/work?kiosk
    styles:
      card:
        - height: 100%
        - border-radius: 16px
        - background: 'rgba(255,255,255,0.06)'
        - border: 1px solid rgba(255,86,86,0.55)
      icon:
        - width: 40px
        - color: '#ff5656'
      name:
        - font-size: 26px
        - font-weight: 700
        - color: '#ff5656'
```

**`multi-actions`** is a button-card extension — cancel both timers, then navigate
away, in one tap. Cancelling a timer that isn't running is harmless, so there's no
need to work out which one is active.

**Cancel is the only button on the page.** No "confirm" button, because arriving here
*is* the confirmation — the countdown is already running. One deliberate action to
start it, one obvious action to stop it. Adding a confirm button would mean two taps
to do the thing and one to not, which is backwards.

---

## Wiring the action

```yaml
automation:
  - alias: PC restart — fire when countdown finishes
    trigger:
      - platform: event
        event_type: timer.finished
        event_data:
          entity_id: timer.pc_restart_countdown
    action:
      - service: button.press
        target:
          entity_id: button.office_pc_restart
```

Swap the action for whatever yours is — a shell command, a switch, an MQTT publish.

**Note it triggers on the event, not the state.** `timer.finished` fires only on
natural expiry. A cancelled timer goes to `idle` without firing it, so cancel really
does mean cancel. If you trigger on `state: idle` instead, cancelling will *cause* the
thing you were trying to prevent — which is a memorable way to shut your PC down.

---

## Starting it

From the header buttons:

```yaml
tap_action:
  action: multi-actions
  actions:
    - action: call-service
      service: timer.start
      service_data:
        entity_id: timer.pc_restart_countdown
    - action: navigate
      navigation_path: /office/power-confirm?kiosk
```

Start the timer, then show the page.

---

## Gotchas

**`remaining` doesn't tick.** Covered above, but it's the first thing everyone tries
and it silently half-works — the number is right when you land on the page and then
never changes. Use `finishes_at`.

**Ten seconds is about right.** Long enough to react, short enough that you don't
walk away assuming it didn't take. Under five is panic; over thirty and people stop
watching.

**The page needs to handle "nothing pending".** Land here by URL, or after cancelling
in another tab, and no timer is active. Without a guard you get `NaN` in 96px type.
The template above returns `NOTHING PENDING` instead.

**Don't rely on the browser being alive.** The whole point of the timer helper is that
the action fires from HA. If you also gate it on the panel being awake, you've
reintroduced the fragility you avoided.

---

## Adapting it

**More than two actions.** Add a timer per action and extend the `ids` array. The
name template already derives its wording from the entity id.

**Different destinations after cancel.** The navigate action is the last item in
`multi-actions`, so point it wherever the user came from.

**No countdown at all** — just a yes/no page — works too: drop the timer, put a
confirm button next to cancel, and call the service directly. You lose the
walk-away safety, and the "tap twice to be sure" pattern is weaker than it looks,
because two taps in the same place is a thing thumbs do accidentally.

**Reuse for anything destructive.** Deleting, unlocking, opening a gate, disarming an
alarm. The pattern doesn't care what the action is — see
[03-camera-grid](03-camera-grid.md) for the door-unlatch variant.
