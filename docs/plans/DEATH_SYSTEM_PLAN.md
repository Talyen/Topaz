# Defeat and Campfire recovery design

Status: agreed gameplay rules; implementation and feel review remain. This is a design note, not a request to change gameplay code.

## Player experience

- Every World has a home Campfire. It is the default recovery point for a Character's first Visit to that World. Later Campfires are both recovery points and fast travel destinations.
- A Campfire activates automatically when the Character enters a clearly visible hearth area. No button press or rest is required. A brief flame response and compact confirmation make the new return point and travel unlock clear. Passing near the fire outside that area does nothing. Tune the area's size in a Mac build.
- The most recently activated Campfire in the current Character–World Visit is the return point. Re-entering an already discovered Campfire changes the return point back to it. Arriving by future fast travel counts as a visit and also makes that Campfire the return point.
- At zero health, the Character is defeated and later recovers at the Campfire. The presentation should imply collapse and recovery rather than literal death and resurrection. No defeat screen or input prompt is needed.
- Proposed transition: stop control and combat actions, show a brief collapse or hit beat, fade out with a restrained ember or fire sound, move to the Campfire's safe arrival spot while the view is covered, then fade in and return control. Aim for roughly 1–2 seconds in ordinary cases; keep the screen covered through any scene load. Reuse the existing short rest fade where it fits, then tune the timing and sound in a Mac build.
- The Character returns with full health and harmful effects cleared. Clear attack, dodge, projectile, and enemy targeting state that could cause an immediate repeat defeat. The safe arrival spot must be walkable and outside enemy reach; provide brief arrival protection if needed during the fade.

## Consequences and world state

- Defeat does not drop or remove inventory, equipped gear, experience, gold, or earned progression. Collected loot, claimed caches, structures, harvested resources, and completed world events remain changed. There is no corpse run or recoverable currency pile.
- Already spent consumables and ammunition remain spent. Defeat does not roll back actions taken since the last Campfire.
- Recovery advances the authoritative World clock by exactly **eight in-game hours**, the same amount as resting. Apply the same elapsed-time consequences, such as tree regrowth and future weather schedule advancement, without replaying skipped hours visually. Time does not advance during the fade or scene load in addition to that jump.
- Ordinary enemies reset to their authored spawn or patrol state, including health and combat state, when the player recovers. Enemy resets must not reset persistent World rewards or state. Story bosses marked as permanently defeated remain defeated after their completion is recorded; an encounter that should repeat needs an explicit authored rule.
- Using defeat as a shortcut back to the Campfire is acceptable. A voluntary return or Campfire teleport can be added with the later world map and fast travel feature.

## State and integration boundaries

| Owner | Responsibility |
| --- | --- |
| Authored Campfire | Stable ID, activation area, visible response, and a safe arrival transform. The home fire is the fallback for missing or invalid saved Campfire IDs. |
| Visit | Last activated Campfire ID and discovered travel Campfire IDs for one Character in one World. A first Visit starts at the home fire. The current region and position continue to belong to the Visit. |
| World | World clock, rewards, bosses, resources, and other shared persistent changes. The authored Campfires exist in this World, while each Character discovers them independently. |
| Character | Health and harmful effects in play, carried inventory, equipment, skills, XP, and gold. Recovery restores health and clears effects without replacing the Character record. |
| Recovery coordinator | Runs a single transition, advances the clock once, moves to the safe arrival point, resets ordinary encounters, updates the Visit, and commits the resulting state. It must ignore additional hits while recovery is running. |

Travel discovery is a Character's knowledge of a particular World, so discovered Campfire IDs belong to the Visit. A newly created Character does not inherit another Character's discovered travel network merely because they enter the same World. The return point also belongs to the Visit.

The current `PlayerVitality` and `WorldSession` contain a temporary practice defeat that returns to home and refills health. Replace that path when this feature is implemented. Use the existing versioned profile and atomic write pattern; never reload a previous save as the recovery mechanism. Validate saved Campfire IDs against current authored content and fall back to the home fire after content changes.

## Acceptance checks for implementation

1. First defeat in a fresh World recovers at its home Campfire with full health, cleared harmful effects, and the World clock advanced exactly eight hours.
2. Entering a second Campfire's visible activation area automatically unlocks it and makes it the return point. Walking past outside the area does neither. Re-entering home changes the return point back.
3. Defeat after gathering, gaining XP, spending a consumable, claiming a cache, or spending gold preserves each resulting state. Save and reload after recovery preserve the same Character, World, and Visit records.
4. Ordinary enemies start again at their authored spawn or patrol state. A permanently defeated story boss and its claimed reward stay completed. Repeated recovery does not duplicate one-time rewards.
5. Defeat in another loaded region covers the load with the fade and arrives at a safe Campfire position. A missing saved Campfire falls back to home without losing unrelated progress.
6. Recovery applies the same eight-hour world-time effects as rest exactly once. It neither advances clock time during the covered load nor replays intermediate day/night or weather transitions.
7. In a Mac build on a 60 Hz display, the transition reads as defeat and recovery without feeling instant or interrupting control for longer than needed. Run project verification, the Windows compatibility build, and `git diff --check` before handoff.
