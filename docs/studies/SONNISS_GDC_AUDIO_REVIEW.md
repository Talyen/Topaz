# Sonniss GDC Audio Review

## Review brief

Topaz is a survival-crafting action fantasy RPG. Review the Sonniss #GameAudioGDC
archive for a broad adventure palette with a balanced hybrid feel: tactile recorded
sounds for actions and materials, and designed fantasy sounds for magic and
supernatural threats. Combat, survival and crafting, and world ambience have equal
priority.

The first pass catalogs the official collections and searches the linked track
indexes for relevant filenames. Filenames are only shortlist evidence; clips still
need to be downloaded and auditioned before deciding which sounds suit Topaz. No
Sonniss audio has been downloaded or selected for use.

## Official archive catalog

Catalog checked 2026-09-25 at the [official Sonniss archive](https://sonniss.com/gameaudiogdc/).
The archive page currently exposes these eight collections. ZIP-part counts below
come from the primary download links on that page.

| Collection | ZIP parts | Official track index | Initial notes |
| --- | ---: | --- | --- |
| 2024 | 9 | [Google Sheet](https://docs.google.com/spreadsheets/d/1HAJdNA-QIug2IZjUV-DwCo1IZ-XJ4Vv9gWIaumNScfc/edit?usp=sharing) | The linked sheet currently opens with the title “GAME AUDIO BUNDLE 2023 - TRACKLIST”; verify whether it is the intended 2024 index before using it to select files. |
| 2023 (archive section “2021to2023”) | 14 | [Google Sheet](https://docs.google.com/spreadsheets/d/1HAJdNA-QIug2IZjUV-DwCo1IZ-XJ4Vv9gWIaumNScfc/edit?usp=sharing) | Search hits include sword slashes, hits, dark magic, spell whooshes, creature/monster textures, birds, rain, caves, and ambience. The 2024 section points to this same sheet. |
| 2020 | 14 | [Google Sheet](https://docs.google.com/spreadsheets/d/1QqIcxJ7y0Acq3HAj9qQvFgvyeBjJ_7JvX8-GguUHIVk/edit?usp=sharing) | Sheet title identifies “Part 6”. Search hits include future melee weapons, axe/tool sounds, rock debris, ancient chests, monsters, spells, cave creatures, animals, rivers, and storm ambience. |
| 2019 | 8 | [Google Sheet](https://docs.google.com/spreadsheets/d/1-McgwdktLVmctBrI4OGbgIBxeCp0A4KyKJzxOOTP6FY/edit?usp=sharing) | Sheet title identifies “Part 5”. Search hits include sword scrape, metal impacts, arcane spell, ghost vocals, footsteps, and eerie cave ambience; also object handling and UI. |
| 2018 | 8 | [Google Sheet](https://docs.google.com/spreadsheets/d/1h_aunO31U1g-QAxYxziSZn22LVe1eyA4o10KEnXKmIc/edit?usp=sharing) | Search hits include sword whoosh, creature spawn, ice footsteps, impacts, water/waves, wildlife, designed fire, and ambience. |
| 2017 | 9 | [Google Sheet](https://docs.google.com/spreadsheets/d/1aCBw_dNba_ajYziogKdi1nL7JmdggBwbZXBFSoNCAvY/edit?usp=sharing) | Sheet title identifies “Part 3”. Search hits include weapon impacts, magic/spells, troll and creature deaths, birds, rainforest, water, crickets, and footsteps. |
| 2016 | 6 | [Google Sheet](https://docs.google.com/spreadsheets/d/1CGa_Vbp6tYEUjzaU34w_U9K6Nul0UOF9IhFop3sedvo/edit?usp=sharing) | Sheet title identifies “Part 2”. Search hits include metal impacts/creaks, rock and soil debris, crickets, footsteps, horror ambience, and creature/robot impacts. |
| 2015 | 5 | [PDF track list](https://cdn.sonniss.com/storage/2025/05/Tracklist-GDC-GameAudio-Giveaway-Sheet1-1.pdf) | PDF index includes battle crowds, monster/zombie and animal vocals, gore, blacksmith/forge, water/weather, action whooshes, rock/brick/dirt impacts, creature screams, and magical explosions. |

The listed collections total 73 ZIP parts. Initial metadata supports a candidate
download batch of 64 ZIP parts across 2015–2020 and 2023. The 2024 index remains
unverified because it points to the 2023 sheet. These are collection-level
shortlists, not sound selections: clips must still be auditioned and individually
kept, held, or rejected. Sonniss's archive page
describes the collections as samples from larger supplier libraries, so absence
from these collections does not establish that a category is unavailable in the
full paid libraries.

## License and repository handling

Sonniss's [current #GameAudioGDC license](https://sonniss.com/gdc-bundle-license/)
is version 2.0, effective 2026-08-27. It grants commercial project use and
modification, while restricting publication or supply of the sound effects as
sound effects. The official [FAQ](https://sonniss.com/gameaudiogdc/) distinguishes
use in a finished game from raw redistribution. The license says downloads made
on or after its effective date are governed by the terms in force on the download
date, regardless of bundle year. Downloading and using the files accepts the
license; obtain owner confirmation immediately before the first download.

Topaz is public, so raw Sonniss audio must remain untracked. The repository already
ignores `/LocalSourceArchives/` for original source downloads. Local Unity audition
imports belong under `Assets/ThirdParty/Sonniss/`, which is now ignored along with
all files and generated `.meta` files beneath it. A clone will not contain these
clips. Keep the curation record here and do not describe the raw audio as included
in the public project.

After download confirmation, store untouched ZIPs under
`LocalSourceArchives/SonnissGDC/<year>/`, and selected unchanged clips under
`Assets/ThirdParty/Sonniss/Selected/<category>/`. Use categories for combat,
survival/crafting, world/ambience, and UI only when a clip has a clear Topaz use.
Record source archive, original filename, SHA-256, assigned category, decision
(keep/hold/reject), and audition rationale. Excluded files stay only in the ignored
original archive; do not duplicate them into the selected area.

The existing Kenney and JaggedStone sound libraries and all gameplay audio wiring
remain unchanged in this review. Wiring or replacement requires a later task.
