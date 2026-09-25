# Mining and home building study

Status: first playable implementation built; Mac visual and feel review remains. The Blacksmith's Anvil is buildable but has no crafting interaction until weapon and armor recipes ship.

## Player loop

- New and migrated Characters own a Pickaxe alongside the renamed Logging Axe. The equipment journal chooses the manually held weapon or tool. Attack near a tree or rock briefly draws the required tool, aims the swing at the nearest available node, and restores the prior selection afterward. A living enemy in melee range keeps attack in combat mode. Trees and rocks have no in-world E prompt; station, travel, and chest prompts remain.
- All authored forest tree and rock scenery in home and the clearing now uses the existing persistent node rules. Starter rocks take two deliberate strikes before talent effects. Completion grants Mining XP even when a talent reduces the strikes needed. A completed rock drops three Stone and one Iron before progression bonuses as separate persistent pickups, disappears, and returns 72 elapsed World hours later. The World owns partial strikes and renewal; the Character owns Mining XP. A full backpack leaves materials on the ground.
- The workbench offers repeatable, flat Stone slabs for one Stone each and multiple Blacksmith's Anvils for six Stone plus two Iron each. Materials can come from the Character's backpack and placed home chest. Confirming a valid snapped position spends them atomically; canceling spends nothing. Paths remain walkable. Anvils block movement and carve navigation.
- Edit Home at the workbench lets a player aim at a path or Anvil and remove it for the full recipe refund. Backpack overflow becomes persistent pickups. Placed builds and recovered materials belong to the current World, shared by any Character visiting it.

## Presentation and future boundary

The rock uses the active KayKit Forest model without painted-on Iron veins; separate pickup cues show the result. Paths use the plain KayKit Dungeon stone tile. The station uses the KayKit anvil on a low stone base and is named **Blacksmith's Anvil**. All three source families are already in the public asset ledger and their selected files were unmarked in the owner's asset review at implementation time. Review joins, scale, hand placement, mining weight, and boundary clarity in a Mac build before accepting the art.

The home woodland adds thirteen trees and sixty fuller grass clumps around the existing ground cover. The 5.5 m home circle remains clear of resource nodes and grass. Old graybox rock barriers were removed and navigation rebaked; resource colliders and carving obstacles now follow each node's availability. The authored scenery pass preserves IDs on existing nodes when reapplied. Rebuilding a forest scene from scratch recreates IDs from authored positions, so shipped node IDs must be migrated if those positions change during a rebuild.

When smithing arrives, the Anvil will list known weapon and armor recipes, spend raw Iron and other materials, finish crafts immediately, and remain removable whenever its menu is closed. This slice intentionally has no Anvil interaction, fuel, queue, or smithing recipe.

The Character and World collection moves from version 4 to 5, adding Mining XP and manual tool selection while retaining prior equipment and progress. The existing stable IDs and atomic snapshot pattern remain the save boundary. The current one-chest placement flow remains separate until broader building is designed.
