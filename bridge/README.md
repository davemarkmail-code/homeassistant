# bridge/ — setup

Getting Windows data into Home Assistant as sensors. Ten minutes end to end.

---

## 1. Create a Home Assistant token

In HA: **Profile → Security → Long-lived access tokens → Create token**. Copy it —
you only see it once.

## 2. Encrypt it to disk

```powershell
cd bridge
.\Set-Token.ps1 -Name ha
```

Paste the token when prompted. It writes `ha-token.dat`, encrypted with DPAPI and
readable only by your Windows account. Nothing plaintext ever hits disk.

## 3. Point the publisher at your HA

Edit the top of `Publish-Sensors.ps1`:

```powershell
[string]$BaseUrl = 'http://homeassistant.local:8123',
```

## 4. Test one collector on its own

```powershell
.\collectors\Get-NowPlaying.ps1
Get-Content .\collectors\NowPlaying.txt
```

You should see a pipe-delimited line. **No HA needed for this step** — that's the
point of the file in the middle. If this works, collection is fine and anything
that breaks later is publishing.

## 5. Publish once, by hand

```powershell
.\Publish-Sensors.ps1
```

Check **Developer Tools → States** in HA for `sensor.office_now_playing`. It'll be
there instantly — no restart, no YAML, no integration.

## 6. Run the loop

```powershell
.\Run-Bridge.ps1
```

Ctrl-C to stop. Once you're happy, run it at login: edit the path inside
`Start-Bridge.vbs`, then drop that file into `shell:startup` (Win+R → `shell:startup`).

---

## Layout

```
bridge/
├── Run-Bridge.ps1          the loop — staggered intervals, single-instance mutex
├── Publish-Sensors.ps1     the ONLY script that knows about HA
├── Set-Token.ps1           run once per token
├── Start-Bridge.vbs        launcher, no console window
├── ha-token.dat            created by Set-Token.ps1 — NEVER COMMIT
└── collectors/
    ├── Get-NowPlaying.ps1  Windows media session
    ├── Get-Bitcoin.ps1     simplest possible example
    ├── Get-NewsFeed.ps1    any RSS feed
    └── *.txt               their output
```

---

## Adding your own collector

1. Write a script in `collectors/` that fetches something and writes **one file**.
   Use `Join-Path $PSScriptRoot` for the output path — never an absolute path.
2. Add an `Invoke-Collector` line to the schedule block in `Run-Bridge.ps1`.
3. Add a `Publish-State` block in `Publish-Sensors.ps1`.

Keep collectors dumb: fetch, format, write, exit. No HA knowledge, no token. That
keeps them independently runnable, which is what makes debugging quick.

---

## When something breaks

| Symptom | Cause |
|---|---|
| Sensor frozen, **file also stale** | Bridge not running — check Startup folder |
| Sensor frozen, **file updating** | Publisher failing — token, HA address, or a parse error |
| One tile stale, rest fine | That collector is erroring. Run it by hand |
| Broke right after you moved the folder | An absolute path. Grep for `C:\` |
| Accented characters mangled | Body not UTF-8 encoded before POST |
| Sensor exists but shows `unknown` | Empty state, or over 255 characters |

**Check `bridge.log`.** It appends and self-trims at 1 MB, so errors survive long
enough to read.

**Don't trust `last_updated` in HA to tell you the bridge is alive.** HA only bumps
it when a value *changes*, so a working bridge reporting the same value for two hours
looks identical to a dead one. Check the file's modified time instead, or watch
`sensor.office_bridge_status`, whose `last_run` attribute always changes.

---

## Security

- `*.dat` is gitignored. Encrypted is not the same as publishable.
- DPAPI `CurrentUser` scope binds tokens to **your account on this machine**.
  Copying the `.dat` to another PC will fail to decrypt — re-run `Set-Token.ps1` there.
- The entropy string in `Set-Token.ps1` is a **salt, not a comment**. Change it and
  every existing token stops decrypting.
- HA long-lived tokens don't expire. If one leaks, revoke it in HA immediately.
