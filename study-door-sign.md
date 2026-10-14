# Study door sign (ESPHome)

A 4" colour touch screen next to the study door that tells the family whether it's OK to come in.

![Study door sign](HA%20Dashboard%20Pics/OfficePannel.jpg)

## Hardware
- **Waveshare ESP32-S3-Touch-LCD-4** (480×480, ST7701 RGB panel, GT911 touch, buzzer, USB-C). Ours is a **Rev 4** board, which uses a CH32V003 IO expander (I2C 0x24) rather than the older TCA9554.
- A 3D-printed UK single-gang wall box mount
- Always on: USB power through the wall (no deep sleep, so updates are instant)

## ESPHome bits that mattered
- Expander: official `waveshare_io_ch32v003` component
- Display: `mipi_rgb`, model `WAVESHARE-4-480X480`, **16-bit pixel mode** (18-bit gave pinkish whites), reset on expander pin 3
- Touch: `gt911` with reset/interrupt on expander pins 1/2
- The SPI id can't be `spi0` (it's reserved), so call it something else
- UI: **LVGL** with two pages, `status_page` and `idle_page`
- `subscribe_logs` on the HA side means device logs show up in the HA log

## Screens
| State | Screen |
|---|---|
| On a call, auto DND (meeting) or manual DND | Red, **ON A CALL** (one screen for all three, so it's less confusing) |
| D&D session | Purple, **ADVENTURING** |
| Fun status | Coffee please ☕ / Come in, I'm lonely 🥺 / Snacks 🍪 |
| Free | Green, face photo in a circle with a status badge |
| Idle (Dad out or PC off) | Dim big clock, date, weather icon + temperature, "Dad's home/out" |
| HA disconnected | A jokey ghost: "Dad not in here (probably)". It never shows a false "free". |

- Status page also shows the time and date (top right)
- Night: 4% brightness; touch wakes it for 30 s
- Inputs from HA: `binary_sensor.dad_on_a_call`, `input_boolean.dad_do_not_disturb`, `input_select.dad_status`, `person.dad`

## Date label in LVGL lambda
```cpp
// e.g. "Tue 13 Oct" (no leading zero)
if (t.is_valid()) {
  std::string dd = t.strftime("%d"); if (dd[0] == '0') dd = dd.substr(1);
  lv_label_set_text(id(sb_date), (t.strftime("%a ") + dd + t.strftime(" %b")).c_str());
}
```

## Ideas list
Knock button (buzzer + pop-up on the office screen with reply buttons), doorbell alert, an idle "filler" with solar and who's home.
