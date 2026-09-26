# Sonniss GDC Audio Review

## Review brief

Topaz is a survival-crafting action fantasy RPG. Review the Sonniss #GameAudioGDC
archive for a broad adventure palette with a balanced hybrid feel: tactile recorded
sounds for actions and materials, and designed fantasy sounds for magic and
supernatural threats. Combat, survival and crafting, and world ambience have equal
priority.

The first pass cataloged the official collections and searched the linked track
indexes for relevant filenames. A metadata-based curation pass has now staged a
broad local audition set from the years that were accessible. Filenames and pack
descriptions guided selection; the clips have not been subjectively auditioned,
so these are candidates for Topaz rather than final sound-design choices.

## Official archive catalog

Catalog checked 2026-09-25 at the [official Sonniss archive](https://sonniss.com/gameaudiogdc/).
The archive page currently exposes these eight collections. ZIP-part counts below
come from the primary download links on that page.

| Collection | ZIP parts | Official track index | Initial notes |
| --- | ---: | --- | --- |
| 2024 | 9 | [Google Sheet](https://docs.google.com/spreadsheets/d/1HAJdNA-QIug2IZjUV-DwCo1IZ-XJ4Vv9gWIaumNScfc/edit?usp=sharing) | Candidate collection. Its index link duplicates the 2023 sheet, so review the actual filenames if Sonniss's 2024 archive becomes accessible. |
| 2023 (archive section “2021to2023”) | 14 | [Google Sheet](https://docs.google.com/spreadsheets/d/1HAJdNA-QIug2IZjUV-DwCo1IZ-XJ4Vv9gWIaumNScfc/edit?usp=sharing) | Search hits include sword slashes, hits, dark magic, spell whooshes, creature/monster textures, birds, rain, caves, and ambience. The 2024 section points to this same sheet. |
| 2020 | 14 | [Google Sheet](https://docs.google.com/spreadsheets/d/1QqIcxJ7y0Acq3HAj9qQvFgvyeBjJ_7JvX8-GguUHIVk/edit?usp=sharing) | Sheet title identifies “Part 6”. Search hits include future melee weapons, axe/tool sounds, rock debris, ancient chests, monsters, spells, cave creatures, animals, rivers, and storm ambience. |
| 2019 | 8 | [Google Sheet](https://docs.google.com/spreadsheets/d/1-McgwdktLVmctBrI4OGbgIBxeCp0A4KyKJzxOOTP6FY/edit?usp=sharing) | Sheet title identifies “Part 5”. Search hits include sword scrape, metal impacts, arcane spell, ghost vocals, footsteps, and eerie cave ambience; also object handling and UI. |
| 2018 | 8 | [Google Sheet](https://docs.google.com/spreadsheets/d/1h_aunO31U1g-QAxYxziSZn22LVe1eyA4o10KEnXKmIc/edit?usp=sharing) | Search hits include sword whoosh, creature spawn, ice footsteps, impacts, water/waves, wildlife, designed fire, and ambience. |
| 2017 | 9 | [Google Sheet](https://docs.google.com/spreadsheets/d/1aCBw_dNba_ajYziogKdi1nL7JmdggBwbZXBFSoNCAvY/edit?usp=sharing) | Sheet title identifies “Part 3”. Search hits include weapon impacts, magic/spells, troll and creature deaths, birds, rainforest, water, crickets, and footsteps. |
| 2016 | 6 | [Google Sheet](https://docs.google.com/spreadsheets/d/1CGa_Vbp6tYEUjzaU34w_U9K6Nul0UOF9IhFop3sedvo/edit?usp=sharing) | Sheet title identifies “Part 2”. Search hits include metal impacts/creaks, rock and soil debris, crickets, footsteps, horror ambience, and creature/robot impacts. |
| 2015 | 5 | [PDF track list](https://cdn.sonniss.com/storage/2025/05/Tracklist-GDC-GameAudio-Giveaway-Sheet1-1.pdf) | PDF index includes battle crowds, monster/zombie and animal vocals, gore, blacksmith/forge, water/weather, action whooshes, rock/brick/dirt impacts, creature screams, and magical explosions. |

The listed collections total 73 ZIP parts. Category-matched material appears
across the archive, and the owner asked to retain useful sound variations. Within
the chosen genre-fit packs, all variations remain; only byte-identical copies are
collapsed. The accessible 2015–2019 years have been staged; the 2020, 2023, and
2024 collections remain unavailable through the official routes tried. Sonniss's
archive page describes these as samples from larger supplier libraries, so
absence from the bundles does not establish that a category is unavailable in the
full paid libraries.

## Download status

The owner confirmed acceptance of Sonniss's current license on 2026-09-25 before
the downloads. All accessible 2015–2019 archive data is in ignored
`LocalSourceArchives/SonnissGDC/`: 32 ZIP files totaling 96,626,196,123 bytes
(96.6 GB), with byte sizes, official mirror URLs, and SHA-256 digests in the local
`download_manifest.csv`. The 2015 mirror supplies one combined ZIP for its five
listed parts; 2016–2019 are split ZIPs. The originals remain untouched.

The primary CDN returned a Cloudflare challenge, and the Sonniss-listed
FeralHosting mirror returned a 504 for a 2024 part. The 2020, 2023, and 2024
collections remain unavailable through the official routes tried so far. Do not
bypass host/client protections or use unofficial mirrors.

## Current staging after approved cuts

The owner approved all clips in [pass 1](SONNISS_GDC_AUDIO_SUGGESTED_DELETIONS.md), [pass 2](SONNISS_GDC_AUDIO_ADDITIONAL_CUTS.md), and [pass 3](SONNISS_GDC_AUDIO_PASS3_SUGGESTIONS.md), including optional and aggressive tiers. All 244 WAVs and their matching metadata have been removed, leaving **784 unique WAVs (15,141,891,818 bytes; 15.14 GB)**. Source ZIPs remain untouched. The import CSVs and counts below describe acquisition history, not current on-disk availability; subtract the three completed deletion lists when reconstructing the current set. Do not rerun the original extraction script without honoring all three lists.

## Original local staging

The five downloaded years contain 4,215 WAV records (112.41 GB uncompressed).
The genre-fit selection contains 927 records; four byte-identical copies were
collapsed, leaving 923 unique WAVs (17,964,776,815 bytes) under ignored
`Assets/ThirdParty/Sonniss/Selected/<category>/<year>/`. All sound variations
within chosen packs remain available.

The owner asked to defer narrowing the candidates. I therefore added the 109
previously held genre-plausible records to the ignored Unity review queue at
`Assets/ThirdParty/Sonniss/NeedsReview/`: 105 new unique WAVs (4,788,533,814
bytes) and four records that are byte-identical to clips already in `Selected/`.
This includes visceral/gore candidates and large long-form ambience. Together,
the `Selected/` and `NeedsReview/` folders contain 1,028 unique WAVs
(22,753,310,629 bytes). All 1,028 have Unity `AudioImporter` metadata, and Unity
resolved the 105 new review-queue files as `AudioClip` assets.

The 923-file genre-fit selection is distributed as follows:

| Category | Clips |
| --- | ---: |
| Combat | 212 |
| World | 211 |
| Survival/crafting | 166 |
| Creatures | 151 |
| Movement/foley | 102 |
| UI | 57 |
| Magic | 24 |

The separate review queue is organized under `NeedsReview/VisceralGore/` and
`NeedsReview/<category>/` so its gore and large ambience candidates are easy to
revisit. The remaining 3,179 files were excluded by the genre-fit rules and stay
only in the ignored source archives. No sound has been wired to scenes or
triggers, and existing libraries and audio wiring remain unchanged. This pass
used pack and filename metadata, not listening; the candidates still need
subjective audition and a later shortlist.

## License and repository handling

Sonniss's [current #GameAudioGDC license](https://sonniss.com/gdc-bundle-license/)
is version 2.0, effective 2026-08-27. It permits commercial use and modification
in synchronized projects, including games, without attribution. It prohibits
publishing or supplying the sounds as standalone assets or as a sound library,
including modified versions, and prohibits AI training use. The version in effect
on the download date governs, regardless of bundle year. The owner accepted
version 2.0 before the first download; a dated source note is kept with the local
archives.

Topaz is public, so raw Sonniss audio must remain untracked. The repository ignores
`/LocalSourceArchives/`, `/Assets/ThirdParty/Sonniss/`, and the root
`/Assets/ThirdParty/Sonniss.meta` file. A clone will not contain the original
archives or Unity audition imports. The public selection and review-queue manifests
contain filenames and hashes, not audio bytes; do not describe local audio as
included in the public project.

Store untouched ZIPs under `LocalSourceArchives/SonnissGDC/<year>/`, and unchanged
candidate clips under
`Assets/ThirdParty/Sonniss/Selected/<category>/<year>/` or
`Assets/ThirdParty/Sonniss/NeedsReview/<category>/<year>/`. The tracked
[`SONNISS_GDC_AUDIO_SELECTIONS.csv`](SONNISS_GDC_AUDIO_SELECTIONS.csv) and
[`SONNISS_GDC_AUDIO_REVIEW_QUEUE.csv`](SONNISS_GDC_AUDIO_REVIEW_QUEUE.csv) record
source year/archive, original filename, SHA-256, category, and duplicate mapping.
The local decision ledger retains excluded-file reasons. Excluded audio stays only
in the ignored original archives.

The existing Kenney and JaggedStone sound libraries and all gameplay audio wiring
remain unchanged in this review. Wiring or replacement requires a later task.
