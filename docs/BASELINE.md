# Topaz playable baseline

Topaz is a single-player fantasy survival-crafting action RPG for Windows, with Mac playtesting. The direction is luminous natural fantasy: Synty POLYGON forms, pine and moss greens, cool readable shadows, selective golden sunlight, organic terrain, warm refuges and readable characters. Enshrouded informs luminosity, The Witcher 3 informs landscape composition, and Crimson Desert informs scale; the geometry remains deliberately stylized. Player-tracked cinematic depth of field is intentional and remains enabled by default. Golden / Silver lighting is the default; Natural remains selectable. Surface Cache Global Illumination is the always-on diffuse lighting direction; Balanced/High scale its quality without a player-facing GI toggle. Adoption remains subject to the visual and performance gates in the roadmap. [ROADMAP.md](ROADMAP.md) owns implementation status and acceptance; this page owns game rules. Performance targets live in [PERFORMANCE.md](PERFORMANCE.md).

## Current quality priority

The owner considers the current presentation unacceptable prototype quality. Improve visual cohesion and enjoyable moment-to-moment play before routine certification or broader content. The intended direction above is not approval of the current result. Judge the normal gameplay camera and interactions: coherent composition/materials, readable lighting and silhouettes, grounded animation, responsive camera/input and satisfying action feedback. Existing mechanics and numerical rules below remain authoritative. [Workflow](AGENT_WORKFLOW.md#iterate-and-hand-off) governs proportional checks.

## World and play

- New worlds use one seeded, finite **2048 × 2048 m** wilderness. Terrain streams in **128 m** chunks and remains immutable during play.
- The wilderness includes seeded Viking, Alpine and goblin destinations plus two visitable skyline anchors: Titan's Grave and Split Sky Peak. Quiet discoveries alternate with occupied sites; optional landmarks yield when a safe, legible placement is unavailable.
- The starting biome is Viking Alpine: dense pine forests, nearly continuous lush Alpine/PNB grass outside narrow trails and occupied camp spaces, rocky highlands, downhill rivers, trail loops and scattered abandoned Viking sites. Forest and mountain enclosure dominate; open vistas are occasional framed reveals. Low-lying mist supports depth between tree groups. Smooth distance haze conceals the outer draw limit by 350 m; nearby cover stays lush while distant grass thins gradually. Required routes cross water at bridges or shallow fords; deeper water blocks walking. Unloaded simulation pauses; bounded elapsed-world-time rules resolve on return.
- Characters can visit different Worlds. Saved ownership and supported-version policy are defined in [architecture](ARCHITECTURE.md#saves-and-state).
- Screen-relative WASD and analog gamepad movement use one travel speed; mouse/right stick rotates a collision-aware third-person camera. A short directional dodge has brief invulnerability and a cooldown, without a stamina requirement. Melee uses an aimed arc without lock-on.
- Combat includes melee, shields, crossbows and magic; gathering tools, inventory, crafting, storage, equipment and classless use-based progression support the outdoor loop. Most encounters are one coherent group of **1–3 enemies**, without a hard global visible-enemy cap. Numerical tuning remains provisional; prototype characters start with a staff and crossbow alongside starter melee/tool/shield equipment.
- Gentle survival: food and rest grant benefits. Empty stamina does not prevent basic actions. Night remains navigable and combat-readable without a lantern. Every character carries a permanent lantern, enabled by default; the saved manual toggle remains authoritative. Its warm 8.5 m light fades down from 10:00–11:00, stays suppressed through 14:00, and returns fully by 15:00 without changing that toggle.

Character creation offers ten cosmetic Viking looks: Villager I/II and Warrior I/II, each with male/female versions, plus male/female Chieftains. Appearance does not define a class.

## Construction and camps

Build throughout suitable wilderness terrain. The Build Journal uses a **0.75 m grid**, quarter-turn rotation, and clear, sufficiently level terrain or supported floors. Moving is free; removal refunds materials and drops chest contents or overflow as persistent pickups. Material availability includes the backpack and saved chests within **30 m**, independently of loaded visuals.

The Viking kit adds timber floors, log half-walls, pillars, fences, benches, chairs, shelves and weapon racks. Furniture additions are decorative; existing storage, rest, camp and crafting interactions retain their rules.

Structures have stable World-owned IDs and world-space positions. They suppress occupied resource regrowth without discarding its deadline. Roofs and walls change rain exposure and environmental fill only; shelter does not grant protection, recovery benefits or character statistics.

Only campfires provide protection, including First Hearth. They cost **5 Wood and 5 Stone**, protect a **12 m radius**, and require another **5 m clearance** from living enemies and reserved encounters/arrivals. There is no fuel upkeep or upgrade system. Camps provide cooking, discovery, recovery and fast travel; entering protection does not heal. Enemy entry/spawns and combat across protection boundaries are prevented.

The builder discovers a newly placed camp; other Characters discover it when visiting. Removal clears matching visit discoveries and redirects affected recovery selections to the permanent starter fire. Tune construction/camp values in BuildingSettings.

## Clock and survival deadlines

A day is **45 minutes of active play**; new worlds begin at **08:00**. Menus, initial loading, travel, rest/recovery transitions and application pause stop ordinary advancement. Closing the game adds no elapsed time. Rest and defeat recovery advance exactly **eight World hours**, behind the existing transition.

Deadlines use monotonic elapsed World hours, not dawn counts or loaded-object lifetimes. Current tree regrowth is **72 World hours** from its HarvestDefinition; enemy return is **24 hours** and cache refill **72 hours**. Nine eight-hour rests span the tree interval. Construction suppression retains the deadline.

No clock/day-counter HUD is required. Environment and ambience communicate time; carried lights and fires add warmth and local detail. Clock authority and presentation integration are described in [the world reference](PROCEDURAL_WORLDS.md#clock-weather-and-rendering).

## Deferred

Procedural dungeons, infinite expansion, swimming/deep-water hazards, terrain editing, adjustable foundations, structural collapse, raids/building damage, multiplayer, advanced reflections and volumetrics are outside this slice. Mobile remains a future platform option. Broader progression and additional biomes remain follow-up work.
