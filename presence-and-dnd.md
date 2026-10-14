# Presence, calls & Do Not Disturb

## "On a call" from the PC itself
Calendars are unreliable for calls, so we ask the PC: **HASS.Agent** reports whether the microphone or webcam is in use (10 s interval over MQTT).

```yaml
template:
  - binary_sensor:
      # Windows Hello face login uses the webcam for ~20 s, so the webcam needs 45 s before it counts
      - name: Office webcam in use
        delay_on: "00:00:45"
        state: "{{ is_state('binary_sensor.pc_webcamactive', 'on') }}"
      - name: Dad on a call
        delay_off: "00:00:20"
        state: >
          {{ is_state('person.dad', 'home')
             and (is_state('binary_sensor.pc_microphoneactive', 'on')
                  or is_state('binary_sensor.office_webcam_in_use', 'on'))
             and not is_state('binary_sensor.dad_dnd_session', 'on') }}
```

Mum's "Busy" stays calendar-based (her tutoring lessons), because her work isn't on a PC.

## Do Not Disturb
- `input_boolean.dad_do_not_disturb`: a manual button on the office dashboard
- **Auto DND:** turns on during a Teams meeting or call and off 1 minute after both end. It only clears DND it set itself (tracked with a second boolean), so a manual DND is never cleared by a meeting ending.
- Cleared automatically when Dad leaves home or at 23:00
- Guard: a template reload briefly makes sensors `unavailable`, which once falsely triggered DND. The triggers use `not_from: [unavailable, unknown]`.

## Fun statuses
`input_select.dad_status`: None / Coffee / Lonely / Snacks / DnD. Shown on the faces ("Wants coffee ☕") and on the door sign, and reset after 30 minutes, on leaving home, or at 23:00.

## D&D mode (Roll20)
When an online Dungeons & Dragons session is open in **any browser tab**, even in the background, the sign says **ADVENTURING** and the faces say "Adventuring 🐉" instead of "On a call", even though the mic is in use.

A HASS.Agent **PowerShell sensor** runs a script (the sensor command box is limited to about 240 characters, so use a file). It only returns `on`/`off`; no window titles leave the PC.

```powershell
# C:\Scripts\roll20.ps1 - is Roll20 open in any browser tab?
Add-Type -AssemblyName UIAutomationClient
$A=[Windows.Automation.AutomationElement]
$tab=New-Object Windows.Automation.PropertyCondition($A::ControlTypeProperty,[Windows.Automation.ControlType]::TabItem)
$hit=$false
foreach($w in $A::RootElement.FindAll(2,[Windows.Automation.Condition]::TrueCondition)){
  try{$p=(Get-Process -Id $w.Current.ProcessId -ErrorAction Stop).ProcessName}catch{continue}
  if($p -notmatch '^(chrome|msedge|firefox|brave|opera|vivaldi)$'){continue}   # browsers only: Electron apps gave false positives
  if($w.Current.Name -match 'Roll20'){$hit=$true;break}
  if($w.FindAll(4,$tab) | ?{ $_.Current.Name -match 'Roll20' }){$hit=$true;break}
}
if($hit){'on'}else{'off'}
```

```yaml
- binary_sensor:
    - name: Dad D&D session
      icon: mdi:dice-d20
      delay_off: "00:00:30"     # back to normal 30 s after the tab closes
      state: "{{ is_state('sensor.office_roll20', 'on') }}"
```

An automation sets the status to `DnD` while the session is on (unless real DND is on) and back to `None` afterwards.
