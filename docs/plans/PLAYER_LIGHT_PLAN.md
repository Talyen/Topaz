# Player-carried light: research and proposed plan

Status: first implementation complete, 2026-09-24; a low-light tuning pass centers a full 360-degree warm point light on the player. The player always has a hands-free lantern. It is an atmosphere and visibility feature with no gameplay-rule effect. The prop stays visible and dark when switched off. The lantern is toggled from its entry in the backpack journal, with no dedicated gameplay hotkey. Its on/off state belongs to the character and is saved; new and older characters start off. The Mac build remains the visual review surface for range, warmth, and prop placement.

## Existing Topaz constraints

- Unity 6000.6.2f1, URP 17.6.0, fixed-angle orthographic 3D camera, and desktop-first controls. The desktop renderer is Forward+; additional lights, additional-light shadows, and cookies are enabled in the desktop URP asset. The mobile asset has additional-light shadows disabled.
- The player is a `CharacterController` with a stable aim direction from mouse or right stick. The KayKit Rogue currently attaches a sword or axe to the right-hand socket. A held torch would compete with weapon and eventual shield poses.
- A KayKit RPGTools lantern and torch are already imported under CC0, and the graphics study already uses the lantern as a stationary prop. Asset review decisions still govern new placements. No purchased lighting or animation package is required for a first comparison.
- The day/night design says night remains navigable and combat remains readable with the carried light off. A carried light should add agency and atmosphere without becoming the only way to see attack tells.
- The source is always available to the player. It has no fuel, inventory slot, equipment restriction, enemy-detection effect, or combat modifier in the first version.
- The current backpack journal uses 16 stack slots. Clicking a stack slot selects it and shows item details; storage can transfer those stacks to a chest. Keep the always-owned lantern outside that transferable stack collection.

## Technical options

| Option | Strength | Cost or limitation |
| --- | --- | --- |
| Realtime point light on the player | Natural local, approximately radial illumination. Simple toggle and tuning. | Lights behind the character equally; can brighten the player too much. Shadows are an optional additional cost. |
| Realtime spot light aimed with the character | Clear forward search cone, natural rear darkness, easy angle control. | Can look like a flashlight. A narrow or hard-edged cone may fight the warm lantern fantasy and camera composition. |
| Point light with a cubemap cookie | Radial coverage with an authored soft rear falloff. The light can rotate with facing. | Requires a cubemap asset and art tuning; a mask does not create dynamic occlusion from nearby walls or the player's body. |
| Wide spot plus weak point fill | Forward-biased light with readable immediate surroundings. | Two lights per player and more tuning. Use only if one light cannot achieve the desired shape. |
| Emissive prop or particles only | Cheap visual cue, useful with a real light. | Does not illuminate nearby 3D surfaces by itself in Topaz's realtime setup. |
| Third-party lantern or lighting package | Could add polished models or specialized effects. | The imported CC0 KayKit props already cover the first comparison, and native URP covers the lighting. Any new package adds maintenance and asset-rights review without a demonstrated need yet. |
| Screen-space dark mask, decal, or custom shader | Could enforce a precise visibility shape. | Adds a separate rendering and interaction model; can ignore geometry and local lights. Reserve for a later explicit gameplay need. URP 2D Light is for the 2D renderer, not this 3D scene. |

The **appearance of the prop** and **distribution of the actual light** are independent choices. A belt lantern can use a point light, a soft forward-biased point cookie, or a wide spot. A handheld torch is not inherently radial.

## Selected direction and mounting recommendation

Mount the existing KayKit lantern at the **side of the waist opposite the sword**, beginning with the left hip on the Rogue. Use a socket child of the animated hips bone and adjust its offset in the gameplay camera so the prop clears the leg and reads at all facing angles. The camera is fixed-angle, but the player turns toward aim, so the exact hip offset should be chosen from a Mac build rather than assumed from a front view. This keeps the hands free and leaves shoulder silhouettes clear.

The hips animation already moves the mounted prop. For a little secondary motion, put the model under a pivot at the socket and give it a **small, damped, angle-limited sway** on acceleration, turning, dodging, and landing. Return it gently toward rest. This can be a small Topaz-specific visual component updated after the Animator, or an authored additive animation if a clip gives a better look. It is visual only: no Rigidbody, joint, collider, or effect on the player's movement. Unity's official Animation Rigging package has damped transforms, but it is not installed and is more machinery than this one accessory requires. Consider it if later equipment needs a shared rigging workflow.

Place the actual realtime Light on a **stable waist-height child of the player motion root**, centered on the player rather than under the swinging model. This prevents every sway or attack animation from making the lit world wobble. The emissive wick and any tiny particles remain on the visible prop. Do not parent the Light to the camera. When off, disable the Light and emitter glow/particles while keeping the lantern mesh visible. The model's glass uses its own neutral, nonemissive material so its green atlas color does not read as a second light.

Use one warm point light with **even 360-degree coverage**. The first centered range is 8.5 world units, with no cookie or facing-dependent falloff. This is a lighting shape, with no visibility, stealth, damage, or interaction rule. The light remains unshadowed, so nearby walls do not occlude it.

If the light seems to pass through walls or needs more physical depth, compare an additional-light shadowed version against the unshadowed version in a representative scene and standalone build. Rendering Layers can exclude the player mesh from the carried light if self-lighting harms readability. The darker dusk, night, and interior baselines should still preserve path and combat readability with the lantern off.

## Backpack interaction now; equipment menu later

Add one **permanent Lantern entry** to the existing uGUI backpack journal, visually distinct from the 16 stack slots but on the same page. It is always present, cannot be dropped, consumed, stacked, or transferred to a chest, and does not use a capacity slot. The entry should display its current **On** or **Off** state. Clicking it, or activating it with the gamepad's normal UI Submit action, toggles the light immediately. Update the label and selected-item description in place; keep the backpack open so the player can switch it back just as easily. The lantern remains visible on the character in either state.

Use the existing inventory-opening control and EventSystem navigation. Do not add a separate light hotkey or change the other slot buttons' select-only behavior. Keep the light state and toggle command outside the journal view so the later equipment menu can call the same command and show the same state. If a direct tile toggle proves easy to trigger accidentally during gamepad navigation, switch only that tile to a select-then-activate detail button after playtesting.

## Proposed implementation sequence after the design choice

1. Build a reversible lighting study in home and expedition scenes: waist lantern, centered 360-degree point light, and on/off. Check the owner-facing Mac build at normal and far zoom, day and night, while idle, turning, moving, dodging, attacking, jumping, and near walls. Tune the waist offset, sway, brightness, and range from captures and play feel.
2. Add the permanent Lantern entry to the existing backpack journal and wire its click/UI Submit to a small player-light component. Reuse the existing inventory-opening control and UI navigation. Show On/Off on the entry and in the details. Do not add a light hotkey or put the lantern in the transferable inventory stacks.
3. Keep the on/off state through additive region travel and save/load in character state. Missing fields in older collections default to off. Ensure travel does not duplicate the light. Keep the state API independent of the backpack UI so a future equipment menu can reuse it.
4. Add the visible waist prop, optional small emitter glow, and restrained particles. Keep the off state visible and dark. Review KayKit asset decisions and source records before placing the model; record any new asset and `.meta` file.
5. Verify with `./scripts/verify.sh`, `./scripts/build.sh windows` if the rendering or input changes affect Windows, `git diff --check`, and Mac standalone play at 60 Hz. Profile GPU cost in a representative build if shadows or multiple lights are selected; Windows performance claims require the designated Windows PC.

## Default used for implementation

The choice is remembered across save/load and region travel. New characters and collections written before the lantern was added start with it off, preserving the existing day/night look.

## Unity references

- [Unity 6.6 light types](https://docs.unity3d.com/6000.6/Documentation/Manual/Lighting.html) and [URP Light component](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/light-component.html): point, spot, range, angles, shadows, cookies, and rendering layers.
- [Unity 6.6 cookies](https://docs.unity3d.com/6000.6/Documentation/Manual/Cookies-introduction.html) and [URP asset settings](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/universalrp-asset.html): masks and cookie atlas support.
- [Unity 6.6 Forward+](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/rendering/forward-rendering-paths.html), [light limits](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/lighting/light-limits-in-urp.html), [rendering layers](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/features/rendering-layers-introduction.html), and [URP performance](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/configure-for-better-performance.html).
- [Unity 6.6 Light 2D](https://docs.unity3d.com/6000.6/Documentation/Manual/urp/2DLightProperties.html) explains why a 2D light shape is not the starting point for Topaz's 3D renderer.
- [Animator bone transforms](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Animator.GetBoneTransform.html), [Configurable Joint](https://docs.unity3d.com/6000.6/Documentation/Manual/class-ConfigurableJoint.html), and [Animation Rigging damped transform](https://docs.unity3d.com/Packages/com.unity.animation.rigging@1.2/manual/constraints/DampedTransform.html) describe the first-party attachment and motion alternatives.
