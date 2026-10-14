# Mobile dashboards

## Mobile home
Four big buttons for the things we actually do on a phone: **Jobs**, **Dinners** (both open the [Family ToDo](family-todo.md) pages), **Solar** and **Home Dojos**.

## Mobile Solar
The Office energy card, re-laid out to fit an iPhone 12 Pro Max with **no scrolling** (about 700px): power-flow graphic, today tiles, yesterday's totals, VPP event, battery mode buttons, links to history.

## Home Dojo points keypad
A page for adding or taking away our son's reward points.
- An HTML `<input>` inside button-card was unreliable on iOS (taps never fired), and the native number box only committed on Return
- **What worked:** an on-screen keypad of button-cards (1-9, C, 0, ⌫) calling a script that builds up an `input_number`, then OK / Take away buttons that call `script.points_apply`. Reset needs a confirmation.
