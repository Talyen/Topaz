#!/usr/bin/env python3
"""Focused checks for the result summarizer without launching Unity."""

from contextlib import redirect_stdout
from concurrent.futures import ThreadPoolExecutor
import importlib.util
import io
import json
from pathlib import Path
import sys
import tempfile
import unittest


path = Path(__file__).with_name("topaz-tools.py")
spec = importlib.util.spec_from_file_location("topaz_tools", path)
tools = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tools)


class SummaryTests(unittest.TestCase):
    def test_failed_case_is_named_with_useful_message(self):
        with tempfile.TemporaryDirectory() as folder:
            report = Path(folder) / "result.xml"
            report.write_text("""<testsuites><testsuite><testcase classname="Topaz.Tests.SaveTests"
                name="LoadsBackup"><failure message="Expected backup">at SaveTests.cs:42</failure>
                </testcase></testsuite></testsuites>""")
            output = io.StringIO()
            with redirect_stdout(output):
                passed = tools.summarize_xml(report)
            self.assertFalse(passed)
            self.assertIn("Topaz.Tests.SaveTests.LoadsBackup", output.getvalue())
            self.assertIn("SaveTests.cs:42", output.getvalue())

    def test_empty_report_cannot_pass_filtered_run(self):
        with tempfile.TemporaryDirectory() as folder:
            report = Path(folder) / "result.xml"
            report.write_text("<testsuites />")
            with redirect_stdout(io.StringIO()):
                self.assertFalse(tools.summarize_xml(report))

    def test_overlapping_runs_keep_separate_logs_and_check_history(self):
        with tempfile.TemporaryDirectory() as folder:
            original_root, original_runs = tools.ROOT, tools.RUNS
            try:
                tools.ROOT = Path(folder)
                tools.RUNS = Path(folder) / "runs"
                first, second = tools.new_run(), tools.new_run()
                self.assertNotEqual(first, second)
                with redirect_stdout(io.StringIO()), ThreadPoolExecutor(max_workers=2) as pool:
                    calls = [pool.submit(tools.run, [sys.executable, "-c", f"print('{name}')"],
                                         "check", directory)
                             for name, directory in (("first", first), ("second", second))]
                    self.assertTrue(all(call.result() for call in calls))
                self.assertEqual((first / "check.log").read_text().strip(), "first")
                self.assertEqual((second / "check.log").read_text().strip(), "second")
                tools.record(first, "full", True, "all checks")
                tools.record(second, "build-windows", False, "build failed")
                self.assertTrue(tools.latest_record("full")["passed"])
                self.assertFalse(tools.latest_record("build-windows")["passed"])
            finally:
                tools.ROOT, tools.RUNS = original_root, original_runs

    def test_changed_selection_fails_open_for_unmapped_unity_input(self):
        areas = tools.load_areas()
        selected, unknown = tools.selection_for_changes([
            "Assets/Topaz/Gameplay/Combat/Runtime/EnemyCombatant.cs",
            "Packages/manifest.json"], areas)
        self.assertIn("combat", selected)
        self.assertEqual(unknown, ["Packages/manifest.json"])

    def test_area_map_points_to_existing_tests_docs_and_scenes(self):
        for area in tools.load_areas().values():
            for path in area["docs"] + area["scenes"]:
                self.assertTrue((tools.ROOT / path).is_file(), path)
            for mode, names in area["tests"].items():
                folder = "Editor" if mode == "EditMode" else "PlayMode"
                for name in names:
                    self.assertTrue((tools.ROOT / "Assets/Topaz/Tests" / folder /
                                     f"{name}.cs").is_file(), name)

    def test_fingerprint_includes_untracked_content_not_only_names(self):
        with tempfile.TemporaryDirectory() as folder:
            original_root, original_paths = tools.ROOT, tools.project_paths
            try:
                tools.ROOT = Path(folder)
                target = tools.ROOT / "new.cs"
                target.write_text("first")
                tools.project_paths = lambda: ["new.cs"]
                first = tools.input_fingerprint()
                target.write_text("second")
                self.assertNotEqual(first, tools.input_fingerprint())
            finally:
                tools.ROOT, tools.project_paths = original_root, original_paths

    def test_triage_gives_bounded_failure_and_source(self):
        with tempfile.TemporaryDirectory() as folder:
            original_root, original_runs = tools.ROOT, tools.RUNS
            try:
                tools.ROOT = Path(folder)
                tools.RUNS = tools.ROOT / "runs"
                directory = tools.RUNS / "example"
                directory.mkdir(parents=True)
                (directory / "play.xml").write_text("""<testsuites><testsuite>
                    <testcase classname="Topaz.Tests.SaveTests" name="LoadsBackup">
                    <failure message="Expected backup">at Assets/Topaz/Tests/SaveTests.cs:42</failure>
                    </testcase></testsuite></testsuites>""")
                (directory / "result.json").write_text(json.dumps({"passed": False, "details": "tests"}))
                output = io.StringIO()
                with redirect_stdout(output):
                    self.assertEqual(tools.triage("example", 1), 0)
                self.assertIn("Assets/Topaz/Tests/SaveTests.cs:42", output.getvalue())
            finally:
                tools.ROOT, tools.RUNS = original_root, original_runs


if __name__ == "__main__":
    unittest.main()
