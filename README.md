# A whole-house Home Assistant control system

> ## Code and documentation aided by Claude and Cowork
>
> The dashboards, scripts, card templates and the booklet itself were produced
> with substantial help from **Claude** and **Cowork** (Anthropic), working
> interactively over several months. Code was generated, debugged and refined by
> AI; everything here was then tested in a real house on real hardware.
>
> **What that means for you:** the patterns work — they run here daily, and every
> fault described is one I actually hit. But this is not hand-written reference
> code that has been through review. **Test anything you copy.**
>
> The architecture, hardware choices, design language and every judgement about
> what was worth building are mine.

Build notes and working code for a Home Assistant setup spanning three control
surfaces — a widescreen desk kiosk, a wall-mounted tablet and a phone — driving
lighting, blinds, heating, multi-room audio, a full AV stack, solar and battery
storage, an EV charger, cameras, a door lock and a UPS.

**Read it here: [BOOKLET.md](BOOKLET.md)** — the full write-up, rendered in the
browser with screenshots of all three dashboards and a **tagged kit list** so you
can tell at a glance whether any of this applies to your hardware.

**Or download the PDF:** [`ha-control-system-booklet.pdf`](ha-control-system-booklet.pdf)
(29 pages, same content, better for reading offline or printing).

---

## What's here

```
ha-control-system-booklet.pdf     the booklet - read this first
images/                           screenshots referenced by the docs
app/XEON-DashboardV3/             full source of the Windows kiosk host app
code/
  01-activity-scripts.yaml            parallel-branch AV activity scripts
  02-battery-and-audio-scripts.yaml   battery modes + multi-room audio zones
  03-automations-and-watchdogs.yaml   alerts and stale-integration detection
  04-tile-design-system.yaml          button-card templates and tile logic
  05-styling-inside-shadow-dom.md     slider colours, progress rings, pitfalls
  06-page-rotation-controller.js      kiosk page rotation + countdown ring
  07-energy-flow-card.md              energy flow card and the arithmetic trap
  08-kiosk-host-app.md                the Windows kiosk host application
```

### The kiosk host app

`app/XEON-DashboardV3/` is the complete source of the Windows application that
drives the widescreen panel — a WPF/.NET 8 host wrapping WebView2, with a tray
icon, a monitor picker and a self-contained publish. Appendix A of the booklet
explains it.

**Build it yourself; do not trust a binary from a stranger.**

```powershell
dotnet restore
dotnet build -c Debug
dotnet run --project src/XeonDashboard/XeonDashboard.csproj
```

Copy `Docs/settings.sample.json` to `settings.json` and set `DashboardUrl`. Set
`MonitorName` to an empty string to use the primary display.

Excluded deliberately: `bin`, `obj` and `publish`. That last one contains the
WebView2 browser profile, which holds a Home Assistant session cookie — never
share it.

It is unsigned, so Windows SmartScreen will warn. It was written almost entirely
by AI to my specification, has no test suite, and has been reviewed by nobody
but me. It has however run 24 hours a day for months.

---

## Is this relevant to you?

Chapter two lists every piece of kit with how it is integrated. In brief:
**GivEnergy** solar and battery (local Modbus via GivTCP), **Octopus Energy**,
**Axle Energy**, **LG** OLED television, **Onkyo** amplifier, **Sonos**,
**Xbox**, **Fire TV**, **SwitchBot** IR, **Tuya**, **Govee**, **Somfy** via
**Overkiz**, **TP-Link Tapo**, **Google Nest**, **BlueAir**, **Roborock**,
**Segway Navimow**, **Ring**, **Nuki** over MQTT, **Reolink** (planned), and an
**Eaton** UPS via NUT.

If your brands differ the patterns still hold — only the entity IDs change.

## Requirements

- **Home Assistant** (any recent version; storage-mode dashboards)
- **custom:button-card** (HACS) — essential, nearly everything visual uses it
- **card-mod** (HACS) — for styling Home Assistant's own cards
- Optionally **custom-brand-icons** (HACS) for device logos

---

## Before you copy anything

Every entity ID in the code folder is a **placeholder**. Substitute your own —
pasting mine in will achieve nothing. The quickest way to find what you have is
Developer Tools → Template:

```jinja
{{ states.sensor | selectattr('entity_id','search','inverter')
                 | map(attribute='entity_id') | list }}
```

And for an amplifier's exact source strings, which are always peculiar:

```jinja
{{ state_attr('media_player.your_receiver','source_list') }}
```

---

## The short version, if you read nothing else

**Colour tiles from the thing that physically knows the answer.** Not from a
helper you set earlier. An amplifier cannot be wrong about its own input.

**Trust measurements, not state words.** My EV charger reports "Charging"
whenever a cable is attached. A card that believed it told me my house used no
electricity.

**Check timestamps before values.** Most faults here announced themselves the
moment I looked at `last_changed`. Stale data looks exactly like real data.

**Pause audio before anything else.** Anything that can play silently will
eventually play loudly at 3am. Mine did.

**`@property` does not work inside shadow DOM.** It is document-scoped and fails
silently. Use SVG stroke animation for progress rings.

**Prefer local control.** Faster, and it survives the manufacturer going out of
business — which mine did.

---

## Licence

Take it, adapt it, publish your own version. No attribution needed.

If one of these patterns fails on your hardware, please say so publicly — half
the value here is the documented faults, and they only exist because things
misbehaved in a real house.
