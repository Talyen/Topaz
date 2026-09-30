# Roadmap and current status

Reviewed 2026-09-30. [BASELINE.md](BASELINE.md) owns game rules; this page owns implementation status and remaining work. Implementation, passing checks, agent visual review and owner acceptance are separate. The owner considers the current presentation unacceptable prototype quality.

## Implemented foundation

| Area | Current capability | Reference |
| --- | --- | --- |
| World | Persistent eight-area graph, six procedural recipes, paired interaction passages and whole-area terrain residency | [World reference](PROCEDURAL_WORLDS.md) |
| Player/combat | Fixed isometric-style perspective camera, independent cursor/twin-stick aim, dodge, melee/shield/crossbow/staff and prototype enemies | [Baseline](BASELINE.md) |
| Gathering/building/camps | Tools, crafting/storage, terrain-aware construction, camp protection/cooking/recovery/travel | [Baseline](BASELINE.md) |
| Shelter/visibility | Construction-based rain exposure and environmental fill; local scenery cutaways, roof hiding and engaged-character silhouettes preserve collision, shelter and shadows | [World reference](PROCEDURAL_WORLDS.md#spatial-shelter) |
| Persistence/survival | Character/World/Visit ownership, recoverable saves, area-qualified state, food/rest benefits, clock/weather and deadlines | [Architecture](ARCHITECTURE.md) |
| Presentation | Golden / Silver lighting, native Surface Cache GI, dense grass, native pine mesh LODs, analytic atmosphere, cinematic DoF and graphics controls | [World reference](PROCEDURAL_WORLDS.md#clock-weather-and-rendering) |
| UI/audio | Journal and desktop flows, contextual interactions, ten Viking appearances, two ambience beds and eight derived footsteps; weapon/spell cues await sound selection | [UI reference](UI_DESIGN_SYSTEM.md); [audio policy](AUDIO_ASSET_POLICY.md) |

The supported collection is `Areas-v2`, with profile schema 4, graph version 2 and terrain generator version 8. Earlier collections remain intact. Forest basins and river terraces now use distinct route/landform recipes, shorter scenic exclusions, angle-varying enclosure and stronger passage framing. These capabilities do not establish enjoyable pacing or visual acceptance.

## Next work

The owner-supplied [visual and performance plan](plans/Unity_Visual_Performance_CoDevelopment_Plan_v4_2026.md) guides bounded experiments. The baseline retains product authority and the [workflow](AGENT_WORKFLOW.md#iterate-and-hand-off) governs proportional validation. Techniques and proposed settings are hypotheses, not a feature checklist.

1. **Select and implement a URP visual overhaul.** The owner has opened terrain treatment, lighting, shaders, post-processing and character rendering to substantial replacement while retaining Synty assets and URP. Twelve isolated Unity comparison recipes now share a fully generated environment texture set across fourteen unique asset groups, with overview/detail captures, the working multi-view character bake and a separate walkable twelve-look review scene; [projection workflow and findings](AI_TEXTURE_PROJECTION.md) records their limits. Choose a few distinct directions, then carry them through the same playable generated journey. These samples are not owner acceptance or an 8/10 result. **Improve the bounded-area journey.** Refine forest-basin → river-terrace → return through terrain/prop integration, lighting hierarchy, character grounding and animation, camera response, combat/gathering feedback and active audio. Inspect briefly, implement a substantial pass, then play and refine. Review three meaningful beats, passage variety and natural boundaries from the fixed gameplay camera; route length alone does not prove the 4–8 minute first-exploration target. Extend that quality across the eight-area graph. Keep title/creator design separate from the outdoor pass.
2. **Present a coherent completed batch.** Use a representative playable Mac result when consequential judgment is needed. Ordinary refinements within the established direction proceed autonomously. Camera composition, cutaway edges, extreme terrain, full interiors/bridges/combat and foreground foot contact still need review. Report specific weaknesses and retain rollback; do not require every weapon/display/input combination for a local improvement.
3. **Harden demonstrated risks.** Validate area-qualified saves/storage, camp recovery, encounter retreat and bounded residency over repeated journeys when changing those systems. Investigate the world-data entry timeout and intermittent JobTempAlloc warning. Foreground populated 1440p/60 measurement remains necessary for certification, but does not block independent visual/feel work. Run Windows compatibility checks at the next relevant integration checkpoint.
4. **Add later content after the outdoor loop is enjoyable.** Agree the first procedural dungeon scope before introducing its framework, then broaden crafting/progression and production content.

## Retained evidence and open acceptance

These are results on earlier inputs, not a fresh gate for the current dirty checkout. Consult receipts and input freshness before reusing them. Detailed captures, experiments and test outputs belong in ignored managed artifacts; this page retains only evidence needed for current decisions.

| Retained result | What it establishes and what remains open |
| --- | --- |
| Bounded-area generation | The v2 refinement recorded representative checks, 100 seeds / 800 plans and realization/retirement of all eight areas. Forest/river route networks total about 573/552 m including branches and exits. Meaningful discovery pacing and first-exploration duration remain unverified. |
| Area travel/state integration | Earlier v1 focused checks covered storage isolation, paired exits, save round-trips, departure/return, paused damage and destination-failure reconstruction. A seed-42 standalone forest → river → return diagnostic reported zero runtime errors, four resident terrain cells and approximately 1.5-second transitions including fades. This is instrumented loading evidence, not a general guarantee or repeated-travel memory certification. |
| Fixed camera/actions | Retained action tests passed camera-heading, elevated-bolt obstruction and fall/teleport checks. All eight standalone seed-42 controls passed, including dodge, construction input ownership, cursor targeting and committed staff facing. Day/night explicit renders reported zero runtime errors; the route scan found no terrain obstruction. Extreme terrain cutaway and full interior/bridge/combat coverage remain open. |
| Movement test failure | All six MovementInputTests failed their forty-second world-data preparation deadline before movement assertions. Isolated worker preparation/validation completing in about fourteen seconds does not resolve the connected Play Mode failure. Earlier lantern/overlapping forest-condition checks also remain incomplete. |
| Short M5 graphics controls | Earlier 2560×1440 samples recorded mean GPU times of 37.7 ms for native High, 21.1 ms for High/adaptive STP and 15.1 ms for Balanced/adaptive STP at 75–85% internal scale. Balanced/STP became the Mac starting preset, preserving later explicit preferences. Its roughly 34 ms p95 frame interval still missed steady 60 FPS; none of those short controls completed a circuit. |
| Fixed-camera timing sample | A later High/STP 2560×1440 sample traversed about 163 m in thirty seconds with zero errors/readiness stops and 23.75 ms mean GPU time. Its recorded scale stayed at 1.0, focus was lost and it exercised retained movement-driven terrain streaming. It certifies neither foreground 60 FPS nor current bounded-area performance. |

**Remaining acceptance:** ordinary populated gameplay on the owner's M5 MacBook at 2560×1440/60 or above, including one cold plus three warm circuits and combat/construction/weather coverage. Gameplay, loading and repeated-travel residency are separate measurements. The future Windows baseline is RTX 4060 plus a mid-range six-core CPU; exact hardware is unselected and Windows device performance is deferred. Owner aesthetic acceptance remains open.

Severe flickering reported during automated traversal remains unresolved and is deprioritized at the owner's request; a clean slow-pan clip does not close it. Native GPU occlusion remains disabled after severe Metal hitches. Native Terrain instancing remains disabled after black-terrain standalone controls; grass and ordinary meshes still instance. These restrictions and native GI acceptance limits live in [feature policy](UNITY_FEATURE_POLICY.md#standalone-rendering-constraints).

## Current review outputs

The owner-review app is `Builds/Topaz.app`, with `Builds/Topaz.build-receipt.json`. Its retained fixed-camera build receipt reports success with zero errors and 59 warnings. New review work uses this root app rather than creating another review directory. The camera review is under `TestResults/artifacts/isometric-camera/final-review/`.

The Windows cross-build is `Builds/Topaz.exe`, adjacent `Topaz_Data` and `Builds/Topaz-windows.build-receipt.json`. Its v7 terrain/rock-grounding checkpoint reports success with zero errors and 65 warnings; it predates the later camera and other refinements and does not cover their compatibility. Windows runtime/performance remains unverified.

Older review directories are historical local evidence, not the current launch target. Unresolved flicker/graphics experiments remain under `TestResults/artifacts/luminous-alpine-v5/`; allocation diagnostics remain under `TestResults/Fantasy/`. Preserve pinned evidence. These ignored outputs may be absent in a fresh clone. Use `./scripts/doctor.sh` when retained evidence needs diagnosis, and the [world diagnostics](PROCEDURAL_WORLDS.md#diagnostics-and-authoring) for reproduction.

The isolated twelve-look visual-review app is `Builds/reviews/visual-lab/TopazVisualLab.app`; its generated scene is `Assets/Topaz/ReviewContent/VisualReview.unity`. The Mac review build passed with zero errors; the selector was inspected in Play mode and the standalone app. A separate Windows cross-build passed with zero errors; Windows runtime/performance and physical gamepad review remain unverified. Neither result establishes artistic acceptance or production rendering performance.
