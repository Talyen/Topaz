# Topaz UI image explorations

Generated with the built-in ImageGen tool on 2026-09-24. These images are
visual references; the world details in them are illustrative. Unity renders
the actual menus over Topaz's live scene.

| File | Prompt direction |
| --- | --- |
| `title-iron-brass.png` | Full-screen 16:9 Topaz title over a stylized low-poly orthographic homestead, with crafted iron, warm vellum, fine brass inlay, and only the exact menu labels `TOPAZ`, `Continue`, `New Game`, `Options`, `Quit`. The title uses the provided Mac gameplay screenshot as a visual reference. No HUD or control instructions. |
| `backpack-field-journal.png` | Full-screen 16:9 tactile cartographer's journal inventory: soft parchment, leather, charcoal ink, a selected Wood item with details, and the live world visible around the book. No HUD or control instructions. |
| `pause-cinematic.png` | Full-screen 16:9 minimal pause menu: subtle charcoal scrim, warm-white type, thin brass focus mark, most of the world visible, with `Paused`, `Resume`, `Backpack`, `Options`, `Main Menu`, `Quit`. No HUD or control instructions. |

Two production UI art plates were then generated:

| File | Prompt direction |
| --- | --- |
| `Assets/Topaz/UI/Art/JournalBackground.png` | Edit the field-journal inventory concept to retain only the leather-bound open book, blank parchment pages, subtle bottom-corner landscape drawings, and genuine transparency outside the silhouette. Remove all text, icons, item art, slots, controls, and scenery. |
| `Assets/Topaz/UI/Art/WoodJournal.png` | A centered bundle of three rough-cut logs tied with twine, rendered as a refined ink and muted-sepia watercolor study on genuine transparent alpha, with no text, page, frame, or scenery. |

The editable state and behavior of every shipped UI control remain native
uGUI/TextMesh Pro objects. The generated plates contain no interface text.
