# Cycle 2 execution report — OS1-LOCAL-002 / Revision 1 (archived)

Archived verbatim from OCTOPUS_S1_WORKLOG.md (report commit `ae9b31b`) when cycle 3 started.

**Order/Revision executed:** OS1-LOCAL-002 / Revision 1 — cycle 2 (steps 1–7 local part done; 7 remote, 8, 9 blocked on owner actions)  
**Agent/session and UTC:** Claude Code (local, owner's Windows 11 PC), 2026-10-26 ~19:55–20:30 UTC (owner clock 2026-10-27)  
**Previous report:** cycle 1 archived in [docs/reports/CYCLE_1_EXECUTION.md](docs/reports/CYCLE_1_EXECUTION.md).  
**Windows project path:** `C:\Users\octa1\Desktop\projects octa\saeed game battle royal\octopus S1`  
**Branch + code SHA:** `codex/octopus-s1-charter-and-prototype`. Order commit `af2497e` fast-forwarded (tree was clean;
nothing to save). WIP checkpoint `868e958` (pushed before an owner app update). **Dev 0.1.1 source: `9d4a1ce065834cfc73dfa153b60d007249838dda`.**  
**Working tree / synchronization:** one Unity process at a time; Unity inputs clean before and after the build; pushed; no force-push/PR merge.  
**Host/editor:** Unity 6000.6.4f1 (`12bfff696524`), owner-selected (now reflected in the active order). Android SDK/NDK/OpenJDK unchanged.
No Unity reinstall. A stuck `winget install GitHub.cli` from cycle 1 (waiting on UAC) was stopped, not retried.  

**R2 — Pause/input (P1): fixed, reproduced first.**
- Added a test path that runs the real per-frame code (`Update → Step(dt) → Simulate(dt)`) with injected touch samples.
- Reproduction on the unfixed logic (run 2 of the cycle): a cell **was collected in the frame Pause was touched** (expected 0, got 1)
  and **touch movement lagged one frame** (z stayed −9.0). A third case first passed for a test reason
  (teleporting via `transform` is undone by `CharacterController.Move`); the tests now teleport correctly.
- Fix: touches are read before movement; when a touch leaves Playing, the frame ends and later touches in it are ignored.
- **New defect found by these tests and fixed:** after start/restart while `timeScale` was 0, physics had not synced the new
  colliders, so the first `Move` could fall through the ground (0.981 m = one frame of gravity). `Physics.SyncTransforms()` after
  world build and teleports. Regression test added.

**R1 — test provenance (P1): fixed.** `-Mode test` runs EditMode then PlayMode and writes `Logs/test-evidence.json`:
content fingerprint of every tracked/untracked-non-ignored file under Assets/, Packages/, ProjectSettings/ (no timestamps;
docs/scripts outside the domain), exact editor version, per-platform counts and XML SHA-256. Inputs must not change during tests.
`-Mode build` recomputes the fingerprint and rejects missing/empty/failing/zero-test XML, tampered XML, a different editor or a
different source; it fails if inputs change during the build. Logic lives in `scripts/OctopusS1.Release.psm1`.

**R3 — build/signing state (P1, before LiveCandidate): fixed.** `OctopusS1BuildState` snapshots and restores, on success and
failure: application ID, product name, version/versionCode, `useCustomKeystore`, **raw keystore name/alias** (public setter writes
`{inproject}: ` for ""), in-memory passwords, minSdk, backend, architectures, orientation, development/allowDebugging/
connectProfiler/deep-profiling/app-bundle flags, scene list. The runner **no longer runs `git restore`** (could discard owner edits).
Build scripts moved to editor asmdef `OctopusS1.Editor` so EditMode tests can call them (entry names unchanged).
Real-build check: Dev 0.1.1 build left Unity inputs byte-identical (gate passed; `git status` clean for Assets/Packages/ProjectSettings).

**R4 — consistency (P2): done.** `Packages/manifest.json` now declares `com.unity.test-framework` **1.8.0**: on 6000.6.4f1 it is a
built-in (core) package and the lock already resolved `builtin 1.8.0`; the old 1.4.6 declaration was not what ran. Import/tests
pass with 1.8.0. `inputlegacy` not re-added. Committed `releases/release-register.json` (append-only) feeds the versionCode check
together with retained build folders; it records the superseded prototype (`com.evento.octopuss1`, code 1, debug-signed),
so **the first LiveCandidate must use versionCode ≥ 2**. Full signer fingerprints are in build.json and the register.

**Commands actually run + exit codes:**
`tests/release-gates.Tests.ps1` → 12/12 checks, exit 0 ·
`-Mode test` runs: (a) pre-fix R2 reproduction, exit 1 (3 expected failures) · (b) after fix, exit 1 (ground fall-through found) ·
(c) EditMode 4/5 (test compared a path with `\` vs Unity's `/`; test corrected) · (d) runner bug (`$null.Count` under strict mode; fixed) ·
(e) **final: EditMode 5/5, PlayMode 11/11, 0 skipped, exit 0** ·
`-Mode build -Channel Dev -Version 0.1.1 -VersionCode 2` → exit 0.  
**Unity import + package resolution:** OK on 6000.6.4f1 after the manifest change; no compile errors.  
**Tests + evidence (bound to source):** inputs fingerprint `2d54336481d46e689d973a5b7bb506355654e518329f08f7c7d160d80c5a857c` (62 files),
Unity `6000.6.4f1_12bfff696524`. EditMode 5/5 (`editmode.xml` sha256 `ea68eb64…6956df7b`): Dev settings restore, Live keystore/flags
restore, restore after failure, Live without key refused, unknown channel rejected. PlayMode 11/11 (`playmode.xml` sha256 `844dca0c…fac1a11`):
the 7 cycle-1 tests + pause-frame collection, interact around pause, joystick same-frame/held-through-pause/cancel, ground after start/restart.
Copies retained beside the APK. Python setup tests: not run (no Python on this host).  
**Dev artifact:** `Builds/Android/Dev/2/OctopusS1-Dev-0.1.1-2.apk`, 23,567,792 bytes, SHA-256
`f001031cd7563be7351d91bd9aff63e65da29802df1e756083d849920283790d` (re-hashed after build). aapt2/apksigner:
`com.evento.octopuss1.dev`, "Octopus S1 Dev", 0.1.1 (2), debuggable, `arm64-v8a`, signer CN=Android Debug SHA-256
`f3e9dc669f9264b6df366eb721a3697b4c21981c577162a423aaa1af52676bd7`. Dev 0.1.0 (1) retained unchanged (re-hashed `82df86f4…ed92e84b`).  
**GitHub Draft Release:** not created. No GitHub CLI or other release-capable session on this host; `scripts/new-draft-release.ps1`
is ready (verifies the hash, uploads APK + checksum + build.json + test evidence/XML as a **draft**; never publishes).  
**Live candidate:** not built — no owner Live keystore on this host (`%USERPROFILE%\.octopus-s1` absent). Next code must be ≥ 2.  
**Device results:** not run — `adb devices` empty at start and end of the cycle. G3 open; Arabic glyph rendering, touch feel,
camera near rocks, repeated-restart resource growth and performance remain unmeasured.  
**Observed charges/quota:** none by this cycle (local only, no Cloud). Prior account-wide Cloud charges: unknown, not zero.  
**Blocker / required owner action (one per gate):**
1. Draft Release: allow the official portable GitHub CLI (or install it) and run `gh auth login` locally; then `scripts/new-draft-release.ps1 -Channel Dev -VersionCode 2`.
2. Device: connect an authorized Android ARM64 phone with USB debugging and accept the prompt.
3. LiveCandidate: run `scripts/new-live-keystore.ps1` and keep the key + an offline backup.  
**Recommended next action:** device run of Dev 0.1.1 (install, mission, AR/EN, pause/background, second run, logcat, FPS).  
**Handoff commit:** recorded in the cycle history after push.
