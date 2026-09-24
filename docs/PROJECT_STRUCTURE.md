# Topaz project structure

Topaz uses game domains first and asset types inside each domain. Unity's [project organization guidance](https://unity.com/how-to/organizing-your-project) permits different layouts but emphasizes consistency, separate third-party assets, and preserving `.meta` files when moving assets.

```text
Assets/Topaz/
  Build/Profiles/                 desktop build profiles
  Characters/Animation/          shared character clips and controllers
  Core/Input, Core/Diagnostics   input actions and capture code
  Experimental/Graybox           replaceable feel prototype assets
  Gameplay/Combat, Gameplay/WorldLoop
  Player/Runtime                  movement and camera
  Presentation/Art, Effects, Graphics, Rendering
  UI/HUD, UI/Menus, UI/Art, UI/Fonts, UI/Prefabs, UI/Themes
  World/Environment, World/Expedition, World/Home, World/Scenes
  Tests/Editor, Tests/PlayMode
Assets/ThirdParty/                    imported assets and license records
```

The two enabled scenes live in `Assets/Topaz/World/Scenes`. Renderer assets and URP settings live in `Presentation/Rendering`; build profiles live in `Build/Profiles`. The `Resources/TopazEffectsParticles` load key was preserved while its material moved under `Presentation/Effects/Resources`. Unity-provided TextMesh Pro files remain in their package-style location.

## Migration boundary

The domain migration used Unity's `AssetDatabase.MoveAsset`; file and folder `.meta` GUIDs were preserved except for deliberately removed empty folders. Scene names, stable save IDs, JSON save shape, ScriptableObject types, class names, and namespaces remain unchanged. Existing Editor build commands and tests use the new asset paths. `scripts/build.sh` passes the build-profile asset path explicitly because the Unity CLI's profile-name lookup targets the template profile folder.

When a new feature grows, keep its authored definitions, runtime code, Editor tools, materials, and prefabs near one another. Use `Experimental` only for replaceable comparisons. Any later class or namespace renaming should be a separate change with its own scene and save verification; a file's new location alone does not require a type rename.
