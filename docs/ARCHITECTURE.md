# Architecture decisions

## Product boundary

Topaz is a premium single-player desktop game. The world and dungeon layouts are authored, not procedurally generated. Outdoor regions connect through designed transitions; interiors and dungeons are separate authored spaces. Mobile remains an option, but desktop performance and controls lead development.

## Visual and simulation baseline

Use the URP 3D renderer, a fixed-angle orthographic camera with zoom, stylized meshes and materials, and restrained effects. The intended day/night cycle should change ambience with a limited number of dynamic lights and shadow casters; measure a representative scene before setting exact texture, light, draw-call, or geometry budgets. Keep quality settings scalable. Do not pick HDRP solely for DLSS.

Build around readable combat with 1–10 visible enemies, not mass swarms. Use conventional GameObjects, physics, and navigation initially. Profile before introducing spatial indexes, Burst/Jobs, ECS, GPU animation, or custom draw submission.

## Player feel and first prototype

Moment-to-moment play favors responsive travel and deliberate, readable attacks. One safe homestead anchors expeditions into the authored world. The first playable study should prove movement and camera feel in a small Mac build before adding progression systems.

WASD moves relative to the screen; W moves toward the top of the view. Gamepad movement is analog. Start with one responsive travel speed and a short directional dodge, without stamina. The fixed-angle orthographic camera supports zoom and gently, within a bounded distance, looks ahead toward the aim direction while keeping the character near center. Tune speeds, dodge duration, zoom range, and camera smoothing by playtesting rather than freezing numbers in this document.

The dodge provides brief invulnerability and uses a cooldown rather than stamina. Early melee attacks sweep a readable arc toward the mouse cursor or right-stick aim, without automatic lock-on. Homestead crafting, storage, and recovery make return trips valuable, but expeditions have no forced return timer.

The first combat slice uses one sword and one enemy with a visible attack windup. A sword swing slows movement during windup and strike; dodge can cancel recovery, but not the committed swing. The central homestead is safe: threats stay in the expedition space and no mandatory raids interrupt crafting or rest.

## Boundaries for later systems

- **Content definitions:** immutable item, recipe, skill, talent, loot, and building definitions. Author in Unity assets; use stable definition IDs rather than scene object references in saves.
- **Runtime state:** plain data for character progress, inventory, world changes, structures, harvested nodes, and dungeon reset state. Keep presentation objects replaceable.
- **Persistence:** versioned local save format; stable IDs for authored objects and player-built instances; migration tests whenever format changes. Recoverable death updates the same world state rather than deleting it.
- **Regions:** asynchronously load and unload authored scenes at transitions. Persist region changes. Unloaded regions do not run full real-time simulation; apply bounded elapsed-time rules when revisited.
- **Building:** designated homestead plots, with snapping or a grid. A placement change updates local navigation and persisted state, not the entire world.
- **Progression:** classless, use-based skills with deliberate talent choices. Domain events such as a completed harvest or a confirmed hit award progress, rather than per-frame polling.

These are design contracts for future work, not systems implemented by this foundation milestone. Choose concrete schemas and gameplay numbers during representative feature slices.
