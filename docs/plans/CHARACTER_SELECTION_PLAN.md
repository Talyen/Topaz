# Character selection plan

Status: implemented first pass; owner review of the Mac build remains. This is
an appearance choice, not a class, ability, equipment, or name choice. The
player-level structure is in
[Characters and worlds](../CHARACTERS_AND_WORLDS.md); its carry-over rules are
agreed for this design.

## Player experience

The desktop title has **Continue** when a previously played Character + World
pair exists and **Play** to choose a pair. **New Character** opens **Choose
Your Look**. The player browses six KayKit Adventurer looks: Rogue, Hooded
Rogue, Knight, Ranger, Mage, and Barbarian. The selected model stands beside
a campfire in a full-screen 3D menu scene, with the choices overlaid at left.
It uses the same FBX, texture atlas, Generic Avatar, and Animator Controller
as gameplay. The dedicated menu camera has its own framing and warm lights,
while using the in-world camera's URP effects settings. The only descriptive
line is "Appearance only. Every look plays the same."
There is no name field, class description, stat comparison, or cosmetic editor.

**Create Character** confirms the highlighted look and opens the World picker.
The player can choose an existing World or create a fresh World before entering
the homestead. The new Character remains a draft until **Enter World**.
**Back**, Esc, or gamepad cancel returns to the previous screen without
creating a Character or World. Continue resumes the most recently
played pair. If local data is unreadable, show the existing persistence problem
and do not silently replace it. Appearance stays fixed for that Character in
this proposal; a later wardrobe or mirror could allow changes.

## Visual direction

The title, Character list, look picker, and World list use one dedicated,
full-screen campfire clearing. A translucent iron rail keeps warm ivory text
and brass focus visible while the scene stays in view. Character and World
choices are native uGUI controls over the scene, not modal cards. The preview
model idles and can rotate in 45-degree steps; mouse clicks choose a look,
while keyboard and gamepad focus also update it. Hover alone does not swap
models. Held weapons are omitted so the visual choice does not suggest a
class. The scene uses already imported KayKit floor, tree, rock, grass, and
Wood assets, Unity particles and lights, and the project's active URP renderer
effects. Its camera and lighting differ from gameplay, so review each look at
normal gameplay size as well.

The earlier boxed [wireframe](../design/character-selection-wireframe.svg) is
superseded by this full-screen implementation. Use the existing uGUI, TextMesh
Pro, Input System UI navigation, `TopazUiTheme`, and shared button/focus
styling. Test the 1920x1080 reference, 1280x720, 1600x900, and native Mac
display.

## Asset and animation gate

The project has six character FBX files in
`Assets/ThirdParty/KayKit/Adventurers/Characters/`. All six now import with
Generic Avatars and can be selected in the playable scene. The PlayMode gate
checks each model, Avatar, renderer, and live preview. The Mac review still
needs to judge locomotion, dodge, hit, sword and axe poses, right-hand
placement, facing, scale, materials, and shadows at gameplay size. The
CharacterController and gameplay timing remain unchanged.

The pack has five base texture atlases for these six models and no imported
palette variant set. Do not offer a color picker in this pass. A later small,
authored set of color variants is reasonable only after inspecting which
material regions can change independently and checking their readability in
the world. Tinting an entire atlas is not an acceptable substitute.

## Implementation sequence

1. **Separate title from gameplay initialization.** Detect whether a valid
   Character + World pair exists before showing Continue. Keep persistent data
   untouched while the title and selection screens are open. The current
   `WorldSession` loads data in `Awake` and commits in `Start`; delay gameplay
   initialization until a pair is selected.
2. **Define looks as data.** Give each choice a stable, nonlocalized ID and a
   definition containing its display label, model/prefab, preview framing,
   and player visual binding. Preserve the current player controller, camera,
   combat, and equipped-tool state; replace only the visual and its Animator
   binding. Reuse the existing Animator Controller and clips where compatible.
3. **Build the selection view.** Add a full-screen menu stage and camera to
   Bootstrap, with an authored campfire clearing behind overlaid controls.
   Bind list selection, live preview, focus, confirm, and back. Keep the view
   a thin presentation layer.
4. **Persist the choice.** Store the stable look ID on the Character record.
   Migrate the existing combined save to a Rogue Character, one World, and
   their Visit. Resolve unknown look IDs safely to Rogue while keeping other
   valid state. Continue restores the selected Character's look before play.
5. **Review and verify.** Exercise every look in the title preview and the
   homestead: idle, directional movement, dodge, jump, hit, sword and axe use,
   expedition travel, persistence, quit, relaunch, and Continue. Verify
   back/cancel leaves existing Characters and Worlds intact. Run `./scripts/verify.sh`,
   inspect console errors, build a Mac player for owner review, and run
   `./scripts/build.sh windows` because this changes desktop startup and
   referenced player assets. Inspect `git diff --check`.

## Acceptance criteria

- On a fresh install, title offers Play, then New Character and New World;
  no player data is written before those choices are confirmed.
- Every offered look is visibly distinct in preview and in play, with the
  same movement, combat, equipment, and progression rules.
- Existing data migrates to a Rogue Character and one World; new Characters
  restore their chosen look after quitting, relaunching, and changing worlds.
- Cancel preserves existing Characters and Worlds; unreadable data is not
  replaced automatically.
- Mouse, keyboard, and gamepad selection work with visible focus at the
  supported desktop sizes.
- The Mac build is reviewable on a 60 Hz display without obvious preview or
  transition hitches. Windows performance claims wait for the designated PC.

## Agreed scope

The selected appearance, skills, equipment, and carried items belong to the
Character and travel between Worlds. Shared World state and per-Character
return positions follow [Characters and worlds](../CHARACTERS_AND_WORLDS.md).
Appearance is fixed for a Character in the first pass; revisit a wardrobe
after equipment exists.

Unity references: [Generic animation import](https://docs.unity3d.com/6000.6/Documentation/Manual/GenericAnimations.html),
[ScriptableObject definitions](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ScriptableObject.html),
and Topaz's [versioned UI guide](../UNITY_REFERENCE_GUIDE.md).
