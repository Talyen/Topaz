using System;
using System.Reflection;
using NUnit.Framework;
namespace Topaz.Tests
{
    public sealed class EnvironmentRulesTests
    {
        static readonly Type Schedule=Type.GetType("Topaz.Gameplay.WeatherSchedule, Assembly-CSharp",true);
        [Test]
        public void MacStartingPresetPreservesOtherPreferencesAndLaterExplicitSelections()
        {
            var type=Type.GetType("Topaz.Rendering.GraphicsPreferences, Assembly-CSharp",true);
            var prefs=Activator.CreateInstance(type);
            void Set(string name,object value)=>type.GetField(name).SetValue(prefs,value);
            object Get(string name)=>type.GetField(name).GetValue(prefs);
            Set("bokehAperture",6f);Set("exposure",.7f);Set("foliageDensity",.9f);Set("lightingStyle",0);
            var apply=type.GetMethod("ApplyMacStartingPreset");
            Assert.That(apply.Invoke(prefs,null),Is.True);
            Assert.That(Get("look"),Is.EqualTo(0));Assert.That(Get("antiAliasing"),Is.EqualTo(4));
            Assert.That(Get("focusMode"),Is.EqualTo(2));Assert.That(Get("bokehAperture"),Is.EqualTo(6f));
            Assert.That(Get("exposure"),Is.EqualTo(.7f));Assert.That(Get("foliageDensity"),Is.EqualTo(.9f));
            Assert.That(Get("lightingStyle"),Is.EqualTo(0));
            Set("look",1);Set("antiAliasing",3);
            prefs=UnityEngine.JsonUtility.FromJson(UnityEngine.JsonUtility.ToJson(prefs),type);
            Assert.That(apply.Invoke(prefs,null),Is.False);
            Assert.That(Get("look"),Is.EqualTo(1));Assert.That(Get("antiAliasing"),Is.EqualTo(3));
        }
        [Test]
        public void WetnessAccumulatesDriesAndIsFrameRateIndependent()
        {
            var type=Type.GetType("Topaz.Rendering.EnvironmentPresentationState, Assembly-CSharp",true);
            float Step(float current,float rain,float seconds)=>(float)type.GetMethod("AdvanceWetness").Invoke(null,new object[]{current,rain,seconds});
            float sixty=0,oneTwenty=0;
            for(int i=0;i<600;i++)sixty=Step(sixty,1,1/60f);
            for(int i=0;i<1200;i++)oneTwenty=Step(oneTwenty,1,1/120f);
            Assert.That(sixty,Is.EqualTo(oneTwenty).Within(.0002));Assert.That(sixty,Is.InRange(.05f,.3f));
            Assert.That(Step(sixty,0,0),Is.EqualTo(sixty));Assert.That(Step(sixty,0,10),Is.LessThan(sixty));
            Assert.That(Step(sixty,1,900),Is.GreaterThan(.99f));Assert.That(Step(1,0,900),Is.LessThan(.06f));
        }
        [Test]
        public void ScheduleIsStableAcrossTimeSkipsAndOverridesHaveExplicitPriority()
        {
            MethodInfo at = Schedule.GetMethod("At");
            MethodInfo resolve = Schedule.GetMethod("Resolve");
            MethodInfo next = Schedule.GetMethod("NextBoundary");
            const string world = "weather-test-world";
            Assert.That(at.Invoke(null, new object[] { world, 8d }), Is.EqualTo("clear"));
            double boundary = (double)next.Invoke(null, new object[] { world, 32d });
            Assert.That(boundary, Is.GreaterThan(32d));
            Assert.That(boundary, Is.LessThanOrEqualTo(40d));
            string afterSkip = (string)at.Invoke(null, new object[] { world, 32d + 8d });
            Assert.That(at.Invoke(null, new object[] { world, 40d }),
                Is.EqualTo(afterSkip), "Rest must land on the saved world's timeline.");
            Assert.That(resolve.Invoke(null, new object[] { "clear", "cloudy", "rain" }),
                Is.EqualTo("rain"));
            Assert.That(resolve.Invoke(null, new object[] { "clear", "cloudy", null }),
                Is.EqualTo("cloudy"));
            Assert.That(resolve.Invoke(null, new object[] { "clear", null, null }),
                Is.EqualTo("clear"));
        }
    }
}
