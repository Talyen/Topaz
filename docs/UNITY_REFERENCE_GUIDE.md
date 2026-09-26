# Unity reference guide and task map

Baseline: Unity 6000.6.2f1; URP/Shader Graph/VFX Graph 17.6.0; Cinemachine 6.6.0; Input System 1.20.0; AI Navigation 2.0.14. Keep these versions unless an upgrade is explicitly needed.

Use [BASELINE.md](BASELINE.md), [architecture](ARCHITECTURE.md), and [project structure](PROJECT_STRUCTURE.md) before editing. `scripts/agent-areas.json` maps current paths to tests.

| Task | Source domain | Focused checks |
|---|---|---|
| Movement/camera | Player, Core/Input | MovementInputTests |
| Combat/equipment | Gameplay/Combat, Gameplay/Progression, Characters | CombatStudyTests, EquipmentTests, ProgressionPlayTests |
| Inventory/gathering | Gameplay/Inventory, Gameplay/Gathering | WorldLoopTests, MiningAndHomeTests |
| Construction/camps | Gameplay/Building, World/Regions | RegionalBaselineTests, HomeBuildingPlayTests, CampfireTravelTests, BaselineRulesTests |
| Persistence | Gameplay/Persistence | ProfilePersistenceTests, BaselineRulesTests |
| Procedural terrain/travel | World/Generation, World/Regions | WoodlandGenerationTests, ExpeditionTests |
| UI | UI | MenuTests, HomeBuildingPlayTests |
| Rendering/audio | Presentation | VisualStudyTests, WeatherTests, PlayerLanternTests, AudioOptionsTests |

## Versioned first-party references

- [Unity 6.6 manual](https://docs.unity3d.com/6000.6/Documentation/Manual/index.html)
- [URP 17.6](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.6/manual/index.html)
- [AI Navigation 2.0](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html)
- [Input System 1.20](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/index.html)
- [uGUI 2.6](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/index.html)
- [AssetDatabase.MoveAsset](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetDatabase.MoveAsset.html)
- [SceneManager.LoadSceneAsync](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html)

Read the installed PackageCache documentation/source when precise package behavior matters. Use the connected Editor for asset changes, Package Manager for dependencies, and the existing scripts for checks/builds.
