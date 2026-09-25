using System.IO;
using TMPro;
using Topaz.LoopStudy;
using Topaz.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Topaz.Editor
{
    /// <summary>Replaces flat prototype blocks with paper, ink, and leather controls.</summary>
    public static class JournalPolishSetup
    {
        const string ScenePath = "Assets/Topaz/World/Scenes/Bootstrap.unity";
        const string FocusFramePath = "Assets/Topaz/UI/Art/FocusFrame.png";
        static readonly Color Paper = new Color32(209, 182, 145, 255);
        static readonly Color Ink = new Color32(55, 39, 31, 255);
        static readonly Color Leather = new Color32(49, 37, 30, 255);
        static readonly Color Ivory = new Color32(255, 241, 216, 255);
        static readonly Color Brass = new Color32(142, 83, 41, 255);
        static Sprite _focusFrame;

        [MenuItem("Topaz/Polish Journal UI")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject canvas = GameObject.Find("Loop HUD");
            if (canvas == null) throw new System.InvalidOperationException("Loop HUD is missing.");
            ApplyToCanvas(canvas.transform);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Topaz] Journal pages use paper, ink, and leather control styles.");
        }

        internal static void ApplyToCanvas(Transform canvas)
        {
            _focusFrame = EnsureFocusFrame();
            foreach (string page in new[] { "Backpack", "Equipment", "Skills", "Workbench",
                         "Storage Chest", "Gear Rack" })
            {
                Transform journal = canvas.Find(page + "/Open Journal");
                if (journal == null) continue;
                foreach (Button button in journal.GetComponentsInChildren<Button>(true))
                    StyleButton(button);
                StylePageHeading(journal);
                TopazJournalFit fit = journal.GetComponent<TopazJournalFit>();
                if (fit == null) fit = journal.gameObject.AddComponent<TopazJournalFit>();
                fit.SetMaxTextScale(page == "Equipment" || page == "Storage Chest" ||
                    page == "Workbench" ? 1.25f : 1.5f);
                if (page == "Equipment")
                {
                    StyleEquipmentLayout(journal);
                    FramePortrait(journal.Find("Character Portrait"));
                }
                if (page == "Backpack")
                    EnsureBackpackIcons(journal, canvas.GetComponent<LoopHud>());
            }
        }

        static void StyleButton(Button button)
        {
            string name = button.name;
            bool tab = name.EndsWith(" Tab");
            bool primary = name == "Talent Action" || name == "Craft chest" ||
                name == "Equip" || name == "Deposit materials" ||
                name == "Withdraw materials" || name == "Store gear" ||
                name == "Take gear";
            bool leather = tab || primary || name == "Close";
            Image image = button.targetGraphic as Image;
            if (image == null) return;
            button.transition = Selectable.Transition.None;
            TopazMenuRowVisual visual = button.GetComponent<TopazMenuRowVisual>();
            if (visual == null) visual = button.gameObject.AddComponent<TopazMenuRowVisual>();
            visual.SetPalette(image,
                leather ? Leather : new Color(Paper.r, Paper.g, Paper.b, .12f),
                leather ? new Color(.28f, .19f, .13f, 1f) :
                    new Color(Paper.r, Paper.g, Paper.b, .30f),
                leather ? new Color(.34f, .22f, .14f, 1f) :
                    new Color(Brass.r, Brass.g, Brass.b, .34f));
            foreach (Outline outline in image.GetComponents<Outline>())
                Object.DestroyImmediate(outline);
            TopazFocusIndicator focus = button.GetComponent<TopazFocusIndicator>();
            if (focus == null) focus = button.gameObject.AddComponent<TopazFocusIndicator>();
            foreach (string prefix in new[] { "Frame", "Focus" })
                foreach (string side in new[] { "Top", "Bottom", "Left", "Right" })
                {
                    Transform old = button.transform.Find(prefix + " " + side);
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                }
            Transform existingFrame = button.transform.Find("Focus Frame");
            Image frame;
            if (existingFrame == null)
            {
                var go = new GameObject("Focus Frame", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(button.transform, false);
                frame = go.GetComponent<Image>();
            }
            else frame = existingFrame.GetComponent<Image>();
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            frame.sprite = _focusFrame;
            frame.type = Image.Type.Sliced;
            frame.color = Brass;
            frame.raycastTarget = false;
            focus.SetBorders(new Graphic[] { frame });
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.font = TMP_Settings.defaultFontAsset;
                label.color = leather ? Ivory : Ink;
                label.fontSize = Mathf.Max(22f, label.fontSize);
                label.raycastTarget = false;
            }
            if (primary)
            {
                Shadow shadow = null;
                foreach (Shadow effect in image.GetComponents<Shadow>())
                    if (effect.GetType() == typeof(Shadow)) shadow = effect;
                if (shadow == null) shadow = image.gameObject.AddComponent<Shadow>();
                shadow.effectColor = new Color(25f / 255f, 18f / 255f, 13f / 255f, .35f);
                shadow.effectDistance = new Vector2(0f, -3f);
            }
        }

        static void StylePageHeading(Transform journal)
        {
            Transform heading = journal.Find("Heading") ?? journal.Find("Backpack Heading");
            TMP_Text text = heading?.GetComponent<TMP_Text>();
            if (text != null) text.color = Ink;
            string[] names = { "Left Page Rule", "Right Page Rule" };
            float[] x = { 250f, 900f };
            for (int i = 0; i < names.Length; i++)
            {
                Transform old = journal.Find(names[i]);
                RectTransform rect;
                if (old == null)
                {
                    var rule = new GameObject(names[i], typeof(RectTransform), typeof(Image));
                    rule.transform.SetParent(journal, false);
                    rect = rule.GetComponent<RectTransform>();
                }
                else rect = old.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x[i], -178f);
                rect.sizeDelta = new Vector2(i == 0 ? 565f : 550f, 2f);
                Image line = rect.GetComponent<Image>();
                line.color = new Color(Brass.r, Brass.g, Brass.b, .44f);
                line.raycastTarget = false;
            }
        }

        static void FramePortrait(Transform portrait)
        {
            Image image = portrait?.GetComponent<Image>();
            if (image == null) return;
            foreach (Outline old in image.GetComponents<Outline>())
                Object.DestroyImmediate(old);
            RectTransform portraitRect = portrait.GetComponent<RectTransform>();
            Transform oldMat = portrait.parent.Find("Portrait Mat");
            Image mat;
            if (oldMat == null)
            {
                var go = new GameObject("Portrait Mat", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(portrait.parent, false);
                mat = go.GetComponent<Image>();
            }
            else mat = oldMat.GetComponent<Image>();
            RectTransform matRect = mat.GetComponent<RectTransform>();
            matRect.anchorMin = matRect.anchorMax = portraitRect.anchorMin;
            matRect.pivot = portraitRect.pivot;
            matRect.anchoredPosition = portraitRect.anchoredPosition + new Vector2(-8f, 8f);
            matRect.sizeDelta = portraitRect.sizeDelta + new Vector2(16f, 16f);
            if (matRect.GetSiblingIndex() > portrait.GetSiblingIndex())
                matRect.SetSiblingIndex(portrait.GetSiblingIndex());
            mat.color = Leather;
            mat.raycastTarget = false;

            Transform oldFrame = portrait.Find("Portrait Frame");
            Image frame;
            if (oldFrame == null)
            {
                var go = new GameObject("Portrait Frame", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(portrait, false);
                frame = go.GetComponent<Image>();
            }
            else frame = oldFrame.GetComponent<Image>();
            RectTransform frameRect = frame.GetComponent<RectTransform>();
            frameRect.anchorMin = Vector2.zero;
            frameRect.anchorMax = Vector2.one;
            frameRect.offsetMin = frameRect.offsetMax = Vector2.zero;
            frame.sprite = _focusFrame;
            frame.type = Image.Type.Sliced;
            frame.color = new Color(Brass.r, Brass.g, Brass.b, .9f);
            frame.raycastTarget = false;
        }

        static void StyleEquipmentLayout(Transform journal)
        {
            RectTransform portrait = journal.Find("Character Portrait") as RectTransform;
            if (portrait != null) portrait.sizeDelta = new Vector2(315f, 420f);
            Transform redundantSkills = journal.Find("Skills");
            if (redundantSkills != null) redundantSkills.gameObject.SetActive(false);
            foreach (Transform child in journal)
            {
                if (!child.name.StartsWith("Gear ") || child.GetComponent<Button>() == null)
                    continue;
                RectTransform rect = child.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(225f, rect.sizeDelta.y);
            }
            foreach (string name in new[] { "Weapon Tool", "Logging Axe Tool", "Pickaxe Tool" })
            {
                TMP_Text label = journal.Find(name)?.GetComponentInChildren<TMP_Text>(true);
                if (label != null) label.fontSize = 22f;
            }
            for (int i = 0; i < 16; i++)
            {
                RectTransform slot = journal.Find($"Gear Pack {i + 1:00}") as RectTransform;
                if (slot == null) continue;
                slot.anchoredPosition = new Vector2(900f + (i % 4) * 130f,
                    -345f - (i / 4) * 84f);
                slot.sizeDelta = new Vector2(118f, 72f);
            }
        }

        static void EnsureBackpackIcons(Transform journal, LoopHud hud)
        {
            if (hud == null) return;
            Transform grid = journal.Find("Backpack Slots");
            if (grid == null) return;
            var icons = new Image[WorldSession.BackpackCapacity];
            for (int i = 0; i < icons.Length; i++)
            {
                Transform slot = grid.Find($"Slot {i + 1:00}");
                if (slot == null) continue;
                Transform existing = slot.Find("Item Icon");
                Image icon;
                if (existing == null)
                {
                    var go = new GameObject("Item Icon", typeof(RectTransform), typeof(Image));
                    go.transform.SetParent(slot, false);
                    icon = go.GetComponent<Image>();
                }
                else icon = existing.GetComponent<Image>();
                RectTransform rect = icon.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
                rect.pivot = new Vector2(.5f, .5f);
                rect.anchoredPosition = new Vector2(0f, 5f);
                rect.sizeDelta = new Vector2(72f, 72f);
                rect.SetAsFirstSibling();
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.enabled = false;
                icons[i] = icon;
            }
            var data = new SerializedObject(hud);
            SerializedProperty array = data.FindProperty("backpackSlotIcons");
            array.arraySize = icons.Length;
            for (int i = 0; i < icons.Length; i++)
                array.GetArrayElementAtIndex(i).objectReferenceValue = icons[i];
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        static Sprite EnsureFocusFrame()
        {
            if (!File.Exists(FocusFramePath))
            {
                var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false);
                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        bool edge = x < 3 || x >= 13 || y < 3 || y >= 13;
                        texture.SetPixel(x, y, edge ? Color.white : Color.clear);
                    }
                texture.Apply();
                File.WriteAllBytes(FocusFramePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(FocusFramePath, ImportAssetOptions.ForceSynchronousImport);
            }
            var importer = AssetImporter.GetAtPath(FocusFramePath) as TextureImporter;
            if (importer == null)
                throw new System.InvalidOperationException("Focus frame texture did not import.");
            if (importer.textureType != TextureImporterType.Sprite ||
                importer.spriteBorder != new Vector4(3f, 3f, 3f, 3f))
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = new Vector4(3f, 3f, 3f, 3f);
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(FocusFramePath);
            if (sprite == null)
                throw new System.InvalidOperationException("Focus frame sprite is unavailable.");
            return sprite;
        }
    }
}
