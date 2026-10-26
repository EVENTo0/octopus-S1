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

## Initial evidence snapshot — historical, before local cycle 1

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

## Latest reviewed state — 2026-10-26

Review basis: local cycle-1 report at `95844ca12ab54f2ab3ae38667adf1905adc03978`
and its code changes. See [cycle-1 review](docs/10_REVIEW_CYCLE_1_AR.md).
The initial snapshot above is superseded for current decisions.

- Active project/editor pin: Unity 6000.6.4f1, owner-selected installed editor according to the
  local report, consistent with ProjectVersion and build guard. Do not downgrade automatically.
- Local report: import and PlayMode 7/7 passed, Dev APK 0.1.0 (1) produced from
  `bf8d6ecfaacdaa4822a812b57335a07bfd4d98df`; size/hash are preserved below.
- Reviewer checked source/test definitions and ran the two Python setup tests successfully.
  Reviewer has not obtained/rehashed the local APK or XML, run Unity on this Windows host,
  verified the reported Android metadata independently or tested a phone.
- Channel-aware code exists. Dev build is reported; LiveCandidate is not produced.
  Device testing and side-by-side installation/save isolation remain unverified. G2/G3 stay open.
- Review found stale test-result acceptance, continued frame processing after touch Pause,
  and incomplete build/signing-state restoration. These are source-review findings, not
  device-reproduced failures. Address them in the next local cycle.
- First local cycle used existing hardware; prior account-wide Cloud charges remain unknown.
- Owner-first destination remains local APK + GitHub Draft Release; no public distribution.

## CHATGPT — Active work order

**Order ID:** OS1-LOCAL-002

**Revision:** 1

**Status:** READY_FOR_LOCAL_AGENT — supersedes unfinished steps of OS1-LOCAL-001 after a safe checkpoint

**Base source/report:** `95844ca12ab54f2ab3ae38667adf1905adc03978`

**Objective:** Repair the reviewed release/input gates, produce the next Dev APK, deliver it
owner-first, obtain real Android evidence, then prepare a correctly signed Live candidate.

Read [the detailed review and acceptance cases](docs/10_REVIEW_CYCLE_1_AR.md).
Do not rerun already completed host setup or reinstall Unity to follow historical pins.

**Execute in order:**
1. Checkpoint current local work and integrate this order. If a build is running, preserve
   its outcome and avoid concurrent editors/builds in the same checkout.
2. **R1:** bind valid nonempty PlayMode results to actual tested inputs/editor/XML fingerprint.
   Reject stale/mismatched/failing results before building; verify relevant inputs remain stable.
3. **R2:** stop gameplay immediately when touch input changes state from Playing to Paused;
   test multi-touch order, no paused collection/interaction and no stuck input on resume.
4. **R3 before LiveCandidate:** restore all touched build/signing state on success and failure
   without discarding unrelated edits; include keystoreName and diagnostic flags.
5. **R4:** reconcile the current test-framework declaration/resolution with Unity 6000.6.4f1,
   document actual built-in behavior, and account for retained/distributed versionCodes beyond one folder.
6. Run the updated relevant suite; keep exact counts, source fingerprints, XML hash and logs.
   Build Dev **0.1.1** with the next unused increasing versionCode (2 only if no higher code
   exists). Retain Dev 0.1.0 (1) unchanged as historical evidence.
7. Inspect actual APK identity/version/debuggability/ARM64/signature, rehash it, deliver locally
   and create/update a **GitHub Draft Release** for that exact APK/source. Attach manifest,
   checksum and sanitized test evidence. Do not publish it.
8. On an owner-authorized Android ARM64 device, exercise install/upgrade, touch mission,
   pause/focus recovery, AR/EN, persistence and a second run; record measured performance
   and sanitized logcat. Do not delete an old prototype/Live install to conceal a signing mismatch.
9. If the owner supplies the persistent Live key locally, build/test LiveCandidate and
   side-by-side save isolation. Otherwise keep that gate open, complete independent tasks
   and state the specific local owner action once. Never label a candidate Approved.

**Allowed scope:** Existing game fixes/tests/build tooling/evidence; local resources first.
No rewrite, public APK/release, store submission, website deployment, automatic PR merge or Live approval.
The USD 20 ceiling remains phase-wide; do not start another paid Cloud attempt for this order.

**Test delivery preference:** OWNER_LOCAL_AND_GITHUB_DRAFT — unchanged.
A missing installed gh is not necessarily a blocker: use an existing authorized tool/session,
the official portable CLI or the authorized GitHub UI if available. Owner login stays local.
If release access is absent, complete local delivery and report that one remote-delivery dependency.

**Acceptance:** R1–R3 failure-path tests plus a fresh source-linked test run and inspected Dev APK;
private draft/local delivery; a truthful device report or explicit device dependency. Live remains
pending until the exact candidate is tested and the owner accepts its version/fingerprint.

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

**Reviewed result ID/commit:** OS1-LOCAL-001 / Revision 1 / cycle 1;
`95844ca12ab54f2ab3ae38667adf1905adc03978`.

**Decision:** advance with corrections. Local import/test/build progress is recorded as reported
evidence; no independent APK/XML/device verification or Live acceptance is claimed by this review.

**Required corrections:** R1 test provenance, R2 Pause/input state, R3 settings/signing cleanup,
and R4 configuration/version-record consistency. See [review](docs/10_REVIEW_CYCLE_1_AR.md).

**Next order/revision:** OS1-LOCAL-002 / Revision 1, active above.

**Next expected result:** updated tests + Dev 0.1.1, owner-only Draft Release/local APK,
phone results if available, and LiveCandidate readiness. Preserve the previous APK/report.

The preserved execution section above still describes cycle 1 until the local agent starts order 002.
Archive its material results before replacing the execution fields. Do not present an old report
as execution of the new order. Continue independent authorized work without awaiting a new
ChatGPT message after each successful step; Live acceptance remains the owner's decision.

## Release register

| Channel / version | Source SHA | APK SHA-256 / link | Device evidence | Owner acceptance |
|---|---|---|---|---|
| Dev 0.1.0 (1) `com.evento.octopuss1.dev` | `bf8d6ecfaacdaa4822a812b57335a07bfd4d98df` | `82df86f49a3d5500fe7aa091a1c9216a58f6818e29cd821b433b9e2ced92e84b` — owner PC `Builds/Android/Dev/1/`; Draft Release pending | Not tested on device | Not required to iterate |
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

- 2026-10-26 — ChatGPT reviewed cycle 1 at `95844ca` and issued OS1-LOCAL-002 / Revision 1.
  Source review + two Python setup tests only; local APK/XML not independently retrieved.
  Recorded Dev progress; required input/test-provenance/signing-state fixes, new owner-first
  Dev delivery and Android validation. No Live accepted or artifact overwritten.
