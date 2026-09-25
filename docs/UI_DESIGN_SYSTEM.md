# Topaz UI design system

Status: full redesign in progress as of 2026-09-24. The first implementation
below records earlier decisions; its layout and color treatment are now
references, not constraints. The owner chose a warm-refuge first impression,
screen-by-screen review, a playable Mac build, and a 100%/125%/150% UI Scale
control without a reduced-motion setting. The
[research and current-flow audit](UI_UX_RESEARCH.md) is the active screen
checklist. Preserve the shipped Journal backdrop.

## Redesign method and review record

For each implemented screen or transient state, record (1) the player's current
question, (2) the primary action and its consequence, (3) supporting facts,
(4) empty/loading/error states, (5) focus entry, directional path, Back, and
return focus, and (6) the approved wireframe and visual treatment. Review the
screen with the owner before applying its treatment in Unity. The owner need
only choose between concrete visuals; agents handle Unity and code work.

The title comes first. Its information order is: Topaz identity and safe-world
feeling; Continue when a valid last Character/World pair exists; Play to choose
a pair; Options; Quit. Continue's secondary line identifies the last pair.
On a fresh profile, Play receives initial focus and Continue is visibly
unavailable. A save failure stays visible near the actions and provides a
safe route to Options or Quit. Both concept plates below are preview art and
do not claim that their illustrated scenery exists in the current build.

| Title candidate | Visual and interaction idea | Current decision |
| --- | --- | --- |
| [A — Scenic refuge](design/explorations/title-refuge-landscape.png) | Menu on a quiet left rail; warm homestead and outward path carry the mood. A full outlined button shows controller focus. | Keep the homestead setting and left menu. |
| [B — Field journal](design/explorations/title-refuge-journal.png) | Close tactile journal and lantern on the left; restrained right-side menu. The focused choice gains an outlined panel and edge bar. | Keep its warm palette and quieter control treatment. |

The owner's combined choice is the [warm-refuge hybrid](design/explorations/title-warm-refuge-hybrid.png): use Topaz's actual homestead menu stage as the background; place the menu on the left with B's warm ink, ivory, and brass treatment. Its focused row gains an unmistakable full outline and edge bar; unfocused rows use fine dividers rather than boxes. The generated scenery in these plates is illustrative, not a shippable level asset. The editable SVG compositions are alongside the PNG previews. Shippable text and controls remain live TextMesh Pro and uGUI content over the actual Unity menu stage.

### Approved title foundation

| Token or behavior | Value for the first Unity pass |
| --- | --- |
| Dark surface | Warm ink `#171312`, opaque enough for text against any homestead lighting. |
| Primary text | Ivory `#FFF1D8`; supporting text `#E0C6A3`. |
| Focus/accent | Brass `#EFC784` outline and left edge bar; use shape and luminance, not color alone. |
| Heading/body | Cormorant Garamond for short identity/headings; readable TMP sans-serif for actions, details, and errors. |
| Primary row | Continue when a valid last pair exists; otherwise Play. Show the last Character and World names beneath Continue. |
| Action order | Continue, Play, Options, Quit; move focus vertically, submit activates, Back from child pages returns here. |
| Failure state | Keep the existing save problem visible near the action list; do not erase it after a brief toast timer. |

These tokens are the starting language for later screen reviews; the Journal's light parchment uses dark ink text and need not inherit the title's dark surface.

### Playable implementation pass

- The title and Options share a left-side warm-ink rail, live homestead menu stage, ivory action text, and a brass focus outline. Continue names the last Character and World. The stage now frames the character with a campfire, workbench, wood stores, and existing KayKit trees; concept plates are still only mood references.
- Character/World selection, delete confirmation, Pause, and Graphics use the same warm palette. Deletion retains Cancel as initial focus and states separately what removing a Character or World loses. Pause still returns to the invoking layer, including Backpack opened from Pause.
- All current Journal pages retain `JournalBackground.png`. Backpack, Equipment, Skills, Workbench, Storage Chest, and Gear Rack use ink-on-paper choice surfaces, darker leather actions/tabs, and a shared hollow focus-frame sprite. Occupied backpack slots show available item art. Equipment slots stay on the left page; detailed skill progress appears on Skills rather than repeating under Equipment. Existing appearance portraits are framed as illustrations. A generated transparent-cutout preview changed character identity and was not used. A talent with an available choice reads **READY**, not **LOCKED**.
- UI Scale is a local 100%/125%/150% preference available from title and Pause. Title and Options reflow vertically; the Character/World picker and Graphics panel fit within the screen; large illustrated Journal frames fit inside the canvas while functional text grows where it fits. The denser Equipment, Storage, and Workbench pages cap text enlargement at 125% to avoid overlap; continue reviewing those pages with the owner at the largest setting.
- Exploration remains clear. The contextual action chip and temporary vitality cue inherit the warm dark surface and ivory/brass text; brief status messages and persistent save failures keep their different lifetimes.

The visual implementation is an owner-review build, not a final sign-off for every screen. Record future screen-specific feedback here as it is decided.

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
