# Movement and camera feel study

This is a replaceable graybox for evaluating controls and camera motion on a Mac build. It now also contains a [one-enemy combat study](COMBAT_STUDY.md). It has no loot, inventory, gathering skill, or persistent world state.

| Action | Keyboard and mouse | Gamepad |
| --- | --- | --- |
| Move | W A S D, relative to the screen | Left stick or D-pad |
| Aim | Mouse cursor on the ground plane | Right stick |
| Swing sword | Left mouse button | Right trigger |
| Dodge | Space | East face button (B on Xbox layout) |
| Interact with a nearby practice resource | E | West face button (X on Xbox layout) |
| Zoom | Mouse wheel | Right shoulder in, left shoulder out |

The player faces the aim direction. The small green marker shows that direction, and the camera follows with modest aim look-ahead. During a dodge the player brightens and briefly ignores damage. A resource pillar gains a ring when nearby and briefly turns green on interaction. It creates no item or experience.

Playtest for direct movement at 60 Hz, camera comfort while aiming around the character, stable zoom, dodge distance and collision near obstacles, and whether keyboard and controller inputs feel equally clear. Record subjective notes before tuning numbers. Formal frame-time scenarios start after a representative gameplay slice exists.
