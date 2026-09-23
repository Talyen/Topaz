# One-sword, one-enemy combat study

This small extension to the movement graybox tests combat timing. It is not an item, talent, death, or save system.

- **Sword:** Left mouse button or gamepad right trigger starts a fixed-direction swing toward the current aim. The cyan ground arc shows its reach. Movement slows during windup and the active strike. A dodge can cancel recovery, but cannot cancel windup or the strike.
- **Enemy:** One red practice enemy navigates around the authored obstacles. It turns toward the player, then shows a red arc for 0.65 seconds before striking. Stepping or dodging outside that arc avoids the hit. Sword hits briefly flash the enemy; three hits defeat it, and it returns after three seconds for repeated practice.
- **Safe home:** The blue ring around the starting position marks a protected area. The enemy disengages when the player enters it, and the player's temporary practice health refills on return. Outside it, three enemy hits return the player to the center. This is a temporary reset, not Topaz's final death rule.

The sword and enemy have separate authored definition assets with stable IDs. Current health, attack phase, path, and cooldowns are runtime state. The graybox has a baked NavMesh for Unity's default humanoid agent; its floor and static obstacles are the only included navigation geometry. Re-bake through `Topaz/Build Combat Study` after changing those obstacles.

Playtest whether swing commitment, dodge timing, enemy tells, and the safe-home boundary are legible. Keep numerical tuning flexible until those choices feel good. Inventory, loot, skill experience, and final death consequences come later.
