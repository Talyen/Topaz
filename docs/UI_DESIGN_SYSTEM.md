# Topaz UI design system

Status: first Unity implementation in review. The old UI was a functional
prototype, not the layout or visual reference for this system.

## Experience principles

1. **The world leads.** Ordinary exploration has no permanent location, day,
   resource, XP, or controls panel. The camera view should be nearly clear.
2. **Information appears when it helps a decision.** Show a small action chip
   beside the selected interactable, transient combat health when relevant,
   and brief event feedback for gathering or progression. Persistent save
   errors remain visible until resolved.
3. **Menus answer a purpose.** Inventory shows what is carried and what an
   item does. Crafting shows the result, cost, and available materials.
   Storage shows what can move between containers. Settings show the present
   value and effect of each choice.
4. **No controls manual on screen.** Do not add always-visible movement,
   combat, or menu-navigation instructions. The only in-world control label
   is the concise binding and verb for a currently available interaction.
5. **Focus is visible.** Mouse, keyboard, and gamepad must show a clear current
   choice, follow a predictable path, and return to the previous layer.

## Information architecture

| Situation | Surface | Information |
| --- | --- | --- |
| Title | Dedicated title composition | Continue the last Character and World, or Play to choose a Character and World; options and quit |
| Ordinary exploration | World only | No persistent overlay |
| Near an interactable | Chip anchored to that object | Current binding and short verb |
| Combat or recent damage | Temporary, compact vitality cue | Player health only while relevant |
| Backpack | Deliberately opened inventory | Items, quantities, capacity, selected item details |
| Crafting / storage | Deliberately opened workspace | Actions, costs, available items, resulting state |
| Pause | Quiet overlay over frozen world | Resume, inventory, options, main menu, quit |
| Options | Clear settings surface | Current settings and immediate feedback |

Day and resource totals belong in the backpack, crafting, storage, or an
eventual journal, according to the player's task. They do not occupy the
exploration view. The current health system is a transient combat prototype;
its display should follow that state rather than create a permanent HUD.

## Visual exploration

The owner chose grounded adventure styling and a nearly clear exploration
screen. Three generated explorations test complementary treatments:

- [Iron and brass title](design/explorations/title-iron-brass.png): a strong
  identity and clear initial selection over the game world.
- [Field journal backpack](design/explorations/backpack-field-journal.png):
  tactile organization and item inspection.
- [Cinematic pause](design/explorations/pause-cinematic.png): the world remains
  visible while a compact menu takes focus.

These are concept images, not screenshots or shippable scene art. They contain
illustrative world details that are not currently in Topaz and must not be
treated as promised content. Build the selected layout with native Unity UI
and assets whose redistribution rights are verified. The earlier contact
sheet based on the prototype overlay was withdrawn.

The recommended system uses a common typography scale, warm ivory text,
restrained brass selection cue, consistent spacing, and the same interactive
states across screens. Material treatment changes by purpose: iron for title
and settings, a journal for inventory/crafting/storage, and a quiet pause
overlay. The shipped journal background and Wood illustration were generated
separately from the concepts, with all live text and controls built in Unity.

## Unity implementation

Use the installed uGUI 2.6, TextMesh Pro, and Input System 1.20 for runtime
screens. Use shared component prefabs, normal Unity layout components, and
small view binding scripts. Keep gameplay decisions in the gameplay systems;
views display their state. UI Toolkit remains a strong choice for new Editor
tools and can be reevaluated for future menu-heavy features. Unity's
[6.6 UI comparison](https://docs.unity3d.com/6000.6/Documentation/Manual/UI-system-compare.html)
recommends uGUI for runtime and UI Toolkit for Editor UI.

The existing Canvas uses a 1920 x 1080 reference and balanced width/height
matching. Test 1280 x 720, 1600 x 900, and native Mac resolution; keep a
useful layout at each size. A visible focus state must work without pointer
hover. Decorative graphics should not intercept clicks. If profiling shows
Canvas rebuild cost in a representative player, separate frequently changing
content from static menus. See Unity's
[uGUI optimization guidance](https://unity.com/how-to/unity-ui-optimization-tips).

## Review gates

1. The owner selected the hybrid visual direction. Build the shared tokens and
   component states in Unity.
2. Re-author title, pause, backpack, crafting, storage, and options around
   player tasks. Remove the old overlay and generic controls text.
3. Keep the minimal in-world interaction chip tied to the same target and
   action decision used by gameplay. Show vitality only when damage or
   reduced health makes it relevant.
4. Run `./scripts/verify.sh`; inspect the Mac player at supported sizes and
   navigate mouse, keyboard, and gamepad flows. Use a Windows build when the
   change affects Windows compatibility.

The first implementation is ready for review when ordinary exploration is
clear, menus share a coherent identity, all actions remain reachable, and no
instructional controls text appears outside a relevant in-world chip.
