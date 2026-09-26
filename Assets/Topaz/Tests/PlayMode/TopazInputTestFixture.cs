using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
namespace Topaz.Tests
{
    /// <summary>Failed coroutine fixtures must not leak their save-directory override into another test.</summary>
    public class TopazInputTestFixture : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            Type.GetType("Topaz.Gameplay.WorldSession, Assembly-CSharp",true)
                .GetProperty("EditorTestSaveDirectory").SetValue(null,null);
            Time.timeScale=1;
        }
        [TearDown]
        public override void TearDown()
        {
            Type.GetType("Topaz.Gameplay.WorldSession, Assembly-CSharp",true)
                .GetProperty("EditorTestSaveDirectory").SetValue(null,null);
            base.TearDown();
        }
        protected static void AimAt(Vector3 point)
        {
            var rig=Camera.main.GetComponent("PlayerCamera");
            rig.GetType().GetMethod("LookAtPoint").Invoke(rig,new object[]{point});
        }
    }
}
