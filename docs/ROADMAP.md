# Feature roadmap

This is Topaz's working list of features and the project systems needed to support them. It answers **what should we work on next?** without becoming a design spec or implementation plan.

## How to use this list

- Keep each feature to one line: a short name and the player-facing outcome.
- Order **Next** from highest to lowest priority and reorder it whenever priorities change.
- Put implementation details, acceptance criteria, and design exploration in a separate study document and link it here if useful.
- Delete a feature once it is complete and accepted. Git history is the archive.
- Add uncertain possibilities to **Ideas** without prioritizing or treating them as commitments; move them to **Next** when we decide to build them.

Example: `1. **Recoverable death** — Make defeat meaningful without erasing the player's world or long-term progress.`

## Next

The ordered feature backlog. The first item is the default choice for the next new piece of work.

1. **UI design system** — Establish a consistent visual language and reusable patterns for menus, HUDs, inventories, prompts, and controls.
2. **Asset approval and exclusion system** — Track which candidate assets are approved for Topaz or ruled out, with a short reason for each decision.
3. **Characters and worlds** — Let players create appearance-only Characters and enter any fresh or existing World. See [the data model](CHARACTERS_AND_WORLDS.md) and [selection plan](plans/CHARACTER_SELECTION_PLAN.md).
4. **Equipment system** — Equip, compare, and persist weapons, tools, armor, and other wearable items.
5. **Weapon types** — Add weapons with meaningfully different reach, timing, impact, and combat decisions.
6. **Tool types and gathering** — Expand beyond the axe and tree with distinct tools, resources, and gathering interactions.
7. **Deeper use-based progression** — Grow skills through play and make deliberate talent choices without fixed classes.
8. **Recoverable death** — Make defeat meaningful without erasing the player's world or long-term progress.
9. **Home-area dungeon entrance and first dungeon** — Add a visible entrance near the homestead leading to an authored challenge space with a clear reset rule and worthwhile rewards.
10. **Broader homestead building** — Let the player place and use more structures within designated home plots.
11. **Day and night cycle** — Move through readable times of day that change the world's ambience without forcing expedition timers.
12. **Weather** — Add weather states that make the world feel alive while preserving visibility and combat readability.
13. **Expanded authored regions** — Travel through multiple connected outdoor areas whose important changes persist.
14. **Campfire travel** — Discover campfires and use them as understandable travel points between visited areas.
15. **Gentle survival needs** — Add low-pressure needs that make preparation and returning home valuable.

## Ideas

Unsorted possibilities for brainstorming. Promote an idea only when we want it on the ordered backlog.

- **Pets** — Befriend or adopt companions that can accompany the player or live at the homestead.
- **Foliage wind** — Give grass, leaves, and small plants restrained motion that responds naturally to the environment.
- **Characters and quests** — Give the authored world inhabitants, relationships, and reasons to explore.
- **Homestead growth** — Let the safe home visibly develop as the player returns with resources and discoveries.
- **World audio and music** — Give regions, combat, and the homestead distinct sound identities.
