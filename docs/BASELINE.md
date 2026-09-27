# Topaz playable baseline

Topaz is a single-player fantasy survival-crafting action RPG for Windows, with Mac playtesting. URP and a freely rotating third-person camera are the current direction. Natural-fantasy Synty POLYGON art is the direction. The Storybook implementation uses owned POLYGON Starter environment assets, Sidekick characters and compatible licensed motion. See STORYBOOK_IMPLEMENTATION.md for implemented versus verified work.

## Game contract

- One seeded, finite 1024 × 1024 m wilderness streams in 128 m chunks. Worlds persist terrain identity, resources, structures, pickups and encounter deadlines. Unloaded simulation pauses; bounded elapsed-time rules resolve on return. Terrain remains immutable.
- Characters own inventory, equipment, skills, food/rest benefits, and appearance. Character–World visits own location, camp discoveries, and recovery selection. Characters can visit different Worlds.
- Retain deliberate melee, crossbows, magic, dodge invulnerability/cooldown, shield use, gathering tools, inventory, crafting, storage, equipment, and classless use-based progression. Existing numerical tuning is provisional. Starter characters carry a staff and crossbow for prototype review.
- Gentle survival: food and rest grant benefits. Empty stamina does not prevent basic actions. Rest and defeat recovery advance eight World hours; menus and unloaded play do not run real-time simulation.
- Build across suitable outdoor terrain. Only campfires provide protection, including at the starting camp; building itself does not remove threats.

## Construction and camps

The Build Journal is available throughout the wilderness. Construction snaps to a 0.75 m grid, rotates in quarter turns, and requires clear, sufficiently level terrain or supported floors. Terrain is immutable. Moving is free; removal refunds materials and drops chest contents or overflow as persistent pickups. Build material availability includes the backpack and saved chests within 30 m of the player, independently of which visuals are loaded.

Structures have World-owned stable IDs, world-space XYZ positions. Harvested resources stay suppressed while occupied by a structure; their regrowth deadlines remain intact.

Campfires cost 5 Wood and 5 Stone, protect a 12 m radius, and require another 5 m of clearance from living enemies and reserved encounters/arrivals. No fuel upkeep or upgrades. Camps provide cooking, discovery, recovery, and fast travel. Enemy spawns/entry and combat across protection boundaries are prevented. Camp removal clears destinations and restores affected recovery selections to the permanent starter fire. Tune these values in BuildingSettings.

## Deferred

Procedural dungeons, infinite expansion, terrain editing, adjustable foundations, structural collapse, raids/building damage, multiplayer, and final character art are outside this baseline. RenderingLab is a development reference, not part of normal builds. Do not infer future performance from the small prototype.
