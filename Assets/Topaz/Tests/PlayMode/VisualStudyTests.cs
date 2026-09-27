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
        public IEnumerator GraphicsMenuAppliesAndSavesSelectionsImmediately()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");
            var menu=GameObject.Find("Loop HUD").GetComponent("VisualOptionsMenu");Call(menu,"Toggle");yield return null;
            var panel=GameObject.Find("Loop HUD").transform.Find("Graphics");
            var quality=panel.Find("Depth of Field/Depth of Field Dropdown").GetComponent("TMP_Dropdown");
            var aa=panel.Find("Anti-aliasing/Anti-aliasing Dropdown").GetComponent("TMP_Dropdown");
            quality.GetType().GetProperty("value").SetValue(quality,1);aa.GetType().GetProperty("value").SetValue(aa,4);
            panel.Find("Bloom").GetComponent<UnityEngine.UI.Toggle>().isOn=false;
            panel.Find("Ambient Occlusion").GetComponent<UnityEngine.UI.Toggle>().isOn=false;
            Assert.That(Get(controller,"HighQuality"),Is.True);Assert.That(Get(controller,"NativeResolution"),Is.False);
            string path=(string)Get(controller,"SettingsPath");Assert.That(path,Does.StartWith(Application.temporaryCachePath));
            var saved=JsonUtility.FromJson<SavedValue>(File.ReadAllText(path));Assert.That(saved.look,Is.EqualTo(1));Assert.That(saved.antiAliasing,Is.EqualTo(4));Assert.That(saved.bloomEnabled||saved.ambientOcclusion,Is.False);
            Call(controller,"ResetSelection");Assert.That(Get(controller,"HighQuality"),Is.False);Assert.That(Get(controller,"NativeResolution"),Is.True);
            Call(menu,"Close");Assert.That(panel.gameObject.activeSelf,Is.False);
        }
        [UnityTest]
        public IEnumerator NightKeepsMoonAndAmbientLightingAndRestFadeRecovers()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();yield return null;
            var controller=GameObject.Find("Visual Study").GetComponent("VisualLookController");Call(controller,"SetWorldHours",0d);
            Assert.That(GameObject.Find("Moon").GetComponent<Light>().intensity,Is.GreaterThan(.1f));
            Assert.That(RenderSettings.ambientSkyColor.maxColorComponent,Is.GreaterThan(.03f));
            Call(controller,"SetRestFade",1f);Call(controller,"SetRestFade",0f);Call(controller,"SetWorldHours",12d);
            Assert.That(GameObject.Find("Directional Light").GetComponent<Light>().intensity,Is.GreaterThan(1));
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
