# Characters and worlds

Status: implemented first pass; standalone visual review remains. Topaz supports multiple
persistent Characters and Worlds, and any Character can enter any World. This
is sequential single-player play; there is no multiplayer or concurrent
editing of a World in this feature.

## Player-facing concepts

- A **Character** is the continuing adventurer: chosen appearance, learned
  skills, equipped gear, and items being carried. All of these travel with
  the character into any World. Appearance selection starts a new Character;
  the player then chooses a World.
- A **World** is one instance of Topaz's authored regions with its own mutable
  history: day, harvested resources, built structures, storage and stashes,
  loose pickups, and completed world events. Every Character visiting it sees
  that same history. A new World starts fresh on the same authored map; it
  does not imply a procedural map or new content.
- A **Visit** is one Character's place in one World: last region and position,
  and later any genuinely personal progress tied to that World. It exists
  only for a Character-World pair that has been played. A Visit is not a copy
  of either the Character or the World. A first visit starts at the homestead.
- A **save** is the internal persistence mechanism for these records. Menus
  use Character and World names and never ask players to choose save files,
  slots, or overwrite a save to begin a new adventure.

**Continue** resumes the most recently played valid Character + World pair.
**Play** lets the player choose a Character, then an existing World or a fresh
one. **New Character** opens the appearance picker and then the World picker.
**New World** starts a fresh World for the selected Character. Newly chosen
Characters and Worlds remain drafts until **Enter World**; Back or cancel
creates neither an orphan Character nor an unwanted World. The lists use
automatic labels such as "Rogue 2" and "World 3" plus appearance, day, and
last-played details. There is no naming field in character creation; labels
are independent of stable IDs and can be renamed later if desired.

```text
Local collection
  Characters: A, B, ...
  Worlds:     1, 2, ...
  Visits:     (A,1), (A,2), (B,1), ...

Entering (A,2) uses A's current carried state,
World 2's current world state, and A's last position in World 2.
```

## Ownership of today's data

| Current field or state | Proposed owner | Reason |
| --- | --- | --- |
| Selected model, Logging XP, Swords XP, equipped tool, backpack | Character | They describe the adventurer and travel with them. |
| Day, harvested nodes, placed chest and its contents, other storage/stashes, loose pickups, claimed expedition cache | World | They are changes to that World that another Character should encounter. |
| Current region and player position | Visit | Each character can resume where they left that specific world. |
| Crafted but unplaced chest (`pendingChest`) | Character initially | It is a carried object in the current prototype; when item rules mature, represent it as an item rather than a global world flag. Placement UI state stays transient. |
| Display mode and graphics settings | Local device preferences | These are not part of any Character or World. |

All records need stable IDs independent of their display labels. A World ID
scopes authored object IDs and player-built instance state. Character IDs
remain stable when a look or label changes. One local session has one active
Character + World pair; selecting a different pair finishes pending writes
for the current pair before loading the next. A Character's carried state is
never copied into a World or duplicated per Visit. Placing or dropping an
item transfers it from the Character to the World; picking it up transfers it
back. Thus a Character can intentionally bring resources between Worlds, but
World storage cannot follow them.

An initial schema can use one collection version plus independent record IDs:

```text
Collection { version, characters[], worlds[], visits[], lastPlayedPair }
Character  { id, appearanceId, label, skills, equipment, backpack }
World      { id, label, day, nodes, structures, storage, pickups, eventFlags }
Visit      { characterId, worldId, regionId, position }
```

The profile uses ID references, not serialized Unity scene objects or copies
of mutable state. Authored item/region/appearance definitions stay in Unity
assets and are resolved by stable definition IDs at runtime. Future
character-specific progress within one World belongs in Visit only when it
truly differs by Character; shared quest outcomes and one-time rewards belong
to World by default.

## Persistence boundary

Store the three kinds of record separately **within one versioned local
collection** at first. A single atomic profile snapshot and backup preserve
their logical separation while keeping cross-boundary changes together. For
example, picking up Wood changes both World pickups and a Character backpack;
depositing Wood changes both the backpack and the World chest. Writing those
as unrelated files would need an explicit multi-file transaction or journal
to avoid lost or duplicated items after a crash. Split physical storage later
only if collection size or write cost demonstrates a need, with a transaction
design at that point. This is an implementation detail, not a menu concept.

The legacy `topaz-save.json` is one combined version-5 snapshot. Migration
first loads it through the existing validated reader (including versions
1–4), then creates one Rogue Character, one World, and their Visit with the
same progress. Keep the original file and backup until the new collection has
been validated and written successfully. Import must be idempotent: if a
valid new collection exists, do not import the old file again; an interrupted
import must not create duplicate Characters or Worlds. An unreadable legacy
file stays untouched and surfaces a visible recovery message. Editor and test
data remain isolated from the player's local collection.

The current `WorldSession` loads or creates combined state in `Awake` and
commits in `Start`. Move collection discovery to the title flow, then bind
gameplay only after an active pair is chosen. A fresh launch writes nothing
until Enter World. Preserve the current atomic-replace and backup behavior;
flush queued writes before switching Characters or Worlds. If a write fails,
show a persistent error and protect the last good collection rather than
silently switching to a new one. Measure snapshot size and write time once
multiple Worlds exist; only then consider separate physical files with a
proper transaction mechanism.

## Agreed behavior and edge cases

- Character appearance, skills, equipped gear, and carried inventory travel
  between Worlds. Placed structures, storage and stashes, dropped items,
  harvested resources, day, and world events stay in their World. There is no
  multiplayer in this plan; sharing means sequential visits by the player's
  different Characters.
- A one-time World reward claimed by one Character is unavailable to another
  Character in that World. The first Character may carry its reward to a
  different World. Future world-bound quest items need an explicit authored
  exception to the default portable-item rule.
- A first Visit begins at the homestead. Returning to a World restores that
  Character's last region and position there, while loading the World's
  latest shared state. If the saved location becomes invalid after a content
  update, use a safe arrival point without changing other progress.
- Removing a Character must never remove a World; removing a World must never
  remove a Character. Deletion is outside the first flow. When management is
  added, identify the affected Character or World clearly and provide a
  recovery path or explicit confirmation.

## Implementation and verification map

1. **Schema and repository.** Define Character, World, Visit, and Collection
   records with stable IDs, validation, and a last-played pair. Keep one
   atomic collection snapshot. Add tests for cross-record item transfers,
   missing references, backups, and interrupted writes.
2. **Legacy migration.** Convert the current combined save without deleting
   it. Cover versions 1–4, repeated startup, invalid saves, and interrupted
   conversion in focused tests.
3. **Session lifecycle.** Make title discovery read-only; initialize gameplay
   only after choosing a pair. Flush before pair switches and application
   exit. Apply Character, World, and Visit state to the existing scene objects
   without duplicating the Character's inventory per World.
4. **Player-facing selection.** Build Continue; Character list and New
   Character; World list and New World; Back/cancel; and Enter World using
   the existing uGUI/Input System patterns. Use the live appearance preview
   in [the selection plan](plans/CHARACTER_SELECTION_PLAN.md). Keep labels and
   errors about Characters and Worlds, not storage files.
5. **Game integration.** Route XP, equipment, and backpack changes to
   Character; day, nodes, structures, storage, pickups, and one-time rewards
   to World; location to Visit. Update the current crafted-but-unplaced chest
   as a portable Character item or transitional Character flag until the
   equipment/inventory feature formalizes it.
6. **End-to-end verification.** Run the scenarios below, `./scripts/verify.sh`,
   and `./scripts/build.sh windows`; inspect console/build errors and
   `git diff --check`. Provide a Mac build for the owner to review flow and
   appearance on a 60 Hz display.

## Acceptance scenarios

1. Character A collects Wood in World 1, leaves some in a chest, then enters
   fresh World 2. Only A's carried Wood and progress travel. World 2 starts
   on day one without World 1's chest, pickups, or harvested nodes.
2. Character B enters World 1 later. B has their own appearance, skills, and
   backpack, but sees World 1's current day, chest contents, harvested nodes,
   and claimed cache state. B starts at the homestead on first visit.
3. A returns to World 1 at A's last valid location; B returns to B's own
   location. Both see the latest World 1 state. Switching does not roll back
   either Character's carried progress or duplicate items.
4. Creating then cancelling a Character or World leaves the existing
   collection unchanged. Relaunch resumes the last valid pair. A version-5
   legacy file migrates to one Rogue Character and one World once, with XP,
   inventory, region, position, chest, and pickups intact.
5. Interrupting a transfer write or migration recovers the last valid
   collection or backup. An unreadable collection produces a visible error
   and is never replaced automatically.

This uses Unity serialization and Topaz's existing atomic replacement pattern.
The custom layer is limited to Topaz's ownership, migration, and item-transfer
rules; Unity does not define those game-specific boundaries. See
[Unity 6.6 serialization](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization.html)
and [Topaz's feature policy](UNITY_FEATURE_POLICY.md).
