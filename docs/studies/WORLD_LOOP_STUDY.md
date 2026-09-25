# First world loop study

This is a small, authored systems study in the Bootstrap scene. The [day and night cycle](../DAY_NIGHT_CYCLE.md) now supplies its world clock and tree regrowth timing; inventory and building remain limited studies.

## Try the loop

1. Leave the blue home boundary and approach the tree to the upper-left. Press `E` (`X` on gamepad) three separate times to chop it. Each press temporarily draws the Logging Axe, aims at the tree, performs one timed chop, and restores your journal-selected weapon or tool. Completing the tree earns Logging XP even if gear or a talent reduces the chops needed. The final chop drops Wood on the ground; walk near it to collect it.
2. Return to the wooden workbench inside the home boundary. Press `E` (`X` on gamepad) and craft the chest for three Wood.
3. Aim at a clear spot inside the home boundary and click (`A` on gamepad) to place the chest. `Esc` or `B` cancels placement without losing the crafted chest.
4. Approach the chest and press `E`/`X` to deposit or withdraw Wood. The bedroll can be used anytime and skips eight in-game hours. The tree returns 72 in-game hours after harvest, whether time passes through play, rest, or both.
5. Press `B` (`Start` on gamepad) for the backpack. It has 16 fixed slots. The chest has 12 slots. Wood stacks to 20 per slot, with no weight limit. If there is insufficient space, collected amounts fill available slots and the remainder stays as a visible world drop.

Exploration shows one nearby interaction chip instead of a permanent controls HUD. Day, Wood, skills, and equipment remain available through the backpack and related menus. The combat study sword and enemy remain available.

The [first expedition clearing](EXPEDITION_STUDY.md) adds a player-chosen trip through two scouts and a wide-sweep guardian. Its one-time Wood cache feeds this same chest recipe, inventory, and save.
The [mining and home building slice](MINING_AND_HOME_BUILDING_STUDY.md) adds Stone, Iron, and repeatable home builds alongside this original study.

## Data boundaries

- Unity assets hold immutable Wood, tree, chest, recipe, and axe definitions with stable IDs. Scene objects and save records hold mutable state.
- The authored tree has a stable object ID. The placed chest receives an instance ID. The current collection stores elapsed World time, partial tree chops and absolute regrowth deadlines on the World; appearance, skills, tool, backpack, and a crafted but unplaced chest on the Character; and region and position on their Visit. The old rest-day field remains only for legacy migration.
- `topaz-collection.json` in Unity's persistent data directory contains separate Character, World, and Visit records in one atomic snapshot with a `.bak` copy. The previous `topaz-save.json` (versions 1–5) migrates to a Rogue Character, one World, and their Visit without deleting the old file. An unreadable collection or unsupported legacy file protects existing data and disables writes. Editor sessions and tests use isolated temporary directories. Gameplay writes are queued and coalesced; pair switches, pause, and quit flush the queue. See [Characters and Worlds](../CHARACTERS_AND_WORLDS.md).
- Time advances during active play, pauses for menus and scene loading, and does not pass while the game is closed. Resting skips eight in-game hours. Tree regrowth uses elapsed world time, including those skips. Building is limited to one chest and one home plot so we can review feel and clarity before generalizing.

Run `./scripts/verify.sh` and `./scripts/build.sh windows` after changes. The Mac build is for a 60 Hz smoothness playtest; it does not establish the later Windows 120 FPS target.

## Unity references

[ScriptableObject](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ScriptableObject.html) and [Unity 6.6 serialization](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization.html) explain the authored definitions and serializable save fields. [AI Navigation's NavMesh Obstacle](https://docs.unity3d.com/Packages/com.unity.ai.navigation@2.0/manual/NavMeshObstacle.html) covers the tree's dynamic obstacle. The [reference index](../UNITY_REFERENCE_GUIDE.md) points to the newer dictionary serialization guidance; it does not replace Topaz's stable IDs or save migrations.
