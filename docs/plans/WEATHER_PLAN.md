# Weather system design

Status: implemented in the first Mac review build; visual and audio values remain prototype targets. The owner chose atmosphere-only weather and the first three conditions: clear, cloudy, and rain.

## First implementation

- `WeatherSchedule` derives ambient weather from the World ID and saved absolute world hours. Its stable hash selects clear, cloudy, or rain in staggered eight-hour periods; the first eight hours of a new World are clear. Reload, rest, travel, and another Character visiting the same World see the same condition without new save fields or a separate weather clock. No format migration is needed because existing saves already contain both inputs.
- `WorldSession` resolves story override, region override, then ambient weather. Overrides are explicit runtime APIs; the owning story or region must clear them when finished. The first home and clearing use the shared ambient condition. Successful voluntary travel advances the clock by 30 in-game minutes in each direction; restoring a save or returning after defeat does not charge that time.
- `VisualLookController` composes cloud and rain with the existing day/night light, ambient fill, fog, URP grading, rest fade, and player graphics preferences. `WeatherPresentation` creates a bounded Particle System around the player and an original generated rain ambience loop. The rain loop now uses filtered noise without sharp random clicks, and its AudioSource follows the Ambient slider. A reviewed authored sound can replace the generated loop when the world-audio system arrives.
- Natural changes blend over 30 seconds of active play. Rest changes the condition under its existing dark transition. The isolated Mac preview accepts `-runTests -weather-preview=clear|cloudy|rain -weather-preview-hour=12` so reviewers can compare conditions without touching their normal save.
- Clear and rain were visually inspected in the Mac player at midday: rain reads at gameplay scale while the player, bedroll, trail marker, and nearby ground remain visible. The white streaks, audio texture, transition length, and condition frequency still need owner review in motion. No third-party weather asset was imported.

## Player experience

- Weather changes naturally during active play. A condition lasts long enough to establish a mood; changes blend rather than switch in one frame. No condition imposes damage, a combat modifier, a resource rule, or an expedition deadline in the first version.
- The existing day/night task supplies the authoritative world clock. Its starting target is one full day per **45 minutes of active play**. Menus, scene loads, application pause, and closed-game time do not advance it. Rest advances it by **eight in-game hours**, equivalent to 15 minutes of active-play cycle time. Weather uses that same clock and never runs a second timer for rest.
- Successful voluntary travel between home and clearing advances the same world clock by 30 in-game minutes. Restoring a save and returning after defeat do not charge travel time. If a scheduled change falls within a rest or travel skip, show the condition at the destination time; do not replay every skipped condition.
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

1. **Ambient schedule:** A stable hash of World ID and an eight-hour time period selects clear, cloudy, or rain. The first eight world hours are clear. The World ID and world time already persist, so reloads and skips produce the same condition without another random roll or save field.
2. **Climate:** For the first two outdoor regions, use the same schedule and presentation. Later, a region climate definition can adjust the presentation or force a local condition. Region entry must not reroll the world schedule. If separate local schedules become necessary, key and persist them by stable region ID. Local shelter affects the rain effect and sound at the player, not the world's condition.
3. **Event override:** An authored event can force clear, cloudy, or rain for a specified scope and lifetime. It takes priority over the ambient presentation while active. The ambient schedule continues against world time, so ending the event reveals the correct current condition. Define persistence with the event that owns the override; a transient cutscene override should not silently survive a reload.

The first tuning target is **one or two visible weather changes per full day**. The current eight-hour periods give clear a 47% selection weight, cloudy 33%, and rain 20%; repeated selections extend a condition. The first natural transition takes 30 seconds of active play, while rest applies the destination look under its dark fade. These values are prototypes. Direct clear-to-rain transitions can occur; the visual blend softens them. Review frequency and pacing in the Mac build before locking them.

## State, authoring, and integration

| Owner | Responsibility |
| --- | --- |
| World clock | `WorldSession` already owns saved monotonic `worldHours` and the active-play/rest rules. Weather reads it and adds no timer. |
| Weather coordinator | `WorldSession` resolves the ambient schedule and optional event/region overrides. It lives outside the additively loaded clearing. |
| Authored definitions | The first three stable condition IDs and tuning values live in `WeatherSchedule` and `WeatherPresentation`. Move them to ScriptableObject assets when visual review shows which values need repeated content-author tuning. These are immutable definitions, not save data. |
| World save | The existing stable World ID and `worldHours` determine ambient weather. Do not save particles, audio playback position, or transient Volume values. Existing saves need no new migration fields. |
| Presentation adapter | `VisualLookController` combines time of day, weather, rest fade, and player graphics preferences before writing the shared sun, fog, and URP Volume values. |
| Presenter | One persistent `WeatherPresentation` follows the player with bounded rain particles and ambience in either outdoor region. The Bootstrap camera and main directional light remain shared during additive expedition loading. |

The current collection already has separate Characters, Worlds, and Visits. Weather belongs to the World so two Characters entering the same World encounter its current ambient condition. Temporary overrides belong to their event or region owner and clear when the active Character/World pair changes.

The owner confirmed that a full day can pass while exploring. The implemented [day/night cycle](../DAY_NIGHT_CYCLE.md) uses the same absolute clock for 72-hour tree regrowth. Weather reads that time but does not own the regrowth rule.

## Unity implementation choices

- Keep Unity 6.6 URP and the current fixed orthographic camera. Use the existing real-time directional light, environment fog settings, and [URP Volumes](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/Volumes.html) for restrained overcast and rain grades. Cloudy weather should primarily soften direct light and shift the ambient color; rain adds a slightly darker, cooler look. The home local Volume and player graphics choices still apply. URP does not provide native volumetric clouds or local volumetric fog, so those are outside this slice. [Unity pipeline comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/render-pipelines-feature-comparison.html)
- Author rain with bounded built-in [Particle Systems](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ParticleSystem.html) around the visible play area, plus sparse ground splashes if they improve the image. Test in motion at close, balanced, and far zoom, including TAA and SMAA, for distracting streaks, ghosting, and combat occlusion. A whole-region particle field is unnecessary for the fixed camera. The Visual Effect Graph package is not installed; its current [Unity 6.6 documentation](https://docs.unity3d.com/6000.6/Documentation/Manual/com.unity.visualeffectgraph.html) says full URP support is still in development. Reconsider it only if the built-in system fails a measured visual or performance need.
- The first prototype crossfades an original generated rain loop through one AudioSource. Route authored ambience through Unity [Audio Mixer](https://docs.unity3d.com/6000.6/Documentation/Manual/class-AudioMixer.html) when the broader world-audio system arrives. Keep rain below enemy warnings, interaction cues, and music. Any imported sound must pass `ASSET_SOURCING.md` and be registered in `THIRD_PARTY_ASSETS.md` with redistribution rights before entering the public repository.
- Rain currently has a slight fixed slant; foliage wind is deferred. A future [Wind Zone](https://docs.unity3d.com/6000.6/Documentation/Manual/class-WindZone.html) can drive supported trees and Particle Systems with External Forces, but it will not automatically animate arbitrary KayKit mesh foliage. Test the actual vegetation and shader before adding visible sway.
- Keep puddles, material wetness, lightning, snow, storms, shelter acoustics, and gameplay effects as later visual/content decisions. A small local shelter mask may be needed in the first rain study if the visible home roof otherwise shows rain passing through it.

## Review and handoff sequence

1. **Visual comparison:** Use isolated preview launches to compare clear, cloudy, and rain in home and clearing at day, dusk, and night, including rainy combat. Tune rain streaks, audio, and visibility in a Mac build at 60 Hz.
2. **Pacing review:** Play through natural changes, eight-hour rest, voluntary travel, pause, and save/reload. Decide whether eight-hour periods and 30-second blends feel right in a normal session.
3. **Handoff:** Record the implemented first-party choices, small custom schedule rationale, and upgrade path in `UNITY_FEATURE_POLICY.md`. Run `./scripts/verify.sh`, `./scripts/build.sh windows`, inspect console/build errors, and run `git diff --check`. Measure the Windows performance target only on the designated Windows PC with representative content.

## Acceptance checks

- Weather can change while the player explores; pausing menus, loading, application pause, and a closed game do not advance it.
- Rest advances the same clock by eight in-game hours. A rest or travel skip lands on the scheduled condition without a new random roll or intermediate effect replay.
- Save/reload and home/clearing travel preserve the same world weather. An older collection derives a valid condition from its existing World ID and saved time without changing unrelated progress.
- An event override takes priority for its defined lifetime and then returns to the ambient condition for the current time. Region entry follows its authored climate/presentation policy without rerolling.
- Clear, cloudy, and rain remain distinct during day, dusk, and night. Rain and fog do not hide enemy windups, paths, pickups, or interaction prompts at any supported zoom or anti-aliasing mode.
- Weather has no first-version gameplay consequences or forced expedition return.

## Review questions for the first Mac comparison

- Does rain feel like a welcome change at roughly one or two changes per day, or should conditions hold longer?
- Should rain at the homestead feel gentler than in the clearing, even when the logical condition is shared?
- Should a later forecast be available through an in-world object once weather affects gameplay, or does the environment alone communicate enough for this atmosphere-only version?

For comparable precedents, Sucker Punch describes *Ghost of Tsushima* as combining dynamic, regional, and story-linked weather with a day/night cycle in its [developer interview](https://blog.playstation.com/?p=329947). Minecraft Bedrock documents an ongoing [weather cycle](https://learn.microsoft.com/en-us/minecraft/creator/documents/bedrockeditor/editorworldoptions?view=minecraft-bedrock-stable) plus direct [weather changes and biome-dependent precipitation](https://learn.microsoft.com/en-us/minecraft/creator/commands/commands/weather?view=minecraft-bedrock-stable). Topaz uses the same broad categories of input, with timing and presentation tuned for its smaller authored world.
