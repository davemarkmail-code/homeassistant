/* ===========================================================================
   AUTOMATIC PAGE ROTATION FOR A KIOSK DASHBOARD
   ===========================================================================

   THE GOAL
   --------
   A wall-mounted or desk kiosk display that cycles through several views on a
   timer — work/system, home automation, room controls — with a pause button and
   a visible countdown so you know when it's about to change.

   THE CONSTRAINT
   --------------
   Home Assistant storage-mode dashboards give you nowhere to put a script. The
   only place arbitrary JavaScript can live is inside a `custom:button-card`
   template, which is evaluated on render.

   HOW NOT TO DO IT
   ----------------
   My first version ran the whole rotation engine as a side effect of a card's
   `name:` template, with a 1-second `update_timer` to keep it ticking. It
   returned 'PAUSE' or 'PLAY' as the button label, and did the navigation on the
   way past. Clever, and it mostly worked.

   It also failed silently. On one particular view the card stopped re-rendering
   — and because the engine only existed *during* a render, rotation simply
   stopped. Polled data kept updating, the button still said PAUSE, and the
   countdown ticked into negative numbers with nothing listening. The dashboard
   just sat there.

   THE FIX
   -------
   Install a SINGLE PERSISTENT LOOP, once per page load, that reads state
   directly from the `hass` object rather than depending on any card rendering.
   A render stall can no longer stop it.

   The template below is idempotent: it installs the controller on first
   evaluation and does nothing on every subsequent one.

   WHERE TO PUT IT
   ---------------
   As a `custom_fields` entry on any button-card that appears on every rotating
   view — a header card is ideal. Style it to zero size; it renders nothing.

       custom_fields:
         rotctl: |
           [[[ ...the code below... ]]]
       styles:
         custom_fields:
           rotctl:
             - position: absolute
             - width: 0
             - height: 0
             - overflow: hidden
             - opacity: 0

   =========================================================================== */

[[[
if (!window.__rotCtl) {
  window.__rotCtl = true;

  /* ---- configure these three things ------------------------------------ */
  var DASHBOARD = '/my-dashboard';                 // your dashboard url_path
  var PAGES     = ['/work', '/home', '/controls']; // view paths, in order
  var PERIOD    = 60000;                           // ms per page
  var ENABLE    = 'input_boolean.dashboard_rotation';  // pause/play helper
  var SUFFIX    = '?kiosk';                        // kept on every navigation
  /* --------------------------------------------------------------------- */

  /* Which page are we on? -1 means "not a rotating view", so do nothing. */
  var idx = function () {
    var p = location.pathname;
    for (var i = 0; i < PAGES.length; i++) {
      if (p.indexOf(PAGES[i]) >= 0) return i;
    }
    return -1;
  };

  /* Read the pause/play helper straight from hass, not from a card. */
  var enabled = function () {
    var el = document.querySelector('home-assistant');
    if (!el || !el.hass) return false;
    var st = el.hass.states[ENABLE];
    return !!(st && st.state === 'on');
  };

  /* The next-swap timestamp is a global so the countdown ring can read it. */
  var arm = function () {
    window.__rotNext = Date.now() + PERIOD;
  };

  var tick = function () {
    try {
      var cur = idx();
      if (cur < 0) return;                    // not a rotating view

      if (!enabled()) {                       // paused: clear the clock so the
        window.__rotNext = 0;                 // countdown ring hides itself
        return;
      }

      var nxt = window.__rotNext || 0;
      if (!nxt) { arm(); return; }            // just un-paused: start counting

      if (window.__rotNavigating) return;     // a swap is already in flight
      if (Date.now() < nxt) return;           // not due yet

      window.__rotNavigating = true;
      arm();                                  // re-arm BEFORE navigating

      var target = DASHBOARD + PAGES[(cur + 1) % PAGES.length] + SUFFIX;

      var go = function () {
        /* pushState + location-changed is how you navigate a Lovelace
           dashboard from script without a full page reload. */
        window.history.pushState(null, '', target);
        window.dispatchEvent(new Event('location-changed'));
      };

      /* Crossfade where the browser supports it, plain swap where it doesn't */
      if (document.startViewTransition) {
        try { document.startViewTransition(go); } catch (e) { go(); }
      } else {
        go();
      }

      setTimeout(function () { window.__rotNavigating = false; }, 700);

    } catch (e) {
      /* Never let an exception leave the navigating flag stuck true, or
         rotation deadlocks until the page is reloaded. */
      window.__rotNavigating = false;
    }
  };

  /* 250ms is frequent enough to feel exact without costing anything. */
  window.__rotCtlTimer = setInterval(tick, 250);

  /* Browsers throttle timers in backgrounded tabs, so catch up on return. */
  document.addEventListener('visibilitychange', function () {
    if (!document.hidden) setTimeout(tick, 300);
  });
}
return '';
]]]


/* ===========================================================================
   THE COUNTDOWN RING
   ===========================================================================

   Add this as a `custom_fields` entry on each navigation button. It renders
   nothing unless (a) this button is the current page, and (b) rotation is
   running — so a paused dashboard shows no ring at all, which is honest.

   Read 05-styling-inside-shadow-dom.md for why this uses SVG rather than a
   conic gradient, and why the negative animation-delay is essential.

       custom_fields:
         sweep: |
           [[[ ...the code below, with PAGE set per button... ]]]
       styles:
         custom_fields:
           sweep:
             - position: absolute
             - inset: -2px          # sit ON the border, not inside it
             - z-index: 6
             - pointer-events: none
       extra_styles: |
         ha-card { position: relative !important; overflow: visible !important; }

   =========================================================================== */

[[[
var PAGE = '/work';                       /* this button's own page */

var here = location.pathname.indexOf(PAGE) >= 0;
if (!here) return '';                     /* not the active page */

var nxt = window.__rotNext || 0;
if (!nxt) return '';                      /* rotation paused */

var total   = 60000;                      /* must match PERIOD above */
var rem     = nxt - Date.now();
if (rem < 0)     rem = 0;
if (rem > total) rem = total;
var elapsed = total - rem;                /* how far through we already are */

return '<svg style="position:absolute;inset:0;width:100%;height:100%;'
     + 'overflow:visible;pointer-events:none">'
     + '<style>@keyframes dmsw{to{stroke-dashoffset:0}}'
     + '.dmr{width:calc(100% - 2px);height:calc(100% - 2px);}</style>'
     + '<rect class="dmr" x="1" y="1" rx="9" fill="none" stroke="#ff2f2f"'
     + ' stroke-width="3" pathLength="100" stroke-dasharray="100"'
     + ' stroke-dashoffset="100"'
     + ' style="animation:dmsw ' + total + 'ms linear forwards;'
     + ' animation-delay:-' + elapsed + 'ms"/></svg>';
]]]


/* ===========================================================================
   NOTES FROM LIVE USE
   ===========================================================================

   * A kiosk browser is a SEPARATE SESSION from the one on your desk. If the
     dashboard looks stuck on the wall panel while it behaves fine in your
     browser, reload the kiosk rather than assuming a server fault. These
     globals are per-page-load.

   * `triggers_update: 'all'` on a navigation button is a trap. It makes the
     card re-render on EVERY state change in Home Assistant, which restarted my
     countdown ring several times a second and interfered with the rotation
     timing. The ring needs no triggers at all — it computes its position from
     the clock and CSS does the rest.

   * If you have an existing engine you can't easily remove, this controller
     works fine as a WATCHDOG instead: give it a grace period and let it act
     only when the original is overdue.

         if (Date.now() - nxt < 5000) return;   // let the original try first

     That was my intermediate step. It worked, but the five-second pause after
     the ring completed looked like a fault, so I promoted the controller to
     primary and never looked back.

   =========================================================================== */
