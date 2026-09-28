using TMPro;
using System.Collections.Generic;
using Topaz.Menus;
using Topaz.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Topaz.Rendering
{
    /// <summary>Compact Graphics menu over authored URP settings.</summary>
    public sealed class VisualOptionsMenu : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Dropdown cameraZoomDropdown;
        [SerializeField] TMP_Dropdown antiAliasingDropdown;
        [SerializeField] TMP_Dropdown depthOfFieldDropdown;
        [SerializeField] Toggle bloomToggle;
        [SerializeField] Toggle ambientOcclusionToggle;
        [SerializeField] Button resetButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text status;
        [SerializeField] Topaz.Gameplay.LoopHud loopHud;
        [SerializeField] Button graphicsTabButton;
        [SerializeField] Button audioTabButton;
        [SerializeField] GameObject[] graphicsRows;
        [SerializeField] GameObject[] audioRows;
        [SerializeField] Slider masterSlider;
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider ambienceSlider;
        [SerializeField] Slider effectsSlider;
        [SerializeField] Toggle muteInBackgroundToggle;

        VisualLookController _controller;
        TopazAudioSettings _audio;
        GameMenus _menus;
        bool _bound;
        GameObject effectsScroll;
        ScrollRect effectsScroller;
        RectTransform effectsContent;
        TMP_Dropdown focusDropdown,lightingDropdown;
        readonly List<Slider> effectSliders=new List<Slider>();
        readonly List<TMP_Text> effectLabels=new List<TMP_Text>();
        GameObject lastSelection;
        readonly Vector3[] focusCorners=new Vector3[4];
        bool _audioTab;

        public bool IsOpen => panel != null && panel.activeSelf;

        public void Bind(VisualLookController controller)
        {
            if (_bound) return;
            _controller = controller;
            _menus = GetComponent<GameMenus>();
            _audio = GetComponent<TopazAudioSettings>();
            if (panel == null || cameraZoomDropdown == null || antiAliasingDropdown == null ||
                depthOfFieldDropdown == null || bloomToggle == null ||
                ambientOcclusionToggle == null || resetButton == null || closeButton == null ||
                status == null || loopHud == null || _menus == null || _audio == null ||
                graphicsTabButton == null || audioTabButton == null ||
                graphicsRows == null || graphicsRows.Length != 5 ||
                audioRows == null || audioRows.Length != 5 ||
                masterSlider == null || musicSlider == null || ambienceSlider == null ||
                effectsSlider == null || muteInBackgroundToggle == null)
            {
                Debug.LogError("Graphics menu is missing a required reference.", this);
                enabled = false;
                return;
            }
            SetOptions(cameraZoomDropdown, "Close", "Balanced", "Far");
            SetOptions(antiAliasingDropdown, "Off", "FXAA", "SMAA", "TAA (native)", "STP (adaptive)");
            SetOptions(depthOfFieldDropdown, "Balanced", "High");
            cameraZoomDropdown.onValueChanged.AddListener(OnCameraZoomChanged);
            antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
            depthOfFieldDropdown.onValueChanged.AddListener(OnDepthOfFieldChanged);
            bloomToggle.onValueChanged.AddListener(OnBloomChanged);
            ambientOcclusionToggle.onValueChanged.AddListener(OnAmbientOcclusionChanged);
            resetButton.onClick.AddListener(ResetToDefaults);
            closeButton.onClick.AddListener(Close);
            graphicsTabButton.onClick.AddListener(OpenGraphicsTab);
            audioTabButton.onClick.AddListener(OpenAudioTab);
            masterSlider.onValueChanged.AddListener(OnMasterChanged);
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
            ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);
            effectsSlider.onValueChanged.AddListener(OnEffectsChanged);
            muteInBackgroundToggle.onValueChanged.AddListener(OnMuteInBackgroundChanged);
            BuildEffectsControls();
            _bound = true;
            ShowTab(false);
            Refresh();
        }

        void OnDestroy()
        {
            if (!_bound) return;
            cameraZoomDropdown.onValueChanged.RemoveListener(OnCameraZoomChanged);
            antiAliasingDropdown.onValueChanged.RemoveListener(OnAntiAliasingChanged);
            depthOfFieldDropdown.onValueChanged.RemoveListener(OnDepthOfFieldChanged);
            bloomToggle.onValueChanged.RemoveListener(OnBloomChanged);
            ambientOcclusionToggle.onValueChanged.RemoveListener(OnAmbientOcclusionChanged);
            resetButton.onClick.RemoveListener(ResetToDefaults);
            closeButton.onClick.RemoveListener(Close);
            graphicsTabButton.onClick.RemoveListener(OpenGraphicsTab);
            audioTabButton.onClick.RemoveListener(OpenAudioTab);
            masterSlider.onValueChanged.RemoveListener(OnMasterChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
            ambienceSlider.onValueChanged.RemoveListener(OnAmbienceChanged);
            effectsSlider.onValueChanged.RemoveListener(OnEffectsChanged);
            muteInBackgroundToggle.onValueChanged.RemoveListener(OnMuteInBackgroundChanged);
        }

        static void SetOptions(TMP_Dropdown dropdown, params string[] labels)
        {
            dropdown.ClearOptions();
            foreach (string label in labels)
                dropdown.options.Add(new TMP_Dropdown.OptionData(label));
            dropdown.RefreshShownValue();
        }

        void OnCameraZoomChanged(int value) => Change(() => _menus.SetCameraZoomIndex(value));
        void OnAntiAliasingChanged(int value) => Change(() => _controller.SetAa(value));
        void OnDepthOfFieldChanged(int value) => Change(() => _controller.SetLook(value));
        void OnBloomChanged(bool value) => Change(() => _controller.SetBloom(value));
        void OnAmbientOcclusionChanged(bool value) =>
            Change(() => _controller.SetAmbientOcclusion(value));

        void OnMasterChanged(float value) => ChangeAudio(() => _audio.SetMaster(value));
        void OnMusicChanged(float value) => ChangeAudio(() => _audio.SetMusic(value));
        void OnAmbienceChanged(float value) => ChangeAudio(() => _audio.SetAmbience(value));
        void OnEffectsChanged(float value) => ChangeAudio(() => _audio.SetEffects(value));
        void OnMuteInBackgroundChanged(bool value) =>
            ChangeAudio(() => _audio.SetMuteInBackground(value));

        void ChangeAudio(System.Action apply)
        {
            apply();
            SetStatus("Audio settings saved automatically.");
        }

        void OpenGraphicsTab()
        {
            ShowTab(false);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                cameraZoomDropdown.gameObject);
        }

        void OpenAudioTab()
        {
            ShowTab(true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                masterSlider.gameObject);
        }

        void ShowTab(bool audio)
        {
            _audioTab = audio;
            foreach (GameObject row in graphicsRows) row.SetActive(!audio);
            if(effectsScroll!=null)effectsScroll.SetActive(!audio);
            foreach (GameObject row in audioRows) row.SetActive(audio);
            graphicsTabButton.GetComponent<Image>().color = audio
                ? new Color32(49, 37, 30, 255) : new Color32(239, 199, 132, 255);
            audioTabButton.GetComponent<Image>().color = audio
                ? new Color32(239, 199, 132, 255) : new Color32(49, 37, 30, 255);
            graphicsTabButton.GetComponentInChildren<TMP_Text>().color = audio
                ? new Color32(255, 241, 216, 255) : new Color32(23, 19, 18, 255);
            audioTabButton.GetComponentInChildren<TMP_Text>().color = audio
                ? new Color32(23, 19, 18, 255) : new Color32(255, 241, 216, 255);
        }

        void Change(System.Action apply)
        {
            try
            {
                apply();
                _controller.SaveSelection();
                SetStatus("Saved automatically.");
                Refresh();
            }
            catch (System.Exception error)
            {
                SetStatus("Could not save settings: " + error.Message);
                Debug.LogWarning("[Topaz] Graphics preference save failed: " + error.Message, this);
            }
        }

        void ResetToDefaults()
        {
            try
            {
                if (_audioTab)
                {
                    _audio.ResetDefaults();
                    Refresh();
                    SetStatus("Audio defaults restored and saved.");
                    return;
                }
                _controller.ResetSelection();
                _menus.SetCameraZoomIndex(1);
                _controller.SaveSelection();
                Refresh();
                SetStatus("Defaults restored and saved.");
            }
            catch (System.Exception error)
            {
                SetStatus("Could not save defaults: " + error.Message);
                Debug.LogWarning("[Topaz] Graphics reset failed: " + error.Message, this);
            }
        }

        public void Toggle()
        {
            bool open = !IsOpen;
            loopHud.ClosePanels();
            panel.SetActive(open);
            if (open)
            {
                _menus?.OnVisualLabOpening();
                panel.transform.SetAsLastSibling();
                ShowTab(false);
                Refresh();
                UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(
                    graphicsTabButton.gameObject);
            }
            else _menus?.OnVisualLabClosed();
        }

        public void Close()
        {
            panel.SetActive(false);
            _menus?.OnVisualLabClosed();
        }

        public void MarkUnsaved() => SetStatus("Applying…");

        public void Refresh()
        {
            if (_controller == null || _menus == null || !_bound) return;
            cameraZoomDropdown.SetValueWithoutNotify(_menus.CurrentCameraZoomIndex);
            antiAliasingDropdown.SetValueWithoutNotify(_controller.CurrentAa);
            depthOfFieldDropdown.SetValueWithoutNotify(_controller.CurrentLook);
            focusDropdown.SetValueWithoutNotify(_controller.CurrentFocusMode);
            lightingDropdown.SetValueWithoutNotify(_controller.CurrentLightingStyle);
            for(int i=0;i<effectSliders.Count;i++)
            {
                effectSliders[i].SetValueWithoutNotify(_controller.GetSetting(i));
                effectLabels[i].text=_controller.GetSettingName(i)+"  "+_controller.FormatSetting(i);
            }
            bloomToggle.SetIsOnWithoutNotify(_controller.BloomEnabled);
            ambientOcclusionToggle.SetIsOnWithoutNotify(_controller.AmbientOcclusionEnabled);
            masterSlider.SetValueWithoutNotify(_audio.Master);
            musicSlider.SetValueWithoutNotify(_audio.Music);
            ambienceSlider.SetValueWithoutNotify(_audio.Ambience);
            effectsSlider.SetValueWithoutNotify(_audio.Effects);
            muteInBackgroundToggle.SetIsOnWithoutNotify(_audio.MuteInBackground);
        }

        void BuildEffectsControls()
        {
            effectsScroll=new GameObject("Graphics Effects",typeof(RectTransform),typeof(ScrollRect));
            var rect=(RectTransform)effectsScroll.transform;rect.SetParent(panel.transform,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
            rect.anchoredPosition=new Vector2(0,-164);rect.sizeDelta=new Vector2(840,458);
            var viewport=new GameObject("Viewport",typeof(RectTransform),typeof(Image),typeof(Mask));
            var view=(RectTransform)viewport.transform;view.SetParent(rect,false);view.anchorMin=Vector2.zero;view.anchorMax=Vector2.one;view.offsetMin=view.offsetMax=Vector2.zero;
            viewport.GetComponent<Image>().color=Color.white;viewport.GetComponent<Mask>().showMaskGraphic=false;
            effectsContent=(RectTransform)new GameObject("Content",typeof(RectTransform),typeof(VerticalLayoutGroup),typeof(ContentSizeFitter)).transform;
            effectsContent.SetParent(view,false);effectsContent.anchorMin=new Vector2(0,1);effectsContent.anchorMax=Vector2.one;effectsContent.pivot=new Vector2(.5f,1);effectsContent.sizeDelta=Vector2.zero;
            var layout=effectsContent.GetComponent<VerticalLayoutGroup>();layout.spacing=10;layout.padding=new RectOffset(22,22,8,8);layout.childControlHeight=true;layout.childControlWidth=true;layout.childForceExpandHeight=false;
            effectsContent.GetComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            effectsScroller=effectsScroll.GetComponent<ScrollRect>();effectsScroller.viewport=view;effectsScroller.content=effectsContent;effectsScroller.horizontal=false;effectsScroller.movementType=ScrollRect.MovementType.Clamped;effectsScroller.scrollSensitivity=35;
            void Row(GameObject row){row.transform.SetParent(effectsContent,false);var size=row.GetComponent<LayoutElement>()??row.AddComponent<LayoutElement>();size.preferredHeight=76;size.minHeight=76;}
            foreach(var row in graphicsRows)Row(row);
            var qualityLabel=depthOfFieldDropdown.transform.parent.GetComponentInChildren<TMP_Text>();qualityLabel.text="Quality";
            var focusRow=Instantiate(depthOfFieldDropdown.transform.parent.gameObject,effectsContent);focusRow.name="Depth of field mode";Row(focusRow);
            focusDropdown=focusRow.GetComponentInChildren<TMP_Dropdown>();focusDropdown.onValueChanged=new TMP_Dropdown.DropdownEvent();
            focusRow.GetComponentInChildren<TMP_Text>().text="Depth of field";SetOptions(focusDropdown,"Off","Distant softness","Cinematic bokeh");
            focusDropdown.onValueChanged.AddListener(value=>Change(()=>_controller.SetDepthMode(value)));
            var lightingRow=Instantiate(depthOfFieldDropdown.transform.parent.gameObject,effectsContent);lightingRow.name="Lighting style";Row(lightingRow);
            lightingDropdown=lightingRow.GetComponentInChildren<TMP_Dropdown>();lightingDropdown.onValueChanged=new TMP_Dropdown.DropdownEvent();
            lightingRow.GetComponentInChildren<TMP_Text>().text="Lighting style";SetOptions(lightingDropdown,"Natural cycle","Golden / Silver");
            lightingDropdown.onValueChanged.AddListener(value=>Change(()=>_controller.SetLightingStyle(value)));
            for(int i=0;i<VisualLookController.SettingNames.Length;i++)
            {
                int setting=i;var row=Instantiate(masterSlider.transform.parent.gameObject,effectsContent);row.name="Effect "+_controller.GetSettingName(i);row.SetActive(true);Row(row);
                var slider=row.GetComponentInChildren<Slider>();slider.onValueChanged=new Slider.SliderEvent();slider.minValue=_controller.GetMinimum(i);slider.maxValue=_controller.GetMaximum(i);slider.wholeNumbers=false;
                var label=row.GetComponentInChildren<TMP_Text>();effectLabels.Add(label);effectSliders.Add(slider);
                slider.onValueChanged.AddListener(value=>Change(()=>_controller.SetSetting(setting,value)));
            }
            foreach(var selectable in effectsContent.GetComponentsInChildren<Selectable>(true))
            {var navigation=selectable.navigation;navigation.mode=Navigation.Mode.Automatic;selectable.navigation=navigation;}
            var explanation=panel.transform.Find("Explanation")?.GetComponent<TMP_Text>();
            if(explanation!=null)
            {
                explanation.text="Shape the world’s look. Scroll for lighting, lens and foliage controls.";
                explanation.fontSize=18;explanation.rectTransform.sizeDelta=new Vector2(780,32);explanation.rectTransform.anchoredPosition=new Vector2(0,-91);
            }
        }
        void Update()
        {
            if(!IsOpen || _audioTab || effectsContent==null)return;
            var selected=UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            if(selected==null || selected==lastSelection)return;lastSelection=selected;
            if(!selected.transform.IsChildOf(effectsContent))return;
            Canvas.ForceUpdateCanvases();
            var row=selected.transform;
            while(row.parent!=effectsContent)row=row.parent;
            ((RectTransform)row).GetWorldCorners(focusCorners);
            float rowBottom=effectsScroller.viewport.InverseTransformPoint(focusCorners[0]).y;
            float rowTop=effectsScroller.viewport.InverseTransformPoint(focusCorners[1]).y;
            float top=effectsScroller.viewport.rect.yMax,bottom=effectsScroller.viewport.rect.yMin;
            float offset=rowTop>top?rowTop-top:rowBottom<bottom?rowBottom-bottom:0;
            var position=effectsContent.anchoredPosition;
            position.y=Mathf.Clamp(position.y-offset,0,Mathf.Max(0,effectsContent.rect.height-effectsScroller.viewport.rect.height));
            effectsContent.anchoredPosition=position;
        }

        void SetStatus(string message)
        {
            if (status != null) status.text = message;
        }
    }
}
