# Working on Topaz

Topaz's owner delegates Unity and code work to agents. Give the owner a concise design choice and a reviewable Mac build or visual when a feature needs judgment; do not require them to edit code or operate the Unity Editor.

## Technical baseline

- Unity 6000.6.2f1; Universal Render Pipeline; Windows desktop first, Mac playtesting, mobile only as a future option.
- Single-player, hand-authored connected regions, fixed-angle orthographic camera, 1–10 visible enemies.
- Use ordinary Unity GameObjects and components first. Add Burst, Jobs, ECS, custom rendering, or Addressables only for a measured bottleneck or clear content-loading need.
- Use the Input System action model for keyboard/mouse and gamepad. Keep simulation, rendering, and camera motion coherent at both 60 Hz and 120 Hz.
- First feel prototype: screen-relative WASD and analog gamepad movement, one travel speed, a short directional dodge without stamina, and bounded camera look-ahead toward aim. Tune numerical values in a Mac build.
- When combat arrives: the dodge has brief invulnerability and a cooldown; melee swings in an aimed arc without lock-on. Expeditions return to the homestead by player choice, not a timer.
- Keep static definitions separate from runtime state. Future persistent objects need stable IDs and versioned saves; unloaded regions pause and resolve bounded changes on return.
- Prefer Unity's maintained first-party features and supported official packages for gameplay foundations, content authoring, UI, graphics, effects, audio, and tooling. Check the installed version and English Unity docs before custom work; use small custom code for Topaz-specific rules. Record tradeoffs and upgrade paths in `docs/UNITY_FEATURE_POLICY.md`.

## Agent workflow

1. Inspect `git status`, relevant scenes, settings, and current package versions. Preserve unrelated edits.
   For substantial features, check whether a supported Unity core feature or official package already provides the capability; document why a custom layer is necessary.
   Start with the task map in `docs/UNITY_REFERENCE_GUIDE.md` for versioned Unity 6.6 and installed-package documentation.
2. Prefer the connected Editor through `unity` / Unity MCP to modify scenes, prefabs, settings, and assets. Configure locally with `unity mcp configure codex --local --project-path "$PWD" --yes`; its absolute-path config is ignored.
3. Add packages through Unity's Package Manager API or `unity pipeline install`, not by hand-editing `Packages/manifest.json`.
4. Keep every asset with its `.meta` file. Do not commit `Library`, `Temp`, logs, builds, reports, or machine-specific settings.
5. Before importing any free placeholder asset, verify that its terms allow public source redistribution and record source, license, attribution, and files in `docs/THIRD_PARTY_ASSETS.md`. Ordinary Unity Asset Store downloads do not qualify.
6. Run `./scripts/verify.sh` for project changes and `./scripts/build.sh windows` when Windows build compatibility is affected. Inspect console/build errors and `git diff --check`.
7. During early feel prototypes, play the Mac build on a 60 Hz display and fix obvious jitter or hitches. Formal scenario capture and CPU/GPU/memory comparisons begin when a representative gameplay slice exists. See `docs/PERFORMANCE.md`.

Do not claim the future 1080p/120 target from Editor Play mode or the empty bootstrap scene. Windows performance requires measurements on the designated Windows PC.
