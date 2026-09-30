"""Bounded Editor feedback; no implicit tests, builds, or fallback Editor."""
import json
from pathlib import Path
import subprocess
import time


def editor_command(root, name):
    command = ['unity', 'command', '--caller', 'plugin', '--skill', 'unity-cli',
               '--project-path', str(root), '--json', '--no-banner', '--timeout', '15', name]
    result = subprocess.run(command, cwd=root, capture_output=True, text=True, timeout=20)
    try:
        envelope = json.loads(result.stdout)
    except ValueError as error:
        raise RuntimeError('Editor response unavailable: ' + (result.stderr or result.stdout)[:300]) from error
    if not isinstance(envelope, dict):
        raise RuntimeError('Unexpected Editor envelope; no success inferred')
    if result.returncode or not envelope.get('success'):
        raise RuntimeError('Editor command unavailable: ' + json.dumps(envelope.get('errors', []))[:400])
    data = envelope.get('data')
    if isinstance(data, str):
        data = json.loads(data)
    if isinstance(data, dict) and 'result' in data:
        if not data.get('success'):
            raise RuntimeError('Editor command result failed; no success inferred')
        data = data['result']
        if isinstance(data, str):
            data = json.loads(data)
    if not isinstance(data, dict):
        raise RuntimeError('Unexpected Editor response; no success inferred')
    return data


def iterate(args, tools):
    if bool(args.mode) != bool(args.filter) or bool(args.batch) != bool(args.filter):
        raise ValueError('Tests require --batch --mode EditMode|PlayMode --filter <specific fixture or test>')
    if args.filter and (not args.filter.strip() or any(c in args.filter for c in '*?')):
        raise ValueError('Choose a named fixture/test, not a wildcard suite')
    if args.batch and args.refresh:
        raise ValueError('Choose connected-Editor refresh or explicit batch tests')
    paths = args.path or tools.task_changes()
    for path in paths:
        if Path(path).is_absolute() or '..' in Path(path).parts:
            raise ValueError('--path requires repository-relative paths')
    _, _, unknown = tools.validation.selected_tests(paths, tools.load_areas())
    if unknown:
        print('Scope warning: shared/unmapped inputs; no suites selected automatically: ' + ', '.join(unknown[:6]))
    selection = {args.mode: [args.filter]} if args.batch else {}
    print('Iteration scope: ' + (json.dumps(selection) if selection else 'Editor status only' + (' + explicit refresh' if args.refresh else '')))
    if args.dry_run:
        return 0
    started = time.monotonic()
    directory = tools.new_run()
    code, detail = 2, 'Editor unavailable'
    evidence = {}
    try:
        if args.batch:
            # Never launch a competing Editor, including when its server is unreachable.
            if (tools.ROOT / 'Temp/UnityLockfile').exists():
                raise RuntimeError('An Editor owns this checkout; keep it open and use connected tests, or defer batch testing')
            report = directory / 'iteration-tests.xml'
            before = tools.current_fingerprints()[args.mode.lower()]
            command = tools.validation.test_command(tools.ROOT, args.mode, [args.filter], '!Stress', report, timeout=180)
            ok = tools.run(command, 'iteration-tests', directory, report)
            after = tools.current_fingerprints()[args.mode.lower()]
            code = 0 if ok and before == after else 1
            detail = 'Explicit non-stress tests only; not full verification'
            evidence = {'fingerprint': before, 'inputs_unchanged': before == after}
        else:
            status = editor_command(tools.ROOT, 'editor_status')
            if Path(status.get('projectPath', '')).resolve() != tools.ROOT.resolve():
                raise RuntimeError('Editor project mismatch; refusing to inspect or refresh another checkout')
            evidence['editor'] = status
            busy = status.get('compiling') or status.get('domainReloadInProgress') or status.get('status') not in ('ready', 'playing')
            if busy:
                detail = 'Editor busy: ' + str(status.get('status')) + '; no polling or fallback launched'
            elif args.refresh and status.get('playMode') != 'stopped':
                detail = 'Stop Play mode deliberately before requesting a refresh; session preserved'
            else:
                compile_state = editor_command(tools.ROOT, 'recompile' if args.refresh else 'recompile_status')
                evidence['compilation'] = compile_state
                state = compile_state.get('status')
                if compile_state.get('failed') or compile_state.get('compilationFailed') or state == 'failed':
                    code, detail = 1, 'Editor reports compilation errors; inspect Console'
                elif state in ('completed', 'up_to_date'):
                    code, detail = 0, 'Compilation status clear; inspect/play the change. No gameplay or visual validation performed'
                else:
                    detail = 'Compilation status: ' + str(state) + '; not a compile pass. Recheck after settling or explicitly refresh changed scripts'
    except (OSError, ValueError, RuntimeError, subprocess.TimeoutExpired) as error:
        detail = str(error) + '; no build, new Editor, or broad test fallback launched'
    elapsed = time.monotonic() - started
    tools.record(directory, 'iteration', code == 0, detail, started=started,
                 selection=selection, evidence=evidence, reusable=False, exit_code=code)
    print(f'Iteration {"OK" if code == 0 else "FAILED" if code == 1 else "BLOCKED/PENDING"}: {detail} ({elapsed:.1f}s)')
    print('Receipt: ' + str(directory.relative_to(tools.ROOT)))
    return code
