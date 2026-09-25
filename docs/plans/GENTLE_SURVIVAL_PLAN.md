# Gentle survival and stamina

Status: first playable implementation built for Mac review on 2026-09-25. The numerical values and food art are prototype targets; the benefit-only rules below are the design choice.

## Player experience

- Stamina is a short reserve for smoother physical actions. Empty stamina never prevents an attack, tool strike, dodge, crossbow shot, or jump. Ordinary movement, aim, guard, and spells cost nothing. A funded attack or dodge recovers 10% quicker; a funded jump rises 10% higher. Windups, damage, dodge distance, and invulnerability remain unchanged.
- A base reserve is 100. Sword and crossbow actions cost 25, heavy axe 30, tool strikes 20, dodge 25, and jump 15. Four to five consecutive enhanced actions should fit in a fresh reserve. Refill starts one second after exertion and continues while walking at 20 per second. The contextual meter appears during and briefly after exertion, then leaves exploration clear.
- Rest at any bedroll or bed advances eight World hours, restores full health and stamina, and grants Rested for 24 World hours beginning after the skip. Rested raises maximum stamina to 125. Rest works during nearby combat: protect the covered transition, then reset the local encounter from World-owned state and normal enemy deadlines. Claimed rewards and gathered resources never roll back.
- Red Berries are eaten raw for 20% faster stamina refill for 12 World hours. One Mushroom cooks into one Mushroom Stew at any Campfire; Stew gives 50% faster refill for 24 World hours. Eating uses the paused Backpack and works during combat. An equal or stronger food effect disables Eat so no item is wasted. Stew can replace Berries; Rested stacks with either food.

## World, clock, and inventory

- Authored Red Berry and Mushroom nodes are a short safe walk from Home. One interaction takes one item if the Backpack has space. Each World-owned node has a stable ID and renews 24 World hours after harvest. Full inventory leaves the plant untouched.
- Interact at a Campfire opens one Journal with Travel on the left and Cook on the right. The existing Journal parchment is the selected backdrop. Travel still costs no time and uses the Character-World Visit's discoveries. Cooking takes no time, consumes Mushrooms from the Backpack first and then a chest near the fire, and places Stew in the Backpack only if it fits. The ingredient and meal move atomically in the save snapshot.
- Character-owned stamina, Rested, and food durations follow that Character between Worlds. Timers advance only while the Character is active, using elapsed World hours. Rest and defeat recovery advance timers by eight hours; menus, loads, and time with the game closed do not. Future timed status effects follow the same clock so a rest expires any effect whose remaining duration is at most eight hours.
- The Journal's Status Effects page lists active effects only, with icons, names, descriptions, and remaining World time. An empty page says no effects are active. Additional effects should supply entries to the same page rather than add permanent exploration icons.

## Implementation and review

Use Unity GameObjects, authored stable IDs, ScriptableObject item definitions, the existing uGUI/TMP Journal, and the Input System. Topaz-specific stamina, food priority, time skip, and save rules stay in small code. The initial plant and food art is owned placeholder geometry and icons; replace it after a Mac visual review without changing item or node IDs.

Review action cadence, dodge readability, forage visibility and reach, Campfire two-page navigation, Backpack Eat, the Status page, rest under threat, and UI Scale 100%/125%/150% in a Mac build. Verify save migration and rest/defeat time skips, then run the project verification and Windows compatibility build.
