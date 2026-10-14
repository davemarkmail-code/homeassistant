# Kitchen dashboard (iPad)

A 10th-gen iPad in landscape, running the HA Companion app in kiosk mode on a 1228×820 canvas.

![Kitchen dashboard](../HA%20Dashboard%20Pics/Kitchen.jpg)

## Full view
- **Today** timeline across the top (same colours as the [Family](family.md) panel)
- **Jobs:** Dad / Mum / our son, plus the **Alexa shopping list** as a 4th column. Tap to tick.
- **Menu this week:** short day names (Mon…Thur…Sun) with the planned dinners. Rolls over weekly by automation.
- **AirFryer conversion:** oven temperature/time → air fryer, in its own box
- **Timers:** live Alexa timers from the kitchen Echo, plus quick buttons (1m/5m/10m/30m/1h) that add up before sending
- **Speaker:** now playing, with play/pause that copes with Alexa's live-radio quirks
- **Look something up:** a real text box that opens a Google search

## Glance mode
After 30 minutes the iPad switches to **4 big tiles**: time/weather/energy, who's home, next event, tonight's dinner. When timers run, the dinner tile splits to show up to 3 timers with cancel buttons. Tap anywhere for the full view.

## Battery care
The iPad's charger is a smart plug. An automation holds the battery between 65–80% using the Companion app's battery sensor, so a wall-mounted iPad doesn't cook its battery.

## Notes
- The Alexa timer sensor only updates on a poll (push events never arrived), so new timers take about a minute to appear
- Search can't be embedded (cookie consent can't persist), so it opens in a new window
