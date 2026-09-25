using System.Collections;
using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Topaz.Tests
{
    public sealed class AudioOptionsTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator AudioTabChangesSourcesPersistsValuesAndMutesInBackground()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject canvas = GameObject.Find("Loop HUD");
            Component preferences = canvas.GetComponent("TopazAudioSettings");
            Component menu = canvas.GetComponent("VisualOptionsMenu");
            Assert.That(preferences, Is.Not.Null);
            Assert.That(menu, Is.Not.Null);
            float previousMaster = (float)Get(preferences, "Master");
            float previousMusic = (float)Get(preferences, "Music");
            float previousAmbience = (float)Get(preferences, "Ambience");
            float previousEffects = (float)Get(preferences, "Effects");
            bool previousMute = (bool)Get(preferences, "MuteInBackground");
            GameObject sourceObject = new GameObject("Audio Preference Test", typeof(AudioSource));
            Type outputType = Type.GetType("Topaz.Audio.TopazAudioOutput, Assembly-CSharp", true);
            Component output = sourceObject.AddComponent(outputType);
            try
            {
                Call(menu, "Toggle");
                Transform panel = canvas.transform.Find("Graphics");
                Assert.That(panel.gameObject.activeSelf, Is.True);
                panel.Find("Audio Tab").GetComponent<Button>().onClick.Invoke();
                Assert.That(panel.Find("Master Volume").gameObject.activeSelf, Is.True);
                Assert.That(panel.Find("Camera Zoom").gameObject.activeSelf, Is.False);

                Call(preferences, "SetMuteInBackground", false);
                panel.Find("Master Volume/Master Volume Slider").GetComponent<Slider>().value = .6f;
                panel.Find("Music Volume/Music Volume Slider").GetComponent<Slider>().value = .4f;
                panel.Find("Ambient Volume/Ambient Volume Slider").GetComponent<Slider>().value = .5f;
                panel.Find("Sound Effects/Sound Effects Slider").GetComponent<Slider>().value = .25f;
                Assert.That((float)Get(preferences, "Master"), Is.EqualTo(.6f).Within(.001f));
                Assert.That((float)Get(preferences, "Music"), Is.EqualTo(.4f).Within(.001f));
                Assert.That(PlayerPrefs.GetFloat("Topaz.Audio.Music"), Is.EqualTo(.4f).Within(.001f));
                Assert.That(AudioListener.volume, Is.EqualTo(.6f).Within(.001f));
                Type categoryType = outputType.GetNestedType("Category");
                Call(output, "Configure", Enum.ToObject(categoryType, 1), .4f);
                Assert.That(sourceObject.GetComponent<AudioSource>().volume,
                    Is.EqualTo(.2f).Within(.001f));
                Call(output, "Configure", Enum.ToObject(categoryType, 2), .4f);
                Assert.That(sourceObject.GetComponent<AudioSource>().volume,
                    Is.EqualTo(.1f).Within(.001f));
                Call(output, "Configure", Enum.ToObject(categoryType, 0), .4f);
                Assert.That(sourceObject.GetComponent<AudioSource>().volume,
                    Is.EqualTo(.16f).Within(.001f));

                Toggle background = panel.Find("Mute in Background").GetComponent<Toggle>();
                background.isOn = false;
                background.isOn = true;
                preferences.SendMessage("OnApplicationFocus", false);
                Assert.That(AudioListener.volume, Is.Zero);
                preferences.SendMessage("OnApplicationFocus", true);
                Assert.That(AudioListener.volume, Is.EqualTo(.6f).Within(.001f));
                Call(menu, "Close");
                Assert.That(panel.gameObject.activeSelf, Is.False);
            }
            finally
            {
                Call(preferences, "SetMaster", previousMaster);
                Call(preferences, "SetMusic", previousMusic);
                Call(preferences, "SetAmbience", previousAmbience);
                Call(preferences, "SetEffects", previousEffects);
                Call(preferences, "SetMuteInBackground", previousMute);
                UnityEngine.Object.Destroy(sourceObject);
            }
        }

        static object Get(Component component, string property) =>
            component.GetType().GetProperty(property).GetValue(component);

        static void Call(Component component, string method, params object[] args) =>
            component.GetType().GetMethod(method).Invoke(component, args);
    }
}
