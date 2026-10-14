# Examples

Working pieces from our setup, anonymised. Rename the entity IDs to match yours.

| File | What it is |
|---|---|
| `automations/nightly_auto_updates.yaml` | 03:00: backup first, stop if it failed, then add-on/HACS updates, then one Core (skip x.0) / OS restart |
| `automations/front_door_lock_battery_alert.yaml` | Lock low / critical / keypad / offline push + 18:00 daily reminder |
| `automations/auto_dnd_meetings.yaml` | Auto Do-Not-Disturb during calls and meetings; never clears a manual DND |
| `automations/roll20_dnd_mode.yaml` | "Adventuring" status while a Roll20 tab is open |
| `automations/office_pc_shutdown_office_off.yaml` | PC turns off → office turns off |
| `automations/jobs_nudge.yaml` | 10:00 / 16:30 push of each parent's unfinished jobs, tap opens their list |
| `automations/kitchen_ipad_battery_care.yaml` | Keep a wall iPad at 65-80% with a smart plug, with safety nets |
| `templates/presence.yaml` | Call / webcam / D&D / PC-on template sensors |
| `templates/battery_last_reading.yaml` | Detects a GivEnergy battery whose data has frozen |
| `pc/roll20.ps1` | HASS.Agent PowerShell sensor: Roll20 open in any browser tab |
| `esphome/study-sign-outline.yaml` | Outline of the ESPHome door sign config (key parts only) |
| `tools/lovelace_edit.mjs` | Node script: exact find/replace in a dashboard over the HA websocket, with a backup |

`lovelace_edit.mjs` runs inside an HA add-on with `SUPERVISOR_TOKEN` set:
`node lovelace_edit.mjs <dashboard-url-path> '<exact old text>' '<new text>' backup.json`
