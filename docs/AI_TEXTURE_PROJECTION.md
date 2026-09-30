# AI texture projection and URP visual experiments

This reference owns the experimental texture-authoring workflow, its measured limitations and the criteria for adopting it. It does not replace the [visual/performance plan](plans/Unity_Visual_Performance_CoDevelopment_Plan_v4_2026.md), [asset sourcing rules](ASSET_SOURCING.md) or production rendering contract. The owner has authorized a URP-only visual overhaul; Synty models remain the asset foundation. Existing lighting, terrain treatment, shaders and post-processing are available for comparison and replacement when a coherent playable result justifies it.

## What is working

ImageGen can paint calibrated views of an existing model, and those images can be projected back onto its surfaces and baked into a unique UV atlas. The resulting texture is an ordinary albedo image used by native URP Lit. ImageGen runs during authoring, never during gameplay or procedural generation.

The prototype uses the owned Viking Realm `SM_Chr_Warrior_Male_01` prefab. It exports the three active skinned parts: body, wolf hood and fur collar. Static beard/pouch attachments retain their original materials. The experiment changes cloned mesh UVs and materials; the vendor mesh, hierarchy, avatar and source textures are unchanged.

| Observation | Result and meaning |
| --- | --- |
| Front projection | Covers 27.2% of occupied atlas texels in this fixture. A single attractive front image is insufficient. |
| Six rest-pose views | Front, back, left, right, upper and lower oblique views cover 72.3%. |
| Four views in another pose | Arm/leg changes expose additional surfaces; filling gaps increases coverage to 75.7%. |
| Remaining surfaces | 24.3% retain their original palette colors. Uncovered areas never receive arbitrary grey background paint. |
| Atlas | 2048² pixels; 48.0% occupied by mesh islands. Export contains 8,938 source vertices and 4,268 triangles; unique corner UVs require 12,804 vertices across the cloned parts. |
| Rig preservation | All copied legacy bone weights and bind poses are checked. Across twelve posed previews, cloned vertex positions match the original mesh's corresponding positions within 0.00001 m. |
| Visible improvement | Leather panels, Nordic motifs, cloth trim, fasteners and boot decoration survive the bake and render under URP lighting. |
| Visible weaknesses | Soft seams, patchy fur, some distorted motifs and imperfect generated albedo remain. These are prototype results, not accepted character quality. |

Coverage is texture-space coverage of this particular atlas, not a count of anatomically hidden surfaces or an artistic quality score. The second pose gives a modest improvement; more images alone do not guarantee complete coverage. Some surfaces remain physically enclosed by other character parts.

The deformation previews use Unity's CPU `BakeMesh` snapshots rendered as meshes. They demonstrate that the atlas stays attached to deforming geometry and that skin data survives duplication. They do **not** certify live GPU skinning, retargeted movement/combat clips, cloth motion, temporal reconstruction or gameplay readability. Unity documents that [BakeMesh produces a skinned snapshot with CPU skinning](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SkinnedMeshRenderer.BakeMesh.html).

## Authoring workflow

1. Instantiate a private review copy in a temporary additive Editor scene. Record the active mesh parts and deterministic vertex/triangle order. Capture the model with calibrated orthographic cameras; store camera right/up/forward vectors, position, center and scale alongside exported posed vertex positions.
2. Ask ImageGen to change surface paint only, preserving silhouette, panel layout, pose, proportions and costume. Request diffuse albedo without directional light, shadows or highlights. Use one sheet for material consistency and supply the first painted sheet as a design reference for further poses.
3. Inspect every generated sheet before baking. A prompt is not a registration guarantee: output dimensions, silhouettes and features can move. Normalize the sheet dimensions to the calibrated grid, and reject serious shape or panel drift. The current bake does not automatically repair silhouette alignment or hallucinated anatomy.
4. Unwrap a unique atlas in Blender. Temporarily weld coincident vertices for efficient UV islands, but map the UVs back by original triangle corners. Preserve Unity's original vertex order as the identity used by the application step; do not replace the rig with an OBJ import.
5. Rasterize each calibrated view's depth against the actual model, including static attachments that occlude the surfaces being painted. For an atlas texel, interpolate its surface point, project it into each view, reject back-facing or occluded samples, and blend eligible views using the fourth power of the facing angle. The prototype uses a 0.024 m depth tolerance; this must be reviewed for thin parts and differently sized characters.
6. For another pose, interpolate the **corresponding posed triangle** before projecting. Do not project a raised-arm image onto rest-pose coordinates. The tool rejects changed mesh part order or triangle topology. Extra poses currently fill uncovered texels only, preserving the primary design on already covered surfaces.
7. Retain original palette color where no valid view exists. Dilate island colors into eight pixels of otherwise empty space for filtering. This does not establish safe mip behavior: island gutter size, compression and seam padding still need production review.
8. Build review meshes from original triangle corners. Copy positions, normals, bone indices/weights and bind poses; assign unique UV0 and regenerate tangents. Attach the mesh to a fresh SkinnedMeshRenderer using the original bones/root. This avoids the vertex-stride mismatch observed when replacing an already rendered mesh inside a synchronous Editor capture.
9. Inspect native URP renders from several headings and at actual gameplay size, then review idle, movement, attacks, dodge and equipment combinations. Keep a palette-only control under the same lighting. A large portrait can hide the fact that fine decoration disappears at isometric scale.

The current application supports the inspected meshes' maximum of four influences per vertex and rejects a mesh with more. A broader implementation must preserve variable influence counts through Unity's [GetAllBoneWeights](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.GetAllBoneWeights.html), `GetBonesPerVertex` and `SetBoneWeights` APIs. It must also handle multiple submeshes, blend shapes, static attachments and any additional vertex channels actually consumed by the destination shader. The current prototype is not a universal character importer.

## Tools and repeatable experiments

The connected Editor executes the C# files through `run_script`; they live outside Assets to avoid a compilation/import cycle for capture changes. Pass an absolute **unregistered** directory under `TestResults/artifacts/` as the single JSON-array argument. Stop Play mode before running. All captures use private temporary scenes and restore the prior active scene/pipeline; they do not save scene or prefab changes.

| Entry | Purpose |
| --- | --- |
| [CharacterCapture.Main](../scripts/visual-review/CharacterCapture.cs) | Export rest-pose geometry/calibration and six 512² reference images; writes `source.json`. |
| [CharacterPoseCapture.Main](../scripts/visual-review/CharacterPoseCapture.cs) | Export the additional pose and four calibrated views; writes `posed-source.json`. |
| [unwrap_projection.py](../scripts/visual-review/unwrap_projection.py) | Run inside Blender; writes corner UV correspondence in `atlas-uv.json`. |
| [bake_projection.py](../scripts/visual-review/bake_projection.py) | Bake `painted-six-views.png` and optional `painted-hidden-pose.png`; writes albedo, coverage and numeric report. Requires NumPy/Pillow. |
| [CharacterBakeReview.Main](../scripts/visual-review/CharacterBakeReview.cs) | Apply the atlas to review copies, validate skin correspondence, and capture before/after plus twelve CPU deformation previews. |
| [ReviewLab.Main](../scripts/visual-review/ReviewLab.cs) | Capture twelve URP recipes using the Editor-only fixture and generated terrain sheet. Requires `terrain-material-sheet.png` in the output directory. |

Example preparation/bake commands, after the agent has captured and inspected the references and copied the generated images into the experiment directory:

```sh
/Applications/Blender.app/Contents/MacOS/Blender --background \
  --python scripts/visual-review/unwrap_projection.py -- /absolute/experiment/directory
python3 scripts/visual-review/bake_projection.py /absolute/experiment/directory
python3 -m unittest discover -s scripts/visual-review -p 'test_*.py'
```

Agents, not the owner, operate these tools. Create a new experiment directory when revising a registered artifact; [retention](AGENT_WORKFLOW.md#artifact-retention) makes registered artifacts immutable. Preserve calibration, prompt text, generated originals, UV mapping, coverage and actual Unity renders together. The current private review directories are `TestResults/artifacts/character-projection/` and `TestResults/artifacts/urp-visual-lab-12/`; chat supplies clickable review images. These local derivatives are intentionally absent from Git.

## Terrain, props and the twelve-look comparison

The Editor-only [fixture](../Assets/Topaz/Core/Editor/VisualLab/VisualLabFixture.cs) compares the same faceted terraces, river, path, owned trees, cabin, rocks and warrior. [Recipes](../Assets/Topaz/Core/Diagnostics/VisualLabLook.cs) vary incumbent asset shading, wrapped diffuse, quantized diffuse bands and direction-based illustration shading, alongside lighting, ambient colors, grading, bloom and one bokeh comparison. The “Ink and color” label denotes a dark two-band palette; there is no outline renderer in this batch.

Six recipes additionally use generated moss/path/slate/bank albedo through world-space projection. Ground routes and banks choose materials from landscape relationships; rock uses three-axis projection. This demonstrates a practical alternative for broad surfaces: reusable projected materials instead of baking a unique texture for every procedural terrain seed. Generated tiles are candidates, not verified seamless production materials. Their busy detail and repetition need refinement, and flat river geometry lacks convincing water treatment.

The experiment shader and fixture live under `Assets/Topaz/Core/Editor/VisualLab/`. Shared look recipes and the isolated review controller live under Diagnostics; normal gameplay does not load them. Custom shading has no production Meta, motion-vector or DOTS/GRD contract. The review disables GI explicitly and uses a temporary pipeline clone with GPU Resident Drawer disabled, leaving the production asset unchanged. The twelve frames are **not** a GI comparison, a performance benchmark, twelve independent rendering engines or a complete production-generated journey. Recipes carrying an intended GI flag do not certify GI compatibility. Synty palette property names differ by pack (`_Base_Texture` versus `_Albedo_Map`); dropping the correct property produced white models until the adapter preserved both.

Static lighting/material changes settle quickly through explicit URP render requests. Synchronous requests do not advance ordinary animated frame state; repeated skinned renders initially showed an unchanged pose. CPU pose snapshots were used for the deformation evidence. Do not describe a changing PNG hash as proof of animation; inspect geometry movement.

## Full environment projection comparison

The environment comparison now applies generated paint to **all fourteen unique environment groups** in the fixture: five pine variants, the ruin arch and four cliff variants, the cabin, bridge, terrain and river. Their bakes are reused across 32 placed groups and 35 environment renderers. The prior three-part character atlas is also applied. Every one of the twelve recipes uses this same texture set, so a flat-versus-painted material difference no longer biases the lighting/shading comparison. Overview and closer destination views are captured for each look.

The [environment exporter](../scripts/visual-review/EnvironmentCapture.cs) captures three calibrated views per prop and a top-down view for terrain/water. Native-resolution bilinear paint sampling avoids the block patterns introduced by resizing the generated terrain to the reference/depth-buffer resolution. ImageGen paints five family sheets: trees, stone, structures, terrain and water. The [batch unwrap](../scripts/visual-review/unwrap_environment.py) reuses the corner-correspondence unwrap; terrain and water use calibrated planar coordinates. The [environment baker](../scripts/visual-review/bake_environment.py) rasterizes the whole asset, including all its submeshes, for projection visibility. The [Editor texture owner](../Assets/Topaz/Core/Editor/VisualLab/EnvironmentTextureSet.cs) validates triangle order, preserves local positions/normals, rebuilds unique-UV review meshes and owns their textures/materials. Imported meshes, prefabs and production materials stay unchanged.

Calibrated projection coverage is 36.8–48.1% for the pines, 71.6–89.8% for stone/ruins, 34.8% for the cabin, 47.2% for the bridge, 99.9% for terrain and 99.7% for water. These are occupied-atlas fractions, including bottoms, enclosed geometry and branch undersides that may be invisible during play. Three views are insufficient for complete asset coverage. Gaps receive palette-matched samples from the generated paint, with deterministic small surface variation. This is an approximate generated-material fill, **not** reconstruction of unseen surface motifs. Its fraction is recorded separately in each bake report; it never inflates projection coverage.

The private current comparison is `TestResults/artifacts/environment-projection-samples/`. It contains exact prompts, calibrated references, original generated sheets, fourteen atlases/coverage reports, overview frames, closer frames and application counts. The original comparison and character evidence remain intact. Run `ReviewLab.Textured` through the connected Editor with that absolute directory as its single argument. Pass the installed `SURFACE_CACHE` define to the ephemeral capture compiler so its explicit GI-disable override executes; the main project already supports that native preview. The capture consumes the retained character artifact under `TestResults/artifacts/character-projection/` and fails if its topology no longer matches.

This full comparison establishes a working static texturing experiment. Remaining issues include overly bright water patterns, noisy ground detail, dark roof/canopy values in several looks, approximate hidden-surface fill and seams. The water is still an opaque static review surface; painted ripples do not establish animated flow, physical transparency or reflection quality. The environment derivatives still need LOD correspondence, filtering/compression and runtime memory review before production promotion. Shared GI/GRD and standalone limitations described above continue to apply.

## What to improve before adoption

- Start with broad material values and coherent costume identity. Refine the face, hood and sleeve seams with close calibrated views and targeted repair. Include the static beard/pouch if the whole appearance is promoted.
- Capture paired poses that expose both armpits, hands/palms, inner legs, soles and under-skirt surfaces. Avoid painting surfaces that are permanently enclosed; prioritize surfaces exposed by gameplay animation and equipment changes.
- Requesting albedo does not remove all generated lighting. Inspect for baked shadows/highlights and remove them before dynamic day/night lighting; stylized brush variation should not imply a second sun.
- Generated normal, roughness or metallic images are not automatically physically meaningful. Begin with albedo plus restrained authored material values. Introduce other maps only with deliberate validation.
- Evaluate atlas size, padding, compression, memory and vertex duplication against the actual character count. This approach trades the shared palette's cheap reuse for individual texture memory and additional vertices; a hybrid shared-material/detail-atlas workflow may be better for crowds.
- For procedural places, reuse approved material families and modular asset variants. Preserve deterministic geography and resource identity; do not make an external image service a world-generation dependency.
- Select a few materially different URP directions, then build them into the same playable forest/river journey with convincing ground integration, water, vegetation groups and character action. Sparse composition, empty surfaces and weak silhouettes cannot be fixed by post-processing alone.

A candidate is ready for production integration only after owner visual judgment, seam/material refinement, gameplay animation and camera review, and the relevant Mac/Windows shader/build checks. No runtime performance or Windows compatibility claim has been established by this experiment.

## Provenance and ownership

ImageGen was used through the built-in image tool. Exact prompts and outputs remain beside the review evidence. Reference meshes/textures are from the privately owned Synty Viking Realm and Alpine library. Generated paintovers and baked textures remain private derivatives under the existing [Synty source boundaries](SYNTY_LIBRARY.md); generation does not establish permission to redistribute the underlying asset. Any production promotion must follow [asset sourcing](ASSET_SOURCING.md) and record the exact inputs and rights in [the asset register](THIRD_PARTY_ASSETS.md).

## Playable visual review

The private generated scene is `Assets/Topaz/ReviewContent/VisualReview.unity`. [PreparePlayableReview](../scripts/visual-review/PreparePlayableReview.cs) creates it from the retained bakes and saves its private textures, meshes, per-look materials, volume and pipeline. Open a different clean scene before regenerating it: Unity cannot overwrite a scene asset while another open scene uses that path. Preparation preserves existing generated GUIDs. The content directory is ignored because it includes Synty-derived geometry and textures; the source builder remains versioned.

[VisualLabReviewPlayer](../Assets/Topaz/Core/Diagnostics/VisualLabReviewPlayer.cs) switches all material slots and lighting/post-processing together. It uses the existing PlayerController, PlayerCamera, Input System bindings and PrototypeHumanoidMotion. Terrain/prop collision, a raised bridge deck, water exclusions and area bounds make the fixture traversable. World-specific cutaways and the world aiming marker are disabled in this isolated scene, which contains no WorldSession or campaign save operations. The comparison uses native resolution/FXAA and a separate GI/GRD-disabled pipeline; it is not production rendering acceptance.

Walk with WASD/left stick, aim with cursor/right stick, zoom with wheel/shoulders, dodge with Shift/East and jump with Space/South. Tab, B/View or Escape/Menu opens/closes the look selector; East also closes it. The selector pauses movement/camera input, supports a two-column keyboard/gamepad focus graph and offers all twelve labelled looks. Brackets switch looks without moving the camera; R resets position. The app is built separately under `Builds/reviews/visual-lab/`, preserving the normal Topaz app.

Generated mesh assets explicitly enable Unity 6.6's triangle collision bake before the standalone build. The first build warned that triangle collision was disabled; the reviewed rebuild includes that data. The [Unity Mesh inspector source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/Inspector/ModelInspector.cs) describes this setting for non-convex MeshColliders.
