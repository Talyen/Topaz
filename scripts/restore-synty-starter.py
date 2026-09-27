#!/usr/bin/env python3
"""Restore selected owned Synty Starter Pack assets without demo scenes or settings."""
import argparse
import gzip
import hashlib
import json
from pathlib import Path
import tarfile

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'scripts/synty-starter-files.json'
DEFAULT = Path.home() / 'Library/Unity/Asset Store-5.x/Synty Studios/3D ModelsEnvironments/POLYGON - Starter Pack - Art by Synty.unitypackage'


def members(archive):
    with gzip.open(archive, 'rb') as stream, tarfile.open(fileobj=stream, mode='r|') as package:
        for member in package:
            yield package, member


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archive', nargs='?', type=Path, default=DEFAULT)
    args = parser.parse_args()
    manifest = json.loads(MANIFEST.read_text())
    expected = {entry['path']: entry['sha256'] for entry in manifest['files']}
    if not args.archive.is_file():
        parser.error('Download owned Unity Asset Store product 156819 in Package Manager first, or pass its .unitypackage path.')
    names = {}
    prefix = 'Assets/Synty/'
    for package, member in members(args.archive):
        if member.name.endswith('/pathname'):
            path = package.extractfile(member).read().decode().splitlines()[0].strip('\0')
            if path.startswith(prefix):
                names[member.name.split('/')[0]] = path
    staged = {}
    for package, member in members(args.archive):
        parts = member.name.split('/')
        if parts[0] not in names or parts[-1] not in ('asset', 'asset.meta'):
            continue
        path = names[parts[0]] + ('.meta' if parts[-1] == 'asset.meta' else '')
        if path not in expected:
            continue
        content = package.extractfile(member).read()
        if hashlib.sha256(content).hexdigest() != expected[path]:
            raise SystemExit('Package revision mismatch for ' + path + '; no files restored.')
        staged[path] = content
    if set(staged) != set(expected):
        raise SystemExit('The package is missing selected dependencies; no files restored.')
    target = ROOT
    for name, content in staged.items():
        path = target / name
        if '..' in Path(name).parts or Path(name).is_absolute():
            raise SystemExit('Unsafe manifest path')
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(content)
    print(f'Restored {len(staged)} private files from Synty Starter {manifest["version"]}. Open Unity to import them.')


if __name__ == '__main__':
    main()
