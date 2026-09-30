# Unity 6.6 URP — Isometric Areas: Visual + Performance Co-development Plan

**Version:** 5 — procedural bounded-area revision, September 29, 2026
**Project:** Persistent, procedurally generated Synty fantasy areas, initially alpine, with a fixed-angle perspective isometric camera and explicit loading transitions; Windows shipping target; Mac development/testing; 1440p/60 with a separate 120 FPS ambition.
**Starting point:** The owner rates both performance and visuals about 2/10 and wants about 8/10 on both. These are qualitative assessments, not measured scores. The owner's approximately 8/10 ambition remains on both axes. This revision incorporates procedural-design research and inspection of the current camera, generator, persistence and renderer source. Source/configuration inspection establishes implementation facts; it does not establish visual acceptance or measured gains.

**Authority:** This guides visual/performance execution for the bounded-area direction. [BASELINE](../BASELINE.md) owns product rules, [ARCHITECTURE](../ARCHITECTURE.md) owns system/state ownership, [ROADMAP](../ROADMAP.md) owns capability and acceptance status, and [AGENT_WORKFLOW](../AGENT_WORKFLOW.md#iterate-and-hand-off) owns validation commands and proportional scope. Techniques and budgets here remain hypotheses unless verified in the current build. The filename is retained for existing references; the version above identifies the current revision. Read the start section and selected package; consult other sections only when relevant.

**Evidence convention:** Requirements and artistic intent carry forward from version 3. Linked references support engine behavior. Proposed priorities, settings, budgets, and acceptance rules are project hypotheses—not Unity recommendations, measured gains, or an approved final look. Rendering references were retained from September 28; procedural references and local implementation were reviewed September 29, 2026; verify the installed patch/package when touching a version-sensitive feature. Do not upgrade the engine merely to match a documentation page.

## Authorized URP overhaul exploration

The owner has opened every visual implementation choice to overhaul while retaining Synty assets and URP. Current look, GI, grass and post-processing choices are incumbents, not requirements for every experimental candidate. Begin with 10–20 meaningfully distinct inexpensive Unity samples on matched geometry, then carry a few selected directions into the same playable generated journey. Test different material/shading families, ground treatment and lighting hierarchy instead of multiplying tiny slider variants. Keep the camera session's ownership intact. The [projection-texture workflow](../AI_TEXTURE_PROJECTION.md) records the working character prototype, terrain candidates and their limits. Isolated captures help select a direction; they do not satisfy the playable milestone or establish performance.

## Start here: the bounded assignment

Improve the actual playable generated world, not a settings checklist, a screenshot-only showcase, or an isolated benchmark. Preserve the intended identity, not today's unattractive implementation.

1. **Inspect narrowly.** Read project instructions, baseline, roadmap and architecture; reuse relevant chat/artifact evidence. Reuse working run/capture commands. Once, record the installed Editor/URP versions, active quality/renderer assets, graphics API, and gameplay camera; do not repeatedly audit them. Inspect only the code/assets relevant to one likely improvement.
2. **Get usable evidence.** Obtain the simplest current-game view and basic frame-time/hitch observation using the existing Player or Editor. Editor/Mac evidence is provisional for Windows. Do not build a profiling suite before editing.
3. **Choose one consequential task.** Name one visible weakness and one plausible source of wasted work. Select a coherent task addressing one or both. Favor large forms, lighting, atmosphere, material cohesion, distribution, and redundant-work removal over feature accumulation.
4. **Implement one reversible batch.** Make a coherent set of compatible edits or one substantial feature slice, including related systems needed for the player outcome. A coordinated palette/light/fog treatment can be one artistic hypothesis. During the initial authorized exploration, capture 10–20 materially different inexpensive URP looks with shared geometry and camera. Shortlist a few for playable refinement; subsequent focused batches compare those candidates against the incumbent. Avoid exhaustive parameter grids or simultaneous renderer rewrites.
5. **Inspect the result.** Open matched before/after gameplay images and inspect a short movement sequence. Compare performance using the simplest working method. Compilation is not evidence of visual success; fewer objects or draw calls are not an FPS result.
6. **Keep, isolate, or revert.** Keep a clear improvement, preserve an attractive but unaffordable candidate disabled, or revert a failed experiment. Leave the main path working. Record evidence, rollback point, and one next task.

**Effort allocation:** Spend roughly 75–80% on implementation and refinement, including the edit–see–adjust loop. Inspect narrowly and expand verification for concrete correctness or performance risk. This is guidance, not a timing ledger or a reason to omit required checks.

**Tooling limit:** One attempt plus one focused repair for optional profiler, screenshot, or video tooling; then use a simpler existing path. Normal Game-view capture is sufficient initially. Without image access, do not certify artistic improvement. Without execution, restrict work to one small source-supported patch and report the limitation. Fix or revert compilation/correctness failures caused by the patch.

**Progress rule:** Do not make visuals wait for a perfect performance baseline, or keep adding expensive effects while performance deteriorates. Individual batches may improve one axis; across a short sequence, advance both. After two accepted batches advancing only one axis, make the neglected axis primary unless a specific correctness blocker intervenes.

**First milestone:** A coherent production-generated forest-basin → river-terrace → return journey, with three meaningful beats in each area, readable natural passages, complete loading arrivals and more intentional terrain/vegetation/light composition, without material deterioration in matched play. A minor slider change without an observable benefit does not satisfy the milestone. Prioritize terrain/material composition and character quality alongside lighting and atmosphere; keep all experiments in URP. Retain the performance headroom policy when promoting a candidate.

---

## Procedural bounded-area contract and research basis

- A World is an interconnected network of eight persistent seeded places initially, with a loop, branches, side destinations and paths to Titan's Grave and Split Sky Peak.
- Ordinary areas target three meaningful encounters/discoveries and 4–8 minutes of first-time exploration, excluding prolonged gathering, building and combat. Exits do not automatically count as discoveries.
- Start from a 256 m terrain footprint; selected landmark areas use a 384 m logical footprint. The reused 128 m terrain lattice may require outer support tiles beyond that logical footprint. Measure actual resident tiles/memory rather than claiming logical dimensions establish allocation.
- Every exit requires interaction. Combat retreat is allowed; surviving enemy health/position, defeated deadlines and dropped loot survive area retirement.
- First Hearth remains a small place in the network. Construction remains available on suitable terrain, with permanent exclusions around arrivals, exit corridors and destination footprints.
- One area is fully realized during ordinary play. Loading pauses World time, applies saved state, prepares collision/navigation and resets presentation history before revealing gameplay. No artificial minimum loading delay.
- PlayerCamera and SceneryCutaway own heading, perspective projection, zoom, cutaways and safety offsets. Consume the actual serialized envelope; do not duplicate settings or replace it with an orthographic assumption.

### Connectivity → composition → realization

Create a stable World graph first, then a local area graph of three beats, approaches and loops. Reserve required exits and arrivals before laying out destinations and routes. Shape terrain/water around the functional plan, then add scenery and materials. Noise contributes relief/ecology; it does not decide where required exits happen to fit. Keep topology, gameplay and decoration random streams independent.

Use forest-basin clearing graphs, river-led terrace/crossing layouts, ridge/contour highland routes, courtyard-based ruins, shoreline/headland layouts and landmark-first precincts. Reuse the existing terrain/route/reservation queries and authored destination inputs. These recipes generate geography and placement; they are not a catalog of fixed whole maps.

Bound candidate searches and validate their results. Required connections fail explicitly rather than being omitted. A fallback must retain graph obligations and remain procedural; report failures if no valid fallback exists. Validate generated routes with actual controller/collider/navigation evidence as well as arithmetic checks.

Graph/layout separation is documented by [Edgar for Unity][P1] and Dungeon Architect's [Unity flow graph][P2]. Edgar's original implementation is primarily 2D; its architecture is a reference, not a 3D dependency recommendation. WFC's original implementation documents local compatibility and contradictions [P3]; explicit global path constraints are an extension [P4]. Keep WFC out of the first implementation; revisit only for bounded architectural adjacency that benefits from it. Unity AI Navigation provides runtime surfaces [P5]; installed 2.0.14 source confirms asynchronous updates, with source collection still a separate cost. No third-party generator is required or approved by this research.

### Natural outdoor boundaries and paired passages

Compose playable interior, a permanent boundary belt and limited scenic surround. Mix steep wooded banks, rock, deep water, cliff sections and ruins so every map is not the same circular cliff bowl. Harvestable trees cannot be the sole permanent barrier. Fog supports depth; visible geometry explains where movement stops.

| Passage | Composition and pairing |
| --- | --- |
| Wooded bend | Trail curves between pines, roots and banks; permanently shaped ground/rock supplies the boundary. |
| Rock cleft/ravine | Flanking rock and a bend conceal the next landscape; reserve clear approach and arrival ground. |
| Mountain saddle | A sheltered crest frames the destination valley; both sides communicate compatible elevation. |
| Bridge/causeway | Crossing leads toward a concealed bend/gatehouse; share river identity and structural language. |
| Ruined gate | Wall breach is integrated with slopes/retaining ground; preserve the architectural identity on return. |
| Shore/headland path | Water bounds one side and relief the other; shoreline character continues across the connection. |
| Cliff stair | Traversable terraces imply ascent/descent without requiring a continuous adjoining world. |
| Ferry, waterfall or runestone route | Occasional special connections; dedicated content follows the ordinary outdoor journey. |

An edge normally needs an exit composition and an arrival composition, not another empty loaded corridor. A dedicated pass must earn its loading screen with worthwhile content. [Torchlight II's randomized outdoor areas and themed passes][P6] remain the closest pacing reference.

Validate a moving camera envelope along paths, around beats and at arrivals at supported zoom/aspect ratios, including safe lens offsets. Reserve destination sightlines and avoid repeatedly putting the player behind foreground crowns/cliffs. Cutaways assist visibility without replacing adequate terrain composition. Distant landmarks may be scenic representations rather than resident adjoining areas.

## 1. What “better” means in this project

The target is **art-directed atmospheric fantasy built from low-poly assets**, not photorealism or maximum rendering settings. Aim for an expressive, cohesive image that remains readable and stable while playing.

Preserve runtime seed/save generation, day/night and weather, player structures and lights, harvestable trees and rocks, and no unnecessary distant persistent simulation. Use the Synty assets actually present; additional packs are not assumed purchased. Preserve vendor assets and palette/UV conventions through project-owned materials, variants, shaders, and generation rules.

### Provisional alpine art direction

> Tactile, stylized alpine fantasy: readable faceted trees and stone, organic traversable ground, deliberately framed mountain silhouettes, cool layered local depth, warm destinations, coherent vegetation groups, expressive atmospheric light, selective luminous accents, and intentional distance softness. The world should feel composed and inviting rather than uniformly decorated.

Compare cool blue-gray mountains, evergreen foreground masses, warmer meadow/path accents, and amber destinations as one starting hypothesis. Do not force orange/teal grading onto every weather state or turn night into blue-filtered daytime. Preserve the hybrid-ground direction and stylized physically lit characters with restrained rim treatment; thick outlines, heavy cel bands, full-body emission, and noisy photorealistic surfaces are not automatic improvements.

**Substantial post-processing is welcome.** Strong atmosphere, bloom character, and deliberate depth softness may define the identity. “Performance-conscious” does not mean visually sterile, and Bokeh DOF is not prohibited. Protect gameplay and material identity rather than enforcing realism or minimal effect strength.

### Five visual questions, not an automated beauty score

| Dimension | Ask about the actual render |
| --- | --- |
| Composition and exploration | Is there a readable route, useful focal point, and rhythm of enclosed and open spaces? |
| Palette and lighting | Do sky, shadow, ground, foliage, and accents belong together? Do important silhouettes separate? |
| Depth and atmosphere | Are foreground crowns, playable middle ground, boundaries and occasional background reveals layered without hiding interaction? |
| Integration and materials | Do terrain, rocks, paths, trees, buildings, and characters feel grounded and materially distinct? |
| Motion and gameplay | Does the image hold together through movement, wind, combat, weather, and LOD transitions? |

Use **better / similar / worse / unverified**, confidence, and a concrete observation. Do not invent decimal beauty scores or self-award “8/10.” The owner's preference is authoritative; offer milestone A/B evidence without requiring approval for every iteration. Until feedback arrives, preserve rollback and label artistic selections provisional.

## 2. Establish a small visual reference in the real world

Use the production-generated forest-basin/river-terrace journey. Save three bookmarks: an ordinary route, a destination and a passage/arrival. Inspect one short walk, camera-follow motion and zoom plus departure/return. Use the actual fixed-angle perspective gameplay camera; retain matched lens/zoom settings unless they are the experiment.

This is a slice of the production generator using ordinary terrain, shaders, whole-area loading, interaction, and lighting—not a hand-built showcase, a per-seed lighting bake, or decoration confined to one camera frustum. Keep a second seed or unedited region for a single generalization check after substantial global or generative changes; add cases only after observed failures justify them.

Normal captures and saved transforms are enough. Add a tiny capture helper only when it removes repeated friction. Use the same capture path for A/B; do not add extra camera rendering to the measured gameplay workload.

### Match what is not intentionally changing

Hold camera, output resolution, internal render scale, AA/upscaler, game time, weather, and relevant gameplay conditions constant except the variables under test. Record SDR/HDR output and build/platform differences. An upscaling experiment deliberately changes internal resolution, not the claimed output resolution. A terrain-layout change may invalidate a bookmark; record the relocation and compare corresponding gameplay views rather than a silently improved composition.

Keep neutral daylight and one atmospheric state. Check night, backlighting, a clear horizon, or construction lights when affected—not every combination after every edit. Capture raw game output without external enhancement, selective crops, or enlarged render scale. Let temporal history settle for stills, then inspect motion; settled images can conceal ghosting and shimmer.

## 3. The hill-climbing loop

### A. Critique briefly

Inspect the images and movement. State at most three specific weaknesses and choose one: for example, the path disappears into equal-value grass, tree crowns merge with the distance, or rocks have unrelated ground contacts. Do not diagnose unseen images or replace implementation with an aesthetic essay.

### B. Choose leverage, not novelty

Prefer a change affecting a large, important portion of normal play at modest implementation cost. Palette/lighting/fog may need coordinated adjustment; perfect per-slider attribution is unnecessary. For a performance diagnosis, separate competing mechanisms only when the distinction changes the next implementation. A new technology earns its place by solving a visible or observed problem.

### C. Implement one bounded candidate

Use existing systems and save the incumbent settings/diff. Suitable slices include a forest-edge rule, one lighting/tonal profile, one ground/rock treatment, one representative tree LOD chain, or one atmospheric pass. “Build an environment framework” is not a candidate. Follow the selected package, not every technique listed in this document.

### D. Review appearance, motion, and basic cost

Compare matched views; a contact sheet is optional. For close artistic choices, inspect unnamed A/B images before revealing cost or labels such as “ultra” and “optimized.” One actual image-inspection pass is enough; do not create a committee of agents.

Inspect walking/turning for shimmer, ghosting, blur pumping, shadow instability, vegetation crawling, and transitions. Without video inspection, use sequential frames and available interactive observation, and mark temporal behavior unverified where appropriate. Still images cannot prove stability.

Use comparable frame-time evidence plus a hitch observation. When a frame cap or VSync masks differences, use available CPU/GPU timings or a brief matched uncapped comparison; otherwise report the performance difference as unresolved. Do not mistake cap-waiting for useful work. Stop after two short trials if the change remains within noise; expand profiling only when it distinguishes different next implementations.

### E. Keep, isolate, or revert

| Result | Decision |
| --- | --- |
| Clearly better art; cost improves or remains similar within available evidence | Accept after relevant correctness checks. |
| Similar appearance; substantial performance improvement | Accept and retain some headroom. |
| Clearly better art; additional cost fits the current envelope | Accept as an explicit visual investment, not a free optimization. |
| Attractive but unaffordable | Preserve a lightweight optional preset or reversible diff, disabled in the default. |
| Faster by degrading intended appearance or gameplay | Reject as the main solution; label diagnostic or explicit lower-quality option. |
| No visible benefit, inconclusive performance, increased complexity | Revert or simplify; retain only independently justified fixes. |
| Broken rendering, collision, persistence, stability, or resource ownership | Fix or revert before completing the batch. |

Do not accumulate unfinished feature branches. After two low-impact attempts in one dimension, change the problem category: composition may need generation rules rather than grading; motion may need shader correctness rather than more blur. Permit one larger coherent artistic alternative when the incumbent has plateaued, with the same bounded review and rollback.

## 4. Performance envelopes that permit visual progress

60 FPS corresponds to approximately 16.67 ms per frame; 120 FPS to 8.33 ms. These are calculations, not predictions. The active development target is the owner's Apple M5 MacBook at 2560 × 1440/60. Future Windows targets RTX 4060 with a mid-range six-core CPU; exact device/CPU remains unselected. Windows hardware performance is unverified. Neither device is inferred equivalent to the other.

**Current development envelope:** Keep the current default's matched frame time and hitch behavior from materially deteriorating while improving the image. The current result has not received owner aesthetic acceptance. Favor inexpensive artistic changes, work removal, or paired optimization. Keep expensive unfunded candidates out of the default; do not require 60 FPS before look development.

**Shipping envelope:** Move toward 1440p/60 on the selected Windows baseline with headroom and a separately evaluated high-refresh preset. Label native and upscaled rendering distinctly. Evaluate Windows Players periodically and after meaningful rendering changes, not after every profile tweak. Generated/interpolated display frames, where an integration supports them, are not proof that the game meets the underlying 60/120 FPS target.

### Reinvest selectively

Initially, retain at least half of a verified reduction in the limiting GPU workload as headroom; spend at most the remainder on effects until baseline performance is healthy. This is a conservative project policy, not a Unity rule. Change it deliberately when evidence or artistic priorities justify it.

CPU and GPU work overlap. Saving 5 ms of CPU work does not automatically fund 5 ms of GPU effects. A CPU-limited scene may already have independently measured GPU headroom; verify it. Without GPU timing, use matched end-to-end observations provisionally and do not invent a savings account or add timings from different runs/platforms.

Track only the accepted frame-time observation, important hitches, candidate cost, and uncertainty. A small average gain does not excuse worse movement/combat spikes. Treat gameplay, loading and repeated-travel residency as separate workloads; exclude loading-screen frames from gameplay timing. Record logical footprint and actual resident geometry, cold/warm load stages and peak/residual memory. Smaller regions can still expose substantial foliage/roof/ground coverage from above. No budgeting dashboard or initial benchmark matrix is required.

## 5. Highest-leverage visual/performance work packages

The order is a starting priority, not mandatory sequencing. **Select one package or coherent slice, not the whole stack.** Numerical starting points below are experiments, not universal optimal settings.

### Package A — Palette, sky, light, and an intentional post-processing stack

**Visual aim:** Replace the default asset-preview appearance with a coherent fantasy image. Establish key light, ambient fill, sky, ground, foliage, and luminous accents before chasing surface detail. Preserve colored shadow detail and readable highlights.

**Color foundation.** Reuse the day/night controller and active URP Volume profile. URP integrates post-processing through Volumes; Post Processing Stack v2 is not its rendering path. HDR scene rendering and HDR display output are separate choices: HDR lighting/bloom can be developed for SDR presentation, while HDR display output needs its own tone-mapping/calibration checks. [U1] [U32]

Check the existing color-space/HDR configuration rather than casually switching the whole project's color space. Retain the current Golden/Silver direction, Natural option and ACES/LUT treatment as the incumbent. Compare Neutral only for a visible color-response problem. Neutral changes hue/saturation less; ACES changes contrast and color response. Neither is inherently more artistic. Use white balance, broad tonal relationships, and selective color adjustments to preserve Synty material distinctions. [U7]

Drive exposure/look transitions deliberately from time, weather, and existing environment state. The checked URP feature table provides fixed exposure, not HDRP-style built-in automatic exposure. Do not build an exposure-metering system merely to hide inconsistent lighting. Avoid multiple controllers writing the same properties. [U18]

**Bloom as authored light, not a veil.** Make flames, magic, sunlit accents, and selected emissive objects carry the glow; do not brighten every diffuse surface. Start with HDR values and the normal global bloom path rather than another camera for selective bloom. Unity's Threshold is expressed in gamma-space brightness. The 6.6 reference documents Gaussian, Dual, and Kawase filters, with different appearance/cost, plus Downscale, Max Iterations, and High Quality Filtering. [U8]

Quarter-resolution Dual is already configured. For a justified cost/halo problem, compare the incumbent against one alternate candidate, retuning its halo width/intensity rather than copying Gaussian values unchanged. Keep Gaussian or higher-quality filtering when motion reveals unstable small highlights. Do not test every filter/setting combination. Judge bloom against pale rocks, sky, night lamps, and moving magic—not only a flattering sunset. [U8]

**Finishing.** Use modest vignette only where it supports composition; inspect dithering if smooth fog/sky gradients band. Film grain, chromatic aberration, lens dirt, aggressive sharpening, and distortion are not default quality upgrades. Deliberate far softness belongs in Package D. Reserve striking flares or distortion for specific sources/events rather than making ordinary traversal a permanent camera artifact.

**Cost/acceptance:** Reconfigure existing passes first; do not duplicate grading, bloom, or controllers. Accept a visibly stronger overall image in daylight and one alternate state without crushed gameplay shadows, excessive glow, or substantial unfunded cost. A coherent artistic change can be bold without enabling more features.

### Package B — Composed vegetation, efficient representation, and submission

**Visual aim:** Create forest edges, tree families, clearings, framed routes, and density contrast. Keep large/medium/small hierarchy and intentional empty space. Better distribution is not merely a thinner forest.

Use seeded world-space cluster, slope, path, and spacing rules. Cache static placement/exclusion data on generation or invalidation rather than every frame. Preserve biome identity and harvested-object IDs. Retain important silhouette trees and near-path masses while removing redundant clutter.

**Representation first.** Prototype one representative tree's near/mid/far geometry, material, shadow, and wind behavior before creating an impostor pipeline. Evaluate screen contribution rather than triangle count alone. Preserve silhouette and stable transitions; combining an entire forest into one giant mesh sacrifices independent visibility and interaction. Keep harvest state independent of render representation.

**Modern submission.** GPU Resident Drawer (GRD) is already enabled. Verify changed renderers participate before proposing a new submission system. Preserve the current compatible path before custom BRG/indirect rendering or an Entities rewrite. The 6.6 documentation supports assets whose renderers use **Forward+ or Deferred+**. GRD uses BatchRendererGroup, supports runtime-created/changed objects, and requires compatible materials/settings; MaterialPropertyBlocks prevent participation. Retain required BRG shader variants and verify the target objects actually use GRD. Animated time-based LOD crossfades are unsupported; distance-based crossfading is the documented fallback. [U11]

Keep SRP Batcher enabled where compatible. It reduces render-state setup; it is not the same operation as instancing. Do not stack every batching switch: static batching conflicts with GRD/BRG, and material-instancing checkboxes are not a universal SRP optimization. Prefer shared materials, stable mesh data, or a bounded set of variants for variation rather than per-tree material cloning/MPBs. Custom per-instance data paths are justified only after the simpler route fails. [U13]

**Occlusion is conditional.** GRD's GPU occlusion can help genuinely occluded geometry but adds work and can lose on open vistas. It is currently disabled. Compare a forest/cliff case and an exposed overhead clearing; do not enable it because “GPU-driven” sounds faster. Its conservative depth-based tests do not substitute for streaming, simulation relevance, or correct bounds. [U12]

**Accept when:** The forest looks more composed across viewpoints and a second seed, transitions/harvesting remain correct, and any performance claim comes from comparable execution rather than object counts alone.

### Package C — Terrain/prop cohesion and material identity

**Visual aim:** Integrate ground, stone, tree bases, paths, snow, and structures. Differentiate stone, wood, foliage, cloth, and metal without uniformly glossy surfaces or photorealistic noise.

Reuse biome/slope/height fields for palette changes and selective rock/ground transitions. Add restrained low-frequency world-space macro variation. Preserve atlas UVs, faceted/hard-edge intent, and project-owned overrides. Prefer cached vertex/mask data when the variation is static; do not rebuild masks or duplicate materials continuously.

Prototype one coherent material family. For foliage, compare a simple backlit color response and believable roughness before expensive translucent shading. For characters, preserve readable roughness/specular differences and restrained rim treatment rather than adding outlines or full-body emission. These are proposed artistic treatments, not new native URP features.

Use decals selectively for important paths, contacts, or localized storytelling where generation/material blending is insufficient. URP decals do not project onto transparent surfaces; their batching differs from ordinary SRP-batched materials. Shared decal atlases and the decal-specific GPU-instancing path can reduce submissions, but coverage, passes, and projector count still need a budget. Do not add a projector to every rock by default. [U26]

**Water slice:** Start with coherent body color, restrained moving normals, shoreline/depth treatment, and sky/probe reflection matching the world. Add opaque-color refraction only if it visibly improves gameplay views; check what its texture dependency adds. SSR and planar reflections are separate custom investments in URP, not required checkboxes for attractive water. Match fog and inspect shorelines, alpha effects, and camera movement. [U18] [U19]

**Cost/acceptance:** Avoid blanket triplanar sampling, universal expensive shaders, or needless detail maps. Simplify distant shading without changing material identity. Accept when contacts improve from the gameplay camera, chunk boundaries agree, required vertex channels survive Player builds, and new masks/materials have bounded ownership and memory. Shader changes must meet the pass contract in Package F.

### Package D — Atmospheric depth, deliberate softness, and selective volumetrics

**Visual aim:** Distinct foreground crowns, playable middle ground, mist-filled local pockets and scenic boundary layers, with occasional monumental reveals suggesting a larger realm. Atmosphere should compose the landscape, not erase it.

**Base atmosphere.** Topaz already uses shared analytic height/distance fog and an analytic sky/cloud treatment; native distance fog is disabled. Refine that incumbent before adding another atmosphere path. URP's checked native feature set has distance fog, not HDRP's volumetric atmosphere/cloud system. Analytic height fog and volumetric light scattering are different features. [U18]

Preserve coordinated sky/horizon color and focal silhouettes using shared parameters across terrain, foliage, proxies and relevant transparent materials. Do not layer native fog plus custom fog over the same contribution unintentionally. Couple distant simplification to the atmosphere, while retaining clear-weather boundary/arrival and maximum-zoom checks. The [world reference](../PROCEDURAL_WORLDS.md#clock-weather-and-rendering) owns the current opaque/transparent fog contract.

**Depth of field as an artistic choice.** Player-tracked Bokeh is the current intentional gameplay default. Retune its protected gameplay depth band for isometric slopes/zoom. Compare far-only Gaussian only when a specific softness/cost problem justifies an alternative. Gaussian is faster and far-only with limited blur radius; Bokeh supports near/far blur at greater cost. Bokeh remains a legitimate gameplay candidate when its visible benefit and budget justify it—not automatically a cutscene-only feature. [U3]

Protect combat and interaction distances in camera space. Reuse the player-tracked focus plane, preserving nearby enemies, aim/gather targets, building previews and exits. Review camera-space depth on slopes and at maximum zoom; a circular world-space interaction radius does not establish focus coverage. Test foreground crossings, foliage edges, transparent effects, and turning. Do not stack strong temporal softness, DOF, and bloom until all texture and silhouettes disappear, or use blur to excuse missing scenery.

**Funded volumetric slice.** When ordinary haze cannot deliver the desired shafts or depth, allow one bounded, shadow-aware main-light scattering experiment in a clearing or destination. Limit integration distance, participating lights, and sample workload. Reduced-resolution evaluation with depth-aware reconstruction is a design option only where the implementation supports it; temporal reuse adds history/disocclusion obligations. A low-resolution implementation is not automatically cheap or artifact-free.

A concrete free code/reference candidate is Cristian Qiu's MIT-licensed **Unity-URP-Volumetric-Light**. Its README documents Render Graph support, but its stated version coverage ends at **6000.4**, not verified 6.6 support; it also reports incorrect blending with transparent objects. Quarter-resolution rendering and reprojection are listed as future work, not delivered features. Treat it as a pinned, isolated feasibility candidate, not an approved dependency. Metal/6.6 behavior must be checked locally before adoption. Preserve its license if used. [O1]

**Transparency is a design constraint.** A full-screen effect sampling opaque depth cannot automatically fog water, particles, or glass at their actual depths. Moving the pass after transparents does not fix absent transparent depth. Prefer compatible material-level fog/shared transmittance or an explicit, bounded composition solution. Do not fix it by adding a second full-world camera or indiscriminately forcing transparent depth writes.

**Cost/acceptance:** Fog and DOF do not remove the underlying draw/shading workload. Count actual representation/culling savings separately from effect cost. Accept when the depth hierarchy improves while panning/zooming and at boundaries/arrivals, without loading holes, boundary halos, silhouette loss, blur pumping, or returning harvested objects.

### Package E — Grounding, shadow character, and important lights

**Visual aim:** Near-field contact, modeled forms, readable characters, and inviting destinations without physically exact distant shadows.

**SSAO with a purpose.** URP SSAO is a **Renderer Feature**, not a Volume override. Start with contact-scale occlusion rather than broad dirty shading. One reasonable trial is downsampling, four samples, limited falloff distance, and restrained direct-light darkening; increase quality only for an observed defect. Depth Normals generally gives better geometric information; Depth reconstruction may avoid that pass but is not universally faster or equivalent. [U9] [U10]

Check whether normals/depth are already produced. `After Opaque` with Depth can avoid an SSAO-driven prepass, but changes compositing and can over-darken existing occlusion. Blur quality changes both edge handling and pass count: evaluate halos and thin geometry rather than automatically choosing the cheapest blur. Small intrinsic mesh/material AO can complement contact shading, but does not describe new contacts between runtime-placed objects. [U10]

**Shadow character.** Compare targeted distance/cascade allocation, coherent filtering, and representative simplified distant casters. Lower-resolution filtered shadows can suit this art direction, but do not confuse softness with unstable or detached shadows. Prioritize near contact and silhouette trees. Set distance/cascades from the visible receiver envelope and relevant offscreen casters, including long low-sun shadows; do not copy the wilderness horizon budget. Point-light shadows require six directional captures, so separate decorative emission, ordinary unshadowed light, and a small important shadow-casting set. Fade transitions and use stable priority/hysteresis rather than visibly switching nearest lights. [U4]

**One optional cost experiment:** Screen Space Shadows resolves main-light shadows into a screen texture; it is not a new contact-shadow effect and does not eliminate the underlying shadow-map rendering. The extra depth/pass/texture cost may or may not beat repeated sampling. Test only for a relevant shadow-receiving workload, not as a universal quality toggle. [U33]

**Runtime world lighting.** Keep sky/ambient fill and specular environment response consistent through day/night and weather. Bound runtime reflection probes and update them intentionally, using suitable resolution, culling, and time slicing rather than refreshing every probe every frame. Probe captures render six faces and require filtering; scheduling smooths spikes but does not erase total work. [U25]

APV lighting scenarios and sky occlusion operate on baked data. They do not automatically generate valid indirect lighting for arbitrary new seeds, harvested scenery, and player construction. The newer Compute Light Baker likewise does not establish a ready-made runtime GI solution. Keep APV/baked approaches for explicitly supported fixed content. Topaz already uses native Surface Cache GI for runtime diffuse lighting: retain its measured-quality direction, exclude scenic-only geometry where appropriate, apply saved structures before cache preparation and reuse the existing ready-geometry reset. Check arrival, panning, harvest and building costs/stability; a bounded procedural area does not become valid baked content. The local installed implementation is stronger evidence of availability than an announcement [P7]. [U24] [U27]

**Accept when:** Important forms and destinations are grounded in daylight and at night, construction lights work, and there are no dark halos, unstable shadows, hidden light-count explosions, or stale environment reflections obvious during play.

### Package F — Motion, AA/upscaling, and environmental life

**Visual aim:** Coherent wind, responsive weather, convincing water, occasional local effects, and unobtrusive transitions. A beautiful settled frame with crawling foliage is not an accepted look.

#### Choose one supported image-reconstruction path

| Candidate | Use and limitations |
| --- | --- |
| Native resolution with SMAA | Useful sharp, non-temporal reference/fallback while fixing motion-vector issues; does not supply temporal stability. |
| Native TAA or STP at scale 1 | Evaluate history-based stability against ghosting and loss of fine shape. STP remains active at scale 1. |
| STP with a fixed lower render scale | A proposed first quality/performance trial is 0.85 at 1440p output. Evaluate actual output and cost; keep native and reconstructed results labeled separately. |
| MSAA in a supported forward path | Can improve geometric edges, but does not solve specular/texture aliasing; do not stack with TAA. |

URP TAA is incompatible with MSAA, camera stacking, and Dynamic Resolution. STP implicitly uses TAA preprocessing; its documented URP path uses **Render Scale, not the camera's Dynamic Resolution feature**. Topaz already has a bounded render-scale adapter and saved preferences. Retain them and the STP/native comparison; do not add a second TAA stage or another resolution controller. [U5] [U15] [U16]

The checked 6.6 resolution documentation lists STP and FSR 1 for URP, not native DLSS 4/FSR 2 support. A roadmap announcement or a shared Core API is not proof of installed URP integration. Any newer/vendor upscaler needs explicit package, API, platform, and motion-input verification; do not make it a prerequisite for this plan. [U31]

#### Make deformation correct before increasing temporal filtering

URP motion vectors support opaque and alpha-clipped materials, not ordinary transparent materials. Animated wind therefore needs correct object/deformation vectors where supported; water and particles still need explicit artifact checks. Do not enable Alembic motion-vector settings for unrelated wind shaders. [U6] [U17]

Shader Graph's **Time-Based** additional vectors reevaluate deformation at current/previous time, but assume other inputs remain unchanged between frames. Changing global wind amplitude, direction, or weather parameters violates that assumption. Use an appropriate previous-state/custom-vector solution when needed; do not claim a Time node alone fixes changing weather. Time-Based/Custom vectors also add per-object rendering work, so distant wind/representation should be intentionally simplified. [U17]

**Shared shader pass contract:** Any changed wind, vertex displacement, alpha clipping, or LOD fade must agree across color, shadow, depth, depth-normal, and motion-vector passes that the active renderer uses. Keep bounds large enough for deformation. Gameplay scenery cutaways are an intentional exception: camera color/depth/normals/motion share the reveal, while shadow and Meta/GI retain physical geometry as described in [feature policy](../UNITY_FEATURE_POLICY.md#fixed-camera-visibility). Retain matching current/previous state, and reset temporal history on discontinuities such as camera cuts, teleports, or incompatible resolution changes. Test spawning, harvesting, and LOD transitions as well as steady motion. [U6] [U14] [U17]

Reuse coherent global weather inputs and bounded camera-relevant particles. Unity 6.6 adds Shader Graph support for the Built-in Particle System, including mesh-particle instancing; an existing particle system does not need a VFX Graph rewrite just for modern materials. [U27]

**Accept when:** The walk/turn improves, thin silhouettes do not trail, highlights do not crawl, and rain/water/magic remain readable. Reduce the originating aliasing—tiny geometry, noisy normals, excessive smoothness, inconsistent deformation—rather than reflexively adding stronger blur or sharpening.

### Package G — Procedural area composition, boundaries and loading

**Visual aim:** Three meaningful beats in an intimate, varied outdoor landscape, with readable choices, natural passages and complete arrivals. Forest basins, terraces, ruins, passes and shores need different organizing geography. The eight-area network is implemented; refine and validate the representative journey before adding more topology or content. Three generated reservations alone do not establish meaningful pacing.

Keep WorldSession as state authority. WorldGraph supplies stable area IDs/seeds and paired passages; AreaDefinition supplies recipe overrides; AreaPlan supplies immutable local queries; AreaLocation pairs area ID with local coordinates; AreaRuntime owns resident scene resources. Travel requests serialize through WorldSession, never a second save manager.

Prepare the whole area before revealing gameplay. Reuse terrain/data upload and art-placement machinery; stop movement-driven terrain residency in this mode. A bounded terrain allocation may include support tiles beyond its logical footprint; count them honestly. Remove distant canopy/horizon responsibilities from the new mode only after replacement coverage is demonstrated. Retain useful local LOD and camera-relevant lighting/casters.

Loading records departure durably, fades out, prepares geometry and saved state, finishes collision/navigation, validates arrival and resets camera/render/GI state before committing/revealing arrival. If destination realization fails after retirement, reconstruct the recorded origin; if that fails, retain the last valid save and allow return to title. Show destination and ordinary loading feedback, not internal pipeline jargon or invented percentages.

Area identity qualifies every spatial service: structures/storage, pickups, resources, shelter, camp protection, recovery and encounter state. Nearby materials remain saved-state queries within 30 m in the same area. Preserve resource and defeated-enemy deadlines; surviving enemy health/position must survive immediate retreat/return. Loading pauses the World clock. Reserve passage/arrival corridors against construction.

**Targeted CPU option:** Jobs/Burst remains available for a measured terrain/placement computation, not a prerequisite or gameplay rewrite. Main-thread uploads, collider activation and navigation source collection remain separate costs. Native writable MeshData is an option for measured mesh-generation work [U27] [U28].

**Accept when:** Seed variation produces useful three-beat rhythm and different area shapes; ordinary paths/arrivals are readable at supported zooms; return state is correct; terrain no longer appears during in-area movement; and repeated travel reaches bounded resident resources. Cold/warm loading and first-use effects are measured separately from gameplay. Small areas do not automatically make vegetation, GI or post-processing affordable.

## 6. Translate good images into generation rules

Identify the scope of every accepted improvement: renderer-wide look, reusable material/prefab behavior, local composition rule, or region-generation rule. Express the principle, not the screenshot.

| Local observation | Reusable rule to test |
| --- | --- |
| A tree gap makes a destination readable | Reserve bounded sightline openings along selected approaches, not the whole forest. |
| Rock groups feel more convincing than scattered pebbles | Hierarchical groups with scale/spacing constraints and coherent nearby ground treatment. |
| Dense borders and open meadow interior work better | Correlate vegetation with clearing/route fields instead of uniform density. |
| A layered ridge expands the apparent world | Generate limited area-owned scenic silhouettes consistent with landmark direction; keep adjoining gameplay areas unloaded. |
| Light shafts improve one forest opening | Select suitable openings/light conditions, not a full-cost volume around every tree. |

Use stable world-space fields and bounded updates. A local art parameter must not rebuild the whole world. When changes affect persistent identity or existing saves, version the data and test a fresh seed plus the relevant saved-world behavior. A beautiful reference view does not override persistence or navigation correctness.

## 7. Minimal controls and rendering integration

Reuse existing configuration assets. Introduce a small shared visual profile only when no adequate control point exists; do not create an environment-service framework.

Separate **art direction**—palette, atmosphere, roughness intent, lighting character, focal glow—from **quality implementation**—samples, resolution, shadow/representation distances, and reconstruction mode. Maintain one working production preset and, at most, an optional aspirational preset. Lower quality should preserve identity, not substitute an unrelated palette or remove all atmosphere.

Record coupling: focus protects combat range; fog matches proxy/water coloration; shadow distance does not define horizon distance; grading preserves material differences. Keep day/night, weather, local-volume blending, and quality changes under clear ownership. Do not stop Volume updates for CPU savings without preserving all required transitions. [U2]

### Contract for a custom full-screen effect

Use a **Full Screen Pass/Fullscreen Shader Graph** for a suitable simple effect, or a **Render Graph renderer feature** for explicit intermediate textures, multiple resolutions, or history. Reuse installed URP samples. Use `RecordRenderGraph` and appropriate `AddBlitPass`/SRP Blitter paths rather than legacy `CommandBuffer.Blit`/`Graphics.Blit` integration. Do not disable modern rendering to accommodate a tutorial. [U19] [U20] [U21]

Declare only actual inputs. In the checked **6.6 Full Screen Pass**, `Fetch Color Buffer` exposes current camera color through `_BlitTexture`; `Requirements: Color` requests the **opaque texture**, a different resource. Request depth, normals, or motion only when used; `Everything` can introduce avoidable passes. Keep intermediate textures automatic where supported rather than forcing them to mask undeclared dependencies. [U19] [U14]

Choose placement from the intended signal: luminous HDR contributions should participate in the intended bloom/tone pipeline; display-space finishing belongs after tone mapping. Keep HUD/text outside world blur/grading unless intentional. Verify actual ordering around transparents and temporal reconstruction in the installed renderer rather than copying a universal pass-order diagram. Combining compatible pointwise operations can avoid extra passes; Render Graph does not automatically merge arbitrary neighborhood filters.

Use graph-managed transient textures for within-frame work. Imported/history resources require explicit ownership; do not retain transient handles across frames or share incompatible history between cameras. Filter camera types so gameplay effects do not accidentally repeat on reflection, preview, or stacked cameras. Release owned resources and handle resize/disable/reload correctly. [U20]

**Two 2026 techniques to use selectively:**

| Technique | Decision for this project |
| --- | --- |
| On-tile post-processing | Designed to retain supported effects in tile memory; documented support is grading, vignette, tone mapping, dithering, and grain, with integrated post-processing disabled. It is not a drop-in replacement for this Windows bloom/DOF stack or a guaranteed desktop speedup. [U23] |
| Depth input attachments | Unity 6.6 allows current-pixel depth fetches on DX12/Vulkan. Consider only for a relevant depth-copy cost; use capability checks and an explicit sampled-depth fallback on unsupported APIs such as Metal. It does not supply arbitrary neighboring depth samples for free. [U22] |

For any added dependency, pin the version/commit, retain the license, and verify the actual 6.6 render path plus Windows/Mac behavior. A successful import or a “Unity 6” README is not compatibility evidence for every patch/API.

## 8. Validation without recreating the previous failure mode

Use the cheapest evidence appropriate to the decision. Editor/Mac observations screen candidates; they do not certify Windows shipping performance. Unsupported counters are unavailable, not zero. Keep build type/instrumentation, resolution, frame cap, and platform comparable; do not mix a debug run with an optimized Player and attribute the difference to a shader change.

**Journey milestone review:** Three matched journey views, one camera-follow/zoom/movement observation, departure/return and one comparable gameplay-cost observation, plus patch-specific correctness checks. Ordinary local refinements use the narrower [workflow](../AGENT_WORKFLOW.md#iterate-and-hand-off) scope. Whole-area retirement/loading changes additionally need cold/warm stages and repeated-travel residency; they do not require a new profiling suite. Add a second seed after substantial generative/global changes. Add memory, cold-start, or platform investigation only when the patch introduces that risk or an observed failure requires it. Do not run a full build for every profile edit; batch compatible changes. Renderer/shader changes need an actual Player check before being declared shipping-complete. Headless/no-graphics execution is not a rendering test.

Select correctness checks from what changed: collision/routes at terrain seams and exit/arrival footprints; harvesting across LODs/revisits; construction lights; water/transparent compositing; shadow/depth/motion agreement; and resource disposal. This is not permission to retest every subsystem.

**Runtime feature retention:** Shader stripping can remove states that appear only through runtime weather/quality changes. URP's unused-post-variant option assumes the Player does not create otherwise unseen Volume Profiles. Keep representative serialized profiles/material states or an explicit retention strategy for the features actually used; retain GRD-required variants when applicable. Exercise the changed states in a Player instead of preserving every shader variant indiscriminately. Investigate first-use shader stalls only when observed; no universal prewarming framework is required. [U29] [U30] [U11]

Before deeper profiling, state the unresolved question and the two possible next implementations. If both outcomes lead to the same small fix, implement it first. Before another artistic variant, state the unresolved preference or visible defect it tests. Do not force precise timing attribution for a coherent retained batch, or an artistic verdict when evidence is unavailable.

## 9. Compact handoff

Use chat and existing managed artifacts, not routine persistent milestone/handoff notes. State the accepted diff/configuration, concrete visible changes and weaknesses, actual images/motion inspected, relevant correctness checks, comparable gameplay/loading/residency observations, platform/evidence level, rollback and one next task. Mark unavailable evidence honestly. Pin the current owner-review build and unresolved diagnostics under the repository retention workflow.

The milestone is a better playable generated journey, raw evidence and an honest cost/uncertainty summary. Documentation volume, a new renderer framework or profiling machinery does not substitute for owner judgment. Preserve source archives, dependencies and unfinished audio reviews.

## 10. Relationship to the earlier guide

`Unity_6_6_URP_Procedural_World_Optimization_Guide.md` remains optional technical reference, using version 3's section mapping: sections 5–6 for rendering/instancing; 7–9 for LOD/relevance/visibility; 10–11 for fog/post-processing/shadows; 12–16 for placement/streaming/memory/prewarming/combination. That separate guide was not supplied for this revision and has not been re-reviewed here.

Its exhaustive audit, measurement suite, gate sequence, many-document deliverables, and original implementation prompt are **not active prerequisites**. Use this bounded loop and current installed-version behavior when older technical guidance conflicts.

## 11. Research basis and principal changes

**Preserved:** The real-world reference slice, owner-led artistic direction, two-axis progress, bounded experiments/tooling, provisional Mac evidence, deterministic persistence, minimal handoff, and rejection of both visual degradation and profiling paralysis.

**Changed in revision 5:** Replaced continuous-world priorities with persistent procedural areas, paired natural thresholds, local three-beat graphs, moving isometric camera envelopes and separate gameplay/loading/residency evidence. Corrected hardware targets and described existing ACES/bloom/GRD/STP/SCGI as incumbents. Retained the researched rendering safeguards and optional experiments rather than reopening every choice. Earlier research added a concrete artistic post stack; conditional GRD/occlusion and reconstruction choices; exact 6.6 full-screen resource semantics; shader-motion/transparent-composition requirements; bounded runtime lighting/streaming options; and an explicit distinction between native features, optional implementations, and roadmap expectations. On-tile processing, baked APV, or an unverified upscaler is not presented as a universal solution.

Sources below are primary Unity documentation unless marked otherwise. They support specific engine behavior, not aesthetic rankings or promised speedups. Most are versioned **6000.6** pages; this is a current-2026 assessment, not a claim that every listed technique debuted in 2026. Read only the source needed for the selected change. When documentation and the installed package disagree, record the discrepancy and use locally verified behavior rather than starting an upgrade/migration project.

### Core pipeline and post-processing

- [U1 — URP post-processing integration][U1]: integrated Volumes versus legacy PPSv2.
- [U2 — URP performance configuration][U2]: costs and dependencies to inspect, not a blanket disable list.
- [U3 — Depth of Field][U3]: Gaussian/Bokeh capabilities and tradeoffs.
- [U4 — Shadow optimization][U4]: casters, cascades, filtering, and point-light shadow workload.
- [U7 — Tonemapping][U7]: Neutral/ACES color response.
- [U8 — Bloom][U8]: filter choices, threshold convention, quality and downsampling controls.
- [U9 — SSAO integration][U9] and [U10 — SSAO settings][U10]: Renderer Feature behavior, sources, downsampling, and compositing.
- [U18 — Pipeline feature comparison][U18]: native URP versus HDRP capability boundaries.
- [U32 — HDR output][U32]: display output and tone-mapping configuration.
- [U33 — Screen Space Shadows][U33]: shadow resolve behavior and additional costs.

### Geometry, motion, and reconstruction

- [U5 — Anti-aliasing][U5]: method behavior and TAA incompatibilities.
- [U6 — Motion vectors][U6] and [U17 — Shader support for motion vectors][U17]: material/deformation support and previous-state requirements.
- [U11 — GPU Resident Drawer][U11]: eligible paths, objects, materials, and LOD limitations.
- [U12 — GPU occlusion][U12]: scene-dependent value and conservative visibility tests.
- [U13 — Draw-call optimization choices][U13]: SRP Batcher, GRD/BRG, and batching interactions.
- [U14 — Universal Renderer settings][U14]: paths, prepasses, texture timing, and intermediate targets.
- [U15 — STP requirements][U15] and [U16 — STP enablement][U16]: temporal reconstruction and Render Scale restriction.
- [U31 — Resolution scaling comparison][U31]: documented pipeline-specific upscaler availability.

### Integration, lighting, and generated content

- [U19 — Full Screen Pass][U19]: injection points and distinct current-color/opaque-color requirements.
- [U20 — Render Graph][U20] and [U21 — Blitting in URP][U21]: resource lifetimes and supported pass integration.
- [U22 — Depth input attachments][U22]: DX12/Vulkan capability and current-pixel depth access.
- [U23 — On-tile post-processing][U23]: supported subset and setup constraints.
- [U24 — APV runtime lighting changes][U24]: changing the use of baked lighting data.
- [U25 — Reflection probe optimization][U25]: capture cost and update scheduling.
- [U26 — Decals][U26]: transparency and batching limitations.
- [U27 — New in Unity 6.6][U27]: built-in Burst, particle Shader Graph, and light-baking changes.
- [U28 — Writable MeshData API][U28]: job-compatible mesh creation and application/disposal.
- [U29 — Shader variant stripping][U29] and [U30 — URP Graphics settings][U30]: build retention and runtime-profile assumptions.
- [O1 — Cristian Qiu, Unity-URP-Volumetric-Light][O1]: original MIT-licensed implementation; a feasibility reference with declared compatibility/transparency limits, not a verified 6.6 dependency.

**Core instruction:** Make the world look deliberately better and run usefully better through bounded, reversible iterations. Optimize perceived artistic value and playable consistency together—not realism, feature count, benchmark theater, or today's image in isolation.

[U1]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/integration-with-post-processing.html
[U2]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/configure-for-better-performance.html
[U3]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/depth-of-field-volume-override.html
[U4]: https://docs.unity3d.com/6000.6/Documentation/Manual/shadows-optimization.html
[U5]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/anti-aliasing.html
[U6]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/features/motion-vectors.html
[U7]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-tonemapping.html
[U8]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-bloom.html
[U9]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing-ssao.html
[U10]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/ssao-renderer-feature-reference.html
[U11]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/gpu-resident-drawer.html
[U12]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/gpu-culling.html
[U13]: https://docs.unity3d.com/6000.6/Documentation/Manual/optimizing-draw-calls-choose-method.html
[U14]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/urp-universal-renderer.html
[U15]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/stp/stp-upscaler.html
[U16]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/stp/stp-enable.html
[U17]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/features/motion-vectors-shader-support.html
[U18]: https://docs.unity3d.com/6000.6/Documentation/Manual/render-pipelines-feature-comparison.html
[U19]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/renderer-features/renderer-feature-full-screen-pass.html
[U20]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/render-graph-introduction.html
[U21]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/customize/blit-overview.html
[U22]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/read-depth-input-attachment.html
[U23]: https://docs.unity3d.com/6000.6/Documentation/Manual/on-tile-post-processing.html
[U24]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/probevolumes-understand-changing-lighting-at-runtime.html
[U25]: https://docs.unity3d.com/6000.6/Documentation/Manual/RefProbePerformance.html
[U26]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/renderer-feature-decal.html
[U27]: https://docs.unity3d.com/6000.6/Documentation/Manual/WhatsNewUnity66.html
[U28]: https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Mesh.AllocateWritableMeshData.html
[U29]: https://docs.unity3d.com/6000.6/Documentation/Manual/shader-variant-stripping.html
[U30]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/urp-global-settings.html
[U31]: https://docs.unity3d.com/6000.6/Documentation/Manual/resolution-scale-introduction.html
[U32]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/post-processing/hdr-output.html
[U33]: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/renderer-feature-screen-space-shadows.html
[O1]: https://github.com/CristianQiu/Unity-URP-Volumetric-Light

[P1]: https://github.com/OndrejNepozitek/Edgar-Unity
[P2]: https://docs.dungeonarchitect.dev/unity/grid-flow/gridflow-design-graph/
[P3]: https://github.com/mxgmn/WaveFunctionCollapse
[P4]: https://www.boristhebrave.com/2020/02/08/wave-function-collapse-tips-and-tricks/
[P5]: https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshSurface.html
[P6]: https://www.runicgames.com/blog/2011/05/20/making-the-world-of-torchlight-ii/
[P7]: https://discussions.unity.com/t/surface-cache-gi-preview/1720494
