# Continuous Storybook wilderness

The active implementation now uses a 1024 m continuous wilderness, 128 m chunks,
Unity Terrain, URP, and a local asynchronous AI Navigation surface. See
[implementation/evidence ledger](STORYBOOK_IMPLEMENTATION.md), [baseline](BASELINE.md)
and [architecture](ARCHITECTURE.md). The former two-region description below is
historical context while its remaining test/settings adapters are retired; it is
not the current player-facing world contract.

# Previous procedural woodland foundation

Topaz uses Unity 6000.6.2f1, URP 17.6, VFX Graph 17.6, Cinemachine 6.6,
Input System 1.20, and AI Navigation 2.0.14. HDRP was evaluated and removed.
Unity's [2026 render pipeline strategy](https://unity.com/topics/render-pipelines-strategy-for-2026)
focuses new feature development on URP. Surface Cache GI remains a future-version
candidate, not a dependency or a claim about current lighting.

## World contract

Each World record stores a seed, generator version, and a copy of generation
settings. WoodlandPlan is plain C# data generation with independent layout,
resource, and decoration random streams. A home clearing, route, encounter area,
and exits are reserved before terrain is shaped. Changing decoration density does
not move resources or terrain. Terrain is fixed after generation.

Bootstrap owns the home region; Woodland is now the generated woodland expedition
(scene and saved region identifiers are retained to preserve the existing systems).
WorldSession remains the authority for inventories, progress, storage, harvesting,
time, and travel. WoodlandRegion composes Terrain, collisions, decoration and runtime
navigation during loading. Generated resources use stable region-local IDs. Existing
worlds must not be regenerated with a different generator version or settings.
New standalone data is isolated under `Baseline-v1` in persistentDataPath.

The first slice has finite connected regions. Seamless streaming, digging, rivers,
and generated dungeons are deferred. Old authored studies are not the current world
layout contract; their reusable game rules and save tests remain valuable.

## Rendering

The desktop URP asset uses Forward+, Render Graph, HDR, four shadow cascades,
reflection-probe blending/box projection, SSAO, decals, and GPU Resident Drawer.
GPU occlusion is configured as a separate option and starts disabled pending a
representative comparison. Standard Terrain and instanced terrain details remain
separate from MeshRenderer GPU Resident Drawer coverage.

Cinemachine owns third-person follow and collision. Input actions supply mouse and
stick rotation. Graphics offers Balanced/High and native TAA or optional STP.
The small URP scale adapter only reacts to valid GPU timings, clamps scale to
70–100%, and leaves native resolution unchanged. No GPU timing means no automatic
scale adjustment. CPU bottlenecks cannot be fixed by scaling resolution.

The world uses real-time sun/moon lighting, a procedural sky, ambient lighting,
distance fog and day/night/weather-driven material parameters. APV and the Compute
Light Baker are for the fixed RenderingLab, not newly generated terrain. HDRP-only
volumetric cloud/fog, hardware ray tracing and HDRP water are not enabled.

WorldEffects binds VFX Graph rain, campfire embers and weapon-hit bursts to game
state/events. Particle state never controls damage or saves. Graphs have bounded
particle capacity and explicit bounds. Shader Graph templates expose wetness, wind,
emission and dissolve controls; the graph authoring adapter is pinned to Unity 17.6
and should be revalidated when upgrading packages.

## Current Synty review sample

The active preset now uses the owned Synty Starter Pack subset and matte Topaz terrain layers. See [sample inventory and restore instructions](SYNTY_SAMPLE.md). The Unity Terrain Sample below is preserved as a retired source dependency; it is no longer the active woodland binding. Procedural layout and save contracts are unchanged.

## Previous private art dependency

The account-owned Unity URP Terrain Sample (product 213197, downloaded version 1.0.3)
is licensed under the standard Asset Store EULA according to its included
ThirdPartyNotices.txt. Selected source assets stay under ignored
`Assets/ThirdParty/UnityTerrainSample/`. We import no demo scene, scripts, TerrainData,
package manifest or rendering settings. The selected set supplies pines, a rock,
grass/fern detail and ground textures; player and building visuals remain placeholders.

Restore with `python3 scripts/restore-terrain-sample.py [path-to-unitypackage]` after
acquiring the package through Unity Package Manager. The source manifest validates
all selected file hashes before writing. Public wrapper prefabs reference the private
art by GUID. Do not commit the raw package or its textures/models.

## Agent entry points

- `WoodlandPlan.Generate(seed, regionId, settings)` creates and validates data.
- `WoodlandGenerationTools.ValidateBatch` supports `--topaz-seed=N` and `--topaz-count=N`; writes an ignored JSON report under TestResults/Generation.
- Standalone `--topaz-smoke --topaz-seed=42 --topaz-capture-dir=<directory>` uses temporary saves and captures home/expedition plus timing; add `--topaz-smoke-quit` for unattended runs.
- `Topaz/Generation/Bind Unity Terrain Sample` rebuilds the owned art bindings.
- `Topaz/Rendering/Configure Modern URP` configures the rendering baseline.
- `Topaz/Migration/Configure Shader Graph Templates` rebuilds graph templates.
- `./scripts/verify.sh` is the full handoff check; targeted runs use `--quick`.
- `./scripts/build.sh mac` and `./scripts/build.sh windows` produce review builds.

Keep source audio archives, curated imports and all review records intact. Audio
curation may continue independently during this migration. Audio review remains independent of visual asset cleanup.

Windows 60–120 FPS is a target pending named Windows hardware and representative
standalone measurements, not an Editor performance claim.


See [the baseline](BASELINE.md) for regional construction and dynamic camp travel. RenderingLab is excluded from normal player builds. Historical save migration chains and authored dungeon fixtures have been removed.
