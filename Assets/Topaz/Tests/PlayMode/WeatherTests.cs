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
    public sealed class WeatherTests : TopazInputTestFixture
    {
        static readonly Type Schedule = Type.GetType(
            "Topaz.Gameplay.WeatherSchedule, Assembly-CSharp", true);



        [UnityTest]
        public IEnumerator RainOverrideChangesTheLookAndAmbienceThenRestoresTheSchedule()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return WaitForWilderness();
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

            Assert.That(region.Invoke(session, new object[] { "wilderness", "cloudy" }), Is.True);
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("cloudy"));
            Assert.That(story.Invoke(session, new object[] { "rain" }), Is.True);
            tick.Invoke(weather, new object[] { 30f });
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("rain"));
            Assert.That((float)weather.GetType().GetProperty("Rain").GetValue(weather),
                Is.EqualTo(1f).Within(.001f));
            Assert.That(sun.intensity, Is.LessThan(clearSun));
            Component rainOutput = weather.GetComponent("TopazAudioOutput");
            Assert.That(rainOutput, Is.Not.Null);
            Assert.That((float)rainOutput.GetType().GetProperty("BaseVolume")
                .GetValue(rainOutput), Is.GreaterThan(.1f));
            AudioClip rainClip = weather.GetComponent<AudioSource>().clip;
            var samples = new float[rainClip.samples];
            Assert.That(rainClip.GetData(samples, 0), Is.True);
            double energy = 0d;
            double changes = 0d;
            for (int i = 1; i < samples.Length; i++)
            {
                energy += samples[i] * samples[i];
                double delta = samples[i] - samples[i - 1];
                changes += delta * delta;
            }
            Assert.That(Math.Sqrt(changes / energy), Is.LessThan(.85d),
                "Rain ambience should be a soft wash, without white-noise hiss.");
            ParticleSystem particles = weather.transform.Find("Weather Rain")
                .GetComponent<ParticleSystem>();
            Assert.That(particles.emission.rateOverTime.constant, Is.GreaterThan(0f));

            Assert.That(story.Invoke(session, new object[] { "invalid" }), Is.False);
            Assert.That(story.Invoke(session, new object[] { null }), Is.True);
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("cloudy"));
            Assert.That(region.Invoke(session, new object[] { "wilderness", null }), Is.True);
            tick.Invoke(weather, new object[] { 30f });
            Assert.That(session.GetType().GetProperty("CurrentWeather").GetValue(session),
                Is.EqualTo("clear"));
            Assert.That(sun.intensity, Is.EqualTo(clearSun).Within(.02f));
        }
    }
}
