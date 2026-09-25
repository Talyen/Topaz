# Third-party asset register

## JaggedStone crypt audio

Three original CC0 Ogg files from JaggedStone's [Magic Spell SFX](https://opengameart.org/content/magic-spell-sfx) and [Loopable Dungeon Ambience](https://opengameart.org/node/29778) are under `Assets/ThirdParty/JaggedStone/CryptAudio/`. The exact file URLs, SHA-256 digests, acquisition date, and license evidence are in that folder's `SOURCE.md`. The files may be publicly redistributed and used commercially under CC0; credit is voluntary.

KayKit CC0 game-content assets are registered below. Unity's packages and the Universal 3D template remain under their respective Unity terms. The world loop UI imports the TextMesh Pro Essential Resources distributed with Unity's `com.unity.ugui` package.

## Kenney weapon sounds

The weapon and crossbow slices use six unmodified CC0 clips from Kenney's free
[RPG Audio](https://kenney.nl/assets/rpg-audio) and
[Impact Sounds](https://kenney.nl/assets/impact-sounds) packs. The exact archive
licenses, archive SHA-256 digests, dates, and selected filenames are recorded in
`Assets/ThirdParty/Kenney/RpgAudio/SOURCE.md` and
`Assets/ThirdParty/Kenney/ImpactSounds/SOURCE.md`. Kenney is credited voluntarily.

For candidate free 3D pack families, licensing pitfalls, and the import process, see [Finding 3D art for Topaz](ASSET_SOURCING.md). A candidate listed there is not approved or imported merely because it is free to download.

Before adding a free placeholder, record:

| Asset and files | Source URL | License and version | Public source redistribution permitted? | Attribution and changes |
| --- | --- | --- | --- | --- |
| Liberation Sans font, SDF assets, and TMP Essential Resources under `Assets/TextMesh Pro/` | Unity `com.unity.ugui` package, `Package Resources/TMP Essential Resources.unitypackage` | Font: SIL Open Font License 1.1, notice retained at `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt`; TMP resources: Unity package terms | Yes for the OFL font with notice; Unity resources are included as the engine's standard project resources | Imported without changes for readable prototype UI. |
| Cormorant Garamond variable display font in `Assets/ThirdParty/Fonts/CormorantGaramond/` and its generated TMP SDF asset | [Google Fonts source](https://github.com/google/fonts/tree/main/ofl/cormorantgaramond); exact URL and SHA-256 in `SOURCE.md` | SIL Open Font License 1.1; copyright and license retained in `OFL.txt` | Yes, with notice and license | Source font unchanged; SDF asset generated in Unity for UI headings. |
| `Assets/Topaz/Presentation/Graphics/Shaders/Orthographic*DepthOfField.shader` | Unity URP package `Shaders/PostProcessing/{Gaussian,Bokeh}DepthOfField.shader` | [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license) for Unity-derived CoC logic | Yes, in connection with this Unity project under the package license | Minimal orthographic depth conversion and Bokeh mask dither; blur and composite passes reference the installed Unity shaders via `UsePass`. Original Unity copyright and license are noted in each adapter. |

“Free to download” is insufficient for a public source repository. If source redistribution is not expressly permitted, do not commit the asset; find a compatible alternative. Keep this register in sync with imported files and retain license notices where required.

For each imported pack, also retain its exact license text or a dated copy of the source terms, acquisition date, pack version/revision, and original archive SHA-256. Identify the subset of files used and any changes in the row above or a linked per-pack note. This lets agents distinguish one free tier or license revision from another.

## KayKit free-pack imports, 2026-09-23

The owner supplied the free download folders. Every imported pack contains its supplied CC0 1.0 `LICENSE.txt` and a `SOURCE.md` with version, the original extracted-folder SHA-256 tree digest, and the included file count. The canonical source is [Kay Lousberg's KayKit catalog](https://kaylousberg.com/game-assets); the [art study record](studies/KAYKIT_ART_STUDY.md) lists the visual use in Topaz. The source files are publicly redistributable under CC0. Topaz credits Kay Lousberg voluntarily and does not claim ownership of these models.

| Pack and repository files | Source / exact free folder | License and changes |
| --- | --- | --- |
| `Assets/ThirdParty/KayKit/Adventurers/` | `KayKit_Adventurers_2.0_FREE` | CC0 1.0; Unity FBX models, characters, animations, and texture atlases; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/BlockBits/` | `KayKit_BlockBits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/CharacterAnimations/` | `KayKit_Character_Animations_1.1` | CC0 1.0; FBX animation files and mannequin atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/Dungeon/` | `KayKit_Dungeon_Pack_1.1_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/FantasyWeapons/` | `KayKit_FantasyWeaponsBits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/Forest/` | `KayKit_Forest_Nature_Pack_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/Furniture/` | `KayKit_Furniture_Bits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/Halloween/` | `KayKit_HalloweenBits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/RPGTools/` | `KayKit_RPGToolsBits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/ResourceBits/` | `KayKit_ResourceBits_1.0_FREE` | CC0 1.0; Unity FBX models and atlas; otherwise unmodified. |
| `Assets/ThirdParty/KayKit/Skeletons/` | `KayKit_Skeletons_1.1_FREE` | CC0 1.0; Unity FBX models, characters, animations, and atlas; otherwise unmodified. |

## KayKit art curation, 2026-09-24

The owner marked 69 FBX models Archive. They retain their original bytes and
Unity `.meta` files under `ArtArchive/KayKit/`, outside Unity's `Assets/`
folder. The affected BlockBits, Forest, Furniture, and ResourceBits archive
folders contain copies of their supplied `LICENSE.txt` and `SOURCE.md`. Active
pack folders retain the original license, provenance note, atlas, and other
models. `AssetReview/decisions.json` records the owner's GUID-level choices;
`AssetReview/replacements.json` records replacements for models used by the
authored scenes. Review previews for the archived files are in
`ArtArchive/Previews/`.
