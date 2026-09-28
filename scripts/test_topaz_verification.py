"""Verification scope/freshness regressions, without starting Unity."""
import argparse
from contextlib import redirect_stdout
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import topaz_verification as v
spec = importlib.util.spec_from_file_location('speed_tools', Path(__file__).with_name('topaz-tools.py'))
tools = importlib.util.module_from_spec(spec)
spec.loader.exec_module(tools)


class IdentityTests(unittest.TestCase):
    def test_docs_cannot_invalidate_runtime_but_gameplay_must(self):
        before = {'docs/ROADMAP.md':'a', 'Assets/Topaz/Player/Runtime/PlayerController.cs':'one'}
        docs = dict(before, **{'docs/ROADMAP.md':'b'})
        code = dict(before, **{'Assets/Topaz/Player/Runtime/PlayerController.cs':'two'})
        self.assertNotEqual(v.fingerprint(before,'documentation'),v.fingerprint(docs,'documentation'))
        for domain in ('editmode','playmode','build'):
            self.assertEqual(v.fingerprint(before,domain),v.fingerprint(docs,domain))
            self.assertNotEqual(v.fingerprint(before,domain),v.fingerprint(code,domain))

    def test_test_inputs_invalidate_their_suite_without_invalidating_player(self):
        before={'Assets/Topaz/Tests/Editor/RulesTests.cs':'one'}
        after={'Assets/Topaz/Tests/Editor/RulesTests.cs':'two'}
        self.assertNotEqual(v.fingerprint(before,'editmode'),v.fingerprint(after,'editmode'))
        self.assertEqual(v.fingerprint(before,'playmode'),v.fingerprint(after,'playmode'))
        self.assertEqual(v.fingerprint(before,'build'),v.fingerprint(after,'build'))
        for domain in ('editmode','playmode','build'):
            self.assertNotEqual(v.fingerprint({'Packages/manifest.json':'a'},domain),v.fingerprint({'Packages/manifest.json':'b'},domain))

    def test_content_baseline_preserves_dirty_start_and_detects_edits_additions_deletions(self):
        self.assertEqual(v.delta({'dirty.cs':'original','delete.cs':'a'}, {'dirty.cs':'edited','new.cs':'b'}),
                         ['delete.cs','dirty.cs','new.cs'])
        self.assertEqual(v.delta({'already-dirty.cs':'same'},{'already-dirty.cs':'same'}),[])

    def test_two_modes_are_batched_and_category_does_not_lose_filter(self):
        command=v.test_command(Path('/repo'),'PlayMode',['A','B'],'!Stress',Path('/report.xml'))
        self.assertEqual(command.count('unity'),1)
        self.assertEqual(command[command.index('--filter')+1],'A;B')
        self.assertEqual(command[-3:],['--','-testCategory','!Stress'])
        full=v.test_command(Path('/repo'),'EditMode',[None],None,Path('/report.xml'))
        self.assertNotIn('--filter',full);self.assertNotIn('-testCategory',full)

    def test_docs_and_tooling_select_no_unity(self):
        areas={'ui':{'paths':['Assets/Topaz/UI/'],'tests':{'PlayMode':['MenuTests']},'docs':['docs/UI.md'],'scenes':[]}}
        self.assertEqual(v.selected_tests(['docs/UI.md','scripts/tool.py'],areas)[0],{})
        self.assertEqual(v.selected_tests(['Assets/Topaz/UI/Menu.cs'],areas)[0],{'PlayMode':['MenuTests']})
        self.assertEqual(v.selected_tests(['Assets/Topaz/Tests/Editor/ProfilePersistenceTests.cs'],areas)[0],{'EditMode':['ProfilePersistenceTests']})
        self.assertEqual(v.selected_tests(['Assets/Topaz/Tests/PlayMode/TopazInputTestFixture.cs'],areas)[0],{'EditMode':[None],'PlayMode':[None]})

    def test_shared_or_unmapped_source_broadens_instead_of_false_green(self):
        for name in ('Packages/manifest.json','ProjectSettings/QualitySettings.asset','Assets/Topaz/World/Scenes/Bootstrap.unity','Assets/Unknown.cs'):
            tests,_,unknown=v.selected_tests([name],{})
            self.assertEqual(set(tests),{'EditMode','PlayMode'});self.assertEqual(unknown,[name])

    def test_windows_risk_has_no_docs_or_python_false_positive(self):
        paths=['docs/ROADMAP.md','scripts/topaz-tools.py','Assets/Topaz/UI/Menu.cs','Assets/Plugins/plugin.dll','Assets/Topaz/Shader.shader','ProjectSettings/ProjectSettings.asset']
        self.assertEqual(v.windows_reasons(paths),sorted(paths[3:]))


class ExecutionTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup)
        self.root=Path(self.temp.name)
        (self.root/'scripts').mkdir();(self.root/'docs').mkdir();(self.root/'Assets').mkdir()
        (self.root/'scripts/tool.py').write_text('original')
        (self.root/'docs/current.md').write_text('original')
        (self.root/'Assets/game.cs').write_text('original')
        self.patches=[patch.object(tools,'ROOT',self.root),patch.object(tools,'RUNS',self.root/'TestResults/runs'),
                      patch.object(tools,'project_paths',side_effect=lambda:[str(p.relative_to(self.root)) for base in ('scripts','docs','Assets') for p in (self.root/base).rglob('*') if p.is_file()]),
                      patch.object(tools,'load_areas',return_value={}),patch.object(tools.housekeeping,'hygiene',return_value=0),
                      patch.object(tools.shutil,'which',return_value='/bin/unity'),
                      patch.object(tools.subprocess,'run',return_value=argparse.Namespace(returncode=0,stdout='',stderr=''))]
        for p in self.patches:p.start();self.addCleanup(p.stop)
        self.output=io.StringIO();self.redirect=redirect_stdout(self.output);self.redirect.__enter__();self.addCleanup(self.redirect.__exit__,None,None,None)

    def args(self,**kw):
        d=dict(full=False,stress=False,quick=False,path=[],mode=None,filter=None,area=[],changed=False,dry_run=False)
        d.update(kw);return argparse.Namespace(**d)

    def run_stage(self,command,label,directory,report=None):
        (directory/(label+'.log')).write_text('Ran 5 tests\nOK\n')
        if report: report.write_text('<testsuites><testsuite><testcase classname="Tests.Rules" name="works" time="1"/></testsuite></testsuites>')
        return True

    def receipt(self):
        return json.loads(next(tools.RUNS.glob('*/result.json')).read_text())

    def test_static_checks_do_not_require_unity_or_build(self):
        with patch.object(tools,'run',side_effect=self.run_stage) as run,patch.object(tools.shutil,'which',return_value=None),patch.object(tools,'build_platform') as build:
            self.assertEqual(tools.verify(self.args(path=['scripts/tool.py'])),0)
            self.assertEqual(run.call_count,1);build.assert_not_called()
        self.assertEqual(self.receipt()['selection'],{})

    def test_docs_changed_during_unity_refresh_only_docs_and_preserve_runtime(self):
        def run(*args):
            if args[1]=='editmode-batch':(self.root/'docs/current.md').write_text('updated')
            return self.run_stage(*args)
        with patch.object(tools,'run',side_effect=run),patch.object(tools,'build_platform') as build:
            self.assertEqual(tools.verify(self.args(mode='EditMode')),0)
            build.assert_not_called()
        receipt=self.receipt()
        self.assertTrue(receipt['stages']['editmode-batch']['reusable'])
        self.assertIn('refreshing documentation checks only',self.output.getvalue())

    def test_runtime_changed_during_test_cannot_be_recorded_as_pass(self):
        def run(*args):
            if args[1]=='editmode-batch':(self.root/'Assets/game.cs').write_text('updated')
            return self.run_stage(*args)
        with patch.object(tools,'run',side_effect=run):
            self.assertEqual(tools.verify(self.args(mode='EditMode')),1)
        self.assertFalse(self.receipt()['passed'])
        self.assertFalse(self.receipt()['stages']['editmode-batch']['reusable'])

    def test_failure_is_preserved_and_full_is_not_an_implicit_player_build(self):
        with patch.object(tools,'run',side_effect=self.run_stage) as run,patch.object(tools,'build_platform') as build:
            self.assertEqual(tools.verify(self.args(full=True)),0)
            build.assert_not_called()
            commands=[c.args[0] for c in run.call_args_list if c.args[0][0]=='unity']
            self.assertEqual(len(commands),2)
            self.assertTrue(all('-testCategory' not in cmd for cmd in commands))
        self.assertEqual(self.receipt()['kind'],'full')

    def test_stress_is_explicit_and_zero_reports_still_fail(self):
        with patch.object(tools,'run',side_effect=self.run_stage) as run:
            self.assertEqual(tools.verify(self.args(stress=True)),0)
        commands=[c.args[0] for c in run.call_args_list if c.args[0][0]=='unity']
        self.assertTrue(all(cmd[-1]=='Stress' for cmd in commands))
        empty=self.root/'empty.xml';empty.write_text('<testsuites/>')
        self.assertFalse(tools.summarize_xml(empty))

    def test_dry_run_does_not_start_unity_or_cleanup(self):
        with patch.object(tools.sys,'argv',['topaz-tools.py','verify','--dry-run','--path','scripts/tool.py']),patch.object(tools,'run') as run,patch.object(tools.housekeeping,'cleanup') as cleanup:
            self.assertEqual(tools.main(),0)
            run.assert_not_called();cleanup.assert_not_called()

    def test_unity_operations_cannot_compete_in_one_checkout(self):
        with v.unity_operation(self.root):
            with self.assertRaises(RuntimeError):
                with v.unity_operation(self.root):pass

    def test_test_meta_selects_the_same_fixture_without_broadening(self):
        expected={'EditMode':['RulesTests']}
        self.assertEqual(v.selected_tests(['Assets/Topaz/Tests/Editor/RulesTests.cs.meta'],{})[0],expected)

    def test_moved_fixture_only_selects_its_new_assembly(self):
        old='Assets/Topaz/Tests/PlayMode/ProfilePersistenceTests.cs'
        new='Assets/Topaz/Tests/Editor/ProfilePersistenceTests.cs'
        self.assertEqual(v.selected_tests([old,old+'.meta',new,new+'.meta'],{},existing={new})[0],{'EditMode':['ProfilePersistenceTests']})

    def test_native_code_inside_regular_source_is_flagged_for_windows(self):
        p=self.root/'Assets/game.cs';p.write_text('[DllImport("native")]')
        self.assertEqual(v.windows_reasons(['Assets/game.cs'],self.root),['Assets/game.cs'])

    def test_renderer_provenance_is_not_a_windows_build_trigger(self):
        self.assertEqual(v.windows_reasons(['Assets/Topaz/Presentation/Rendering/SOURCE.md','Assets/Topaz/Presentation/Rendering/SOURCE.md.meta'],self.root),[])

    def test_untracked_content_change_invalidates_runtime_identity(self):
        path=self.root/'Assets/new.cs';path.write_text('first')
        first=v.fingerprint(v.snapshot(self.root,['Assets/new.cs']),'playmode')
        path.write_text('second')
        self.assertNotEqual(first,v.fingerprint(v.snapshot(self.root,['Assets/new.cs']),'playmode'))

    def test_all_skipped_is_not_a_successful_fast_check(self):
        report=self.root/'skip.xml';report.write_text('<testsuites><testsuite><testcase name="disabled"><skipped/></testcase></testsuite></testsuites>')
        self.assertFalse(tools.summarize_xml(report))
