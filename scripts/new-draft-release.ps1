# Creates (never publishes) a GitHub Draft Release for one retained APK build folder.
# Requires an authenticated GitHub CLI session that the owner created locally (gh auth login).
# A draft is visible only to people with write access to the repository; its tag is not
# created until someone publishes it, which this script never does.
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [ValidateSet("Dev", "LiveCandidate")] [string]$Channel,
    [Parameter(Mandatory)] [int]$VersionCode,
    [string]$Gh = "gh",
    [string]$Repository = "EVENTo0/octopus-S1"
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
$dir = Join-Path $repoRoot "Builds\Android\$Channel\$VersionCode"
$manifest = Get-Content -LiteralPath (Join-Path $dir "build.json") -Raw | ConvertFrom-Json
$apk = Get-ChildItem -LiteralPath $dir -Filter *.apk | Select-Object -First 1
if (-not $apk) { throw "No APK in $dir" }
$digest = (Get-FileHash -LiteralPath $apk.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
if ($digest -ne $manifest.sha256) { throw "APK hash $digest does not match build.json $($manifest.sha256). Not uploading." }
& $Gh auth status --hostname github.com | Out-Null
if ($LASTEXITCODE -ne 0) { throw "GitHub CLI is not signed in. The owner runs: $Gh auth login" }

$tag = if ($Channel -eq "Dev") { "dev-v$($manifest.version)-$VersionCode" } else { "live-candidate-v$($manifest.version)-$VersionCode" }
$notes = Join-Path $dir "release-notes.md"
@"
**$($manifest.label) $($manifest.version) ($VersionCode)** — $Channel test build for the owner. Not a public release, not a store build.

| | |
|---|---|
| Application ID | ``$($manifest.applicationId)`` |
| Source commit | ``$($manifest.commit)`` |
| Unity | $($manifest.unity) |
| APK SHA-256 | ``$($manifest.sha256)`` |
| Size | $($manifest.sizeBytes) bytes |
| Signer SHA-256 | ``$($manifest.signerSha256)`` ($($manifest.signingPolicy)) |
| Debuggable | $($manifest.debuggable) |
| Native code | $($manifest.nativeCode) |
| Minimum Android | 8.0 (API 26), ARM64 |
| Tests | $($manifest.tests -join '<br>') |
| Device tested | $($manifest.deviceTested) |

Install: enable installing unknown apps for your file manager or browser, open the APK, and confirm.
Dev installs beside Live (different application ID) and keeps separate data. Verify the SHA-256 first.
Sideloaded test builds have not been through any store review. Known issues: see OCTOPUS_S1_WORKLOG.md.
"@ | Set-Content -LiteralPath $notes -Encoding UTF8

$files = @($apk.FullName, "$($apk.FullName).sha256", (Join-Path $dir "build.json"), (Join-Path $dir "test-evidence.json")) +
    @(Get-ChildItem -LiteralPath $dir -Filter *.xml | ForEach-Object { $_.FullName })
& $Gh release create $tag --repo $Repository --draft --target $manifest.commit `
    --title "Octopus S1 $Channel $($manifest.version) ($VersionCode) — owner test draft" --notes-file $notes @files
if ($LASTEXITCODE -ne 0) { throw "gh release create failed." }
& $Gh release view $tag --repo $Repository --json url,isDraft
