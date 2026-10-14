# Lounge tablet

Big, simple tiles that kids and visitors can use.

![Lounge tablet](../HA%20Dashboard%20Pics/Lounge.jpg)

- **Row 1:** a split tile (Main light / All lamps), Rear lamp, Front lamp, LED strips (both strips toggle together)
- **Row 2:** three blinds with up/down arrows, plus a weather tile with a big clock
- Icons scale with the tile size; text in the weather tile uses `min(px, vh)` caps so it never clips on a shorter tablet viewport
- Own-drawn inline SVG icons (e.g. an LED-strip ribbon) instead of copied icon packs, to avoid licence issues

**Next idea:** an Apple-Home-hub style glass version. A first attempt was parked. Lesson: with button-card v7, templates inside nested cards are evaluated in the parent's context, so nested cards should only reference named templates.
