# Audio deletion pass 2 — approved and completed

Pass 1 is complete: 138 approved WAVs and metadata removed, 2.360 GB reclaimed, 890 unique Sonniss clips remaining (20.39 GB). The original source ZIPs remain untouched.

**Execution status:** The owner approved all three tiers. All 57 listed WAVs and their matching `.meta` files were deleted, reclaiming 4,502,161,964 bytes (4.502 GB). Both completed passes total 195 clips and 6.862 GB, leaving 833 WAVs (15.89 GB). The Editor was occupied with shader imports and its deletion requests failed/timed out; the exact approved file pairs were removed from disk after hash/reference preflight. Original ZIPs remain intact. The local source/hash record is `LocalSourceArchives/SonnissGDC/approved_audio_deletions_pass2.json`.

## Original proposal

This second pass reviewed the remaining filename/pack metadata. The more obvious mismatches were removed in pass 1; this list therefore distinguishes relatively direct cuts from creative/storage tradeoffs. It does not discard useful variations simply to hit a count.

## Summary

| Tier | Clips | WAV GB |
| --- | ---: | ---: |
| Recommend next | 23 | 0.280 |
| Optional further cuts | 28 | 0.739 |
| Aggressive storage option | 6 | 3.483 |
| All additional proposals | 57 | 4.502 |

All tiers are now approved and removed, leaving 833 Sonniss WAVs (15.89 GB). Sizes are source WAV bytes, not measured build savings. Source archives are outside this proposal.

## Exact deleted paths


### Recommend next: Electronic interface and designed effects

**Eiravaein Works - Start Select** — 1 clips. Camera-mechanism/servo UI treatment repeats the technological theme removed in pass 1; retain the strings and simple tonal alternatives.

- `Assets/ThirdParty/Sonniss/Selected/UI/2015/Eiravaein Works - Start Select-f0fd8d/StartSelect,UI,set57,camera mechanics,servo,clicks clacks,meaty,operative,forward.wav`

**Airborne Sound - Interface Accents** — 2 clips. Data chatter is less aligned with the fantasy Journal than simple clicks, bells, and tactile feedback.

- `Assets/ThirdParty/Sonniss/Selected/UI/2018/Airborne Sound - Interface Accen-7ef22c/Accent,Interface,UI,Data,Chatter,Clean,Crystal,Low.wav`
- `Assets/ThirdParty/Sonniss/Selected/UI/2018/Airborne Sound - Interface Accen-7ef22c/Accent,Interface,UI,Data,Chatter,Warble,Resonant,Clean,Pumping,Medium Slow.wav`

**CB_Sounddesign – ACTIVATION UI & HUD Sound Effects Library** — 1 clips. Explicit high electronic beeps; retain bell and less-specific notifications.

- `Assets/ThirdParty/Sonniss/Selected/UI/2018/CB_Sounddesign – ACTIVATION UI &-c7983b/BeepBeep_high.wav`

**2496SoundEffects - Whoosh Pack 1** — 1 clips. Electronic beep-whoosh is lower priority than organic swishes and magical movement.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/2496SoundEffects - Whoosh Pack 1-fcabb4/Whoosh Electronic Short Beep.wav`

**Glitchedtones - Impact** — 1 clips. Technology-specific designed impact; retain abstract, bass, and large-space impacts.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/Glitchedtones - Impact-ab1427/Impact - Tech Debris 12.wav`

**3maze - IMPACTUS** — 1 clips. Digital impact is a weaker fit than the pack’s ice and material impacts.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/3maze - IMPACTUS-6a21c8/digi_hit_008.wav`


### Recommend next: Modern locations, surfaces, and plumbing

**TS Sound - Savannah, GA Ambiences** — 1 clips. Parking-garage rain perspective is lower priority than the many outdoor rain and sheltered building recordings.

- `Assets/ThirdParty/Sonniss/Selected/World/2016/TS Sound - Savannah, GA Ambience-d32faf/AMB_EXT_Rain_Dripping_In_Parking_Garage.wav`

**2496SoundEffects - Rain in the City** — 1 clips. Urban concrete alley rain duplicates a role covered by natural rain recordings; audition-free relevance cut, not a quality judgment.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2019/2496SoundEffects - Rain in the C-12f85e/Rain On Concrete In Alley Splattering Dripping.wav`

**2496SoundEffects - Surround The Rain 5.1- Stereo** — 1 clips. Remaining clip is concrete/metal rain in 5.1; current forest/homestead weather already has other candidates.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/2496SoundEffects - Surround The-a4f011/Rain On Concrete, Drips On Metal_5.1 LCRLsRsLFE.wav`

**Double Trouble Audio - Rain and Thunder** — 1 clips. Plastic-surface rain is a narrower, less useful perspective than natural rain.

- `Assets/ThirdParty/Sonniss/Selected/World/2017/Double Trouble Audio - Rain and-621577/RATH - Rain on Plastic Outside Loop.wav`

**Tovusound - Edward – Foleyart Collection Add-On Extended Footsteps** — 1 clips. Asphalt surface is outside the authored forest, graveyard, crypt, and homestead palette; retain grass and rocks regardless of shoe labels.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2017/Tovusound - Edward – Foleyart Co-d2064c/015_Foley_Footsteps_Asphalt_Boot_Walk_Fast_Run_Jog_Close.wav`

**Studio 23 - Ultimate Footstep Collection** — 1 clips. Doormat scrapes have little demonstrated use; retain gravel/snow and boot movement.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2019/Studio 23 - Ultimate Footstep Co-35e37a/S23_SFX_Footsteps_Extras_Doormat_Scrapes_01.wav`

**Omar Alvarado - Household, kitchen and hotel sounds** — 2 clips. Sink switching and soap-pump/bathroom sequence are modern domestic actions; water pouring and splashing exist elsewhere.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2018/Omar Alvarado - Household, kitch-05cfde/sink, running, maximum stream of water, shut off.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/Omar Alvarado - Household, kitch-05cfde/washing hands inside a bathroom mostly empty soap pump sink on then off.wav`

**Sound Ex Machina - Kitchen Sounds 1.2** — 1 clips. Faucet recording is less directly useful than pouring water and natural flow; retain boiling and bottle/glass handling.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Sound Ex Machina - Kitchen Sound-e36979/Faucet Running Water 01.wav`

**TheLibrarybyEmptySea - Gateway Part1** — 2 clips. Parking-garage gate and bearproof trashcan are low-priority mechanisms; retain wooden doors, viewing slide, and generic deadbolt.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/TheLibrarybyEmptySea - Gateway P-25154a/basement_parking_garage_gate_metal_general_perspective_open_close_03.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/TheLibrarybyEmptySea - Gateway P-25154a/bearproof_trashcan_metal_open_close_10.wav`

**Bert Foley - Noisy Buildings V1** — 2 clips. Elevator-shaft glass destruction has a specific modern acoustic context; retain generic corridor metal impact.

- `Assets/ThirdParty/Sonniss/Selected/UI/2018/Bert Foley - Noisy Buildings V1-4e128d/Buildings_Rumble_Elevator_Shaft_Glass_Break_04.wav`
- `Assets/ThirdParty/Sonniss/Selected/UI/2018/Bert Foley - Noisy Buildings V1-4e128d/Buildings_Rumble_Elevator_Shaft_Glass_Break_Further_03.wav`


### Recommend next: Modern prop and creature-performance details

**Coll Anderson - Metal Plate** — 1 clips. Explicit tape-pull treatment has little current role; retain ordinary metal impacts/grinding.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Coll Anderson - Metal Plate-01bd12/EFX SD Metal plate duct tape pull 03 C.M.wav`

**Soundopolis - Water 01** — 1 clips. Smoking water pipe is a niche prop unrelated to current food/water/crafting systems.

- `Assets/ThirdParty/Sonniss/Selected/World/2015/Soundopolis - Water 01-33e6a6/WaterPipe_Smoke_Fienup_003.wav`

**Lukas Tvrdon - 6Monsters** — 1 clips. Dead-baby creature voice is an unusually specific horror theme with no planned enemy; keep other monster voices.

- `Assets/ThirdParty/Sonniss/Selected/Creatures/2018/Lukas Tvrdon - 6Monsters-b04542/6Monsters_DeadBaby_Talk_01.wav`


### Optional further cuts: Low-priority sound-design source recordings

**Ivo Vicic - Structure borne sound - metal resonances and textures** — 3 clips. Three overhead-power-line wind recordings total 314 MB. These may produce interesting supernatural textures, but are less immediately useful than direct wind, magic, and ghost sounds. Previously retained as material sources; now a lower-priority storage tradeoff.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Ivo Vicic - Structure borne soun-26b98f/03 Overhead power line in wind.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Ivo Vicic - Structure borne soun-26b98f/11 Overhead power line in wind.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Ivo Vicic - Structure borne soun-26b98f/08 Overhead power line in wind.wav`

**Mindful Audio - Metal Atmosphere** — 2 clips. Contact-mic wire-fence wind is specialized sound-design material rather than a ready outdoor ambience; direct wind and metal effects remain.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2017/Mindful Audio - Metal Atmosphere-6b865f/MAFX005 wire fence contact mic wind 19.WAV`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2017/Mindful Audio - Metal Atmosphere-6b865f/MAFX005 wire fence contact mic wind 23.WAV`

**Ivo Vicic - Bora wind - European local wind** — 1 clips. Wind resonating in wires is less useful than this pack’s grass, leaves, and trees; could still serve a dungeon texture.

- `Assets/ThirdParty/Sonniss/Selected/World/2018/Ivo Vicic - Bora wind - European-e06a9d/38 Wind_hurricane_wires_whistle.wav`

**RDGSFX008 - The Metal Shelf** — 1 clips. Rubber-band-on-shelf texture is niche; retain normal hits, creaks, movement, and scrape.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/RDGSFX008 - The Metal Shelf-bf7d29/Metal Shelf Rubber Band Hits 03.wav`

**SoundBits -  Just Whoosh 3 _ Whoosh Essentials** — 1 clips. Shaver-derived whoosh is lower priority than rope, leather, and rod alternatives; it may still be usable after audition.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2016/SoundBits - Just Whoosh 3 _ Whoo-8e6e66/Whoosh_ShaverPassBy_005.wav`

**TheLibrarybyEmptySea - Robobiotics** — 1 clips. Remaining robot-joint movement is more specialized than ordinary armor/chain/metal movement; keep the deep metal hatch for a heavy door.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/TheLibrarybyEmptySea - Robobioti-1a33e8/TheLibrarybyMTC_Robo_Foley_Joints_Mvmt_002.wav`

**Coll Anderson - Metal Plate** — 1 clips. Jet-like designed metal pass-by is a lower-priority effect than direct combat whooshes; not assumed to be an actual aircraft recording.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Coll Anderson - Metal Plate-01bd12/EFX SD Metal plate jet bys 02 A.wav`

**G4F SFX - SFX09 - Metal** — 1 clips. Mixer-in-vent contact recording is specialized processing material; keep direct rolling metal and designed earthquake.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/G4F SFX - SFX09 - Metal-5b5b15/G4F SFX09 - METAL - Mixer Type 2 in Metal Vent - Short - Contact.wav`

**SoundHolder -  Metal Contact** — 1 clips. Blender-derived lid scrape is a niche texture, though possibly usable without an audible motor; retain simpler lid hits and scrapes.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2017/SoundHolder - Metal Contact-e69b73/contact cast iron lid scraped with blender resonate and muted mono.wav`


### Optional further cuts: Niche comic or vocal treatments

**Eiravaein Works - Start Select** — 2 clips. Vocalized/toonish menu treatments may clash with a consistent tactile fantasy interface; no final sound style has been auditioned.

- `Assets/ThirdParty/Sonniss/Selected/UI/2015/Eiravaein Works - Start Select-f0fd8d/StartSelect,UI,set100,lady,vocalizations,crunchy,multipitched,multitimbral,back.wav`
- `Assets/ThirdParty/Sonniss/Selected/UI/2015/Eiravaein Works - Start Select-f0fd8d/StartSelect,UI,set93,talking tones,bright percussion,playful,toonish,light ambience,select.wav`

**InspectorJ - Hidden Voices- Flexatone** — 4 clips. Quivering, laughing, talking instrument voices are lower priority than ordinary character effort and monster vocalizations.

- `Assets/ThirdParty/Sonniss/Selected/UI/2019/InspectorJ - Hidden Voices- Flex-11fc2d/Flexatone_Quiver_09.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/InspectorJ - Hidden Voices- Flex-11fc2d/Flexatone_VoiceA_Laugh_Long_08.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/InspectorJ - Hidden Voices- Flex-11fc2d/Flexatone_VoiceA_Really_16.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/InspectorJ - Hidden Voices- Flex-11fc2d/Flexatone_VoiceD_Talk_20.wav`

**Articulated Sounds - Fun Monsters** — 1 clips. Explicit silly gibberish has no identified enemy role; keep the bestial groan and less-specific character cue.

- `Assets/ThirdParty/Sonniss/Selected/Creatures/2018/Articulated Sounds - Fun Monster-3c45e2/FUN MONSTER Jaja silly talk gibberish 10.wav`

**Articulated Sounds - Fight Vocalizations** — 1 clips. Child ninja exertion is a niche casting choice; retain adult combat effort/pain.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Articulated Sounds - Fight Vocal-f11b13/EMOTE Lily, Child, Anger Fight Ninja 01.wav`

**2496SoundEffects - Super Heroes Sound Design** — 1 clips. Web-shooting superhero effect has no current mechanic; retain arrow flight and shield hit. Could be useful if spiders gain a web attack.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/2496SoundEffects - Super Heroes-862073/The Web Slinger Shoots 4.wav`


### Optional further cuts: Further explicit gore

**Timothy McHugh - Gorification [HD]** — 4 clips. Further gut movement, blood-squirt, and gouging cues follow the tone of the approved explicit-gore cuts. Keep knife rings/swings and basic hits/cracks; these are not all interchangeable.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/Timothy McHugh - Gorification [H-5daa2c/gore - flesh guts gore impact blood squirt wet - medium - 104.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/Timothy McHugh - Gorification [H-5daa2c/gore - flesh guts gore movements wet - 22.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/Timothy McHugh - Gorification [H-5daa2c/gore - small metal blade flesh movement with rip and gouge wet - 1.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/Timothy McHugh - Gorification [H-5daa2c/gore - flesh guts gore impact blood squirt wet - hard - 49.wav`

**Articulated Sounds - Bones & Blood - Gore Elements** — 2 clips. Tortured squishy movement and flesh-pressure cues are more specific than routine combat. Keep cracks and dagger impact.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2018/Articulated Sounds - Bones & Blo-1f69b4/GORE Squeeze flesh with squealing air pressure.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2018/Articulated Sounds - Bones & Blo-1f69b4/GORE Wriggle tortured squishy slime.wav`

**MatiasMacSD - INFECTED ZONE** — 1 clips. Isolated dripping blood has no current persistent gore presentation; ordinary creature bite remains.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2016/MatiasMacSD - INFECTED ZONE-0f28cd/BloodDrips.wav`


### Aggressive storage option: Large dark drone source tracks

**Eneas Mentzel - DARK DEEPSEA DRONES** — 2 clips. Two source tracks total 889 MB. They could suit the crypt, so this reverses the first-pass retention only as an aggressive storage choice: retain direct cave/ghost ambience and recover from original archives if later wanted. Filenames do not prove redundancy.

- `Assets/ThirdParty/Sonniss/Selected/World/2017/Eneas Mentzel - DARK DEEPSEA DRO-f1c144/1 Into the Deep.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2017/Eneas Mentzel - DARK DEEPSEA DRO-f1c144/6 Lost in the Abyss.wav`


### Aggressive storage option: Large spatial forest recordings

**Soundreorganized - Ambisonic Forest - Spring** — 2 clips. Optional removal of the multi-channel spatial source candidates while keeping both stereo forest recordings from these packs and the broader forest library. These are DIFFERENT recordings, not duplicate encodings; removing them loses distinct scenes. No dedicated spatial-field playback requirement is established in current project plans.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/Soundreorganized - Ambisonic For-a28929/Forest_Spring_Afternoon_CloseToField_MediumToStrongWind_SqueakyTree_Birds_Tit_Amb_Atmo_FuMa.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/Soundreorganized - Ambisonic For-a28929/Forest_Spring_Evening_CloseToField_OpenArea_LightWind_Birds_Amb_Atmo_FuMa.wav`

**Soundreorganized - Ambisonic Forest - Summer** — 2 clips. Optional removal of the multi-channel spatial source candidates while keeping both stereo forest recordings from these packs and the broader forest library. These are DIFFERENT recordings, not duplicate encodings; removing them loses distinct scenes. No dedicated spatial-field playback requirement is established in current project plans.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/Soundreorganized - Ambisonic For-46b161/Forest_Summer_Morning_ActiveBirds_Woodpecker_Owl_Willowwarbler_AmbiX.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/Soundreorganized - Ambisonic For-46b161/Forest_Summer_Sunrise_NearLake_LightWind_Birds_GreatTit_Swan_Woodpecker_FuMa.wav`

## Retained priorities

Keep the existing Kenney/JaggedStone gameplay effects; core weapon impacts and whooshes; magic; ordinary enemy and animal sounds; wood/stone/metal/cloth crafting foley; food, fire, and water; outdoor birds and weather; and useful natural-region coverage. No blanket exclusion for tropical, coastal, snowy, underwater, alien, or gore names.

The four large spatial forest files and two deep-sea drones were deliberately retained in pass 1. Their inclusion here is an explicit, lower-confidence storage tradeoff, not new evidence that they are unsuitable.

## Validation of completed pass 1

- Verified all 138 approved WAV hashes/sizes and checked asset GUID references before deletion.
- Unity AssetDatabase reported 138 successful removals and zero failures. Confirmed every corresponding WAV and `.meta` is absent.
- Reconciled all 890 remaining WAV paths and sizes against the historical import manifests minus the completed deletion list.
- `./scripts/verify.sh`: seven tooling tests and both asset checks passed; EditMode tests could not start because Unity already has the project open. No full-gate or build pass is claimed.
- Connected Editor reported no compilation failure and zero current Console errors after deletion.
- `git diff --check`: passed.

Evidence: the two Sonniss import CSVs, completed pass-1 report, current filesystem, feature roadmap/status, and prior filename review. No audition or new audio import was performed.
