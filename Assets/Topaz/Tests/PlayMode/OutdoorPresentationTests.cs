using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Topaz.Tests
{
    public sealed class OutdoorPresentationTests : TopazInputTestFixture
    {
        static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
        static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
        static object Call(object o,string n,params object[] args)=>o.GetType().GetMethod(n).Invoke(o,args);
        [TestCase(7d)]
        [TestCase(17d)]
        public void LowSunRemainsDominantWhileAboveTheHorizon(double hour)
        {
            var type=Type.GetType("Topaz.Rendering.EnvironmentPresentationState, Assembly-CSharp",true);
            var state=Activator.CreateInstance(type,new object[]{hour,0f,0f,0f,Vector4.zero,Vector4.zero,240f,Vector3.zero,0f,0f});
            Assert.That((float)F(state,"SunIntensity"),Is.GreaterThan((float)F(state,"MoonIntensity")));
        }

        [UnityTest]
        public IEnumerator SingleRainSharedStateWaterAndSkySurviveReload()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var weather=GameObject.Find("Visual Study").GetComponent("WeatherPresentation");
            var look=weather.GetComponent("VisualLookController");
            Call(weather,"SetCondition","rain",false);Call(weather,"Tick",30f);
            var state=P(look,"State");
            Assert.That(F(state,"Rain"),Is.EqualTo(1f));Assert.That((float)F(state,"Wetness"),Is.InRange(.1f,.5f));
            Call(weather,"Tick",0f);Assert.That(F(P(look,"State"),"Wetness"),Is.EqualTo(F(state,"Wetness")));
            Assert.That(GameObject.Find("Rain"),Is.Null,"WorldEffects must not create a competing VFX rain emitter.");
            Assert.That(GameObject.Find("Weather Rain"),Is.Not.Null);
            Assert.That(RenderSettings.fog,Is.False,"Native fog must not double-fog the opaque pass.");
            Assert.That(RenderSettings.skybox.shader.name,Is.EqualTo("Topaz/Outdoor Sky"));
            Assert.That(GameObject.Find("pond.0").GetComponent<MeshRenderer>().sharedMaterial.shader.name,Is.EqualTo("Topaz/Shallow Water"));
            var horizon=(Color)F(P(look,"State"),"Horizon");
            Assert.That(Vector4.Distance(RenderSettings.skybox.GetVector("_HorizonColor"),(Vector4)horizon.linear),Is.LessThan(.0001f),"Sky and fog must share linear radiance without a second sRGB conversion.");
            var oldSky=RenderSettings.skybox;var oldMesh=GameObject.Find("pond.0").GetComponent<MeshFilter>().sharedMesh;
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();yield return null;
            Assert.That(oldSky==null,Is.True);Assert.That(oldMesh==null,Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length,Is.GreaterThanOrEqualTo(1));
        }
    }
}
