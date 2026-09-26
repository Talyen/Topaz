# Characters, Worlds, and visits

A Character owns inventory, equipment, skills, food/rest effects, lantern state and appearance. A World owns its procedural seed/settings, elapsed time, resources, placed structures and storage, pickups, and encounter deadlines. A Character–World Visit owns position, discovered campfires and the recovery selection. Characters can visit multiple Worlds without copying or resetting a World's resources.

Title Continue resumes the last valid pair. Play selects or creates a Character, then selects or creates a World. Back from an unfinished draft creates nothing. Deleting a Character removes their visits and carried progress, preserving Worlds. Deleting a World removes its visits and World-owned state, preserving Characters. Confirm destructive collection actions and retain a clear Back route.

The fresh Baseline-v1 save root uses one validated collection snapshot and atomic replacement, with backup recovery and coalesced writes. Old format migration is intentionally removed. Save failures must not silently overwrite unreadable progress. Structures store stable IDs, region identity and local XYZ; generation settings/version remain part of the World contract.

A new Character carries prototype staff/crossbow equipment as well as the equipped starter melee/tool/shield set. New visits start at the permanent starter camp. A newly placed camp is discovered by its builder; other Characters discover it when visiting. Removing a camp removes all matching World-visit discoveries and redirects recovery to the starter camp.
