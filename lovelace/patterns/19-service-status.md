# 19 — Service status

Whether a cloud service is up — Microsoft 365, AWS, GitHub, your own hosting.

**Needs:** the [Windows bridge](../../bridge/README.md) reading a public status feed,
or an HA `rest` sensor if the endpoint is simple enough.

---

## The pattern

Most status pages publish JSON or RSS. Reduce it to **one word and a colour**:

```yaml
custom_fields:
  status: |
    [[[
      const s  = states['sensor.m365_status'];
      const ok = s.state === 'operational';
      const col = ok ? '#4ade80' : (s.state === 'degraded' ? '#e0a63c' : '#ff5656');
      return `<div>
        <div style="font-size:11px;opacity:.55">SERVICE STATUS</div>
        <div style="font-size:22px;font-weight:700;color:${col}">
          ${ok ? 'M365 is operational' : s.state.toUpperCase()}</div>
        <div style="font-size:11px;opacity:.45;margin-top:4px">
          ${s.attributes.detail || 'No incidents reported'}</div>
      </div>`;
    ]]]
```

---

## Worth knowing

**Green when boring is the entire design.** You'll glance at this a hundred times for
every once it matters, so "operational" should be quiet and anything else should be
loud.

**Poll every ten minutes at most.** Status pages don't change quickly and several are
rate-limited. A ten-second poll is both rude and useless.

**Never let a fetch failure read as an outage.** If your collector can't reach the
status page, that's *your* connection, not their service. Publish a distinct
`unknown` state rather than defaulting to "down" — otherwise the tile will confidently
tell you Microsoft is broken every time your own wifi drops.

**Keep the last known good value** rather than blanking on error. A stale "operational"
with a timestamp is more useful than an empty tile, provided the timestamp is visible.

**The same tile works for anything with an up/down state** — a website you run, a VPN
endpoint, a backup job. Only the source changes.
