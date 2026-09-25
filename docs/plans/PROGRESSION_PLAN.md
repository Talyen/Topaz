# Use-based progression

Status: first playable implementation built; Mac combat, gathering, and journal feel review remains. The home crypt reveals Staff and Crossbows when their weapons are found, bringing the current total to seven skills. The current level-1 sources taper to zero XP at skill level 5. Higher-level authored regions are future content.

## Player rules

Swords, Axes, Shield, Logging, Mining, Staff, and Crossbows belong to the Character and travel between Worlds. A completed tree or ore deposit grants 10 base XP even when gear or a talent reduces its strike count. Direct weapon, bolt, and Staff spell hits grant XP equal to health actually removed; bleed ticks grant none. A successful directional block prevents damage and grants 2.5 base Shield XP. Failed blocks, dodges, and zero-XP actions are silent.

An enemy or gather node has an authored source level. XP is full at equal levels, falls to 75%, 50%, 25%, then zero as the skill moves one through four levels ahead, and gains 5% per level the source is ahead, capped at 25%. There is no repeat-use penalty. Experience is stored in hundredths so small rewards remain useful.

The cumulative XP thresholds for levels 1–10 are 0, 10, 22, 36, 52, 70, 90, 112, 136, and 162. Level 10 is the visible and mechanical cap. Levels 2, 5, and 8 each grant a choice of one of four talents within that skill; level 10 unlocks all remaining options. The first choice occupies one active slot. A second slot opens at level 5. A third talent learned away from home waits until the player changes the two active picks at the homestead. Initial choices happen in the Skills journal anywhere; later loadout changes are instant and free inside the home boundary. The journal shows all four choices and their effects before unlock. A brief cue marks level gain without interrupting play.

## Level benefits and talents

| Skill | Per-level handling and milestones | Talents |
| --- | --- | --- |
| Swords | Swing phases 1% quicker per level; +1 direct damage at levels 5 and 10. | Footwork: swing movement 45% to 55%. Flow: landed-hit recovery 0.04 s shorter. Wide Cut: arc +10°. Dodge Strike: first swing begun within 1 s of a dodge gains +1 damage. |
| Axes | Recovery 1% quicker per level; windup remains heavy; +1 direct damage at levels 5 and 10. | Bleed: non-stacking, one damage after 1.5 s, two-second duration refreshed by another hit without resetting tick timing. Rage: actual enemy damage while the axe is equipped grants +20% Axe damage for 5 s, refreshed on another hit. Heavy Impact: stagger +0.1 s where allowed. Finishing Blow: +1 damage when the enemy was at half health or less before the hit. |
| Shield | Guarded movement gains one percentage point per level, from 55% to 64%; blocked-enemy recovery +0.05 s at levels 5 and 10. | Quick Raise: 0.12 s to 0.10 s. Broad Guard: 120° to 130°. Mobile Guard: +5 percentage points guarded movement. Hold Line: blocked-enemy recovery +0.1 s. |
| Logging | Chop phases 1.5% quicker per level; +1 Wood at levels 5 and 10. | Quick Chop: action time −10%. Deep Bite: +1 work per chop. Clean Fell: +1 Wood. Stewardship: regrowth 12 World hours sooner, with an eight-hour minimum, recorded on the felled World node. |
| Mining | Strike phases 1.5% quicker per level; +1 Stone at level 5 and +1 Iron at level 10. | Quick Strike: action time −10%. Heavy Pick: +1 work per strike. Stone Lode: +1 Stone. Iron Seeker: +1 Iron. |
| Staff | Spell cooldown 1% quicker per level; +1 spell damage at levels 5 and 10. | Mobile Casting: movement while channeling 30% to 55%. Far Sigil: placement range +1 m. Wide Circle: radius +0.25 m. Dodge Focus: cast begun within one second of a dodge deals +1 damage. |
| Crossbows | Reload 1% quicker per level; +1 bolt damage at levels 5 and 10. | Pierce: 20% chance for a damaging bolt to pass through one target and strike one more aligned enemy. Quick Reload: 0.18 s shorter. Quick Aim: 0.07 s shorter windup. Long Sight: +1 m range. |

Talent effects apply only with their matching gear or action. A direct hit uses equipment Attack plus level and conditional talent damage, then applies the Rage multiplier and rounds to an integer. Bleed can defeat an enemy but never grants skill XP. Rage clears on weapon change, defeat, or travel; bleed clears when its enemy falls or resets. The starting numbers are for owner review in a 60 Hz Mac build.

## Ownership and implementation

Unity ScriptableObjects hold authored skill, talent, enemy, tree, and mining definitions. Plain C# rules calculate levels and XP scaling; existing GameObjects and attack phases apply outcomes. Character collection version 8 stores centi-XP, stable learned/active talent IDs, Staff and Crossbows discovery, and the World-owned first Crossbow drop. Migration preserves old levels and progress within them; old overflow above level 10 remains saved. World nodes retain their stable IDs and regrowth deadlines. The journal is uGUI and uses the existing focus and input setup. No additional package is required. If future regions need substantially different source rewards, extend authored node definitions rather than adding a parallel progression system.

Run `./scripts/verify.sh`, `./scripts/build.sh windows`, inspect console errors, and `git diff --check` for handoff. In the Mac player, inspect timing, feedback, and journal navigation at 60 Hz; balance against a higher-level authored region when one exists. This build establishes the rules and first choices, not the final level-10 playtime.
