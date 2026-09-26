using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Topaz.Tests.Editor
{
    public sealed class AnimationStudyTests
    {
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
