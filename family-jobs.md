# Family jobs & pocket money

## Recurring chores
- Local To-do lists per person (Dad, Mum, our son) plus daily/weekly/fortnightly/monthly "template" lists
- Automations **top up** each person's list on schedule, **stamp** an item when ticked, **reschedule** recurring ones, and **clear** ticked items an hour later
- A combined `sensor.family_jobs` (template sensor with attributes per person) feeds all the dashboards
- Items starting with `! ` are urgent (shown with ❗)

## Voice: Alexa lists
"Alexa, add X to the **Blue** list" → Dad's jobs; "**Pink** list" → Mum's jobs. An automation moves new items from the Alexa lists (core *Alexa Devices* integration) into the family lists.

Gotcha: Alexa treats any list name containing a person's name (e.g. "Jobs for Mum") as a voice profile and drops the item in the default To-do. Colour names work.

## Nudges
At 10:00 and 16:30 each parent gets a push listing their unfinished jobs. Tapping it opens their list. Nothing is sent if the list is done.

## Pocket money
- Each job our son ticks adds to a weekly count, at a per-job rate (`input_number`)
- A Sunday reminder shows the payout
- Gotcha: the counter diffs the sensor's attributes between old and new state. A template reload makes the sensor briefly `unavailable`, so already-ticked jobs were counted twice. Ignore `unavailable`/`unknown` on either side.

## Home Dojo points → LEGO
A thermometer next to our son's jobs on the Family panel fills towards **300 points**, with a LEGO brick trophy at the top and the running total on a tag that rides up the tube. Points are added from a phone keypad ([mobile](dashboards/mobile.md)).
