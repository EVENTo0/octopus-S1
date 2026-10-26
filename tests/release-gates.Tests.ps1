# Tests for scripts/OctopusS1.Release.psm1 (review R1/R4). Needs only git and Windows PowerShell.
# Run: powershell -NoProfile -File tests\release-gates.Tests.ps1
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
Import-Module (Join-Path $PSScriptRoot "..\scripts\OctopusS1.Release.psm1") -Force

$script:failures = 0
$script:count = 0
function Check([string]$Name, [scriptblock]$Body) {
    $script:count++
    try { & $Body; Write-Output "PASS $Name" }
    catch { $script:failures++; Write-Output "FAIL $Name -- $($_.Exception.Message)" }
}
function Expect-Throw([string]$Pattern, [scriptblock]$Body) {
    try { & $Body } catch {
        if ($_.Exception.Message -match $Pattern) { return }
        throw "threw '$($_.Exception.Message)', expected /$Pattern/"
    }
    throw "did not throw (expected /$Pattern/)"
}
function Expect([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }

$root = Join-Path ([IO.Path]::GetTempPath()) ("octopus-gates-" + [Guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory -Path $root | Out-Null
try {
    # A tiny repository with the same layout as the Unity project.
    $repo = Join-Path $root "repo"
    New-Item -ItemType Directory -Path "$repo\Assets", "$repo\Packages", "$repo\ProjectSettings", "$repo\docs", "$repo\Logs" | Out-Null
    Set-Content "$repo\Assets\Game.cs" "class Game {}"
    Set-Content "$repo\Packages\manifest.json" "{}"
    Set-Content "$repo\ProjectSettings\ProjectVersion.txt" "m_EditorVersion: 6000.6.4f1"
    Set-Content "$repo\docs\notes.md" "notes"
    Set-Content "$repo\.gitignore" "Logs/`nAssets/ignored.tmp"
    & git -C $repo init -q
    & git -C $repo add -A
    & git -C $repo -c user.name=test -c user.email=test@example.invalid commit -q -m init

    $base = Get-UnityInputFingerprint $repo

    Check "fingerprint is stable and covers the three Unity roots" {
        $again = Get-UnityInputFingerprint $repo
        Expect ($again.Fingerprint -eq $base.Fingerprint) "same content gave a different fingerprint"
        Expect ($base.FileCount -eq 3) "expected 3 Unity input files, got $($base.FileCount)"
    }
    Check "documentation and ignored files do not change the fingerprint" {
        Set-Content "$repo\docs\notes.md" "edited notes"
        Set-Content "$repo\Assets\ignored.tmp" "scratch"
        Expect ((Get-UnityInputFingerprint $repo).Fingerprint -eq $base.Fingerprint) "docs/ignored edit changed the fingerprint"
    }
    Check "editing a tracked source changes the fingerprint and is reported" {
        Set-Content "$repo\Assets\Game.cs" "class Game { int x; }"
        $after = Get-UnityInputFingerprint $repo
        Expect ($after.Fingerprint -ne $base.Fingerprint) "source edit not detected"
        $changed = Compare-UnityInputs $base $after
        Expect (($changed -join ",") -eq "Assets/Game.cs") "unexpected change list: $($changed -join ',')"
        Set-Content "$repo\Assets\Game.cs" "class Game {}"
    }
    Check "a new untracked source is part of the fingerprint" {
        Set-Content "$repo\Assets\New.cs" "class New {}"
        Expect ((Get-UnityInputFingerprint $repo).Fingerprint -ne $base.Fingerprint) "untracked source not detected"
        Remove-Item "$repo\Assets\New.cs"
        Expect ((Get-UnityInputFingerprint $repo).Fingerprint -eq $base.Fingerprint) "fingerprint did not return to base"
    }

    $logs = "$repo\Logs"
    function Write-Xml([string]$Name, [string]$Result, [int]$Total, [int]$Passed, [int]$Failed) {
        Set-Content "$logs\$Name" "<?xml version=`"1.0`"?><test-run result=`"$Result`" total=`"$Total`" passed=`"$Passed`" failed=`"$Failed`" skipped=`"0`"></test-run>"
    }

    Check "missing, empty, failing and zero-test results are rejected" {
        Expect-Throw "missing" { Read-TestResult "$logs\none.xml" "PlayMode" }
        New-Item -ItemType File "$logs\empty.xml" -Force | Out-Null
        Expect-Throw "empty" { Read-TestResult "$logs\empty.xml" "PlayMode" }
        Write-Xml "zero.xml" "Passed" 0 0 0
        Expect-Throw "ran no tests" { Read-TestResult "$logs\zero.xml" "PlayMode" }
        Write-Xml "failed.xml" "Failed(Child)" 5 4 1
        Expect-Throw "did not pass" { Read-TestResult "$logs\failed.xml" "PlayMode" }
    }

    Write-Xml "editmode.xml" "Passed" 5 5 0
    Write-Xml "playmode.xml" "Passed" 11 11 0
    $results = @((Read-TestResult "$logs\editmode.xml" "EditMode"), (Read-TestResult "$logs\playmode.xml" "PlayMode"))
    $evidence = "$logs\test-evidence.json"
    Write-TestEvidence $evidence $base "6000.6.4f1_12bfff696524" $results "abc"
    $platforms = @("EditMode", "PlayMode")

    Check "matching evidence is accepted" {
        $ok = Assert-TestEvidence $evidence $logs (Get-UnityInputFingerprint $repo) "6000.6.4f1_12bfff696524" $platforms
        Expect (@($ok).Count -eq 2) "expected two verified platforms"
    }
    Check "evidence from a different source is rejected" {
        Set-Content "$repo\Assets\Game.cs" "class Game { /* changed after tests */ }"
        Expect-Throw "fingerprint mismatch" { Assert-TestEvidence $evidence $logs (Get-UnityInputFingerprint $repo) "6000.6.4f1_12bfff696524" $platforms }
        Set-Content "$repo\Assets\Game.cs" "class Game {}"
    }
    Check "evidence from a different editor is rejected" {
        Expect-Throw "Tests ran on Unity" { Assert-TestEvidence $evidence $logs $base "6000.3.25f1_e1dba0a9aba4" $platforms }
    }
    Check "a result XML replaced after recording is rejected" {
        Write-Xml "playmode.xml" "Passed" 12 12 0
        Expect-Throw "changed after it was recorded" { Assert-TestEvidence $evidence $logs $base "6000.6.4f1_12bfff696524" $platforms }
        Write-Xml "playmode.xml" "Passed" 11 11 0
    }
    Check "a required platform missing from the evidence is rejected" {
        Write-TestEvidence $evidence $base "6000.6.4f1_12bfff696524" @($results[1]) "abc"
        Expect-Throw "no EditMode result" { Assert-TestEvidence $evidence $logs $base "6000.6.4f1_12bfff696524" $platforms }
    }
    Check "no evidence at all is rejected" {
        Expect-Throw "No test evidence" { Assert-TestEvidence "$logs\absent.json" $logs $base "6000.6.4f1_12bfff696524" $platforms }
    }

    Check "highest versionCode combines the register and retained build folders" {
        $register = "$root\register.json"
        Add-ReleaseRecord $register ([ordered]@{ applicationId = "com.evento.octopuss1.dev"; versionCode = 7 })
        Add-ReleaseRecord $register ([ordered]@{ applicationId = "com.evento.octopuss1"; versionCode = 30 })
        New-Item -ItemType Directory "$root\Builds\Dev\3", "$root\Builds\Dev\9" | Out-Null
        Expect ((Get-HighestVersionCode $register "com.evento.octopuss1.dev" "$root\Builds\Dev") -eq 9) "folder code 9 should win"
        Expect ((Get-HighestVersionCode $register "com.evento.octopuss1" "$root\Builds\Live") -eq 30) "register code 30 should win"
        Expect ((Get-HighestVersionCode "$root\none.json" "x" "$root\nothing") -eq 0) "no history should give 0"
    }
}
finally {
    Remove-Item -LiteralPath $root -Recurse -Force -ErrorAction SilentlyContinue
}
Write-Output "$($script:count - $script:failures)/$($script:count) release-gate checks passed."
if ($script:failures -gt 0) { exit 1 }
