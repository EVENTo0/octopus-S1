# Creates the owner's persistent Live signing key OUTSIDE the repository.
# Run it yourself; keytool asks for the passwords interactively. Never commit or share the
# keystore or its passwords. Keep a secure offline backup: losing it means Live updates can no
# longer install over the previous Live version.
[CmdletBinding()]
param(
    [string]$KeystorePath = (Join-Path $env:USERPROFILE ".octopus-s1\octopus-s1-live.keystore"),
    [string]$Alias = "octopus-s1-live",
    [string]$UnityEditor = (Join-Path $env:ProgramFiles "Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe")
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([IO.Path]::GetFullPath($KeystorePath).StartsWith($repoRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Keep the Live keystore outside the repository."
}
if (Test-Path -LiteralPath $KeystorePath) { throw "$KeystorePath already exists. It is never overwritten." }
$keytool = Join-Path (Split-Path $UnityEditor -Parent) "Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe"
if (-not (Test-Path -LiteralPath $keytool)) { throw "keytool not found at $keytool" }
New-Item -ItemType Directory -Path (Split-Path $KeystorePath -Parent) -Force | Out-Null
& $keytool -genkeypair -keystore $KeystorePath -alias $Alias -keyalg RSA -keysize 4096 -validity 10000 `
    -dname "CN=Octopus S1 Live, O=EVENTO"
if ($LASTEXITCODE -ne 0) { throw "keytool failed." }
Write-Output "Created $KeystorePath (alias $Alias). Public fingerprint (safe to record):"
& $keytool -list -v -keystore $KeystorePath -alias $Alias | Select-String "SHA256:"
Write-Output "Before building LiveCandidate in the same PowerShell window, set:"
Write-Output '  $env:OCTOPUS_LIVE_KEYSTORE = "<path above>"; $env:OCTOPUS_LIVE_KEY_ALIAS = "octopus-s1-live"'
Write-Output '  $p = Read-Host -AsSecureString "Keystore password"'
Write-Output '  $env:OCTOPUS_LIVE_KEYSTORE_PASS = [Net.NetworkCredential]::new("", $p).Password'
Write-Output '  $env:OCTOPUS_LIVE_KEY_PASS = $env:OCTOPUS_LIVE_KEYSTORE_PASS   # if you used the same key password'
