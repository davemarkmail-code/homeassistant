# 20 — Weather tile

Temperature, conditions and a feels-like reading, without looking like every other
weather card.

```
┌────────────────────────────────┐
│ ⛅ Weather                     │
│ ────────────────────────────── │
│      ☁       24°               │
│              Partly cloudy     │
│         feels 24° · 33% humid  │
└────────────────────────────────┘
```

**Needs:** a `weather` entity — the Met Office, Open-Meteo and AccuWeather
integrations all work. No bridge.

---

## The tile

```yaml
type: custom:button-card
entity: weather.home
show_name: false
show_icon: false
triggers_update:
  - weather.home
tap_action:
  action: more-info
custom_fields:
  c: |
    [[[
      const w = states['weather.home'];
      const a = w.attributes;

      const icons = {
        'clear-night':'mdi:weather-night', 'cloudy':'mdi:weather-cloudy',
        'fog':'mdi:weather-fog', 'hail':'mdi:weather-hail',
        'lightning':'mdi:weather-lightning', 'lightning-rainy':'mdi:weather-lightning-rainy',
        'partlycloudy':'mdi:weather-partly-cloudy', 'pouring':'mdi:weather-pouring',
        'rainy':'mdi:weather-rainy', 'snowy':'mdi:weather-snowy',
        'snowy-rainy':'mdi:weather-snowy-rainy', 'sunny':'mdi:weather-sunny',
        'windy':'mdi:weather-windy', 'exceptional':'mdi:alert-circle-outline'
      };

      const label = w.state.replace(/-/g,' ')
                     .replace(/\b\w/g, c => c.toUpperCase());

      return `<div style="display:flex;align-items:center;gap:16px">
        <ha-icon icon="${icons[w.state] || 'mdi:weather-cloudy'}"
                 style="--mdc-icon-size:56px;color:#e0a63c"></ha-icon>
        <div>
          <div style="font-size:38px;font-weight:700;line-height:1">
            ${Math.round(a.temperature)}°</div>
          <div style="font-size:14px;opacity:.7;margin-top:2px">${label}</div>
          <div style="font-size:11px;opacity:.45;margin-top:4px">
            feels ${Math.round(a.apparent_temperature ?? a.temperature)}° ·
            ${a.humidity}% humidity</div>
        </div>
      </div>`;
    ]]]
```

### Map the state yourself

HA weather states are a **fixed vocabulary** — `sunny`, `partlycloudy`, `rainy`,
`pouring`, and so on. Mapping them to your own icons is the single thing that stops a
weather tile looking generic, and it takes one object literal.

The same map lets you pick colours per condition if you want — amber for sun, grey
for cloud, blue for rain.

---

## A forecast row, if you want one

Forecasts moved to a **service call** in HA 2024.4 — they're no longer an attribute,
which trips up a lot of older examples:

```yaml
service: weather.get_forecasts
data: { type: daily }
target: { entity_id: weather.home }
```

That means a button-card template **cannot fetch a forecast directly** — templates
read state, they can't call services. Options:

- Use HA's built-in `weather-forecast` card alongside your tile
- Run an automation that calls the service and writes results into an
  `input_text` or a template sensor you can then read

For a glanceable dashboard, current conditions are usually enough. Reach for the
built-in card when you want the week ahead.

---

## Gotchas

**`apparent_temperature` isn't universal.** Some integrations provide it, some don't.
Use `??` to fall back to `temperature` rather than rendering "feels undefined°".

**Round for display.** Weather integrations report to one or two decimals and
`23.870000000000001` shows up more often than you'd like.

**Check your units.** `temperature_unit` is in the entity attributes. If you have
sensors from mixed sources, don't assume everything is °C.

**Sun-based conditions need location.** `clear-night` vs `sunny` depends on HA knowing
your latitude and longitude — if your tile shows sun at midnight, that's the cause.
