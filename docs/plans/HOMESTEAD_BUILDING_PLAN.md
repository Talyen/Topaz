# Broader homestead building

Status: first implementation for Mac owner review. The annotated [layout and interaction plate](../design/homestead-building.svg) is a design reference, not a screenshot or a promise of final art. The first timber geometry needs an in-player composition and feel review.

## Player loop

The Home region is safe throughout. The campfire's building boundary is shown only while building or editing; it does not determine whether an enemy can attack. A new World begins with the current 5.5 m build circle. The player can interact with the home campfire and confirm a 9 Wood + 6 Stone upgrade to open roughly 9 m, then a 24 Wood + 24 Stone + 8 Iron upgrade to build across the authored Home ground area. The campfire gains a larger stone ring at each stage. Entrances, arrival, workbench, and gear rack retain clear access.

Open Backpack and choose **Home** from anywhere in the Home region. The page lists each known build, its cost, and available Wood, Stone, and Iron. Select a build to return to the world with a snapped ghost; rotate with `R` or right-stick press, confirm with the existing Place action, or cancel. Move and Remove use the same aim-and-confirm view. World time continues while aiming and editing. Material costs are paid only on confirmed placement, from the backpack and all placed home chests. Repositioning is free. Removing returns the full recipe; overflow becomes persistent pickups. A removed chest also drops its contents as persistent pickups, including unknown saved item IDs.

The final build boundary is authored from the Home ground tiles rather than the combat NavMesh. The first structural kit uses 1.5 m floor modules on the existing 0.75 m placement grid: Stone floors, timber walls, automatically opening doorways, and individually placed low roof tiles. Walls initially attach to floor edges and roofs initially need a nearby wall. Moving or removing support afterward leaves other pieces in place. Floors, walls, and roofs can form multiple one-story footprints; there is no arbitrary building or chest count. Roof tiles hide near the player to keep the fixed camera readable. Timber windows wait for matching art.

The starter Bedroll becomes World-owned and movable. Any placed Bedroll or Bed rests for the current eight hours; neither requires a roof or gives an extra bonus. Each chest retains its own 12-slot contents. Beds, chests, anvils, lanterns, tables, and paths can be placed indoors or outdoors. Lanterns light automatically from dusk. The existing Anvil offers three known, immediate recipes: Forged Sword (+1 Attack over starter), Iron Helm (+1 Armor), and Reinforced Axe (+1 Logging). Forged items are reliable baseline upgrades; expedition loot remains free to offer different strengths.

Available trees and rocks block construction. After harvest, a structure may occupy their site; it suppresses their visible regrowth until moved, without deleting the elapsed World-time deadline or progress. Removing the structure reveals a resource that is already due to return.

## Data and Unity boundaries

`TopazProfileData` version 10 stores campfire tier and all placed builds on the World. Structure records add quarter-turn rotation; legacy chest contents, paths, anvils, pickups, and character inventory migrate unchanged. The starting Bedroll is materialized once at its authored position for each World. Stable IDs and inventory slots remain plain save data. The three forged item definitions are Unity `ScriptableObject` assets. The furniture visuals are Topaz-owned prefabs around active, unmarked KayKit models. The first timber and roof kit is built with ordinary GameObjects, renderers, colliders, and small Topaz placement rules. The UI uses the installed uGUI/TMP Canvas and Input System action asset; no new package is needed.

The current first pass uses simple code-native structural geometry so shape and controls can be judged before a final art pass. Convert the chosen structural geometry to replaceable owned prefabs after the Mac composition review. Revisit NavMesh obstacle setup only if future Home NPCs or pets need paths through player-built rooms; the Home is enemy-free now.

## Review

- In a new World, build a small room, place an indoor Bed and chest, then build a second odd-shaped structure. Check ghost validity, rotation, doorway passage, roof visibility, and the 60 Hz Mac camera at near and far zoom.
- Upgrade the campfire twice, check the boundary only appears during editing, and verify travel markers and recovery arrival remain reachable. Compare daytime and nighttime lantern readability.
- Fill and move a chest, remove it, collect its dropped contents, then reload. Verify old saves retain the original chest and all materials. Rest at a moved Bedroll and a built Bed; each skips eight World hours.
- Forge each item with a nearly full backpack and from materials held in different chests. An unsuccessful action must leave every material and item untouched.
- Run `./scripts/verify.sh`, `./scripts/build.sh windows`, `./scripts/build.sh mac`, inspect logs, and run `git diff --check`. Accept the Mac visual review only after the player can enter the World without Console errors.
