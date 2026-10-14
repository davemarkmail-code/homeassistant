# Maintenance & alerts

## Nightly auto-updates (after a backup)
At **03:00**, only if updates are pending:
1. `backup.create_automatic`: full backup, local + cloud, keep 7
2. If the "last successful automatic backup" sensor didn't change → push a notification and **stop**
3. Persistent notification listing what's about to install
4. `update.install` each add-on and HACS update (`continue_on_error: true`)
5. **One restart per night:** Core (skipping `x.0` releases and waiting for `x.1`), otherwise OS, otherwise an HA restart if a HACS integration was updated

Excluded: ESPHome device firmware and other device firmware, which are installed by hand.

```yaml
# is the pending core update a .0 release?
core_ok: >
  {% set v = state_attr('update.home_assistant_core_update','latest_version') or '' %}
  {{ is_state('update.home_assistant_core_update','on') and not v.endswith('.0') }}
```

## Front door lock battery
The lock went flat once and the door stayed unlocked, even though a "battery critical" sensor had been on for days. Now:
- Dashboards show **Battery low** (under 25% or critical), **Keypad low** and **Offline** in amber
- Phone push when low for 10 min, critical, keypad critical, or offline for 10 min, plus a daily 18:00 reminder while it's still true
- Note: an 80% charge limit set in the Nuki app makes charging "stop" at 80%. That's normal.

## Other watchdogs
- **UPS:** power-cut alert
- **Ring:** reload the integration if events go stale
- **iPad battery care:** hold the battery at 65–80% with smart plugs
- **Panels:** wake in the morning, dim/off at night, keep asleep overnight
