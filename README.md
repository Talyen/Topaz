# Topaz

Topaz is an early-stage, single-player action RPG and survival crafting game in a hand-authored world. This repository currently contains the Unity foundation and verification tools, not a playable game.

## Project brief

- Stylized 3D, fixed-angle orthographic view; Windows/Steam is the primary target, with Mac playtesting.
- WASD movement and mouse aiming, with gamepad support planned from the first playable prototype.
- Readable encounters of roughly 1–10 visible enemies, connected authored regions, and authored dungeons that may reset.
- Gentle survival needs, plot-based homestead building, recoverable death, and classless skills that improve through use.
- Responsive travel and deliberate attacks; one safe homestead anchors expeditions. The first playable study focuses on screen-relative movement, a short dodge, aiming, and gentle camera look-ahead.
- Use a 60 Hz Mac display as the near-term smoothness baseline. A designated Windows PC will later test the 1080p/120 FPS gameplay target; a lower-tier 60 FPS target will be selected after a representative scene exists.

## Tools and checks

Use Unity **6000.6.2f1**. The project was created from the Universal 3D template. Unity CLI and `com.unity.pipeline` let agents operate a running Editor; [AGENTS.md](AGENTS.md) describes the workflow.

```sh
git lfs install
unity mcp configure codex --local --project-path "$PWD" --yes
./scripts/verify.sh
./scripts/build.sh windows
```

The `unity mcp configure` command writes a machine-specific `.codex/config.toml` that is ignored by Git. `verify.sh` checks Unity Edit Mode tests and builds a Mac player. The Windows build is a cross-platform smoke check; final Windows performance must be measured on Windows hardware. Generated players, reports, Unity `Library`, and logs are ignored.

An empty bootstrap scene can produce a diagnostic timing report when a player is launched with `--topaz-perf`. That report only checks the capture pipeline; it says nothing about the future game's frame rate. See [performance process](docs/PERFORMANCE.md).

## Rights and assets

Project-authored software source is licensed under [PolyForm Noncommercial 1.0.0](LICENSE.md). Original Topaz creative content is all rights reserved. Third-party content keeps its own license. See [rights](RIGHTS.md) and the [asset register](docs/THIRD_PARTY_ASSETS.md). We are not accepting outside pull requests yet because a premium release requires clear commercial rights to contributions.
