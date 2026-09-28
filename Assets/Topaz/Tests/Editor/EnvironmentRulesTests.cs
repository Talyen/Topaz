using System;
using System.Reflection;
using NUnit.Framework;
namespace Topaz.Tests
{
    public sealed class EnvironmentRulesTests
    {
        static readonly Type Schedule=Type.GetType("Topaz.Gameplay.WeatherSchedule, Assembly-CSharp",true);
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
