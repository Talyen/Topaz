using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor;

namespace Topaz.Tests
{
    public sealed class IsometricVisibilityTests
    {
        static readonly Type Cutaway=Type.GetType("Topaz.Player.SceneryCutaway, Assembly-CSharp",true);
        static bool Revealed(Vector3 p,Vector3 end,float radius,Vector3 forward)=>(bool)Cutaway.GetMethod("InReveal").Invoke(null,new object[]{p,end,radius,forward});
        [Test]
        public void ForegroundHillIsRevealedButSupportingGroundAndFarSideRemainSolid()
        {
            Vector3 forward=Quaternion.Euler(50,45,0)*Vector3.forward;
            Assert.That(Revealed(-forward*3,Vector3.zero,3,forward),Is.True);
            Assert.That(Revealed(Vector3.zero,Vector3.zero,3,forward),Is.False);
            Assert.That(Revealed(forward*3,Vector3.zero,3,forward),Is.False);
            Assert.That(Revealed(-forward*3+Vector3.right*10,Vector3.zero,3,forward),Is.False);
            Assert.That(Revealed(-forward*3,Vector3.zero,0,forward),Is.False);
        }
        [Test]
        public void HiddenCharacterFeatureDoesNotWriteDepthOrRevealEntireWorld()
        {
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Topaz/Presentation/Rendering/Environment/Woodland Renderer.asset");
            RenderObjects feature=null;
            foreach(var item in renderer.rendererFeatures)if(item.name=="Hidden Characters")feature=item as RenderObjects;
            Assert.That(feature,Is.Not.Null);
            Assert.That(feature.settings.enableWrite,Is.False);
            Assert.That(feature.settings.depthCompareFunction,Is.EqualTo(CompareFunction.Greater));
            Assert.That(feature.settings.filterSettings.LayerMask.value,Is.EqualTo(1<<LayerMask.NameToLayer("Character Visibility")));
            Assert.That(feature.settings.overrideMaterial.shader.name,Is.EqualTo("Topaz/Hidden Character"));
        }
        [Test]
        public void RoofMembershipKeepsCollisionAndRestoresShadowParticipation()
        {
            var roof=GameObject.CreatePrimitive(PrimitiveType.Cube);var player=new GameObject("Interior Test Player");
            try
            {
                roof.transform.position=new Vector3(0,3,0);roof.transform.localScale=new Vector3(4,.25f,4);
                player.transform.position=Vector3.zero;
                var visibility=roof.AddComponent(Type.GetType("Topaz.Gameplay.HomeRoofVisibility, Assembly-CSharp",true));
                visibility.GetType().GetMethod("Bind").Invoke(visibility,new object[]{player.transform});
                visibility.GetType().GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(visibility,null);
                Assert.That(roof.GetComponent<Collider>().enabled,Is.True);
                Assert.That(roof.GetComponent<Renderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.ShadowsOnly));
                player.transform.position=Vector3.right*8;
                visibility.GetType().GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(visibility,null);
                Assert.That(roof.GetComponent<Renderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.On));
                ((Behaviour)visibility).enabled=false;
                Assert.That(roof.GetComponent<Renderer>().shadowCastingMode,Is.EqualTo(ShadowCastingMode.On));
            }
            finally{UnityEngine.Object.DestroyImmediate(roof);UnityEngine.Object.DestroyImmediate(player);}
        }
    }
}
