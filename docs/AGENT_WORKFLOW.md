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

Keep the connected Editor open. Target under a minute for material/layout/tuning feedback and 1–3 minutes for ordinary code-to-play feedback. Investigate a routine loop exceeding five minutes before repeating it. Batch source edits before compilation, keep import/build caches, avoid platform switching, and do not rerun restoration or setup methods during ordinary authoring. Tune existing serialized settings/materials/prefabs; add only controls needed by current work. Shared Starter prefab helpers and the outdoor LUT generator remain available to current authoring tools; the obsolete foundation, URP migration, placeholder UI and Starter/Storybook reconstruction commands are retired. Measure a representative compile before considering assembly separation.

```sh
python3 scripts/topaz-tools.py iterate
python3 scripts/topaz-tools.py iterate --refresh
python3 scripts/topaz-tools.py iterate --dry-run --path Assets/Topaz/World/Scenes/Bootstrap.unity
```

`iterate` reads the installed Pipeline's Editor and compilation status with bounded calls. It does not force compilation by default. `--refresh` explicitly imports changed assets/scripts in a stopped Editor; it preserves an active Play session. Compilation/reload in progress returns pending rather than polling indefinitely. Recheck after settling. Status alone does not prove changed files were imported or gameplay/visuals are correct. Exit codes are 0 for clear reported compilation status or passing selected tests, 1 for failure, and 2 for blocked/pending/unavailable status. Iteration receipts are observations, never reusable full-gate passes.

Shared/unmapped paths produce a scope warning, never automatic suites. No build, capture, benchmark or second Editor is launched as a fallback. If Pipeline is unreachable, report it; an open Editor may still own the checkout. Diagnose connectivity only when needed for current work, without closing someone else's session.

For behavior changes, select the smallest existing set covering the changed normal player flow; follow the [lean testing policy](#lean-testing-during-early-development). Run it once after a coherent batch, not after every edit. Connected tests can use the installed Pipeline `run_tests`/`test_status` capabilities after inspecting their schema. A completed result with zero tests is not a pass. In the pinned test framework, repeated PlayMode runs with domain reload disabled can retain an emptied `PlayerTestAssemblyProvider.m_LoadedAssemblies` cache. Confirm discovery versus execution counts; refresh the scripting domain or reset that temporary cache through the connected Editor before retrying. Do not edit the package or accept an empty suite.

When the Editor is closed, the following explicit batch option runs one named selection with a three-minute test timeout:

```sh
python3 scripts/topaz-tools.py iterate --batch --mode PlayMode --filter MovementInputTests
```

Both mode and a non-wildcard fixture/test filter are required. Batch iteration excludes Stress, retains test reports and input-change detection, and refuses a checkout with an Editor lock. It does not run tooling/hygiene/full suites as side work. Check the printed selection. Use existing `metrics --task "$TOPAZ_AGENT_TASK"` and run receipts to identify slow stages; no new timing dashboard is needed.

### Lean testing during early development

Rapid iteration takes priority over exhaustive correctness coverage while Topaz is early in development. Keep a small number of fast tests that each cover substantial normal player behavior or a core system contract. Coverage means useful player flows, not a target percentage of lines or branches.

- Reuse or extend an existing test before creating a new fixture. Default to zero new tests; when coverage is missing, one to three substantial tests will usually cover a coherent batch. This is a guideline, not a quota.
- Prefer representative flows such as gather → craft → use, save → reload, or depart → return, with a few meaningful outcome assertions. Do not create one test per method, field, branch or bug fix, mirror implementation details, or build new test infrastructure for a local change.
- Do not add edge-case, malformed-input, exhaustive seed, boundary-permutation or combinatorial coverage unless the owner explicitly requests it. Save and transaction changes should use representative round-trip/commit checks rather than automatically expanding into failure matrices.
- Keep setup minimal and deterministic. Prefer fast EditMode checks for rules/data/file IO; use PlayMode only for behavior requiring live integration. Avoid unnecessary world generation, long waits and repeated scene loads. Aim for the routine selected checks to finish within a minute; investigate expensive setup before expanding them.
- When changing tests, consolidate overlapping coverage and remove assertions tied to retired behavior where their replacement is clear. Preserve unrelated tests and meaningful assertions; do not delete tests or hide failures merely to make a run faster. Wholesale suite pruning is a separate task.

An observed bug can be reproduced and verified through the affected interaction or an existing test; it does not automatically require a permanent new regression. Run broader existing suites only at substantial integration/release checkpoints or for a concrete unresolved failure. Stress/exhaustive runs require an explicit request or a reproduced failure that representative checks cannot resolve. Report what was checked and leave untested cases explicit.

### Finished batches and concrete risks

| Change | Default validation |
| --- | --- |
| Documentation | Consistency, links, hygiene and diff check |
| Visual/layout/audio tuning | Relevant Editor inspection and one representative playable review of the completed batch |
| Gameplay behavior | Compilation, smallest relevant existing test selection and the affected interaction |
| Save integrity, transactions or generation correctness | Representative existing round-trip, commit or generation-flow checks; add a small test only for substantial missing core coverage |
| Shader/platform/build integration | Relevant standalone checks and required platform build once after the batch |
| Release or substantial cross-system integration | Explicitly scoped broad regression and compatibility review |

Follow the lean testing policy above rather than treating every behavior change as a test-writing task. Subjective colors, spacing and tuning need inspection, not new tests. Keep integration state isolated and assertions meaningful; do not weaken tests for timing targets.

Default to one final Mac review build per meaningful batch when player judgment or build-specific behavior needs it. Run required Windows checks at a relevant platform/native/shader/build integration checkpoint, not every visual tweak. Preserve caches and use working build outputs. Full display/input matrices are for shared navigation/scaling changes or release review. Benchmarks require a reproduced hitch, plausible performance risk or explicit request; [performance](PERFORMANCE.md) supplies that optional process. Generation/traversal changes use representative checks by default; stress checks follow the lean testing policy above.

### Integration tools and evidence

`verify` retains conservative selection: Python tests, hygiene and affected non-stress Unity tests. Shared scenes/packages/settings or unmapped Unity inputs select both suites. This is an integration path, not the everyday edit loop. Inspect `--dry-run`; use `--mode`/`--filter`, `--area` or repeatable `--path` to describe justified scope. `--quick` is only a compatibility alias, not the new iteration command.

```sh
./scripts/verify.sh --mode PlayMode --filter MovementInputTests
./scripts/verify.sh --full
./scripts/verify.sh --stress
./scripts/build.sh mac
./scripts/build.sh windows
```

Broader non-stress regression is for substantial integration/release checkpoints or concrete unresolved failures. `--full` includes stress; `--stress` alone selects exhaustive cases. Use either only for an explicit request or a reproduced failure that representative checks cannot resolve; scope ordinary integration checks with filters instead. Neither builds a player. Tests for a mode are batched in one invocation. Keep the existing operation lock and do not launch competing Editors. Zero/all-skipped tests fail. A blocked optional check does not prevent independent improvement work.

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
