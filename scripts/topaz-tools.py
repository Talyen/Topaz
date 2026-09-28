#!/usr/bin/env python3
"""Concise Unity check results. Complete command output stays in TestResults."""

from __future__ import annotations

import argparse
from contextlib import nullcontext
from datetime import datetime, timezone
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

# Also support importlib-based tool tests.
sys.path.insert(0, str(Path(__file__).resolve().parent))
import topaz_hygiene as housekeeping
import topaz_verification as validation
import topaz_iteration as iteration

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
    return not failures and len(cases) > skipped


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
        try:
            with validation.unity_operation(ROOT) if command[0] == "unity" else nullcontext():
                result = subprocess.run(command, cwd=ROOT, stdout=output, stderr=subprocess.STDOUT, check=False)
        except RuntimeError as error:
            output.write(str(error) + "\n")
            result = subprocess.CompletedProcess(command, 1)
    report_ok = summarize_xml(report) if report and report.is_file() else False
    if result.returncode or (report and not report_ok):
        summarize_log(log)
        editor_log = (Path(command[command.index("--log-file") + 1])
                      if "--log-file" in command else ROOT / "Logs/Editor.log")
        if editor_log.is_file() and command and command[0] == "unity":
            snapshot = directory / f"{label}-editor.log"
            if editor_log.resolve() != snapshot.resolve():
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
           fingerprint: str | None = None, started: float | None = None, **extra) -> None:
    (directory / "result.json").write_text(json.dumps({
        "time": datetime.now(timezone.utc).isoformat(), "kind": kind,
        "passed": ok, "details": details, "task": task_id(),
        "fingerprint": fingerprint,
        "duration_seconds": round(time.monotonic() - started, 3) if started else None, **extra},
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
    before = current_fingerprints()["build"] if own_run else None
    profile, output = PROFILES[platform]
    (ROOT / "Builds").mkdir(exist_ok=True)
    command = ["unity", "build", str(ROOT), "--target",
               "StandaloneOSX" if platform == "mac" else "StandaloneWindows64",
               "--args", f"-topazBuildProfile Assets/Topaz/Build/Profiles/{profile}",
               "--execute-method", "Topaz.Editor.PlayerBuild.BuildConfiguredProfile",
               "--output-path", str(ROOT / "Builds" / output), "--allow-dirty-build",
               "--log-file", str(directory / f"build-{platform}-editor.log"),
               "--timeout", "1200", "--no-tail", "--no-banner"]
    receipt_path = ROOT / "Builds" / (output + ".build-report.json")
    receipt_path.unlink(missing_ok=True)
    ok = run(command, f"build-{platform}", directory)
    if ok:
        try:
            receipt = json.loads(receipt_path.read_text())
            ok = (receipt.get("result") == "Succeeded" and receipt.get("errors") == 0
                  and (ROOT / "Builds" / output).exists())
        except (OSError, ValueError):
            ok = False
        if not ok:
            print("Build did not produce a successful BuildReport receipt; an older output is not proof of success.")
    if own_run:
        after = current_fingerprints()["build"]
        if before != after:
            print("Project inputs changed during the build; result is not reusable")
            ok = False
        record(directory, f"build-{platform}", ok, output, started=started, fingerprint_version=validation.SCHEMA,
               fingerprints={"build": after}, stages={"build": {"domain": "build", "fingerprint": before, "passed": ok, "reusable": ok}})
    return 0 if ok else 1


def current_fingerprints():
    return validation.fingerprints(validation.snapshot(ROOT, project_paths()))


def begin_task(name):
    if not name or name == "unassigned":
        raise ValueError("Choose a task ID: begin --task <name> or set TOPAZ_AGENT_TASK")
    directory = new_run()
    (directory / "baseline.json").write_text(json.dumps(validation.snapshot(ROOT, project_paths())))
    record(directory, "baseline", True, "Task input snapshot", task=name)
    print(f"Task baseline: {directory.relative_to(ROOT)}; use TOPAZ_AGENT_TASK={name} for verification")
    return 0


def task_changes():
    name = task_id()
    baselines = []
    for path in RUNS.glob("*/result.json"):
        try:
            data = json.loads(path.read_text())
            if data.get("kind") == "baseline" and data.get("task") == name:
                baselines.append((data["time"], path.parent))
        except (OSError, ValueError, KeyError):
            continue
    if baselines:
        before = json.loads((max(baselines)[1] / "baseline.json").read_text())
        return validation.delta(before, validation.snapshot(ROOT, project_paths()))
    print("No task baseline; selecting against all Git changes. Use begin before the next task or --path for explicit scope.")
    return changed_paths()


def verify(args: argparse.Namespace) -> int:
    full, stress = getattr(args, "full", False), getattr(args, "stress", False)
    paths = getattr(args, "path", [])
    if args.filter and not args.mode:
        raise ValueError("--filter requires --mode EditMode or --mode PlayMode")
    if sum(bool(x) for x in (args.area, paths, args.changed, args.filter or args.mode)) > 1:
        raise ValueError("Choose --area, --path, --changed, or --mode/--filter")
    if full and (args.area or paths or args.changed or args.filter or args.mode):
        raise ValueError("--full runs both complete suites; omit selectors")
    areas = load_areas()
    if any(area not in areas for area in args.area):
        raise ValueError("Unknown area. Choose: " + ", ".join(areas))
    for name in paths:
        if Path(name).is_absolute() or ".." in Path(name).parts:
            raise ValueError("--path requires repository-relative paths")
    selected, unknown, changed = [], [], []
    if full or (stress and not (args.area or paths or args.changed or args.mode)):
        tests = {"EditMode": [None], "PlayMode": [None]}
    elif args.mode:
        tests = {args.mode: [args.filter] if args.filter else [None]}
    elif args.area:
        selected = args.area
        tests = {}
        for area in selected:
            for mode, names in areas[area]["tests"].items():
                tests.setdefault(mode, []).extend(names)
        tests = {mode: sorted(set(names)) for mode, names in tests.items()}
    else:
        changed = paths or task_changes()
        tests, selected, unknown = validation.selected_tests(changed, areas, {p for p in project_paths() if (ROOT / p).is_file()})
    if unknown:
        print("Shared/unmapped Unity inputs; running both non-stress suites: " + ", ".join(unknown[:6]))
    category = None if full else "Stress" if stress else "!Stress"
    selection = {mode: {"names": names, "category": category} for mode, names in tests.items()}
    print("Selection: " + (json.dumps(selection) if selection else "Python, hygiene and static checks only"))
    risk = validation.windows_reasons(changed, ROOT)
    if risk:
        print("Windows cross-build recommended for these inputs (explicit build command): " + ", ".join(risk[:6]))
    if getattr(args, "dry_run", False):
        for mode, names in tests.items():
            print("Unity batch: " + json.dumps(validation.test_command(ROOT, mode, names, category, Path("<report>"))))
        return 0
    if tests and shutil.which("unity") is None:
        raise RuntimeError("Unity CLI is required for the selected Unity tests")
    directory = new_run()
    started = time.monotonic()
    kind = "full" if full else "stress" if stress else "fast"
    stages = {}
    def stage(label, domain, action):
        before = current_fingerprints()[domain]
        passed = action()
        after = current_fingerprints()[domain]
        stages[label] = {"domain": domain, "fingerprint": before, "passed": bool(passed), "reusable": bool(passed and before == after)}
        if before != after:
            print(f"{label}: relevant inputs changed; this stage is not reusable")
        return stages[label]["reusable"]
    def finish(ok, detail):
        now = current_fingerprints()
        for item in stages.values():
            item["reusable"] = item["reusable"] and item["fingerprint"] == now[item["domain"]]
        ok = ok and all(item["reusable"] for item in stages.values())
        record(directory, kind, ok, detail if ok else detail + "; inspect stage evidence", started=started,
               fingerprint_version=validation.SCHEMA, fingerprints=now, stages=stages, selection=selection)
        print(f"Topaz {kind} verification: {'PASS' if ok else 'FAIL'} (no player build requested)")
        return 0 if ok else 1
    def tool_checks():
        return run([sys.executable, "-m", "unittest", "discover", "-s", "scripts", "-p", "test_*.py"], "tool-tests", directory) and bool(re.search(r"Ran [1-9]\d* tests?\b", (directory / "tool-tests.log").read_text()))
    if not stage("tooling", "tooling", tool_checks):
        return finish(False, "tooling")
    if not stage("documentation", "documentation", lambda: housekeeping.hygiene(ROOT) == 0):
        return finish(False, "documentation")
    if tests and not stage("asset-metadata", "editmode", lambda: run([sys.executable, str(ROOT / "scripts/check-assets.py")], "check-assets", directory)):
        return finish(False, "asset metadata")
    for mode, names in tests.items():
        label = mode.lower() + "-batch"
        report = directory / (label + ".xml")
        command = validation.test_command(ROOT, mode, names, category, report)
        if not stage(label, mode.lower(), lambda: run(command, label, directory, report)):
            return finish(False, mode + " tests")
    # Documentation changes don't invalidate runtime tests. Recheck only the cheap owning stage.
    if stages["documentation"]["fingerprint"] != current_fingerprints()["documentation"]:
        print("Documentation changed; refreshing documentation checks only")
        if not stage("documentation", "documentation", lambda: housekeeping.hygiene(ROOT) == 0):
            return finish(False, "documentation")
    diff = subprocess.run(["git", "diff", "--check"], cwd=ROOT, capture_output=True, text=True)
    if diff.returncode:
        print(diff.stdout or diff.stderr)
        return finish(False, "git diff --check")
    return finish(True, "Selected " + ", ".join(tests) + " checks passed" if tests else "Static checks only")


def doctor() -> int:
    housekeeping.hygiene(ROOT)
    housekeeping.summary(ROOT)
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
    current = current_fingerprints()
    for kind, title in (("fast", "Fast checks"), ("full", "Full regression"), ("stress", "Stress tests"), ("build-mac", "Mac build"), ("build-windows", "Windows build")):
        last = latest_record(kind)
        if not last:
            print(f"{title}: none recorded")
            continue
        if last.get("fingerprint_version") == validation.SCHEMA:
            stages = last.get("stages", {})
            fresh = bool(stages) and all(s["reusable"] and s["fingerprint"] == current[s["domain"]] for s in stages.values())
            stale = sorted({s["domain"] for s in stages.values() if not s["reusable"] or s["fingerprint"] != current[s["domain"]]})
            freshness = "current" if fresh else "refresh " + ", ".join(stale or ["unknown stages"]) + "; other recorded domains remain current"
        else:
            freshness = "legacy receipt; domain freshness unknown"
        print(f"{title}: {'PASS' if last['passed'] else 'FAIL'} at {last['time']} ({freshness}; {last['details']})")
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
    timings = []
    for step in steps:
        report = ROOT / step["report"] if step.get("report") else None
        if report and report.is_file():
            try:
                for case in ET.parse(report).getroot().findall(".//testcase"):
                    timings.append((float(case.get("time", "0")), case.get("classname", "") + "." + case.get("name", "")))
            except (OSError, ValueError, ET.ParseError):
                continue
    for seconds, name in sorted(timings, reverse=True)[:5]:
        print(f"Slow test: {seconds:.2f}s {name}")
    print("Repeated annotated reads: " +
          (", ".join(f"{path} ({count})" for count, path in repeats[:10]) if repeats else "none"))
    return 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    check = commands.add_parser("verify", help="Affected non-stress tests by default; player builds are explicit")
    check.add_argument("--quick", action="store_true", help="Compatibility alias for the default fast checks")
    suite = check.add_mutually_exclusive_group()
    suite.add_argument("--full", action="store_true", help="Both complete suites including stress; no player build")
    suite.add_argument("--stress", action="store_true", help="Only exhaustive/stress cases")
    check.add_argument("--path", action="append", default=[], help="Task-owned changed path; repeatable")
    check.add_argument("--dry-run", action="store_true", help="Print selection/commands without running checks")
    loop = commands.add_parser("iterate", help="Bounded Editor status; no implicit tests or builds")
    loop.add_argument("--refresh", action="store_true", help="Explicitly refresh changed assets/scripts in the connected Editor")
    loop.add_argument("--path", action="append", default=[], help="Task-owned path for scope warnings; repeatable")
    loop.add_argument("--batch", action="store_true", help="Explicit named tests in batch mode; only with the Editor closed")
    loop.add_argument("--mode", choices=("EditMode", "PlayMode"))
    loop.add_argument("--filter", help="Named test/fixture; required with --batch and --mode")
    loop.add_argument("--dry-run", action="store_true", help="Show scope without contacting Unity")
    begin = commands.add_parser("begin", help="Snapshot the current checkout before starting a task")
    begin.add_argument("--task", default=task_id(), help="Task label, also set TOPAZ_AGENT_TASK for later checks")
    check.add_argument("--mode", choices=("EditMode", "PlayMode"))
    check.add_argument("--filter", help="Unity test name filter; ")
    check.add_argument("--area", action="append", default=[], help="Mapped area; repeatable; ")
    check.add_argument("--changed", action="store_true", help="Select changes since task baseline, falling back to Git changes")
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
    commands.add_parser("hygiene", help="Check current documentation and repository inputs without Unity")
    clean = commands.add_parser("cleanup", help="Preview expired managed local output")
    clean.add_argument("--apply", action="store_true", help="Delete only eligible managed output")
    artifacts = commands.add_parser("artifact", help="Register completed artifacts or change retention pins")
    ops = artifacts.add_subparsers(dest="operation", required=True)
    register = ops.add_parser("register")
    register.add_argument("path", help="Repository-relative completed artifact directory")
    register.add_argument("--category", required=True, choices=sorted(housekeeping.ARTIFACT_KINDS))
    register.add_argument("--run", action="append", default=[], help="Related completed run ID; repeatable")
    register.add_argument("--pin", help="Reason for retaining this artifact indefinitely")
    pin = ops.add_parser("pin")
    pin.add_argument("path")
    pin.add_argument("--reason", required=True)
    unpin = ops.add_parser("unpin")
    unpin.add_argument("path")
    args = parser.parse_args()
    if args.command == "iterate":
        with housekeeping.output_lock(ROOT):
            return iteration.iterate(args, argparse.Namespace(**globals()))
    if args.command == "begin":
        with housekeeping.output_lock(ROOT):
            return begin_task(args.task)
    if args.command == "hygiene":
        return housekeeping.hygiene(ROOT)
    if args.command == "cleanup":
        return housekeeping.cleanup(ROOT, args.apply)
    if args.command == "artifact":
        if args.operation == "register":
            housekeeping.register(ROOT, args.path, args.category, args.run, args.pin)
        else:
            housekeeping.set_pin(ROOT, args.path, args.reason if args.operation == "pin" else None)
        return 0
    if args.command == "verify" and args.dry_run:
        return verify(args)
    if args.command in ("verify", "build"):
        try:
            with housekeeping.output_lock(ROOT):
                result = verify(args) if args.command == "verify" else build_platform(args.platform)
        finally:
            try:
                housekeeping.cleanup(ROOT, apply=True, quiet=True)
            except Exception as error:
                print(f"Retention warning (verification result unchanged): {error}")
        return result
    if args.command == "context":
        return context(args.area, max(1, min(100, args.max)))
    if args.command == "triage":
        return triage(args.run, max(1, min(50, args.max)))
    if args.command == "observe":
        args.max_bytes = max(0, min(10000, args.max_bytes))
        with housekeeping.output_lock(ROOT):
            return observe(args)
    if args.command == "metrics":
        return metrics(args.task)
    return doctor()


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (ValueError, OSError, RuntimeError) as error:
        print(f"Topaz tools: {error}", file=sys.stderr)
        raise SystemExit(1)
