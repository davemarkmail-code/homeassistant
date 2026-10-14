# Hardware & integrations

## Core
- **Home Assistant OS** on a generic x86-64 mini PC
- **UPS** monitored with Network UPS Tools (power-cut alert automation)
- Remote access via **Nabu Casa** (Home Assistant Cloud)
- **MQTT** (Mosquitto add-on) carries GivTCP, HASS.Agent and the Nuki lock

## Energy
| Device | Integration |
|---|---|
| GivEnergy hybrid inverter + 2 × 5.2 kWh batteries | GivTCP add-on (local Modbus → MQTT) |
| GivEnergy EV charger | GivTCP (being replaced, see [energy](energy.md)) |
| Octopus Energy tariff | `octopus_energy` (HACS) |
| Axle Energy (virtual power plant events) | `axle_vpp` (HACS) |
| Solar maths | `integration`, `statistics`, `history_stats` helpers |

## Security & doors
- **Nuki smart lock + keypad** over MQTT (rechargeable lock battery)
- **Ring** doorbell and cameras (Reolink swap planned)
- **Google Nest** (heating / cameras)

## Lights, blinds, plugs
- **Govee** LED strips and lamps: `govee_light_local` (LAN control must be enabled in the Govee app)
- **Somfy** blinds: Overkiz
- **TP-Link**, **Tuya** and **SwitchBot** plugs and switches
- **Matter / Thread** devices

## Media
- Amazon **Echo** devices: core *Alexa Devices* (lists) + *Alexa Media Player* (HACS; timers, TTS, playback)
- **Sonos**, **KEF** speakers (`kef_connector`), **Onkyo** amp
- **LG webOS** TV, **Android TV**, **Chromecast**, **Xbox**
- Radio Browser

## Appliances & robots
- **Roborock** vacuum/mop (room scripts, map on the Office dashboard)
- **Segway Navimow** robot mower (`navimow`, HACS)
- **Blueair** purifier (`ha_blueair`, HACS)
- **Brother** printer (toner/status)
- IR air-con via a smart remote (assumed state)

## Computers & phones
- **HASS.Agent** on the office Windows PC: mic/webcam-in-use sensors, PC on/off, a custom PowerShell sensor ([D&D mode](presence-and-dnd.md#dd-mode-roll20))
- **HA Companion app** on iPhones and iPads (presence, notifications, battery level for charge control)
- **iBeacon** tracking for a child's presence

## Calendars
- **ICS Calendar** (HACS) for read-only feeds (work, tutoring)
- **CalDAV** / Google for the family diary
- **Local To-do** lists (16 of them) for chores, plus the Alexa shopping list

## DIY
- **ESPHome**: the [study door sign](study-door-sign.md) (Waveshare ESP32-S3 4" touch LCD)
- **NeonPlan 3D** (HACS): a 3D floor plan of the house with solar panels on the roof (work in progress)

## Monitoring
- Ping, Speedtest.net (hourly), System Monitor, Met.no weather, Sun, Time & Date
