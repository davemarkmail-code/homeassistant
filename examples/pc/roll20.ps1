# HASS.Agent PowerShell sensor: is Roll20 open in any browser tab (focused or not)?
# Returns only "on"/"off" - no window titles leave the PC.
# HASS.Agent: PowerShell sensor, command:  powershell -ExecutionPolicy Bypass -File C:\Scripts\roll20.ps1
Add-Type -AssemblyName UIAutomationClient
$A=[Windows.Automation.AutomationElement]
$tab=New-Object Windows.Automation.PropertyCondition($A::ControlTypeProperty,[Windows.Automation.ControlType]::TabItem)
$hit=$false
foreach($w in $A::RootElement.FindAll(2,[Windows.Automation.Condition]::TrueCondition)){
  try{$p=(Get-Process -Id $w.Current.ProcessId -ErrorAction Stop).ProcessName}catch{continue}
  # browsers only - Electron apps (Discord, Teams, VS Code) expose tab-like items and gave false positives
  if($p -notmatch '^(chrome|msedge|firefox|brave|opera|vivaldi)$'){continue}
  if($w.Current.Name -match 'Roll20'){$hit=$true;break}
  if($w.FindAll(4,$tab) | ?{ $_.Current.Name -match 'Roll20' }){$hit=$true;break}
}
if($hit){'on'}else{'off'}
