# Unity feature policy

Use Unity's maintained first-party features before adding custom infrastructure. Installed baseline: Unity 6000.6.2f1; URP/Shader Graph/VFX Graph 17.6; Cinemachine 6.6; AI Navigation 2.0.14; Input System 1.20; uGUI/TMP.

Unity Terrain renders generated ground; Cinemachine owns camera follow/collision; NavMeshSurface/Obstacle provide navigation and camp exclusion; Input Actions support keyboard/mouse/gamepad. URP owns lighting and rendering. No HDRP dependency remains. Visual Scripting and Unity Version Control integrations were unused and removed; retain Git/LFS and existing CLI tooling.

Custom code is limited to deterministic layout, stable identity/persistence, region transitions, terrain-aware placement, camp protection, and game-specific inventory/combat/progression rules. Unity does not supply those product rules. Keep conventional GameObjects first; consider Jobs/Burst/ECS or custom rendering only for measured bottlenecks.

RenderingLab retains fixed-scene lighting experiments. Its baked lighting is not a runtime solution for procedural worlds. Maintain private art restore manifests and asset licenses. On package upgrades, revalidate graph authoring adapters and URP tests.

References: [AssetDatabase moves](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetDatabase.MoveAsset.html), [AI Navigation](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/index.html), [reference guide](UNITY_REFERENCE_GUIDE.md).

## Synty Starter sample

Keep Unity Terrain and URP Terrain/Lit for smooth generated ground; no custom terrain renderer is needed. Use Mecanim humanoid avatars with temporary owned procedural poses because Starter Pack includes no clips. Future licensed humanoid clips can replace PrototypeHumanoidMotion without changing gameplay timing. Use prefab visual overrides for building art while retaining placement and collision contracts. See SYNTY_SAMPLE.md.

The standalone Synty sample exposed stripped instancing variants that Editor
rendering did not reveal. Graphics Settings retain BatchRendererGroup variants
(required by the existing GPU Resident Drawer) and instancing variants (Terrain
and details are created at runtime, absent from static scene analysis). Other URP
variant stripping remains enabled. Review standalone visuals after changing this.

## Storybook continuous world (2026-09-26)

Use Unity Terrain neighbor links, instanced mesh details, NavMeshSurface asynchronous
updates, Mecanim humanoid retargeting, Cinemachine, uGUI/TMP and the installed URP
Volume/Shader Graph stack. Topaz-specific code owns deterministic ecology/layout,
chunk scheduling, stable resource IDs and saved-state transactions; first-party
authoring/rendering systems do not supply those game rules. No Burst/Jobs/ECS or
Addressables dependency has been introduced. Measure activation/navigation spikes
before adding parallel generation or another loading framework.

The pinned Shader Graph adapter now sets foliage input positions to Object space
through the installed GeometryNode spacePopup API; feeding World positions into
the Vertex Position output would offset instances incorrectly. Terrain mesh details
use a root MeshFilter/Renderer rather than an empty wrapper. Revalidate these
assumptions when upgrading Unity/URP. Current evidence and unfinished validation are
in STORYBOOK_IMPLEMENTATION.md.
