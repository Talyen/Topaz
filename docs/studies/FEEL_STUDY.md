# Movement and camera feel study

This is a replaceable graybox for evaluating controls and camera motion on a Mac build. It now also contains a [one-enemy combat study](COMBAT_STUDY.md). It has no loot, inventory, gathering skill, or persistent world state.

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move | W A S D, relative to the screen | Left stick or D-pad |
| Aim | Mouse cursor on the ground plane | Right stick |
| Swing sword | Left mouse button | Right trigger |
| Dodge | Left Shift | East face button (B on Xbox layout) |
| Jump | Space | South face button (A on Xbox layout) |
| Interact with a nearby practice resource | E | West face button (X on Xbox layout) |
| Zoom | Mouse wheel | Right shoulder in, left shoulder out |

The player faces the aim direction, and the camera follows with modest aim look-ahead. Jump is a single grounded hop, about 0.9 world units high, with a KayKit short-jump pose. It cannot begin during a dodge, committed attack, menu, travel, or placement. The south gamepad button also places a structure when placement mode is active. During a dodge the player brightens and briefly ignores damage. A resource pillar gains a ring when nearby and briefly turns green on interaction. It creates no item or experience.

Playtest for direct movement at 60 Hz, camera comfort while aiming around the character, stable zoom, dodge distance and collision near obstacles, and whether keyboard and controller inputs feel equally clear. Record subjective notes before tuning numbers. Formal frame-time scenarios start after a representative gameplay slice exists.

## Unity references

Use the [Input System 1.20 action guide](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/manual/Actions.html) when changing bindings or callbacks, and the [Unity 6.6 Character Controller reference](https://docs.unity3d.com/6000.6/Documentation/Manual/class-CharacterController.html) when changing movement or collision. The [time and frame rate guide](https://docs.unity3d.com/6000.6/Documentation/Manual/managing-time-and-frame-rate.html) explains update timing; check it before changing the relationship between input, movement, and camera follow. See the [reference index](../UNITY_REFERENCE_GUIDE.md) for related physics and Play mode guidance.
