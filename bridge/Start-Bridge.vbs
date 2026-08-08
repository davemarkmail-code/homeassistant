Option Explicit
' Start-Bridge.vbs
' ----------------
' Launches the bridge with NO console window.
'
' Put this file (or a shortcut to it) in your Startup folder:
'     Win+R  ->  shell:startup
'
' Use an ABSOLUTE path below. It's tempting to make this self-locating with
' WScript.ScriptFullName, but then copying it into the Startup folder breaks it,
' because it would resolve relative to Startup instead of the bridge folder.

Dim shell, command
Set shell = CreateObject("WScript.Shell")

command = "powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden " & _
          "-File ""C:\path\to\bridge\Run-Bridge.ps1"""

shell.Run command, 0, False
