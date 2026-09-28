# Working on Topaz

Topaz's owner delegates Unity and code work to agents. Give the owner a concise design choice and a reviewable Mac build or visual when judgment is needed; do not require them to edit code or operate the Unity Editor. Make meaningful improvements to what the player sees, does and feels. Spend most effort implementing and refining; expand verification only for concrete risk. Prefer elegance over complexity.

## Read and preserve

Start with [the baseline](docs/BASELINE.md), [current status and next work](docs/ROADMAP.md), and [architecture](docs/ARCHITECTURE.md). Use [the task map](docs/UNITY_REFERENCE_GUIDE.md) for pinned versions and relevant source/tests. Scope work around a coherent player outcome, including related art, camera, animation, effects, audio and UI changes. Do not stop for a handoff after each tiny edit.

Inspect `git status` and relevant diffs, scenes, settings and package versions before editing. Outstanding changes are a valid starting point, including work in files needed by the task. Build on them without overwriting or deleting unrelated work. Preserve source archives, restricted dependencies, the sound library and unfinished audio reviews.

## Build first, iterate in minutes

Briefly inspect the affected experience, identify its biggest deficiencies, implement a substantial batch, then play/inspect and correct obvious shortcomings. Aim for roughly 75–80% implementation and refinement, without time-tracking machinery. Tests, imported assets and screenshots do not establish aesthetic quality. Report specific improvements and remaining weaknesses; do not call work polished or production-ready without evidence and owner acceptance.

Use the connected Editor for the edit–see–adjust loop. Target under a minute for material/layout/tuning feedback and 1–3 minutes for ordinary code-to-play feedback. These are targets, not guarantees. If a routine loop exceeds five minutes, identify the slow stage before repeating it. Batch source edits, preserve caches, and avoid routine clean builds, platform switches and setup/reimport commands. Use existing serialized settings for frequent tuning; no new tuning framework or assembly overhaul without a measured need.

Execute ordinary refinements within the established direction autonomously. Seek owner judgment for consequential design choices, with a coherent result to review. The owner should not have to operate Unity. Missing hardware or an optional blocked check does not stop independent improvement work.

## Implementation rules

- Use ordinary GameObjects/components and Unity's maintained first-party systems. Add Jobs/Burst/ECS, custom rendering or Addressables only for a measured bottleneck or clear loading need. Verify installed APIs and English Unity documentation; record custom tradeoffs in [feature policy](docs/UNITY_FEATURE_POLICY.md).
- Retain Input System keyboard/mouse/gamepad actions and coherent simulation, rendering and camera motion at 60/120 Hz. Product rules and numerical tuning belong in the baseline, not a second contract here.
- Keep definitions separate from runtime state, stable persistent IDs and versioned saves. WorldSession remains state authority; unloaded simulation resolves bounded changes on return.
- Prefer the connected Editor through Unity MCP/CLI for scene, prefab, settings and asset changes. Connection and diagnostic commands are in [workflow](docs/AGENT_WORKFLOW.md).
- Add packages through Unity's Package Manager API or `unity pipeline install`, never by hand-editing `Packages/manifest.json`.
- Keep assets paired with `.meta` files and preserve GUIDs. Do not commit Library, Temp, builds, logs, reports or machine-specific settings.
- Follow [asset sourcing](docs/ASSET_SOURCING.md) before imports. Only commit source assets whose exact terms permit public redistribution; record provenance in [the register](docs/THIRD_PARTY_ASSETS.md). Ordinary Asset Store downloads do not qualify. Review coherent results rather than requiring per-asset galleries.

## Verify and hand off

Before editing, set `TOPAZ_AGENT_TASK` to a unique label and run `python3 scripts/topaz-tools.py begin`. This snapshots the checkout; it does not require baseline tests. Use `python3 scripts/topaz-tools.py iterate` for bounded Editor/compilation status and `--refresh` only when changed scripts/assets need importing. [Workflow](docs/AGENT_WORKFLOW.md#iterate-and-hand-off) owns precise command behavior and proportional checks.

Run relevant existing tests for changed behavior; add regressions for meaningful risks such as save loss, transactions and generation correctness, not subjective colors/spacing/tuning. Broad regression belongs at substantial integration or release checkpoints. Stress coverage belongs to relevant generation/traversal correctness changes. Benchmark a reproduced hitch, a plausible performance-sensitive change or an explicit performance task. Before expensive work, state which uncertainty it resolves; do not repeat unchanged checks without new evidence.

Default to one final Mac review build per meaningful batch when needed. Run required Windows compatibility checks once at a relevant platform/native/shader/build integration checkpoint, not after each presentation tweak. Full display/input matrices belong to shared navigation/scaling changes or release review. Capture only what communicates the improvement or diagnoses a problem; default to one representative image or short clip.

Documentation requires consistency/link checks, hygiene and `git diff --check`; Python tooling additionally needs relevant Python tests, not Unity builds. Report actual scope and limits honestly. Reuse evidence only when relevant inputs, selection and environment match; a narrow result is not a full pass. Windows performance claims still require identified Windows hardware. Fix obvious jitter in the affected experience without turning every task into the complete [performance process](docs/PERFORMANCE.md).

## Keep the repository current

Edit the owning reference in place: baseline for product rules, architecture for ownership, roadmap for current status and unresolved work, subsystem references for operational details. Remove resolved roadmap items and replace stale guidance when finishing a task. Do not add routine milestone reports, dated handoffs, completion ledgers, or historical copies; use the chat for handoff and managed ignored artifacts for evidence. Git preserves earlier tracked versions.

New durable documentation needs a distinct ongoing purpose registered in `scripts/repository-policy.json`; otherwise extend an existing reference. Keep licenses, source notes and dependency manifests beside their assets. Concepts and experiments belong in managed review artifacts until promoted to production assets with provenance. Do not infer obsolete code/assets from age: check actual use and ask about ambiguous categories.

Run `python3 scripts/topaz-tools.py hygiene` for documentation changes. Follow [artifact retention](docs/AGENT_WORKFLOW.md#artifact-retention) for temporary output and pin the current owner-review build and unresolved diagnostic evidence.
