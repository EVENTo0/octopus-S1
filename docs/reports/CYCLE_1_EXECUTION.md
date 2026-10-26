# Cycle 1 execution report — OS1-LOCAL-001 / Revision 1 (archived)

Archived verbatim from OCTOPUS_S1_WORKLOG.md (report commit `95844ca`) when cycle 2 started.

**Order/Revision executed:** OS1-LOCAL-001 / Revision 1 — cycle 1 (in progress, slice 1–6 partially done)  
**Agent/session and UTC:** Claude Code (local, owner's Windows 11 PC), 2026-10-26 ~18:00–19:40 UTC  
**Windows project path:** `C:\Users\octa1\Desktop\projects octa\saeed game battle royal\octopus S1`
(fresh clone of the branch; separate from the owner's other projects in the parent folder)  
**Branch + code SHA:** `codex/octopus-s1-charter-and-prototype`; Dev APK source `bf8d6ec`;
build-tool fix `7583b41` (no gameplay change). Remote order commit `581e1db` merged (`487e0b2`), no conflicts.  
**Working tree / remote synchronization:** clean before each build; pushed to origin (no force-push, no PR merge).  
**Host/editor/modules/license:** **Unity 6000.6.4f1** (`12bfff696524`) — owner decision in chat:
use the already installed and licensed editor instead of 6000.3.25f1 (not installed; its install
needed UAC + module EULAs). This supersedes the 6000.3.25f1 pin in AGENTS/EMPIRE for this host.
Android Build Support, SDK (build-tools 36.0.0, platform 36), NDK, OpenJDK present (accepted earlier in Hub
by the owner). Unity Personal license active, signed in as the owner. `doctor` passed.  
**Changed behavior / files:**
- Version pin → 6000.6.4f1 (ProjectVersion, build guard, scripts, workflow label).
- `MissionTests` teardown hang fixed: it unloaded the runner's InitTestScene, so the run never wrote results.
- First import generated ProjectSettings + `packages-lock.json` (test-framework 1.8.0); reviewed and committed.
  `activeInputHandler: 0` (legacy Input), matching the prototype.
- `OctopusS1Prototype.cs` extended (not rewritten): Menu/Help/Settings/Pause/Result states; Android back;
  multi-touch left joystick + right camera drag + Interact/Scan/Pause buttons in the safe area;
  rejection feedback; fixed double-count of a cell in one frame; companion without colliders;
  invisible boundary + out-of-bounds recovery + "return to start"; pause on background/focus loss;
  restart rebuilds a fresh world; procedural tones; AR/EN text.
- New: `OctopusS1SaveData.cs` (schema 1 JSON in PlayerPrefs, corrupt/unknown → reset, bad values repaired),
  `ArabicText.cs` (Arabic presentation-form shaping + RTL line ordering for IMGUI), `OctopusS1Text.cs`.
- Channels: `OctopusS1Build.Android` requires `-octopusChannel Dev|LiveCandidate`, `-octopusVersion`,
  `-octopusVersionCode`, `-octopusOutput`; identity applied after scene generation; Live refuses debug
  signing (keystore only from `OCTOPUS_LIVE_*` env vars). Cloud hook reads `OCTOPUS_*` env vars, no BuildPlayer.
  `unity-local.ps1 -Channel/-Version/-VersionCode` writes `Builds/Android/<Channel>/<versionCode>/`,
  refuses non-increasing versionCode, verifies the APK with aapt2 + apksigner, writes `build.json` + `.sha256`.
  `scripts/new-live-keystore.ps1`: owner-run keytool helper, keystore kept outside the repo.  
**Commands actually run + exit codes:**
`unity-local.ps1 -Mode doctor` 0 · `-Mode test` 1st run hung (killed, root cause above) then 0 ·
prototype `-Mode build` 0 · `-Mode test` (7 tests) 0 · `-Mode build -Channel Dev -Version 0.1.0 -VersionCode 1` 0 ·
`-Channel LiveCandidate` without keystore → refused (expected) · Dev versionCode 1 again → refused (expected).  
**Unity import + package resolution:** succeeded on 6000.6.4f1; no compiler errors or warnings in `Logs/tests.log`.  
**Tests + counts + XML evidence:** PlayMode 7/7 passed, 0 skipped (`Logs/playmode.xml`, local, not committed):
MissionRequiresCellsRelayAndExtraction, CellCountsOnceEvenWhenCheckedTwiceInOneFrame,
RestartGivesAFreshMissionAfterSuccess, FallingOutOfTheWorldReturnsToStart, TouchJoystickAndButtons,
SaveDataRecoversFromCorruptionAndBadValues, ArabicIsShapedAndOrderedForDisplay.
Python setup tests not run: Python is not installed on this host.  
**Dev artifact + SHA-256:** `Builds/Android/Dev/1/OctopusS1-Dev-0.1.0-1.apk` (23,556,696 bytes) —
SHA-256 `82df86f49a3d5500fe7aa091a1c9216a58f6818e29cd821b433b9e2ced92e84b`.
aapt2/apksigner: `com.evento.octopuss1.dev`, label "Octopus S1 Dev", 0.1.0 (1), minSdk 26, targetSdk 36,
debuggable, `arm64-v8a`, signer "CN=Android Debug" SHA-256 `f3e9dc66…52676bd7` (this host's debug key = Dev test policy).
Earlier single-channel prototype APK (`f3f045a`, id `com.evento.octopuss1`, debug-signed,
SHA-256 `1e5724b1…7f84b8b5`) is superseded and must not be distributed as Live.  
**Live candidate + SHA-256:** not produced — blocked on the owner's Live keystore (by design).  
**Device results:** not run — `adb devices` shows no connected device. G3 open. Arabic glyph rendering
(system-font fallback for presentation forms) and touch feel are unverified on a phone.  
**Observed charges/quota:** none incurred by this cycle (local hardware only, no Cloud build started);
the account's total Cloud charges from earlier attempts were not inspected — unknown, not zero.  
**Blocker / required owner action:**
1. GitHub Draft Release: GitHub CLI install is waiting for a Windows admin (UAC) prompt; then `gh auth login` by the owner.
2. LiveCandidate: owner runs `scripts/new-live-keystore.ps1` and keeps the key + backup offline.
3. G3: connect an authorized Android ARM64 phone with USB debugging (owner accepts the prompt).  
**Recommended next action:** install Dev APK on the owner's phone and run the device checklist; then LiveCandidate.  
**Handoff commit:** recorded in the cycle history after push.
