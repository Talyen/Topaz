#!/usr/bin/env python3
"""Check that Unity assets and folders have their paired metadata files."""

from pathlib import Path
import sys

assets = Path(__file__).resolve().parents[1] / "Assets"
missing = [
    path.relative_to(assets)
    for path in assets.rglob("*")
    if not path.name.endswith(".meta")
    and not path.name.startswith(".")
    and not Path(f"{path}.meta").is_file()
]

if missing:
    for path in missing:
        print(f"Missing .meta: Assets/{path}", file=sys.stderr)
    sys.exit(1)

print("Unity asset metadata pairs are complete.")
