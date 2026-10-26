# Octopus S1 — Shared command center / ملف العمل المشترك

**Owner:** EVENTO  
**Repository:** https://github.com/EVENTo0/octopus-S1  
**Working branch:** `codex/octopus-s1-charter-and-prototype` — Draft PR #1  
**Budget:** USD 20 total for this phase, shared across attempts. Remaining billed balance: unknown.  
**Host:** Owner's Windows PC via local Codex/Claude Code; not connected to this ChatGPT session.  
**Master brief:** [EMPIRE_PROMPT.md](EMPIRE_PROMPT.md)  
**Channel contract:** [Dev / Live](docs/09_DEV_LIVE_RELEASES_AR.md)

## Synchronization protocol

This file lives in the local checkout and on GitHub. It is not a real-time messaging service.
ChatGPT updates the work-order/review sections when invoked; Claude Code executes locally
and updates the result/evidence sections. Neither must claim to have contacted the other directly.

At session start:
1. Read local AGENTS/CLAUDE instructions, git status and this file before fetching changes.
2. Fetch the working branch; compare remote orders and source. Integrate without losing local edits.
   Do not pull/rebase blindly into a dirty checkout or force-push over another agent.
3. If local work is uncommitted, save it safely with a scoped commit or another documented
   reversible method. Resolve overlapping edits intentionally.
4. Check Order ID, Revision and base source SHA. A newer order invalidates conflicting future
   actions, not previously collected evidence. Pause and report actual scope conflicts.
5. At cycle end, write results and commit/push. If unable to push, report the local commit and
   pending synchronization; do not claim the other agent has read it.

Files are synchronized only after fetch/integration. No continuous unattended agent or polling
service has been installed. The owner can ask ChatGPT to review the latest local-agent report.

## Current evidence snapshot — recorded 2026-10-26

This section is a dated snapshot; use later verified evidence when available.

- Existing game: Unity 6000.3.25f1, runtime blockout, three-cell/relay/extraction mission,
  scene generator, PlayMode source tests, local Linux/Windows and Cloud build entry points.
- Standalone C# reference compilation passed for runtime/Editor; this is not Unity import/playtest.
- Cloud #1 failed at source checkout. Cloud #2 checked out the requested commit but failed
  package resolution before test assertions ran.
- Invalid `com.unity.modules.inputlegacy` dependency was removed; the setup test was corrected.
  Latest reviewed evidence document: `docs/07_CLOUD_RETRY_EVIDENCE_AR.md`.
  A corrected Cloud attempt was started; its final result is not recorded in this snapshot.
- Windows runner syntax was checked on Linux; Windows execution and the owner's Unity
  entitlement/device have not been verified by this session.
- APK proven by the reviewed evidence: **none**. G2/G3: **open**.
- Two-channel build support: **specified, not implemented/verified**.
- Approved Live: **none**. Dev/Live coexistence: **not tested**.
- Temporary Cloud credentials may exist according to the latest Cloud work.
  Check current evidence/owner before another Cloud attempt; do not expose secret values.

## CHATGPT — Active work order

**Order ID:** OS1-LOCAL-001  
**Revision:** 1  
**Status:** READY_FOR_LOCAL_AGENT  
**Base source:** `8b1a5b5671d820d4b9e43869445200760676fa58` — verify and fetch newer instructions before work.  
**Objective:** Open/import the real project on Windows, clear verified build blockers, then
advance toward a touch-playable Dev APK and an independently installed Live candidate.

**First executable slice:**
1. Synchronize the checkout and inspect whether newer local/Cloud results supersede the snapshot.
2. Run Windows host diagnosis; verify actual Unity version, matching Android modules and activation.
3. Import/resolve packages, fix actual compilation errors, run PlayMode with failure gates intact.
4. If an engine/activation dependency blocks work, finish independent channel design/test preparation
   and record the one exact owner action required; do not merely repeat old blockers.
5. Once import/tests pass, implement Dev/Live build selection and artifact verification according to
   the channel contract; then work through the complete mission acceptance checklist in the master brief.
6. Build a Dev test APK; deliver it to the recorded test channel with evidence. Build and test a separate
   non-development Live candidate before requesting Live acceptance.

**Allowed scope:** Existing Unity project, necessary gameplay/input/UI fixes for the first mission,
meaningful tests, channel-aware build tooling, release records, original assets and documentation.
No full rewrite, automatic merge, store submission or unrelated new product.

**Test delivery preference:** OWNER_LOCAL_AND_GITHUB_DRAFT — explicitly selected by the owner.
Deliver APK on the owner's Windows PC plus a GitHub Draft Release accessible to the owner
with appropriate repository permissions. No public prerelease/release or public artifact upload.
If release-write access is unavailable, deliver the local APK/manifest and record that specific
remote-delivery blocker; do not block local production or request broad account credentials.
This destination does not imply any APK has already been built.

**Acceptance:** Evidence of actual Unity import/tests and playable mission, accurate artifact
metadata, and Dev/Live isolation. No Live acceptance without the owner's concrete release decision.

## CLAUDE CODE / LOCAL CODEX — Execution result

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

Replace the result fields with real results; keep older cycle summaries in the log below.
Never paste credentials, private signing data or raw unsanitized logs here.

## CHATGPT — Review and next decision

**Reviewed result ID/commit:** none  
**Decision:** awaiting actual local execution report  
**Required corrections:** none issued yet  
**Next order/revision:** issue after reviewing evidence; do not predeclare tests successful

The local agent may continue the active authorized plan without waiting for a new ChatGPT
message after every routine successful step. Acceptance of a Live candidate remains a distinct
owner decision.

## Release register

| Channel / version | Source SHA | APK SHA-256 / link | Device evidence | Owner acceptance |
|---|---|---|---|---|
| Dev 0.1.0 (1) `com.evento.octopuss1.dev` | `bf8d6ec` | `82df86f49a3d5500fe7aa091a1c9216a58f6818e29cd821b433b9e2ced92e84b` — owner PC `Builds/Android/Dev/1/`; Draft Release pending | Not tested on device | Not required to iterate |
| Live candidate | — | Not produced (needs owner Live keystore) | Not tested | Pending |
| Approved Live | — | No release | None | None |

Do not replace an accepted row in place. Append each version, its immutable artifact and source.
Keep build/source SHA separate from a later documentation/report commit.

## Cycle history

- 2026-10-26 — Workflow established: master prompt, shared command center and Dev/Live contract.
  Owner selected local APK + GitHub Draft Release first.
  Documentation only; no two-channel APK or local gameplay success claimed.
- 2026-10-26 — Cycle 1 (Claude Code, Windows): first real Unity import (6000.6.4f1 by owner decision),
  PlayMode teardown hang fixed, first-mission UI/touch/save/AR-EN slice, Dev/LiveCandidate build contract
  with APK inspection. Dev 0.1.0 (1) built from `bf8d6ec`; 7/7 PlayMode tests. No device test, no Live candidate.
