# Unity feature policy

Use Unity's maintained first-party features before adding custom infrastructure. Installed baseline: Unity 6000.6.2f1; URP/Shader Graph/VFX Graph 17.6; Cinemachine 6.6; AI Navigation 2.0.14; Input System 1.20; uGUI/TMP.

Unity Terrain renders generated ground; Cinemachine owns camera follow/collision; NavMeshSurface/Obstacle provide navigation and camp exclusion; Input Actions support keyboard/mouse/gamepad. URP owns lighting and rendering. No HDRP dependency remains. Visual Scripting and Unity Version Control integrations were unused and removed; retain Git/LFS and existing CLI tooling.

Custom code is limited to deterministic layout, stable identity/persistence, region transitions, terrain-aware placement, camp protection, and game-specific inventory/combat/progression rules. Unity does not supply those product rules. Keep conventional GameObjects first; consider Jobs/Burst/ECS or custom rendering only for measured bottlenecks.

RenderingLab retains fixed-scene lighting experiments. Its baked lighting is not a runtime solution for procedural worlds. Maintain private art restore manifests and asset licenses. On package upgrades, revalidate graph authoring adapters and URP tests.

References: [AssetDatabase moves](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetDatabase.MoveAsset.html), [AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html), [reference guide](UNITY_REFERENCE_GUIDE.md).
