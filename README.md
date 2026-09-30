# Topaz

An early single-player fantasy survival-crafting action RPG for Windows, with Mac playtesting. Explore eight persistent procedural outdoor areas connected by interactive passages and loading transitions; gather, craft, build, and fight using Unity URP. Gameplay uses a fixed isometric-style perspective camera with independent movement and aiming.

## Start here

- [Baseline](docs/BASELINE.md): game rules and deliberate exclusions.
- [Roadmap](docs/ROADMAP.md): implemented features, remaining acceptance, and next work.
- [Architecture](docs/ARCHITECTURE.md): ownership, saves, source layout, and prefab conventions.
- [World reference](docs/PROCEDURAL_WORLDS.md) and [UI reference](docs/UI_DESIGN_SYSTEM.md): current subsystem contracts.
- [Agent instructions](AGENTS.md), [workflow](docs/AGENT_WORKFLOW.md), and [Unity task map](docs/UNITY_REFERENCE_GUIDE.md): how to work and verify.

## Open and develop

Use the pinned Editor and package versions in the [Unity reference guide](docs/UNITY_REFERENCE_GUIDE.md). Run `git lfs install` and `git lfs pull` after cloning. Obtain the owned dependencies listed in the [asset register](docs/THIRD_PARTY_ASSETS.md#restoring-current-private-dependencies), including the eight direct-download Synty packages. Restore the selected files from the local Unity download cache and package archives:

```sh
python3 scripts/restore-synty-starter.py
python3 scripts/restore-storybook-assets.py
python3 scripts/import-synty-downloads.py restore
```

Restore private runtime audio, generated art derivatives and their metadata as described in the same register; the art scripts do not restore sound. Raw vendor art and Sonniss audio are not included in this public repository. The retired Unity Terrain Sample is not an active-world setup prerequisite. Do not rerun historical scene-authoring commands merely to open a clone.

Open the project in Unity, then configure the local connection and inspect readiness:

```sh
unity mcp configure codex --local --project-path "$PWD" --yes
./scripts/doctor.sh
python3 scripts/topaz-tools.py iterate
```

Make meaningful visual/play/feel improvements through the connected Editor. Before editing, set `TOPAZ_AGENT_TASK` and run `python3 scripts/topaz-tools.py begin`. Use `iterate` for bounded status, or `iterate --refresh` to explicitly import changed scripts/assets. Target minute-scale feedback and batch improvements before standalone review.

Tests are explicit during iteration: with the Editor closed, `iterate --batch --mode PlayMode --filter MovementInputTests` runs that non-stress selection. The conservative `./scripts/verify.sh` remains an integration tool; shared/unmapped inputs can select both suites. Early development favors a small, fast suite covering normal player flows: reuse existing tests, default to no new tests, and defer new edge-case coverage unless explicitly requested. Broader existing regression belongs to integration/release checkpoints or concrete unresolved failures; stress/exhaustive runs require an explicit request or a reproduced failure that representative checks cannot resolve. Platform builds belong to relevant integration/release checkpoints. See [workflow](docs/AGENT_WORKFLOW.md#iterate-and-hand-off) for exact behavior and [performance](docs/PERFORMANCE.md) when measurement is warranted.

## Rights and assets

Project-authored software uses [PolyForm Noncommercial](LICENSE.md); original creative content is all rights reserved. Third-party assets retain their own licenses. See [rights](RIGHTS.md), [asset sourcing](docs/ASSET_SOURCING.md), and the [asset register](docs/THIRD_PARTY_ASSETS.md). Preserve original downloads and independent audio review records.
