import hashlib
import json
import os
from pathlib import Path
apk = Path('Builds/Android/OctopusS1-0.1.0.apk')
digest = hashlib.sha256(apk.read_bytes()).hexdigest()
apk.with_suffix('.apk.sha256').write_text(digest + '  ' + apk.name + '\n')
apk.with_suffix('.build.json').write_text(json.dumps({
    'commit': os.environ['OCTOPUS_BUILD_COMMIT'],
    'unity': '6000.6.4f1', 'version': '0.1.0', 'versionCode': 1,
    'sha256': digest, 'development': True, 'signing': 'local debug keystore',
    'deviceTested': False
}, indent=2) + '\n')
