# Office dashboard (Corsair Xeneon Edge, 2560×720)

A 14.5" touch strip under the main monitor. It's built for **glancing**, so it only shows what actually gets used.

![Office dashboard](../HA%20Dashboard%20Pics/Office.jpg)

## Columns (left → right)
1. **Status:** time, weather, crypto price, PC CPU/RAM (green/amber/red), internet health (speed, ping), HA updates count. Problem-only rows: front door (with lock and keypad battery icons), comms UPS on battery, PC link offline.
2. **Who's home:** faces with status rings
3. **Energy flow:** animated power-flow graphic (Solar & Grid → Home → Battery & Car). Fixed colours per source; only the numbers change colour for status. Inverter mode bar and virtual-power-plant next/last event with an earnings tally.
4. **Jobs:** Dad's jobs (tap to tick) and "Coming up" family events
5. **Controls:** desk light, clock, purifier, air-con, "office all", 1 h heating boost, Do Not Disturb, fun statuses (Coffee please / Come in, I'm lonely), sign sleep, front door Lock / Unlock (double-tap)

Sub-views: **Solar** history, **Cameras** (front, drive, garden), **Cleaning** (robot vacuum map + per-room clean/mop).

## Style
Dark glass: strong but dim background glows, frosted cards with `backdrop-filter: blur(34px) saturate(190%)`, a radial top sheen (`::before`) and a thin gradient rim (`::after` with a mask).

## PC power follows the office
- PC boots → HASS.Agent comes online → automation presses the "Office ON" button (lights, screen, etc.)
- PC shuts down → HASS.Agent sensors go unavailable for 30 s → automation presses the same toggle to turn the office OFF
