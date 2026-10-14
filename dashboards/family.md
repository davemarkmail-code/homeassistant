# Family dashboard (wall panel)

The main family screen: one look tells you what's happening today, this week, who's home, what jobs are left and what the house is doing.

![Family dashboard](../HA%20Dashboard%20Pics/Family.jpg)

## Layout
- **Top bar:** greeting + weather, the house/energy stats (solar, battery, grid, car charging), a "sleeps until…" countdown for birthdays and holidays
- **Who's home:** face circles with a coloured ring. Shows *On a call*, *Busy*, *Adventuring 🐉*, *Wants coffee ☕*, *Come in! 🥺*. See [presence](../presence-and-dnd.md).
- **Front door chip:** Locked / Unlocked, plus *Battery low*, *Keypad low* or *Offline* in amber
- **Today timeline:** one horizontal strip of the day
  - Family events in green on the centre line, with titles in two staggered rows under the bars
  - Dad's work (orange) moves to an upper lane when it clashes with family; Mum's lessons (purple, labelled just "Lesson") move to a lower lane
  - Past events go grey with no label; the live event's title turns blue
  - A short white "now" line with its label in the hours row
- **Week view:** 7 day boxes, up to 3 events each plus "+N more · tap". Tapping a day opens a pop-up.
- **Jobs:** 3 columns (Dad, Mum, our son) of to-do items with a hand-drawn pen-tick and strike-through when done
- **Points thermometer:** next to our son's jobs, filling up towards 300 points = a LEGO set. See [family jobs](../family-jobs.md).
- **Doorbell pop-up:** live camera for 30 s when someone rings

## Privacy rule
Work calendars only ever show as anonymous "Work" / "Lesson" bars. Meeting titles and links **never** reach family screens.

## How it's built
- One `custom:button-card` in a panel view. Its body is about 180 KB of HTML/CSS/JS built from live `hass.states`.
- A fixed design canvas, scaled with `transform: scale()` to fit the screen
- Tap zones are separate absolutely-positioned custom fields that call scripts (`script.job_toggle`, etc.)
- A **Dev copy** of the dashboard mirrors live; big changes are tried there first
- Edits are made by a small Node script over the HA websocket (`lovelace/config` → string replace → `lovelace/config/save`) with an automatic backup, rather than the UI editor, which struggles with a card this size

### Glass style (CSS idea)
```css
/* soft background glows */
.fam { background:
  radial-gradient(900px 600px at 10% 0%, rgba(70,110,255,.34), transparent 60%),
  radial-gradient(700px 500px at 90% 100%, rgba(40,180,160,.16), transparent 60%),
  radial-gradient(800px 600px at 80% 10%, rgba(150,90,255,.24), transparent 60%),
  #070b10; }
/* frosted panels */
.card { background: linear-gradient(155deg, rgba(255,255,255,.10), rgba(255,255,255,.02) 38%, rgba(255,255,255,0) 60%, rgba(255,255,255,.05));
  backdrop-filter: blur(28px) saturate(170%);
  border: 1px solid rgba(255,255,255,.16);
  box-shadow: inset 0 1px 0 rgba(255,255,255,.26), inset 0 -1px 0 rgba(0,0,0,.25), 0 18px 46px rgba(0,0,0,.5); }
```
