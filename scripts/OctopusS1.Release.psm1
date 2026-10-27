# Release gates shared by scripts/unity-local.ps1 and tests/release-gates.Tests.ps1.
Set-StrictMode -Version Latest

# Fingerprint domain: the content of every tracked or untracked-but-not-ignored file under
# Assets/, Packages/ and ProjectSettings/ (what Unity compiles, resolves and builds).
# Docs, scripts, logs and build outputs are outside the domain, so documentation-only
# commits do not invalidate a test run. File timestamps are never used.
$script:FingerprintRoots = @("Assets", "Packages", "ProjectSettings")

function Get-UnityInputFingerprint([string]$RepoRoot) {
    $tracked = @(& git -C $RepoRoot ls-files -- $script:FingerprintRoots)
    if ($LASTEXITCODE -ne 0) { throw "git ls-files failed in $RepoRoot" }
    $untracked = @(& git -C $RepoRoot ls-files --others --exclude-standard -- $script:FingerprintRoots)
    $paths = @($tracked + $untracked | Where-Object { $_ } | Sort-Object -Unique)
    $lines = foreach ($path in $paths) {
        $full = Join-Path $RepoRoot $path
        $hash = if (Test-Path -LiteralPath $full -PathType Leaf) {
            (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
        } else { "deleted" }
        "$path`t$hash"
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { $digest = -join ($sha.ComputeHash($bytes) | ForEach-Object { $_.ToString("x2") }) } finally { $sha.Dispose() }
    return [pscustomobject]@{ Fingerprint = $digest; FileCount = $paths.Count; Lines = $lines }
}

function Compare-UnityInputs($Before, $After) {
    if ($Before.Fingerprint -eq $After.Fingerprint) { return @() }
    return @(Compare-Object -ReferenceObject @($Before.Lines) -DifferenceObject @($After.Lines) |
        ForEach-Object { ($_.InputObject -split "`t")[0] } | Sort-Object -Unique)
}

function Read-TestResult([string]$XmlPath, [string]$Platform) {
    if (-not (Test-Path -LiteralPath $XmlPath)) { throw "$Platform test results are missing: $XmlPath" }
    $raw = Get-Content -LiteralPath $XmlPath -Raw
    if (-not $raw) { throw "$Platform test results are empty: $XmlPath" }
    [xml]$report = $raw
    $run = $report.DocumentElement
    $result = [pscustomobject]@{
        Platform = $Platform
        Xml = (Split-Path $XmlPath -Leaf)
        XmlSha256 = (Get-FileHash -LiteralPath $XmlPath -Algorithm SHA256).Hash.ToLowerInvariant()
        Result = $run.GetAttribute("result")
        Total = [int]$run.GetAttribute("total")
        Passed = [int]$run.GetAttribute("passed")
        Failed = [int]$run.GetAttribute("failed")
        Skipped = [int]$run.GetAttribute("skipped")
    }
    if ($result.Total -lt 1) { throw "$Platform ran no tests." }
    if ($result.Result -notlike "Passed*" -or $result.Failed -ne 0) {
        throw "$Platform tests did not pass: result=$($result.Result) failed=$($result.Failed)."
    }
    if ($result.Passed -lt 1) { throw "$Platform reported no passing tests." }
    return $result
}

function Write-TestEvidence([string]$EvidencePath, $Inputs, [string]$UnityVersion, [object[]]$Results, [string]$Commit) {
    [ordered]@{
        schema = 1
        inputsFingerprint = $Inputs.Fingerprint
        inputFiles = $Inputs.FileCount
        unity = $UnityVersion
        commit = $Commit
        results = $Results
        recordedUtc = (Get-Date).ToUniversalTime().ToString("o")
    } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $EvidencePath -Encoding UTF8
}

# Rejects missing, empty, failing, tampered, different-editor or different-source test evidence.
function Assert-TestEvidence([string]$EvidencePath, [string]$LogRoot, $CurrentInputs, [string]$UnityVersion, [string[]]$RequiredPlatforms) {
    if (-not (Test-Path -LiteralPath $EvidencePath)) { throw "No test evidence. Run -Mode test on this source first." }
    $evidence = Get-Content -LiteralPath $EvidencePath -Raw | ConvertFrom-Json
    if ($evidence.unity -ne $UnityVersion) { throw "Tests ran on Unity $($evidence.unity), building with $UnityVersion. Rerun -Mode test." }
    if ($evidence.inputsFingerprint -ne $CurrentInputs.Fingerprint) {
        throw "Unity sources changed since the tests ran (fingerprint mismatch). Rerun -Mode test."
    }
    $checked = @()
    foreach ($platform in $RequiredPlatforms) {
        $recorded = @($evidence.results | Where-Object { $_.Platform -eq $platform })
        if ($recorded.Count -ne 1) { throw "Test evidence has no $platform result." }
        $current = Read-TestResult (Join-Path $LogRoot $recorded[0].Xml) $platform
        if ($current.XmlSha256 -ne $recorded[0].XmlSha256) { throw "$platform result XML changed after it was recorded." }
        $checked += $current
    }
    return $checked
}

# versionCode must exceed every code known for the application ID: retained build folders
# and the committed release register (which also covers builds made on other machines).
function Get-HighestVersionCode([string]$RegisterPath, [string]$ApplicationId, [string]$ChannelRoot) {
    $codes = @(0)
    if (Test-Path -LiteralPath $RegisterPath) {
        $codes += @((Get-Content -LiteralPath $RegisterPath -Raw | ConvertFrom-Json).records |
            Where-Object { $_.applicationId -eq $ApplicationId } | ForEach-Object { [int]$_.versionCode })
    }
    if ($ChannelRoot -and (Test-Path -LiteralPath $ChannelRoot)) {
        $codes += @(Get-ChildItem -LiteralPath $ChannelRoot -Directory | Where-Object { $_.Name -match '^\d+$' } | ForEach-Object { [int]$_.Name })
    }
    return ($codes | Measure-Object -Maximum).Maximum
}

function Add-ReleaseRecord([string]$RegisterPath, $Record) {
    $register = if (Test-Path -LiteralPath $RegisterPath) { Get-Content -LiteralPath $RegisterPath -Raw | ConvertFrom-Json }
                else { [pscustomobject]@{ schema = 1; records = @() } }
    $register.records = @($register.records) + @([pscustomobject]$Record)
    $register | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $RegisterPath -Encoding UTF8
}

Export-ModuleMember -Function Get-UnityInputFingerprint, Compare-UnityInputs, Read-TestResult, Write-TestEvidence,
    Assert-TestEvidence, Get-HighestVersionCode, Add-ReleaseRecord
