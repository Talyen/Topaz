using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Topaz.Tests.Editor
{
    public sealed class AnimationStudyTests
    {
        [Test]
        public void StorybookHumanoidControllerContainsOnlyFiniteAnimationValues()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/Presentation/Rendering/SyntySample/Storybook Knight.prefab");
            Assert.That(prefab,Is.Not.Null);
            var animator=prefab.GetComponent<Animator>();
            Assert.That(animator.avatar.isHuman && animator.avatar.isValid,Is.True);
            foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
            {
                Assert.That(clip.length,Is.GreaterThan(0));
                foreach(var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    var curve=AnimationUtility.GetEditorCurve(clip,binding);
                    foreach(var key in curve.keys)Assert.That(float.IsNaN(key.value)||float.IsInfinity(key.value),Is.False,clip.name+" "+binding.propertyName);
                }
            }
        }
        [Test]
        public void PlaceholderCharacterHasReplaceableVisualAndEquipmentSockets()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/Presentation/Rendering/Environment/Prototype Character.prefab");
            Assert.That(prefab, Is.Not.Null);
            var type = Type.GetType("Topaz.Characters.CharacterVisual, Assembly-CSharp", true);
            var visual = prefab.GetComponent(type);
            Assert.That(visual, Is.Not.Null);
            Assert.That(type.GetProperty("HasPlayerBindings").GetValue(visual), Is.True);
        }
    }
}
