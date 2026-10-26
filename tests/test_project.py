import json
import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

class ProjectChecks(unittest.TestCase):
    def test_required_modules(self):
        deps = json.loads((ROOT / 'Packages/manifest.json').read_text())['dependencies']
        for module in ['audio', 'physics', 'imgui', 'jsonserialize']:
            self.assertEqual(deps.get('com.unity.modules.' + module), '1.0.0')

    def test_version_and_batch_entry(self):
        self.assertIn('6000.3.25f1', (ROOT / 'ProjectSettings/ProjectVersion.txt').read_text())
        source = (ROOT / 'Assets/Editor/OctopusS1Build.cs').read_text()
        self.assertIn('public static void Android()', source)
        self.assertIn('BuildResult.Succeeded', source)

if __name__ == '__main__':
    unittest.main()
