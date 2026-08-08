# Kiosk setup

Running the dashboard full-screen on a dedicated panel.

---

## Hiding Home Assistant's chrome

Install [kiosk-mode](https://github.com/NemesisRE/kiosk-mode) via HACS, then either add
`?kiosk` to the URL, or set it per-dashboard:

```yaml
kiosk_mode:
  hide_header: true
  hide_sidebar: true
```

**Consider leaving the sidebar on for touch dashboards you interact with**, otherwise
there's no way back out without a keyboard. A wall panel that only ever displays is a
different case — hide everything.

### Design for the space you actually get

With the header hidden you get the full panel height. With it visible you lose ~56px.
If you build against a browser window that has HA's header and then deploy to a kiosk
that doesn't, every `1fr` row will be taller than you designed for.

Pick one and be consistent. For a 2560×720 panel with everything hidden, the usable
area is the full 2560×720.

---

## The browser

Any of these work:

- **Fullscreen Chrome/Edge** — `--kiosk --app=http://homeassistant.local:8123/lovelace/dash?kiosk`
- **A WebView2 wrapper** — a small WPF/.NET app hosting WebView2, which is what I use
- **Fully Kiosk Browser** — if the panel is Android

A wrapper is worth it if you want: launch on a specific monitor by name, a delay so it
waits for the network, auto-reconnect, and a hidden cursor. Otherwise Chrome in kiosk
mode is fine.

Whatever you use, remember **the URL determines the landing view**:

```
http://ha.local:8123/my-dashboard?kiosk            → first view in the config
http://ha.local:8123/my-dashboard/home?kiosk       → the 'home' view
```

Easy to forget, and you end up staring at the wrong page every boot wondering why.

---

## Rendering notes

**Rendering is entirely client-side.** Home Assistant serves the assets and pushes
state over WebSocket; your panel machine does all the layout and compositing. Heavy CSS
— `backdrop-filter` included — costs the HA server nothing.

So a modest HA box with a decent kiosk PC is fine. 2560×720 is only 1.84M pixels, fewer
than 1080p.

**The exception is history-backed cards.** Anything charting from the recorder database
(ApexCharts, Plotly) is bound by HA's database, not your panel. A view with several of
those can take **90–120 seconds** to paint on first load, during which it looks
completely blank with no console errors.

That's worth knowing before you conclude your config is broken and start reverting
things. If a chart-heavy view is blank, wait longer before touching anything.

**Watch for software rendering.** If WebView2 or Chrome falls back to CPU compositing
— missing GPU driver, or running over remote desktop — `backdrop-filter` becomes
expensive regardless of how fast the CPU is. If glass feels sluggish, check GPU
acceleration first.

---

## Reloading after config changes

Dashboard changes made via the API don't always reach an already-open kiosk. Restart
the wrapper app, or force a reload, rather than assuming it picked them up. More than
once I've "fixed" something twice because the panel was showing a cached config.

---

## Rotating views

If you cycle views on a timer, remember to **turn it off while you're working on the
dashboard** — otherwise the page navigates away mid-edit and any live CSS you injected
for testing is destroyed.

An `input_boolean` plus an automation is enough:

```yaml
- alias: Rotate dashboard views
  trigger:
    - platform: time_pattern
      minutes: "/2"
  condition:
    - condition: state
      entity_id: input_boolean.dashboard_rotation
      state: "on"
  action:
    - service: browser_mod.navigate       # or your preferred method
      data: { path: /my-dashboard/next-view }
```
