# 22 — VPP event tile

A single tile that answers three questions about a paid grid-export event: what is
coming, what the last one earned, and whether the automations behind it actually did
their jobs.

![placeholder — screenshot goes here]

---

## What it is

A full-height `custom:button-card` split into three blocks:

- **Now** — event state, window, direction, inverter mode, force-export state
- **Last event** — window, exported kWh, battery kWh, estimated return
- **Automations** — one row per automation with a *Ran* and an *Outcome* column

The third block is the point of the tile. Anyone can show whether an automation
triggered; this shows whether it accomplished anything, which is a different question
and the one you actually care about at 6am when there is money on the line.

Everything is a readout — no tap actions. The tile is for knowing, not doing.

---

## Needs

- `custom:button-card` (HACS)
- A VPP integration publishing event window sensors. Examples use Axle; any provider
  that exposes start/end/in-progress sensors will map across
- GivTCP for the inverter mode and energy counters
- A shared header template — ours is `dm_home_header`, see
  `lovelace/button-card-templates.yaml`. Substitute your own or inline a header
- The helpers and capture automation from `docs/07-vpp-events.md`. **Build those first**
  — the Last event and Automations blocks read helpers, not sensors, and will sit empty
  without them

> Replace `aa1111b222` with your GivTCP inverter serial.

Sized for a quarter-width column on a 2560×720 panel, roughly 620 × 640px. It is close
to full at that size; see Gotchas.

---

## The tile

```yaml
type: custom:button-card
show_icon: false
show_name: false
show_label: false
tap_action:
  action: none
hold_action:
  action: none

triggers_update:
  - sensor.axle_vpp_axle_event_window_state
  - sensor.axle_vpp_axle_event_in_progress
  - sensor.axle_vpp_axle_start_time
  - sensor.axle_vpp_axle_end_time
  - sensor.axle_vpp_axle_event_minutes_to_start
  - sensor.axle_vpp_axle_event_remaining_minutes
  - sensor.axle_vpp_axle_import_export
  - sensor.axle_vpp_axle_event_tomorrow
  - sensor.axle_vpp_axle_event_later_today
  - sensor.axle_vpp_axle_event_completed_today
  - select.givtcp_aa1111b222_mode
  - select.givtcp_aa1111b222_force_export
  - select.givtcp_aa1111b222_charge_start_time_slot_2
  - select.givtcp_aa1111b222_charge_end_time_slot_2
  - input_number.axle_last_export
  - input_number.axle_last_battery
  - input_number.axle_last_precharge
  - input_number.axle_event_rate
  - input_text.axle_last_event_when
  - input_text.axle_last_event_actions
  - input_text.axle_eco_result

styles:
  card:
    - padding: 16px 22px
    - border-radius: 18px
    - background: linear-gradient(160deg, rgba(20,26,32,.96), rgba(12,16,20,.98))
    - border: 1px solid rgba(255,255,255,0.08)
    - box-shadow: none
    - overflow: hidden
  grid:
    - grid-template-areas: '"header" "line" "body"'
    - grid-template-columns: 1fr
    - grid-template-rows: 42px 2px 1fr
  custom_fields:
    header:
      - height: 100%
      - width: 100%
    line:
      - height: 2px
      - width: 100%
      - background: rgba(255,255,255,0.22)
    body:
      - height: 100%
      - width: 100%
      - padding-top: 10px
      - min-height: 0
      - overflow: hidden

custom_fields:
  header:
    card:
      type: custom:button-card
      template: dm_home_header
      name: Axle VPP
      icon: mdi:transmission-tower-export

  line: ""

  body: |
    [[[
      const S = (e) => states[e] ? states[e].state : null;
      const N = (e, d) => { const v = parseFloat(S(e)); return isNaN(v) ? d : v; };

      /* ---------- live event ---------- */
      const dir   = (S('sensor.axle_vpp_axle_import_export') || '').toLowerCase();
      const win   = S('sensor.axle_vpp_axle_event_window_state') || 'none';
      const live   = S('sensor.axle_vpp_axle_event_in_progress') === 'on';
      const tmr    = S('sensor.axle_vpp_axle_event_tomorrow') === 'on';
      const today  = S('sensor.axle_vpp_axle_event_later_today') === 'on';
      const done   = S('sensor.axle_vpp_axle_event_completed_today') === 'on';
      const mins   = N('sensor.axle_vpp_axle_event_minutes_to_start', null);
      const rem    = N('sensor.axle_vpp_axle_event_remaining_minutes', null);
      const st     = S('sensor.axle_vpp_axle_start_time');
      const en     = S('sensor.axle_vpp_axle_end_time');
      const mode   = S('select.givtcp_aa1111b222_mode') || '--';
      const fe     = S('select.givtcp_aa1111b222_force_export') || '--';
      const has    = !!st;

      /* ---------- last event, from helpers ---------- */
      const lw   = S('input_text.axle_last_event_when') || '';
      const lex  = N('input_number.axle_last_export', 0);
      const lbat = N('input_number.axle_last_battery', 0);
      const rate = N('input_number.axle_event_rate', 1);
      const acts = S('input_text.axle_last_event_actions') || '';
      const earn = lex * rate;
      const actsL = acts.toLowerCase();

      /* ---------- helpers ---------- */
      const hhmm = (iso) => {
        if (!iso) return '--:--';
        const d = new Date(iso);
        return isNaN(d) ? '--:--'
          : ('0'+d.getHours()).slice(-2) + ':' + ('0'+d.getMinutes()).slice(-2);
      };
      const dur = (m) => {
        if (m === null) return '--';
        const h = Math.floor(m/60), mm = Math.round(m%60);
        return (h ? h+'h ' : '') + mm + 'm';
      };
      const row = (l, v, vc) =>
        "<div style='display:flex;justify-content:space-between;align-items:baseline;"
        + "padding:2px 0;border-bottom:1px solid rgba(255,255,255,.07)'>"
        + "<span style='font-size:14px;letter-spacing:1.4px;color:#8fa3ad;"
        + "text-transform:uppercase'>" + l + "</span>"
        + "<span style='font-size:20px;font-weight:600;color:" + (vc||'#f1f1f1') + "'>"
        + v + "</span></div>";

      /* ---------- headline state ---------- */
      const dc = dir === 'export' ? '#7ee0a8'
               : (dir === 'import' ? '#ff9d6e' : '#8fa3ad');
      const state = live ? 'EVENT ACTIVE'
                  : (done ? 'COMPLETED TODAY'
                  : (today ? 'LATER TODAY'
                  : (tmr ? 'TOMORROW'
                  : (win === 'upcoming' ? 'SCHEDULED' : 'NO EVENT'))));
      const sc = live ? '#7ee0a8' : (done ? '#8fa3ad' : '#ffcf6e');

      /* ---------- automations block ---------- */
      const SH = {
        '6b_pre_event_export_pre_charge_same_day': 'Pre-charge',
        'self_dispatch_if_their_command_does_not_land': 'Self dispatch',
        '6a_post_event_ensure_eco': 'Eco restore',
        '6b_reset_day_charge_slot': 'Reset charge slot',
        'nightly_eco_backstop': 'Nightly Eco check'
      };
      const ORD = [
        '6b_pre_event_export_pre_charge_same_day',
        'self_dispatch_if_their_command_does_not_land',
        '6a_post_event_ensure_eco',
        '6b_reset_day_charge_slot',
        'nightly_eco_backstop'
      ];
      const oi = (k) => {
        const i = ORD.indexOf(k.split('.')[1].replace(/^axle_/, ''));
        return i < 0 ? 99 : i;
      };
      const arow = (l, r, rc, o, oc) =>
        "<div style='display:flex;align-items:baseline;padding:2px 0;text-align:left'>"
        + "<span style='flex:1;text-align:left;font-size:17px;color:#cfd8dc'>"+l+"</span>"
        + "<span style='width:56px;text-align:right;white-space:nowrap;font-size:15px;"
        + "font-weight:600;color:"+rc+"'>"+r+"</span>"
        + "<span style='width:150px;text-align:right;white-space:nowrap;font-size:15px;"
        + "font-weight:600;color:"+oc+"'>"+o+"</span></div>";

      const GY='#6b7c85', GN='#7ee0a8', AM='#ffcf6e', RD='#ff7b6e', NU='#8fa3ad';
      let autoRows = '';
      Object.keys(states)
        .filter(k => k.indexOf('automation.axle') === 0
                  && k.indexOf('event_capture') < 0)
        .sort((a,c) => oi(a) - oi(c))
        .forEach(k => {
          const key = k.split('.')[1].replace(/^axle_/, '');
          const lbl = SH[key] || key.replace(/_/g, ' ');
          const off = states[k].state !== 'on';
          const fired = actsL.indexOf(key.replace(/_/g, ' ')) >= 0;
          let r, rc, o, oc;

          if (off) { r='OFF'; rc=AM; o='Disabled'; oc=AM; }

          /* measured: did the battery actually charge? */
          else if (key === '6b_pre_event_export_pre_charge_same_day') {
            const pcs = states['input_number.axle_last_precharge'];
            const pc = pcs ? parseFloat(pcs.state) : NaN;
            const did = !isNaN(pc) && pc > 0.2;
            r = fired ? 'YES' : 'NO';  rc = fired ? GN : GY;
            o = did ? ('+' + pc.toFixed(1) + ' kWh')
                    : (fired ? 'Not needed' : '\u2014');
            oc = did ? GN : NU;
          }

          /* inferred from the trigger only */
          else if (key === 'self_dispatch_if_their_command_does_not_land') {
            r = fired ? 'YES' : 'NO';  rc = fired ? GN : GY;
            o = fired ? 'Dispatched' : '\u2014';  oc = fired ? GN : GY;
          }

          /* measured: mode before vs after */
          else if (key === '6a_post_event_ensure_eco') {
            const ers = states['input_text.axle_eco_result'];
            const er = ers ? ers.state : '';
            const hasRes = /confirm|chang|not eco/i.test(er);
            r = hasRes ? 'YES' : (/pending/i.test(er) ? 'PENDING' : '\u2014');
            rc = hasRes ? GN : (/pending/i.test(er) ? NU : GY);
            if (/confirm/i.test(er))      { o='No action';  oc=GN; }
            else if (/chang/i.test(er))   { o='Set to Eco'; oc=GN; }
            else if (/not eco/i.test(er)) { o='Failed';     oc=RD; }
            else if (/pending/i.test(er)) { o='Pending';    oc=NU; }
            else                          { o='\u2014';     oc=GY; }
          }

          /* verified from live state, not from the write */
          else if (key === '6b_reset_day_charge_slot') {
            const ss = states['select.givtcp_aa1111b222_charge_start_time_slot_2'];
            const se = states['select.givtcp_aa1111b222_charge_end_time_slot_2'];
            const cleared = !!ss && !!se
              && ss.state === '00:00:00' && se.state === '00:00:00';
            r = fired ? 'YES' : 'NO';  rc = fired ? GN : GY;
            o = cleared ? 'Slot cleared'
                        : (ss ? ('Slot ' + String(ss.state).slice(0,5)) : '\u2014');
            oc = cleared ? GN : AM;
          }

          /* daily job: did it run today? */
          else if (key === 'nightly_eco_backstop') {
            const lt = states[k].attributes ? states[k].attributes.last_triggered : null;
            const ran = lt
              && (new Date(lt)).toDateString() === (new Date()).toDateString();
            r = ran ? 'YES' : 'PENDING';  rc = ran ? GN : NU;
            o = ran ? ((mode === 'Eco') ? 'Eco confirmed' : (mode || '\u2014'))
                    : 'Pending';
            oc = ran ? ((mode === 'Eco') ? GN : AM) : NU;
          }

          else { r = fired ? 'YES' : 'NO'; rc = fired ? GN : GY;
                 o = fired ? 'Not measured' : '\u2014'; oc = NU; }

          autoRows += arow(lbl, r, rc, o, oc);
        });
      if (!autoRows) autoRows =
        "<div style='font-size:16px;color:#8fa3ad;padding:4px 0'>"
        + "No VPP automations found</div>";

      /* ---------- render ---------- */
      return `
      <div style='display:flex;flex-direction:column;height:100%;
                  justify-content:flex-start'>

        <div style='text-align:center;margin:0 0 6px'>
          <div style='font-size:29px;font-weight:700;letter-spacing:2px;
                      color:${sc}'>${state}</div>
          <div style='font-size:17px;letter-spacing:2px;color:${dc};
                      text-transform:uppercase;margin-top:4px'>
            ${has ? dir : 'no window published'}
          </div>
        </div>

        ${has ? row('Window', hhmm(st) + ' &ndash; ' + hhmm(en), '#f1f1f1') : ''}
        ${has && !live && mins !== null ? row('Starts in', dur(mins), '#ffcf6e') : ''}
        ${live && rem !== null ? row('Remaining', dur(rem), '#7ee0a8') : ''}
        ${row('Inverter mode', mode, mode === 'Eco' ? '#7ee0a8' : '#ffcf6e')}
        ${row('Force export', fe, /normal/i.test(fe) ? '#8fa3ad' : '#ff9d6e')}

        <div style='margin-top:4px;padding-top:4px;
                    border-top:2px solid rgba(255,255,255,.18)'>
          <div style='font-size:14px;letter-spacing:2px;color:#8fa3ad;
                      text-transform:uppercase;margin-bottom:4px;
                      text-align:center'>Last event</div>
          ${lw ? (
              row('When', lw, '#f1f1f1')
            + row('Exported', lex.toFixed(2) + ' kWh', '#7ee0a8')
            + row('From battery', lbat.toFixed(2) + ' kWh', '#9fd0ff')
            + row('Est. return', '&pound;' + earn.toFixed(2)
                + " <span style='font-size:13px;color:#8fa3ad'>@ &pound;"
                + rate.toFixed(2) + '/kWh</span>', '#ffcf6e')
          ) : "<div style='font-size:16px;color:#8fa3ad;padding:10px 0'>"
              + "No completed event recorded yet</div>"}
        </div>

        <div style='margin-top:4px'>
          <div style='display:flex;align-items:baseline;margin-bottom:2px;
                      text-align:left'>
            <span style='flex:1;text-align:left;font-size:14px;letter-spacing:2px;
                         color:#8fa3ad;text-transform:uppercase'>Automations</span>
            <span style='width:56px;text-align:right;font-size:12px;
                         letter-spacing:1px;color:#6b7c85'>RAN</span>
            <span style='width:150px;text-align:right;font-size:12px;
                         letter-spacing:1px;color:#6b7c85'>OUTCOME</span>
          </div>
          ${autoRows}
        </div>

      </div>`;
    ]]]
```

---

## How it works

**The top block reads sensors directly.** State is derived by precedence rather than
trusting a single sensor: in progress beats completed, beats later today, beats tomorrow,
beats a generic upcoming flag. Rows appear and disappear with context — *Starts in* only
before an event, *Remaining* only during one, the window row only when a window exists.

**The middle block reads helpers, not sensors.** Historical figures cannot come from
sensors, because a button-card template only ever sees current state — there is no
history access. The capture automation writes what happened into helpers at the moment
it happens, and the tile renders those. Consequence: the middle block is empty until the
first event completes.

**Exported and From battery are deliberately both shown.** Exported is what the meter
saw and what gets paid. From battery is what the event cost you in stored energy. They
differ whenever solar was generating during the window, and the gap between them is
information, not noise.

**The bottom block builds itself.** It scans `states` for `automation.axle*`, orders them
by a fixed lifecycle array rather than alphabetically, and renders a row each. A new
automation appears automatically with its entity name until you add a friendly label.

**Each outcome is measured differently, on purpose.** See the table in
`docs/07-vpp-events.md`. The short version: pre-charge compares an energy counter,
eco restore compares mode before and after, the slot reset reads the slot back, and the
nightly job checks whether it ran today. Only self dispatch is inferred from its trigger,
and it says so by not claiming more than "Dispatched".

**Status colours carry meaning.** Green for done or nothing needed, amber for pending or
disabled or a state you should look at, red for a genuine failure, grey for not
applicable. Four statuses beat a tick and a cross because "ran and correctly did nothing"
is a real outcome that a boolean cannot express.

---

## Gotchas

**This tile is full.** At 620 × 640 the content exactly fills the body. Adding a row, or
a value long enough to wrap, will overflow and clip. Check with:

```js
// in devtools, on the inner button-card's shadow root
const b = card.shadowRoot.querySelector('#body');
b.scrollHeight > b.clientHeight   // true means it is clipping
```

If you need room, *Inverter mode* and *Force export* are the first to drop — they usually
appear on a solar view too. Or set `overflow-y: auto` on the body and let it scroll.

**Fixed column widths clip silently.** The Ran and Outcome columns are 56px and 150px.
Raise the font size without widening them and adjacent values run together with no
visible error — `YESNo action needed` was a real bug during development. `white-space:
nowrap` makes the overflow obvious rather than reflowing.

**Verify CSS in the browser, not in the config.** Checking your `extra_styles` text with
a regex proves nothing. A single stray brace makes the parser discard everything up to
the next valid rule, and the config still *looks* correct. Read `sheet.cssRules` instead.

**Pre-flight every template.** This tile lives on a single full-screen button-card, where
one template error blanks the entire view. Test with `new Function(...)` against several
fake state objects — including one where every entity is missing — before saving.

**Times render in browser-local time.** The sensors publish UTC. Correct on the panel,
but a browser in another timezone shifts the window text.

---

## Adapting it

**Different VPP provider.** Replace the sensor names in the top block and the
`automation.axle*` prefix in the scan. Octopus saving sessions expose a binary sensor
plus a calendar entity; the same three-block structure holds.

**No GivTCP.** Drop the *Inverter mode* and *Force export* rows and remove the slot-reset
verification, which is GivEnergy-specific. That frees roughly 60px.

**Fewer automations.** The block is generated from whatever exists, so it degrades
gracefully. With no matching automations it prints a single "No VPP automations found"
line.

**Half-height tile.** Cut the automations block and keep Now plus Last event. That is the
natural split if you want the money figures at a glance and the diagnostics on a
drill-down — see `lovelace/patterns/13-drilldown-views.md`.

**A different rate.** `input_number.axle_event_rate` drives the estimate, so it is
editable from the UI without touching the card. The label says *Est. return* because the
rate is an assumption, not a settled figure — keep that honesty in any fork.
