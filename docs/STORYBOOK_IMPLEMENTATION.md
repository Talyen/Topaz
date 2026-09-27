# Storybook wilderness implementation checkpoint

Approved direction: cozy storybook, deliberate combat, one finite 1024 m continuous
world with 128 m streaming chunks. Owned POLYGON Starter and Sidekick Starter;
compatible licensed motion is allowed, no purchases. Only campfire protection;
construction uses backpack and saved chests within 30 m. No legacy save migration.

## Implemented foundation

- Deterministic world-space elevation, moisture, woodland clusters, resource IDs,
  discovery pads and independent decoration sampling; connected Unity Terrain chunks.
- A 3 x 3 detailed neighborhood, surrounding height-data preload ring, distant terrain
  and tree silhouettes, budgeted decoration activation, asynchronous local navigation.
  Distant trees use the same variant, rotation and scale streams as detailed trees.
- WorldSession owns persistent transactions. Coordinate chunk IDs and world-space
  records replace the two-scene layout. Retired trail markers remain only in source
  authoring scenes; runtime scene-transition paths have been removed.
- Saved chest materials are indexed within 30 m independently of loaded visuals.
  Structures and dropped loot release distant scene objects while retaining records.
  Resource/cache deadlines and defeated-enemy state survive unload/reload.
- Initial arrival and camp travel wait behind a fade for terrain and navigation.
  Arrival checks reject obstructed positions. Loading iterator exceptions become
  persistent retry feedback; failed destinations do not commit a new visit location.
- Protection is 12 m at every campfire, including First Hearth. Entering protection
  does not heal. Recovery, camp removal, saved destinations and talents use camps.
- Fresh Storybook-v2 save collections and generator version 2; previous collections
  remain untouched and are not migrated.

## Presentation checkpoint

- Expanded owned Synty trees, shrubs, ferns, ground cover, rocks, modular discoveries,
  building assemblies and equipment. Ground cover clears around resources/buildings.
- Warm sun, blue-green ambient shadows, moonlight, fog and restrained post-processing;
  bounded foliage deformation and camp embers. Terrain details use instance-count
  scatter mode, readable source geometry and instanced wind materials.
- Corrected the Terrain Lit material's explicit instanced-normal shader keyword after
  an Editor/player lighting discrepancy. Latest player parity still needs visual review.
- Sidekick knight, humanoid Animator locomotion, directional dodge, tools, weapons,
  guard, hit and death; gameplay retains movement/damage/invulnerability authority.
  Foot IK and retargeted motion still need a complete visual/contact review.
- Selected illustrated Journal retained; ink-on-paper rows, shared focus treatment,
  headings, costs, nearby-storage explanation and short unscaled panel transitions.
  Owner visual approval is outstanding.
- Existing licensed sound library supplies footsteps and woodland/weather ambience
  through existing audio controls. Derived clips are reproducible; originals remain
  untouched. Auditory review is outstanding.

## Verification evidence

- Full gate passed on 2026-09-26: run `20260927T022204.667740Z-64374eca`,
  32 EditMode tests, 107 PlayMode tests and Mac build. UTC run IDs cross midnight.
- The Editor suite includes 100-seed determinism, matching terrain edges, stable
  resources and discovery approach slope checks. These do not prove collision-level
  navigation for every seed.
- Fourteen continuous-world camp scenarios passed, including actual gamepad boundary
  crossing, harvesting/unloading/returning, independent caches, saved nearby storage,
  built-camp travel, missing-camp fallback, and recovery at an unloaded built camp.
- After that gate, the new loot-streaming regression and character/terrain checks
  passed: six tests in `20260927T022637.773710Z-91727b95`.
- Earlier Mac smoke completed gathering, cooking, building, camp protection/travel
  and reload without runtime errors. The latest standalone visual run could not be
  completed because the Mac was locked. Do not treat earlier captures as final art.
- Earlier short stationary 1600x900 samples averaged 16.67 ms; they do not establish
  traversal frame pacing, memory stability, or Windows performance.

Logs, JUnit reports and captures are ignored under TestResults/Storybook and
TestResults/runs. Run `./scripts/verify.sh` after project changes; Windows handoff uses
`./scripts/build.sh windows`. Exact latest build receipts are in TestResults/runs.

## Still required to complete the approved plan

1. Unlock the Mac and review the latest standalone lighting/grass/Journal checkpoint
   at daylight, dusk, night and rain before extending the art treatment further.
2. Complete character appearance variety (the six current IDs share one Sidekick
   knight), remaining prop fallbacks, animation contact/grip/death/foot sliding,
   enemy role silhouettes, camera impact feedback, and auditory review.
3. Exercise every weapon/tool and all screens with keyboard/mouse and gamepad;
   visually check 1280x720, 1600x900 and native resolution at 100/125/150% UI scale.
4. Measure repeated traversal circuits, activation spikes, memory growth and ten-enemy
   combat in a standalone build; profile and fix measured stalls. Test enemy pursuit
   and constructions across borders, and inject loading failures to validate rollback.
5. Complete the latest full gate and Windows build, capture approved views, and
   deliver the reviewed Mac build. Windows 1080p/120 remains unmeasured.

This is an implementation checkpoint, not completion of the full polish plan.
All original audio archives, unfinished curation and pre-existing checkout work are
preserved. Restricted vendor dependencies remain private with acquisition manifests.
