# Topaz prefab authoring

Topaz owns the objects placed in a level. Art packs supply nested visuals. The
September 2026 migration covers Bootstrap, Expedition, Crypt, the menu stage,
all six player looks, enemies, held equipment, pickups, home builds, and static
prototype geometry. The old prototype reconstruction commands were retired;
the authored scenes and prefab assets are now the source of truth.

## Ownership

- Environment visuals and collision geometry: `Assets/Topaz/World/Environment/Prefabs`.
- Resource nodes, storage, campfires, and build templates: `Assets/Topaz/Gameplay/WorldLoop/Prefabs`.
- Enemies and equipment visuals: `Assets/Topaz/Gameplay/Combat/Prefabs`.
- Character appearances: `Assets/Topaz/Characters/Prefabs/Visuals`.
- Player composition and carried lantern: `Assets/Topaz/Player/Prefabs`.
- Menu stage: `Assets/Topaz/UI/Menus/Prefabs/Main Menu Stage.prefab`.
- Crossbow projectile: `Assets/Topaz/World/Crypt/Prefabs/CrossbowBolt.prefab`.

Use ordinary prefab instances. Keep imported models nested beneath `Visual` in
an owned visual prefab; do not unpack them or derive gameplay prefab roots
from vendor model prefabs. Shared material treatments use native prefab
variants. The `Variant` suffix denotes an authored alternative; inspect its
base in Unity before changing it. Characters with different skeletons and
animations have independent visual prefabs.

Keep gameplay rules, collision, navigation obstacles, and saved identity outside
the replaceable model. Repeated gameplay objects have their own prefab around
the visual prefab. Scene composition supplies references to the player, camera,
UI, regions, and other scene services as instance overrides. Reusable assets
must not contain scene references or a persistent instance ID.

Position and rotate the outer object to compose a scene. Author shared material,
model correction, pivot, and scale changes inside the visual prefab. Use a
variant for an intentional shared difference; avoid repeating renderer overrides
on individual placements. Root transform overrides are expected for scene layout.

## Replacing art

1. Open the owned visual prefab in Prefab Mode. Replace its nested model; keep
   the owned prefab asset and `.meta` identity.
2. Correct the new model's local offset, rotation, materials, and size beneath
   the stable root. Keep the ground-contact plane and gameplay footprint.
3. For characters, reconnect `CharacterVisual`: Animator, body renderer, right
   and left equipment sockets, lantern socket, guard arm bones, and shield.
   Configure the Animator/avatar/clips in the prefab. Attach the animation
   driver beside its Animator. Drivers bind to their owning player or enemy;
   previews disable gameplay presentation drivers.
4. Keep equipment as nested owned prefabs. The appearance system instantiates
   complete configured looks; it no longer copies weapons from a hidden Rogue.
   The lantern socket includes its rig-specific offset. The carried-lantern
   prefab owns its model scale, materials, and emissive surface.
5. Review clipping, visibility, collision, navigation, animation, and performance
   in a Mac build. Rebuild navigation if walkable/blocking geometry changes.

Animation retargeting across incompatible rigs remains art work. The authored
bindings localize that work; they do not make every rig interchangeable.

## Geometry conventions

The [measured prefab footprints](PREFAB_FOOTPRINTS.md) record the current reference sizes.
Unity units represent metres. Use Y up and +Z forward. Ground props use a root
at ground contact; put any mesh pivot correction under `Visual`. Held props
use their grip at the socket origin. Keep rendered ground separate from the
walkable collision floor where already authored.

Current tree collision uses a 0.43 m trunk radius and 2.2 m capsule height;
its navigation obstacle uses a 0.48 m radius. Resource rocks use a 0.75 m
collision radius and 0.8 m navigation radius. Preserve these gameplay clearances
when replacing decorative art, or deliberately update collision and navigation
alongside the visual.

Dirt floor visuals are composed on a 4 m grid; their local scale accommodates
rectangular areas. Home paths retain the existing 0.75 m placement grid. For
walls, gates, stairs, and other modular pieces, use the current prefab's bounds
and collision as the reference dimensions: preserve end planes, doorway opening,
walkable width, and step endpoints. A different module size requires a layout
pass; do not hide it by arbitrary nonuniform scaling of an entire building.

## Identity and checks

Resource-node IDs and campfire IDs belong to scene instances, not prefab assets.
Keep existing IDs when moving or restyling a persistent object. After adding a
new instance, use `Topaz/Prefabs/Assign Missing Instance IDs In Open Scenes`.
Duplicating an existing persistent object also copies its ID: select only the
new duplicate and use `Topaz/Prefabs/Assign New IDs To Selected Objects` before saving. Set a campfire's region on its scene instance.
The validator rejects missing and duplicate IDs. Save schemas and definition IDs
were not changed by this migration.

`Topaz/Prefabs/Validate Authored Content` checks scenes and prefab assets for
missing references, direct vendor-model placement/spawn references, character
bindings, and identity ownership. The EditMode regression suite also proves
that replacing a visual updates two scenes without losing placement, collision,
or distinct saved IDs, and that repeated extraction preserves authored edits.

`Topaz/Prefabs/Migrate Prototype Content` is an explicit compatibility tool for
loose prototype objects, not a scene generator. It reuses existing prefab assets
without overwriting their artwork. `PrefabSources.json` beside that Editor tool
records source-model provenance and replacement paths; no runtime system reads
it. Normal art changes happen in Prefab Mode. Asset-review decisions remain
owned by the owner; no new packs were imported.

Run `./scripts/verify.sh` and `./scripts/build.sh windows` before handoff, and
review the Mac build. Generated logs, captures, builds, and test fixtures stay
outside version control.
