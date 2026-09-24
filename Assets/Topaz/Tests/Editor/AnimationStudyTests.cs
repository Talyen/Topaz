using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Topaz.Tests.Editor
{
    public sealed class AnimationStudyTests
    {
        [TestCase("Assets/ThirdParty/KayKit/Adventurers/Characters/Rogue.fbx")]
        [TestCase("Assets/ThirdParty/KayKit/Skeletons/Characters/Skeleton_Minion.fbx")]
        public void AnimatedCharacterHasValidGenericAvatar(string modelPath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            Assert.That(avatar, Is.Not.Null, modelPath);
            Assert.That(avatar.isValid, Is.True, modelPath);
            Assert.That(avatar.isHuman, Is.False, modelPath);
        }

        [TestCase("Player")]
        [TestCase("Skeleton")]
        public void CharacterControllerHasLocomotionBlend(string character)
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                $"Assets/Topaz/Characters/Animation/Controllers/{character}.controller");
            Assert.That(controller, Is.Not.Null);
            Assert.That(controller.layers[0].stateMachine.defaultState.name, Is.EqualTo("Locomotion"));
            Assert.That(controller.layers[0].stateMachine.defaultState.motion, Is.TypeOf<BlendTree>());
        }

        [Test]
        public void PlayerBlendsMovementRelativeToAim()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Topaz/Characters/Animation/Controllers/Player.controller");
            var tree = controller.layers[0].stateMachine.defaultState.motion as BlendTree;
            Assert.That(tree, Is.Not.Null);
            Assert.That(tree.blendType, Is.EqualTo(BlendTreeType.SimpleDirectional2D));
            Assert.That(tree.blendParameter, Is.EqualTo("MoveX"));
            Assert.That(tree.blendParameterY, Is.EqualTo("MoveY"));
            Assert.That(tree.children.First(c => c.position == Vector2.zero).motion.name, Is.EqualTo("Idle"));
            Assert.That(tree.children.First(c => c.position == Vector2.up).motion.name, Is.EqualTo("Run"));
            Assert.That(tree.children.First(c => c.position == Vector2.left).motion.name, Is.EqualTo("Strafe Left"));
            Assert.That(tree.children.First(c => c.position == Vector2.right).motion.name, Is.EqualTo("Strafe Right"));
            Assert.That(tree.children.First(c => c.position == Vector2.down).motion.name, Is.EqualTo("Backpedal"));
        }

        [Test]
        public void PlayerHasJumpPose()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(
                "Assets/Topaz/Characters/Animation/Controllers/Player.controller");
            var jump = controller.layers[0].stateMachine.states
                .Select(child => child.state).FirstOrDefault(state => state.name == "Jump");
            Assert.That(jump, Is.Not.Null);
            Assert.That(jump.motion, Is.Not.Null);
        }
    }
}
