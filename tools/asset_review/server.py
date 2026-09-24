#!/usr/bin/env python3
"""Local, dependency-free review gallery for Topaz's imported KayKit FBX assets."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import shutil
import tempfile
from threading import Lock
from urllib.parse import urlparse


ROOT = Path(__file__).resolve().parents[2]
PACK_ROOT = ROOT / "Assets/ThirdParty/KayKit"
ARCHIVE_ROOT = ROOT / "ArtArchive/KayKit"
REVIEW_ROOT = ROOT / "AssetReview"
PREVIEWS = REVIEW_ROOT / "Previews"
ARCHIVE_PREVIEWS = ROOT / "ArtArchive/Previews"
DECISIONS = REVIEW_ROOT / "decisions.json"
WEB = Path(__file__).resolve().parent / "web"
GUID_RE = re.compile(r"^guid: ([0-9a-f]{32})$", re.MULTILINE)
REFERENCE_RE = re.compile(r"guid: ([0-9a-f]{32})")
REFERENCE_EXTENSIONS = {".unity", ".prefab", ".asset", ".mat", ".controller", ".anim"}
STATUSES = {"keep", "maybe", "archive"}
WRITE_LOCK = Lock()


def catalog() -> list[dict]:
    referenced = referenced_guids()
    items = []
    for fbx in sorted((*PACK_ROOT.rglob("*.fbx"), *ARCHIVE_ROOT.rglob("*.fbx"))):
        meta = Path(f"{fbx}.meta")
        if not meta.is_file():
            continue
        match = GUID_RE.search(meta.read_text(encoding="utf-8"))
        if not match:
            continue
        guid = match.group(1)
        archived_source = ARCHIVE_ROOT in fbx.parents
        relative = fbx.relative_to(ARCHIVE_ROOT if archived_source else PACK_ROOT)
        pack = relative.parts[0]
        family = relative.parts[1] if len(relative.parts) > 2 else "Models"
        kind = "Animation" if family == "Animations" else (
            "Character" if family == "Characters" else "Model"
        )
        path = fbx.relative_to(ROOT).as_posix()
        preview = PREVIEWS / f"{guid}.png"
        if not preview.is_file():
            preview = ARCHIVE_PREVIEWS / f"{guid}.png"
        clip_file = PREVIEWS / f"{guid}.txt"
        frames = sorted(PREVIEWS.glob(f"{guid}-*.png")) if kind == "Animation" else []
        items.append({
            "guid": guid,
            "name": fbx.stem.replace("_", " "),
            "pack": pack,
            "family": family,
            "kind": kind,
            "path": path,
            "archivedSource": archived_source,
            "referenced": guid in referenced,
            "preview": f"/previews/{guid}.png" if preview.is_file() else None,
            "frames": [f"/previews/{frame.name}" for frame in frames],
            "previewClip": clip_file.read_text(encoding="utf-8") if clip_file.is_file() else None,
        })
    return items


def referenced_guids() -> set[str]:
    result = set()
    for path in (ROOT / "Assets").rglob("*"):
        if path.suffix not in REFERENCE_EXTENSIONS or PACK_ROOT in path.parents:
            continue
        try:
            result.update(REFERENCE_RE.findall(path.read_text(encoding="utf-8")))
        except (UnicodeError, OSError):
            pass
    return result


def load_decisions() -> dict:
    if not DECISIONS.is_file():
        return {"schemaVersion": 1, "choices": {}}
    data = json.loads(DECISIONS.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != 1 or not isinstance(data.get("choices"), dict):
        raise ValueError("Unsupported asset review decisions format")
    return data


def save_decisions(data: dict) -> None:
    REVIEW_ROOT.mkdir(parents=True, exist_ok=True)
    fd, temporary = tempfile.mkstemp(prefix="decisions-", suffix=".json", dir=REVIEW_ROOT)
    try:
        with os.fdopen(fd, "w", encoding="utf-8") as stream:
            json.dump(data, stream, indent=2, sort_keys=True)
            stream.write("\n")
        os.replace(temporary, DECISIONS)
    finally:
        if os.path.exists(temporary):
            os.unlink(temporary)


def restore_archived_source(item: dict) -> None:
    source = ROOT / item["path"]
    destination = PACK_ROOT / source.relative_to(ARCHIVE_ROOT)
    source_meta = Path(f"{source}.meta")
    destination_meta = Path(f"{destination}.meta")
    if not item["archivedSource"] or not source.is_file() or not source_meta.is_file():
        raise ValueError("Archived source or metadata is missing")
    if destination.exists() or destination_meta.exists():
        raise ValueError("Active destination already exists")
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.move(source, destination)
    try:
        shutil.move(source_meta, destination_meta)
    except Exception:
        shutil.move(destination, source)
        raise


class Handler(BaseHTTPRequestHandler):
    def send_bytes(self, content: bytes, content_type: str, status: int = 200) -> None:
        self.send_response(status)
        self.send_header("Content-Type", content_type)
        self.send_header("Content-Length", str(len(content)))
        self.send_header("Cache-Control", "no-store")
        self.end_headers()
        self.wfile.write(content)

    def send_json(self, value: object, status: int = 200) -> None:
        self.send_bytes(json.dumps(value).encode("utf-8"), "application/json; charset=utf-8", status)

    def do_GET(self) -> None:
        path = urlparse(self.path).path
        if path == "/api/catalog":
            self.send_json({"items": catalog(), "choices": load_decisions()["choices"]})
            return
        if path == "/api/export":
            self.send_json(load_decisions())
            return
        if path.startswith("/previews/"):
            name = path.removeprefix("/previews/")
            if not re.fullmatch(r"[0-9a-f]{32}(?:-\d+)?\.png", name):
                self.send_error(404)
                return
            file = PREVIEWS / name
            if not file.is_file():
                file = ARCHIVE_PREVIEWS / name
            if file.is_file():
                self.send_bytes(file.read_bytes(), "image/png")
            else:
                self.send_error(404)
            return
        static = {"/": "index.html", "/app.css": "app.css", "/app.js": "app.js"}
        if path in static:
            file = WEB / static[path]
            mime = "text/html" if path == "/" else "text/css" if path.endswith(".css") else "text/javascript"
            self.send_bytes(file.read_bytes(), f"{mime}; charset=utf-8")
            return
        self.send_error(404)

    def do_POST(self) -> None:
        if urlparse(self.path).path != "/api/decisions":
            self.send_error(404)
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if length > 100_000:
                raise ValueError("Request too large")
            payload = json.loads(self.rfile.read(length))
            changes = payload.get("changes")
            if not isinstance(changes, list) or not 1 <= len(changes) <= 60:
                raise ValueError("Expected 1 to 60 changes")
            valid_items = {item["guid"]: item for item in catalog()}
            for change in changes:
                if not isinstance(change, dict) or change.get("guid") not in valid_items:
                    raise ValueError("Unknown asset")
                if change.get("status") not in (*STATUSES, None):
                    raise ValueError("Invalid decision")
                if not isinstance(change.get("note", ""), str) or len(change.get("note", "")) > 500:
                    raise ValueError("Note must be at most 500 characters")
            with WRITE_LOCK:
                data = load_decisions()
                timestamp = datetime.now(timezone.utc).isoformat(timespec="seconds")
                catalog_changed = False
                restored = []
                try:
                    for change in changes:
                        item = valid_items[change["guid"]]
                        if item["archivedSource"] and change["status"] in (None, "keep"):
                            restore_archived_source(item)
                            restored.append(item)
                            catalog_changed = True
                        if change["status"] is None:
                            data["choices"].pop(change["guid"], None)
                        else:
                            data["choices"][change["guid"]] = {
                                "status": change["status"],
                                "note": change.get("note", "").strip(),
                                "reviewedAt": timestamp,
                            }
                    save_decisions(data)
                except Exception:
                    for item in reversed(restored):
                        active = PACK_ROOT / (ROOT / item["path"]).relative_to(ARCHIVE_ROOT)
                        archive = ROOT / item["path"]
                        shutil.move(active, archive)
                        shutil.move(Path(f"{active}.meta"), Path(f"{archive}.meta"))
                    raise
            self.send_json({"choices": data["choices"], "catalogChanged": catalog_changed})
        except (ValueError, KeyError, OSError, json.JSONDecodeError) as error:
            self.send_json({"error": str(error)}, 400)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--port", type=int, default=8765)
    args = parser.parse_args()
    server = ThreadingHTTPServer(("127.0.0.1", args.port), Handler)
    print(f"Topaz asset review: http://127.0.0.1:{args.port}/", flush=True)
    server.serve_forever()


if __name__ == "__main__":
    main()
