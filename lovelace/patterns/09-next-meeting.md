# 09 — Next meeting

Your next calendar event, with a join link.

**Needs:** a `calendar` entity (Google, CalDAV, Local Calendar), or the
[Windows bridge](../../bridge/README.md) if you need Outlook/Exchange via Microsoft
Graph.

---

## The pattern

```yaml
custom_fields:
  meeting: |
    [[[
      const c = states['calendar.work'];
      const a = c.attributes;
      if (!a.start_time) {
        return `<div style="text-align:center;padding-top:14px">
                  <div style="font-size:20px;font-weight:700">No meetings today</div>
                  <div style="font-size:12px;opacity:.5">Enjoy the peace</div>
                </div>`;
      }
      const start = new Date(a.start_time);
      const mins  = Math.round((start - Date.now()) / 60000);
      const when  = mins < 0 ? 'in progress'
                  : mins < 60 ? 'in ' + mins + ' min'
                  : start.toLocaleTimeString('en-GB',{hour:'2-digit',minute:'2-digit'});
      const soon  = mins >= 0 && mins <= 10;
      return `<div>
        <div style="font-size:11px;opacity:.5">NEXT MEETING</div>
        <div style="font-size:18px;font-weight:700;white-space:nowrap;
             overflow:hidden;text-overflow:ellipsis">${a.message}</div>
        <div style="font-size:14px;margin-top:3px;
             color:${soon ? '#e0a63c' : 'rgba(255,255,255,0.6)'}">${when}</div>
      </div>`;
    ]]]
triggers_update:
  - calendar.work
  - sensor.time
```

`sensor.time` in `triggers_update` is what makes the countdown tick — without it the
"in 12 min" only recalculates when the calendar itself changes.

---

## Worth knowing

**"No meetings" deserves as much design as a meeting.** It's the state you'll see most
of the time, and a blank tile looks broken. A line of copy costs nothing.

**Colour when it's imminent.** Amber inside ten minutes is the single most useful thing
this tile does — it's the difference between a calendar and a prompt.

**Join links are awkward.** `a.description` often contains a Teams or Meet URL buried
in text. Extracting it with a regex works but is brittle, and on a **kiosk in Guided
Access a link can't open anything anyway**. Consider showing "join link available"
rather than a button that goes nowhere.

**One event only.** A `calendar` entity's attributes describe the *next* event. For a
list you need `calendar.get_events`, which is a service call — so it can't be done from
a card template. Same constraint as forecasts in
[20-weather-tile](20-weather-tile.md).
