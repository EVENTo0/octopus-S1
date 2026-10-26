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

**Order/Revision executed:** not started  
**Agent/session and UTC:** not recorded  
**Windows project path:** not yet recorded; do not invent a path  
**Branch + code SHA:** not yet recorded  
**Working tree / remote synchronization:** not inspected on Windows  
**Host/editor/modules/license:** not verified on Windows  
**Changed behavior / files:** none for this order  
**Commands actually run + exit codes:** none for this order  
**Unity import + package resolution:** not run locally for this order  
**Tests + counts + XML evidence:** not run locally for this order  
**Dev artifact + SHA-256:** not produced  
**Live candidate + SHA-256:** not produced  
**Device results:** not run  
**Observed charges/quota:** not inspected; do not treat as zero  
**Blocker / required owner action:** local agent must first read this order inside the Windows checkout  
**Recommended next action:** synchronize and execute the first slice above  
**Handoff commit:** record after saving/pushing this report; do not put a commit's own future hash inside it

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
| Dev | — | Not produced | Not tested | Not required to iterate |
| Live candidate | — | Not produced | Not tested | Pending |
| Approved Live | — | No release | None | None |

Do not replace an accepted row in place. Append each version, its immutable artifact and source.
Keep build/source SHA separate from a later documentation/report commit.

## Cycle history

- 2026-10-26 — Workflow established: master prompt, shared command center and Dev/Live contract.
  Owner selected local APK + GitHub Draft Release first.
  Documentation only; no two-channel APK or local gameplay success claimed.
