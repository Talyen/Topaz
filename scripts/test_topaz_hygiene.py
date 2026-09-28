"""Safety/behavior regressions for cleanup and first-party documentation checks."""
from contextlib import redirect_stdout
from datetime import datetime, timedelta, timezone
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import topaz_hygiene as h


class RepositoryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name).resolve()
        subprocess.run(['git', 'init', '-q', str(self.root)], check=True)
        self.write('.gitignore', 'TestResults/\nBuilds/\n')
        self.write('scripts/repository-policy.json', json.dumps({
            'documents': {'docs/CURRENT.md': 'Current rules', 'README.md': 'Entrypoint'}, 'retention_days': 14, 'retention_count': 10,
            'forbidden_doc_roots': ['docs/history'], 'generated_roots': ['Builds', 'TestResults', 'Library']}))
        self.write('scripts/agent-areas.json', '{}')
        self.write('docs/CURRENT.md', '# Current\n\n## Shared rules\n')
        self.write('README.md', '[Rules](docs/CURRENT.md#shared-rules)\n')
        self.opened = patch.object(h, 'open_paths', return_value=set())
        self.opened.start()
        self.addCleanup(self.opened.stop)
        self.out = io.StringIO()
        self.capture = redirect_stdout(self.out)
        self.capture.__enter__()
        self.addCleanup(self.capture.__exit__, None, None, None)

    def write(self, name, content):
        path = self.root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(content)
        return path

    def old(self, days):
        return (datetime.now(timezone.utc) - timedelta(days=days)).isoformat()

    def run_record(self, n, days=30, passed=False, kind='full'):
        name = f'20260101T000000.{n:06d}Z-abcdef12'
        path = self.write(f'TestResults/runs/{name}/result.json', json.dumps({
            'kind': kind, 'time': self.old(days), 'passed': passed}))
        return path.parent

    def artifact(self, name, days=30, runs=(), pin=None):
        path = self.write(f'TestResults/artifacts/{name}/data.txt', name).parent
        self.write(str(path.relative_to(self.root) / h.META), json.dumps({
            'version': 1, 'completed': self.old(days), 'category': 'diagnostic', 'runs': list(runs), 'pin': pin}))
        return path

    def test_current_docs_and_reference_links_pass(self):
        self.write('README.md', '[Rules][rules]\n\n[rules]: docs/CURRENT.md#shared-rules\n')
        self.assertEqual(h.hygiene(self.root), 0)

    def test_untracked_docs_images_missing_catalog_and_broken_anchors_fail(self):
        self.write('docs/new.png', 'image')
        self.write('docs/history/old.md', '# Old')
        self.write('README.md', '[missing](docs/CURRENT.md#gone)')
        self.assertEqual(h.hygiene(self.root), 1)
        self.assertIn('unregistered', self.out.getvalue())
        self.assertIn('missing anchor', self.out.getvalue())
        (self.root / 'docs/CURRENT.md').unlink()
        self.assertEqual(h.hygiene(self.root), 1)
        self.assertIn('missing catalog', self.out.getvalue())

    def test_force_added_generated_output_fails(self):
        self.write('Builds/output.txt', 'generated')
        subprocess.run(['git', 'add', '-f', 'Builds/output.txt'], cwd=self.root, check=True)
        self.assertEqual(h.hygiene(self.root), 1)
        self.assertIn('must not be versioned', self.out.getvalue())

    def test_age_or_count_retains_and_preview_matches_apply(self):
        old = self.run_record(0, 50)
        newest = [self.run_record(n, 30 - n) for n in range(1, 11)]
        recent = self.run_record(11, 1, kind='quick')
        self.assertEqual(h.cleanup(self.root), 0)
        self.assertTrue(old.exists())
        preview = self.out.getvalue().split('Cleanup preview: ')[-1]
        self.out.truncate(0); self.out.seek(0)
        h.cleanup(self.root, apply=True)
        self.assertFalse(old.exists())
        self.assertTrue(all(p.exists() for p in newest + [recent]))
        self.assertEqual(preview, self.out.getvalue().split('Cleanup applied: ')[-1])

    def test_pins_linked_runs_and_latest_success_survive(self):
        success = self.run_record(0, 100, passed=True)
        linked = self.run_record(1, 90)
        for n in range(2, 13):
            self.run_record(n, 40 - n)
        art = self.artifact('pinned', 100, [linked.name], 'Investigating')
        for n in range(11):
            self.artifact(f'new-{n}', 20 - n)
        h.cleanup(self.root, apply=True)
        self.assertTrue(success.exists())
        self.assertTrue(linked.exists())
        self.assertTrue(art.exists())

    def test_writer_lock_prevents_deletion(self):
        old = self.run_record(0, 100)
        for n in range(1, 11): self.run_record(n)
        with h.output_lock(self.root):
            h.cleanup(self.root, apply=True)
        self.assertTrue(old.exists())
        self.assertIn('writer', self.out.getvalue())

    def test_malformed_and_incomplete_metadata_is_preserved(self):
        incomplete = self.write('TestResults/runs/20260101T000000.000000Z-abcdef12/step.log', 'in progress')
        broken = self.write('TestResults/artifacts/broken/' + h.META, '{}')
        h.cleanup(self.root, apply=True)
        self.assertTrue(incomplete.exists())
        self.assertTrue(broken.exists())

    def test_tracked_and_open_artifacts_are_protected(self):
        tracked = self.artifact('tracked', 100)
        opened = self.artifact('open', 99)
        for n in range(11): self.artifact(f'new-{n}', 30 - n)
        subprocess.run(['git', 'add', '-f', str((tracked / 'data.txt').relative_to(self.root))], cwd=self.root, check=True)
        with patch.object(h, 'open_paths', return_value={opened / 'data.txt'}):
            h.cleanup(self.root, apply=True)
        self.assertTrue(tracked.exists())
        self.assertTrue(opened.exists())

    def test_changed_since_inventory_is_not_deleted(self):
        old = self.run_record(0, 100)
        for n in range(1, 11): self.run_record(n)
        original = h.inventory
        def changed(root):
            result = original(root)
            (old / 'new.log').write_text('new data')
            return result
        with patch.object(h, 'inventory', side_effect=changed):
            h.cleanup(self.root, apply=True)
        self.assertTrue(old.exists())
        self.assertIn('changed during cleanup', self.out.getvalue())

    def test_escape_symlinks_and_nested_registrations_rejected(self):
        outside = self.write('source/keep.txt', 'source')
        candidate = self.artifact('escape')
        (candidate / 'link').symlink_to(outside)
        with self.assertRaises(ValueError):
            h.snapshot(candidate)
        for path in ['../source', '/tmp/source', 'Assets/Topaz']:
            with self.assertRaises(ValueError): h.safe_path(self.root, path)
        (self.root / 'Builds').mkdir()
        (self.root / 'Builds/linked').symlink_to(self.root / 'source', target_is_directory=True)
        with self.assertRaises(ValueError): h.safe_path(self.root, 'Builds/linked')
        parent = self.artifact('parent')
        (parent / 'nested').mkdir()
        with self.assertRaises(ValueError):
            h.register(self.root, 'TestResults/artifacts/parent/nested', 'diagnostic')

    def test_observation_log_and_ledger_expire_together(self):
        rows = []
        for n in range(11):
            name = f'TestResults/observed/{n:032x}.log'
            self.write(name, 'output')
            rows.append({'time': self.old(50 - n), 'log': name})
        ledger = self.write('TestResults/agent-observations.jsonl', ''.join(json.dumps(r)+'\n' for r in rows))
        h.cleanup(self.root, apply=True)
        self.assertFalse((self.root / rows[0]['log']).exists())
        self.assertEqual(len(ledger.read_text().splitlines()), 10)
        self.assertTrue((self.root / rows[-1]['log']).exists())

    def test_register_pin_unpin_preserves_completion_time(self):
        self.write('Builds/reviews/new/player', 'binary')
        h.register(self.root, 'Builds/reviews/new', 'review', pin='Current review')
        meta = self.root / 'Builds/reviews/new' / h.META
        before = json.loads(meta.read_text())
        h.set_pin(self.root, 'Builds/reviews/new', None)
        after = json.loads(meta.read_text())
        self.assertEqual(before['completed'], after['completed'])
        self.assertIsNone(after['pin'])

    def test_malformed_artifact_preserves_potential_run_dependencies(self):
        oldest = self.run_record(0, 100)
        for n in range(1, 11): self.run_record(n)
        self.write('TestResults/artifacts/broken/' + h.META, '{')
        h.cleanup(self.root, apply=True)
        self.assertTrue(oldest.exists())

    def test_missing_open_file_inspection_fails_closed(self):
        old = self.run_record(0, 100)
        for n in range(1, 11): self.run_record(n)
        with patch.object(h, 'open_paths', side_effect=RuntimeError('unavailable')):
            with self.assertRaises(RuntimeError): h.cleanup(self.root, apply=True)
        self.assertTrue(old.exists())

    def test_deleted_tracked_doc_and_bad_task_map_are_reported(self):
        self.write('scripts/agent-areas.json', json.dumps({'test': {'paths': ['missing/'], 'docs': [], 'scenes': [], 'tests': {}}}))
        self.assertEqual(h.hygiene(self.root), 1)
        self.assertIn('missing path', self.out.getvalue())

    def test_symlink_metadata_cannot_be_pinned(self):
        original = self.write('source/data.json', '{}')
        self.write('Builds/reviews/new/player', 'binary')
        (self.root / 'Builds/reviews/new' / h.META).symlink_to(original)
        with self.assertRaises(ValueError): h.set_pin(self.root, 'Builds/reviews/new', 'retain')
        self.assertEqual(original.read_text(), '{}')

    def test_cannot_register_container_or_invent_category(self):
        self.write('TestResults/artifacts/new/item.txt', 'generated')
        with self.assertRaises(ValueError): h.register(self.root, 'TestResults/artifacts', 'diagnostic')
        with self.assertRaises(ValueError): h.register(self.root, 'TestResults/artifacts/new', 'task-1')

    def test_newest_failure_is_retained_without_replacing_last_success(self):
        last_success = self.run_record(0, 100, True)
        newest = [self.run_record(n, 30 - n, False) for n in range(1, 13)]
        h.cleanup(self.root, apply=True)
        self.assertTrue(last_success.exists())
        self.assertTrue(newest[-1].exists())
        self.assertFalse(json.loads((newest[-1] / 'result.json').read_text())['passed'])


    def test_root_handoff_cannot_bypass_document_catalog(self):
        self.write('COMPLETED_HANDOFF.md', '# Finished')
        self.assertEqual(h.hygiene(self.root), 1)
        self.assertIn('COMPLETED_HANDOFF.md: unregistered', self.out.getvalue())
