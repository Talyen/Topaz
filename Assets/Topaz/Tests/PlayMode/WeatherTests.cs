using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class WeatherTests : InputTestFixture
    {
        static readonly Type Schedule = Type.GetType(
            "Topaz.LoopStudy.WeatherSchedule, Assembly-CSharp", true);

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

        [UnityTest]
        public IEnumerator RainOverrideChangesTheLookAndAmbienceThenRestoresTheSchedule()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            if (!(bool)session.GetType().GetProperty("HasActivePair").GetValue(session))
                session.GetType().GetMethod("EnterEditorTestPair").Invoke(session, null);
            Component weather = GameObject.Find("Visual Study")
                .GetComponent("WeatherPresentation");
            Assert.That(weather, Is.Not.Null);
            Light sun = GameObject.Find("Directional Light").GetComponent<Light>();
            float clearSun = sun.intensity;
            MethodInfo story = session.GetType().GetMethod("SetEventWeatherOverride");
            MethodInfo region = session.GetType().GetMethod("SetRegionWeatherOverride");
            MethodInfo tick = weather.GetType().GetMethod("Tick");

            Assert.That(region.Invoke(session, new object[] { "home", "cloudy" }), Is.True);
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("cloudy"));
            Assert.That(story.Invoke(session, new object[] { "rain" }), Is.True);
            tick.Invoke(weather, new object[] { 30f });
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("rain"));
            Assert.That((float)weather.GetType().GetProperty("Rain").GetValue(weather),
                Is.EqualTo(1f).Within(.001f));
            Assert.That(sun.intensity, Is.LessThan(clearSun));
            Assert.That(weather.GetComponent<AudioSource>().volume, Is.GreaterThan(.1f));
            ParticleSystem particles = weather.transform.Find("Weather Rain")
                .GetComponent<ParticleSystem>();
            Assert.That(particles.emission.rateOverTime.constant, Is.GreaterThan(0f));

            Assert.That(story.Invoke(session, new object[] { "invalid" }), Is.False);
            Assert.That(story.Invoke(session, new object[] { null }), Is.True);
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("cloudy"));
            Assert.That(region.Invoke(session, new object[] { "home", null }), Is.True);
            tick.Invoke(weather, new object[] { 30f });
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("clear"));
            Assert.That(sun.intensity, Is.EqualTo(clearSun).Within(.02f));
        }
    }
}
