# 08 — News carousel

Rotating headlines with images, one at a time.

**Needs:** an RSS feed via the [Windows bridge](../../bridge/README.md)
(see [`Get-NewsFeed.ps1`](../../bridge/collectors/Get-NewsFeed.ps1)), or HA's own
`feedparser` integration.

---

## The pattern

Publish all the stories as an **attribute array** on one sensor, then use an
`input_number` as the index. An automation steps the index on a timer; the tile just
reads whichever item is current.

```yaml
custom_fields:
  story: |
    [[[
      const s = states['sensor.news'];
      const items = s.attributes.headlines || [];
      if (!items.length) return `<div style="opacity:.4">No stories</div>`;
      const i = Number(states['input_number.news_index'].state) % items.length;
      const [headline, summary, when, link, image] = items[i].split('|');
      return `<div style="display:flex;gap:12px;align-items:flex-start">
        ${image ? `<img src="${image}" style="width:96px;height:64px;
             border-radius:8px;object-fit:cover">` : ''}
        <div style="flex:1;min-width:0">
          <div style="font-size:15px;font-weight:700">${headline}</div>
          <div style="font-size:12px;opacity:.6;margin-top:3px">${summary}</div>
          <div style="font-size:10px;opacity:.4;margin-top:4px">
            ${when} · ${i + 1} of ${items.length}</div>
        </div>
      </div>`;
    ]]]
triggers_update:
  - input_number.news_index
  - sensor.news
```

```yaml
- alias: Rotate news
  trigger: [{ platform: time_pattern, seconds: '/20' }]
  action:
    - service: input_number.increment
      target: { entity_id: input_number.news_index }
```

---

## Worth knowing

**One sensor with an array beats one sensor per story.** Fewer entities, less recorder
churn, and the tile can show a position indicator.

**Set the `input_number` max high and use modulo** rather than trying to wrap it —
`i % items.length` handles a feed that changes length without the index going out of
range.

**20 seconds is about right.** Faster and it's unreadable; slower and it feels stuck.

**Every rotation writes to the recorder.** A three-second rotation is 28,800 state
changes a day for no benefit. Worth excluding the index helper from recorder if you
go fast.
