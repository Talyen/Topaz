# Unity feature selection

Topaz prefers Unity's maintained, first-party capabilities wherever they fit the game. This applies to gameplay foundations, authoring, rendering, visual effects, animation, audio, UI, input, assets, build tooling, and profiling. We do not write replacement infrastructure simply because custom code is possible.

For every substantial system or effect:

1. **Inventory the project and Editor version.** Check the installed Unity version, active render pipeline, packages, and existing scene patterns. Search the English Unity Manual, Scripting API, package documentation, samples, and release notes for a supported solution in that version. Do not infer availability from a tutorial for an older or future version.
2. **Prefer the smallest first-party fit.** Use a core engine feature or an already installed official package when it meets the need. Consider an additional official package only when its functionality is needed now. Use Unity's normal components, import settings, authoring tools, and package APIs before building parallel tools.
3. **Check the tradeoffs.** Verify platform support, production maturity, maintenance path, licensing and public-repository rights, ease of agent automation, upgrade behavior, accessibility and input support, and measured performance where relevant. A newer API is not automatically the better choice. Avoid preview or experimental features for a critical path without a clear benefit and a fallback.
4. **Write only game-specific rules.** Unity is an engine, not a complete action RPG. Item stack limits, loot rules, talents, chest permissions, and save migrations are Topaz rules. Keep those rules in small components or plain data behind stable IDs and interfaces to Unity features. Separate immutable authored assets from runtime state and keep save formats versioned.
5. **Make changes reversible.** Do not fork or patch engine/package source. Prefer serialized Unity assets and ordinary package APIs. Isolate any custom adapter so a future Unity feature can replace it without rewriting the game state or content. Record why a custom solution was chosen and what would trigger reevaluation.
6. **Revisit at upgrades and measured bottlenecks.** Review relevant new Unity features when upgrading the Editor and before generalizing a prototype. Test the specific project on Mac and Windows; performance claims require representative standalone scenes.

## Current application

| Need | Unity feature used | Topaz-specific code | Reason |
| --- | --- | --- | --- |
| Item authoring | `ScriptableObject` assets | Stable item IDs and stack limits | Definitions are shared and read-only at runtime. |
| Inventory persistence | Unity serialization and `JsonUtility` | Fixed slot rules, transfers, schema migration, atomic save replacement | Unity provides serialization, but Topaz defines item ownership and compatibility. |
| Inventory UI | Existing uGUI Canvas, Grid Layout Group, TextMesh Pro, Input System | Bind slot data and actions to the view | Matches the existing runtime UI and works with current controls. Review UI Toolkit at a larger UI milestone. |
| World and combat | GameObjects, CharacterController, NavMesh, URP | Authored interaction and combat rules | Existing first-party components meet the present scale. |

Unity's [UI system comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/UI-system-compare.html) describes uGUI and UI Toolkit as supported choices with different strengths. Its [ScriptableObject guidance](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ScriptableObject.html) supports shared authored data, while [Unity serialization](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization.html) covers the runtime fields used in saves. Use the [Package Manager](https://docs.unity3d.com/6000.6/Documentation/Manual/upm-ui.html) to inspect official packages and compatible versions. Use English documentation links in Topaz docs and handoffs.

The [Unity reference guide](UNITY_REFERENCE_GUIDE.md) maps the current 6000.6 Editor and installed package versions to task-specific sources. Unity 6.6 changes include [Play mode without domain reload](https://docs.unity3d.com/6000.6/Documentation/Manual/domain-reloading.html) and [serialized dictionary fields](https://docs.unity3d.com/6000.6/Documentation/Manual/script-serialization-dictionaries.html); check current project settings and migration needs before applying older advice.
