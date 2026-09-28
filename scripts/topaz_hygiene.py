"""Repository checks and bounded, opt-in local artifact ownership. Standard library only."""
from __future__ import annotations

from contextlib import contextmanager
from datetime import datetime, timedelta, timezone
import fcntl
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
from urllib.parse import unquote, urlsplit

META = '.topaz-artifact.json'
RUN_ID = re.compile(r'\d{8}T\d{6}\.\d{6}Z-[0-9a-f]{8}')
KINDS = {'full', 'quick', 'fast', 'stress', 'baseline', 'build-mac', 'build-windows'}
ARTIFACT_KINDS = {'review', 'diagnostic', 'concept', 'report'}


def git_paths(root, tracked=False):
    args = ['git', 'ls-files', '--cached']
    if not tracked:
        args += ['--others', '--exclude-standard']
    data = subprocess.check_output(args + ['-z'], cwd=root)
    return {p.decode('utf-8', errors='surrogateescape') for p in data.split(b'\0') if p}


def policy(root):
    return json.loads((root / 'scripts/repository-policy.json').read_text())


def markdown_body(text):
    return re.sub(r'^\s*(`{3,}|~{3,}).*?^\s*\1\s*$', '', text, flags=re.M | re.S)


def anchors(text):
    found, counts = set(), {}
    for heading in re.findall(r'^ {0,3}#{1,6}\s+(.+?)\s*#*\s*$', markdown_body(text), re.M):
        heading = re.sub(r'\[([^]]+)\]\([^)]*\)', r'\1', heading)
        slug = re.sub(r'[^\w\-\s]', '', heading.lower()).replace(' ', '-')
        n = counts.get(slug, 0)
        counts[slug] = n + 1
        found.add(slug + (f'-{n}' if n else ''))
    found.update(re.findall(r'\b(?:id|name)=["\']([^"\']+)', text))
    return found


def hygiene(root):
    config = policy(root)
    names = git_paths(root)
    present = {name for name in names if (root / name).is_file()}
    errors = []
    docs = config['documents']
    for name in sorted(present):
        asset_note = name.startswith('Assets/') and Path(name).name in ('SOURCE.md', 'LICENSE.md', 'README.md', 'NOTICE.md')
        vendor = name.startswith(('Assets/ThirdParty/', 'Assets/Synty/', 'Packages/'))
        if (name.startswith('docs/') or (name.endswith('.md') and not asset_note and not vendor)) and name not in docs:
            errors.append(f'{name}: unregistered document/image; extend an owning reference or register a distinct purpose')
        if any(name == p or name.startswith(p + '/') for p in config['forbidden_doc_roots']):
            errors.append(f'{name}: obsolete report/concept directory; use managed local artifacts')
        if name.split('/')[0].lower() in {p.lower() for p in config['generated_roots']}:
            errors.append(f'{name}: generated/private output must not be versioned')
    for name, purpose in docs.items():
        if name not in present or not purpose.strip():
            errors.append(f'{name}: missing catalog document or purpose')
    for name in sorted(present):
        if not name.endswith('.md') or name.startswith(('Assets/ThirdParty/', 'Assets/Synty/', 'Packages/')):
            continue
        text = markdown_body((root / name).read_text())
        references = {key.strip().lower(): url for key, url in
                      re.findall(r'^\s*\[([^]]+)\]:\s*<?([^\s>]+)>?', text, re.M)}
        links = re.findall(r'!?\[[^\]\n]*\]\(\s*(<[^>]+>|[^\s)]+)(?:\s+["\'][^\n]*?["\'])?\s*\)', text)
        for label, key in re.findall(r'!?\[([^]\n]+)\]\[([^]\n]*)\]', text):
            key = (key or label).strip().lower()
            if key not in references:
                errors.append(f'{name}: undefined link reference [{key}]')
            else:
                links.append(references[key])
        links.extend(references.values())
        for raw in links:
            url = urlsplit(raw.strip('<>'))
            if url.scheme or url.netloc:
                continue
            destination = unquote(url.path)
            target = (root / destination.lstrip('/')) if destination.startswith('/') else (root / name).parent / destination
            if not destination:
                target = root / name
            target = target.resolve()
            if not target.is_relative_to(root.resolve()):
                errors.append(f'{name}: link escapes repository: {raw}')
            elif not target.exists():
                errors.append(f'{name}: missing local link: {raw}')
            elif url.fragment and target.suffix == '.md' and unquote(url.fragment) not in anchors(target.read_text()):
                errors.append(f'{name}: missing anchor: {raw}')
    areas = json.loads((root / 'scripts/agent-areas.json').read_text())
    for area, spec in areas.items():
        for name in spec['paths'] + spec['docs'] + spec['scenes']:
            if not (root / name).exists():
                errors.append(f'area {area}: missing path {name}')
        for mode, tests in spec['tests'].items():
            folder = 'Editor' if mode == 'EditMode' else 'PlayMode'
            for test in tests:
                if not (root / 'Assets/Topaz/Tests' / folder / (test + '.cs')).is_file():
                    errors.append(f'area {area}: missing {mode} test {test}')
    for error in errors:
        print(error)
    print(f'Hygiene: {len(errors)} error(s); {len(docs)} registered current documents')
    return 1 if errors else 0


@contextmanager
def output_lock(root, exclusive=False):
    """Writers share the lock; cleanup/registration require exclusive access."""
    folder = root / 'TestResults'
    if folder.is_symlink() or (folder / '.retention.lock').is_symlink():
        raise ValueError('output lock path must not be a symlink')
    folder.mkdir(exist_ok=True)
    with (folder / '.retention.lock').open('a') as stream:
        flags = fcntl.LOCK_EX if exclusive else fcntl.LOCK_SH
        fcntl.flock(stream, flags | (fcntl.LOCK_NB if exclusive else 0))
        try:
            yield
        finally:
            fcntl.flock(stream, fcntl.LOCK_UN)


def stamp(value):
    time = datetime.fromisoformat(value)
    if time.tzinfo is None:
        raise ValueError('timestamp must have timezone')
    return time


def safe_path(root, name):
    p = Path(name)
    if p.is_absolute() or '..' in p.parts or len(p.parts) < 2 or p.parts[0] not in ('TestResults', 'Builds'):
        raise ValueError('path must be inside a local output root')
    path = root / p
    for ancestor in [path, *path.parents]:
        if ancestor == root:
            break
        if ancestor.is_symlink():
            raise ValueError('symlink path')
    if not path.resolve().is_relative_to(root.resolve()):
        raise ValueError('path escapes repository')
    return path


def snapshot(path):
    """Metadata identity catches changed content/trees; never follow directory links."""
    digest, size = hashlib.sha256(), 0
    entries = [path] + (sorted(path.rglob('*')) if path.is_dir() else [])
    for p in entries:
        if p.is_symlink() and not p.resolve().is_relative_to(path.resolve()):
            raise ValueError('symlink escapes artifact')
        s = p.lstat()
        digest.update(f'{p.relative_to(path)}:{s.st_ino}:{s.st_size}:{s.st_mtime_ns}:{s.st_ctime_ns}'.encode())
        if p.is_file() and not p.is_symlink():
            size += s.st_size
    return digest.hexdigest(), size


def open_paths():
    """Use OS open files, including executable/text mappings, before destructive cleanup."""
    if not shutil.which('lsof'):
        raise RuntimeError('lsof is required for safe cleanup; nothing removed')
    try:
        result = subprocess.run(['lsof', '-n', '-P', '-F', 'n'], capture_output=True, text=True, timeout=15)
    except subprocess.TimeoutExpired as error:
        raise RuntimeError('open-file inspection timed out; nothing removed') from error
    if result.returncode not in (0, 1):
        raise RuntimeError('cannot inspect open files; nothing removed')
    return {Path(s[1:]) for s in result.stdout.splitlines() if s.startswith('n/')}


def occupied(path, opened):
    return any(p == path or p.is_relative_to(path) for p in opened)


def write_json(path, data):
    temp = path.with_name(path.name + '.tmp')
    temp.write_text(json.dumps(data, indent=2) + '\n')
    temp.replace(path)


def register(root, name, category, runs=(), pin=None):
    path = safe_path(root, name)
    name = str(path.relative_to(root))
    if name in ('Builds/reviews', 'TestResults/artifacts', 'Builds/Topaz.app', 'Builds/Topaz_Data', 'Builds/MonoBleedingEdge', 'Builds/D3D12'):
        raise ValueError('do not register output containers or current working builds')
    if not path.is_dir() or Path(name).parts[1] in ('runs', 'observed'):
        raise ValueError('register an existing completed artifact directory, not a verification/observation directory')
    if pin is not None and not pin.strip():
        raise ValueError('pin reason must be nonempty')
    if category not in ARTIFACT_KINDS:
        raise ValueError('category must be review, diagnostic, concept or report')
    if any(not RUN_ID.fullmatch(run) or not (root / 'TestResults/runs' / run / 'result.json').is_file() for run in runs):
        raise ValueError('related run IDs must identify existing completed runs')
    with output_lock(root, exclusive=True):
        tracked = git_paths(root, tracked=True)
        if any(n == name or n.startswith(name.rstrip('/') + '/') for n in tracked):
            raise ValueError('cannot manage tracked files')
        if occupied(path, open_paths()):
            raise ValueError('artifact is in use')
        if any(p != path / META for p in path.rglob(META)) or any((p / META).exists() for p in path.parents if p != root):
            raise ValueError('nested artifact registrations are not allowed')
        snapshot(path)
        write_json(path / META, {'version': 1, 'completed': datetime.now(timezone.utc).isoformat(),
                               'category': category, 'runs': list(runs), 'pin': pin})
    print(f'Registered {name}' + (f' (pinned: {pin})' if pin else ''))


def set_pin(root, name, reason):
    path = safe_path(root, name)
    if reason is not None and not reason.strip():
        raise ValueError('pin reason must be nonempty')
    with output_lock(root, exclusive=True):
        if (path / META).is_symlink():
            raise ValueError('symlink metadata')
        data = read_artifact(path / META)
        data['pin'] = reason
        write_json(path / META, data)
    print(f'{"Pinned" if reason else "Unpinned"} {name}')


def read_artifact(path):
    if path.is_symlink():
        raise ValueError('symlink metadata')
    data = json.loads(path.read_text())
    if data['version'] != 1 or data['category'] not in ARTIFACT_KINDS:
        raise ValueError('invalid artifact metadata')
    stamp(data['completed'])
    if not isinstance(data['runs'], list) or any(not isinstance(s, str) or not RUN_ID.fullmatch(s) for s in data['runs']):
        raise ValueError('invalid related runs')
    if data.get('pin') is not None and (not isinstance(data['pin'], str) or not data['pin'].strip()):
        raise ValueError('invalid pin')
    return data


def inventory(root, now=None):
    now = now or datetime.now(timezone.utc)
    config = policy(root)
    items, warnings, protected_runs = [], [], set()
    for base in ('TestResults', 'Builds'):
        for meta in (root / base).rglob(META):
            try:
                path = safe_path(root, str(meta.parent.relative_to(root)))
                if any((p / META).exists() for p in path.parents if p != root) or any(p != meta for p in path.rglob(META)):
                    raise ValueError('nested registration')
                d = read_artifact(meta)
                digest, size = snapshot(path)
                # Keep dependencies for this pass even if their artifact is removed or becomes busy.
                protected_runs.update(d['runs'])
                items.append(dict(path=path, category='artifact:' + d['category'], time=stamp(d['completed']),
                                  reason=('pin: ' + d['pin']) if d.get('pin') else '', runs=d['runs'], digest=digest, size=size))
            except (OSError, ValueError, KeyError, TypeError) as error:
                warnings.append(f'{meta.relative_to(root)}: skipped invalid metadata ({error})')
                # Cannot establish dependencies: protect every run if any artifact metadata is invalid.
                protected_runs.add('*')
    for path in (root / 'TestResults/runs').glob('*'):
        if not path.is_dir():
            continue
        try:
            safe_path(root, str(path.relative_to(root)))
            if not RUN_ID.fullmatch(path.name):
                raise ValueError('unrecognized run name')
            d = json.loads((path / 'result.json').read_text())
            if d['kind'] not in KINDS or type(d['passed']) is not bool:
                raise ValueError('unrecognized result')
            digest, size = snapshot(path)
            items.append(dict(path=path, category='run:' + d['kind'], time=stamp(d['time']), reason='',
                              runs=[], digest=digest, size=size, passed=d['passed']))
        except (OSError, ValueError, KeyError, TypeError):
            warnings.append(f'{path.relative_to(root)}: skipped incomplete/unrecognized run')
    ledger = root / 'TestResults/agent-observations.jsonl'
    observations = []
    if ledger.exists():
        try:
            observations = [json.loads(line) for line in ledger.read_text().splitlines() if line.strip()]
            pending = []
            for d in observations:
                name = d['log']
                if not re.fullmatch(r'TestResults/observed/[0-9a-f]{32}\.log', name):
                    raise ValueError('unrecognized observation path')
                path = safe_path(root, name)
                completed = stamp(d['time'])
                if path.exists():
                    digest, size = snapshot(path)
                    pending.append(dict(path=path, category='observation', time=completed, reason='', runs=[], digest=digest, size=size))
            items.extend(pending)
        except (ValueError, OSError, KeyError, TypeError):
            observations = None
            warnings.append('Observation ledger malformed; observation cleanup skipped')
    for category in {i['category'] for i in items}:
        group = sorted((i for i in items if i['category'] == category), key=lambda i:i['time'], reverse=True)
        for n, item in enumerate(group):
            if not item['reason']:
                if item['time'] >= now - timedelta(days=config['retention_days']):
                    item['reason'] = 'younger than retention window'
                elif n < config['retention_count']:
                    item['reason'] = 'newest per category'
        if category == 'run:full':
            passed = next((i for i in group if i['passed']), None)
            if passed and not passed['reason']:
                passed['reason'] = 'latest successful full gate'
    for item in items:
        if item['reason']:
            protected_runs.update(item['runs'])
    for item in items:
        if item['category'].startswith('run:') and (item['path'].name in protected_runs or '*' in protected_runs):
            item['reason'] = item['reason'] or 'referenced by retained artifact'
    return items, warnings, observations


def cleanup(root, apply=False, quiet=False):
    try:
        with output_lock(root, exclusive=True):
            items, warnings, observations = inventory(root)
            tracked = git_paths(root, tracked=True)
            opened = open_paths() if apply else set()
            for item in items:
                name = str(item['path'].relative_to(root))
                if any(n == name or n.startswith(name + '/') for n in tracked):
                    item['reason'] = 'contains tracked files'
                if occupied(item['path'], opened):
                    item['reason'] = 'open in a process'
            protected = {run for item in items if item['reason'] for run in item['runs']}
            for item in items:
                if item['category'].startswith('run:') and item['path'].name in protected:
                    item['reason'] = item['reason'] or 'referenced by retained artifact'
            deleted_logs, reclaimed, removed = set(), 0, 0
            ledger = root / 'TestResults/agent-observations.jsonl'
            ledger_before = ledger.read_bytes() if ledger.exists() else None
            for item in items:
                path = item['path']
                name = str(path.relative_to(root))
                if any(n == name or n.startswith(name + '/') for n in tracked):
                    item['reason'] = 'contains tracked files'
                if occupied(path, opened):
                    item['reason'] = 'open in a process'
                if not quiet or not item['reason']:
                    print(f'{"KEEP" if item["reason"] else "REMOVE"} {name} ({item["size"]:,} bytes): {item["reason"] or "expired managed output"}')
                if item['reason']:
                    continue
                if apply:
                    try:
                        safe_path(root, name)
                        if occupied(path, open_paths()):
                            print(f'SKIP {name}: opened during cleanup')
                            continue
                        if snapshot(path)[0] != item['digest']:
                            print(f'SKIP {name}: changed during cleanup')
                            continue
                        if item['category'] == 'observation' and ledger.read_bytes() != ledger_before:
                            print(f'SKIP {name}: observation ledger changed')
                            continue
                        if path.is_dir():
                            shutil.rmtree(path)
                        else:
                            path.unlink()
                        if item['category'] == 'observation':
                            deleted_logs.add(name)
                    except (OSError, ValueError) as error:
                        print(f'SKIP {name}: {error}')
                        continue
                reclaimed += item['size']
                removed += 1
            if deleted_logs:
                temp = ledger.with_suffix('.tmp')
                temp.write_text(''.join(json.dumps(d) + '\n' for d in observations if d['log'] not in deleted_logs))
                temp.replace(ledger)
            for warning in warnings[:10]:
                print('Retention: ' + warning)
            if len(warnings) > 10:
                print(f'Retention: {len(warnings) - 10} more unmanaged/incomplete entries preserved')
            print(f'Cleanup {"applied" if apply else "preview"}: {removed} items, {reclaimed:,} bytes')
            return 0
    except BlockingIOError:
        print('Cleanup skipped: output writer/cleanup is active')
        return 0


def summary(root):
    items, warnings, _ = inventory(root)
    pinned = [i for i in items if i['reason'].startswith('pin:')]
    managed = [i['path'] for i in items]
    unmanaged = 0
    for base in ('Builds', 'TestResults'):
        for p in (root / base).rglob('*'):
            if p.is_file() and not p.is_symlink() and not any(p == m or p.is_relative_to(m) for m in managed):
                unmanaged += p.stat().st_size
    print(f'Retention: {len(pinned)} pins / {sum(i["size"] for i in pinned):,} bytes; unmanaged output {unmanaged:,} bytes; {len(warnings)} incomplete/invalid entries')
