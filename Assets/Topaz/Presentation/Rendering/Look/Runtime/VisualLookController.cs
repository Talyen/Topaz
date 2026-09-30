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
        public static readonly string[] SettingNames = { "Temperature", "Exposure", "Contrast", "Saturation", "Bloom", "Bloom threshold", "Vignette", "Focus start", "Focus end", "Focus radius", "Fog distance", "Home light", "Shadow", "Warmth", "Motion blur", "Chromatic aberration", "Color grading", "Wind", "Foliage density", "Ambient particles", "Bokeh aperture", "Bokeh focal length" };
        public static readonly float[] Minimum = { -40,-2,-30,-40,0,.5f,0,1,2,.1f,40,0,0,-10,0,0,0,0,0,0,1,30 };
        public static readonly float[] Maximum = { 40,2,30,40,1,2,.4f,60,160,2,400,16,1,35,.7f,.25f,1,2,1,2,16,100 };
        [SerializeField] UniversalAdditionalCameraData cameraData;
        [SerializeField] Volume painterlyVolume;
        [SerializeField] ScriptableRendererFeature ambientOcclusionFeature;
        [SerializeField] ScriptableRendererFeature surfaceCacheFeature;
        [SerializeField] Light sun;
        [SerializeField] Light moon;
        [SerializeField] Light homeLight;
        [SerializeField] VisualOptionsMenu optionsMenu;
        VolumeProfile profile;
        Transform focusSubject;
        Camera focusCamera;
        public void SetFocusSubject(Transform subject,Camera camera){focusSubject=subject;focusCamera=camera;}
        GraphicsPreferences settings = new GraphicsPreferences();
        double hours = WorldClock.StartingHour;
        float cloudiness, rain, fade, wetness;
        double windSeconds;
        Vector4 wind,previousWind;
        Material runtimeSky,originalSky;
        bool resetGiAfterWorldReady;
        Topaz.Gameplay.WorldSession session;
        public EnvironmentPresentationState State { get; private set; }
        public void FreezePresentation(){previousWind=wind;ApplyEnvironment();}
        public void BeginWorldPresentation(double initialHours)
        {
            resetGiAfterWorldReady = true;
            hours=initialHours;wetness=0;windSeconds=initialHours*112.5;
            AdvancePresentation(.001f,0);previousWind=wind;ApplyEnvironment();
        }
        public void AdvancePresentation(float activeSeconds,float precipitation)
        {
            if(activeSeconds<=0)return;
            wetness=EnvironmentPresentationState.AdvanceWetness(wetness,precipitation,activeSeconds);
            previousWind=wind;windSeconds+=activeSeconds;
            float gust=1+Mathf.Sin((float)windSeconds*.37f)*.18f+Mathf.Sin((float)windSeconds*.11f)*.12f;
            wind=new Vector4((.4f+cloudiness*.45f)*gust,0,.25f*gust,(float)windSeconds);
        }
        bool interior,noSunShadows,isolatedSettings;
        public int CurrentLightingStyle => settings.lightingStyle;
        public void SetLightingStyle(int value){settings.lightingStyle=Mathf.Clamp(value,0,1);Apply();SaveSelection();}
        public int CurrentLook => settings.look;
        public int CurrentAa => settings.antiAliasing;
        public int CurrentDepthMode => settings.focusMode;
        public int CurrentFocusMode => settings.focusMode;
        public bool AmbientOcclusionEnabled => settings.ambientOcclusion;
        public bool BloomEnabled => settings.bloomEnabled;
        public string SettingsPath { get; private set; }
        public float FoliageDensity => settings.foliageDensity;
        public float AmbientParticles => settings.ambientParticles;
        public bool HighQuality => settings.look == 1;
        public bool NativeResolution => settings.antiAliasing != 4;
        public SurfaceCacheLighting.Status GlobalIllumination => SurfaceCacheLighting.Describe(profile);
        void Start()
        {
            if (painterlyVolume == null || sun == null || cameraData == null)
            { Debug.LogError("URP environment references are incomplete.", this); enabled = false; return; }
            profile = painterlyVolume.profile;
            session=FindAnyObjectByType<Topaz.Gameplay.WorldSession>();
            originalSky=RenderSettings.skybox;
            var sky=Resources.Load<Material>("TopazOutdoorSky");
            if(sky!=null){runtimeSky=new Material(sky);RenderSettings.skybox=runtimeSky;}
            var backdrop=Resources.Load<Cubemap>("TopazAlpineBackdrop");
            Shader.SetGlobalFloat("_TopazHasBackdrop",backdrop!=null?1:0);
            if(backdrop!=null)Shader.SetGlobalTexture("_TopazAlpineBackdrop",backdrop);
            windSeconds=hours*112.5;AdvancePresentation(.001f,0);previousWind=wind;
            noSunShadows=Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-no-sun-shadows")>=0;
            bool isolated=Application.isEditor || Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-smoke")>=0;
            isolatedSettings=isolated;
            string directory=isolated?Path.Combine(Application.temporaryCachePath,"TopazVisual-"+Guid.NewGuid().ToString("N")):Application.persistentDataPath;
            Directory.CreateDirectory(directory);
            SettingsPath = Path.Combine(directory, "urp-world-settings.json");
            if (!isolated && File.Exists(SettingsPath))
                try { settings = JsonUtility.FromJson<GraphicsPreferences>(File.ReadAllText(SettingsPath)) ?? settings; }
                catch (Exception e) { Debug.LogWarning("URP settings reset: " + e.Message); }
            if(settings.version != GraphicsPreferences.CurrentVersion) settings = new GraphicsPreferences();
            bool adoptedMacPreset=!isolated && Application.platform==RuntimePlatform.OSXPlayer && settings.ApplyMacStartingPreset();
            if(isolated && Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-stp")>=0)settings.antiAliasing=4;
            if(isolated && Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-balanced")>=0)settings.look=0;
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-smoke")>=0)
                foreach(var argument in Environment.GetCommandLineArgs())
                    if(argument.StartsWith("--topaz-grass-density=") && float.TryParse(argument.Substring(22),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out float density))
                        settings.foliageDensity=Mathf.Clamp01(density);
            Apply();
            if(adoptedMacPreset)SaveSelection();
            optionsMenu?.Bind(this);
        }
        public void SetLook(int look) { settings.look = Mathf.Clamp(look, 0, 1); Apply(); SaveSelection(); }
        public void SetWorldHours(double value)
        {
            // Rest skips are a single bounded update; normal frames are advanced by WeatherPresentation.
            double delta=value-hours;
            if(delta>.1)AdvancePresentation((float)(delta*112.5),rain);
            hours=value;ApplyEnvironment();
        }
        public void SetInterior(bool value) { interior = value; ApplyEnvironment(); }
        public void SetRestFade(float value) { fade = Mathf.Clamp01(value); ApplyEnvironment(); }
        public void SetWeather(float cloud, float precipitation) { cloudiness = Mathf.Clamp01(cloud); rain = Mathf.Clamp01(precipitation); ApplyEnvironment(); }
        public void SetAa(int value) { settings.antiAliasing = Mathf.Clamp(value, 0, 4); Apply(); SaveSelection(); }
        public void SetDepthMode(int mode) { settings.focusMode = Mathf.Clamp(mode, 0, 2); Apply(); SaveSelection(); }
        public void SetAmbientOcclusion(bool value) { settings.ambientOcclusion = value; Apply(); SaveSelection(); }
        public void SetBloom(bool value) { settings.bloomEnabled = value; Apply(); SaveSelection(); }
        public void ToggleDepthOfField() => SetDepthMode((settings.focusMode + 1) % 3);
        public void ToggleFocusMode() => ToggleDepthOfField();
        public void ToggleAmbientOcclusion() => SetAmbientOcclusion(!settings.ambientOcclusion);
        public string GetSettingName(int i) => SettingNames[i];
        public float GetMinimum(int i) => Minimum[i];
        public float GetMaximum(int i) => Maximum[i];
        public bool UsesWholeNumbers(int i) => false;
        public float GetSetting(int i) => i switch { 0=>settings.temperature, 1=>settings.exposure, 2=>settings.contrast, 3=>settings.saturation, 4=>settings.bloomIntensity, 5=>settings.bloomThreshold, 6=>settings.vignette, 7=>settings.depthStart, 8=>settings.depthEnd, 9=>settings.depthRadius, 10=>settings.fogEnd, 11=>settings.homeLight, 12=>settings.shadowStrength, 13=>settings.homeWarmth, 14=>settings.motionBlur, 15=>settings.chromaticAberration, 16=>settings.grading, 17=>settings.windStrength, 18=>settings.foliageDensity, 19=>settings.ambientParticles, 20=>settings.bokehAperture, _=>settings.bokehFocalLength };
        public void SetSetting(int i, float v)
        {
            if (i < 0 || i >= SettingNames.Length || float.IsNaN(v) || float.IsInfinity(v)) return;
            v = Mathf.Clamp(v, Minimum[i], Maximum[i]);
            switch(i) { case 0: settings.temperature=v; break; case 1: settings.exposure=v; break; case 2: settings.contrast=v; break; case 3: settings.saturation=v; break; case 4: settings.bloomIntensity=v; break; case 5: settings.bloomThreshold=v; break; case 6: settings.vignette=v; break; case 7: settings.depthStart=v; break; case 8: settings.depthEnd=v; break; case 9: settings.depthRadius=v; break; case 10: settings.fogEnd=v; break; case 11: settings.homeLight=v; break; case 12: settings.shadowStrength=v; break; case 13: settings.homeWarmth=v; break; case 14: settings.motionBlur=v; break; case 15: settings.chromaticAberration=v; break; case 16: settings.grading=v; break; case 17: settings.windStrength=v; break; case 18: settings.foliageDensity=v; break; case 19: settings.ambientParticles=v; break; case 20: settings.bokehAperture=v; break; case 21: settings.bokehFocalLength=v; break; }
            Apply(); SaveSelection();
        }
        public string FormatSetting(int i) => GetSetting(i).ToString("0.##");
        public void ResetSelection()
        {
            settings=new GraphicsPreferences();
            if(!isolatedSettings && Application.platform==RuntimePlatform.OSXPlayer)settings.ApplyMacStartingPreset();
            Apply();SaveSelection();
        }
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
            SurfaceCacheLighting.Apply(profile, HighQuality);
            cameraData.antialiasing = settings.antiAliasing switch { 0 => AntialiasingMode.None, 1 => AntialiasingMode.FastApproximateAntialiasing, 2 => AntialiasingMode.SubpixelMorphologicalAntiAliasing, _ => AntialiasingMode.TemporalAntiAliasing };
            cameraData.renderPostProcessing = true;
            cameraData.dithering = false;
            cameraData.taaSettings.quality = HighQuality ? TemporalAAQuality.VeryHigh : TemporalAAQuality.High;
            cameraData.taaSettings.baseBlendFactor = .92f;
            cameraData.taaSettings.mipBias = 0;
            cameraData.taaSettings.contrastAdaptiveSharpening = .08f;
            cameraData.taaSettings.varianceClampScale = 1f;
            Override<Tonemapping>().mode.Override(TonemappingMode.ACES);
            Override<ColorLookup>().texture.Override(Resources.Load<Texture2D>("TopazFantasyLut"));
            Override<ColorLookup>().contribution.Override(.7f);
            Override<FilmGrain>().intensity.Override(0);
            Override<Bloom>().threshold.Override(settings.bloomThreshold);
            // Quarter-resolution Dual keeps broad authored halos without the full Gaussian pyramid.
            Override<Bloom>().filter.Override(BloomFilterMode.Dual);
            Override<Bloom>().downscale.Override(BloomDownscaleMode.Quarter);
            Override<Bloom>().maxIterations.Override(5);
            Override<Bloom>().scatter.Override(.68f);
            Override<Bloom>().highQualityFiltering.Override(false);
            Override<MotionBlur>().intensity.Override(settings.motionBlur);
            Override<MotionBlur>().clamp.Override(.025f);
            Override<ChromaticAberration>().intensity.Override(settings.chromaticAberration);
            var split=Override<SplitToning>();
            split.shadows.Override(Color.Lerp(new Color(.5f,.5f,.5f),new Color(.43f,.48f,.56f),settings.grading));
            split.highlights.Override(Color.Lerp(new Color(.5f,.5f,.5f),new Color(.57f,.53f,.45f),settings.grading));
            split.balance.Override(10);
            if (cameraData.TryGetComponent<Topaz.Rendering.UrpRenderScaleController>(out var resolution)) {resolution.SetNative(NativeResolution);resolution.SetQuality(HighQuality);}
            Override<Bloom>().intensity.Override(settings.bloomEnabled ? settings.bloomIntensity : 0);
            Override<ColorAdjustments>().contrast.Override(settings.contrast);
            Override<ColorAdjustments>().saturation.Override(settings.saturation);
            Override<WhiteBalance>().temperature.Override(settings.temperature);
            Override<Vignette>().intensity.Override(settings.vignette);
            ambientOcclusionFeature?.SetActive(settings.ambientOcclusion);
            var focus=Override<DepthOfField>();
            focus.active = settings.focusMode != 0;
            focus.mode.Override(settings.focusMode==2 ? DepthOfFieldMode.Bokeh : DepthOfFieldMode.Gaussian);
            focus.gaussianStart.Override(settings.depthStart);
            focus.gaussianEnd.Override(Mathf.Max(settings.depthStart+1,settings.depthEnd));
            focus.gaussianMaxRadius.Override(settings.depthRadius);
            focus.highQualitySampling.Override(HighQuality);
            focus.aperture.Override(settings.bokehAperture);
            focus.focalLength.Override(settings.bokehFocalLength);
            focus.bladeCount.Override(6);
            focus.focusDistance.Override(settings.bokehFocusDistance);
            QualitySettings.lodBias = HighQuality ? 1.1f : .85f;
            Shader.SetGlobalVector("_TopazGrassDistance",HighQuality?new Vector4(55,80,0,0):new Vector4(38,56,0,0));
            ApplyEnvironment();
        }
        void ApplyEnvironment()
        {
            if (profile == null) return;
            float hour = (float)(hours % 24);
            var plan=session?.ActiveRegion?.Wilderness;
            var point=session!=null?session.transform.position:Vector3.zero;
            var biome=plan!=null?plan.Biomes(point.x,point.z):default;
            State=new EnvironmentPresentationState(hours,cloudiness,rain,wetness,wind,previousWind,settings.fogEnd,
                new Vector3(biome.Meadow,biome.Woodland,biome.Highland),plan?.Moisture(point.x,point.z)??0,plan?.WaterDepth(point.x,point.z)??0,settings.lightingStyle==1);
            float daylight=State.Daylight;
            bool sunDominant=moon==null || State.SunIntensity>=State.MoonIntensity;
            sun.transform.rotation = Quaternion.Euler(Mathf.Sin((hour - 6) / 24 * Mathf.PI * 2) * (settings.lightingStyle==1?44:58), -40 + (hour - 12) * 12, 0);
            sun.renderingLayerMask|=(int)SurfaceCacheLighting.VisualOnlyRenderingLayer;
            if(moon!=null)moon.renderingLayerMask|=(int)SurfaceCacheLighting.VisualOnlyRenderingLayer;
            sun.intensity = State.SunIntensity;
            sun.useColorTemperature=false;
            sun.shadowStrength = settings.shadowStrength*(settings.lightingStyle==1?.86f:1);
            sun.color = State.Sun;
            if (moon != null) { moon.useColorTemperature=false;moon.color=settings.lightingStyle==1?new Color(.78f,.84f,1):new Color(.6f,.72f,1);moon.intensity = State.MoonIntensity; moon.shadowStrength=settings.shadowStrength*.8f;moon.shadows=sunDominant?LightShadows.None:LightShadows.Soft; moon.transform.rotation = Quaternion.Euler(settings.lightingStyle==1?25:45,120,0); }
            sun.shadows=!noSunShadows && sunDominant?LightShadows.Soft:LightShadows.None;
            RenderSettings.sun=sunDominant?sun:moon;
            var lightDirection=-sun.transform.forward;
            Shader.SetGlobalVector("_TopazSkySun",new Vector4(lightDirection.x,lightDirection.y,lightDirection.z,daylight));
            Shader.SetGlobalVector("_TopazSkyWeather",new Vector4(cloudiness,(float)windSeconds,0,0));
            Shader.SetGlobalColor("_TopazSkyLight",State.Sun.linear);
            var moonDirection=moon!=null?-moon.transform.forward:Vector3.up;
            Shader.SetGlobalVector("_TopazSkyMoon",new Vector4(moonDirection.x,moonDirection.y,moonDirection.z,1-daylight));
            Override<ColorAdjustments>().postExposure.Override(settings.exposure + (1-daylight)*.3f - fade * 12);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = State.AmbientSky;
            RenderSettings.ambientEquatorColor = State.AmbientEquator;
            RenderSettings.ambientGroundColor = State.AmbientGround;
            // Opaques are fogged once by the URP full-screen pass before transparent rendering.
            RenderSettings.fog=false;
            Shader.SetGlobalColor("_TopazFogColor",State.Fog.linear);
            Shader.SetGlobalColor("_TopazSkyZenith",State.Sky.linear);Shader.SetGlobalColor("_TopazSkyHorizon",State.Horizon.linear);
            Shader.SetGlobalVector("_TopazFogParameters",interior?Vector4.zero:State.FogParameters);
            float visibilityEnd=Mathf.Clamp(settings.fogEnd*1.1f,180,350);
            Shader.SetGlobalVector("_TopazVisibility",new Vector4(visibilityEnd*.38f,visibilityEnd,interior?0:1,0));
            cameraData.GetComponent<Topaz.Player.PlayerCamera>()?.SetVisibilityDistance(visibilityEnd+32);
            if(runtimeSky!=null){runtimeSky.SetVector("_SkyColor",State.Sky.linear);runtimeSky.SetVector("_HorizonColor",State.Horizon.linear);}
            if (homeLight != null) { homeLight.intensity = settings.homeLight*Mathf.Lerp(.25f,.04f,daylight); homeLight.color=Color.Lerp(Color.white,new Color(1,.6f,.25f),Mathf.Clamp01(settings.homeWarmth/35f)); }
            Shader.SetGlobalFloat("_TopazWetness",wetness);
            Shader.SetGlobalVector("_TopazWind",new Vector4(wind.x*settings.windStrength,wind.y,wind.z*settings.windStrength,wind.w));
            Shader.SetGlobalVector("_TopazPreviousWind",new Vector4(previousWind.x*settings.windStrength,previousWind.y,previousWind.z*settings.windStrength,previousWind.w));
        }
        void LateUpdate()
        {
            if(profile==null || cameraData==null)return;
            if(resetGiAfterWorldReady && session?.ActiveRegion?.Streaming?.InitialReady==true)
            {
                // The 17.6 preview can retain invalid irradiance seeded while the initial
                // procedural world is incomplete. Start its native cache with ready geometry.
                // Ordinary chunk streaming continues through Unity's native object tracking.
                if(surfaceCacheFeature==null)
                    throw new InvalidOperationException("[Topaz/SCGI] Missing native renderer feature reference.");
                surfaceCacheFeature.Create();
                resetGiAfterWorldReady=false;
            }
            var focus=Override<DepthOfField>();
            // URP's photographic depth-of-field path assumes a perspective projection.
            // Keep the authored orthographic title stage sharp, then restore the chosen game focus.
            focus.active=settings.focusMode!=0 && (focusCamera==null || !focusCamera.orthographic);
            if(!focus.active || settings.focusMode!=2 || (focusSubject==null && session==null))return;
            // Track the player plane, retaining enemies and interactions near the player in focus.
            var view=focusCamera!=null?focusCamera.transform:cameraData.transform;
            var subject=focusSubject!=null?focusSubject:session.transform;
            float distance=Vector3.Dot(subject.position+Vector3.up-view.position,view.forward);
            float previous=focus.focusDistance.value;
            float tracked=Mathf.Abs(previous-distance)>16?distance:Mathf.Lerp(previous,distance,1-Mathf.Exp(-Time.unscaledDeltaTime/.2f));
            focus.focusDistance.Override(Mathf.Max(.5f,tracked));
        }
        void OnDestroy()
        {
            if(profile!=null)Destroy(profile);
            if(RenderSettings.skybox==runtimeSky)RenderSettings.skybox=originalSky;
            if(runtimeSky!=null)Destroy(runtimeSky);
            Shader.SetGlobalVector("_TopazFogParameters",Vector4.zero);
            Shader.SetGlobalVector("_TopazVisibility",Vector4.zero);Shader.SetGlobalFloat("_TopazHasBackdrop",0);
            Shader.SetGlobalFloat("_TopazWetness",0);
        }
    }
}
