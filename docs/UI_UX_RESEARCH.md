# Topaz UI/UX research and current-flow audit

Reviewed 2026-09-24 for Unity 6000.6.2f1, uGUI 2.6.0, Input System 1.20.0, and the current Topaz checkout. Recheck package versions and linked English documentation at implementation time.

## Evidence that changes the design

| Primary source | Guidance | Topaz application |
| --- | --- | --- |
| [Unity 6.6 UI system comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/UI-system-compare.html) | Unity recommends uGUI for runtime UI and UI Toolkit for Editor UI in 6.6. Both support adaptive layouts and Input System navigation. | Keep the existing runtime Canvas/TMP foundation for this overhaul. Review a migration only if later menu volume makes UI Toolkit's global styling and document workflow worth the scene migration. |
| [uGUI Canvas Scaler](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/script-CanvasScaler.html) | Scale With Screen Size lays out against a reference resolution and adapts to the screen. | Keep the 1920×1080 design reference, add a separate player UI-scale preference, and test the smallest supported window as well as native Mac resolution. |
| [uGUI Selectable Navigation](https://docs.unity3d.com/Packages/com.unity.ugui@2.6/manual/script-SelectableNavigation.html) | Explicit links can set predictable directional focus; Unity can visualize the graph. | Inspect affected lists, grids, tabs, dropdowns and confirmations when navigation changes; use explicit focus links where spatial automatic navigation is unreliable. |
| [Unity UI optimization tips](https://unity.com/how-to/unity-ui-optimization-tips) | Changes on one Canvas can trigger costly batching work; separating static and frequently changing content and disabling decorative raycast targets can help. | Profile a representative build before splitting canvases. Keep decorative art non-interactive and avoid rebuilding whole menus for a small status update. |
| [Xbox text display](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/101), [contrast](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/102) | PC text guidance gives an 18-pixel body-height default at 1080p; standard important text and visual elements target at least 4.5:1 contrast, with 3:1 for large text. Stylized fonts should have a readable sans-serif alternative. | Reserve expressive lettering for short headings; use readable sans-serif text for controls, details, errors, and item descriptions. Measure contrast against the lightest/darkest part of illustrated backgrounds. |
| [Xbox UI navigation](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/112), [focus](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/113) | Menus should follow a consistent logical order, work with digital keyboard/controller input, and always show a clear focus indicator. | Define focus entry and return for every layer. Use an outline/shape plus color and keep selected controls in view when scrolling. |
| [Xbox errors and destructive actions](https://learn.microsoft.com/en-us/xbox/accessibility/xbox-accessibility-guidelines/115) | Make an error actionable and identify the consequence before a permanent action. | Explain Character/World deletion separately, default focus to Cancel, and keep save failures visible until resolved. |
| [Nielsen Norman Group usability heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/) | Show system status, use terms familiar to players, favor recognition over recall, and support recovery. | Prefer Character and World language over save-file internals; show costs and consequences at the point of action; avoid an always-visible controls manual. |

These are design targets for the PC-first game, not a claim that the current prototype meets them.

Use this research to resolve relevant design questions, not as a recurring audit checklist. Inspect changed flows and boundaries during ordinary work; the [workflow](AGENT_WORKFLOW.md#iterate-and-hand-off) determines when broader input/display review is warranted.

## Current application

The source references above guide the existing uGUI/TMP foundation. The current product contract is BASELINE.md; UI_DESIGN_SYSTEM.md supersedes the former authored-world screen audit. Preserve Character/World ownership, explicit confirmation for deleting collections, an unobstructed world view, clear focus and Back navigation, and persistent save-error feedback. No new UI framework is required.
