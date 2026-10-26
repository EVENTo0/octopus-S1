"""Supplementary Linux C# check. Does not import a Unity project or run tests."""
import argparse
from pathlib import Path
import subprocess

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--editor', type=Path, required=True, help='Unity Editor directory containing Unity and Data')
parser.add_argument('--output', type=Path, default=Path('Logs/csharp'))
args = parser.parse_args()
editor = args.editor.resolve()
data = editor / 'Data'
dotnet = data / 'NetCoreRuntime/dotnet'
compiler = data / 'DotNetSdkRoslyn/csc.dll'
if not dotnet.is_file() or not compiler.is_file():
    parser.error('Expected an extracted Linux Unity Editor with bundled .NET and Roslyn')
version = subprocess.check_output([str(editor / 'Unity'), '-version'], text=True).strip()
if version != '6000.3.25f1':
    parser.error('Expected Unity 6000.3.25f1; found ' + version)

root = Path(__file__).resolve().parents[1]
output = args.output.resolve()
output.mkdir(parents=True, exist_ok=True)
# Do not also reference Managed/UnityEditor.dll: its legacy facade duplicates
# types exported by UnityEditor.CoreModule.dll in this release.
references = sorted((data / 'NetStandard/ref/2.1.0').glob('*.dll'))
references += sorted((data / 'Managed/UnityEngine').glob('*.dll'))
if not references:
    parser.error('Unity reference assemblies are missing')

def quote(value):
    return '"' + str(value).replace('\\', '/').replace('"', '\\"') + '"'

def compile_assembly(name, sources, extra=()):
    assembly = output / (name + '.dll')
    response = output / (name + '.rsp')
    lines = ['-nologo', '-target:library', '-nostdlib+', '-langversion:9.0',
             '-out:' + quote(assembly)]
    lines += ['-r:' + quote(p) for p in references]
    lines += list(extra)
    lines += [quote(p) for p in sources]
    response.write_text('\n'.join(lines) + '\n')
    subprocess.run([str(dotnet), str(compiler), '@' + str(response)], cwd=root, check=True)
    print(name + ': C# check passed', flush=True)
    return assembly

runtime = compile_assembly('OctopusS1.Runtime', sorted((root / 'Assets/Scripts').glob('*.cs')))
compile_assembly('OctopusS1.Editor', sorted((root / 'Assets/Editor').glob('*.cs')),
                 ['-define:UNITY_EDITOR', '-r:' + quote(runtime)])
print('This does not verify Unity import, package resolution, PlayMode, Android/IL2CPP or device behavior.')
