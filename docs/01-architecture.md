# Architecture

How the pieces fit together, and why it's built this way.

---

## The problem

Home Assistant runs somewhere else — a Pi, a NUC, a VM. It has no visibility of what's
happening *on a Windows PC*: what's playing, what's in your calendar, whether your work
ticket queue is on fire.

There's no integration for most of that. So the PC has to push its own data in.

---

## The shape of it

```
┌─────────────────────────────── Windows PC ───────────────────────────────┐
│                                                                          │
│   Run-OfficeDashboardBridge.ps1        (loop, ~5s)                       │
│            │                                                             │
│            ├── runs collectors on staggered timers                       │
│            │      Get-NowPlaying.ps1        →  NowPlaying.txt            │
│            │      Get-NextMeeting.ps1       →  Meeting.txt               │
│            │      Get-Comms.ps1             →  Comms.txt                 │
│            │      Get-Bitcoin.ps1           →  Bitcoin.txt               │
│            │      Get-TicketSummary.ps1     →  ticket-summary.json       │
│            │      Get-ServiceStatus.ps1     →  ServiceHealth.txt         │
│            │      Get-News.ps1              →  NewsItems.txt             │
│            │                                                             │
│            └── Publish-Sensors.ps1                                       │
│                     reads those files                                    │
│                     POSTs each as a sensor  ──────────┐                  │
└───────────────────────────────────────────────────────┼──────────────────┘
                                                        │ HTTP + bearer token
                                                        ▼
┌────────────────────────── Home Assistant ────────────────────────────────┐
│   POST /api/states/sensor.office_now_playing                             │
│   POST /api/states/sensor.office_next_meeting                            │
│   POST /api/states/sensor.office_support_desk        …etc                │
│                                                                          │
│   These become ordinary entities: history, templates, automations        │
└───────────────────────────────┬──────────────────────────────────────────┘
                                │ WebSocket
                                ▼
┌──────────────────────── Kiosk panel (2560×720) ──────────────────────────┐
│   WebView2 app → /lovelace/dashboard?kiosk                               │
│   One full-screen custom:button-card per view                            │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## Why files in the middle?

The collectors could POST directly. They don't, and that's deliberate:

- **Each collector is independently testable.** Run it, look at the text file, done.
  No HA needed, no token needed.
- **One publisher means one place that knows about HA.** Change your token or move HA
  to a new IP and you edit one script.
- **A failing collector doesn't lose the last good value.** The file still holds what
  it last wrote, so the tile shows stale data rather than blanking.
- **The files are trivially debuggable.** A pipe-delimited line you can open in
  Notepad beats a JSON payload you have to intercept.

The cost is that "the file is stale" and "the value hasn't changed" look identical.
Use file timestamps for liveness, not the value — see
[05-gotchas.md](05-gotchas.md).

---

## Push, not poll

Everything here uses `POST /api/states/<entity_id>`, which **creates entities that
don't exist in any integration**. Consequences worth understanding:

- **They're not persistent config.** They exist because something posted them. They
  survive restarts (HA restores last state) but nothing re-creates them if the source
  is gone forever.
- **HA never fetches.** If the bridge stops, the sensor freezes on its last value
  indefinitely. It won't go `unavailable` and nothing will alert you.
- **`last_updated` only moves when a value changes.** Posting an identical payload is
  a no-op as far as HA's timestamps are concerned.

That last point is the single most misleading thing about this design. Add a heartbeat
sensor that always carries a changing value (a timestamp, an uptime counter) so you
can tell "idle" from "dead" at a glance.

---

## Dashboard layout model

Standard Lovelace stacks cards vertically in columns. That works badly on a 2560×720
ultrawide, where you want a fixed grid that exactly fills the screen with no scrolling.

So each view is **one** `custom:button-card` in panel mode, with a CSS grid inside it:

```yaml
type: custom:button-card
styles:
  grid:
    - grid-template-areas: >
        "nowplaying nav nav power clock"
        "solar security vacuum mower"
    - grid-template-columns: repeat(12, 1fr)
    - grid-template-rows: 72px 10px minmax(0,1fr) 10px minmax(0,1fr)
    - column-gap: 10px
custom_fields:
  solar:
    card:
      type: custom:button-card     # each tile is a nested button-card
```

Every tile is a nested `custom:button-card` placed into a named grid area. It gives
total control, at the cost of one significant trap — see
[05-gotchas.md](05-gotchas.md).

---

## What's deliberately *not* here

- **No add-on, no HACS package, no installer.** This is a reference, not a product.
- **No MQTT.** REST is simpler for one PC pushing a handful of sensors. MQTT is the
  better answer for many sources or many machines.
- **No two-way control.** The bridge only pushes data in. Buttons on the dashboard
  call HA services, which is a separate path.
