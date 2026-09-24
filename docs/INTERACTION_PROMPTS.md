# Contextual interaction cues

The exploration view uses one short action chip beside the nearest usable object within 1.4 m. The chip uses the installed uGUI, TextMesh Pro, and Input System action asset. The UI builder obtains its key labels with [`GetBindingDisplayString`](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputActionRebindingExtensions.html); the runtime view switches between the authored keyboard and gamepad labels after meaningful input. No per-object Canvas or always-visible controls text is needed.

`WorldSession` selects the same interaction candidate for the chip and the E/gamepad-west action. It compares planar distance across the region marker, unlocked supply cache, placed chest, workbench, rest point, and available tree. Authored `Interaction Anchor` children position the chip above each object. The chip hides during menus, travel, placement, dodge, jump, committed attacks, or when its target goes off-screen. A locked or empty cache has no chip; pressing Interact there can still produce the existing short status message.

## Tool-assisted gathering

An available tree shows **Chop** even when the sword is equipped. One Interact press temporarily equips the axe, turns toward the selected tree, and starts the normal aimed axe windup, strike, and recovery. Each successful strike awards Logging XP and advances the tree's saved partial harvest. The previous weapon returns after recovery or cancellation; the temporary selection is never written to the save. Repeated presses during that swing do not queue another chop. Three separate chops produce the usual Wood drop. Manual axe attacks remain available.

The tree's `HarvestDefinition` supplies `requiredToolId = "axe"`. Topaz currently provides the axe by default. A future equipment system must check ownership before offering a tool action that the player cannot perform.

## Future UI changes

There is no runtime rebinding screen yet. If one is added, refresh the chip's display strings when binding overrides change. Subtle target highlights and one-time discovery hints remain optional visual auditions; do not make them permanent HUD instructions.
