# Lessons learned

## Templates & sensors
- **Template reloads make sensors `unavailable` for a moment.** Anything triggered on state changes (DND, counters that diff attributes) needs `not_from: [unavailable, unknown]` or an equivalent guard.
- Track "who set it" with a separate boolean, so automations only undo what they themselves did.
- Watchdogs should check that a value *changes*, not just that it exists: a frozen battery reading looked perfectly healthy.

## button-card dashboards
- Build big dashboards as one `custom:button-card` with an HTML body: total layout control, but edit it by script, not in the UI editor
- If the body is minified onto one line, **never** insert a `//` comment. It comments out the rest of the line and the page renders blank.
- Absolutely-positioned pop-ups stay in the DOM when hidden and swallow taps. Use `pointer-events:none` on the field and `auto` on its card.
- In button-card v7, `[[[ ]]]` templates inside nested cards are evaluated in the **parent's** context. Nested cards should only reference named templates.
- CSS specificity: a later `.we{}` rule loses to an earlier `.fam .we{}`. Check before assuming an override applies.
- Keep a Dev copy of the main dashboard and try bigger changes there first

## iOS / Companion app
- HTML `<input>` + inline handlers inside button-card don't fire reliably on iPhone. Use buttons and scripts.
- The real viewport is smaller than the screen (status bar, nav bar). Report it to an `input_text` and design to that.

## Devices
- **Alexa timers** only update on a poll (~1 min lag), and Alexa ignores `media_play` for live radio
- **Govee** LAN control must be enabled per device, and lights read "off" briefly after a reload
- **Windows Hello** face login uses the webcam: delay the webcam sensor or every login looks like a call
- **HASS.Agent** PowerShell sensors: the command box is about 240 characters, so call a `.ps1` file
- When scanning windows with UIAutomation, limit it to browser processes. Electron apps (Discord, Teams, VS Code) expose tabs too.
- Rechargeable smart locks: alert on *low*, not just *critical*, and alert when the lock goes offline
