# Topaz playable baseline

Topaz is a single-player fantasy survival-crafting action RPG for Windows, with Mac playtesting. URP and a freely rotating third-person camera are the current direction. Higher-fidelity natural environments are combined with neutral prototype character, equipment, and furniture visuals until replacement art is selected.

## Game contract

- Seeded, finite outdoor regions connect through loading transitions. Worlds persist their seed, generator version, settings, resources, structures, pickups, and encounter deadlines. Unloaded regions pause; bounded elapsed-time rules resolve on return.
- Characters own inventory, equipment, skills, food/rest benefits, and appearance. Character–World visits own location, camp discoveries, and recovery selection. Characters can visit different Worlds.
- Retain deliberate melee, crossbows, magic, dodge invulnerability/cooldown, shield use, gathering tools, inventory, crafting, storage, equipment, and classless use-based progression. Existing numerical tuning is provisional. Starter characters carry a staff and crossbow for prototype review.
- Gentle survival: food and rest grant benefits. Empty stamina does not prevent basic actions. Rest and defeat recovery advance eight World hours; menus and unloaded play do not run real-time simulation.
- Build across suitable outdoor terrain. The starter region remains safe. Outside it, player camps create protected areas; building itself does not remove threats.

## Construction and camps

The Build Journal is available in either outdoor region. Construction snaps to a 0.75 m grid, rotates in quarter turns, and requires clear, sufficiently level terrain or supported floors. Terrain is immutable. Moving is free; removal refunds materials and drops chest contents or overflow as persistent pickups. Build material availability includes the backpack and chests in the active region only.

Structures have World-owned stable IDs, region IDs, and region-local XYZ positions. Harvested resources stay suppressed while occupied by a structure; their regrowth deadlines remain intact.

Campfires cost 5 Wood and 5 Stone, protect a 12 m radius, and require another 5 m of clearance from living enemies and reserved encounters/arrivals. No fuel upkeep or upgrades. Camps provide cooking, discovery, recovery, and fast travel. Enemy spawns/entry and combat across protection boundaries are prevented. Camp removal clears destinations and restores affected recovery selections to the permanent starter fire. Tune these values in BuildingSettings.

## Deferred

Procedural dungeons, seamless streaming, terrain editing, adjustable foundations, structural collapse, raids/building damage, multiplayer, and final character art are outside this baseline. RenderingLab is a development reference, not part of normal builds. Do not infer future performance from the small prototype.
