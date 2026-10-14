# Automations

| Area | Automation | What it does |
|---|---|---|
| Office | Heating boost: return to 17 °C | Ends the 1 h boost |
| Office | PC restart / shutdown after countdown | Countdown buttons for the PC |
| Office | PC boot → Office ON | HASS.Agent comes online → toggle office power on |
| Office | PC shutdown → Office OFF | HASS.Agent unavailable for 30 s → toggle office off |
| Office | Front door ding alert | Doorbell pop-up on the office screen |
| Office | Camera rotation | Cycles camera tiles |
| Energy | EV charger watchdog | Reconnects a stale charger (guarded around VPP events) |
| Energy | VPP pre-event pre-charge / post-event Eco / day-charge reset / nightly Eco backstop / self-dispatch fallback / event capture | See [energy](energy.md) |
| Energy | Capture yesterday's solar figures | End-of-day snapshot |
| Energy | Solar dashboard chart follows the period | Chart range matches the selected period |
| Energy | Battery data watchdog / Solar health alert / Charge schedule auto-fix | See [energy](energy.md) |
| House | UPS power cut alert | Push notification |
| House | Ring watchdog | Reload if events go stale |
| House | Blind press reset | Resets momentary blind buttons |
| House | Christmas mode on / off | Seasonal theme on the panels |
| Kids | Projector / bedroom light off at 8:30 | School-morning tidy-up |
| Kids | Count jobs done / weekly payout reminder | Pocket money |
| Kids | Presence from beacon | iBeacon → home/away |
| Panels | Day mode / night mode / screen off / keep asleep / monitor off & on | Screen schedule (dark theme all day) |
| Panels | iPad battery care (×2) | Hold the battery at 65–80% |
| Panels | Doorbell pop-up (Kitchen + Family) | 30 s live camera |
| Panels | Close day pop-up / seasonal preview off / back to glance mode | Auto-tidy the screens |
| Jobs | Daily / weekly / fortnightly / monthly top-up | Recurring chores |
| Jobs | Add when text entered / stamp when ticked / reschedule / clear an hour after | List housekeeping |
| Jobs | Alexa job lists → family jobs | Voice-added jobs |
| Jobs | Jobs nudge | 10:00 / 16:30 push |
| Food | Dinners weekly rollover | Menu moves on a week |
| Presence | DND during meetings and calls / DND auto off / status reset | See [presence](presence-and-dnd.md) |
| Presence | Roll20 open → D&D mode | "Adventuring" status |
| Security | Front door lock battery alert | See [maintenance](maintenance.md) |
| System | Nightly auto updates (after a backup) | See [maintenance](maintenance.md) |
