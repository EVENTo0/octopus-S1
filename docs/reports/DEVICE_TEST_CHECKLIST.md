# Device test checklist — Octopus S1 Dev 0.1.1 (2)

APK under test: `Builds/Android/Dev/2/OctopusS1-Dev-0.1.1-2.apk`,
SHA-256 `f001031cd7563be7351d91bd9aff63e65da29802df1e756083d849920283790d`. Verify before install.
Record results in OCTOPUS_S1_WORKLOG.md. Do not publish the device serial or personal identifiers.

## Owner setup (once)
1. Phone: Settings → About phone → tap Build number 7 times; Developer options → USB debugging on.
2. Connect USB, unlock the phone, accept "Allow USB debugging" for this PC.
3. The agent runs `adb devices -l` (Unity SDK platform-tools) and confirms which device is authorized.

## Record
- Model, Android version, ARM64 ABI, screen resolution/refresh, battery/thermal state at start.
- Install method and whether Dev 0.1.0 (1) was already installed (upgrade path).
- Session length; performance method (`adb shell dumpsys gfxinfo com.evento.octopuss1.dev` frame stats
  and/or Unity development-build profiler connection), target sustained 30 FPS.
- `adb logcat` filtered to the app's process; remove personal data before storing.

## Scenarios (pass / fail / note)
1. Verify SHA-256, then `adb install -r`; upgrade over 0.1.0 (1) if present without uninstalling.
2. Fresh launch: menu readable in Arabic (connected letters, right-to-left) and English; language toggle.
3. Help screen text wraps and stays inside the panel in both languages.
4. Start: left-thumb joystick moves; right-thumb drag turns camera; both at once (two fingers).
5. Camera near the three rocks: no clipping that hides the player; collisions block correctly.
6. Scan button highlights the three cells; companion follows without blocking or pushing.
7. Collect the three cells; counter goes 1/3 → 3/3 exactly once each.
8. Interact at the relay before 3 cells → rejected message; at extraction before relay → rejected.
9. Pause button stops movement immediately; touches during Pause do nothing; Resume has no stuck movement.
10. Activate relay with 3 cells; reach extraction; result screen with time/best/missions count.
11. Play again (restart) and complete a second run; repeat restart ~5 times, watch memory in `dumpsys meminfo`.
12. Home/background during play → app returns paused; resume works. Screen off/on likewise.
13. Settings: sound toggle and volume persist after force-stop and relaunch; progress (missions, best time) persists.
14. Walk into the world edge: blocked; "Return to start" in Pause works.
15. Android back button: pauses in play, closes Help/Settings, never exits unexpectedly.
16. 10+ minutes continuous play: FPS, temperature, crashes/ANRs in logcat.
