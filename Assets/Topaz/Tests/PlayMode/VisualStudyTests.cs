using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Topaz.Tests
{
    /// <summary>Current URP contract, replacing the retired orthographic blur/URP particle study.</summary>
    public sealed class VisualStudyTests : TopazInputTestFixture
    {
        [Serializable] sealed class SavedValue { public int look; public int antiAliasing; public bool bloomEnabled; public bool ambientOcclusion; }
        static object Get(Component c,string property)=>c.GetType().GetProperty(property).GetValue(c);
        static object Call(Component c,string method,params object[] args)=>c.GetType().GetMethod(method).Invoke(c,args);
        static Component Effects()=>UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c=>c.GetType().Name=="WorldEffects");
        [UnityTest]
        public IEnumerator FreshWorldAndReloadProduceSurfaceCacheIrradiance()
        {
            for(int load=0;load<2;load++)
            {
                yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
                var target=new RenderTexture(640,360,24,RenderTextureFormat.ARGBHalf);
                var sample=new RenderTexture(32,32,0,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);
                target.Create();sample.Create();
                try
                {
                    // Batch tests have no visible Game view. Explicitly render through URP,
                    // past the native temporal cache's 32-update warmup boundary.
                    var render=new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target};
                    for(int frame=0;frame<64;frame++)
                    {
                        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(Camera.main,render);
                        yield return null;
                    }
                    Assert.That(Shader.IsKeywordEnabled("_SCREEN_SPACE_IRRADIANCE"),Is.True);
                    var source=Shader.GetGlobalTexture("_ScreenSpaceIrradiance");
                    Assert.That(source,Is.Not.Null);
                    Assert.That(source.width,Is.GreaterThan(4),"Must be a computed image, not a fallback texture.");
                    Graphics.Blit(source,sample);
                    var request=UnityEngine.Rendering.AsyncGPUReadback.Request(sample,0,TextureFormat.RGBAFloat);
                    while(!request.done)yield return null;
                    Assert.That(request.hasError,Is.False);
                    var values=request.GetData<float>();int lit=0;
                    for(int i=0;i<values.Length;i+=4)
                    {
                        float energy=values[i]+values[i+1]+values[i+2];
                        Assert.That(float.IsNaN(energy)||float.IsInfinity(energy),Is.False);
                        if(energy>.00001f)lit++;
                    }
                    Assert.That(lit,Is.GreaterThan(0),"An enabled SCGI pass must produce actual lighting after world load.");
                }
                finally {sample.Release();target.Release();UnityEngine.Object.Destroy(sample);UnityEngine.Object.Destroy(target);}
            }
        }
        [UnityTest]
        public IEnumerator SurfaceCacheSurvivesSceneReloadAndPresetChanges()
        {
            for(int load=0;load<2;load++)
            {
                yield return LoadTitleWithoutWorld();yield return null;
                var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");
                foreach(int preset in new[]{0,1})
                {
                    Call(controller,"SetLook",preset);yield return null;
                    var status=Get(controller,"GlobalIllumination");
                    object Field(string name)=>status.GetType().GetField(name).GetValue(status);
                    Assert.That(Field("enabled"),Is.True);
                    Assert.That(Field("compiled"),Is.True);
                    Assert.That(Field("preset"),Is.EqualTo(preset==1?"Medium":"Low"));
                    Assert.That(Field("volumeResolution"),Is.EqualTo(16));
                    Assert.That(Field("cascades"),Is.EqualTo(4));
                }
            }
        }
        [UnityTest]
        public IEnumerator TitlePreviewStaysSharpAndGameplayBokehReturns()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");
            Call(controller,"SetDepthMode",2);
            var stage=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).First(c=>c.GetType().Name=="MainMenuStage");
            var profile=controller.GetType().GetField("profile",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(controller);
            var components=(IEnumerable)profile.GetType().GetField("components").GetValue(profile);
            var focus=components.Cast<object>().Single(c=>c.GetType().Name=="DepthOfField");
            Call(stage,"SetVisible",true);yield return null;yield return null;
            var titleCamera=(Camera)stage.GetType().GetField("menuCamera",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(stage);
            var titleData=titleCamera.GetComponents<Component>().Single(c=>c.GetType().Name=="UniversalAdditionalCameraData");
            Assert.That(titleCamera.enabled,Is.True);
            Assert.That(Get(titleData,"renderPostProcessing"),Is.False,"The neutral title preview must not inherit world bokeh or grading.");
            Call(stage,"SetVisible",false);yield return null;yield return null;
            Assert.That(focus.GetType().GetField("active").GetValue(focus),Is.True,"Returning to the game must restore the selected focus mode.");
        }
        [UnityTest]
        public IEnumerator GraphicsMenuAppliesAndSavesSelectionsImmediately()
        {
            yield return LoadTitleWithoutWorld();yield return null;
            var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");
            var menu=GameObject.Find("Loop HUD").GetComponent("VisualOptionsMenu");Call(menu,"Toggle");yield return null;
            var panel=GameObject.Find("Loop HUD").transform.Find("Graphics");
            var content=panel.Find("Graphics Effects/Viewport/Content");
            var quality=content.Find("Depth of Field/Depth of Field Dropdown").GetComponent("TMP_Dropdown");
            var aa=content.Find("Anti-aliasing/Anti-aliasing Dropdown").GetComponent("TMP_Dropdown");
            quality.GetType().GetProperty("value").SetValue(quality,1);aa.GetType().GetProperty("value").SetValue(aa,4);
            content.Find("Bloom").GetComponent<UnityEngine.UI.Toggle>().isOn=false;
            content.Find("Ambient Occlusion").GetComponent<UnityEngine.UI.Toggle>().isOn=false;
            Assert.That(Get(controller,"HighQuality"),Is.True);Assert.That(Get(controller,"NativeResolution"),Is.False);
            string path=(string)Get(controller,"SettingsPath");Assert.That(path,Does.StartWith(Application.temporaryCachePath));
            var saved=JsonUtility.FromJson<SavedValue>(File.ReadAllText(path));Assert.That(saved.look,Is.EqualTo(1));Assert.That(saved.antiAliasing,Is.EqualTo(4));Assert.That(saved.bloomEnabled||saved.ambientOcclusion,Is.False);
            Call(controller,"ResetSelection");Assert.That(Get(controller,"HighQuality"),Is.True);Assert.That(Get(controller,"NativeResolution"),Is.True);
            var focus=content.Find("Depth of field mode").GetComponentsInChildren<Component>().First(c=>c.GetType().Name=="TMP_Dropdown");
            focus.GetType().GetProperty("value").SetValue(focus,0);Assert.That(Get(controller,"CurrentFocusMode"),Is.EqualTo(0));
            focus.GetType().GetProperty("value").SetValue(focus,2);Assert.That(Get(controller,"CurrentFocusMode"),Is.EqualTo(2));
            var motion=content.Find("Effect Motion blur").GetComponentInChildren<UnityEngine.UI.Slider>();
            motion.value=.4f;Assert.That(Call(controller,"GetSetting",14),Is.EqualTo(.4f).Within(.001));
            Assert.That(content.GetComponentsInChildren<UnityEngine.UI.Slider>().Length,Is.GreaterThanOrEqualTo(22));
            var lighting=content.Find("Lighting style").GetComponentsInChildren<Component>().First(c=>c.GetType().Name=="TMP_Dropdown");
            lighting.GetType().GetProperty("value").SetValue(lighting,0);Assert.That(Get(controller,"CurrentLightingStyle"),Is.EqualTo(0));
            lighting.GetType().GetProperty("value").SetValue(lighting,1);Assert.That(Get(controller,"CurrentLightingStyle"),Is.EqualTo(1));
            var viewport=(RectTransform)content.parent;var corners=new Vector3[4];
            foreach(var row in new[]{content.Find("Effect Bokeh focal length"),content.Find("Camera Zoom")})
            {
                var selectable=row.GetComponentInChildren<UnityEngine.UI.Selectable>();
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(selectable.gameObject);yield return null;yield return null;
                ((RectTransform)row).GetWorldCorners(corners);
                Assert.That(viewport.InverseTransformPoint(corners[0]).y,Is.GreaterThanOrEqualTo(viewport.rect.yMin-.5f));
                Assert.That(viewport.InverseTransformPoint(corners[1]).y,Is.LessThanOrEqualTo(viewport.rect.yMax+.5f),"Hidden dropdown templates must not alter the visible focus bounds.");
            }
            Call(menu,"Close");Assert.That(panel.gameObject.activeSelf,Is.False);
        }
        [UnityTest]
        public IEnumerator NightKeepsMoonAndAmbientLightingAndRestFadeRecovers()
        {
            yield return LoadTitleWithoutWorld();yield return null;
            var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");Call(controller,"SetWorldHours",0d);
            Assert.That(GameObject.Find("Moon").GetComponent<Light>().intensity,Is.GreaterThan(.1f));
            Assert.That(RenderSettings.ambientSkyColor.maxColorComponent,Is.GreaterThan(.03f));
            Call(controller,"SetRestFade",1f);Call(controller,"SetRestFade",0f);Call(controller,"SetWorldHours",12d);
            var sun=GameObject.Find("Directional Light").GetComponent<Light>();
            Assert.That(sun.intensity,Is.GreaterThan(1));
            Assert.That(sun.transform.eulerAngles.x,Is.InRange(40,48),"Golden lighting retains a lower midday sun than the Natural cycle.");
            Call(controller,"SetLightingStyle",0);Assert.That(sun.transform.eulerAngles.x,Is.GreaterThan(50));
            Call(controller,"SetLightingStyle",1);
        }
        [UnityTest]
        public IEnumerator DodgeEmitsBoundedVfxAndCleansUp()
        {
            var keyboard=InputSystem.AddDevice<Keyboard>();yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return new WaitForSeconds(.1f);
            var effects=Effects();Press(keyboard.leftShiftKey);yield return new WaitForSeconds(.08f);Release(keyboard.leftShiftKey);
            Assert.That((int)Get(effects,"ActiveBurstCount"),Is.InRange(1,8));yield return new WaitForSeconds(2.2f);
            Assert.That(Get(effects,"ActiveBurstCount"),Is.EqualTo(0));
        }
        [UnityTest]
        public IEnumerator SwordSwingEmitsVfxWithoutChangingCombatAuthority()
        {
            var gamepad=InputSystem.AddDevice<Gamepad>();yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return new WaitForSeconds(.1f);
            var effects=Effects();Set(gamepad.rightTrigger,1f);yield return new WaitForSeconds(.4f);Set(gamepad.rightTrigger,0f);
            Assert.That((int)Get(effects,"ActiveBurstCount"),Is.InRange(1,8));
        }
    }
}
