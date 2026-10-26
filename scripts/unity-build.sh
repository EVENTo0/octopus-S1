#!/usr/bin/env bash
# Usage: scripts/unity-build.sh test
#        scripts/unity-build.sh build Dev|LiveCandidate <version> <versionCode>
set -euo pipefail
cd "$(dirname "$0")/.."
: "${UNITY_EDITOR:?Set UNITY_EDITOR to the licensed Unity 6000.6.4f1 executable}"
[[ -x "$UNITY_EDITOR" ]] || { echo 'UNITY_EDITOR is not executable' >&2; exit 2; }
mkdir -p Logs
case "${1:-}" in
  test)
    rm -f Logs/playmode.xml
    "$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform PlayMode -testResults "$PWD/Logs/playmode.xml" -logFile "$PWD/Logs/tests.log"
    python3 scripts/check_test_results.py Logs/playmode.xml
    ;;
  build)
    channel="${2:?channel Dev or LiveCandidate}"; version="${3:?version x.y.z}"; code="${4:?versionCode}"
    git diff --quiet HEAD -- Assets Packages ProjectSettings || { echo 'Commit Unity sources before building' >&2; exit 2; }
    out="Builds/Android/$channel/$code"
    [[ ! -e "$out" ]] || { echo "$out already exists; use a higher versionCode" >&2; exit 2; }
    apk="$PWD/$out/OctopusS1-$channel-$version-$code.apk"
    "$UNITY_EDITOR" -batchmode -nographics -quit -buildTarget Android -projectPath "$PWD" \
      -executeMethod OctopusS1Build.Android -octopusChannel "$channel" -octopusVersion "$version" \
      -octopusVersionCode "$code" -octopusOutput "$apk" -logFile "$PWD/Logs/android-$channel-$code.log"
    OCTOPUS_BUILD_COMMIT="$(git rev-parse HEAD)" python3 scripts/build_manifest.py "$apk" "$channel" "$version" "$code"
    ;;
  *) echo 'Usage: scripts/unity-build.sh test | build <channel> <version> <versionCode>' >&2; exit 2 ;;
esac
