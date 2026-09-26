# Audio deletion pass 1 — approved and completed

**Execution status:** The owner approved both tiers. All 138 listed WAVs and their matching `.meta` files were deleted through Unity’s AssetDatabase. This removed 2,359,913,687 bytes (2.360 GB) of WAV source files and left 890 Sonniss clips. Original source ZIPs remain intact. The two import CSVs are historical acquisition manifests; this completed list records which paths are no longer staged. A local hash/source ledger is saved at `LocalSourceArchives/SonnissGDC/approved_audio_deletions.json`.

## Original proposal

Reviewed the filenames and pack names of all 1,028 unique recently staged Sonniss WAV files (22.75 GB), reconciled against the two selection manifests and actual files. Also reviewed the nine existing Kenney/JaggedStone Ogg filenames; none is proposed for deletion. At proposal time no audio was auditioned, deleted, renamed, or reclassified. The approved deletions have now been applied as recorded above. Historical selection manifests are unchanged.

The proposal follows Topaz’s survival-crafting fantasy action RPG direction: individual melee/ranged/magic combat, small encounters, homestead building, gentle survival, forest/graveyard/crypt regions, and future authored regions. Modern technology has little direct utility; material foley and supernatural source textures can have value regardless of the original recording object.

## Summary

| Proposal | Unique clips | WAV size (decimal GB) |
| --- | ---: | ---: |
| Recommended | 67 | 1.921 |
| Optional | 71 | 0.439 |

Counts exclude duplicate manifest records and do not include Unity metadata, import caches, or source ZIPs. These are source-file savings, not measured player-build savings. The original archives are outside this deletion proposal. No proposed clip GUID was found in first-party scenes, prefabs, assets, code, or ProjectSettings; this is a static reference check, not a runtime test.

## Category totals

| Tier | Category | Clips | MB |
| --- | --- | ---: | ---: |
| Recommended | Firearms and military technology | 5 | 10.3 |
| Recommended | Motors, appliances, and power tools | 16 | 149.5 |
| Recommended | Electronic devices and camera effects | 21 | 374.8 |
| Recommended | Modern activity or traffic ambience | 8 | 1297.4 |
| Recommended | Modern props and sports | 17 | 89.0 |
| Optional | Large battle crowds | 15 | 293.1 |
| Optional | Retro or explicitly technological sound design | 13 | 43.0 |
| Optional | Peripheral activities and props | 35 | 37.1 |
| Optional | Explicit gore and dismemberment | 8 | 65.6 |

## Exact suggested list

Each path below names one deleted WAV; its matching `.meta` was also removed. Both Recommended and Optional tiers were approved and completed.

### Recommended: Firearms and military technology

**2016 — Pole Position Production - Enfield L86 LSW 5.56mm** (2 clips). Modern rifle shots have no identified role in the sword, axe, staff, and crossbow combat palette.

- `Assets/ThirdParty/Sonniss/Selected/World/2016/Pole Position Production - Enfie-ac6511/Enfield_L86_LSW_5.56mm_1m_right_MKH8040_2_clean_Triple_shots_x_1.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2016/Pole Position Production - Enfie-ac6511/Enfield_L86_LSW_5.56mm_on_gun_DPA4062_clean_Triple_shots_x_1.wav`

**2018 — SoundMorph - STEAMPUNK WEAPONS** (2 clips). Explicit firearm/rocket shots; no such weapon family is in the current plans.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/SoundMorph - STEAMPUNK WEAPONS-408c48/Steampunk Weapons - Rocket Launcher - Shot - 04.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/SoundMorph - STEAMPUNK WEAPONS-408c48/Steampunk Weapons - Shotgun 2 - Shot - 03.wav`

**2018 — Soundrangers - Hydrology Underwater** (1 clips). Torpedo pass-by is an unlikely fantasy gameplay need.

- `Assets/ThirdParty/Sonniss/Selected/World/2018/Soundrangers - Hydrology Underwa-2ede18/torpedo_by_02.wav`


### Recommended: Motors, appliances, and power tools

**2015 — Sound Ex Machina - Kitchen Sounds 1.2** (3 clips). Explicit modern appliance recordings; keep this pack’s glass, bottle, water, and boiling sounds.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Sound Ex Machina - Kitchen Sound-e36979/Microwave Oven in Operaton 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Sound Ex Machina - Kitchen Sound-e36979/Espresso Machine 02.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Sound Ex Machina - Kitchen Sound-e36979/Refrigerator - Closing the Door 03.wav`

**2015 — Soundopolis - Tools:Construction 01** (3 clips). Powered construction equipment does not fit hand-tool gathering and homestead building; retain hammer and ambiguous demolition.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Soundopolis - Tools_Construction-7403f0/CircularSaw_Cut_Wood_Fienup_001.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Soundopolis - Tools_Construction-7403f0/PowerDrill_Run_Stop_Fienup_010.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2015/Soundopolis - Tools_Construction-7403f0/Jackhammer_Fienup_001.wav`

**2019 — Airborne Sound - Tools** (2 clips). Motor-driven workshop equipment; retain the sledgehammer recording.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Airborne Sound - Tools-82d4ce/Air Compressor,Omega,5 hp,20 Gallon,Twin Cylinder,Air Release,x2.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Airborne Sound - Tools-82d4ce/Saw,Circular,Porter-Cable,315,7 1 4 inch blade,16 tooth,On,Cut,Plywood,Off.wav`

**2018 — Airborne Sound - Kitchen** (2 clips). Electric grinder motor and microwave keypad beeps are unlikely cooking/crafting sounds.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/Airborne Sound - Kitchen-46577e/Coffee Grinder,Prosumer,Baratza,Maestro Plus,On,Run,Medium Grind,Off.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/Airborne Sound - Kitchen-46577e/Microwave,Panasonic,The Genius,Keypad,Press,Beeps,Various.wav`

**2019 — 3maze - Kitchenware** (1 clips). Motorized juicer; keep grill and glass sounds.

- `Assets/ThirdParty/Sonniss/Selected/UI/2019/3maze - Kitchenware-f719eb/juice_machine_running_base_001.wav`

**2018 — Omar Alvarado - Household, kitchen and hotel sounds** (1 clips). Explicit mains-electric relay/beep/buzz.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/Omar Alvarado - Household, kitch-05cfde/Electric power surge protector turned on with beep and loud relay clicks, pause, relay click with buzz2.wav`

**2015 — TheLibrarybyEmptySea - Robobiotics** (3 clips). Explicit motor, powered-window servo, and laser sounds; retain metal hatch and joint movement for potential mechanisms.

- `Assets/ThirdParty/Sonniss/Selected/World/2015/TheLibrarybyEmptySea - Robobioti-1a33e8/TheLibrarybyMTC_Robo_Motor_CoffeeGrinderB_001.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2015/TheLibrarybyEmptySea - Robobioti-1a33e8/TheLibrarybyMTC_Robo_Servo_PowerWindow_035.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2015/TheLibrarybyEmptySea - Robobioti-1a33e8/TheLibrarybyMTC_Robo_LaserBlast_Medium_027.wav`

**2015 — Coll Anderson - Deer** (1 clips). Powered saw on carcass is an especially poor fit; other hunting/butchery textures remain tone-dependent.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2015/Coll Anderson - Deer-7ec268/EFX INT Skil saw on carcass 01 B.wav`


### Recommended: Electronic devices and camera effects

**2018 — Fox Audio Post-Production - Electromagnetic – Field & Emissions** (4 clips). Device-specific electromagnetic recordings are low-priority sound-design source material for Topaz; existing magic and supernatural packs are more directly useful. This is a utility judgment, not an audition finding.

- `Assets/ThirdParty/Sonniss/Selected/World/2018/Fox Audio Post-Production - Elec-080eb5/EMF_Asus_Laptop.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/Fox Audio Post-Production - Elec-080eb5/EMF_Dishwasher.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/Fox Audio Post-Production - Elec-080eb5/EMF_Nose_Trimmer_01.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/Fox Audio Post-Production - Elec-080eb5/EMF_Toaster.wav`

**2019 — ktwAudio - Unheard Sounds Electromagnetic Fields** (4 clips). Device-specific electromagnetic recordings are low-priority sound-design source material for Topaz; existing magic and supernatural packs are more directly useful. This is a utility judgment, not an audition finding.

- `Assets/ThirdParty/Sonniss/Selected/UI/2019/ktwAudio - Unheard Sounds Electr-aaa52d/20_Guitar_Pedal_Marshall_EH1_Echohead_Mod_Filter_Setting.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/ktwAudio - Unheard Sounds Electr-aaa52d/81_Wireless_Landline_Telephone.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/ktwAudio - Unheard Sounds Electr-aaa52d/77_Mobile_Phone_Locked_Screen_On.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/ktwAudio - Unheard Sounds Electr-aaa52d/21_Ford_Focus_In_Motion_Windscreen_Wipers.wav`

**2019 — Glitchedtones - Electromagnetic Field** (4 clips). Device-specific electromagnetic recordings are low-priority sound-design source material for Topaz; existing magic and supernatural packs are more directly useful. This is a utility judgment, not an audition finding.

- `Assets/ThirdParty/Sonniss/Selected/World/2019/Glitchedtones - Electromagnetic-0eb05b/EMF Android Box 02.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/Glitchedtones - Electromagnetic-0eb05b/EMF Cellphone Loop 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/Glitchedtones - Electromagnetic-0eb05b/EMF Extractor Fan Box 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/Glitchedtones - Electromagnetic-0eb05b/EMF Monitor Speaker Abuse 01.wav`

**2019 — Soundbox Library - Electromagnetic Fields Collection** (2 clips). Device-specific electromagnetic recordings are low-priority sound-design source material for Topaz; existing magic and supernatural packs are more directly useful. This is a utility judgment, not an audition finding.

- `Assets/ThirdParty/Sonniss/Selected/World/2019/Soundbox Library - Electromagnet-7721e7/EMF_051_Smartphone.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2019/Soundbox Library - Electromagnet-7721e7/EMF_090_DVD_Player.wav`

**2019 — 2496SoundEffects - Hollywood Cameras** (3 clips). Designed camera shutter/beep sequences have no identified gameplay role.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/2496SoundEffects - Hollywood Cam-7e5fe5/Camera Shutter Click Design Double Trouble 5.1 (LCRLsRsLf).wav`
- `Assets/ThirdParty/Sonniss/Selected/UI/2019/2496SoundEffects - Hollywood Cam-7e5fe5/Camera Shutter Click Design Rise And Quick Stereo.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/2496SoundEffects - Hollywood Cam-7e5fe5/Camera Shutter Click Design Simple Beep And Shutter Click 5.1 (LCRLsRsLf).wav`

**2018 — soniKSound - Data Streams** (3 clips). Data-stream and system shutdown interface theme is a poor match for the fantasy Journal/inventory.

- `Assets/ThirdParty/Sonniss/Selected/World/2018/soniKSound - Data Streams-fa70bb/KS-DST - Alert 1 - Loop A.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/soniKSound - Data Streams-fa70bb/KS-DST - Data Stream Confirm 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/soniKSound - Data Streams-fa70bb/KS-DST - System 3 - Shutdown C.wav`

**2019 — Sound Spark LLC – Forge Sound Design Toolkit** (1 clips). Computation glitches are not forge/blacksmith sounds despite the pack name.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Sound Spark LLC – Forge Sound De-ebebb9/Device_Computation_Glitches_03.wav`


### Recommended: Modern activity or traffic ambience

**2018 — VSCRL FX - The Snow** (4 clips). Filenames explicitly describe ski/snowboard/sled crowds, chairlift hum, or a highway. Keep other clean snow, ice, and wind recordings.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/VSCRL FX - The Snow-f12850/EXT_Day_Snow_Hill_FastSledBys_Walla_Laughter_Playing_AmbiX.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/VSCRL FX - The Snow-f12850/EXT_Day_Snow_SkiLiftEntrance_ChairliftHumClose_SporadicDistinctVoices_7.1.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/VSCRL FX - The Snow-f12850/EXT_Day_Snow_SkiSlope_MidSlope_MediumToCloseSkiAndSnowboardBys_MediumSpeed_HeavyWalla_5.1.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2018/VSCRL FX - The Snow-f12850/EXT_Sunset_Mountain_DistantHighway_DistantRiver_SteadyWind_Stereo.wav`

**2019 — Coll Anderson - Music Drive Bys** (1 clips). Music-playing car drive-by; the word Blood appears to have pulled an unrelated recording into the gore queue.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2019/Coll Anderson - Music Drive Bys-486f79/EFX EXT Music Car By 30 MPH Blood In 03.wav`

**2019 — 2496SoundEffects - Coastal Ambience and Beaches** (1 clips). Theme-park crowd recording, not natural coastal ambience.

- `Assets/ThirdParty/Sonniss/Selected/World/2019/2496SoundEffects - Coastal Ambie-e64f25/Roller Coaster Voices Cheers.wav`

**2019 — 2496SoundEffects - Surround The Rain 5.1- Stereo** (1 clips). Rain inside a parked car has a vehicle-specific perspective.

- `Assets/ThirdParty/Sonniss/Selected/World/2019/2496SoundEffects - Surround The-a4f011/Rain On Auto, Interior Parked, Window Closed 2_5.1 LCRLsRsLFE.wav`

**2018 — Fox Audio Post-Production - Countryside – Nature & Field** (1 clips). Filename identifies tractor contamination; cleaner nature alternatives exist.

- `Assets/ThirdParty/Sonniss/Selected/Creatures/2018/Fox Audio Post-Production - Coun-e683fe/Amb_Countryside_Evening_Birds_Cow_Rooster_Tractor_Far_LP01.wav`


### Recommended: Modern props and sports

**2015 — Mechanical Wave - Foley Session 01** (3 clips). Audiotape, disposable can, and zippered case are low-priority props; retain chain and generic metal movement.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Mechanical Wave - Foley Session-465e05/Zipper Case_VS 02.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Mechanical Wave - Foley Session-465e05/Audiotape Manipulation_VS 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Mechanical Wave - Foley Session-465e05/Aluminum Can Drop On Floor_VS 02.wav`

**2015 — Soundopolis - Foley Plus_Full** (2 clips). Disposable cans and modern lighter; retain frying and cork pop.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Soundopolis - Foley Plus_Full-2a5ce9/Lighter_Blue Flame_E_Light_Fienup_001.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Soundopolis - Foley Plus_Full-2a5ce9/Cans_Aluminum_Rummage_Fienup_002.wav`

**2018 — Articulated Sounds - Essential Kitchen** (1 clips). Modern beverage can; retain knife sharpening and drawer.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2018/Articulated Sounds - Essential K-fb2d82/KITCHEN soda can opening pouring.wav`

**2019 — 3maze - Kitchenware** (1 clips). Modern beverage can.

- `Assets/ThirdParty/Sonniss/Selected/UI/2019/3maze - Kitchenware-f719eb/draft_guinness_can_open_001.wav`

**2016 — Kevin Durr - Ice cubes & Ice Blocks** (2 clips). Modern plastic/foam props; retain ice chipping and steps.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2016/Kevin Durr - Ice cubes & Ice Blo-59d5da/Twisting Ice Tray 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2016/Kevin Durr - Ice cubes & Ice Blo-59d5da/Walking with styrofoam ice cooler 1.wav`

**2016 — Pole Position Production - The Snow & Ice Textures Library** (2 clips). Sport-specific skiing recordings; retain wind and walking on snow.

- `Assets/ThirdParty/Sonniss/Selected/World/2016/Pole Position Production - The S-44877c/snow_details_ski_drop_02.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2016/Pole Position Production - The S-44877c/snow_skiing_rougher_slalom_groomed_runs_harder_surface_03_handheld.wav`

**2017 — Articulated Sounds - Ice Skating** (3 clips). Hockey/skating actions have no planned role; retain the generic skater cloth movement.

- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Ice Skating-abe3cf/ICE Hockey stick medium long scrape 22.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Ice Skating-abe3cf/ICE Hockey stick short scrape 87.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Ice Skating-abe3cf/ICE Skate move combination 13.wav`

**2016 — Filmnoise - Unusual Footsteps and Movements Vol.1** (2 clips). Modern floor surfaces and footwear; the wooden-deck struggle remains a plausible foley source.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2016/Filmnoise - Unusual Footsteps an-e9fc1d/SNEAKER PLASTIC FLOOR MAT FOOTSTEP WALKING.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2016/Filmnoise - Unusual Footsteps an-e9fc1d/SOCKS LINOLEUM FOOT SCRAPE STOMP STRUGGLE FIGHTING BEING PUT IN A REVERSED CHOKE HOLD GROUND WORK.wav`

**2018 — Bert Foley - Noisy Buildings V1** (1 clips). Explicit plastic building-piece rattle; other structural sounds may support collapses.

- `Assets/ThirdParty/Sonniss/Selected/UI/2018/Bert Foley - Noisy Buildings V1-4e128d/Buildings_Rumble_Stairs_Plastic_Piece_Rattle.wav`


### Optional: Large battle crowds

**2015 — Coll Anderson - Battle Crowd** (12 clips). Group battles, marching, cries, and celebrations suggest larger populated conflicts than the current single-player, 1–10-enemy encounters. Future settlements could still use them.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Call Out Help Make Weapons 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Marching Puddle Mud 01.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle End Agony Moans 02 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle Approach 01 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle Steady Fighting 03 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle Movement Forrest 01 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Female Celebration Scream 01 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Agony Scream 02 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle Celebration 02 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Unrest Murmur 01 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Battle Cry 03 A.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2015/Coll Anderson - Battle Crowd-8196cf/EFX EXT GROUP Begging 02.wav`

**2019 — Rock The Speakerbox - Hero** (1 clips). Group fight ambience is unnecessary for current small encounters.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Rock The Speakerbox - Hero-1d4188/HERO - CK - CROWD Fight 02.wav`

**2019 — Coll Anderson - The Battle Crowd Collection Add on** (2 clips). Crowded indoor battle/party movement is lower priority than individual combat and foley.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Coll Anderson - The Battle Crowd-db267e/ADR Battle In House 02.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Coll Anderson - The Battle Crowd-db267e/EFX INT FS House Party Movement Fast 06 C.wav`


### Optional: Retro or explicitly technological sound design

**2019 — Impact Soundworks - Super FX 8 and 16-bit Video Game SFX** (4 clips). Retro 8/16-bit treatment may clash with the tactile-plus-fantasy audio brief; audition if a stylized UI is desired.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Impact Soundworks - Super FX 8 a-187b26/Combat_Triple_Combo_Hit.wav`
- `Assets/ThirdParty/Sonniss/Selected/Magic/2019/Impact Soundworks - Super FX 8 a-187b26/Magic_Teleport.wav`
- `Assets/ThirdParty/Sonniss/Selected/UI/2019/Impact Soundworks - Super FX 8 a-187b26/UI_Level_Up.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2019/Impact Soundworks - Super FX 8 a-187b26/Misc_Glass_Crystal_Shatter.wav`

**2018 — Bluezone Corporation - Electrostatic Field** (4 clips). Pack theme is electronic, but generic impacts/textures could be repurposed for magic; weaker filename-only deletion case.

- `Assets/ThirdParty/Sonniss/Selected/World/2018/Bluezone Corporation - Electrost-cd9d42/Bluezone_BC0235_ambience_sfx_013.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/Bluezone Corporation - Electrost-cd9d42/Bluezone_BC0235_impact_003.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/Bluezone Corporation - Electrost-cd9d42/Bluezone_BC0235_synth_texture_002.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2018/Bluezone Corporation - Electrost-cd9d42/Bluezone_BC0235_transition_008.wav`

**2018 — Airborne Sound – Light and Dark Drones** (1 clips). Cyber-themed drone may be redundant with more directly supernatural atmospheres.

- `Assets/ThirdParty/Sonniss/Selected/Magic/2018/Airborne Sound – Light and Dark-30e048/Drone,Cyber,Deep,Pulse,Disturbing,Invasive,Loop.wav`

**2018 — SoundMorph - STEAMPUNK WEAPONS** (2 clips). No steampunk weapon plan; however Firestorm might serve fire magic, and the other name is ambiguous.

- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/SoundMorph - STEAMPUNK WEAPONS-408c48/Steampunk Weapons - Back From The Past - set_2.wav`
- `Assets/ThirdParty/Sonniss/Selected/Combat/2018/SoundMorph - STEAMPUNK WEAPONS-408c48/Steampunk Weapons - Firestorm Flamer - set_1.wav`

**2015 — RDGSFX007 - Rips and Tears v.2** (1 clips). Robot mechanism is unlikely unless repurposed for a trap or creature.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/RDGSFX007 - Rips and Tears v.2-f89012/RIPS AND TEARS DESIGNED Robot Arm Rotation.wav`

**2015 — SoundMorph - Bloody Nightmare** (1 clips). Robotic rather than organic enemy cue; could still become a supernatural voice.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/SoundMorph - Bloody Nightmare-774e94/Bloody Nightmare - Horror Impacts - Robotic Scream.wav`


### Optional: Peripheral activities and props

**2017 — Articulated Sounds - Dice** (4 clips). No dice/minigame mechanic is planned; could still become inventory or tabletop foley.

- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Dice-d8ce98/DICE on Felt, Throw and Roll, Standard, 16 Sixteen Dice, v1.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Dice-d8ce98/DICE on Felt, Throw and Roll, Standard, 5 Five Dice, v2.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2017/Articulated Sounds - Dice-d8ce98/DICE on Hard Wood, Throw and Roll , Standard, 2 Two Dice, v1.wav`
- `Assets/ThirdParty/Sonniss/Selected/World/2017/Articulated Sounds - Dice-d8ce98/DICE on Neoprene, Throw and Roll, Mini, 1 One Die, v2.wav`

**2019 — Cfry – Hand Tools** (2 clips). Modern toolkit-specific handling is less directly useful than hammer, anvil, and material sounds.

- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Cfry – Hand Tools-33e04e/socket wrench 5.wav`
- `Assets/ThirdParty/Sonniss/Selected/SurvivalCrafting/2019/Cfry – Hand Tools-33e04e/toolbox shuffling 3.wav`

**2017 — Resonance Sound Design - ASSORTED FOLEY ELEMENTS** (1 clips). Toilet vomiting is unlikely for the benefit-only, low-pressure survival design.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2017/Resonance Sound Design - ASSORTE-9ce3d0/BATHROOM_CHUNKY_VOMIT_INTO_TOILET.WAV`

**2018 — The Sound Pack Tree - Footstep Loops** (1 clips). Specific casual footwear is low priority; could still substitute for sandals.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2018/The Sound Pack Tree - Footstep L-7cfba2/1707 - Footsteps - Flip-Flops - 140 fpm - Loop.wav`

**2015 — Timothy McHugh -  Barefoot on Metal** (27 clips). A 27-clip barefoot-on-metal family has limited current surface/footwear coverage value. Do not trim individual variations arbitrarily; optionally remove this entire niche family.

- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Run Barefoot Male - 14.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Jump Barefoot Male - 11.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Walk Barefoot Male - 15.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Land Barefoot Male - 5.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Run Barefoot Male - 21.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Walk Barefoot Male - 2.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Walk Barefoot Male - 3.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Run Barefoot Male - 12.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Walk Barefoot Male - 13.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Rattle Barefoot Male - 18.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Walk Barefoot Male - 24.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Jump Barefoot Male - 3.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Run Barefoot Male - 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Run Barefoot Male - 14.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Walk Barefoot Male - 2.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Run Barefoot Male - 17.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Walk Barefoot Male - 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Jump Barefoot Male - 17.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Land Barefoot Male - 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Slide Barefoot Male - 16.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Walk Barefoot Male - 27.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Plate Slide Barefoot Male - 11.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Walk Barefoot Male - 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Grill Run Barefoot Male - 17.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Land Barefoot Male - 1.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Run Barefoot Male - 17.wav`
- `Assets/ThirdParty/Sonniss/Selected/Movement/2015/Timothy McHugh - Barefoot on Met-6635ec/FOOTSTEP - Metal Board Run Barefoot Male - 10.wav`


### Optional: Explicit gore and dismemberment

**2018 — SoundMorph - Gore** (2 clips). Explicit head explosion/disembowel cues exceed demonstrated combat needs; keep if visceral combat is intended.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2018/SoundMorph - Gore-0d816d/GORE - Head_Explode_6.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2018/SoundMorph - Gore-0d816d/GORE - WEAP - Disembowel_Gore_5.wav`

**2015 — Coll Anderson - Deer** (3 clips). Detailed carcass processing is optional until hunting/butchery tone is established.

- `Assets/ThirdParty/Sonniss/NeedsReview/World/2015/Coll Anderson - Deer-7ec268/EFX INT Leg skinning 01 B.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2015/Coll Anderson - Deer-7ec268/EFX INT Blood spurt on carcass 01 B.wav`
- `Assets/ThirdParty/Sonniss/NeedsReview/World/2015/Coll Anderson - Deer-7ec268/EFX INT Full Skin pull 01 C.wav`

**2015 — RDGSFX007 - Rips and Tears v.2** (1 clips). Explicit gut tearing is optional; paper/cloth/bread tears remain useful.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2015/RDGSFX007 - Rips and Tears v.2-f89012/RIPS AND TEARS DESIGNED Humanoid Guts 04.wav`

**2016 — Game Audio Factory - GAFSFX01 - Dark Materials** (1 clips). Flowing guts is a more specific gore cue than ordinary combat impacts.

- `Assets/ThirdParty/Sonniss/NeedsReview/SurvivalCrafting/2016/Game Audio Factory - GAFSFX01 --dce04d/GAF SFX01 - HORROR - Guts flowed on wood surface, light.wav`

**2019 — PMSFX - BLOODBATH** (1 clips). Cinematic flesh gush is optional pending combat tone.

- `Assets/ThirdParty/Sonniss/NeedsReview/VisceralGore/2019/PMSFX - BLOODBATH-eca586/PM_BB_DESIGNED_CINEMATIC_TEXTURE_FLESH_GUSH_1.wav`

## Deliberately retained

- Forest, weather, rivers, coasts, jungle, snow, caves, birds, and animal sounds: future authored regions are open-ended; region names alone do not justify cuts.
- The six large Soundreorganized forest recordings (3.23 GB): strong genre fit despite size; consider later excerpts or import tuning rather than filename-only deletion.
- Dark deep-sea drones and underwater textures: plausible crypt/magic ambience even without underwater gameplay.
- Bone cracks, slime, ordinary wet impacts, generic gore hits, and creature voices: potentially useful for Skeletons, monsters, and combat; no blanket gore or alien-name exclusion.
- Wood, stone, metal, leather, chains, hand tools, cooking, doors, and glass: material usefulness matters more than modern source-object names.
- Existing Kenney/JaggedStone effects and variations within relevant families.

## Local evidence

- `docs/studies/SONNISS_GDC_AUDIO_SELECTIONS.csv`
- `docs/studies/SONNISS_GDC_AUDIO_REVIEW_QUEUE.csv`
- `docs/studies/SONNISS_GDC_AUDIO_REVIEW.md`
- `docs/FEATURE_STATUS.md` and `docs/ROADMAP.md`
- `AGENTS.md` and `docs/UNITY_REFERENCE_GUIDE.md`

This was a filename-based relevance review, not a claim about sonic quality or audible contamination. The approved deletion has now been applied through Unity; validation is recorded in the pass-2 report.
