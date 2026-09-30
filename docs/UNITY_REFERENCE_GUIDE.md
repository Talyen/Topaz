# Unity reference guide and task map

Baseline: Unity 6000.6.2f1; URP/Shader Graph/VFX Graph 17.6.0; Cinemachine 6.6.0; Input System 1.20.0; AI Navigation 2.0.14; uGUI 2.6.0; Test Framework 1.8.0; Unity Pipeline 0.7.0-exp.1. `ProjectSettings/ProjectVersion.txt` and the resolved package lock record the installed inputs. Keep these versions unless an upgrade is explicitly needed.

Read [BASELINE.md](BASELINE.md) for product rules and [architecture](ARCHITECTURE.md) for ownership and project organization before editing. `scripts/agent-areas.json` maps current paths to tests.
Use [current world contracts](PROCEDURAL_WORLDS.md), [UI guidance](UI_DESIGN_SYSTEM.md), and [the roadmap](ROADMAP.md) for current work.

The table is a discovery map, not a checklist or a requirement to add tests. Follow the [lean testing policy](AGENT_WORKFLOW.md#lean-testing-during-early-development): reuse a small, fast selection covering the changed normal player flow, default to no new tests, and defer new edge-case coverage unless explicitly requested. Use `python3 scripts/topaz-tools.py iterate` for bounded Editor feedback; choose named tests after a coherent behavior batch. Shared/unmapped paths do not broaden iteration. The conservative `verify` selector still can; inspect its selection at integration checkpoints.

| Task | Source domain | Candidate focused checks |
|---|---|---|
| Movement/camera | Player, Core/Input | IsometricVisibilityTests (EditMode), IsometricActionTests, MovementInputTests |
| Combat/equipment | Gameplay/Combat, Gameplay/Progression, Characters | CombatStudyTests, EquipmentTests, ProgressionPlayTests, IsometricActionTests |
| Inventory/gathering | Gameplay/Inventory, Gameplay/Gathering | WorldLoopTests, MiningAndHomeTests |
| Construction/camps | Gameplay/Building, World/Regions | RegionalBaselineTests, HomeBuildingPlayTests, CampfireTravelTests, BaselineRulesTests, SpatialShelterTests |
| Persistence | Gameplay/Persistence, World/Regions | ProfilePersistenceTests, BaselineRulesTests, BoundedAreaTests (EditMode), BoundedAreaTravelTests |
| Procedural areas/travel | World/Generation, World/Regions | BoundedAreaTests (EditMode), BoundedAreaTravelTests, AlpineWorldTests (EditMode), DestinationPlayTests, OutdoorTraversalTests |
| Retained continuous-world paths | World/Generation, World/Expedition | WildernessGenerationTests, WildernessStreamingTests, WoodlandGenerationTests, ExpeditionTests |
| UI | UI | MenuTests, HomeBuildingPlayTests |
| Rendering/audio | Presentation | EnvironmentRulesTests (EditMode), VisualStudyTests, WeatherTests, PlayerLanternTests, SpatialShelterTests, AudioOptionsTests |

## Versioned first-party references

- [Unity 6.6 manual](https://docs.unity3d.com/6000.6/Documentation/Manual/index.html)
- [URP 17.6](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.6/manual/index.html)
- [AI Navigation 2.0](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html)
- [Input System 1.20](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html)
- [uGUI 2.6](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/index.html)
- [AssetDatabase.MoveAsset](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetDatabase.MoveAsset.html)
- [SceneManager.LoadSceneAsync](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)

Pure persistence and environment rules run in EditMode. Normal generation checks use representative seeds. Retained hundred-seed generation and multi-seed controller traversal carry the `Stress` category and are optional diagnostics under the lean testing policy, not routine generation-change requirements. Title/options fixtures avoid generating unused worlds. See [verification commands](AGENT_WORKFLOW.md#iterate-and-hand-off).

The table includes newer fixtures that are not yet included in `scripts/agent-areas.json` selections. Inspect the fixture's mode/category and explicitly select it when its invariant changed; an automatic area selection does not establish coverage of the whole bounded-area or camera flow.

Read the installed PackageCache documentation/source when precise package behavior matters. Use the connected Editor for asset changes, Package Manager for dependencies, and the existing scripts for checks/builds.

## Projection-texture and visual-overhaul authoring

[AI texture projection](AI_TEXTURE_PROJECTION.md) owns the experimental procedure, evidence limits and next artistic work. Capture/apply entry points live under `scripts/visual-review/`; the fixture/shader/texture owner are under `Assets/Topaz/Core/Editor/VisualLab/`, while shared recipes and the isolated playable review controller are under Diagnostics. Use the connected Editor with private managed output, not production scene/setup commands.
