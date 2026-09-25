# Weapon types: first combat choice

Status: first playable sword and two-handed axe slice implemented; Mac feel and
sound review remains.

The Practice Sword keeps its quick, 105-degree aimed swing and can be used with
a shield. A Two-Handed Axe is claimed once per World from the home gear rack.
It uses a slower, 75-degree chop, a modest damage increase, and a 0.3-second
stagger on the practice enemy and expedition Scouts. The Guardian takes damage
but resists stagger. A later hit refreshes ordinary stagger. Each weapon has
one attack on the existing action; dodge cancels recovery, not windup or the
active strike. The combat axe cannot chop trees; the separate Wood Axe remains
the Logging tool.

The initial axe timing is 0.32-second windup, 0.18-second active strike, and
0.44-second recovery at 2.1 units of reach. The sword remains 0.18/0.14/0.30
seconds at 2 units. A subtle ground arc shows each hit area. The existing
KayKit medium-rig two-handed chop clip and axe mesh supply presentation while
gameplay phases determine hits. The existing built-in Particle System and
Trail Renderer supply restrained swing and contact effects. No camera motion
or hit pause is added. Two swing and two impact clips come from the CC0 Kenney
packs registered in [the asset ledger](../THIRD_PARTY_ASSETS.md).

Equipping the axe moves an offhand shield to the backpack as part of the same
saved gear transaction. If the displaced gear cannot fit, the equip action
fails without moving anything. Equipping a shield while holding the axe moves
the axe to the backpack. Weapon and offhand changes are blocked during a
committed swing. Other Character gear and items continue to travel between
Worlds.

Swords, Axes, and Logging are separate Character skills. A valid combat hit
awards its weapon's skill XP equal to health actually removed, capped by the
target's remaining health. The later [progression design](PROGRESSION_PLAN.md)
adds source-level scaling, passive benefits, and talents. Collection version 4
first added Axes XP; later migrations retain it.

Unity's Animator, ScriptableObjects, AudioSource, Particle System, Trail
Renderer, and URP remain the foundations. `WeaponDefinition` and small combat
and save rules are Topaz-specific. No package is added. Revisit Animation
Rigging if the two-hand grip needs procedural correction, and revisit a more
general attack system only when a third weapon exposes a concrete shared need.

Review a Mac build at the gameplay camera with both weapons and both expedition
enemies. Check hand placement across all six Character looks, attack contact,
timing and dodge response at 60 Hz and 120 Hz, readable enemy tells, journal
focus, and whether the Kenney sounds have enough weight without crowding the
mix. The project gate is `./scripts/verify.sh`; run a Windows build for desktop
compatibility and inspect errors and `git diff --check`.
