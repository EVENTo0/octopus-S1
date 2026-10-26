[CmdletBinding()]
param(
    [ValidateSet("doctor", "test", "build", "all")]
    [string]$Mode = "doctor",
    [string]$UnityEditor = $env:UNITY_EDITOR
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path $PSScriptRoot -Parent
Set-Location $repoRoot
$expectedVersion = "6000.3.25f1"
if (-not $UnityEditor) {
    $UnityEditor = Join-Path $env:ProgramFiles "Unity\Hub\Editor\$expectedVersion\Editor\Unity.exe"
}
if (-not (Test-Path -LiteralPath $UnityEditor -PathType Leaf)) {
    throw "Unity not found. Install $expectedVersion in Unity Hub, or pass -UnityEditor with its Unity.exe path."
}
$UnityEditor = (Resolve-Path -LiteralPath $UnityEditor).Path
$android = Join-Path (Split-Path $UnityEditor -Parent) "Data\PlaybackEngines\AndroidPlayer"
$logRoot = Join-Path $repoRoot "Logs"
$buildRoot = Join-Path $repoRoot "Builds\Android"
New-Item -ItemType Directory -Path $logRoot, $buildRoot -Force | Out-Null

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
function Invoke-ProjectTests {
    $resultPath = Join-Path $logRoot "playmode.xml"
    Remove-Item -LiteralPath $resultPath -Force -ErrorAction SilentlyContinue
    Invoke-Unity @("-batchmode", "-nographics", "-projectPath", $repoRoot,
        "-runTests", "-testPlatform", "PlayMode", "-testResults", $resultPath,
        "-logFile", (Join-Path $logRoot "tests.log"))
    if (-not (Test-Path -LiteralPath $resultPath)) { throw "Unity produced no test results." }
    [xml]$report = Get-Content -LiteralPath $resultPath -Raw
    $result = $report.DocumentElement
    if ($result.GetAttribute("result") -ne "Passed" -or
        [int]$result.GetAttribute("total") -lt 1 -or
        [int]$result.GetAttribute("failed") -ne 0) {
        throw "PlayMode tests failed, or no tests ran. Inspect Logs/playmode.xml."
    }
    Write-Output "Unity PlayMode tests passed."
}
function Invoke-AndroidBuild {
    Test-AndroidModules
    $commit = & git rev-parse HEAD
    if ($LASTEXITCODE -ne 0) { throw "Could not identify source commit." }
    $sourceChanges = @(& git status --porcelain -- Assets Packages ProjectSettings)
    if ($LASTEXITCODE -ne 0) { throw "Could not inspect Unity sources." }
    if ($sourceChanges.Count -gt 0) {
        throw "Review and commit Unity source/import changes first. Do not discard local work. Then rerun."
    }
    $apk = Join-Path $buildRoot "OctopusS1-0.1.0.apk"
    $manifest = Join-Path $buildRoot "OctopusS1-0.1.0.build.json"
    Remove-Item -LiteralPath $apk, "$apk.sha256", $manifest -Force -ErrorAction SilentlyContinue
    Invoke-Unity @("-batchmode", "-nographics", "-quit", "-buildTarget", "Android",
        "-projectPath", $repoRoot, "-executeMethod", "OctopusS1Build.Android",
        "-logFile", (Join-Path $logRoot "android.log"))
    if (-not (Test-Path -LiteralPath $apk) -or (Get-Item -LiteralPath $apk).Length -eq 0) {
        throw "Unity did not produce an APK."
    }
    $digest = (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash.ToLowerInvariant()
    "$digest  $(Split-Path $apk -Leaf)" | Set-Content -LiteralPath "$apk.sha256" -Encoding ASCII
    [ordered]@{
        commit = "$commit".Trim()
        unity = $expectedVersion
        version = "0.1.0"
        versionCode = 1
        sha256 = $digest
        development = $true
        signing = "local debug keystore"
        deviceTested = $false
    } | ConvertTo-Json | Set-Content -LiteralPath $manifest -Encoding UTF8
    Write-Output "APK: $apk"
    Write-Output "SHA-256: $digest"
}
switch ($Mode) {
    "doctor" {
        Write-Output "Unity executable: $UnityEditor"
        Write-Output "Required version: $expectedVersion"
        Write-Output "Installed product version: $((Get-Item -LiteralPath $UnityEditor).VersionInfo.ProductVersion)"
        Test-AndroidModules
        Write-Output "Android module directories exist. This does not verify license activation or SDK health."
        Write-Output "Next: run test, then build. Activate your eligible license in Unity Hub if prompted."
    }
    "test" { Invoke-ProjectTests }
    "build" { Invoke-AndroidBuild }
    "all" { Invoke-ProjectTests; Invoke-AndroidBuild }
}
