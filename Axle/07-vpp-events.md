# 07 — VPP export events

Documenting how a Virtual Power Plant (VPP) export event is handled end to end: the
automations that act on it, and the helpers that record **what actually happened**
so a dashboard can report outcomes rather than guesses.

The examples use an [Axle](https://www.axle.energy/) integration feeding a GivEnergy
inverter via [GivTCP](https://github.com/britkat1980/giv_tcp). The pattern applies to
any provider that publishes an event window as sensors — Octopus saving sessions,
for instance.

> Replace `aa1111b222` with your own GivTCP inverter serial throughout.

---

## The problem worth solving

A VPP event is a short, paid window — typically 30 to 120 minutes — during which you
export battery energy to the grid at a rate far above your normal export tariff. Ours
pays roughly £1/kWh against a standard export rate of about 12p.

Two things follow from that:

1. **It matters that the automations worked.** A missed event is real money.
2. **You cannot tell whether they worked by looking at whether they ran.**

The second point is the whole reason this document exists. An automation that fires and
then correctly does nothing looks identical, from the outside, to one that fired and
worked. Home Assistant gives you `last_triggered` and nothing else.

---

## Automation roles

Five automations cover the event lifecycle. Names are ours; the roles generalise.

| Automation | Fires | Purpose |
|---|---|---|
| `axle_6b_pre_event_export_pre_charge_same_day` | ~1h before window | Fill the battery from the grid if it is too low to sustain the export |
| `axle_self_dispatch_if_their_command_does_not_land` | At window start | Force export locally if the provider's own command never reached the inverter |
| `axle_6a_post_event_ensure_eco` | At window end | Return the inverter to Eco mode |
| `axle_6b_reset_day_charge_slot` | At window end | Clear the charge slot the pre-charge step armed |
| `axle_nightly_eco_backstop` | 23:30 daily | Catch-all: if anything left the inverter out of Eco, fix it |

### Why a self-dispatch step exists

The provider issues its command over the internet to the inverter's cloud. If the
inverter is polled locally and its cloud connection is unreliable, that command can
silently fail. Self-dispatch watches the window opening and, if the inverter has not
entered export, issues `force_export` locally.

On our system this fires on **every** event, which tells us the provider's command
never lands. That is useful information in itself — the day it stops firing, something
upstream has changed.

---

## Capture automation

This is the piece that makes outcomes knowable. It records the state of things before
and after, so the deltas can be attributed to the event.

```yaml
alias: VPP event capture
description: Measures pre-charge, export, battery and eco outcome around VPP events
mode: queued
max: 8

triggers:
  # Snapshot battery charge the moment the pre-charge automation fires
  - trigger: event
    event_type: automation_triggered
    event_data:
      entity_id: automation.axle_6b_pre_event_export_pre_charge_same_day
    id: pre

  - trigger: state
    entity_id: sensor.axle_vpp_axle_event_in_progress
    from: "off"
    to: "on"
    id: start

  - trigger: state
    entity_id: sensor.axle_vpp_axle_event_in_progress
    from: "on"
    to: "off"
    id: end

conditions: []

actions:
  - choose:

      # ---------------------------------------------------------- pre-charge
      - conditions:
          - condition: trigger
            id: pre
        sequence:
          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_charge_at_pre
            data:
              value: >
                {{ states('sensor.givtcp_aa1111b222_battery_charge_energy_today_kwh')
                   | float(0) }}

      # ---------------------------------------------------------- window opens
      - conditions:
          - condition: trigger
            id: start
        sequence:
          # How much did the pre-charge step actually put in?
          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_last_precharge
            data:
              value: >
                {{ [ (states('sensor.givtcp_aa1111b222_battery_charge_energy_today_kwh')
                      | float(0))
                     - (states('input_number.axle_charge_at_pre') | float(0)),
                     0 ] | max | round(2) }}

          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_export_at_start
            data:
              value: >
                {{ states('sensor.givtcp_aa1111b222_export_energy_today_kwh')
                   | float(0) }}

          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_battery_at_start
            data:
              value: >
                {{ states('sensor.givtcp_aa1111b222_battery_discharge_energy_today_kwh')
                   | float(0) }}

          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_mode_at_start
            data:
              value: "{{ states('select.givtcp_aa1111b222_mode') }}"

          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_eco_result
            data:
              value: Pending

          # Stamp the window NOW, while the sensors still hold it. See Gotchas.
          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_last_event_when
            data:
              value: >
                {% set st = as_datetime(states('sensor.axle_vpp_axle_start_time')) %}
                {% set en = as_datetime(states('sensor.axle_vpp_axle_end_time')) %}
                {% if st and en %}
                {{ (st | as_local).strftime('%a %d %b %H:%M') }} –
                {{ (en | as_local).strftime('%H:%M') }}
                {% else %}
                {{ now().strftime('%a %d %b %H:%M') }}
                {% endif %}

          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_last_event_actions
            data:
              value: Event running

      # ---------------------------------------------------------- window closes
      - conditions:
          - condition: trigger
            id: end
        sequence:
          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_last_export
            data:
              value: >
                {{ [ (states('sensor.givtcp_aa1111b222_export_energy_today_kwh')
                      | float(0))
                     - (states('input_number.axle_export_at_start') | float(0)),
                     0 ] | max | round(2) }}

          - action: input_number.set_value
            continue_on_error: true
            target:
              entity_id: input_number.axle_last_battery
            data:
              value: >
                {{ [ (states('sensor.givtcp_aa1111b222_battery_discharge_energy_today_kwh')
                      | float(0))
                     - (states('input_number.axle_battery_at_start') | float(0)),
                     0 ] | max | round(2) }}

          # Which VPP automations fired anywhere near this event
          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_last_event_actions
            data:
              value: >
                {% set ns = namespace(a=[]) %}
                {% for e in states.automation %}
                  {% if 'axle' in e.entity_id and 'event_capture' not in e.entity_id %}
                    {% set lt = e.attributes.last_triggered %}
                    {% if lt and (as_timestamp(now()) - as_timestamp(lt, 0)) < 21600 %}
                      {% set ns.a = ns.a + [ e.entity_id.split('.')[1]
                         | replace('axle_','') | replace('_',' ') ] %}
                    {% endif %}
                  {% endif %}
                {% endfor %}
                {{ ns.a | join(', ') if ns.a | count > 0 else 'none fired' }}

          # Give the eco-restore automation time to act, then judge the result
          - delay:
              seconds: 90

          - action: input_text.set_value
            continue_on_error: true
            target:
              entity_id: input_text.axle_eco_result
            data:
              value: >
                {% set before = states('input_text.axle_mode_at_start') %}
                {% set now_mode = states('select.givtcp_aa1111b222_mode') %}
                {% if now_mode != 'Eco' %}Not eco
                {% elif before == 'Eco' %}Confirmed
                {% else %}Changed
                {% endif %}
```

---

## Helpers

Create these before the automation. All are plain UI helpers.

| Helper | Type | Holds |
|---|---|---|
| `input_number.axle_charge_at_pre` | number, kWh | Battery charge counter when pre-charge fired |
| `input_number.axle_last_precharge` | number, kWh | Energy the pre-charge step actually added |
| `input_number.axle_export_at_start` | number, kWh | Export counter at window open |
| `input_number.axle_battery_at_start` | number, kWh | Battery discharge counter at window open |
| `input_number.axle_last_export` | number, kWh | Exported during the last event |
| `input_number.axle_last_battery` | number, kWh | Discharged from battery during the last event |
| `input_number.axle_event_rate` | number, £/kWh | Assumed event rate, default 1.00 |
| `input_text.axle_last_event_when` | text | Window as displayed, e.g. `Sun 17 Aug 06:00 – 07:00` |
| `input_text.axle_last_event_actions` | text | Comma list of automations that fired |
| `input_text.axle_mode_at_start` | text | Inverter mode at window open |
| `input_text.axle_eco_result` | text | `Confirmed` / `Changed` / `Not eco` / `Pending` |

---

## Reading an outcome from each automation

This is the part worth copying. Each row on the dashboard answers a different question,
and each is measured differently.

**Pre-charge — measured.** Snapshot the battery charge counter when the automation
fires, take the delta at window open. Above a small threshold (0.2 kWh) it genuinely
charged; below, it ran and decided nothing was needed. Both are correct outcomes and
they look completely different to the eye.

**Eco restore — measured indirectly.** Record the inverter mode at window open, compare
90 seconds after close. Not Eco → failed. Was Eco and still is → nothing to do. Was not
and now is → it acted. This deliberately avoids depending on the automation reporting
anything about itself.

**Reset charge slot — verified from state.** The automation writes known values, so read
the slot back instead of trusting the write. Start and end both `00:00:00` means cleared;
anything else means the slot is still armed, which is exactly the failure you want to see.

**Self dispatch — inferred from the trigger.** Still the weakest of the five. It fires,
but nothing recorded proves a command was issued. Checking whether `force_export`
transitions to `Running` shortly after window open would close this gap.

**Nightly backstop — ran-today check.** Compare `last_triggered`'s date to today. It sits
outside any event window, so an event-relative lookback would always report it missing.

---

## Gotchas

**The window sensors clear when the event ends.** `start_time` and `end_time` go to
`unknown` the moment the window closes. Formatting them in the end branch throws, and
in a sequence without `continue_on_error` that silently abandons every remaining step —
the numbers update and the text fields do not. Stamp the window text at **window open**.

**`last_triggered` only stamps if conditions pass.** An automation whose top-level
conditions abort never records a trigger, so it is indistinguishable from one that never
ran. Do not build a status column on `last_triggered` alone.

**Daily energy counters reset at midnight.** Every delta here is against a
`*_energy_today_kwh` sensor. An event spanning midnight produces a negative delta —
clamped to zero with `[ x, 0 ] | max`, which fails safe but under-reports. Events in the
small hours are the risk case.

**Export is not the same as event contribution.** Exported kWh includes any solar
generating during the window. On a sunny afternoon event the export figure flatters the
result; the battery discharge figure is what the event actually cost you. Show both.

**The 90-second eco delay is a heuristic.** If the restore automation takes longer to
settle the mode, the result reads `Not eco` incorrectly. Verify against a real event
before trusting it.

**Inverter counters can stall.** During one two-hour event our load counter reported a
0.00 kWh delta, which is not physically possible. If the local polling drops out
mid-event, every delta here is suspect. Treat a suspiciously round zero as a data fault,
not a measurement.

---

## Related

- `lovelace/patterns/22-vpp-event-tile.md` — the dashboard tile that renders all of this
- `docs/04-*` — GivTCP entity naming and mode selects
