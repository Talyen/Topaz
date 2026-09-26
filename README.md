# Topaz

An early single-player fantasy survival-crafting action RPG: third-person movement, procedural connected outdoor regions, gathering, crafting, building, and melee/ranged/magic combat using Unity URP.

Start with [the game baseline](docs/BASELINE.md), [architecture](docs/ARCHITECTURE.md), [feature status](docs/FEATURE_STATUS.md), and [agent workflow](AGENTS.md).

## Open and verify

Use Unity **6000.6.2f1**. Run `git lfs install` and restore the private terrain sample following [procedural worlds](docs/PROCEDURAL_WORLDS.md). The raw sample art and source audio archives are local-only dependencies; they are not committed to this public repository.

```sh
unity mcp configure codex --local --project-path "$PWD" --yes
./scripts/doctor.sh
./scripts/verify.sh
./scripts/build.sh windows
```

`verify.sh` runs tool checks, EditMode and PlayMode tests, and a Mac build. Focused iteration: `./scripts/verify.sh --quick --mode PlayMode --filter RegionalBaselineTests`. Builds and reports remain ignored. The Windows build is a compatibility check; Windows performance requires the designated Windows PC.

## Rights and assets

Project-authored software uses [PolyForm Noncommercial](LICENSE.md); original creative content is all rights reserved. Third-party assets retain their own licenses. See [rights](RIGHTS.md) and [the asset register](docs/THIRD_PARTY_ASSETS.md). Asset approval galleries and per-asset review decisions are no longer part of development. Preserve the audio library and its independent review records.
