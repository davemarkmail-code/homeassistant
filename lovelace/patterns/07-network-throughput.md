# 07 — Network throughput

Download and upload rates as live figures.

**Needs:** a router integration, an SNMP sensor, or the
[Windows bridge](../../bridge/README.md) reading the local adapter.

---

## The pattern

Two figures with units, coloured by direction. Nothing clever:

```yaml
custom_fields:
  net: |
    [[[
      const fmt = bps => {
        if (bps > 1e6) return (bps / 1e6).toFixed(1) + ' Mbps';
        if (bps > 1e3) return (bps / 1e3).toFixed(0) + ' kbps';
        return Math.round(bps) + ' bps';
      };
      const down = Number(states['sensor.net_download'].state) || 0;
      const up   = Number(states['sensor.net_upload'].state) || 0;
      return `<div style="display:grid;grid-template-columns:1fr 1fr;gap:14px">
        <div>
          <div style="font-size:11px;opacity:.55">DOWNLOAD</div>
          <div style="font-size:22px;font-weight:700;color:#4ade80">${fmt(down)}</div>
        </div>
        <div>
          <div style="font-size:11px;opacity:.55">UPLOAD</div>
          <div style="font-size:22px;font-weight:700;color:#4d98ff">${fmt(up)}</div>
        </div>
      </div>`;
    ]]]
```

---

## The one real gotcha

**Most sources give you a cumulative byte counter, not a rate.** `bytes_received`
only ever goes up. To get a rate you must store the previous reading and divide by the
elapsed time:

```powershell
# in a bridge collector
$now   = Get-NetAdapterStatistics -Name 'Ethernet'
$prev  = Get-Content $stateFile | ConvertFrom-Json
$secs  = ((Get-Date) - [datetime]$prev.at).TotalSeconds
$down  = [math]::Round((($now.ReceivedBytes - $prev.rx) * 8) / $secs)
```

That's why the bridge keeps a small state file between runs — see
[02-windows-bridge.md](../../docs/02-windows-bridge.md). Doing the same in an HA
template is possible with `derivative` sensors, but a stateful collector is simpler
to reason about.

Two things follow from it: the **first reading after a restart is meaningless** (there's
no previous sample), and a **counter reset** — reboot, adapter reset — gives you a
negative delta. Clamp at zero rather than showing a nonsense figure.
