"""Fast-loop safety and honest feedback without starting Unity."""
import argparse
from contextlib import redirect_stdout
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

import topaz_iteration as loop

spec = importlib.util.spec_from_file_location('iteration_tools', Path(__file__).with_name('topaz-tools.py'))
tools = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tools)


class IterationTests(unittest.TestCase):
    def setUp(self):
        temp = tempfile.TemporaryDirectory()
        self.addCleanup(temp.cleanup)
        self.root = Path(temp.name)
        for name, value in [('ROOT', self.root), ('RUNS', self.root/'TestResults/runs')]:
            p = patch.object(tools, name, value); p.start(); self.addCleanup(p.stop)
        p = patch.object(tools, 'load_areas', return_value={}); p.start(); self.addCleanup(p.stop)
        self.out = io.StringIO()
        p = redirect_stdout(self.out); p.__enter__(); self.addCleanup(p.__exit__, None, None, None)

    def args(self, **kw):
        values = dict(path=['Assets/Topaz/World/Scenes/Bootstrap.unity', 'Assets/New.asset'],
                      batch=False, mode=None, filter=None, refresh=False, dry_run=False)
        values.update(kw)
        return argparse.Namespace(**values)

    def ready(self):
        return dict(projectPath=str(self.root), status='ready', playMode='stopped', compiling=False, domainReloadInProgress=False)

    def receipt(self):
        return json.loads(next(tools.RUNS.glob('*/result.json')).read_text())

    def test_shared_and_unmapped_changes_never_start_tests_or_builds(self):
        with patch.object(loop, 'editor_command', side_effect=[self.ready(), {'status':'up_to_date'}]) as cmd, patch.object(tools, 'run') as run, patch.object(tools, 'build_platform') as build:
            self.assertEqual(loop.iterate(self.args(), tools), 0)
            run.assert_not_called(); build.assert_not_called()
            self.assertEqual([c.args[1] for c in cmd.call_args_list], ['editor_status','recompile_status'])
        self.assertEqual(self.receipt()['selection'], {})
        self.assertFalse(self.receipt()['reusable'])
        self.assertIn('Scope warning', self.out.getvalue())

    def test_unreachable_server_has_no_fallback(self):
        with patch.object(loop, 'editor_command', side_effect=RuntimeError('unreachable')), patch.object(tools, 'run') as run:
            self.assertEqual(loop.iterate(self.args(refresh=True), tools), 2)
            run.assert_not_called()
        self.assertFalse(self.receipt()['passed'])

    def test_compile_failure_cannot_pass(self):
        with patch.object(loop, 'editor_command', side_effect=[self.ready(), {'status':'completed','failed':True}]):
            self.assertEqual(loop.iterate(self.args(), tools), 1)

    def test_busy_or_unknown_status_does_not_trigger_refresh(self):
        for status in ('compiling','reloading','blocked_by_dialog'):
            with patch.object(loop, 'editor_command', return_value=dict(self.ready(), status=status)) as cmd:
                self.assertEqual(loop.iterate(self.args(refresh=True), tools), 2)
                self.assertEqual(cmd.call_count, 1)

    def test_refresh_is_explicit_and_preserves_play_session(self):
        with patch.object(loop, 'editor_command', side_effect=[self.ready(), {'status':'compiling'}]) as cmd:
            self.assertEqual(loop.iterate(self.args(refresh=True), tools), 2)
            self.assertEqual(cmd.call_args.args[1], 'recompile')
        with patch.object(loop, 'editor_command', return_value=dict(self.ready(), status='playing', playMode='playing')) as cmd:
            self.assertEqual(loop.iterate(self.args(refresh=True), tools), 2)
            self.assertEqual(cmd.call_count, 1)

    def test_wrong_project_never_refreshes(self):
        with patch.object(loop, 'editor_command', return_value=dict(self.ready(), projectPath='/elsewhere')) as cmd:
            self.assertEqual(loop.iterate(self.args(refresh=True), tools), 2)
            self.assertEqual(cmd.call_count, 1)

    def test_no_evidence_is_not_compile_success(self):
        with patch.object(loop, 'editor_command', side_effect=[self.ready(), {'status':'idle'}]):
            self.assertEqual(loop.iterate(self.args(), tools), 2)

    def test_batch_requires_named_selection_and_refuses_open_editor(self):
        for kw in [dict(batch=True), dict(mode='PlayMode'), dict(batch=True,mode='PlayMode',filter='*'), dict(batch=True,mode='PlayMode',filter='A',refresh=True)]:
            with self.assertRaises(ValueError): loop.iterate(self.args(**kw), tools)
        (self.root/'Temp').mkdir(); (self.root/'Temp/UnityLockfile').touch()
        with patch.object(tools, 'run') as run:
            self.assertEqual(loop.iterate(self.args(batch=True, mode='PlayMode', filter='MovementInputTests'), tools), 2)
            run.assert_not_called()

    def test_explicit_batch_selection_and_input_freshness(self):
        with patch.object(tools, 'run', return_value=True) as run, patch.object(tools, 'current_fingerprints', side_effect=[{'playmode':'a'}, {'playmode':'b'}]), patch.object(loop, 'editor_command') as editor:
            self.assertEqual(loop.iterate(self.args(batch=True,mode='PlayMode',filter='MovementInputTests'),tools),1)
            command = run.call_args.args[0]
            self.assertEqual(command[command.index('--filter')+1], 'MovementInputTests')
            self.assertEqual(command[-1], '!Stress'); editor.assert_not_called()
        self.assertEqual(self.receipt()['selection'], {'PlayMode':['MovementInputTests']})
        self.assertFalse(self.receipt()['evidence']['inputs_unchanged'])

    def test_cli_route_dry_run_has_no_editor_side_effects(self):
        with patch.object(tools.sys, 'argv', ['topaz-tools.py','iterate','--dry-run','--path','Assets/New.asset']), patch.object(loop, 'editor_command') as editor:
            self.assertEqual(tools.main(), 0); editor.assert_not_called()

    def test_json_status_and_command_timeout(self):
        response = dict(success=True, data=json.dumps({'status':'completed','failed':True}))
        with patch.object(loop.subprocess, 'run', return_value=argparse.Namespace(returncode=0,stdout=json.dumps(response),stderr='')) as run:
            self.assertTrue(loop.editor_command(self.root,'recompile_status')['failed'])
            self.assertEqual(run.call_args.kwargs['timeout'],20)
            self.assertIn(str(self.root),run.call_args.args[0])
        with patch.object(loop, 'editor_command', side_effect=subprocess.TimeoutExpired('unity',20)):
            self.assertEqual(loop.iterate(self.args(),tools),2)

    def test_wrapped_pipeline_response_and_cli_flag_position(self):
        response = dict(success=True, data=dict(success=True, result={'status': 'ready'}))
        with patch.object(loop.subprocess, 'run', return_value=argparse.Namespace(returncode=0, stdout=json.dumps(response), stderr='')) as run:
            self.assertEqual(loop.editor_command(self.root, 'editor_status')['status'], 'ready')
            command = run.call_args.args[0]
            self.assertLess(command.index('--caller'), command.index('editor_status'))
        response['data']['success'] = False
        with patch.object(loop.subprocess, 'run', return_value=argparse.Namespace(returncode=0, stdout=json.dumps(response), stderr='')):
            with self.assertRaises(RuntimeError):
                loop.editor_command(self.root, 'editor_status')

    def test_selected_test_failure_and_success_remain_narrow(self):
        for ok, expected in ((False, 1), (True, 0)):
            with patch.object(tools, 'run', return_value=ok), patch.object(tools, 'current_fingerprints', return_value={'editmode':'same'}):
                self.assertEqual(loop.iterate(self.args(batch=True, mode='EditMode', filter='ProfilePersistenceTests'), tools), expected)

    def test_malformed_editor_envelope_is_not_success(self):
        for payload in ('[]', '{"success":true,"data":null}', 'not json'):
            with patch.object(loop.subprocess, 'run', return_value=argparse.Namespace(returncode=0, stdout=payload, stderr='')):
                with self.assertRaises(RuntimeError): loop.editor_command(self.root, 'editor_status')
