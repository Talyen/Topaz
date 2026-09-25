using System;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class ProgressionRulesTests
    {
        static readonly Type Rules = Type.GetType(
            "Topaz.LoopStudy.SkillProgression, Assembly-CSharp", true);

        static int Call(string method, params object[] arguments) =>
            (int)Rules.GetMethod(method, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, arguments);

        [Test]
        public void SourceLevelScalesXpWithoutLosingQuarterAwards()
        {
            Assert.That(Call("Award", 1000, 1, 1), Is.EqualTo(1000));
            Assert.That(Call("Award", 1000, 2, 1), Is.EqualTo(750));
            Assert.That(Call("Award", 1000, 3, 1), Is.EqualTo(500));
            Assert.That(Call("Award", 1000, 4, 1), Is.EqualTo(250));
            Assert.That(Call("Award", 1000, 5, 1), Is.Zero);
            Assert.That(Call("Award", 300, 4, 1), Is.EqualTo(75));
            Assert.That(Call("Award", 1000, 1, 6), Is.EqualTo(1250));
            Assert.That(Call("Award", 1000, 1, 10), Is.EqualTo(1250));
        }

        [Test]
        public void LegacyExperienceKeepsLevelAndProgressOnNewCurve()
        {
            Assert.That(Call("FromLegacy", 0), Is.Zero);
            Assert.That(Call("FromLegacy", 15), Is.EqualTo(1600));
            Assert.That(Call("Level", Call("FromLegacy", 15)), Is.EqualTo(2));
            Assert.That(Call("Level", Call("FromLegacy", 89)), Is.EqualTo(9));
            Assert.That(Call("Level", Call("FromLegacy", 120)), Is.EqualTo(10));
            Assert.That(Call("FromLegacy", 120), Is.GreaterThan(Call("FromLegacy", 90)));
        }

        [Test]
        public void SevenAuthoredSkillsHaveFourStableTalentsEach()
        {
            string root = "Assets/Topaz/Gameplay/WorldLoop/Definitions/";
            foreach (string name in new[] {
                "Swords", "Axes", "Shield", "Logging", "Mining", "Staff", "Crossbows" })
            {
                ScriptableObject asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    root + name + "Skill.asset");
                Assert.That(asset, Is.Not.Null, name);
                var data = new SerializedObject(asset);
                Assert.That(data.FindProperty("stableId").stringValue,
                    Is.EqualTo(name.ToLowerInvariant()));
                Assert.That(data.FindProperty("talents").arraySize, Is.EqualTo(4));
            }
            Assert.That(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                root + "StarterMining.asset"), Is.Not.Null);
        }

        [Test]
        public void PiercingChanceIsTwentyPercentAtTheBoundary()
        {
            Type bolt = Type.GetType("Topaz.CombatStudy.CrossbowBolt, Assembly-CSharp", true);
            MethodInfo pierces = bolt.GetMethod("Pierces", BindingFlags.Public | BindingFlags.Static);
            Assert.That((bool)pierces.Invoke(null, new object[] { 0f }), Is.True);
            Assert.That((bool)pierces.Invoke(null, new object[] { .1999f }), Is.True);
            Assert.That((bool)pierces.Invoke(null, new object[] { .2f }), Is.False);
        }
    }
}
