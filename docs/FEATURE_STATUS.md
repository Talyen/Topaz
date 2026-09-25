# Feature status and source of truth

Start with the current note below for behavior and implementation. `docs/plans/`
holds design decisions and future rules; its status line says whether the first
slice is proposed or implemented. A plan is not evidence that an unbuilt future
system already exists. Update this table and the current note when a slice ships.

| Area | Current behavior | Design record or remaining review |
| --- | --- | --- |
| Movement and camera | [Feel study](studies/FEEL_STUDY.md) | Mac feel tuning remains. |
| Combat | [Combat study](studies/COMBAT_STUDY.md) | The study's practice reset predates the current defeat slice; use the defeat design for recovery rules. |
| Weapon types | [Weapon types plan](plans/WEAPON_TYPES_PLAN.md) | Sword and two-handed axe first slice; Mac combat, animation, and sound review remains. |
| Saves, inventory, world clock | [Architecture](ARCHITECTURE.md), [world loop study](studies/WORLD_LOOP_STUDY.md), [day/night](DAY_NIGHT_CYCLE.md) | First playable systems; broader regions and higher-level progression sources are future work. |
| Regions and travel | [Expedition study](studies/EXPEDITION_STUDY.md) | First authored clearing only. |
| Menus and HUD | [UI design system](UI_DESIGN_SYSTEM.md), [characters and worlds](CHARACTERS_AND_WORLDS.md) | [Character selection plan](plans/CHARACTER_SELECTION_PLAN.md): implemented first pass, Mac owner review remains. |
| Lighting and weather | [Visual study](studies/VISUAL_STUDY.md), [day/night](DAY_NIGHT_CYCLE.md) | [Player light plan](plans/PLAYER_LIGHT_PLAN.md) and [weather plan](plans/WEATHER_PLAN.md): first implementations and darker 360-degree lantern tuning are in the Mac build; owner visual review remains. |
| Defeat and Campfire | [Defeat design](plans/DEATH_SYSTEM_PLAN.md) | First playable implementation; this plan is still the design record for future consequences. Mac transition and Campfire feel need owner review. |
| Equipment and shield | [Equipment design](plans/EQUIPMENT_SYSTEM_PLAN.md) | Seven-slot first playable implementation; Mac journal, portrait, and shield feel review remains. |
| Mining and home builds | [Mining and home building study](studies/MINING_AND_HOME_BUILDING_STUDY.md) | First playable implementation; Mac art, placement, and tool feel review remains. Anvil smithing recipes arrive with later equipment work. |
| Use-based progression | [Progression plan](plans/PROGRESSION_PLAN.md) | Seven-skill, ten-level first playable implementation; Staff and Crossbows unlock through the crypt. Mac talent, timing, and journal review remains. |
| Home crypt, Staff, and Crossbow | [Home crypt study](studies/HOME_CRYPT_STUDY.md) | Six-space dungeon with a gallery Rogue, Mage, persistent shortcut, one-time weapons, and two discoverable skills; Mac art, feel, sound, and duration review remains. |

The [agent task map](UNITY_REFERENCE_GUIDE.md) and `scripts/agent-areas.json` map
areas to code and tests. Keep the latter in sync when tests or paths move.
