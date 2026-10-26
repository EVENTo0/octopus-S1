"""Writes the SHA-256 file and build.json beside an APK built by scripts/unity-build.sh.
The Windows runner (scripts/unity-local.ps1) also inspects package/signing with aapt2/apksigner."""
import hashlib
import json
import os
import sys
from pathlib import Path

apk, channel, version, code = Path(sys.argv[1]), sys.argv[2], sys.argv[3], int(sys.argv[4])
digest = hashlib.sha256(apk.read_bytes()).hexdigest()
Path(str(apk) + '.sha256').write_text(digest + '  ' + apk.name + '\n')
(apk.parent / 'build.json').write_text(json.dumps({
    'channel': channel,
    'applicationId': 'com.evento.octopuss1.dev' if channel == 'Dev' else 'com.evento.octopuss1',
    'version': version, 'versionCode': code,
    'commit': os.environ['OCTOPUS_BUILD_COMMIT'], 'unity': '6000.6.4f1',
    'sha256': digest, 'deviceTested': False,
}, indent=2) + '\n')
