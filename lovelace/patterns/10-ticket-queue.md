# 10 — Ticket queue

Counts from a work queue or helpdesk — open, awaiting triage, breached.

**Needs:** the [Windows bridge](../../bridge/README.md) calling your ticket system's
API. This is the most site-specific pattern here; the value is in the shape, not the
specifics.

---

## The pattern

Publish one sensor whose **state is the headline number** and whose attributes carry
the breakdown:

```powershell
Publish-State 'sensor.support_desk' ([string]$open) @{
    open            = $open
    awaiting_triage = $triage
    breached        = $breached
    sla_active      = $slaActive
}
```

Then the tile is just formatting:

```yaml
custom_fields:
  queue: |
    [[[
      const a = states['sensor.support_desk'].attributes;
      const cell = (label, value, colour) => `
        <div style="text-align:center">
          <div style="font-size:11px;opacity:.55">${label}</div>
          <div style="font-size:26px;font-weight:700;color:${colour}">${value}</div>
        </div>`;
      return `<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:10px">
        ${cell('OPEN', a.open, '#e5e5e5')}
        ${cell('SLA ACTIVE', a.sla_active, '#e0a63c')}
        ${cell('TRIAGE', a.awaiting_triage, '#4d98ff')}
        ${cell('BREACHED', a.breached, a.breached > 0 ? '#ff5656' : '#4ade80')}
      </div>`;
    ]]]
```

**Colour the number that matters.** Breached at zero should be green and breached at
three should be red — that's the whole point of the tile.

---

## Worth knowing

**Headline in `state`, detail in attributes.** State is what HA graphs and what shows
in the entity list, so put the number you'd want a history of there.

**Poll gently.** Five minutes is plenty for a work queue. A ticket API hit every ten
seconds will get you rate-limited and tells you nothing new.

**OAuth client-credentials is the usual auth.** Keep the client ID and secret in a
config file outside the repo, and store any token with DPAPI —
see [`Set-Token.ps1`](../../bridge/Set-Token.ps1). Never put a tenant URL or client
secret in a dashboard config; it ends up in your Lovelace storage and any backup of it.

**Same shape works for anything countable** — open PRs, failed backups, overdue tasks,
unread mail. The tile doesn't care what the numbers mean.
