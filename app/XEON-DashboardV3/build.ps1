#Requires -Version 5.1
<#
    XEON Dashboard — build script.

    Produces:
      1) A portable, self-contained single .exe (no .NET install needed).
      2) An installer (setup.exe) if Inno Setup is available.

    Usage (from the repo root):
        .\build.ps1                # portable exe + installer (if Inno present)
        .\build.ps1 -PortableOnly  # just the single exe

    Neither output needs the user to install .NET. WebView2 is auto-provisioned
    at first run (and by the installer). See Docs\PACKAGING.md.
#>
[CmdletBinding()]
param(
    [switch]$PortableOnly
)

$ErrorActionPreference = 'Stop'
$root    = Split-Path -Parent $MyInvocation.MyCommand.Path
$proj    = Join-Path $root 'src\XeonDashboard\XeonDashboard.csproj'
$publish = Join-Path $root 'publish'

Write-Host '==> Publishing self-contained build...' -ForegroundColor Cyan
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }

# Folder publish that the installer will package (self-contained; no .NET needed).
dotnet publish $proj -c Release -r win-x64 --self-contained true `
    -p:PublishSingleFile=false -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish (installer payload) failed.' }

Write-Host '==> Publishing portable single-file exe...' -ForegroundColor Cyan
dotnet publish $proj /p:PublishProfile=win-x64-single
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish (portable) failed.' }

$portable = Join-Path $root 'src\XeonDashboard\bin\Release\net8.0-windows10.0.19041.0\win-x64\portable\XeonDashboard.exe'
if (Test-Path $portable) {
    Write-Host ("    Portable exe: {0}" -f $portable) -ForegroundColor Green
}

if ($PortableOnly) { Write-Host 'Done (portable only).' -ForegroundColor Green; return }

# Locate the Inno Setup compiler.
$iscc = $null
foreach ($p in @(
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe")) {
    if (Test-Path $p) { $iscc = $p; break }
}

if (-not $iscc) {
    Write-Warning 'Inno Setup 6 not found — skipping installer.'
    Write-Host 'Install it from https://jrsoftware.org/isdl.php, then re-run build.ps1.'
    return
}

Write-Host '==> Compiling installer...' -ForegroundColor Cyan
& $iscc (Join-Path $root 'Installer\XeonDashboard.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup compilation failed.' }

$setup = Join-Path $root 'Installer\Output\XEON-Dashboard-Setup.exe'
if (Test-Path $setup) {
    Write-Host ("    Installer: {0}" -f $setup) -ForegroundColor Green
}
Write-Host 'Done.' -ForegroundColor Green
