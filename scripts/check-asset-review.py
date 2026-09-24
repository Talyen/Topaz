#!/usr/bin/env python3
"""Reject use of KayKit art marked Maybe or Archive by the owner."""

from __future__ import annotations

import json
from pathlib import Path
import re


ROOT = Path(__file__).resolve().parents[1]
PACKS = ROOT / "Assets/ThirdParty/KayKit"
ARCHIVE = ROOT / "ArtArchive/KayKit"
DECISIONS = ROOT / "AssetReview/decisions.json"
GUID = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)
REF = re.compile(r"guid: ([0-9a-f]{32})")
EXTENSIONS = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim"}


def guid_of(path: Path) -> str | None:
    meta = Path(f"{path}.meta")
    if not meta.is_file():
        return None
    match = GUID.search(meta.read_text(encoding="utf-8"))
    return match.group(1) if match else None


def models() -> dict[str, Path]:
    return {guid: file for root in (PACKS, ARCHIVE)
            for file in root.rglob("*.fbx") if (guid := guid_of(file))}


def scan_references(known: set[str]) -> list[str]:
    result = []
    for file in (ROOT / "Assets").rglob("*"):
        if file.suffix not in EXTENSIONS or PACKS in file.parents:
            continue
        try:
            found = known.intersection(REF.findall(file.read_text(encoding="utf-8")))
        except (OSError, UnicodeError):
            continue
        result.extend(f"{file.relative_to(ROOT)} references {guid}" for guid in sorted(found))
    return result


def main() -> int:
    if not DECISIONS.is_file():
        print("Asset review decisions file missing.")
        return 1
    choices = json.loads(DECISIONS.read_text(encoding="utf-8"))["choices"]
    paths = models()
    blocked = {guid for guid, choice in choices.items()
               if choice.get("status") in {"maybe", "archive"}}
    violations = scan_references(blocked)
    for guid in sorted(blocked):
        if guid not in paths:
            violations.append(f"Decision {guid} has no retained FBX source")
        elif choices[guid]["status"] == "archive" and PACKS in paths[guid].parents:
            violations.append(f"Archived source still inside Assets: {paths[guid].relative_to(ROOT)}")
    for guid, path in paths.items():
        if ARCHIVE in path.parents and choices.get(guid, {}).get("status") not in {"archive", "maybe"}:
            violations.append(f"Available model still outside Assets: {path.relative_to(ROOT)}")
    for code in (ROOT / "Assets/Topaz").rglob("*.cs"):
        if "AssetReview" in code.parts:
            continue
        text = code.read_text(encoding="utf-8", errors="ignore")
        for guid in sorted(blocked):
            if guid in paths and paths[guid].name in text:
                violations.append(f"{code.relative_to(ROOT)} names blocked model {paths[guid].name}")
    for line in violations:
        print(f"Blocked KayKit use: {line}")
    if violations:
        return 1
    print("No Maybe or Archive KayKit models are available or referenced by Topaz.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
