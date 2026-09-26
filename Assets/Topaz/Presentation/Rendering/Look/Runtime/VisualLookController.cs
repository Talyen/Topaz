using System;
using System.IO;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Topaz.Rendering
{
    /// <summary>URP presentation adapter; the simulation remains authoritative for time and weather.</summary>
    public sealed class VisualLookController : MonoBehaviour
    {
        public static readonly string[] SettingNames = { "Temperature", "Exposure", "Contrast", "Saturation", "Bloom", "Bloom threshold", "Vignette", "Focus start", "Focus end", "Focus radius", "Fog distance", "Home light", "Shadow", "Warmth" };
        public static readonly float[] Minimum = { -40,-2,-30,-40,0,.5f,0,1,2,.1f,40,0,0,-10 };
        public static readonly float[] Maximum = { 40,2,30,40,1,2,.4f,36,60,2,300,16,1,35 };
        [SerializeField] UniversalAdditionalCameraData cameraData;
        [SerializeField] Volume painterlyVolume;
        [SerializeField] ScriptableRendererFeature ambientOcclusionFeature;
        [SerializeField] Light sun;
        [SerializeField] Light moon;
        [SerializeField] Light homeLight;
        [SerializeField] VisualOptionsMenu optionsMenu;
        VolumeProfile profile;
        GraphicsPreferences settings = new GraphicsPreferences();
        double hours = WorldClock.StartingHour;
        float cloudiness, rain, fade;
        bool interior;
        public int CurrentLook => settings.look;
        public int CurrentAa => settings.antiAliasing;
        public int CurrentDepthMode => settings.focusMode;
        public int CurrentFocusMode => settings.focusMode;
        public bool AmbientOcclusionEnabled => settings.ambientOcclusion;
        public bool BloomEnabled => settings.bloomEnabled;
        public string SettingsPath { get; private set; }
        public bool HighQuality => settings.look == 1;
        public bool NativeResolution => settings.antiAliasing != 4;
        void Start()
        {
            if (painterlyVolume == null || sun == null || cameraData == null)
            { Debug.LogError("URP environment references are incomplete.", this); enabled = false; return; }
            profile = painterlyVolume.profile;
            string directory=Application.isEditor?Path.Combine(Application.temporaryCachePath,"TopazVisual-"+Guid.NewGuid().ToString("N")):Application.persistentDataPath;
            Directory.CreateDirectory(directory);
            SettingsPath = Path.Combine(directory, "urp-world-settings.json");
            if (!Application.isEditor && File.Exists(SettingsPath))
                try { settings = JsonUtility.FromJson<GraphicsPreferences>(File.ReadAllText(SettingsPath)) ?? settings; }
                catch (Exception e) { Debug.LogWarning("URP settings reset: " + e.Message); }
            Apply(); optionsMenu?.Bind(this);
        }
        public void SetLook(int look) { settings.look = Mathf.Clamp(look, 0, 1); Apply(); SaveSelection(); }
        public void SetWorldHours(double value) { hours = value; ApplyEnvironment(); }
        public void SetInterior(bool value) { interior = value; ApplyEnvironment(); }
        public void SetRestFade(float value) { fade = Mathf.Clamp01(value); ApplyEnvironment(); }
        public void SetWeather(float cloud, float precipitation) { cloudiness = Mathf.Clamp01(cloud); rain = Mathf.Clamp01(precipitation); ApplyEnvironment(); }
        public void SetAa(int value) { settings.antiAliasing = Mathf.Clamp(value, 0, 4); Apply(); SaveSelection(); }
        public void SetDepthMode(int mode) { settings.focusMode = Mathf.Clamp(mode, 0, 1); Apply(); SaveSelection(); }
        public void SetAmbientOcclusion(bool value) { settings.ambientOcclusion = value; Apply(); SaveSelection(); }
        public void SetBloom(bool value) { settings.bloomEnabled = value; Apply(); SaveSelection(); }
        public void ToggleDepthOfField() => SetDepthMode(1 - settings.focusMode);
        public void ToggleFocusMode() => ToggleDepthOfField();
        public void ToggleAmbientOcclusion() => SetAmbientOcclusion(!settings.ambientOcclusion);
        public string GetSettingName(int i) => SettingNames[i];
        public float GetMinimum(int i) => Minimum[i];
        public float GetMaximum(int i) => Maximum[i];
        public bool UsesWholeNumbers(int i) => false;
        public float GetSetting(int i) => i switch { 0=>settings.temperature, 1=>settings.exposure, 2=>settings.contrast, 3=>settings.saturation, 4=>settings.bloomIntensity, 5=>settings.bloomThreshold, 6=>settings.vignette, 7=>settings.depthStart, 8=>settings.depthEnd, 9=>settings.depthRadius, 10=>settings.fogEnd, 11=>settings.homeLight, 12=>settings.shadowStrength, _=>settings.homeWarmth };
        public void SetSetting(int i, float v)
        {
            if (i < 0 || i >= SettingNames.Length || float.IsNaN(v) || float.IsInfinity(v)) return;
            v = Mathf.Clamp(v, Minimum[i], Maximum[i]);
            switch(i) { case 0: settings.temperature=v; break; case 1: settings.exposure=v; break; case 2: settings.contrast=v; break; case 3: settings.saturation=v; break; case 4: settings.bloomIntensity=v; break; case 5: settings.bloomThreshold=v; break; case 6: settings.vignette=v; break; case 7: settings.depthStart=v; break; case 8: settings.depthEnd=v; break; case 9: settings.depthRadius=v; break; case 10: settings.fogEnd=v; break; case 11: settings.homeLight=v; break; case 12: settings.shadowStrength=v; break; case 13: settings.homeWarmth=v; break; }
            Apply(); SaveSelection();
        }
        public string FormatSetting(int i) => GetSetting(i).ToString("0.##");
        public void ResetSelection() { settings = new GraphicsPreferences(); Apply(); SaveSelection(); }
        public void SaveSelection()
        {
            if (string.IsNullOrEmpty(SettingsPath)) return;
            try { File.WriteAllText(SettingsPath, JsonUtility.ToJson(settings, true)); }
            catch(Exception e) { Debug.LogWarning("Could not save graphics settings: " + e.Message); }
        }
        public void CopySelection() => GUIUtility.systemCopyBuffer = JsonUtility.ToJson(settings, true);
        T Override<T>() where T : VolumeComponent => profile.TryGet(out T value) ? value : profile.Add<T>(true);
        void Apply()
        {
            if (profile == null) return;
            cameraData.antialiasing = settings.antiAliasing switch { 0 => AntialiasingMode.None, 1 => AntialiasingMode.FastApproximateAntialiasing, 2 => AntialiasingMode.SubpixelMorphologicalAntiAliasing, _ => AntialiasingMode.TemporalAntiAliasing };
            cameraData.renderPostProcessing = true;
            if (cameraData.TryGetComponent<Topaz.Rendering.UrpRenderScaleController>(out var resolution)) resolution.SetNative(NativeResolution);
            Override<Bloom>().intensity.Override(settings.bloomEnabled ? settings.bloomIntensity : 0);
            Override<ColorAdjustments>().contrast.Override(settings.contrast);
            Override<ColorAdjustments>().saturation.Override(settings.saturation);
            Override<WhiteBalance>().temperature.Override(settings.temperature);
            Override<Vignette>().intensity.Override(settings.vignette);
            ambientOcclusionFeature?.SetActive(settings.ambientOcclusion);
            Override<DepthOfField>().active = false;
            QualitySettings.lodBias = HighQuality ? 1.5f : 1;
            ApplyEnvironment();
        }
        void ApplyEnvironment()
        {
            if (profile == null) return;
            float hour = (float)(hours % 24);
            float daylight = Mathf.Clamp01(Mathf.Sin((hour - 6) / 24 * Mathf.PI * 2));
            sun.transform.rotation = Quaternion.Euler((hour - 6) * 15, -30, 0);
            sun.intensity = Mathf.Lerp(.02f, 1.6f, daylight) * Mathf.Lerp(1, .5f, cloudiness);
            sun.color = Color.Lerp(new Color(1,.66f,.38f), Color.white, Mathf.Clamp01(daylight * 3));
            if (moon != null) { moon.intensity = Mathf.Lerp(.12f, 0, daylight); moon.transform.rotation = Quaternion.Euler(45,120,0); }
            Override<ColorAdjustments>().postExposure.Override(settings.exposure - fade * 12);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(new Color(.035f,.05f,.09f),new Color(.45f,.55f,.7f),daylight);
            RenderSettings.ambientEquatorColor = Color.Lerp(new Color(.025f,.035f,.055f),new Color(.3f,.34f,.35f),daylight);
            RenderSettings.ambientGroundColor = Color.Lerp(new Color(.015f,.02f,.025f),new Color(.15f,.17f,.12f),daylight);
            RenderSettings.fog = !interior; RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = HighQuality ? Mathf.Lerp(.004f,.012f,rain) : Mathf.Lerp(.003f,.009f,rain);
            RenderSettings.fogColor = Color.Lerp(new Color(.025f,.035f,.06f),new Color(.5f,.59f,.65f),daylight);
            if (homeLight != null) homeLight.intensity = Mathf.Lerp(2.5f, .4f, daylight);
            Shader.SetGlobalFloat("_TopazWetness", rain);
            Shader.SetGlobalVector("_TopazWind", new Vector4(.5f + cloudiness, 0, .3f, Time.time));
        }
        void OnDestroy() { if(profile != null) Destroy(profile); }
    }
}
