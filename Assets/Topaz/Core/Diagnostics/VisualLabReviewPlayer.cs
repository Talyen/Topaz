using System;
using TMPro;
using Topaz.Player;
using Topaz.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz
{
    /// <summary>Isolated visual review. Uses the game movement/camera; never creates a WorldSession.</summary>
    public sealed class VisualLabReviewPlayer : MonoBehaviour
    {
        [Serializable] public sealed class MaterialSet { public Material[] materials; }
        [Serializable] public sealed class Binding
        { public Renderer renderer; public MaterialSet[] looks; }
        public Binding[] bindings;
        public PlayerController player;
        public PlayerCamera view;
        public Light sun;
        public Volume volume;
        public UniversalRenderPipelineAsset pipeline;
        public InputActionAsset controls;
        public TopazUiTheme theme;
        public Vector3 arrival = new Vector3(1, .1f, -6);
        public TMP_FontAsset font;
        public int CurrentLook { get; private set; }
        public bool MenuOpen { get; private set; }
        InputActionAsset reviewInput;
        InputAction pause, journal, cancel;
        RenderPipelineAsset previousPipeline;
        VolumeProfile profile;
        GameObject menu;
        TMP_Text activeLabel;
        UnityEngine.UI.Button[] buttons;
        readonly VisualLabLook[] recipes = VisualLabLook.All;

        void Awake()
        {
            previousPipeline = QualitySettings.renderPipeline;
            QualitySettings.renderPipeline = pipeline;
            reviewInput = Instantiate(controls);
            var map = reviewInput.FindActionMap("Player", true);
            pause = map.FindAction("Pause", true);
            journal = map.FindAction("Inventory", true);
            cancel = map.FindAction("Cancel", true);
            map.Enable();
            profile = volume.profile;
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 1;
        }
        void Start()
        {
            var cutaway=view.GetComponent<SceneryCutaway>();if(cutaway!=null)cutaway.enabled=false;
            var marker=view.GetComponent<WorldAimMarker>();if(marker!=null)marker.enabled=false;
            Shader.SetGlobalFloat("_TopazRevealEnabled",0);Shader.SetGlobalInt("_TopazSilhouetteCount",0);
            BuildMenu();
            SelectLook(0);
            SetMenu(false);
            Screen.SetResolution(1600, 900, FullScreenMode.Windowed);
        }
        void Update()
        {
            var keyboard = Keyboard.current;
            if (pause.WasPressedThisFrame() || journal.WasPressedThisFrame() || keyboard?.tabKey.wasPressedThisFrame == true)
                SetMenu(!MenuOpen);
            else if (MenuOpen && cancel.WasPressedThisFrame()) SetMenu(false);
            if (keyboard?.leftBracketKey.wasPressedThisFrame == true) SelectLook(CurrentLook - 1);
            if (keyboard?.rightBracketKey.wasPressedThisFrame == true) SelectLook(CurrentLook + 1);
            if (keyboard?.rKey.wasPressedThisFrame == true) ResetPosition();
            if (player.transform.position.y < -8) ResetPosition();
        }
        public void SelectLook(int index)
        {
            CurrentLook = (index % recipes.Length + recipes.Length) % recipes.Length;
            var look = recipes[CurrentLook];
            foreach (var binding in bindings) binding.renderer.sharedMaterials = binding.looks[CurrentLook].materials;
            sun.transform.rotation = Quaternion.Euler(look.Angle);
            sun.color = look.Sun; sun.intensity = look.Light; sun.shadowStrength = .85f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = look.Ambient * .7f;
            RenderSettings.ambientEquatorColor = look.Ambient * .48f;
            RenderSettings.ambientGroundColor = look.Shadow * .3f;
            RenderSettings.fog = false;
            var color = Get<ColorAdjustments>();
            color.postExposure.Override(look.Exposure); color.contrast.Override(look.Contrast); color.saturation.Override(look.Saturation);
            Get<Tonemapping>().mode.Override(CurrentLook == 0 || CurrentLook == 9 ? TonemappingMode.Neutral : TonemappingMode.ACES);
            var bloom = Get<Bloom>(); bloom.intensity.Override(look.Bloom); bloom.threshold.Override(1.2f);
            var dof = Get<DepthOfField>(); dof.active = look.DoF;
            dof.mode.Override(DepthOfFieldMode.Bokeh); dof.aperture.Override(8); dof.focalLength.Override(40);
            view.GetComponent<Camera>().GetUniversalAdditionalCameraData().resetHistory = true;
            if (activeLabel != null) activeLabel.text = "VISUAL REVIEW  ·  " + look.Name;
            if (buttons != null) for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].GetComponentInChildren<TMP_Text>().text = (i == CurrentLook ? "> " : "   ") + recipes[i].Name;
                buttons[i].GetComponent<UnityEngine.UI.Image>().color = i == CurrentLook ? theme.Copper : theme.Raised;
                buttons[i].GetComponentInChildren<TMP_Text>().color = i == CurrentLook ? theme.Ink : theme.Text;
            }
        }
        void LateUpdate()
        {
            if (recipes[CurrentLook].DoF)
                Get<DepthOfField>().focusDistance.Override(Mathf.Max(1, view.GetComponent<Camera>().WorldToViewportPoint(player.transform.position + Vector3.up).z));
        }
        T Get<T>() where T : VolumeComponent
        { if (!profile.TryGet<T>(out var value)) value = profile.Add<T>(true); return value; }
        public void SetMenu(bool open)
        {
            MenuOpen = open;
            player.ResetMotion();
            player.enabled = !open;
            view.enabled = !open;
            menu.SetActive(open);
            Cursor.lockState = open ? CursorLockMode.None : CursorLockMode.Confined;
            if (open) EventSystem.current.SetSelectedGameObject(buttons[CurrentLook].gameObject);
            else EventSystem.current.SetSelectedGameObject(null);
        }
        public void ResetPosition()
        {
            var capsule = player.GetComponent<CharacterController>();
            capsule.enabled = false; player.transform.position = arrival; capsule.enabled = true;
            player.ResetMotion(); view.ResetFollow();
        }
        void BuildMenu()
        {
            var canvasObject = new GameObject("Visual review controls", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.matchWidthOrHeight = .5f;
            var heading = Rect("Active look", canvasObject.transform, new Vector2(24,-20), new Vector2(1200,42));
            activeLabel = Label(heading, "", 25);
            var help = Rect("Controls", canvasObject.transform, new Vector2(24,-64), new Vector2(1740,34));
            Label(help, "WASD / left stick move   ·   mouse / right stick aim   ·   wheel / shoulders zoom   ·   [ ] change look   ·   Tab / B / Menu choose look", 19);
            menu = Rect("Look selector", canvasObject.transform, new Vector2(24,-120), new Vector2(650,620)).gameObject;
            var background = menu.AddComponent<UnityEngine.UI.Image>(); background.color = theme.Panel;
            Label(Rect("Title", menu.transform, new Vector2(20,-16),new Vector2(610,42)),"Choose a look",28);
            var grid = Rect("Looks",menu.transform,new Vector2(20,-72),new Vector2(610,380));
            var layout = grid.gameObject.AddComponent<UnityEngine.UI.GridLayoutGroup>();layout.cellSize = new Vector2(300,54);layout.spacing = new Vector2(10,8);layout.constraint = UnityEngine.UI.GridLayoutGroup.Constraint.FixedColumnCount;layout.constraintCount = 2;
            buttons = new UnityEngine.UI.Button[recipes.Length];
            for (int i = 0; i < buttons.Length; i++) { int index = i; buttons[i] = Button((Transform)grid,recipes[i].Name,()=>SelectLook(index)); }
            var returnButton=Button(Rect("Return",menu.transform,new Vector2(20,-470),new Vector2(295,54)),"Return to walking",()=>SetMenu(false));
            var resetButton=Button(Rect("Reset",menu.transform,new Vector2(335,-470),new Vector2(295,54)),"Reset position (R)",ResetPosition);
            var quitButton=Button(Rect("Quit",menu.transform,new Vector2(20,-540),new Vector2(610,54)),"Quit visual review",QuitReview);
            // Left/right and up/down focus follow the two-column visual order.
            for (int i = 0; i < buttons.Length; i++)
            {
                var nav = buttons[i].navigation;nav.mode = UnityEngine.UI.Navigation.Mode.Explicit;
                nav.selectOnLeft = buttons[i%2 == 1 ? i-1 : i];nav.selectOnRight = buttons[i%2 == 0 ? i+1 : i];
                nav.selectOnUp = buttons[i>=2 ? i-2 : i];nav.selectOnDown = i+2 < buttons.Length ? buttons[i+2] : i%2==0 ? returnButton : resetButton;buttons[i].navigation = nav;
            }
            Connect(returnButton,buttons[10],quitButton,returnButton,resetButton);
            Connect(resetButton,buttons[11],quitButton,returnButton,resetButton);
            Connect(quitButton,returnButton,quitButton,quitButton,quitButton);
        }
        void Connect(UnityEngine.UI.Button button,UnityEngine.UI.Selectable up,UnityEngine.UI.Selectable down,UnityEngine.UI.Selectable left,UnityEngine.UI.Selectable right)
        {var nav=button.navigation;nav.mode=UnityEngine.UI.Navigation.Mode.Explicit;nav.selectOnUp=up;nav.selectOnDown=down;nav.selectOnLeft=left;nav.selectOnRight=right;button.navigation=nav;}
        void QuitReview(){
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var r = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0,1);r.anchoredPosition = position;r.sizeDelta = size;return r;
        }
        TMP_Text Label(RectTransform rect,string value,int size)
        { var label=rect.gameObject.AddComponent<TextMeshProUGUI>();label.font=font;label.text=value;label.fontSize=size;label.color=theme.Text;label.raycastTarget=false;label.alignment=TextAlignmentOptions.MidlineLeft;return label; }
        UnityEngine.UI.Button Button(Transform parent,string title,UnityEngine.Events.UnityAction action)
        { var r=Rect(title,parent,Vector2.zero,new Vector2(300,54));return Button(r,title,action); }
        UnityEngine.UI.Button Button(RectTransform rect,string title,UnityEngine.Events.UnityAction action)
        {
            var image=rect.gameObject.AddComponent<UnityEngine.UI.Image>();image.color=theme.Raised;
            var button=rect.gameObject.AddComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(action);
            var colors=button.colors;colors.highlightedColor=theme.SeaGlass;colors.selectedColor=theme.SeaGlass;colors.pressedColor=theme.Copper;button.colors=colors;
            var textRect=Rect("Label",rect,new Vector2(12,-3),rect.sizeDelta-new Vector2(24,6));Label(textRect,title,18);
            var outline=rect.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=theme.Copper;outline.effectDistance=new Vector2(1,-1);return button;
        }
        void OnDestroy()
        {
            reviewInput?.Disable();if(reviewInput!=null)Destroy(reviewInput);
            QualitySettings.renderPipeline=previousPipeline;
            if(profile!=null)Destroy(profile);
        }
    }
}
