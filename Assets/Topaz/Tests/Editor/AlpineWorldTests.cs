using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
namespace Topaz.Tests
{
    public sealed class AlpineWorldTests
    {
        static Type T(string name)=>Type.GetType(name+", Assembly-CSharp",true);
        static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        static object Property(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
        [Test]
        public void VikingRosterHasTenDistinctHumanoidBodiesAndPlayableAttachments()
        {
            var catalog=T("Topaz.Gameplay.CharacterLooks");var ids=(string[])catalog.GetField("All").GetValue(null);
            Assert.That(ids.Length,Is.EqualTo(10));Assert.That(ids.Distinct().Count(),Is.EqualTo(10));
            var meshes=new System.Collections.Generic.HashSet<Mesh>();
            foreach(string id in ids)
            {
                string label=(string)catalog.GetMethod("Label").Invoke(null,new object[]{id});
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Topaz/Presentation/Art/Characters/"+label+".prefab");
                Assert.That(prefab,Is.Not.Null,label);
                var visual=prefab.GetComponent(T("Topaz.Characters.CharacterVisual"));Assert.That(Property(visual,"HasPlayerBindings"),Is.True,label);
                var animator=prefab.GetComponent<Animator>();Assert.That(animator.avatar.isHuman&&animator.avatar.isValid,Is.True,label);
                var body=(SkinnedMeshRenderer)Property(visual,"BodyRenderer");Assert.That(meshes.Add(body.sharedMesh),Is.True,"Each choice must select a different base body.");
                Assert.That(body.skinnedMotionVectors,Is.True);
                foreach(var material in body.sharedMaterials)
                    Assert.That(material.GetShaderPassEnabled("MotionVectors"),Is.True,"Animated bodies need motion vectors for temporal rendering.");
                var driver=new SerializedObject(prefab.GetComponent(T("Topaz.Characters.CharacterAnimationDriver")));
                foreach(string field in new[]{"swordVisual","axeVisual","pickaxeVisual","combatAxeVisual","staffVisual","crossbowVisual","shieldVisual"})
                {
                    var held=driver.FindProperty(field).objectReferenceValue as GameObject;
                    Assert.That(held,Is.Not.Null,label+" "+field);Assert.That(held.transform.IsChildOf(prefab.transform),Is.True);
                }
            }
        }
        [TestCase(0)][TestCase(42)][TestCase(99)]
        public void AlpineRiversHaveContinuousDownhillCoursesAndWadeableCrossings(int seed)
        {
            var settings=T("Topaz.Generation.WoodlandSettings").GetMethod("LargeWorld").Invoke(null,null);
            var plan=Activator.CreateInstance(T("Topaz.Generation.WildernessPlan"),new[]{(object)seed,settings});
            bool deep=false;
            foreach(var river in (IEnumerable)Property(plan,"Rivers"))
            {
                var points=((IEnumerable)Field(river,"Points")).Cast<Vector3>().ToArray();Assert.That(points.Length,Is.GreaterThan(2));
                for(int i=1;i<points.Length;i++)
                {
                    Assert.That(points[i].y,Is.GreaterThanOrEqualTo(points[i-1].y),"Flow is from the final high point toward the first low point.");
                    deep|=(float)Call(plan,"WaterDepth",points[i].x,points[i].z)>.55f;
                }
            }
            Assert.That(deep,Is.True);
            foreach(var crossing in (IEnumerable)Property(plan,"Crossings"))
            {var p=(Vector3)Field(crossing,"Position");Assert.That((float)Call(plan,"WaterDepth",p.x,p.z),Is.LessThanOrEqualTo(.55f));}
            Call(plan,"Validate");
        }
        [Test]
        public void VikingBuildingAdditionsHaveAuthoredVisualsAndCollision()
        {
            var settings=Resources.Load("BuildingSettings");var type=T("Topaz.Gameplay.BuildCatalog");
            foreach(string field in new[]{"TimberFloor","HalfWall","Beam","Fence","Bench","Chair","Shelf","WeaponRack"})
            {
                string id=(string)type.GetField(field).GetValue(null);var prefab=(GameObject)Call(settings,"VisualFor",id);
                Assert.That(prefab,Is.Not.Null,id);Assert.That(prefab.GetComponentsInChildren<Renderer>().Length,Is.GreaterThan(0));
                Assert.That(prefab.GetComponentsInChildren<Collider>().Length,Is.GreaterThan(0));
            }
        }
    }
}
