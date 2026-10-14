# Our Home Assistant setup

A family smart home built on Home Assistant, focused on **glanceable wall dashboards**, **solar + battery energy**, **family jobs and routines** and a **"don't come in, I'm on a call" door sign** for the home office.

> Everything here is shared as ideas and patterns. Entity IDs have been genericised and anything personal (names, addresses, serials, tokens, calendar URLs) has been removed.

## Screenshots

| Family | Kitchen |
|---|---|
| ![Family](HA%20Dashboard%20Pics/Family.jpg) | ![Kitchen](HA%20Dashboard%20Pics/Kitchen.jpg) |
| **Office** | **Lounge** |
| ![Office](HA%20Dashboard%20Pics/Office.jpg) | ![Lounge](HA%20Dashboard%20Pics/Lounge.jpg) |
| **Study door sign** | |
| ![Study door sign](HA%20Dashboard%20Pics/OfficePannel.jpg) | |

## At a glance

| Area | What it does |
|---|---|
| [Hardware & integrations](integrations.md) | What's in the house and how it talks to HA |
| [Family dashboard](dashboards/family.md) | Wall panel: today timeline, week view, jobs, who's home, energy |
| [Kitchen dashboard](dashboards/kitchen.md) | iPad: jobs, shopping list, menu, AirFryer conversions, Alexa timers, glance mode |
| [Office dashboard](dashboards/office.md) | 2560×720 touch strip under the monitor: energy flow, status, controls |
| [Lounge tablet](dashboards/lounge-tablet.md) | Big-tile lighting, blinds, weather and clock |
| [Family ToDo](dashboards/family-todo.md) | Admin pages: add jobs, plan dinners, repeating schedules |
| [Mobile dashboards](dashboards/mobile.md) | Phone home buttons, energy page, points keypad |
| [Study door sign](study-door-sign.md) | ESPHome 4" touch LCD showing call / DND / status |
| [Presence, calls & DND](presence-and-dnd.md) | Mic/webcam call detection, auto Do-Not-Disturb, fun statuses, D&D mode |
| [Energy](energy.md) | Solar, battery, EV, virtual power plant events, watchdogs |
| [Family jobs & pocket money](family-jobs.md) | Recurring chores, voice lists, nudges, points-to-LEGO thermometer |
| [Maintenance & alerts](maintenance.md) | Nightly backup-then-update, lock battery alerts, UPS, health checks |
| [Automations list](automations.md) | Every automation, one line each |
| [Lessons learned](lessons-learned.md) | Gotchas that cost us time, so they don't cost you any |
| [Examples](examples/README.md) | Anonymised automations, templates, the PC script, the ESPHome outline |

Also: a **Family Dev** copy of the Family panel for trying changes, and the built-in **Map**.

## Design principles

- **Dark only.** We tried day/evening/night themes; everyone preferred dark, so it's dark all the time.
- **Glanceable first.** Show state only; show detail only when something is wrong.
- **Few colours, each with one meaning.** Green = family diary, orange = Dad's work, purple = Mum's work, blue = happening now / accents, amber = warning, red = problem.
- **Frosted "glass" panels** over soft, subtle background glows. Not bright.
- **Big touch targets**, and double-tap for anything risky (e.g. unlocking the front door).
- **Never show a false "free".** If the sign loses Home Assistant, it says so rather than guessing.

## Stack

- Home Assistant OS on a small x86 PC, with a UPS
- Add-ons: Mosquitto (MQTT), Matter Server, ESPHome Device Builder, GivTCP, Network UPS Tools, Samba, Studio Code Server
- HACS frontend: `button-card`, `card-mod`, `apexcharts-card`, `plotly-graph-card`, `kiosk-mode`, `custom-brand-icons`
- Most dashboards are a **single `custom:button-card` with hand-written HTML/CSS/JS**, laid out on a fixed canvas and scaled to fit the screen. More on this in [dashboards](dashboards/family.md#how-its-built).

## Licence

Docs and snippets: MIT. Use them however you like.
