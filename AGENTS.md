# Working on Topaz

Topaz's owner delegates Unity and code work to agents. Give the owner a concise design choice and a reviewable Mac build or visual when a feature needs judgment; do not require them to edit code or operate the Unity Editor.

## Technical baseline

- Unity 6000.6.2f1; Universal Render Pipeline; Windows desktop first, Mac playtesting, mobile only as a future option.
- Single-player, hand-authored connected regions, fixed-angle orthographic camera, 1–10 visible enemies.
- Use ordinary Unity GameObjects and components first. Add Burst, Jobs, ECS, custom rendering, or Addressables only for a measured bottleneck or clear content-loading need.
- Use the Input System action model for keyboard/mouse and gamepad. Keep simulation, rendering, and camera motion coherent at both 60 Hz and 120 Hz.
- Keep static definitions separate from runtime state. Future persistent objects need stable IDs and versioned saves; unloaded regions pause and resolve bounded changes on return.

## Agent workflow

1. Inspect `git status`, relevant scenes, settings, and current package versions. Preserve unrelated edits.
2. Prefer the connected Editor through `unity` / Unity MCP to modify scenes, prefabs, settings, and assets. Configure locally with `unity mcp configure codex --local --project-path "$PWD" --yes`; its absolute-path config is ignored.
3. Add packages through Unity's Package Manager API or `unity pipeline install`, not by hand-editing `Packages/manifest.json`.
4. Keep every asset with its `.meta` file. Do not commit `Library`, `Temp`, logs, builds, reports, or machine-specific settings.
5. Before importing any free placeholder asset, verify that its terms allow public source redistribution and record source, license, attribution, and files in `docs/THIRD_PARTY_ASSETS.md`. Ordinary Unity Asset Store downloads do not qualify.
6. Run `./scripts/verify.sh` for project changes and `./scripts/build.sh windows` when Windows build compatibility is affected. Inspect console/build errors and `git diff --check`.
7. For gameplay or art changes, capture a representative standalone-player measurement and compare frame times, hitch counts, CPU/GPU cost, and memory with the prior baseline. See `docs/PERFORMANCE.md`.

Do not claim the 1080p/120 target from Editor Play mode or the empty bootstrap scene. Windows performance requires measurements on the designated Windows PC.
