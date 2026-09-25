# Topaz UI/UX research and current-flow audit

Reviewed 2026-09-24 for Unity 6000.6.2f1, uGUI 2.6.0, Input System 1.20.0, and the current Topaz checkout. Recheck package versions and linked English documentation at implementation time.

## Evidence that changes the design

| Primary source | Guidance | Topaz application |
| --- | --- | --- |
| [Unity 6.6 UI system comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/UI-system-compare.html) | Unity recommends uGUI for runtime UI and UI Toolkit for Editor UI in 6.6. Both support adaptive layouts and Input System navigation. | Keep the existing runtime Canvas/TMP foundation for this overhaul. Review a migration only if later menu volume makes UI Toolkit's global styling and document workflow worth the scene migration. |
| [uGUI Canvas Scaler](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/script-CanvasScaler.html) | Scale With Screen Size lays out against a reference resolution and adapts to the screen. | Keep the 1920×1080 design reference, add a separate player UI-scale preference, and test the smallest supported window as well as native Mac resolution. |
| [uGUI Selectable Navigation](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/script-SelectableNavigation.html) | Explicit links can set predictable directional focus; Unity can visualize the graph. | Audit each list, grid, tab row, dropdown, and confirmation rather than relying on spatial automatic focus everywhere. |
| [Unity UI optimization tips](https://unity.com/how-to/unity-ui-optimization-tips) | Changes on one Canvas can trigger costly batching work; separating static and frequently changing content and disabling decorative raycast targets can help. | Profile a representative build before splitting canvases. Keep decorative art non-interactive and avoid rebuilding whole menus for a small status update. |
| [Xbox text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101), [contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102) | PC text guidance gives an 18-pixel body-height default at 1080p; standard important text and visual elements target at least 4.5:1 contrast, with 3:1 for large text. Stylized fonts should have a readable sans-serif alternative. | Reserve expressive lettering for short headings; use readable sans-serif text for controls, details, errors, and item descriptions. Measure contrast against the lightest/darkest part of illustrated backgrounds. |
| [Xbox UI navigation](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112), [focus](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113) | Menus should follow a consistent logical order, work with digital keyboard/controller input, and always show a clear focus indicator. | Define focus entry and return for every layer. Use an outline/shape plus color and keep selected controls in view when scrolling. |
| [Xbox errors and destructive actions](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/115) | Make an error actionable and identify the consequence before a permanent action. | Explain Character/World deletion separately, default focus to Cancel, and keep save failures visible until resolved. |
| [Nielsen Norman Group usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/) | Show system status, use terms familiar to players, favor recognition over recall, and support recovery. | Prefer Character and World language over save-file internals; show costs and consequences at the point of action; avoid an always-visible controls manual. |

These are design targets for the PC-first game, not a claim that the current prototype meets them.

## First principles for Topaz

Topaz alternates a safe, warm homestead with voluntary expeditions into authored regions. The interface should preserve that rhythm: ordinary play shows the world; a nearby choice appears at its object; an opened workspace supplies the facts needed for that task. The Journal backdrop remains. The title should introduce warm refuge; current title, pause, colors, and typography may be reconsidered.

For each surface, answer in order:

1. What is the player trying to decide or do now?
2. Which one action should read first, and what cost or consequence does it have?
3. What supporting information makes that action safe and understandable?
4. What can wait behind a deliberate menu or detail selection?
5. What happens on empty data, loading, failure, cancellation, and Back?
6. Where does keyboard/controller focus enter and return, and how does a pointer user see the same state?

## Implemented surfaces and transitions

This map comes from the current `GameMenus`, `CharacterWorldMenu`, `LoopHud`, Journal views, `VisualOptionsMenu`, `WorldSession`, and `HomeBuilds`. It is the redesign checklist; future game concepts are outside this pass.

| Surface or state | Entry and primary decision | Exit or notable edge |
| --- | --- | --- |
| Title | Continue last Character/World, or Play to choose; Options and Quit | Continue unavailable without a valid pair; save problem is visible. Standalone player starts here, while Editor Play mode enters a test pair. |
| Character list | Pick an existing Character or New Character | Back returns to title; deletion confirmation removes Character progress and visits but leaves Worlds. |
| Appearance picker | Select one of the playable looks using live preview, rotate preview, create Character draft | Back discards draft; creation is committed only after Enter World succeeds. |
| World list | Pick existing World or fresh World, then Enter World | Back returns to Character or appearance picker; World deletion preserves Characters; entering can report a save/load error. |
| Exploration | Move, aim, fight, gather, approach interactables | No permanent data HUD. Interaction chip shows one current verb/binding; health appears after damage or while reduced. |
| Event/status feedback | Pickup, XP, travel, rest, campfire, build, combat, and errors | Ordinary feedback is brief; save problems remain visible. |
| Backpack | View 16 carried slots, capacity, selected detail; toggle lantern | Open by inventory action or Pause; close from Pause returns to Pause. Tabs lead to Equipment and Skills. |
| Equipment | Inspect portrait, seven slots, backpack candidates, stat preview; equip/unequip and choose manual tool | Full backpack can block unequip; two-handed swaps affect offhand. Tabs return to Backpack/Skills. |
| Skills | Inspect levels, XP, talents; learn or activate at home | Locked, learned, active, and away-from-home states differ. |
| Workbench | Craft chest or begin Stone path/Anvil placement, edit home | Costs/availability matter; a crafted chest may remain pending after cancellation. |
| Storage chest | Compare backpack and chest; deposit/withdraw materials; move gear one at a time | Full backpack and unavailable transfers need explicit feedback. |
| Gear rack | Claim an available World-owned item | Claimed items show Taken; full backpack leaves reward in place. |
| Placement/edit | Aim at valid home spot to place chest/path/Anvil, or select a build to remove/refund | Confirm/cancel and invalid location feedback; no menu stays open. |
| Travel/rest/recovery | Enter/leave expedition, rest, discover campfire, recover after defeat | Fade and short status indicate transition; save/region failure may return home. |
| Pause | Resume, Backpack, Options, Main Menu, Quit | Escape resumes; closing Backpack opened here returns to Pause. |
| Options | Display mode, window size, Graphics, UI Scale, Back | Same surface from title and Pause, but Back returns to its source. Settings apply immediately and persist locally. |
| Graphics | Camera zoom, anti-aliasing, depth of field, Bloom, ambient occlusion, reset | Back restores Options focus; errors report failed preference writes. |

## Review and acceptance method

Review the title first with two warm-refuge compositions. Then agree on type, color, spacing, controls, and focus. For each later screen, record a low-fidelity information hierarchy, the approved visual, a state/transition sketch, and the exact input paths. Validate in a standalone Mac build with mouse, keyboard, and gamepad at 1280×720, 1600×900, and native resolution, at UI Scale 100%, 125%, and 150%. Run the project's verify and Windows build gates and report any input/device checks that could not be performed.
