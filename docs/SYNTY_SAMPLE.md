# Synty Starter review sample

Direction: natural fantasy, smooth matte terrain, warm camps, POLYGON silhouettes.
This is the first playable review checkpoint, not a completed art migration.
No purchases or subscription are required. Menus, fonts, icons, audio and gameplay
rules remain in place. World seeds, generation version, terrain heights, resource
IDs and save schemas are unchanged.

## Acquisition and restore

POLYGON - Starter Pack - Art by Synty, Unity product 156819, version 1.2.1,
downloaded through the owner's Unity Package Manager on 2026-09-26. Its included
PolygonGeneric collection supplies the selected art; no additional pack was acquired.
The package contains 553 prefabs in total; only the selected dependency closure
is imported (44 assets / 88 source files including metadata).

Source: https://assetstore.unity.com/packages/3d/environments/polygon-starter-pack-art-by-synty-156819

The product page specifies the Standard Unity Asset Store EULA (terms dated
2024-12-04): https://unity.com/legal/as-terms . Raw source redistribution is not
permitted by the project policy. Vendor files stay ignored under `Assets/Synty/`.
Original archive and extracted inventory remain local. `scripts/synty-starter-files.json`
records original paths, hashes, archive hash and version. No demo scenes, project
settings or vendor scripts are imported. Topaz wrappers reference vendor assets.

After obtaining that version through My Assets, run:

```sh
python3 scripts/restore-synty-starter.py
```

The restore validates every selected file before writing. Open Unity to import;
`Topaz/Generation/Bind Synty Starter Sample` rebuilds Topaz bindings through the Editor.
The source subset is unmodified. Scales, colliders and equipment anchors are owned
by Topaz wrappers. Package Manager UI is integrated into this Editor; no package
addition was required.

## Replacement inventory

| Current role | Review sample | Remaining work after review |
| --- | --- | --- |
| Terrain grass/path | Owned matte meadow/earth layers on Unity Terrain | Palette refinement only |
| Trees and decorative trees | Generic pine 01/02 | More silhouettes/density tuning if requested |
| Mining/decorative rocks | Generic rock 01 | Mineral-specific markings |
| Grass | Generic grass 01 | Coverage tuning |
| Mushrooms/berry forage | Generic mushroom 01 / bush 01 | Distinct ripe berries |
| Player and menu model previews | Generic male/female peasants | Distinct looks for each existing appearance ID |
| Enemies | Generic skeleton | Visual distinctions for ranged/caster roles |
| Axe, combat axe, pickaxe | Generic axe/pickaxe | Grip/animation polish |
| Sword, staff, crossbow, shield | Owned simple matching shapes | Purpose-built equipment silhouettes |
| Authored and built chest | Generic chest 01 | Lid animation polish |
| Workbench / buildable table | Generic table 01 | More station detail |
| Home fire stones/logs | Generic rock 03 / log 01 with existing fire effects | Built fire presentation refinement |
| Equipment rack | Owned timber stand | Display held equipment |
| Floors/walls/doorways/roofs | Existing owned placeholders | Matching authored modular kit |
| Bed/bedroll/lantern | Existing owned placeholders | Matching authored props |
| Pickups, projectiles, remaining fixtures | Existing owned placeholders | Per-role replacement |
| Rain, spell/hit VFX, sky/fog/day-night | Existing bounded effects with new art | Review readability before retuning |
| UI layout, fonts and icons | Retained intentionally | Outside this pass |
| Sound library and review records | Preserved | Independent work |

## Interfaces and limitations

`WoodlandPreset` remains the environment binding. CharacterVisual and PlayerAppearance
remain the character/preview contract. `PrototypeHumanoidMotion` provides basic
owned humanoid poses and prefab-authored held visuals without root motion or hit events.
This is temporary motion, not a production animation set. Existing appearance IDs
map to male/female stand-ins; no save migration occurs.

`BuildingSettings.visuals` optionally maps a stable building ID to a prefab.
BuildVisuals falls back to existing shapes for unmigrated roles. `BuildingVisualBinding`
provides door and roof anchors for future authored prefabs. The sample table preserves
its previous collider size and position. Authored home replacements retain the existing
interaction roots and colliders.

Shared art bindings affect both existing playable regions so travel remains usable.
The review milestone is one coherent woodland/player/enemy/camp treatment; expansion
of the remaining roles waits for the owner's visual review.

## Review

Use a Mac build to assess movement, gathering, combat, placement and night readability.
Buy further assets only when a missing role or insufficient variety is demonstrated.
Validation results and screenshot/build locations are reported in the task handoff.
