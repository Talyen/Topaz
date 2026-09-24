# Desktop menus and display

The standalone Topaz player opens at its title screen. **Continue** returns to the most recently played Character and World; it is unavailable before the first adventure. **Play** opens the Character list, then the World list. Players can create a Character by choosing one of six appearances, and can enter an existing World or start a fresh one. No naming or save-slot UI appears. **Esc** opens the quiet pause menu during play; Esc again resumes. Pause can open **Backpack**, which returns to pause when closed. Both title and pause lead to **Options** for display mode and window size, then **Graphics** for Camera Zoom, Anti-aliasing, Depth of Field, Bloom, and Ambient Occlusion. Graphics choices use dropdowns and toggles in that order; there are no sliders, Copy, or Save buttons. Changes apply and save immediately, and **Reset to Default** restores the Graphics page. Esc backs out one layer at a time. Mouse and gamepad UI navigation use Unity's existing uGUI EventSystem and Input System module. The menus display no control-instruction legend.

Title and Character/World selection are full-screen overlays on an isolated
campfire clearing. The menu camera uses the same URP effects choices as the
gameplay camera, while its authored campfire and warm key light show the
selected model clearly. The menu stage is inactive during gameplay; the
gameplay camera returns when the player enters a World. No menu preview is
written into Character or World state before **Enter World**.

## Making the view larger

The default desktop display mode is **borderless at the display's native resolution**. Options can switch to a resizable window and cycle 1280×720, 1600×900, and 1920×1080. Unity applies a requested resolution change at the end of the frame. The window size selection switches to windowed mode so its effect is immediately visible. The choices persist locally through Unity PlayerPrefs. They do not change Topaz's gameplay save or the Graphics page's separate JSON settings. Unity's [Screen.SetResolution](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Screen.SetResolution.html) and [FullScreenMode](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FullScreenMode.html) APIs provide the native display modes.

The fixed orthographic camera starts at size **7.2**. Graphics offers **Close 5.8**, **Balanced 7.2**, and **Far 9.0**. Mouse wheel and gamepad shoulders still zoom moment to moment within the camera's bounds. The saved menu selection sets the starting view on the next player launch.

Editor Play Mode and automated tests enter gameplay directly with a temporary default Character and World; standalone players see the title flow. The Mac build is the review artifact for Character and World selection, pause, display, and options. Borderless native rendering increases pixel workload relative to the old small window; compare smoothness on the 60 Hz Mac display before treating it as a final quality choice. Windows 1080p/120 FPS still requires a representative scene and hardware capture.
