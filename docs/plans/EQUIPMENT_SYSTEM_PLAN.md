# Equipment system

Status: first playable implementation built; Mac journal and shield-pose review remains.

Characters own seven equipment slots: weapon, tool, offhand, head, body, hands,
and boots. New Characters and migrated profiles receive a basic sword, axe,
shield, helm, coat, gloves, and boots. Equipped gear does not consume one of the
16 backpack slots. Spare pieces travel with the Character between Worlds and
can move individually through a storage chest. An equip action swaps with the
selected backpack slot; unequipping requires space. Unknown saved item IDs are
retained, but never grant stats or replace known gear silently.

The starter sword gives Attack 1; gloves give Attack 1; helm and coat give
Armor 1 each; boots give Move Speed 1; axe gives Logging 1. Shared base Attack
is 3. Equipment grants add to totals. Attack determines damage per weapon hit;
Armor subtracts from incoming damage, with an unblocked minimum of one.
Attack Speed multiplies each weapon-swing phase by 0.9 per point. Move Speed
adds 0.25 units per second per point to a 5.25 base. Dodge shortens its
0.65-second cooldown by 0.08 seconds per point, with a 0.35-second floor.
Logging adds work on a valid chop toward a tree's three-point requirement.
The basic axe therefore still takes three chops. Swords and Logging XP remain
separate until deeper use-based progression is designed.

The home gear rack has one each of Swift Gloves (Attack Speed 1), Agile Coat
(Dodge 1), and Agile Boots (Dodge 1) per World. A full backpack leaves a piece
on the rack. These are positive-stat choices, not items with penalties or
random rolls. The World owns rack claim flags, while the Character owns taken
items. The journal has Backpack and Equipment pages, a static portrait for
each chosen look, seven slots, selected-item swap actions, and stat totals.
It offers no stat explanations or block tutorial in this slice.

The sword remains visible in the right hand during exploration. Tree
interaction briefly uses the equipped axe, then restores the sword. The old
manual sword/axe toggle is removed. A shield attaches to each look's left-hand
socket and enables an aimed 120-degree frontal block held with right mouse or
gamepad left trigger. It protects after a 0.12-second raise and negates every
correctly faced hit while held. Guarding slows travel to 55% and prevents an
attack; dodging lowers guard. A blocked enemy uses the existing `Hit_A` reaction
and then recovers. There is no guard meter. The shield gives no passive Armor.

Body armor does not replace the selected Character's model. Gear has fixed
ScriptableObject definitions with stable IDs and one-per-slot stacks; the
versioned Character/World collection stores IDs and claims, never Unity object
references. Unity's uGUI, Input System, Animator, GameObjects, and save
serialization remain the first-party foundation. The small custom rules are
Topaz-specific; reconsider authored animation layers if the procedural guard
pose does not hold up in the Mac build.

Review in a 60 Hz Mac player: journal readability at 1280×720 and 1920×1080,
mouse/gamepad focus, every Character portrait, left-hand shield fit on all six
looks, guard impact readability, and combat timing. Run `./scripts/verify.sh`,
`./scripts/build.sh windows`, inspect console errors, and `git diff --check`.
