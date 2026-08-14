# 05 — Printer consumables

Ink, toner, drum and belt levels as coloured bars.

**Needs:** a printer integration, or the [Windows bridge](../../bridge/README.md) if
your printer only reports over SNMP or a vendor API.

---

## The pattern

Four or eight small bars, coloured by level. It's the same progress-bar technique as
[06-system-stats](06-system-stats.md), with one difference: **use the real ink colours**
for CMYK, not a threshold gradient. A cyan bar should be cyan.

```yaml
custom_fields:
  levels: |
    [[[
      const inks = [
        ['K', 'sensor.printer_black',   '#d7dde0'],
        ['C', 'sensor.printer_cyan',    '#26c6e5'],
        ['M', 'sensor.printer_magenta', '#e65aa8'],
        ['Y', 'sensor.printer_yellow',  '#f2cc3d']
      ];
      return `<div style="display:grid;grid-template-columns:repeat(4,1fr);gap:12px">
        ${inks.map(([label, id, colour]) => {
          const v = Number((states[id] || {}).state) || 0;
          return `<div>
            <div style="display:flex;justify-content:space-between;font-size:11px">
              <span style="opacity:.6">${label}</span>
              <span style="color:${v < 10 ? '#ff5656' : colour}">${v}%</span>
            </div>
            <div style="height:5px;background:rgba(255,255,255,0.08);
                 border-radius:3px;margin-top:4px">
              <div style="width:${Math.max(0,Math.min(100,v))}%;height:100%;
                   background:${colour};border-radius:3px"></div>
            </div>
          </div>`;
        }).join('')}
      </div>`;
    ]]]
```

Override to red below 10% so a nearly-empty cartridge stands out regardless of its
ink colour.

---

## Worth knowing

**Consumables belong in a notification, not a dashboard.** Same argument as filter
life in [17-purifier-tile](17-purifier-tile.md) — you'll walk past the tile for weeks.
Use a `numeric_state` trigger below 10% and let the tile be for confirming.

**Offline is the normal state** for most home printers. Show it clearly rather than
rendering zeros, or you'll think you're out of ink when the printer is simply asleep.

**Drum, belt and fuser** are separate consumables with much longer lives than ink.
Worth a second row rather than mixing them in with the cartridges.
