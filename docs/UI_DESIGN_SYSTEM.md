# UI design system

Keep the existing uGUI/TextMesh Pro Journal, its selected backdrop, readable typography and visible focus frames. The baseline cleanup changes availability and labels; it does not commission a new visual style.

Exploration favors an unobstructed world view and contextual interaction cues. Menus answer the player's immediate question: inventory contents, equipment effects, recipes/materials, build placement or travel destination. Persistent save errors stay visible and must not be confused with brief event feedback.

The Build page offers campfire, bedroll, modular pieces, storage and furniture in a two-column layout in both outdoor regions. Display costs and active-region material totals. Moving/removing remains separate from placement; the player returns to the world to aim. Invalid placement has a distinct preview and explanatory message.

Overlays own focus. Enter/select activates; cancel/back dismisses to the invoking layer. Mouse, keyboard and gamepad use the same actions. Keep functional text live, maintain contrast, and verify layout/focus at 100/125/150% scale. See UI_UX_RESEARCH.md for source references, and BASELINE.md for current gameplay intent.

## Storybook treatment

The implementation uses the selected illustrated Journal backdrop with ink-on-paper
rows, restrained forest selection tint, gold focus outlines and readable live costs.
The owner was shown the forest-card prototype and asked for a preference; in the
absence of a reply, ink-on-paper is the stated default under the granted creative
freedom. Disabled build recipes explicitly say they need materials. Current camp
rows say Here. Inventory, equipment, skills, status, forging and travel share this
Journal language; desktop menu panels use dark forest tones with warm text.
