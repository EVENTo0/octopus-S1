[CmdletBinding()]
param(
    [ValidateSet("doctor", "test", "build", "all")]
    [string]$Mode = "doctor",
    [ValidateSet("Dev", "LiveCandidate")]
    [string]$Channel,
    [string]$Version,
    [int]$VersionCode,
    [string]$UnityEditor = $env:UNITY_EDITOR
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
$expectedVersion = "6000.6.4f1"
$applicationIds = @{ Dev = "com.evento.octopuss1.dev"; LiveCandidate = "com.evento.octopuss1" }
if (-not $UnityEditor) {
    $UnityEditor = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity not found. Install $expectedVersion in Unity Hub, or pass -UnityEditor with its Unity.exe path."
}
$UnityEditor = (Resolve-Path -LiteralPath $UnityEditor).Path
$android = Join-Path (Split-Path $UnityEditor -Parent) "Data\PlaybackEngines\AndroidPlayer"
$logRoot = Join-Path $repoRoot "Logs"
New-Item -ItemType Directory -Path $logRoot -Force | Out-Null
Import-Module (Join-Path $PSScriptRoot "OctopusS1.Release.psm1") -Force
$editorVersion = "$((Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion)"
$evidencePath = Join-Path $logRoot "test-evidence.json"
$registerPath = Join-Path $repoRoot "releases\release-register.json"
$testPlatforms = @("EditMode", "PlayMode")
if ($Mode -ne "doctor" -and $editorVersion -notlike "$expectedVersion*") {
    throw "Unity at $UnityEditor is $editorVersion; this project requires $expectedVersion."
}

function Invoke-Unity([string[]]$UnityArguments) {
    # Start-Process -Wait is necessary for GUI executables on Windows.
    # ArgumentList is flattened by PowerShell, so quote each argument explicitly.
    $quoted = @($UnityArguments | ForEach-Object { '"' + $_.Replace('"', '\"') + '"' })
    $process = Start-Process -FilePath $UnityEditor -ArgumentList $quoted -Wait -PassThru
    if ($process.ExitCode -ne 0) {
        throw "Unity exited with code $($process.ExitCode). Inspect Logs; license activation may be required in Hub."
    }
}
function Test-AndroidModules {
    foreach ($entry in @("", "SDK", "NDK", "OpenJDK")) {
        $modulePath = if ($entry) { Join-Path $android $entry } else { $android }
        if (-not (Test-Path -LiteralPath $modulePath -PathType Container)) {
            throw "Missing Android module: $modulePath. Add Android Build Support, SDK/NDK and OpenJDK through Unity Hub."
        }
    }
}
function Get-BuildTool([string]$Name) {
    $tool = Get-ChildItem -Path (Join-Path $android "SDK\build-tools") -Recurse -Filter $Name |
        Sort-Object FullName -Descending | Select-Object -First 1
    if (-not $tool) { throw "Android build tool $Name not found under the Unity SDK." }
    return $tool.FullName
}
function Invoke-ProjectTests {
    # Evidence binds the results to the exact Unity inputs and editor (review R1).
    Remove-Item -LiteralPath $evidencePath -Force -ErrorAction SilentlyContinue
    $before = Get-UnityInputFingerprint $repoRoot
    $results = @()
    foreach ($platform in $testPlatforms) {
        $xml = Join-Path $logRoot "$($platform.ToLowerInvariant()).xml"
        Remove-Item -LiteralPath $xml -Force -ErrorAction SilentlyContinue
        Invoke-Unity @("-batchmode", "-nographics", "-projectPath", $repoRoot,
            "-runTests", "-testPlatform", $platform, "-testResults", $xml,
            "-logFile", (Join-Path $logRoot "tests-$($platform.ToLowerInvariant()).log"))
        $results += Read-TestResult $xml $platform
    }
    $after = Get-UnityInputFingerprint $repoRoot
    $changed = @(Compare-UnityInputs $before $after)
    if ($changed.Count -gt 0) { throw "Unity inputs changed while tests ran: $($changed -join ', '). Review, commit, rerun." }
    Write-TestEvidence $evidencePath $after $editorVersion $results "$(& git rev-parse HEAD)".Trim()
    foreach ($r in $results) { Write-Output "$($r.Platform): $($r.Passed)/$($r.Total) passed, $($r.Skipped) skipped." }
    Write-Output "Test evidence: inputs $($after.Fingerprint) ($($after.FileCount) files), Unity $editorVersion."
}
function Get-ApkFacts([string]$Apk) {
    $env:JAVA_HOME = Join-Path $android "OpenJDK"
    $badging = @(& (Get-BuildTool "aapt2.exe") dump badging $Apk)
    if ($LASTEXITCODE -ne 0) { throw "aapt2 could not read $Apk" }
    $package = $badging | Where-Object { $_ -like "package:*" } | Select-Object -First 1
    $certs = @(& (Get-BuildTool "apksigner.bat") verify --print-certs $Apk)
    if ($LASTEXITCODE -ne 0) { throw "apksigner verification failed for $Apk" }
    $certLine = $certs | Where-Object { $_ -like "Signer #1 certificate SHA-256 digest:*" } | Select-Object -First 1
    $dnLine = $certs | Where-Object { $_ -like "Signer #1 certificate DN:*" } | Select-Object -First 1
    $native = $badging | Where-Object { $_ -like "native-code:*" } | Select-Object -First 1
    $label = $badging | Where-Object { $_ -like "application-label:*" } | Select-Object -First 1
    return [ordered]@{
        applicationId = [regex]::Match($package, "name='([^']+)'").Groups[1].Value
        versionCode = [int][regex]::Match($package, "versionCode='(\d+)'").Groups[1].Value
        versionName = [regex]::Match($package, "versionName='([^']+)'").Groups[1].Value
        label = [regex]::Match("$label", "'(.*)'").Groups[1].Value
        debuggable = [bool]($badging | Where-Object { $_ -eq "application-debuggable" })
        nativeCode = if ($native) { $native.Substring(12).Trim() } else { "" }
        signerDn = if ($dnLine) { $dnLine.Split(":", 2)[1].Trim() } else { "" }
        signerSha256 = if ($certLine) { $certLine.Split(":", 2)[1].Trim() } else { "" }
    }
}
function Invoke-AndroidBuild {
    if (-not $Channel) { throw "Pass -Channel Dev or -Channel LiveCandidate." }
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw "Pass -Version as major.minor.patch, e.g. 0.1.0." }
    if ($VersionCode -lt 1) { throw "Pass a positive -VersionCode." }
    Test-AndroidModules
    if ($Channel -eq "LiveCandidate") {
        foreach ($name in "OCTOPUS_LIVE_KEYSTORE", "OCTOPUS_LIVE_KEYSTORE_PASS", "OCTOPUS_LIVE_KEY_ALIAS", "OCTOPUS_LIVE_KEY_PASS") {
            if (-not [Environment]::GetEnvironmentVariable($name)) {
                throw "LiveCandidate needs the owner's Live keystore in $name (see docs/09). Debug signing is refused."
            }
        }
    }
    $commit = "$(& git rev-parse HEAD)".Trim()
    if ($LASTEXITCODE -ne 0) { throw "Could not identify source commit." }
    $sourceChanges = @(& git status --porcelain -- Assets Packages ProjectSettings)
    if ($sourceChanges.Count -gt 0) {
        throw "Review and commit Unity source/import changes first. Do not discard local work. Then rerun."
    }
    # Review R1: the build must use exactly the inputs and editor the recorded tests ran on.
    $inputs = Get-UnityInputFingerprint $repoRoot
    $tests = Assert-TestEvidence $evidencePath $logRoot $inputs $editorVersion $testPlatforms

    # Review R4: versionCode must exceed every code known for this application ID,
    # in the committed release register and in retained local build folders.
    $channelRoot = Join-Path $repoRoot "Builds\Android\$Channel"
    $highest = Get-HighestVersionCode $registerPath $applicationIds[$Channel] $channelRoot
    if ($VersionCode -le $highest) { throw "versionCode $VersionCode is not above the highest known code for $($applicationIds[$Channel]) ($highest)." }
    $outDir = Join-Path $channelRoot $VersionCode
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    $apk = Join-Path $outDir "OctopusS1-$Channel-$Version-$VersionCode.apk"
    $log = Join-Path $logRoot "android-$Channel-$VersionCode.log"
    try {
        Invoke-Unity @("-batchmode", "-nographics", "-quit", "-buildTarget", "Android",
            "-projectPath", $repoRoot, "-executeMethod", "OctopusS1Build.Android",
            "-octopusChannel", $Channel, "-octopusVersion", $Version,
            "-octopusVersionCode", "$VersionCode", "-octopusOutput", $apk, "-logFile", $log)
        # Review R3: the build restores its own settings; any change to Unity inputs is a defect.
        # Nothing is reverted here, so concurrent owner edits are never discarded.
        $changed = @(Compare-UnityInputs $inputs (Get-UnityInputFingerprint $repoRoot))
        if ($changed.Count -gt 0) { throw "Unity inputs changed during the build: $($changed -join ', '). The APK is not attributable; inspect git diff." }
        if (-not (Test-Path -LiteralPath $apk) -or (Get-Item -LiteralPath $apk).Length -eq 0) {
            throw "Unity did not produce an APK."
        }
        $facts = Get-ApkFacts $apk
        $expectedDebuggable = ($Channel -eq "Dev")
        if ($facts.applicationId -ne $applicationIds[$Channel]) { throw "APK id $($facts.applicationId) != $($applicationIds[$Channel])" }
        if ($facts.versionCode -ne $VersionCode -or $facts.versionName -ne $Version) { throw "APK version mismatch: $($facts.versionName) ($($facts.versionCode))" }
        if ($facts.debuggable -ne $expectedDebuggable) { throw "APK debuggable=$($facts.debuggable), expected $expectedDebuggable for $Channel" }
        if ($facts.nativeCode -notmatch "arm64-v8a") { throw "APK has no arm64-v8a native code." }
        if ($Channel -eq "LiveCandidate" -and $facts.signerDn -match "Android Debug") { throw "LiveCandidate is debug-signed." }
    } catch {
        Remove-Item -LiteralPath $outDir -Recurse -Force -ErrorAction SilentlyContinue
        throw
    }
    $digest = (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash.ToLowerInvariant()
    "$digest  $(Split-Path $apk -Leaf)" | Set-Content -LiteralPath "$apk.sha256" -Encoding ASCII
    # Keep the exact test evidence next to the APK.
    Copy-Item -LiteralPath $evidencePath -Destination (Join-Path $outDir "test-evidence.json")
    foreach ($t in $tests) { Copy-Item -LiteralPath (Join-Path $logRoot $t.Xml) -Destination (Join-Path $outDir $t.Xml) }
    $manifest = [ordered]@{
        channel = $Channel
        applicationId = $facts.applicationId
        label = $facts.label
        version = $facts.versionName
        versionCode = $facts.versionCode
        commit = $commit
        unity = $editorVersion
        unityInputsFingerprint = $inputs.Fingerprint
        sha256 = $digest
        sizeBytes = (Get-Item -LiteralPath $apk).Length
        debuggable = $facts.debuggable
        nativeCode = $facts.nativeCode
        signerDn = $facts.signerDn
        signerSha256 = $facts.signerSha256
        signingPolicy = if ($Channel -eq "Dev") { "build host Android debug key (Dev test policy)" } else { "owner Live keystore" }
        tests = @($tests | ForEach-Object { "$($_.Platform) $($_.Passed)/$($_.Total) passed, $($_.Skipped) skipped, xml sha256 $($_.XmlSha256)" })
        deviceTested = $false
        builtUtc = (Get-Date).ToUniversalTime().ToString("o")
    }
    $manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir "build.json") -Encoding UTF8
    Add-ReleaseRecord $registerPath ([ordered]@{
        channel = $Channel; applicationId = $facts.applicationId; version = $facts.versionName
        versionCode = $facts.versionCode; commit = $commit; sha256 = $digest; signerSha256 = $facts.signerSha256
        builtOn = "owner Windows PC"; status = "built; not yet distributed" })
    Write-Output "APK: $apk"
    Write-Output "SHA-256: $digest"
    Write-Output "Signer SHA-256: $($facts.signerSha256)"
    Write-Output "Release register updated: commit releases/release-register.json."
}
switch ($Mode) {
    "doctor" {
        Write-Output "Unity executable: $UnityEditor"
        Write-Output "Required version: $expectedVersion"
        Write-Output "Installed product version: $((Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion)"
        Test-AndroidModules
        Write-Output "Android module directories exist. This does not verify license activation or SDK health."
        Write-Output "Next: run test, then build -Channel Dev -Version x.y.z -VersionCode n."
    }
    "test" { Invoke-ProjectTests }
    "build" { Invoke-AndroidBuild }
    "all" { Invoke-ProjectTests; Invoke-AndroidBuild }
}
