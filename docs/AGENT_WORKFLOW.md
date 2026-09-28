# Agent workflow for Topaz

Keep routine tool output small and retain complete evidence under ignored `TestResults/`.

## Start and find the right slice

Read the baseline, roadmap and architecture, then only the references needed for the player outcome. Inspect Git status and relevant diffs; build on outstanding changes without overwriting unrelated work. `context <area>` supplies a bounded task map. Use `doctor` when environment or retained evidence needs diagnosis, not as a mandatory preflight every iteration.

## Iterate and hand off

Snapshot once before editing; no baseline test run is required:

```sh
export TOPAZ_AGENT_TASK=short-task-name
python3 scripts/topaz-tools.py begin
```

The snapshot includes pre-existing dirty work, new files and deletions. Keep it throughout the task; do not reset it to hide changes. Without it, selection falls back to all Git changes and says so. Use explicit paths or filters when scope would otherwise include unrelated work.

### Everyday improvement loop

Briefly inspect the affected experience, name its largest deficiencies, implement a coherent batch, inspect/play, and refine obvious weaknesses. Spend most effort on the game; roughly 75–80% implementation/refinement is a guideline, not a tracking requirement. Work across related systems when needed for the player outcome. Ordinary improvements within the chosen direction do not need another owner approval.

Keep the connected Editor open. Target under a minute for material/layout/tuning feedback and 1–3 minutes for ordinary code-to-play feedback. Investigate a routine loop exceeding five minutes before repeating it. Batch source edits before compilation, keep import/build caches, avoid platform switching, and do not rerun restoration or setup methods during ordinary authoring. Tune existing serialized settings/materials/prefabs; add only controls needed by current work. Measure a representative compile before considering assembly separation.

```sh
python3 scripts/topaz-tools.py iterate
python3 scripts/topaz-tools.py iterate --refresh
python3 scripts/topaz-tools.py iterate --dry-run --path Assets/Topaz/World/Scenes/Bootstrap.unity
```

`iterate` reads the installed Pipeline's Editor and compilation status with bounded calls. It does not force compilation by default. `--refresh` explicitly imports changed assets/scripts in a stopped Editor; it preserves an active Play session. Compilation/reload in progress returns pending rather than polling indefinitely. Recheck after settling. Status alone does not prove changed files were imported or gameplay/visuals are correct. Exit codes are 0 for clear reported compilation status or passing selected tests, 1 for failure, and 2 for blocked/pending/unavailable status. Iteration receipts are observations, never reusable full-gate passes.

Shared/unmapped paths produce a scope warning, never automatic suites. No build, capture, benchmark or second Editor is launched as a fallback. If Pipeline is unreachable, report it; an open Editor may still own the checkout. Diagnose connectivity only when needed for current work, without closing someone else's session.

For behavior changes, select existing tests by the actual risk. Connected tests can use the installed Pipeline `run_tests`/`test_status` capabilities after inspecting their schema. When the Editor is closed, this explicit batch option runs one named selection with a three-minute test timeout: A completed result with zero tests is not a pass. In the pinned test framework, repeated PlayMode runs with domain reload disabled can retain an emptied `PlayerTestAssemblyProvider.m_LoadedAssemblies` cache. Confirm discovery versus execution counts; refresh the scripting domain or reset that temporary cache through the connected Editor before retrying. Do not edit the package or accept an empty suite.

```sh
python3 scripts/topaz-tools.py iterate --batch --mode PlayMode --filter MovementInputTests
```

Both mode and a non-wildcard fixture/test filter are required. Batch iteration excludes Stress, retains test reports and input-change detection, and refuses a checkout with an Editor lock. It does not run tooling/hygiene/full suites as side work. Check the printed selection. Use existing `metrics --task "$TOPAZ_AGENT_TASK"` and run receipts to identify slow stages; no new timing dashboard is needed.

### Finished batches and concrete risks

| Change | Default validation |
| --- | --- |
| Documentation | Consistency, links, hygiene and diff check |
| Visual/layout/audio tuning | Relevant Editor inspection and one representative playable review of the completed batch |
| Gameplay behavior | Compilation, relevant existing tests and the affected interaction |
| Save integrity, transactions or generation correctness | Focused regression for the changed invariant |
| Shader/platform/build integration | Relevant standalone checks and required platform build once after the batch |
| Release or substantial cross-system integration | Explicitly scoped broad regression and compatibility review |

Add tests for behavior that can meaningfully regress, not subjective colors, spacing or tuning. Pure rules/data/file-IO checks belong in EditMode; UI/options fixtures should not generate unused wilderness. Keep independent integration worlds and meaningful assertions. Do not weaken tests for timing targets.

Default to one final Mac review build per meaningful batch when player judgment or build-specific behavior needs it. Run required Windows checks at a relevant platform/native/shader/build integration checkpoint, not every visual tweak. Preserve caches and use working build outputs. Full display/input matrices are for shared navigation/scaling changes or release review. Benchmarks require a reproduced hitch, plausible performance risk or explicit request; [performance](PERFORMANCE.md) supplies that optional process. Stress checks apply to relevant generation/traversal correctness changes, not ordinary decoration.

### Integration tools and evidence

`verify` retains conservative selection: Python tests, hygiene and affected non-stress Unity tests. Shared scenes/packages/settings or unmapped Unity inputs select both suites. This is an integration path, not the everyday edit loop. Inspect `--dry-run`; use `--mode`/`--filter`, `--area` or repeatable `--path` to describe justified scope. `--quick` is only a compatibility alias, not the new iteration command.

```sh
./scripts/verify.sh --mode PlayMode --filter MovementInputTests
./scripts/verify.sh --full
./scripts/verify.sh --stress
./scripts/build.sh mac
./scripts/build.sh windows
```

Full regression is for substantial integration/release checkpoints or explicit requests. Full includes stress; stress alone selects exhaustive cases. Neither builds a player. Tests for a mode are batched in one invocation. Keep the existing operation lock and do not launch competing Editors. Zero/all-skipped tests fail. A blocked optional check does not prevent independent improvement work.

Before expensive work, identify the uncertainty it resolves. Reuse retained evidence only when relevant inputs, selection and environment match; do not call historical evidence a fresh pass. Existing integration fingerprints conservatively include runtime inputs; a change invalidates a claim of current coverage, not a mandate to rerun every gate. Markdown does not invalidate runtime tests/builds, and tests do not invalidate player-build identity. Never hide relevant runtime changes or failures. Keep source stable during selected checks.

Use `triage latest` for bounded failures and `metrics --task "$TOPAZ_AGENT_TASK"` for actual durations. `observe` can retain noisy command output. Handoff describes the player improvement, remaining weaknesses, actual checks and limits. Tests, imported assets and attractive isolated captures do not establish owner approval or aesthetic quality. Documentation/Python-only work needs hygiene, relevant Python tests and `git diff --check`, not Unity.

## Connected Editor diagnostics

Configure locally with `unity mcp configure codex --local --project-path "$PWD" --yes`; the absolute-path connection config remains ignored.


When the Editor is connected, `topaz_scene_inventory` returns a bounded list of object paths, components, missing scripts, and stable Editor IDs from the active/open scene or a prefab. Filter by path and set `max` for a narrow answer. `topaz_asset_references` finds direct users of one asset in a selected folder; `topaz_missing_references` inspects an open scene or prefab for missing scripts and object references. These commands do not open or save scenes. `topaz_console_summary` groups warnings and errors, returning a `nextCursor`; pass that as `since` on the next call to see only new messages. Full Editor logs remain available when needed. Use `unity command --caller plugin --skill unity-cli --project-path "$PWD" COMMAND` for these commands, after checking their available arguments with `unity command --query topaz_`.

New diagnostics should identify the Topaz subsystem and the affected object or stable ID, for example `[Topaz/WorldLoop] Save failed for world <id>`. Avoid per-frame success logging. Use the console summary to spot repeated messages before opening full logs.

Capture only when it communicates a result or diagnoses a specific issue; default to one representative image or short clip from the completed batch. Keep captures outside Git; a visual should show the relevant state and display settings. Do not infer Windows frame targets from the Mac screenshot or Editor Play mode.

## Current documentation

Edit the owning reference instead of appending a milestone report: baseline owns product rules, architecture owns state/system boundaries, roadmap owns current status and unresolved acceptance, and subsystem references own operational instructions. Remove resolved work and replace obsolete statements at handoff. Summarize results in chat; store detailed evidence locally. Do not create dated handoffs, history directories or completion ledgers.

`python3 scripts/topaz-tools.py hygiene` checks registered documentation, local Markdown links/anchors, task-map paths and accidentally versioned generated output without starting Unity. Run it for documentation changes; verification and pull-request CI also run it. New durable documents require a distinct purpose in `scripts/repository-policy.json`. Source/license notes remain beside assets. These structural checks do not determine whether prose or code is obsolete: agents must review changed guidance and demonstrate actual disuse before removing code/assets.

## Artifact retention

Put temporary reports, experiments, concepts and captures in `TestResults/artifacts/<task>/`, and complete review snapshots in `Builds/reviews/<name>/`. Pass an absolute capture directory to smoke diagnostics. Finish all writing before registering an artifact; registered artifacts are immutable except retention metadata. To revise one, create a new directory. Use the normal build outputs as working outputs, not a growing collection of manually dated copies.

```sh
python3 scripts/topaz-tools.py artifact register TestResults/artifacts/example --category diagnostic --run RUN_ID --pin 'Unresolved rendering issue'
python3 scripts/topaz-tools.py artifact register Builds/reviews/example --category review --run RUN_ID --pin 'Current owner review'
python3 scripts/topaz-tools.py artifact unpin Builds/reviews/previous
python3 scripts/topaz-tools.py artifact pin TestResults/artifacts/example --reason 'Still needed for diagnosis'
python3 scripts/topaz-tools.py cleanup
python3 scripts/topaz-tools.py cleanup --apply
```

Replace example paths and `RUN_ID` with existing completed artifacts/runs; repeat `--run` for each related receipt. Pin the current owner-review build and unresolved diagnostic evidence. Only unpin the previous review after explicitly designating its replacement. Existing legacy folders can be registered in place without breaking links. Do not register source archives, backups, active build outputs, or directories containing unrelated data.

Use one of the stable artifact categories `review`, `diagnostic`, `concept` or `report`; do not create a new retention category for each task.

Retention keeps outputs younger than 14 days **or** among the newest 10 in their category, plus pins, the latest successful full gate and all runs referenced by retained artifacts. Latest results of every verification kind remain available. Verification/build commands automatically prune eligible managed output after completion; failures to clean up never change test/build status. Tool writers share an output lock; cleanup needs exclusive access and skips open/changed files, malformed metadata and incomplete runs. Unknown output is reported rather than deleted.

`cleanup` previews exact paths, bytes and reasons; `--apply` deletes. `doctor` reports pinned bytes and unmanaged output totals so forgotten pins and ad-hoc output remain visible. Observation logs and their ledger entries expire together. Normal Editor logs, Unity caches, saves, source archives and external libraries are never retention targets. Keep licenses/provenance with source media and promote only approved production assets from concepts.
