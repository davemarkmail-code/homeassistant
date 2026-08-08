# Installer

Contains the Inno Setup script that builds the no-admin `setup.exe`.

- `XeonDashboard.iss` — the installer definition.
- Compiled output lands in `Installer\Output\XEON-Dashboard-Setup.exe`.

Build everything (portable exe + installer) from the repo root with `build.ps1`.
See `Docs\PACKAGING.md` for the full story, including WebView2 provisioning and
code-signing notes.
