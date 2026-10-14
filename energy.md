# Energy

## Kit
- GivEnergy hybrid inverter (Gen 3), 2 × 5.2 kWh batteries in the loft, rear-roof solar array (2 × 7 panels)
- **GivTCP** add-on for fully local control (important now that the vendor is gone)
- Octopus tariff with a cheap overnight window (23:30–05:30)
- **Axle Energy** virtual power plant: paid export events

## Charge schedule
- Slot 1 is **23:31–05:29**, a minute inside the cheap window on each side, with a target SOC of 100%
- **Auto-fix:** at 23:15, if "Enable charge schedule" has been switched off (it happened after a battery power-cycle), turn it back on and send a notification

## Virtual power plant (Axle) automations
- Capture each event: export kWh × rate, added to an "unclaimed £" helper shown on the Office dashboard (the API has no earnings data, so we estimate)
- Pre-event export pre-charge, post-event return to Eco, a nightly Eco backstop, and a "self dispatch if their command doesn't land" fallback
- **Rule:** never restart GivTCP or reload MQTT within 20 minutes before, during, or 10 minutes after an event. Pennies make pounds.

## Health monitoring
GivEnergy batteries can **hang**: one module's data froze at 39% and the inverter forced the discharge rate to 0, so the house ran on the grid while showing a "full" battery. Power-cycling the module fixed it.

`automation.solar_health_alert` sends a phone notification with the fix for each case:
- **stuck:** a battery module's last *real* value change is more than 1 h old (trigger-template sensors record the last change)
- **blocked:** discharge rate under 100 W for 15 min outside the charge slot and outside VPP events
- **offline:** GivTCP not online, or no update for 30 min

Plus a watchdog that restarts GivTCP if battery voltage stops updating while grid data still flows (at most once an hour).

## EV
The GivEnergy charger is flaky over Modbus. The plan is to swap it for an Ohme (Intelligent Octopus Go, core HA integration) and repoint the dashboard "Car" chip.
