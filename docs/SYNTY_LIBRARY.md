# Private Synty asset library

Topaz imports production content from the owner's eight Synty `.unitypackage`
downloads. Original packages and adjacent raw `SourceFiles` downloads stay in
`~/Downloads/`; hash-verified copies of the eight packages are preserved under
ignored `LocalSourceArchives/SyntyDownloads/`. The raw source files are not Unity project inputs. The imported
vendor payloads and `.meta` files stay under ignored `Assets/Synty/`. Do not move
vendor files out of their package paths: their GUIDs and references are shared
across packs. Topaz-owned wrappers live under
`Assets/Topaz/Presentation/Art/World/` and `Art/Characters/` and remain tracked.

The owner supplied these purchased downloads on 2026-09-27. Synty's current
[one-time purchase EULA](https://syntystore.com/pages/one-time-purchase-licence)
is the published license for direct-store pack purchases and prohibits sharing
source files outside the team. A dated HTML snapshot is preserved privately at
`LocalSourceArchives/SyntyDownloads/one-time-purchase-licence-2026-09-27.html`
(SHA-256 `f1ec1a03add9db4ce54e6affc6dcb691c518f8cf6f2d87b17152ffb38e4316b9`).
The precise checkout channel for these eight downloads is not recorded in the
files themselves, so the owner's purchase records remain the entitlement source.

| Download | Unity asset root |
| --- | --- |
| POLYGON Prototype 1.9.2 | `PolygonPrototype` |
| POLYGON Shops 1.6.6 | `PolygonShops` |
| Sidekick Goblin Fighters 1.0.4 | `SidekickCharacters` |
| POLYGON Viking Realm 1.1.1 | `PolygonVikingRealm` |
| Goblin Locomotion 1.0.1 | `AnimationGoblinLocomotion` |
| POLYGON Goblin War Camp 1.1.1 | `PolygonGoblinWarCamp` |
| POLYGON Nature Biomes Alpine Mountain 1.5.5 | `PolygonNatureBiomes`, `PNB_Core` |
| POLYGON Particle FX 1.4.1 | `PolygonParticleFX` |

Each POLYGON package also supplies `PolygonGeneric`. The import preserves any
existing Starter Pack files and their metadata, adds missing Generic files once,
and never overwrites an existing file. The exact archive names, SHA-256 hashes,
selected paths, and file hashes are in `scripts/synty-downloads.json`.
Unity 6.6 rewrote 474 imported material YAML files during its upgrade pass;
the restore script validates the original packages while preserving those
locally upgraded files on repeat runs.

To restore the private library on this machine after checking out the tracked
project, retain the same archives in Downloads and run:

```sh
python3 scripts/restore-synty-starter.py
python3 scripts/restore-storybook-assets.py
python3 scripts/import-synty-downloads.py restore
```

If Downloads has been cleared, pass
`--downloads LocalSourceArchives/SyntyDownloads` to the last command.

The import includes models, textures, materials, prefabs, animation, shaders,
effects, and their production dependencies. It excludes vendor demo scenes,
sample folders, source project files, and optional helper code. The import itself does not place models in the game. `AlpineAssetSetup.Apply` binds the original Viking/Alpine selection; `SyntyDestinationSetup.Apply` and `SyntyEnemySetup.Apply` author additional owned destination and enemy wrappers. The destination setup enables read access on referenced model imports required for runtime mesh-collider navigation; enemy setup does the same for its contributing character models so Surface Cache GI can build. Use Unity's AssetDatabase to move
Topaz-owned wrappers, so their GUIDs remain stable.

Restoration and portrait generation are setup/recovery operations, not routine iteration steps. Reuse imported assets and regenerate only affected derivatives when their source changes; follow the [workflow](AGENT_WORKFLOW.md#iterate-and-hand-off).

## Viking journal portraits

`AlpineReviewCapture.PublishPortraits` renders the ten owned character wrappers and binds the Equipment Journal. Run it with graphics enabled after `AlpineAssetSetup.Apply` when restoring a checkout. The PNGs under `Assets/Topaz/UI/Art/VikingPortraits/` remain ignored private derivatives; their metadata and source note are retained so regeneration preserves GUIDs. The owner does not need to operate the Editor: agents run these authoring methods through Unity CLI.
