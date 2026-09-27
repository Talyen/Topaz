#!/usr/bin/env python3
"""Restore private, owned Sidekick and motion dependencies from Unity's download cache."""
import argparse
import hashlib
import json
from pathlib import Path
import tarfile

ROOT = Path(__file__).resolve().parents[1]

def restore(pack, cache):
    archive = cache / pack['archive']
    if not archive.is_file():
        raise SystemExit('Download owned asset in Unity Package Manager: ' + pack['name'])
    expected = {x['path']: x['sha256'] for x in pack['files']}
    staged = {}
    with tarfile.open(archive, 'r:gz') as tar:
        members = {m.name: m for m in tar.getmembers()}
        for member in members.values():
            if not member.name.endswith('/pathname'):
                continue
            path = tar.extractfile(member).read().decode().splitlines()[0].strip('\0')
            if not path.startswith('Assets/') or '..' in Path(path).parts:
                continue
            guid = member.name.split('/')[0]
            for part, suffix in [('asset', ''), ('asset.meta', '.meta')]:
                name = path + suffix
                if name not in expected:
                    continue
                content = tar.extractfile(members[guid + '/' + part]).read()
                if hashlib.sha256(content).hexdigest() != expected[name]:
                    raise SystemExit('Package version mismatch: ' + name)
                staged[name] = content
    if set(staged) != set(expected):
        raise SystemExit('Missing selected dependencies: ' + pack['name'])
    return staged

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cache', type=Path, default=Path.home() / 'Library/Unity/Asset Store-5.x')
    args = parser.parse_args()
    packs = json.loads((ROOT / 'scripts/storybook-assets.json').read_text())
    staged = {}
    for pack in packs:
        staged.update(restore(pack, args.cache))
    for name, content in staged.items():
        path = ROOT / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
    print(f'Restored {len(staged)} private files; sources remain excluded from Git.')

if __name__ == '__main__':
    main()
