#!/usr/bin/env python3
"""Move owner-rejected KayKit FBX sources outside Unity after reference cleanup."""

from __future__ import annotations

import argparse
import json
from pathlib import Path
import re
import shutil


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/ThirdParty/KayKit"
ARCHIVE = ROOT / "ArtArchive/KayKit"
PREVIEWS = ROOT / "AssetReview/Previews"
ARCHIVE_PREVIEWS = ROOT / "ArtArchive/Previews"
DECISIONS = ROOT / "AssetReview/decisions.json"
GUID = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)
REF = re.compile(r"guid: ([0-9a-f]{32})")
SERIALIZED = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim"}


def guid_of(asset: Path) -> str:
    match = GUID.search(Path(f"{asset}.meta").read_text(encoding="utf-8"))
    if not match:
        raise ValueError(f"Missing Unity GUID: {asset}")
    return match.group(1)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--apply", action="store_true", help="Perform the checked move")
    args = parser.parse_args()
    choices = json.loads(DECISIONS.read_text(encoding="utf-8"))["choices"]
    archived_guids = {guid for guid, choice in choices.items() if choice.get("status") == "archive"}
    files = {guid_of(asset): asset for asset in SOURCE.rglob("*.fbx")}
    to_move = {guid: files[guid] for guid in sorted(archived_guids & files.keys())}
    missing = archived_guids - files.keys() - {
        guid_of(asset) for asset in ARCHIVE.rglob("*.fbx")
    }
    if missing:
        raise RuntimeError(f"Archive decisions have no source file: {sorted(missing)}")
    blocked = []
    for asset in (ROOT / "Assets").rglob("*"):
        if asset.suffix not in SERIALIZED or SOURCE in asset.parents:
            continue
        try:
            found = archived_guids.intersection(REF.findall(asset.read_text(encoding="utf-8")))
        except (OSError, UnicodeError):
            continue
        blocked.extend(f"{asset.relative_to(ROOT)} references {guid}" for guid in sorted(found))
    for code in (ROOT / "Assets/Topaz").rglob("*.cs"):
        if "AssetReview" in code.parts:
            continue
        text = code.read_text(encoding="utf-8", errors="ignore")
        blocked.extend(f"{code.relative_to(ROOT)} names {asset.name}"
                       for asset in to_move.values() if asset.name in text)
    for line in blocked:
        print(f"Needs replacement: {line}")
    print(f"{len(to_move)} selected FBX files ready to move; {len(blocked)} remaining references.")
    if blocked:
        return 1
    if not args.apply:
        return 0
    moves = []
    for guid, source in to_move.items():
        destination = ARCHIVE / source.relative_to(SOURCE)
        if destination.exists() or Path(f"{destination}.meta").exists():
            raise RuntimeError(f"Archive destination already exists: {destination}")
        if not Path(f"{source}.meta").is_file():
            raise RuntimeError(f"Source metadata missing: {source}")
        preview = PREVIEWS / f"{guid}.png"
        if not preview.is_file():
            raise RuntimeError(f"Review preview missing: {guid}")
        moves.append((guid, source, destination, preview))

    completed = []
    copied = []
    try:
        for guid, source, destination, preview in moves:
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.move(source, destination)
            completed.append((source, destination))
            shutil.move(Path(f"{source}.meta"), Path(f"{destination}.meta"))
            ARCHIVE_PREVIEWS.mkdir(parents=True, exist_ok=True)
            archived_preview = ARCHIVE_PREVIEWS / f"{guid}.png"
            shutil.copy2(preview, archived_preview)
            copied.append(archived_preview)
        for pack in {source.relative_to(SOURCE).parts[0] for _, source, _, _ in moves}:
            target = ARCHIVE / pack
            for name in ("LICENSE.txt", "SOURCE.md"):
                file = SOURCE / pack / name
                if file.is_file():
                    copy = target / name
                    shutil.copy2(file, copy)
                    copied.append(copy)
    except Exception:
        for copy in copied:
            copy.unlink(missing_ok=True)
        for source, destination in reversed(completed):
            if Path(f"{destination}.meta").exists():
                shutil.move(Path(f"{destination}.meta"), Path(f"{source}.meta"))
            if destination.exists():
                shutil.move(destination, source)
        raise
    print(f"Moved {len(completed)} FBX files and .meta files to {ARCHIVE.relative_to(ROOT)}.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
