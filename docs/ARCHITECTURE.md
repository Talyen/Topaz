# Architecture

Use ordinary GameObjects/components, ScriptableObject definitions and plain serializable runtime records. [BASELINE.md](BASELINE.md) owns product rules; [ROADMAP.md](ROADMAP.md) owns current status. Keep static definitions separate from mutable state.

## System ownership

- **WorldSession** coordinates the active Character–World pair, clock/weather, atomic inventory/world transactions, resources, encounters, camp travel/recovery and persistence. Do not introduce another save or simulation authority.
- **WorldGraph** owns recorded area identities/seeds and paired passages. **AreaPlan** adapts recorded settings to an immutable local **WildernessPlan**; optional **AreaDefinition** overrides are applied when a World is created and saved in its area records. **WoodlandRegion** composes **StreamedWilderness**, reusing terrain/art realization with whole-area residency. **AreaRuntime** provides the active area scene lifetime; terrain, collision and navigation are prepared behind loading. WorldSession coordinates publication and rollback; see [world contracts](PROCEDURAL_WORLDS.md).
- **RegionBuildings** realizes committed structures from saved records and owns the local SpatialShelter field. **WorldStorageIndex** queries saved chest stock independently of visual residency. Construction changes invalidate navigation and shelter.
- **CampSafety** owns protection and navigation exclusion; damage/projectiles consult it. Player camps resolve from saved structures even while unloaded. Protection neither awards kills nor discards respawn deadlines.
- **PlayerCamera** owns the fixed world-space Cinemachine follow, bounded zoom, warp resets and player-centered hearing. **PlayerController** owns planar facing, ground targeting and elevated projectile aim. **SceneryCutaway** supplies a bounded gameplay-camera shader mask and local engaged-character silhouettes; it never changes world collision or shelter authority.
- **VisualLookController** derives presentation from simulation state. Unity systems supply rendering, camera following, physics queries, navigation, input, animation and UI; Topaz adapters supply game-specific rules and lens safety. [Unity feature policy](UNITY_FEATURE_POLICY.md) records exceptions and upgrade considerations.

## Saves and state

| Owner | Persistent state |
| --- | --- |
| Character | Inventory, equipment, skills, food/rest effects, lantern state and appearance |
| World | Seed, generator/schema settings, area graph, elapsed time, area-qualified resources, structures/storage, pickups, surviving enemy state and defeated deadlines |
| Character–World Visit | Area and local position, discovered areas/campfires and recovery selection |

The supported collection root is `Application.persistentDataPath/Areas-v2`, using profile schema 4, area graph version 2 and terrain generator version 8. These are separate version fields. ProfileRepository uses validated collection snapshots, atomic replacement, backup recovery and coalesced background writes. Unsupported or corrupt data is reported rather than overwritten with empty progress. Structures store stable IDs, area IDs and local XYZ; unloaded visuals do not remove authoritative state.

No legacy saves require compatibility. Incompatible geography/schema changes use a new supported version and collection rather than retaining old generators or adding migration machinery. Preserve unrelated old collections; reliability within the supported version remains mandatory. Supported worlds retain their recorded generation settings. Player-facing creation/deletion rules live in [UI flow](UI_DESIGN_SYSTEM.md#menus-and-charactersworlds).

## Source organization

Keep definitions, runtime code, prefabs and Editor tools beside their owning domain under `Assets/Topaz/`:

| Domain | Contents |
| --- | --- |
| Core; Build | Input, diagnostics, validation and build support; profiles under Build/Profiles |
| Player; Characters | Movement/camera and replaceable character presentation/animation |
| Gameplay | Building, Combat, Gathering, Inventory, Persistence, Progression, Survival |
| World | Generation, Regions, Environment and Scenes |
| Presentation | Rendering, Audio and Effects |
| UI | Journal/HUD, menus, themes, fonts and art |
| Tests | Editor rules/asset checks and PlayMode integration |

Bootstrap is the enabled player build scene and hosts persistent player/UI/session systems. Runtime-created area scenes own the active landscape; unloaded areas retain only saved state. Woodland retains historical authoring/test uses; it is not a second normal player region. `scripts/agent-areas.json` maps subsystems to source, tests and documentation.

Licensed dependencies retain provenance under their vendor locations; restricted Synty/motion/audio sources remain ignored. See [the asset register](THIRD_PARTY_ASSETS.md). Original downloads, builds, logs, reports and caches are not repository inputs.

Frequently adjusted visual/feel values belong in existing serialized definitions, materials and prefab settings so tuning does not require script recompilation. Add only controls needed by current work. Measure representative compile cost before splitting runtime assemblies; assembly restructuring is not a routine prerequisite.

## Prefab and visual authoring

Gameplay roots own behavior, collision and static configuration; replaceable visual children isolate imported art. Persistent instance IDs belong to generated or placed objects, not reusable prefabs. CharacterVisual exposes body, hands and attachment sockets; gameplay timing remains independent of replacement animation.

Use Prefab Mode or AssetDatabase APIs, preserve `.meta` GUIDs during moves, and update serialized bindings when geometry changes. Keep owned wrappers/materials in their subsystem and vendor sources separate. Check ground anchors, functional clearance and colliders against the current geometry. Check affected asset references after replacements; run relevant gameplay tests when anchors, clearance, collision or behavior change, rather than for every cosmetic adjustment; old reconstruction commands are not normal authoring steps.
