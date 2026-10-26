#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity 6000.6.4f1 executable}"
[[ -x "$UNITY_EDITOR" ]] || { echo 'UNITY_EDITOR is not executable' >&2; exit 2; }
mkdir -p Logs Builds/Android
case "${1:-build}" in
  test)
    "$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults "$PWD/Logs/playmode.xml" -logFile "$PWD/Logs/tests.log"
    python3 scripts/check_test_results.py Logs/playmode.xml
    ;;
  build)
    git diff --quiet HEAD -- Assets Packages ProjectSettings || { echo 'Commit Unity sources before building' >&2; exit 2; }
    export OCTOPUS_BUILD_COMMIT="$(git rev-parse HEAD)"
    "$UNITY_EDITOR" -batchmode -nographics -quit -buildTarget Android -projectPath "$PWD" -executeMethod OctopusS1Build.Android -logFile "$PWD/Logs/android.log"
    python3 scripts/build_manifest.py
    ;;
  *) echo 'Usage: scripts/unity-build.sh test|build' >&2; exit 2 ;;
esac
