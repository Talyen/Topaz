# Project organization

Keep definitions, runtime code, prefabs and Editor tools near their owning domain. Preserve `.meta` GUIDs during moves using AssetDatabase.

- `Assets/Topaz/Core`: input, diagnostics, validation and build support.
- `Assets/Topaz/Player`, `Characters`: movement/camera and replaceable character presentation.
- `Assets/Topaz/Gameplay`: Building, Combat, Gathering, Inventory, Persistence, Progression, Survival.
- `Assets/Topaz/World`: Generation, Regions, Environment, Scenes.
- `Assets/Topaz/Presentation`: Rendering (settings, environment bindings and look), Audio, Effects.
- `Assets/Topaz/UI`: Journal/HUD, menus, themes, fonts and art.
- `Assets/Topaz/Tests`: Editor rules/asset checks and PlayMode integration.
- `Assets/ThirdParty`: licensed dependencies; private terrain/audio imports remain ignored.

Bootstrap and Woodland are the player scenes. RenderingLab is development-only. Build profiles remain under Build/Profiles. `scripts/agent-areas.json` maps subsystems to tests. `docs/BASELINE.md` defines intent; FEATURE_STATUS records validation boundaries. Original downloads, build output, logs, reports and caches are not repository inputs.
