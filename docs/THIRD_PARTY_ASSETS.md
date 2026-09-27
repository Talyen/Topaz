# Third-party asset register

## JaggedStone crypt audio

Three original CC0 Ogg files from JaggedStone's [Magic Spell SFX](https://opengameart.org/content/magic-spell-sfx) and [Loopable Dungeon Ambience](https://opengameart.org/node/29778) are under `Assets/ThirdParty/JaggedStone/CryptAudio/`. The exact file URLs, SHA-256 digests, acquisition date, and license evidence are in that folder's `SOURCE.md`. The files may be publicly redistributed and used commercially under CC0; credit is voluntary.

## Sonniss #GameAudioGDC audition set, 2015–2019 (local only)

The original import staged 1,028 unchanged WAV clips (923 genre-fit
selections and 105 additional unique review-queue clips) from Sonniss's
[#GameAudioGDC archive](https://sonniss.com/gameaudiogdc/). The original 32 ZIPs
are in ignored `LocalSourceArchives/SonnissGDC/`; Unity audition files are in
ignored `Assets/ThirdParty/Sonniss/Selected/` and
`Assets/ThirdParty/Sonniss/NeedsReview/`. Four review-queue records are
byte-identical to already selected clips. The owner subsequently approved all
138 clips in [current baseline](BASELINE.md), 57 in
[current baseline](BASELINE.md), and 49 in
[current baseline](BASELINE.md). All 244 WAVs and
their Unity metadata were removed, leaving **784 clips (15.14 GB)**. The import
CSVs remain historical records; the three completed deletion lists record which
paths are no longer staged.

The owner accepted Sonniss's official [#GameAudioGDC license v2.0](https://sonniss.com/gdc-bundle-license/),
effective 2026-08-27, before the downloads on 2026-09-25. It permits commercial
use and modification in synchronized games without attribution, prohibits
publishing or supplying the sounds as standalone assets or libraries, and
prohibits AI training use. The official license page and its version archive are
the source of record; a dated summary and download hashes/URLs are in the local
`LocalSourceArchives/SonnissGDC/SOURCE.md` and `download_manifest.csv`. Because
Topaz's Git repository is public, neither the archives nor audio files are
redistributable here; the source directories and generated Unity metadata are
ignored. The tracked
[current baseline](BASELINE.md)
and [current baseline](BASELINE.md)
record source names, hashes, category paths, and duplicate mappings without
audio data. Selection was based on metadata, not listening. Existing audio
libraries and in-game wiring are unchanged.

Unity's packages and the Universal 3D template remain under their respective Unity terms. The world loop UI imports the TextMesh Pro Essential Resources distributed with Unity's `com.unity.ugui` package.

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

“Free to download” is insufficient for a public source repository. If source redistribution is not expressly permitted, do not commit the asset; find a compatible alternative. Keep this register in sync with imported files and retain license notices where required.

For each imported pack, also retain its exact license text or a dated copy of the source terms, acquisition date, pack version/revision, and original archive SHA-256. Identify the subset of files used and any changes in the row above or a linked per-pack note. This lets agents distinguish one free tier or license revision from another.

## Retired KayKit imports

The former CC0 KayKit imports, derived animation clips, tracked art archive and review gallery were removed in the baseline cleanup. Original local downloads remain preserved under LocalSourceArchives. No current visual depends on those imports. Historical source/attribution records remain recoverable in Git and the cleanup snapshot.

## Unity URP Terrain Sample — private dependency (2026-09-26)

- Source: https://assetstore.unity.com/packages/3d/environments/landscapes/unity-urp-terrain-terrain-sample-project-213197
- Acquired through Unity Package Manager from the owner's existing entitlement; downloaded version 1.0.3.
- Included ThirdPartyNotices.txt assigns the art to the standard Unity Asset Store EULA. Raw files remain ignored under `Assets/ThirdParty/UnityTerrainSample/`.
- Selected 72 source assets (144 files with metadata): two pine variants and dependencies, a rock, grass/fern prefabs, and two terrain layers with textures. No supplied scene layouts, TerrainData, scripts or settings are imported.
- `scripts/terrain-sample-files.json` records exact paths and original SHA-256 hashes; `scripts/restore-terrain-sample.py` restores this selection.
- Runtime wrappers and configuration: `Assets/Topaz/Presentation/Rendering/Environment/`. Original files are staged outside Assets in ignored `LocalSourceArchives/UnityTerrainURP/`.

## Unity graph authoring templates

The generated VFX and Shader Graph assets begin with Unity 17.6 package templates (Simple Loop/Burst and Lit Basic), adapted for Topaz. Package templates are governed by their included Unity Companion License. Topaz-specific HLSL and gameplay adapters are authored in this repository. No sample audio is imported or substituted for the existing sound library.

## Synty POLYGON Starter Pack 1.2.1 — private dependency

Acquired through Unity Package Manager on 2026-09-26, product 156819. Standard Unity Asset Store EULA; raw files are ignored under `Assets/Synty/`. Selected files, source and license evidence, restore workflow and presentation changes are recorded in [SYNTY_SAMPLE.md](SYNTY_SAMPLE.md) and `scripts/synty-starter-files.json`. Original downloads and all existing audio archives remain preserved.

## Storybook wilderness additions (2026-09-26)

- Synty POLYGON Starter 1.2.1: expanded the selected dependency closure to natural
  environment, modular base, and camp props. Original archive unchanged; private
  source files and hashes are in `scripts/synty-starter-files.json`.
- [FREE Starter Pack - Sidekick Modular Characters by Synty](https://assetstore.unity.com/packages/3d/characters/free-starter-pack-sidekick-modular-characters-by-synty-336970),
  owned version 1.0.4, Standard Unity Asset Store EULA. Selected baked characters
  and dependencies only; no vendor editor/runtime scripts. Private under Assets/Synty.
- Kevin Iglesias Human Basic Motions FREE 2.4.2 and Human Crafting Animations FREE:
  account-owned downloads, animation-only dependencies. The publisher's
  [license statement](https://keviniglesias.com/assets/dwarfMeleeAnimationsFREE.html)
  applies the Standard Unity Asset Store EULA to all animation packs, including free
  versions. Public raw redistribution is prohibited; sources remain ignored under
  Assets/Kevin Iglesias. Attribution is appreciated, not required.
- `scripts/storybook-assets.json` records exact imported files, versions and hashes;
  restore using `python3 scripts/restore-storybook-assets.py` after account download.

## KayKit motion reused for Storybook

The owner-approved flexible-motion scope reuses **animation only** from the preserved
KayKit Character Animations 1.1 download. Its included License.txt explicitly grants
CC0 use, including commercial use and redistribution. Source:
https://kaylousberg.itch.io/kaykit-animations . The selected Rig_Medium CombatMelee,
CombatRanged, MovementAdvanced, Tools and General FBXs and original license are under
Assets/ThirdParty/KayKitMotion. No KayKit visible characters or environment art are used.
Topaz configures humanoid retargeting in Unity; original source archives remain intact.

Storybook sound uses the existing private Sonniss library: Tovusound grass footsteps,
Studio 23 gravel footsteps, Mindful Audio woodland birds and Soundopolis forest wind.
`Topaz/Audio/Configure Storybook Sound` derives short normalized/faded footstep clips
using Unity's audio decoder, records original hashes and sample ranges in the private
`Assets/ThirdParty/Sonniss/Derived/Storybook/SOURCE.md`, and streams the ambience beds.
Original WAV files and all source archives remain unchanged. The existing Sonniss
license applies; derived audio remains excluded from public source redistribution.

The validated humanoid `Hit_A` and `Death_A` clips are now saved as standalone CC0
`.anim` assets under Assets/Topaz/Characters/Animation/Clips. Their original General
FBX remains in LocalSourceArchives; removing its redundant imported copy avoids
import-time assertions from unsupported source tracks. The used curves passed the
finite-value regression before export. Other selected KayKit motion FBXs remain
imported with only the clips required by the controllers enabled.
