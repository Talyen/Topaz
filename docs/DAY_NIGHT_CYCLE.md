# Day and night cycle design

Status: implemented in the first Mac review build; dusk and night sun, ambient fill, and fog were lowered in the lantern lighting pass. Visual values remain prototype targets.

## Player experience

- Time advances continuously during active play through a medium-length cycle. Start tuning at **45 minutes of active play per full day**. Menus and scene loading pause the clock. Closing the game does not advance it.
- Dawn, daylight, dusk, and night change the outdoor lighting and ambience. Night is moody but always readable: the player, enemies, attack tells, paths, pickups, and interactions remain identifiable without a carried light.
- Time of day creates no enemy, combat, or expedition deadline rules in this first version. The player returns from expeditions by choice. Tree regrowth is the one gameplay rule tied to elapsed world time.
- The environment communicates time. Do not add a clock, day counter, or time-of-day HUD. The existing home bedroll can be used at any time and advances the clock by **eight in-game hours**, which is **15 minutes of cycle time** at the 45-minute starting speed. Resting is a time skip, so it should use a short visual transition rather than animating eight hours of light movement.
- Keep indoor or authored dungeon lighting locally controlled when those spaces arrive, while the same world clock continues advancing during active play.

## First visual targets

Use these as review points, not fixed lighting values. The 45-minute cycle maps one in-game hour to 1 minute 52.5 seconds of active play.

| Phase | In-game hours | Active play at 45-minute cycle | Visual goal |
| --- | --- | --- | --- |
| Dawn | 04:00–07:00 | 5 min 37.5 sec | Cool shadows give way to warmer light. |
| Day | 07:00–19:00 | 22 min 30 sec | Current readable daylight remains the baseline. |
| Dusk | 19:00–22:00 | 5 min 37.5 sec | Warm light falls gradually; home lights become more prominent. |
| Night | 22:00–04:00 | 11 min 15 sec | Cool ambient fill preserves navigation and combat readability. |

Begin a new game, and migrate existing saves, at 08:00. These phase boundaries and colors should be tuned with screenshots and a Mac player review at Topaz's gameplay camera.

## Unity and Topaz approach

Unity 6.6 and URP provide [real-time lights](https://docs.unity3d.com/6000.6/Documentation/Manual/LightModes-introduction.html), [environment lighting](https://docs.unity3d.com/6000.6/Documentation/Manual/LightingOverview.html), and [Volumes](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/volumes-landing-page.html). They do not supply Topaz's rest, pause, region, and save rules. Use those native rendering features and a small Topaz-specific clock, without adding a package or custom renderer feature.

- Keep one authoritative world clock outside the individual outdoor scenes. Outdoor presentation reads that clock through authored color and intensity curves for the existing directional light, ambient fill, fog, and restrained URP color adjustments. Favor one main shadow-casting directional light until a representative build shows a need for more.
- Compose the time-of-day look with the current `VisualLookController`, which already owns the home light, shadow strength, fog distance, and runtime Volume profiles. Graphics preferences remain preferences; the cycle supplies a time-dependent baseline.
- The Bootstrap scene owns the current main directional light and global Volume. The Expedition scene loads additively; its lighting must follow the same clock and should not reset or create a competing sun during travel. Treat interiors as scene-authored overrides later.
- Use an explicit gameplay-active condition for clock advancement. The current `GameMenus` pauses `Time.timeScale` for main menus in standalone builds, but crafting, chest, and ordinary inventory panels need coverage too; Editor play mode currently leaves time scale at 1. Pause during additive loading and on application pause. Keep frame-rate independent clock and lighting updates.
- Save monotonic elapsed world time through a versioned save migration, deriving time of day and the cycle count from it. Persist clock changes on rest, travel, pause, quit, and periodically during uninterrupted play. Do not use wall-clock time while the game is closed.

## Rest and regrowth

The first tree should regrow **72 in-game hours after harvest**, regardless of how those hours pass. At the initial cycle speed, that is 135 minutes of active play without resting. Each eight-hour rest counts toward the same elapsed time; nine immediate rests span 72 hours. Crossing a dawn has no special effect on the timer. Time spent in menus, during loading, or with the game closed does not count.

Replace the tree's saved `nextAvailableDay` threshold with an absolute ready time on the new monotonic world clock. Existing saves need a migration that preserves outstanding progress: compute the remaining old rest increments from `nextAvailableDay - day`, then convert each remaining increment to an eight-hour skip at the migrated clock time. Already available trees stay available. New harvests use the full 72-hour rule. This keeps existing players from losing earned regrowth progress while making the new rule consistent for future harvests. Retire the old `day` field only when older save migration no longer needs it; do not reuse it with a different meaning. Remove the current rest status text that announces a day number and "three days."

## Review and acceptance

1. Make four representative captures in home and expedition at the gameplay camera: dawn, day, dusk, and night. Check paths, player silhouette, enemy windups, pickups, and home lights at default and far camera zoom.
2. In a Mac player build, watch at least one transition through dusk and dawn on a 60 Hz display. Check for flicker, exposure jumps, shadow movement, and fog or Volume discontinuities.
3. Verify that 45 minutes of active play completes one cycle, that every menu and scene load pauses it, that rest advances exactly eight in-game hours across midnight, and that save/load restores the same look. Verify tree regrowth after 72 elapsed in-game hours through normal play, rest skips, and a mixture of both. Verify migration of available, partially regrown, and not-yet-regrown trees from old saves.
4. Run `./scripts/verify.sh`, a Mac review build, `./scripts/build.sh windows` if Windows build compatibility is affected, and `git diff --check`. Measure Windows performance only on the designated Windows PC with representative content.

## Visual review choices

- At visual review, tune the 45-minute cycle length, phase boundaries, night brightness, and whether ambience audio is ready for this slice. Audio assets need the source and redistribution review in [ASSET_SOURCING.md](ASSET_SOURCING.md) before import.

The first implementation changes lighting, ambient fill, fog, and existing URP color adjustments. It includes a brief dark transition for rest. It does not add ambience audio because this slice has no approved day or night audio assets yet.
