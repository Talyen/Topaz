"""Scope selection and content identity for prototype verification; no Unity dependency."""
from __future__ import annotations
import hashlib
import re
import fcntl
from contextlib import contextmanager
from pathlib import Path

SCHEMA = 2
DOMAINS = ('documentation', 'tooling', 'editmode', 'playmode', 'build')


def documentation(name):
    return name.startswith('docs/') or name.endswith(('.md', '.md.meta')) or name in ('LICENSE', 'RIGHTS')


def unity_input(name):
    return name.startswith(('Assets/', 'Packages/', 'ProjectSettings/')) and not documentation(name)


def belongs(name, domain):
    if domain == 'documentation':
        return documentation(name) or name in ('scripts/repository-policy.json', 'scripts/agent-areas.json')
    if domain == 'tooling':
        return name.startswith(('scripts/', '.github/')) or name in ('.gitignore', '.gitattributes')
    if domain not in DOMAINS:
        raise ValueError('Unknown fingerprint domain: ' + domain)
    if name in ('scripts/topaz-tools.py', 'scripts/topaz_verification.py', 'scripts/build.sh', 'scripts/verify.sh'):
        return True
    if not unity_input(name):
        return False
    if name.startswith('Assets/Topaz/Tests/'):
        return domain == 'editmode' and '/PlayMode/' not in name or domain == 'playmode' and '/Editor/' not in name
    return True


def snapshot(root, names):
    result = {}
    for name in names:
        path = root / name
        if path.is_file():
            result[name] = hashlib.sha256(path.read_bytes()).hexdigest()
    return result


def fingerprint(files, domain):
    digest = hashlib.sha256()
    for name, value in sorted(files.items()):
        if belongs(name, domain):
            digest.update((name + '\0' + value + '\0').encode())
    return digest.hexdigest()


def fingerprints(files):
    return {domain: fingerprint(files, domain) for domain in DOMAINS}


def delta(before, after):
    return sorted(name for name in before.keys() | after.keys() if before.get(name) != after.get(name))


def selected_tests(paths, areas, existing=None):
    """Direct test changes select their fixture; unmapped/shared Unity changes broaden safely."""
    selected, unknown, tests = set(), [], {}
    for name in paths:
        if name.endswith(".cs.meta"):
            name = name[:-5]
        if not unity_input(name):
            continue
        if name.startswith('Assets/Topaz/Tests/') and name.endswith('.cs'):
            test = Path(name).stem
            if existing is not None and name not in existing:
                if any(Path(p).name == Path(name).name for p in existing):
                    continue  # A moved fixture runs only in its new assembly.
                unknown.append(name)
                continue
            if test.endswith('Tests'):
                mode = 'EditMode' if '/Editor/' in name else 'PlayMode'
                tests.setdefault(mode, set()).add(test)
                continue
        if name.startswith(('Packages/', 'ProjectSettings/', 'Assets/Topaz/World/Scenes/')):
            unknown.append(name)
            continue
        matches = [key for key, spec in areas.items() if any(name.startswith(p) for p in spec['paths'])]
        if not matches:
            unknown.append(name)
        selected.update(matches)
    if unknown:
        return {'EditMode': [None], 'PlayMode': [None]}, sorted(selected), unknown
    for key in selected:
        for mode, names in areas[key]['tests'].items():
            tests.setdefault(mode, set()).update(names)
    return {mode: sorted(names) for mode, names in tests.items()}, sorted(selected), unknown


def test_command(root, mode, names, category, report, timeout=900):
    command = ['unity', 'test', str(root), '--mode', mode, '--report-format', 'junit',
               '--output', str(report), '--timeout', str(timeout), '--no-banner']
    if names != [None]:
        command += ['--filter', ';'.join(names)]
    if category:
        command += ['--', '-testCategory', category]
    return command


def windows_reasons(paths, root=None):
    reasons = []
    for name in paths:
        if documentation(name):
            continue
        risk = name.startswith(('Packages/', 'ProjectSettings/', 'Assets/Plugins/', 'Assets/Topaz/Build/',
                               'Assets/Topaz/Core/Editor/PlayerBuild', 'Assets/Topaz/Presentation/Rendering/'))
        risk = risk or Path(name).suffix.lower() in ('.shader', '.shadergraph', '.hlsl', '.dll', '.so', '.dylib', '.bundle')
        risk = risk or name == 'scripts/build.sh' or ('/Editor/' in name and 'Build' in Path(name).name)
        if root is not None and name.endswith('.cs') and (root / name).is_file():
            risk = risk or bool(re.search(r'UNITY_STANDALONE_WIN|RuntimePlatform.Windows|DllImport|OSPlatform.Windows|OperatingSystem.IsWindows', (root / name).read_text()))
        if risk:
            reasons.append(name)
    return sorted(reasons)


@contextmanager
def unity_operation(root):
    """Avoid competing Editors/imports in one checkout; static checks remain concurrent."""
    path = root / "TestResults/.unity-operation.lock"
    path.parent.mkdir(exist_ok=True)
    with path.open("a") as stream:
        try:
            fcntl.flock(stream, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError as error:
            raise RuntimeError("Another Unity operation is active in this checkout; let it finish before starting another") from error
        try:
            yield
        finally:
            fcntl.flock(stream, fcntl.LOCK_UN)
