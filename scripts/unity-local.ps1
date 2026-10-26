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
function Get-PlayModeResult {
    $resultPath = Join-Path $logRoot "playmode.xml"
    if (-not (Test-Path -LiteralPath $resultPath)) { return $null }
    [xml]$report = Get-Content -LiteralPath $resultPath -Raw
    return $report.DocumentElement
}
function Invoke-ProjectTests {
    $resultPath = Join-Path $logRoot "playmode.xml"
    Remove-Item -LiteralPath $resultPath -Force -ErrorAction SilentlyContinue
    Invoke-Unity @("-batchmode", "-nographics", "-projectPath", $repoRoot,
        "-runTests", "-testPlatform", "PlayMode", "-testResults", $resultPath,
        "-logFile", (Join-Path $logRoot "tests.log"))
    $result = Get-PlayModeResult
    if (-not $result) { throw "Unity produced no test results." }
    if ($result.GetAttribute("result") -ne "Passed" -or
        [int]$result.GetAttribute("total") -lt 1 -or
        [int]$result.GetAttribute("failed") -ne 0) {
        throw "PlayMode tests failed, or no tests ran. Inspect Logs/playmode.xml."
    }
    Write-Output "Unity PlayMode tests passed: $($result.GetAttribute('passed'))/$($result.GetAttribute('total'))."
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
    $tests = Get-PlayModeResult
    if (-not $tests -or $tests.GetAttribute("result") -ne "Passed") {
        throw "Run -Mode test successfully before building."
    }
    $commit = "$(& git rev-parse HEAD)".Trim()
    if ($LASTEXITCODE -ne 0) { throw "Could not identify source commit." }
    $sourceChanges = @(& git status --porcelain -- Assets Packages ProjectSettings)
    if ($sourceChanges.Count -gt 0) {
        throw "Review and commit Unity source/import changes first. Do not discard local work. Then rerun."
    }

    # versionCode must increase per application ID; existing build folders are never overwritten.
    $channelRoot = Join-Path $repoRoot "Builds\Android\$Channel"
    $existing = @(Get-ChildItem -LiteralPath $channelRoot -Directory -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -match '^\d+$' } | ForEach-Object { [int]$_.Name })
    if ($existing.Count -gt 0) {
        $highest = ($existing | Measure-Object -Maximum).Maximum
        if ($VersionCode -le $highest) { throw "versionCode $VersionCode is not above the highest existing $Channel build ($highest)." }
    }
    $outDir = Join-Path $channelRoot $VersionCode
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    $apk = Join-Path $outDir "OctopusS1-$Channel-$Version-$VersionCode.apk"
    $log = Join-Path $logRoot "android-$Channel-$VersionCode.log"
    try {
        Invoke-Unity @("-batchmode", "-nographics", "-quit", "-buildTarget", "Android",
            "-projectPath", $repoRoot, "-executeMethod", "OctopusS1Build.Android",
            "-octopusChannel", $Channel, "-octopusVersion", $Version,
            "-octopusVersionCode", "$VersionCode", "-octopusOutput", $apk, "-logFile", $log)
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
    } finally {
        # The tree was verified clean before the build, so this only drops build-time
        # PlayerSettings (channel identity, host keystore path) that Unity wrote back.
        & git restore -- ProjectSettings/ProjectSettings.asset
    }
    $digest = (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash.ToLowerInvariant()
    "$digest  $(Split-Path $apk -Leaf)" | Set-Content -LiteralPath "$apk.sha256" -Encoding ASCII
    [ordered]@{
        channel = $Channel
        applicationId = $facts.applicationId
        label = $facts.label
        version = $facts.versionName
        versionCode = $facts.versionCode
        commit = $commit
        unity = $expectedVersion
        sha256 = $digest
        debuggable = $facts.debuggable
        nativeCode = $facts.nativeCode
        signerDn = $facts.signerDn
        signerSha256 = $facts.signerSha256
        signingPolicy = if ($Channel -eq "Dev") { "build host Android debug key (Dev test policy)" } else { "owner Live keystore" }
        playModeTests = "$($tests.GetAttribute('passed'))/$($tests.GetAttribute('total')) passed"
        deviceTested = $false
        builtUtc = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $outDir "build.json") -Encoding UTF8
    Write-Output "APK: $apk"
    Write-Output "SHA-256: $digest"
    Write-Output "Signer SHA-256: $($facts.signerSha256)"
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
