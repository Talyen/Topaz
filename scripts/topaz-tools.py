#!/usr/bin/env python3
"""Concise Unity check results. Complete command output stays in TestResults."""

from __future__ import annotations

import argparse
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "TestResults"
RUNS = RESULTS / "runs"
PROFILES = {"mac": ("macOS.asset", "Topaz.app"),
            "windows": ("Windows.asset", "Topaz.exe")}
ERROR = re.compile(r"error CS\d+|error:|exception:|build failed|compilation failed", re.I)
AREAS = ROOT / "scripts/agent-areas.json"
OBSERVATIONS = RESULTS / "agent-observations.jsonl"


def project_paths() -> list[str]:
    """Tracked and untracked, non-ignored project inputs, including .meta files."""
    result = subprocess.run(["git", "ls-files", "--cached", "--others", "--exclude-standard", "-z"],
                            cwd=ROOT, capture_output=True, check=True)
    return sorted(set(path.decode("utf-8", errors="surrogateescape")
                      for path in result.stdout.split(b"\0") if path))


def input_fingerprint() -> str:
    digest = hashlib.sha256()
    for name in project_paths():
        path = ROOT / name
        if not path.is_file():
            continue
        digest.update(name.encode("utf-8", errors="surrogateescape") + b"\0")
        with path.open("rb") as source:
            for chunk in iter(lambda: source.read(1024 * 1024), b""):
                digest.update(chunk)
        digest.update(b"\0")
    return digest.hexdigest()


def changed_paths() -> list[str]:
    tracked = subprocess.run(["git", "diff", "HEAD", "--name-only", "-z"], cwd=ROOT,
                             capture_output=True, check=True).stdout
    untracked = subprocess.run(["git", "ls-files", "--others", "--exclude-standard", "-z"],
                               cwd=ROOT, capture_output=True, check=True).stdout
    return sorted(set(path.decode("utf-8", errors="surrogateescape")
                      for path in (tracked + untracked).split(b"\0") if path))


def load_areas() -> dict:
    return json.loads(AREAS.read_text())


def matching_areas(path: str, areas: dict) -> list[str]:
    return [name for name, area in areas.items()
            if any(path.startswith(prefix) for prefix in area["paths"])
            or path in area["docs"] or path in area["scenes"]
            or any(path.endswith("/" + test + ".cs")
                   for tests in area["tests"].values() for test in tests)]


def selection_for_changes(paths: list[str], areas: dict) -> tuple[list[str], list[str]]:
    matched = sorted(set(name for path in paths for name in matching_areas(path, areas)))
    # An unmapped source, scene, package, or settings change can affect every test.
    unknown = [path for path in paths if not matching_areas(path, areas)
               and (path.startswith("Assets/") or path.startswith("Packages/")
                    or path.startswith("ProjectSettings/"))]
    return matched, unknown


def append_observation(entry: dict) -> None:
    OBSERVATIONS.parent.mkdir(parents=True, exist_ok=True)
    with OBSERVATIONS.open("a") as stream:
        stream.write(json.dumps(entry) + "\n")


def task_id() -> str:
    return os.environ.get("TOPAZ_AGENT_TASK", "unassigned")


def summarize_xml(path: Path) -> bool:
    try:
        cases = ET.parse(path).getroot().findall(".//testcase")
    except ET.ParseError as error:
        print(f"Invalid test report: {error}")
        return False
    failures = [(case, case.find("failure") if case.find("failure") is not None else case.find("error")) for case in cases
                if case.find("failure") is not None or case.find("error") is not None]
    skipped = sum(case.find("skipped") is not None for case in cases)
    print(f"Tests: {len(cases) - len(failures) - skipped} passed, "
          f"{len(failures)} failed, {skipped} skipped")
    for case, failure in failures[:10]:
        message = (failure.get("message") or failure.text or "failure").strip()
        first = next((line.strip() for line in message.splitlines() if line.strip()), "failure")
        print(f"  FAIL {case.get('classname')}.{case.get('name')}: {first[:220]}")
        stack = next((line.strip() for line in (failure.text or "").splitlines()
                      if ".cs:" in line or " in " in line), "")
        if stack:
            print(f"       {stack[:220]}")
    if len(failures) > 10:
        print(f"  … {len(failures) - 10} more failures in {path.relative_to(ROOT)}")
    return not failures and bool(cases)


def summarize_log(path: Path) -> None:
    lines = path.read_text(encoding="utf-8", errors="replace").splitlines()
    matches = [line.strip() for line in lines if ERROR.search(line)]
    for line in (matches[-8:] if matches else lines[-3:]):
        if line:
            print(f"  {line[:220]}")


def new_run() -> Path:
    RUNS.mkdir(parents=True, exist_ok=True)
    name = datetime.now(timezone.utc).strftime("%Y%m%dT%H%M%S.%fZ")
    directory = RUNS / f"{name}-{uuid.uuid4().hex[:8]}"
    directory.mkdir()
    return directory


def run(command: list[str], label: str, directory: Path,
        report: Path | None = None) -> bool:
    log = directory / f"{label}.log"
    print(f"Running {label}…", flush=True)
    started = time.monotonic()
    with log.open("w", encoding="utf-8", errors="replace") as output:
        result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT,
                                check=False)
    report_ok = summarize_xml(report) if report and report.is_file() else False
    if result.returncode or (report and not report_ok):
        summarize_log(log)
        editor_log = ROOT / "Logs/Editor.log"
        if editor_log.is_file() and command and command[0] == "unity":
            snapshot = directory / f"{label}-editor.log"
            shutil.copyfile(editor_log, snapshot)
            compiler_errors = [line.strip() for line in snapshot.read_text(errors="replace").splitlines()
                               if re.search(r"error CS\d+|error:.*Assets/Topaz/", line, re.I)]
            for line in compiler_errors[-8:]:
                print(f"  {line[:220]}")
    ok = result.returncode == 0 and (not report or report_ok)
    print(f"{label}: {'PASS' if ok else 'FAIL'} (log: {log.relative_to(ROOT)})")
    if report:
        print(f"Report: {report.relative_to(ROOT) if report.exists() else 'missing'}")
    with (directory / "steps.jsonl").open("a") as stream:
        stream.write(json.dumps({"label": label, "passed": ok,
                                 "duration_seconds": round(time.monotonic() - started, 3),
                                 "log_bytes": log.stat().st_size,
                                 "report": str(report.relative_to(ROOT)) if report else None}) + "\n")
    return ok


def record(directory: Path, kind: str, ok: bool, details: str,
           fingerprint: str | None = None, started: float | None = None) -> None:
    (directory / "result.json").write_text(json.dumps({
        "time": datetime.now(timezone.utc).isoformat(), "kind": kind,
        "passed": ok, "details": details, "task": task_id(),
        "fingerprint": fingerprint,
        "duration_seconds": round(time.monotonic() - started, 3) if started else None},
        indent=2) + "\n")


def latest_record(kind: str) -> dict | None:
    latest = None
    for path in RUNS.glob("*/result.json"):
        try:
            result = json.loads(path.read_text())
        except (OSError, json.JSONDecodeError):
            continue
        if result.get("kind") == kind and (latest is None or result["time"] > latest["time"]):
            latest = result
    return latest


def build_platform(platform: str, directory: Path | None = None) -> int:
    if shutil.which("unity") is None:
        print("Unity CLI is required", file=sys.stderr)
        return 1
    own_run = directory is None
    if directory is None:
        directory = new_run()
    started = time.monotonic()
    before = input_fingerprint() if own_run else None
    profile, output = PROFILES[platform]
    (ROOT / "Builds").mkdir(exist_ok=True)
    command = ["unity", "build", str(ROOT), "--profile",
               str(ROOT / "Assets/Topaz/Build/Profiles" / profile),
               "--output-path", str(ROOT / "Builds" / output), "--allow-dirty-build",
               "--timeout", "1200", "--no-tail", "--no-banner"]
    ok = run(command, f"build-{platform}", directory)
    if own_run:
        after = input_fingerprint()
        if before != after:
            print("Project inputs changed during the build; result is not reusable")
            ok = False
        record(directory, f"build-{platform}", ok, output, after, started)
    return 0 if ok else 1


def verify(args: argparse.Namespace) -> int:
    if shutil.which("unity") is None:
        print("Unity CLI is required", file=sys.stderr)
        return 1
    if (args.filter or args.mode or args.area or args.changed) and not args.quick:
        print("--filter, --mode, --area and --changed require --quick", file=sys.stderr)
        return 2
    if args.filter and not args.mode:
        print("--filter requires --mode EditMode or --mode PlayMode", file=sys.stderr)
        return 2
    if (args.area or args.changed) and (args.filter or args.mode):
        print("Choose an area/changed selection or an explicit mode/filter", file=sys.stderr)
        return 2
    if args.area and args.changed:
        print("Choose --area or --changed", file=sys.stderr)
        return 2
    areas = load_areas()
    if any(area not in areas for area in args.area):
        print("Unknown area. Choose: " + ", ".join(areas), file=sys.stderr)
        return 2
    selected_areas = list(args.area)
    unknown = []
    if args.changed:
        selected_areas, unknown = selection_for_changes(changed_paths(), areas)
        if unknown:
            print("Unmapped project inputs; selecting both complete test suites: " +
                  ", ".join(unknown[:6]))
    tests: dict[str, list[str | None]] = {}
    if not args.quick or unknown or (args.quick and not args.filter and not selected_areas and not args.changed):
        tests = {"EditMode": [None], "PlayMode": [None]}
    elif args.filter:
        tests = {args.mode: [args.filter]}
    else:
        for area in selected_areas:
            for mode, names in areas[area]["tests"].items():
                tests.setdefault(mode, []).extend(names)
        tests = {mode: sorted(set(names)) for mode, names in tests.items()}
    selection = ", ".join(f"{mode}: {', '.join(names if names != [None] else ['all'])}"
                           for mode, names in tests.items()) or "asset checks only"
    print(f"Selected areas: {', '.join(selected_areas) or 'none'}; tests: {selection}")
    directory = new_run()
    started = time.monotonic()
    before = input_fingerprint()
    kind = "quick" if args.quick else "full"
    tool_ok = run([sys.executable, "-m", "unittest", "discover", "-s", "scripts",
                   "-p", "test_*.py"], "tool-tests", directory)
    if not tool_ok or not re.search(r"Ran [1-9]\d* tests?\b",
                                    (directory / "tool-tests.log").read_text()):
        print("Tool tests failed or no tests were discovered")
        record(directory, kind, False, "tool tests", before, started)
        return 1
    for name in ("check-assets.py", "check-asset-review.py"):
        if not run([sys.executable, str(ROOT / "scripts" / name)],
                   name.removesuffix(".py"), directory):
            record(directory, kind, False, name, before, started)
            return 1
    for mode, names in tests.items():
        for index, name in enumerate(names):
            label = f"{mode.lower()}-{index + 1}-{re.sub('[^A-Za-z0-9_-]', '_', name) if name else 'all'}"
            report = directory / f"{label}.xml"
            command = ["unity", "test", str(ROOT), "--mode", mode, "--report-format", "junit",
                       "--output", str(report), "--timeout", "900", "--no-banner"]
            if name:
                command.extend(("--filter", name))
            if not run(command, label, directory, report):
                record(directory, kind, False, f"{mode} tests: {name or 'all'}", before, started)
                return 1
    if not args.quick and build_platform("mac", directory):
        record(directory, kind, False, "Mac build", before, started)
        return 1
    diff = subprocess.run(["git", "diff", "--check"], cwd=ROOT, capture_output=True,
                          text=True, check=False)
    if diff.returncode:
        print(diff.stdout or diff.stderr)
        record(directory, kind, False, "git diff --check", before, started)
        return 1
    after = input_fingerprint()
    if before != after:
        print("Project inputs changed during verification; result is not reusable")
        record(directory, kind, False, "inputs changed during run", after, started)
        return 1
    record(directory, kind, True, selection, after, started)
    print(f"Topaz {kind} verification: PASS")
    return 0


def doctor() -> int:
    version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0]
    packages = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    print(f"Topaz: {version.removeprefix('m_EditorVersion: ')}")
    keys = ("com.unity.pipeline", "com.unity.test-framework", "com.unity.inputsystem",
            "com.unity.render-pipelines.universal")
    print("Packages: " + ", ".join(f"{name.removeprefix('com.unity.')} {packages[name]}"
                                 for name in keys))
    print(f"Unity CLI: {'available' if shutil.which('unity') else 'missing'}")
    if shutil.which("unity"):
        result = subprocess.run(["unity", "status", "--json", "--no-banner"], cwd=ROOT,
                                capture_output=True, text=True, check=False)
        try:
            instances = json.loads(result.stdout).get("data", {}).get("instances", [])
            print(f"Connected Editor: {len(instances)} instance(s)")
        except json.JSONDecodeError:
            print("Connected Editor: status unavailable")
    status = subprocess.run(["git", "status", "--porcelain"], cwd=ROOT, capture_output=True,
                            text=True, check=False)
    paths = status.stdout.splitlines()
    print(f"Working tree: {len(paths)} changed paths" +
          (f" ({', '.join(path[3:] for path in paths[:5])}{'…' if len(paths) > 5 else ''})"
           if paths else ""))
    lock_changed = any(line[3:] == "Packages/packages-lock.json" for line in paths)
    print(f"Package lock: {'changed' if lock_changed else 'clean'}")
    current = None
    for kind, title in (("full", "Full gate"), ("build-windows", "Windows build")):
        last = latest_record(kind)
        if last:
            if current is None:
                current = input_fingerprint()
            freshness = ("current" if last.get("fingerprint") == current else
                         "stale" if last.get("fingerprint") else "unknown freshness")
            print(f"{title}: {'PASS' if last['passed'] else 'FAIL'} "
                  f"at {last['time']} ({freshness}; {last['details']})")
        else:
            print(f"{title}: none recorded")
    return 0


def context(area: str, limit: int) -> int:
    areas = load_areas()
    if area not in areas:
        print("Unknown area. Choose: " + ", ".join(areas), file=sys.stderr)
        return 2
    spec = areas[area]
    files = [path for path in project_paths() if not path.endswith(".meta")
             and any(path.startswith(prefix) for prefix in spec["paths"])]
    dirty = set(changed_paths())
    files.sort(key=lambda path: (path not in dirty, not path.endswith(".cs"), path))
    version = (ROOT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0]
    packages = json.loads((ROOT / "Packages/manifest.json").read_text())["dependencies"]
    print(f"Area: {area} | files: {len(files)} | dirty: {sum(path in dirty for path in files)}")
    print(f"Unity: {version.removeprefix('m_EditorVersion: ')} | "
          f"URP: {packages.get('com.unity.render-pipelines.universal', '?')} | "
          f"Input: {packages.get('com.unity.inputsystem', '?')}")
    print("Code and assets:")
    for path in files[:limit]:
        print(f"  {'* ' if path in dirty else '  '}{path}")
    if len(files) > limit:
        print(f"  … {len(files) - limit} more; narrow with rg --files")
    print("Tests: " + "; ".join(f"{mode}: {', '.join(names)}"
                               for mode, names in spec["tests"].items()))
    print("Current docs: " + ", ".join(spec["docs"]))
    print("Scenes: " + ", ".join(spec["scenes"]))
    other_dirty = [path for path in sorted(dirty)
                   if area in matching_areas(path, areas) and path not in files]
    if other_dirty:
        print("Other dirty paths: " + ", ".join(other_dirty[:8]) +
              (f" … {len(other_dirty) - 8} more" if len(other_dirty) > 8 else ""))
    return 0


def triage(run_name: str, limit: int) -> int:
    directory = RUNS / run_name
    if run_name == "latest":
        candidates = [path for path in RUNS.glob("*/result.json")]
        if not candidates:
            print("No runs recorded", file=sys.stderr)
            return 1
        directory = max(candidates, key=lambda path: path.stat().st_mtime).parent
    if not directory.is_dir() or directory.parent != RUNS:
        print("Run not found", file=sys.stderr)
        return 2
    print(f"Run: {directory.relative_to(ROOT)}")
    result_file = directory / "result.json"
    if result_file.exists():
        result = json.loads(result_file.read_text())
        print(f"Result: {'PASS' if result['passed'] else 'FAIL'} ({result['details']})")
    total = 0
    for report in sorted(directory.glob("*.xml")):
        try:
            cases = ET.parse(report).getroot().findall(".//testcase")
        except ET.ParseError:
            print(f"Invalid report: {report.name}")
            continue
        for case in cases:
            failure = case.find("failure")
            if failure is None:
                failure = case.find("error")
            if failure is None:
                continue
            total += 1
            if total > limit:
                continue
            message = (failure.get("message") or failure.text or "failure").strip()
            first = next((line.strip() for line in message.splitlines() if line.strip()), "failure")
            source = next((line.strip() for line in (failure.text or "").splitlines()
                           if "Assets/Topaz/" in line or ".cs:" in line), "")
            print(f"  {case.get('classname')}.{case.get('name')}: {first[:200]}")
            if source:
                print(f"    {source[:200]}")
            print(f"    report: {report.name}")
    if total > limit:
        print(f"  … {total - limit} more failures")
    if not total:
        print("No test failures in reports; checking command and Editor logs.")
        for log in sorted(directory.glob("*.log")):
            lines = [line.strip() for line in log.read_text(errors="replace").splitlines()
                     if re.search(r"error CS\d+|error:.*Assets/Topaz/", line, re.I)]
            if lines:
                print(f"  {log.name}:")
                for line in lines[-limit:]:
                    print(f"    {line[:220]}")
            elif ERROR.search(log.read_text(errors="replace")):
                print(f"  inspect {log.name}")
    return 0


def observe(args: argparse.Namespace) -> int:
    command = args.command_args
    if command and command[0] == "--":
        command = command[1:]
    if not command:
        print("Pass a command after --", file=sys.stderr)
        return 2
    started = time.monotonic()
    result = subprocess.run(command, cwd=ROOT, capture_output=True, check=False)
    output = result.stdout + result.stderr
    directory = RESULTS / "observed"
    directory.mkdir(parents=True, exist_ok=True)
    log = directory / f"{uuid.uuid4().hex}.log"
    log.write_bytes(output)
    shown = output[:args.max_bytes].decode("utf-8", errors="replace")
    print(shown, end="" if shown.endswith("\n") else "\n")
    if len(output) > args.max_bytes:
        print(f"… {len(output) - args.max_bytes} more bytes in {log.relative_to(ROOT)}")
    append_observation({"time": datetime.now(timezone.utc).isoformat(),
                        "task": args.task or task_id(), "command": command,
                        "output_bytes": len(output), "shown_bytes": min(len(output), args.max_bytes),
                        "read_paths": args.read_path, "duration_seconds": round(time.monotonic() - started, 3),
                        "passed": result.returncode == 0, "log": str(log.relative_to(ROOT))})
    return result.returncode


def metrics(task: str | None) -> int:
    records = []
    steps = []
    for path in RUNS.glob("*/result.json"):
        try:
            record_data = json.loads(path.read_text())
            if task and record_data.get("task") != task:
                continue
            records.append(record_data)
            step_path = path.parent / "steps.jsonl"
            if step_path.exists():
                steps.extend(json.loads(line) for line in step_path.read_text().splitlines())
        except (OSError, json.JSONDecodeError):
            continue
    observations = []
    if OBSERVATIONS.exists():
        for line in OBSERVATIONS.read_text().splitlines():
            try:
                entry = json.loads(line)
                if not task or entry.get("task") == task:
                    observations.append(entry)
            except json.JSONDecodeError:
                continue
    reads: dict[str, int] = {}
    for entry in observations:
        for path in entry.get("read_paths", []):
            reads[path] = reads.get(path, 0) + 1
    repeats = sorted(((count, path) for path, count in reads.items() if count > 1), reverse=True)
    ordered = sorted(records, key=lambda item: item["time"])
    failures_before_pass = 0
    cycles = 0
    for entry in ordered:
        if not entry["passed"]:
            failures_before_pass += 1
        elif failures_before_pass:
            cycles += 1
            failures_before_pass = 0
    print(f"Task: {task or 'all'} | runs: {len(records)} | Unity steps: "
          f"{sum(step['label'].startswith(('editmode', 'playmode', 'build-')) for step in steps)}")
    print(f"Unity duration: {sum(step['duration_seconds'] for step in steps if step['label'].startswith(('editmode', 'playmode', 'build-'))):.1f}s")
    print(f"Unity log bytes: {sum(step['log_bytes'] for step in steps):,} | "
          f"observed tool bytes: {sum(entry['output_bytes'] for entry in observations):,} | "
          f"shown bytes: {sum(entry['shown_bytes'] for entry in observations):,}")
    print(f"Failure-to-pass cycles: {cycles} | unresolved failed runs: {failures_before_pass}")
    print("Repeated annotated reads: " +
          (", ".join(f"{path} ({count})" for count, path in repeats[:10]) if repeats else "none"))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    check = commands.add_parser("verify", help="Full gate, or focused tests without a build")
    check.add_argument("--quick", action="store_true")
    check.add_argument("--mode", choices=("EditMode", "PlayMode"))
    check.add_argument("--filter", help="Unity test name filter; requires --quick")
    check.add_argument("--area", action="append", default=[], help="Mapped area; repeatable; requires --quick")
    check.add_argument("--changed", action="store_true", help="Select mapped tests for dirty paths")
    build = commands.add_parser("build", help="Build a desktop player")
    build.add_argument("platform", choices=tuple(PROFILES), nargs="?", default="mac")
    commands.add_parser("doctor", help="Summarize local project and tool state")
    area = commands.add_parser("context", help="Bounded code, test, scene, and doc map for one area")
    area.add_argument("area", choices=tuple(load_areas()))
    area.add_argument("--max", type=int, default=30, help="Maximum code and asset paths (1-100)")
    failure = commands.add_parser("triage", help="Compact failures for a run ID or latest")
    failure.add_argument("run", nargs="?", default="latest")
    failure.add_argument("--max", type=int, default=10, help="Maximum failures (1-50)")
    measured = commands.add_parser("observe", help="Capture command output and task metrics")
    measured.add_argument("--task", help="Task ID; defaults to TOPAZ_AGENT_TASK")
    measured.add_argument("--read-path", action="append", default=[], help="Annotate a file read")
    measured.add_argument("--max-bytes", type=int, default=3000, help="Output budget (0-10000)")
    measured.add_argument("command_args", nargs=argparse.REMAINDER)
    report = commands.add_parser("metrics", help="Summarize run and observed-command costs")
    report.add_argument("--task", help="Limit to one task ID")
    args = parser.parse_args()
    if args.command == "verify":
        return verify(args)
    if args.command == "build":
        return build_platform(args.platform)
    if args.command == "context":
        return context(args.area, max(1, min(100, args.max)))
    if args.command == "triage":
        return triage(args.run, max(1, min(50, args.max)))
    if args.command == "observe":
        args.max_bytes = max(0, min(10000, args.max_bytes))
        return observe(args)
    if args.command == "metrics":
        return metrics(args.task)
    return doctor()


if __name__ == "__main__":
    raise SystemExit(main())
