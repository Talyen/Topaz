# Topaz

Topaz is an early-stage, single-player action RPG and survival crafting game in a hand-authored world. The current Mac build contains [movement and camera](docs/FEEL_STUDY.md), [one-enemy combat](docs/COMBAT_STUDY.md), and a [small gathering and homestead loop](docs/WORLD_LOOP_STUDY.md).

The standalone player opens at a title menu. Press **Esc** to pause or open Options during play; [display and camera controls](docs/MENUS_AND_DISPLAY.md) and the [Visual Lab](docs/VISUAL_STUDY.md) are available there.

## Project brief

- Stylized 3D, fixed-angle orthographic view; Windows/Steam is the primary target, with Mac playtesting.
- WASD movement and mouse aiming, with analog gamepad movement and right-stick aiming in the current study.
- Readable encounters of roughly 1–10 visible enemies, connected authored regions, and authored dungeons that may reset.
- Gentle survival needs, plot-based homestead building, recoverable death, and classless skills that improve through use.
- Responsive travel and deliberate attacks; one safe homestead anchors expeditions. The first playable study focuses on screen-relative movement, a short dodge, aiming, and gentle camera look-ahead.
- Use a 60 Hz Mac display as the near-term smoothness baseline. A designated Windows PC will later test the 1080p/120 FPS gameplay target; a lower-tier 60 FPS target will be selected after a representative scene exists.

## Tools and checks

Use Unity **6000.6.2f1**. The project was created from the Universal 3D template. Unity CLI and `com.unity.pipeline` let agents operate a running Editor; [AGENTS.md](AGENTS.md) describes the workflow.

Topaz's [Unity feature policy](docs/UNITY_FEATURE_POLICY.md) favors built-in and official features, with custom code limited to game-specific rules and replaceable adapters.
Agents can find versioned Unity 6.6 and installed-package sources in the [Unity reference guide](docs/UNITY_REFERENCE_GUIDE.md), with links from each local feature study.

```sh
git lfs install
unity mcp configure codex --local --project-path "$PWD" --yes
./scripts/verify.sh
./scripts/build.sh windows
```

The `unity mcp configure` command writes a machine-specific `.codex/config.toml` that is ignored by Git. `verify.sh` runs Edit Mode and Play Mode tests and builds a Mac player. The Windows build is a cross-platform smoke check; final Windows performance must be measured on Windows hardware. Generated players, reports, Unity `Library`, and logs are ignored.

The Bootstrap player can produce a diagnostic timing report when launched with `--topaz-perf`. That report checks the capture pipeline; it does not establish the future game's frame rate. See [performance process](docs/PERFORMANCE.md).

## Rights and assets

Project-authored software source is licensed under [PolyForm Noncommercial 1.0.0](LICENSE.md). Original Topaz creative content is all rights reserved. Third-party content keeps its own license. See [rights](RIGHTS.md), the [asset register](docs/THIRD_PARTY_ASSETS.md), and the [free 3D asset sourcing guide](docs/ASSET_SOURCING.md). We are not accepting outside pull requests yet because a premium release requires clear commercial rights to contributions.
