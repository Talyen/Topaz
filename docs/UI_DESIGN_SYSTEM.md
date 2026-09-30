# UI design system

Keep the existing uGUI/TextMesh Pro foundation: live functional text, an unobstructed world view, contextual cues, visible focus and a clear Back route. [UI research](UI_UX_RESEARCH.md) records design sources; [baseline](BASELINE.md) owns gameplay and [roadmap](ROADMAP.md) tracks unfinished review.

## Menus and Characters/Worlds

Standalone play starts at the title menu; Editor Play uses a temporary test pair. Continue resumes the last valid Character–World pair. Play selects or creates a Character, then a World. Back from an unfinished draft creates nothing. New visits start at the permanent starter camp.

Deleting a Character removes their visits and carried progress but preserves Worlds. Deleting a World removes its visits and World-owned state but preserves Characters. Explain the consequence, confirm destructive collection actions, and default confirmation focus to Cancel. Persistent save errors remain visible and distinct from brief event feedback.

Escape opens pause/options or dismisses the active overlay. Overlays own focus; select activates and cancel returns to the invoking layer. Keyboard, mouse and Input System gamepad navigation must retain predictable entry/return and keep selected controls visible while scrolling.

The creator offers ten curated Viking appearances through the existing scrollable choice list, using stable appearance IDs shared by live previews and gameplay. No equipment statistics or classes are attached to the choices.

The title and creator use a neutral backdrop with a front-facing perspective Viking preview. The old camp diorama is removed. This is an interim presentation, not a new title-screen design; future title concepts remain separate work in temporary managed artifacts until approved.

## Journal and construction

Use the selected illustrated book, ink-on-paper rows, restrained forest selection tint, gold focus outlines and readable live costs. Desktop panels use dark forest tones and warm text. Ink-on-paper is the current default, not a claim of owner approval of every screen.

Inventory, equipment, skills, active effects, forging, building and travel share this language. The two-column Build page offers campfires, bedrolls, modular pieces, storage and furniture throughout the wilderness. Show recipe costs and backpack/nearby-saved-storage availability; unavailable recipes explain missing materials. Moving/removing is separate from placement, which returns the player to the world to aim. Invalid placement needs both a distinct preview and an explanation. The current camp's travel row says Here.

## Contextual interaction and gathering

One action chip appears beside the nearest eligible target within **1.4 m**, using planar distance and authored interaction anchors. Binding labels are authored from the Input System and switch after meaningful keyboard/gamepad input. Hide the chip during menus, travel/rest/recovery, placement, dodge, airborne motion, committed attacks and off-screen targets. Do not add per-object Canvases or permanent controls text.

`WorldSession.TryGetInteraction` and Interact use the same eligible candidate query, including tool-assisted trees and mining rocks. The chip projects beside the actual target in the fixed isometric-style perspective view. Empty active caches can show **Inspect empty cache**. The selected chest is resolved from that candidate, rather than a second proximity query.

Tool-assisted interaction can temporarily equip the required tool, aim and execute the normal strike/recovery, then restore the previous weapon on completion or cancellation. Temporary selection is not persisted; repeated presses during a committed swing do not queue another strike. Tool ownership is already checked, including the pickaxe requirement for rocks. Manual tool attacks remain available; yields and strike counts come from the authored definitions.

## Visual quality and iteration

Judge the actual changed flow for hierarchy, spacing, typography, material/art cohesion, readable focus and a clear primary action. Inspect at normal player scale, correct obvious weak layouts and awkward transitions, and describe specific deficiencies instead of calling an isolated capture polished. Iterate in the Editor; use a completed Mac batch when the full title-to-game route or standalone behavior matters. Ordinary refinements within the selected direction do not require another design approval.

## Display and review

Retain display/audio controls, UI Scale, Balanced/High quality and camera zoom. Graphics separates quality from focus mode, with Golden / Silver or Natural lighting, native/adaptive AA, bloom/AO toggles and effect adjustments. Focus modes are Off, Distant softness and Cinematic bokeh; the neutral preview camera uses its own lighting and excludes world post-processing.

For a local edit, inspect the changed flow, its affected input route and the relevant small-window/UI-scale boundary. Shared scaling/navigation changes and release review require the full affected screen matrix at 100%, 125% and 150% scale, including 1280×720, 1600×900 and native resolution, with keyboard/mouse and gamepad. Keep text legible over illustrated backgrounds and use shape/outline as well as color for focus. Automated navigation checks do not substitute for physical-device review.

Gameplay uses WASD/left stick to move and cursor/right stick to aim. Attack is left mouse/right trigger; guard is right mouse/left trigger; dodge is Shift/East; jump is Space/South; interact is E/West. Journal is B/View; pause is Escape/Menu. Shoulder buttons and mouse wheel zoom. Construction uses left mouse/South to confirm, R/right-stick press to rotate and Escape/East to cancel. Gameplay cursor is visible and confined, and overlays release it. Placement and overlay dismissal consume gameplay input to prevent jumping, attacking or dodging through the same press. Ranged weapons use a world-space destination marker, with warm red obstruction feedback.

There is no runtime rebinding screen. If introduced, refresh cue labels after binding overrides change. Target highlights or discovery hints require a scoped design choice rather than permanent HUD instructions.
