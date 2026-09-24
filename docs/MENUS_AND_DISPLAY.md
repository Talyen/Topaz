# Desktop menus and display

The standalone Topaz player opens at its title screen. **Continue** enters the current local save. **Esc** opens the pause menu during play; Esc again resumes. Both title and pause menus lead to **Options** for display mode and window size, then **Graphics** for Camera Zoom, Anti-aliasing, Depth of Field, Bloom, and Ambient Occlusion. Graphics choices use dropdowns and toggles in that order; there are no sliders, Copy, or Save buttons. Changes apply and save immediately, and **Reset to Default** restores the Graphics page. Esc backs out one layer at a time. Mouse and gamepad UI navigation use Unity's existing uGUI EventSystem and Input System module.

## Making the view larger

The default desktop display mode is **borderless at the display's native resolution**. Options can switch to a resizable window and cycle 1280×720, 1600×900, and 1920×1080. Unity applies a requested resolution change at the end of the frame. The window size selection switches to windowed mode so its effect is immediately visible. The choices persist locally through Unity PlayerPrefs. They do not change Topaz's gameplay save or the Graphics page's separate JSON settings. Unity's [Screen.SetResolution](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/Screen.SetResolution.html) and [FullScreenMode](https://docs.unity3d.com/6000.6/Documentation/ScriptReference/FullScreenMode.html) APIs provide the native display modes.

The fixed orthographic camera starts at size **7.2**. Graphics offers **Close 5.8**, **Balanced 7.2**, and **Far 9.0**. Mouse wheel and gamepad shoulders still zoom moment to moment within the camera's bounds. The saved menu selection sets the starting view on the next player launch.

Editor Play Mode and automated tests enter gameplay directly and do not change the desktop window. The Mac build is the review artifact for the title, pause, display, and options flow. Borderless native rendering increases pixel workload relative to the old small window; compare smoothness on the 60 Hz Mac display before treating it as a final quality choice. Windows 1080p/120 FPS still requires a representative scene and hardware capture.
