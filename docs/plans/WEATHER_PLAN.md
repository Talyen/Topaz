# Weather system design

Status: planning draft. The owner chose atmosphere-only weather and the first three conditions: clear, cloudy, and rain. This is a design and review plan, not an implementation request or an accepted visual target.

## Player experience

- Weather changes naturally during active play. A condition lasts long enough to establish a mood; changes blend rather than switch in one frame. No condition imposes damage, a combat modifier, a resource rule, or an expedition deadline in the first version.
- The existing day/night task supplies the authoritative world clock. Its starting target is one full day per **45 minutes of active play**. Menus, scene loads, application pause, and closed-game time do not advance it. Rest advances it by **eight in-game hours**, equivalent to 15 minutes of active-play cycle time. Weather uses that same clock and never runs a second timer for rest.
- Travel advances weather only through elapsed time reported by the travel/world-clock system. The current home-to-clearing transition has no time cost; a starting candidate for a later authored travel cost is 30 in-game minutes for that route. The travel design must approve that value. If a scheduled change falls within a rest or travel skip, show the condition at the destination time with a short arrival blend; do not replay every skipped transition.
- Home and the first clearing share one underlying weather schedule. A future region may change the presentation or temporarily force a condition; add a separate region schedule only when distinct local weather is an actual content need. A story event may temporarily force a condition. When it ends, the schedule at the current world time resumes.
- The environment communicates weather. A weather icon or forecast UI is not required for this atmosphere-only slice. The player can observe light, sky or distant haze where visible, precipitation, and sound. Combat tells, paths, pickups, and interactions stay readable at every condition and time of day.

## Decision model

One world owns one persistent ambient schedule. Rest, travel, scene loads, and event overrides are inputs to it, not independent weather rolls.

```text
World clock ──> ambient schedule ──> current condition
      │                                  │
      ├── rest/travel advances time       ├── region climate/presentation
      └── event start/end                 └── authored event override
                                              │
                                   environment presentation
                              light + fog + Volumes + rain + audio
```

1. **Ambient schedule:** At each scheduled boundary, choose the next condition and its duration from authored weights and minimum/maximum dwell times. Save enough schedule state to make reloads stable. Do not sample random weather every frame, on each scene load, or on each rest interaction.
2. **Climate:** For the first two outdoor regions, use the same schedule and presentation. Later, a region climate definition can adjust the presentation or force a local condition. Region entry must not reroll the world schedule. If separate local schedules become necessary, key and persist them by stable region ID. Local shelter affects the rain effect and sound at the player, not the world's condition.
3. **Event override:** An authored event can force clear, cloudy, or rain for a specified scope and lifetime. It takes priority over the ambient presentation while active. The ambient schedule continues against world time, so ending the event reveals the correct current condition. Define persistence with the event that owns the override; a transient cutscene override should not silently survive a reload.

The first tuning candidates are **one or two visible weather changes per full day**, with a minimum dwell long enough to avoid flicker during a typical expedition. Clear should be most common, cloudy next, rain occasional. These are review targets, not final probabilities. Avoid immediate clear-to-heavy-rain jumps by allowing cloudy as a lead-in, but permit an authored event to override that rule. Natural changes should take roughly 20–40 seconds of active play to blend; rest and travel can use a shorter arrival transition. Review these timings in a Mac build before locking values.

## State, authoring, and integration

| Owner | Responsibility |
| --- | --- |
| World clock from the day/night task | Expose monotonic absolute game time, active-play advancement, and explicit time jumps with old/new timestamps and a reason. Weather must use the implemented clock API rather than create another. |
| Weather coordinator | Keep ambient schedule state, select the logical condition, process clock jumps, and resolve event overrides. It lives with persistent world/session state, outside an individual outdoor scene. |
| Authored definitions | ScriptableObject assets with stable condition IDs, transition and dwell settings, climate weights, and presentation parameters. These are immutable definitions, not save data. |
| World save | Store logical weather state and schedule position or deterministic generator state in the versioned **World** record. Do not save particles, audio playback position, or transient Volume values. Old saves migrate to a clear condition with a valid next change. |
| Presentation adapter | Combine time-of-day, weather, region, and player graphics preferences into one set of light, fog, and URP Volume values. The current `VisualLookController` already writes the sun, fog, and color profile; the day/night task may change this code. Integrate with its final owner rather than let separate components write the same property each frame. |
| Region/presenter | Bind the active outdoor scene's local audio and precipitation emitters to the resolved condition. The persistent Bootstrap camera and main directional light remain shared during additive expedition loading. |

The current prototype stores a single `TopazSaveData` snapshot; `CHARACTERS_AND_WORLDS.md` proposes separate Characters, Worlds, and Visits. Weather belongs to the World so two Characters entering the same World encounter its current condition. Final field names and migration version must match whichever save structure exists when implementation starts. Use stable IDs so authored weather asset renames do not break saves.

The owner confirmed that a full day can pass while exploring. The [day/night design](../DAY_NIGHT_CYCLE.md) still lists natural-day tree regrowth as an open choice; reconcile that document and its save migration with the owner's latest direction. Weather needs the clock's absolute time and jump events but does not own the regrowth rule.

## Unity implementation choices

- Keep Unity 6.6 URP and the current fixed orthographic camera. Use the existing real-time directional light, environment fog settings, and [URP Volumes](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/Volumes.html) for restrained overcast and rain grades. Cloudy weather should primarily soften direct light and shift the ambient color; rain adds a slightly darker, cooler look. The home local Volume and player graphics choices still apply. URP does not provide native volumetric clouds or local volumetric fog, so those are outside this slice. [Unity pipeline comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/render-pipelines-feature-comparison.html)
- Author rain with bounded built-in [Particle Systems](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ParticleSystem.html) around the visible play area, plus sparse ground splashes if they improve the image. Test in motion at close, balanced, and far zoom, including TAA and SMAA, for distracting streaks, ghosting, and combat occlusion. A whole-region particle field is unnecessary for the fixed camera. The Visual Effect Graph package is not installed; its current [Unity 6.6 documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.visualeffectgraph.html) says full URP support is still in development. Reconsider it only if the built-in system fails a measured visual or performance need.
- Crossfade a rain ambience loop through Unity [Audio Mixer](https://docs.unity3d.com/6000.6/Documentation/Manual/class-AudioMixer.html) routing. Keep rain below enemy warnings, interaction cues, and music. Any imported sound must pass `ASSET_SOURCING.md` and be registered in `THIRD_PARTY_ASSETS.md` with redistribution rights before entering the public repository.
- Treat wind as an authored direction and strength shared by rain motion and audio. A [Wind Zone](https://docs.unity3d.com/6000.6/Documentation/Manual/class-WindZone.html) can drive supported trees and Particle Systems with External Forces, but it will not automatically animate arbitrary KayKit mesh foliage. Test the actual vegetation and shader before adding visible sway.
- Keep puddles, material wetness, lightning, snow, storms, shelter acoustics, and gameplay effects as later visual/content decisions. A small local shelter mask may be needed in the first rain study if the visible home roof otherwise shows rain passing through it.

## Implementation and review sequence

1. **Clock handoff:** Inspect the completed day/night clock, pause behavior, save schema, and environment writer. Record the event or query API weather will consume. Check the final rest/regrowth decision and additive-scene ownership.
2. **Controlled visual study:** Author clear, cloudy, and rain looks with a development-only condition selector. Produce comparable captures in home and clearing at day, dusk, and night, plus a short rainy combat clip. Review the fixed-angle view before adding the scheduler.
3. **Ambient schedule:** Add world-owned weather state, deterministic selection, minimum dwell, natural transitions, rest/travel jumps, save migration, and reload. Keep region climate and event override hooks small; wire one test override and one test region policy to prove precedence without building a generic event framework.
4. **Mac review build:** Play at 60 Hz through natural change, eight-hour rest, travel, region entry, event start/end, pause, save/reload, and a full 45-minute clock cycle. Tune how often rain appears, how long it lasts, transition speed, visual density, and audio mix. Give the owner the Mac build or visual comparisons for the final feel choice.
5. **Handoff:** Record the implemented first-party choices, small custom scheduler rationale, and upgrade path in `UNITY_FEATURE_POLICY.md`. Run `./scripts/verify.sh`, `./scripts/build.sh windows` for Windows compatibility, inspect console/build errors, and run `git diff --check`. Measure the Windows performance target only on the designated Windows PC with representative content.

## Acceptance checks

- Weather can change while the player explores; pausing menus, loading, application pause, and a closed game do not advance it.
- Rest advances the same clock by eight in-game hours. A rest or travel skip lands on the scheduled condition without a new random roll or intermediate effect replay.
- Save/reload and home/clearing travel preserve the same world weather. An old save starts from a valid migrated condition without changing unrelated progress.
- An event override takes priority for its defined lifetime and then returns to the ambient condition for the current time. Region entry follows its authored climate/presentation policy without rerolling.
- Clear, cloudy, and rain remain distinct during day, dusk, and night. Rain and fog do not hide enemy windups, paths, pickups, or interaction prompts at any supported zoom or anti-aliasing mode.
- Weather has no first-version gameplay consequences or forced expedition return.

## Review questions for the first Mac comparison

- Does rain feel like a welcome change at roughly one or two changes per day, or should conditions hold longer?
- Should rain at the homestead feel gentler than in the clearing, even when the logical condition is shared?
- Should a later forecast be available through an in-world object once weather affects gameplay, or does the environment alone communicate enough for this atmosphere-only version?

For comparable precedents, Sucker Punch describes *Ghost of Tsushima* as combining dynamic, regional, and story-linked weather with a day/night cycle in its [developer interview](https://blog.playstation.com/?p=329947). Minecraft Bedrock documents an ongoing [weather cycle](https://learn.microsoft.com/en-us/minecraft/creator/documents/bedrockeditor/editorworldoptions?view=minecraft-bedrock-stable) plus direct [weather changes and biome-dependent precipitation](https://learn.microsoft.com/en-us/minecraft/creator/commands/commands/weather?view=minecraft-bedrock-stable). Topaz uses the same broad categories of input, with timing and presentation tuned for its smaller authored world.
