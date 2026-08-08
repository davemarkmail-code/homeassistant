<#
    Set-Token.ps1
    -------------
    Encrypts an API token to disk using Windows DPAPI, so it never sits in
    plain text in a script or a config file.

    Run this ONCE, interactively, per token you need.

        .\Set-Token.ps1 -Name ha
        .\Set-Token.ps1 -Name vendor -Entropy 'MyDashboard.Vendor.v1'

    IMPORTANT
      * 'CurrentUser' scope binds the file to YOUR Windows account on THIS machine.
        Copying the .dat elsewhere will fail to decrypt. Re-run this on the new box.
      * The entropy string is a SALT, not a comment. Change it and existing tokens
        stop decrypting. Pick one and never touch it again.
      * Add *.dat to .gitignore. Encrypted is not the same as publishable.

    For a Home Assistant token: Profile -> Security -> Long-lived access tokens.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Name,
    [string]$Entropy = 'MyDashboard.HomeAssistant.v1',
    [string]$OutputDirectory = $PSScriptRoot
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Security

$path = Join-Path $OutputDirectory "$Name-token.dat"

if (Test-Path -LiteralPath $path) {
    $answer = Read-Host "$path already exists. Overwrite? (y/N)"
    if ($answer -notmatch '^y') { Write-Host 'Cancelled.'; exit 0 }
}

$secure = Read-Host "Paste the token for '$Name'" -AsSecureString
if ($secure.Length -eq 0) { throw 'No token entered.' }

# SecureString -> plain bytes (in memory only, cleared immediately after)
$bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
try {
    $plain = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    $bytes = [Text.Encoding]::UTF8.GetBytes($plain)
}
finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
}

$entropyBytes = [Text.Encoding]::UTF8.GetBytes($Entropy)
$encrypted = [Security.Cryptography.ProtectedData]::Protect(
    $bytes, $entropyBytes, [Security.Cryptography.DataProtectionScope]::CurrentUser)

[IO.File]::WriteAllBytes($path, $encrypted)

# Scrub the plaintext copy from memory
[Array]::Clear($bytes, 0, $bytes.Length)
Remove-Variable plain -ErrorAction SilentlyContinue

Write-Host "Saved encrypted token to: $path"
Write-Host "Entropy used: $Entropy   (your reader must use the SAME string)"


<#
    ── Reading it back, from any other script ─────────────────────────────────

    function Unprotect-Token {
        param([string]$Path, [string]$Entropy)
        Add-Type -AssemblyName System.Security
        $enc = [IO.File]::ReadAllBytes($Path)
        $ent = [Text.Encoding]::UTF8.GetBytes($Entropy)
        return [Text.Encoding]::UTF8.GetString(
            [Security.Cryptography.ProtectedData]::Unprotect(
                $enc, $ent, [Security.Cryptography.DataProtectionScope]::CurrentUser))
    }

    $token = Unprotect-Token (Join-Path $PSScriptRoot 'ha-token.dat') 'MyDashboard.HomeAssistant.v1'
#>
