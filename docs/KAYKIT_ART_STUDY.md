# KayKit art study

Topaz's first art audition uses free KayKit packs by Kay Lousberg in the existing Bootstrap scene. These models are a visual baseline for review, not a final art commitment. The world remains hand-authored and the combat/gathering rules remain Topaz code.

## Imported library

The owner supplied 11 free KayKit download folders on 2026-09-23: Adventurers 2.0, Block Bits 1.0, Character Animations 1.1, Dungeon 1.1, Fantasy Weapons Bits 1.0, Forest Nature 1.0, Furniture Bits 1.0, Halloween Bits 1.0, RPG Tools Bits 1.0, Resource Bits 1.0, and Skeletons 1.1. Each folder included Kay Lousberg's CC0 1.0 license. The Unity project contains the Unity-oriented FBX export of every unique model, character and animation FBX files, required texture atlases, and the exact license text under `Assets/ThirdParty/KayKit/<pack>/`. Each pack's `SOURCE.md` records its free download folder, acquisition date, imported file counts, and SHA-256 tree digest of the original extracted download. Duplicate OBJ, glTF, generic FBX exports, sample images, and web shortcuts were omitted. The original download folders remain in Downloads as backups.

KayKit's [official asset catalog](https://kaylousberg.com/game-assets) links the pack pages. The [Forest](https://kaylousberg.itch.io/kaykit-forest), [Resource Bits](https://kaylousberg.itch.io/resource-bits), and [RPG Tools](https://kaylousberg.itch.io/rpg-tools-bits) pages explicitly describe free CC0 tiers. All imported files retain the exact `LICENSE.txt` supplied with their downloaded pack. See [the third-party asset register](THIRD_PARTY_ASSETS.md) and the per-pack source notes for provenance.

Git LFS tracks the FBX source binaries. Unity imports them as model assets; only referenced models enter a player build. The first scene uses Rogue, Skeleton Minion, forest trees and rocks, a Wood log pickup, an axe and sword, and dungeon furniture. The rest of the library is available for later authored scenes. URP materials reference the packs' own atlas textures.

## Review in the Mac build

At Topaz's fixed orthographic camera, check the silhouettes, scale, palette, ground contrast, weapon readability, and whether attack tells remain visible over the forest forms. The art study keeps the existing movement and combat graybox mechanics. Character animation, lighting polish, and a wider environment layout need separate review before we treat KayKit as the production style.

The tree now drops a visible, saved Wood bundle on its final chop. Walking close to a drop collects as much as the backpack can hold; any remainder stays in the world. Every successful tree chop earns Logging XP, and every sword hit that actually damages the enemy earns Swords XP. Neither uses a per-target award cap. Saves use version 3; earlier versions migrate. Disk writes run on a background task and rapid changes coalesce, with a flush when the player pauses or quits.
