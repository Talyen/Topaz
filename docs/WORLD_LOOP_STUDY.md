# First world loop study

This is a small, authored systems study in the Bootstrap scene. It is not an inventory, building, or day/night system for a finished game.

## Try the loop

1. Leave the blue home boundary and approach the green tree to the upper-left. Press `2` to equip the axe (`Y` cycles tools on gamepad). Aim with the mouse or right stick; click or pull the right trigger three times. Wood and Logging XP arrive only after the third chop.
2. Return to the wooden workbench inside the home boundary. Press `E` (`X` on gamepad) and craft the chest for three Wood.
3. Aim at a clear spot inside the home boundary and click (`A` on gamepad) to place the chest. `Esc` or `B` cancels placement without losing the crafted chest.
4. Approach the chest and press `E`/`X` to deposit or withdraw Wood. Approach the bedroll and press `E`/`X` to advance a day. The tree returns on day four after a day-one harvest.

The HUD reports the day, backpack Wood, Logging XP and level, equipped tool, and nearby interaction. The combat study sword and enemy remain available.

## Data boundaries

- Unity assets hold immutable Wood, tree, chest, recipe, and axe definitions with stable IDs. Scene objects and save records hold mutable state.
- The authored tree has a stable object ID. The placed chest receives an instance ID. The save records day, position, tool, backpack, partial tree chops and regrowth, pending chest, and placed chest contents.
- One versioned JSON save lives in Unity's persistent data directory (`topaz-save.json`). A replacement keeps a `.bak` copy. An unsupported or unreadable save disables writes rather than overwriting it. Editor sessions, including tests, use isolated temporary directories so they cannot change the standalone player's save.
- Resting advances an abstract day for regrowth; there is no simulated time of day, hunger, or unloaded-region simulation here. Building is limited to one chest and one home plot so we can review feel and clarity before generalizing.

Run `./scripts/verify.sh` and `./scripts/build.sh windows` after changes. The Mac build is for a 60 Hz smoothness playtest; it does not establish the later Windows 120 FPS target.
