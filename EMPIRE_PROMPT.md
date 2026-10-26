# Octopus S1 — Empire Prompt

Version: 1.0. Owner: EVENTO. Working language: Arabic for owner reports; English identifiers/code.
This file is an execution brief for the local Windows agent, not proof that a game or APK is complete.
Read the latest owner instruction and repository evidence before acting.

## Mission

Continue the existing independent Unity game **Octopus S1 | أخطبوط S1** in
https://github.com/EVENTo0/octopus-S1, initially on
`codex/octopus-s1-charter-and-prototype` (Draft PR #1).
Deliver a genuinely playable Android mission, a downloadable test APK, and a repeatable
Dev → tested Live candidate → approved Live release process.
Maintain both an experimental installation and the last approved installation during development.

Reuse and repair the current project. Do not create a replacement game, switch engines,
fork another product into this one, or report scaffold/configuration work as completed gameplay.
The SAEED ROYALE image is a visual reference only. Its branding, characters, source code
and assets, and the separate previous Claude project, must remain separate.

## Read first and establish reality

1. Read `AGENTS.md`, `OCTOPUS_S1_WORKLOG.md`, and this entire file.
2. Read `docs/01_PROJECT_CHARTER_AR.md`, `docs/02_GAME_AND_WEB_CATALOG_AR.md`,
   and relevant build/evidence documents 04–09. The 32-entry catalog is a roadmap,
   not 32 implemented screens or a requirement to build all of them before the first playable APK.
3. Inspect the actual Windows checkout and `git status`. Verify its remote and branch.
   Preserve local edits and any running Unity session. Fetch and compare remote changes;
   integrate safely. Do not use `reset --hard`, forced pushes or broad cleanup.
4. Identify the active work order, its base code commit, current blockers and latest
   Cloud/local results. Historical claims may be superseded; do not start duplicate paid builds.
5. Inspect scripts, scene generation, package manifest, lockfile, project settings and
   actual logs before changing implementation. Record findings in the worklog.

Use Unity **6000.3.25f1** with its matching Android Build Support, SDK, NDK and OpenJDK.
The Windows host is preferred. The owner confirms that Codex or Claude Code can run there.
This ChatGPT cloud session does not automatically control that host.

## Authority, cost and persistent boundaries

- Work locally on this repository; commit and push reviewable changes to the working
  branch/PR when authentication permits. Do not merge PRs automatically.
- The current phase has a **USD 20 total ceiling**, shared across agents and attempts,
  including compute, storage, egress, fees and tax. It is not USD 20 per run.
  Prefer existing hardware and already available free quotas. Record estimates and actual
  observed charges; an unknown bill is not a zero bill. Do not create recurring subscriptions.
- Do not request the owner's password, PAT, license file or signing secret in chat.
  If licensing requires personal login, let the owner perform it inside Unity Hub.
  Do not bypass Unity activation or install unrelated remote-control software.
- Keep signing keys and credentials outside Git and out of logs/screenshots.
  Do not expose desktop/SSH ports to use this workflow.
- The owner selected **a local APK on their Windows PC plus an owner-accessible GitHub
  Draft Release first**. Follow that destination; do not publish a public prerelease/release.
  A Draft Release is not a public download link and requires appropriate repository access.
  Do not substitute public Actions artifacts or another public host for the selected owner-first delivery.
- Google Play submission, public website deployment and automatic Live promotion are
  outside this execution brief. The owner approves the first Live candidate and later
  Live promotions using the concrete tested version and APK fingerprint.
- Ask only for missing information or an action genuinely needed to unblock the next
  dependency. Continue independent authorized work. Do not repeatedly ask for the budget,
  the supplied concept image, permission to inspect files, or routine reversible fixes.

## Two installations, one codebase

Implement the contract in `docs/09_DEV_LIVE_RELEASES_AR.md`.
It is a **target specification**: current prototype build scripts are not yet two-channel builders.

| Property | Dev / تجريبية | Live / معتمدة |
|---|---|---|
| Android application ID | `com.evento.octopuss1.dev` | `com.evento.octopuss1` |
| Display name | `Octopus S1 Dev` | `Octopus S1` |
| Purpose | Iteration and testing | Last accepted release |
| Data | Dev-only sandbox | Independent stable data |
| Diagnostics | Development options allowed | Development/debug/profiler options disabled |
| Signing | Deliberate test signing policy | Persistent owner-managed Live signing identity |
| Promotion | Never auto-promote | Owner accepts the tested Live candidate artifact |

Do not duplicate the Unity project into two diverging source folders. Use build configuration,
immutable source commits, separate application IDs, output paths and release records.
Retain the last approved Live APK and source tag while developing the next candidate.
Before the first approval, record **Live: not released**. Never invent a stable release.

## Execute in this order

### 1. Unblock the real Unity project

- Find Unity through Hub or a configured path. Start with
  `powershell -NoProfile -File .\scripts\unity-local.ps1 -Mode doctor`.
  This checks module directories, not entitlement validity or SDK health.
- If modules are missing, use Unity Hub's matching modules and the supported activation
  path. If the user must log in, explain the exact local action without asking for secrets.
- Open/import a safe working checkout. Capture actual Package Manager/compiler errors.
  The nonexistent `com.unity.modules.inputlegacy` dependency was removed after a real
  Cloud failure; do not re-add it from an older build guide.
- Review generated settings, .meta files and `Packages/packages-lock.json`; commit
  legitimate reproducibility changes. Do not commit Library, Editor installs or licenses.
- Run PlayMode tests and the generated scene. Keep fail-on-test-failure enabled.
  Standalone Roslyn C# checks do not replace Unity import or gameplay verification.
- Fix the earliest proven failure, reproduce it, verify the fix, then advance.
  Do not blindly retry the same Cloud build or change working gameplay to mask a Git error.

### 2. Finish a playable first mission

Build on `OctopusS1Prototype.cs`, `OctopusS1SceneBuilder.cs` and the existing tests.
Refactor narrowly when justified by the feature or bug; do not rewrite the game wholesale.

The first playable release must support this complete loop:
launch → start/resume → move/explore with companion → scan/find → collect exactly three
distinct relay cells → activate the nearby relay → reach and interact with extraction →
see success → restart or return to the menu.

Make that loop work with actual Android touch input, not just editor keyboard input.
Verify camera visibility, colliders, navigation through the blockout and interaction distance.
The companion must follow and scan without trapping or pushing the player.
Collectibles count once; the relay rejects early activation; extraction rejects completion
before relay activation; actions stop or behave correctly after mission success.

Implement the essential catalog surfaces needed for that loop: start/help, gameplay HUD,
pause/resume, mission result/retry, basic settings and Arabic/English text.
Keep touch targets and safe areas usable on the target phone. Arabic must display legibly
with correct direction/shaping using legally distributable font/runtime resources.
If the initial IMGUI controls cannot deliver reliable touch/camera behavior, replace that
control surface while preserving mission logic.

Handle app background/resume, audio settings, fresh install, repeat sessions and a safe
out-of-bounds recovery/retry path. Implement catalog-required settings/progress persistence
with a documented schema and recovery for missing/corrupt data; do not add accounts or servers.
Do not add multiplayer, monetization, extra regions or combat merely because they appear
in a reference board. Keep original-art improvements incremental and mark placeholders.

### 3. Implement and verify channel-aware builds

- Centralize channel and version settings. Cover both local scripts and the Cloud hook
  without nested BuildPlayer calls.
- Pass the channel explicitly; reject missing/invalid release configuration.
  Apply final identity/settings after scene generation so it cannot overwrite the chosen channel.
- Generate Dev and Live-candidate APKs in distinct directories, with manifests and SHA-256.
  Use an increasing versionCode per application ID and an explicit human version.
- Validate the actual APK package ID, version, signing certificate, debuggable flag and
  ARM64/native content with Android tooling. Do not merely echo intended PlayerSettings.
- Verify installation of Dev and Live-candidate together, isolated saves, upgrading each
  within its own signing lineage, and retention of the previous approved Live artifact.
- Live signing must remain reproducible across machines. A host-specific debug keystore
  is not an acceptable long-term Live signing plan. Let the owner retain the private key
  and backup; store only the public certificate fingerprint and non-secret policy in Git.
- The existing `-Mode build` command builds one debug prototype. Do not call that output
  a two-channel release until the implementation and artifact inspections prove it.
- The current prototype uses the intended Live application ID with debug signing. If it is
  already installed, diagnose that signing transition explicitly; do not uninstall it or wipe
  its data without the owner choosing a migration/removal plan.

### 4. Verify behavior and produce evidence

Choose meaningful automated tests for changed mission/state/input/save/build behavior.
Do not spend time on tests that only restate constants or mock the entire feature.
Keep exact test counts, failures and result XML references. Inspect the full relevant suite.

On an owner-authorized Android ARM64 device, record model, Android version and build SHA.
Ask which device is authorized before installing if more than one is available; the owner
accepts Android's USB debugging prompt locally. Do not wipe data or uninstall an old Live app
to make an upgrade test pass.

Exercise:
- fresh launch, touch movement, camera and scan;
- each distinct cell, duplicate collection prevention, premature relay/extraction attempts;
- completed relay/extraction flow, success screen, pause/resume and restart;
- Arabic/English display, settings/progress restore and independent channel data;
- a second mission run, focus/background recovery, collision/boundary recovery;
- Dev/Live side-by-side install and same-channel upgrade compatibility;
- launch stability and a measured play session with logcat inspection.

Set and record a practical device performance target before the run (initial target:
sustained 30 FPS for the first mission). Report actual duration, frame behavior, crashes,
memory/thermal observations and limitations. Do not report a measurement not collected.
An emulator or successful Gradle build alone does not accept G3.
If no device is available, deliver the APK for owner testing and explicitly leave G3 open.

### 5. Deliver, approve and preserve a release

Deliver the APK and manifest to the owner's selected test channel.
Include source commit, Unity version, channel, version/versionCode, SHA-256, application ID,
public signing fingerprint, install instructions, tested devices and known issues.
Keep large artifacts out of Git source history and credentials out of artifacts.

Build a non-development **Live candidate**, test that exact signed APK, then ask the owner
to accept its version and fingerprint. Do not rename a Dev APK to Live or rebuild after
approval and claim the new bytes were tested. If any bytes change, create a new candidate.

After explicit owner acceptance, retain that same Live artifact, record acceptance in the
worklog and create the agreed immutable release tag/record. Keep development moving on
the working branch. Never move/delete an accepted tag or overwrite a released APK.
A rollback uses the retained known-good artifact/source; do not assume Android allows
downgrading versionCode without data loss.

## Continuous development loop

Use **`OCTOPUS_S1_WORKLOG.md` as the single shared command-and-report file**.
ChatGPT owns the next work order and review fields. The local agent owns execution results
and evidence entries. Read its synchronization protocol before each cycle.

For every cycle:
1. Fetch, inspect and safely integrate remote instructions; identify Order ID and Revision.
2. Pick the smallest coherent next slice that advances the current acceptance gate.
3. Inspect evidence, implement, run the relevant checks and correct concrete failures.
4. Update the worklog with what actually ran, code/build commits, artifact paths,
   blockers, test results and the recommended next action.
5. Commit/push reviewable changes when possible. Preserve remote work and local edits.
6. Continue to the next already authorized dependency while runtime/resources permit.
   If blocked, complete independent work, record the exact blocker and ask one focused question.

If a new order conflicts with work in progress, pause that dependent change, report the
conflict and preserve both sets of evidence. Do not silently overwrite the other agent's section.
Do not invent an automatic connection between ChatGPT and Claude Code.
Instructions travel through fetched repository files, and owner messages when needed.
When a session ends, leave a restartable handoff. Do not promise background work that is not running.

## Required end-of-cycle report

Report in Arabic, briefly:
- Order/Revision, active branch, code commit and handoff commit if available.
- Changed behavior and files; actual tests, counts and failures.
- Dev APK / Live-candidate APK / approved Live artifact: paths or links and fingerprints,
  or explicitly **not produced / not approved**.
- Device results and unresolved limits.
- Observed spending/quota and whether total charges remain unknown.
- Next concrete action and any single owner action necessary to unblock it.

A task is not complete because a prompt, scene generator, concept image or workflow exists.
Completion requires evidence for the deliverable actually claimed.
