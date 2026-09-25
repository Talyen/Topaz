# Campfire travel design

Status: first implementation built for Mac owner review on 2026-09-25. This plan covers travel within one World. A broader exploration map is a later design decision when more connected regions exist.

## Player experience

- A Character may travel from any activated Campfire to any Campfire they have discovered in the same World, whether either fire is indoors or outdoors. Home, Graveyard, and Crypt follow the same rule. Discovery remains specific to one Character–World Visit; another Character in that World learns the network independently.
- Entering a hearth area still discovers and activates the Campfire automatically and sets the defeat return point. Interact at the fire opens the shared Journal with **Travel** on the left and **Cook** on the right. There is no return-to-fire command from the field. [Gentle survival](GENTLE_SURVIVAL_PLAN.md) defines cooking.
- Travel shows discovered destination rows with a short label. The current fire stays visible but disabled and marked **Here**. A destination can be clicked or activated with keyboard/gamepad Submit; moving focus through the list only highlights rows. Activating a destination starts travel immediately, without a second confirmation dialog. The Cook page shows the first recipe and its available ingredients.
- Travel is free, has no cooldown, and makes no World-clock jump. The World clock stays paused while the menu and covered transition are open. Health and harmful effects remain as they were; traveling is separate from rest and defeat recovery.
- Travel may be opened during combat. Opening the menu pauses player and enemy simulation. Back closes the menu and resumes the same fight. Once a destination is activated, control and damage are blocked through the covered move; enemies and projectiles in the departure region cannot hit the player during transition. Arrival at the destination counts as activating that Campfire, making it the new defeat return point.

## Screen and input flow

The Travel page asks **Where do I want to go?** The Cook page asks **What can I make here?** The shared Back action, Escape, and gamepad east Cancel close the Journal. Do not close on an outside click.

The earlier [names-only screen wireframe](../design/campfire-travel-wireframe.svg) records the travel hierarchy. The owner selected the existing Journal parchment for the later two-page Travel/Cook composition; all controls remain live uGUI/TMP elements.

```text
At an activated Campfire
  Interact: Open Campfire Journal (pause the fight and World clock)
    Travel (left): Home / Graveyard / Crypt [destination or Here]
    Cook (right): Mushroom Stew; Mushrooms available; Cook
    Back
  Destination click / Submit: covered travel, then resume play
  Back / Escape / gamepad east: close Journal, return focus to play, resume fight
```

Show only discovered fires. If the current fire is the only discovery, show its disabled row and focus Cook when available, otherwise Back. Focus normally enters the first enabled destination. A selected destination should have the same visible outline and readable label used by Topaz's current uGUI menu system; hover alone is not focus. At 100%, 125%, and 150% UI Scale, the list must scroll without clipping and keep the focused row visible. Opening the menu should not activate a destination from the same Interact press.

## State and content boundaries

| Source | Responsibility |
| --- | --- |
| Authored Campfire | Stable ID, region identity, display name, activation area, and safe arrival transform. A fire is the single source for its destination name and position. |
| Character–World Visit | Discovered Campfire IDs, current region and position, and most recently activated return Campfire ID. |
| World | Clock and persistent region changes. Travel changes no World time or rewards. |
| Travel coordinator | Validate source and destination; pause the menu; save departure state; cover asynchronous region load; move the player; clear transient combat actions and old-region projectiles; update Visit and return point only after successful arrival; save the result. |

The Home fire's existing **Improve campfire** interaction stays in the Home Journal so Interact at every fire consistently opens Travel/Cook. The upgrade remains available there with its current costs and progression.

Do not infer destination identity from a display name or scene index. The existing Campfire IDs and Visit discoveries are the starting point. As additional regions and fires arrive, build a validated destination registry from authored scene Campfires, rather than extending the current three-ID branch in `WorldSession`. The registry should reject duplicate IDs, missing scene/region references, or missing safe arrival points during development and build validation. Its display names can change without changing save IDs. It must make indoor fires first-class destinations.

The home Campfire remains the first Visit's default discovery and return point. Travel stays within the current World; switching Worlds remains the title's Character/World selection flow. A missing or removed **listed** destination must not silently send the player to Home. Keep the player at the source Campfire, restore input and combat safely, and explain that destination is unavailable. Defeat recovery retains its separate documented Home fallback for an invalid saved return ID. An interrupted or failed load must not change the Visit's region, position, or return point. A save failure remains visible under Topaz's persistent save-error rule.

## Map decision and Unity research

The first travel screen is a waypoint menu. Unity 6.6 supplies runtime UI through [uGUI](https://docs.unity3d.com/6000.6/Documentation/Manual/UI-system-compare.html), [URP camera output to a Render Texture](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/rendering-to-a-render-texture.html), and [asynchronous scene loading](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/SceneManagement.SceneManager.LoadSceneAsync.html). These are building blocks, not a complete in-game world-map system. [Tilemaps](https://docs.unity3d.com/6000.6/Documentation/Manual/tilemaps/tilemaps.html) author 2D levels; they would add a second representation of Topaz's 3D authored regions. The installed package list has no map package.

A hand-painted continuous map would require updates whenever region geometry or connections change. A top-down camera can render actual scene geometry, but foliage, roofs, stacked interiors, dynamic structures, and unloaded regions make one accurate, readable live image difficult. The first design therefore avoids implying geographic accuracy that the game cannot maintain automatically.

If a later exploration feature needs geography, compare these options against a concrete player question:

1. **Region and connection diagram:** derive nodes and links from authored region/entrance metadata and position Campfire markers from their stable IDs. This can stay accurate about destinations and connectivity, but is deliberately schematic about terrain.
2. **Per-region generated image:** capture the actual authored scene from a map camera during content builds and place markers from the same Campfire transforms. [Asset dependency hashes](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/AssetDatabase.GetAssetDependencyHash.html) can mark generated images stale when source assets change. This reflects authored scenery, but cannot by itself show player-built or other runtime changes; readability still needs review.
3. **Live rendered map:** render a loaded region from above. This can reflect live state in that region, but distant regions require loading or separate maintained snapshots. It is a poor first choice for an anywhere-to-anywhere travel screen.

Any future visual map should read destination IDs, labels, and discovery from the same travel data, not keep a second marker list. A generated image must fail its build check when stale. Do not promise exact live-world fidelity from a static capture.

## Review scenarios

1. A fresh Character sees a disabled Home row and **×**. Discover Graveyard and Crypt; their rows appear in Home, Graveyard, Crypt order, with only the current row disabled. Another Character entering the same World initially sees only Home.
2. Open Travel at the Crypt Campfire while a threat is active, cancel, and confirm that combat resumes. Open again and choose Graveyard; the same input that opened the menu must not choose a row.
3. Travel Crypt → Home and Home → Crypt without a World-time jump, resource charge, healing, or cleared harmful effect. Destination arrival becomes the defeat return point.
4. Reload after travel and confirm the Visit's region, position, discovered fires, and return point. An unavailable destination or failed region load leaves the Character at the departure fire with a clear error and no partial state change.
5. Review mouse, keyboard, and gamepad focus, **×**, Escape, and gamepad east behavior and UI Scale 100%/125%/150% at 1280×720, 1600×900, and native Mac resolution. Validate safe arrivals and covered loads in a Mac build; run `./scripts/verify.sh`, `./scripts/build.sh windows` when compatibility is affected, and `git diff --check` for implementation handoff.
