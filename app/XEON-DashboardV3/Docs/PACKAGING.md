# Packaging & distribution

Goal: users download one thing, run it, and never install a prerequisite by
hand. There are two prerequisites to account for — here's how each is removed.

## The two prerequisites

**1. .NET 8 runtime — eliminated by self-contained publishing.**
Every build here uses `--self-contained true`, which bundles the .NET runtime
inside the app. Users do **not** need .NET installed.

**2. WebView2 Runtime — auto-provisioned.**
WebView2 ships by default on Windows 11 and current Windows 10, so most machines
already have it. For the rest, the app handles it automatically:

- At first run, `WebView2RuntimeService` checks whether the runtime is present.
- If it's missing **and** you bundled the Evergreen bootstrapper (below), the app
  installs it silently — no clicks, no admin.
- If it's missing and no bootstrapper was bundled, the app opens Microsoft's
  download page as a friendly fallback.
- The **installer** also installs WebView2 silently if it's missing, so the
  installed experience is guaranteed prerequisite-free.

### Bundling the WebView2 bootstrapper (recommended, ~2 MB)

Download the **Evergreen Bootstrapper** from
<https://developer.microsoft.com/microsoft-edge/webview2/> (the small
"Evergreen Bootstrapper" download), and place `MicrosoftEdgeWebView2Setup.exe`:

- next to `XeonDashboard.csproj` (it's copied into the app output automatically), and/or
- next to `Installer\XeonDashboard.iss` (the installer picks it up).

With it bundled, a machine that lacks WebView2 provisions it on its own the
first time the app runs. (This step needs a brief internet connection once.)
For a truly offline guarantee, use the **fixed-version** WebView2 distribution
instead — a larger (~180 MB) copy of the runtime shipped inside the app; see the
"Fixed Version" section of Microsoft's WebView2 distribution docs.

## Building

From the repo root, one command does everything:

```powershell
.\build.ps1                 # portable single exe + installer (if Inno Setup present)
.\build.ps1 -PortableOnly   # just the single exe
```

### Output A — portable single exe (simplest for users)

`src\XeonDashboard\bin\Release\net8.0-windows10.0.19041.0\win-x64\portable\XeonDashboard.exe`

One file, ~70 MB, no install — download and double-click. Great for a forum
post: "download, run, done."

### Output B — installer (the "nice packed app")

`Installer\Output\XEON-Dashboard-Setup.exe`

A no-admin `setup.exe` that installs to the user's Program Files, adds a Start
Menu entry, offers a "start with Windows" option, ensures WebView2, and provides
a clean uninstall. Requires [Inno Setup 6](https://jrsoftware.org/isdl.php)
installed to compile (free).

## One honest caveat: SmartScreen

Any unsigned Windows app — exe or installer — will trigger a SmartScreen
"unknown publisher" warning the first time it runs (users click *More info →
Run anyway*). This isn't specific to this app; it's how Windows treats software
without a **code-signing certificate**. To remove the warning entirely you'd buy
an OV/EV code-signing certificate (roughly $100–400/year) and sign both the exe
and the installer with `signtool`. Worth it if you distribute widely; optional
for a forum release, but mention it in your post so people aren't alarmed.
