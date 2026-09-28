using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class LuminousAlpineTests
    {
        static Type T(string name)=>Type.GetType("Topaz.Generation."+name+", Assembly-CSharp",true);
        static object Plan(int seed)=>Activator.CreateInstance(T("WildernessPlan"),new object[]{seed});
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
        static object Field(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        [TestCase(0)][TestCase(42)][TestCase(99)]
        public void HabitatPatchesHaveDenseCoverAndDistinctSpeciesWithoutCoveringPaths(int seed)
        {
            var plan=Plan(seed);var repeat=Plan(seed);int quiet=0,covered=0;var species=new float[4];
            for(int z=-256;z<=256;z+=8)for(int x=-256;x<=256;x+=8)
            {
                var cover=(Vector4)Call(plan,"GroundCover",(float)x,(float)z);
                Assert.That(cover,Is.EqualTo((Vector4)Call(repeat,"GroundCover",(float)x,(float)z)));
                if(cover.sqrMagnitude<.01f)quiet++;else covered++;
                for(int i=0;i<4;i++){Assert.That(cover[i],Is.InRange(0f,48f));species[i]+=cover[i];}
                if((float)Call(plan,"RouteDistance",(float)x,(float)z)<(float)Call(plan,"TrailHalfWidth",(float)x,(float)z) || (float)Call(plan,"WaterDepth",(float)x,(float)z)>0)
                    Assert.That(cover,Is.EqualTo(Vector4.zero),$"seed {seed}: route/water at {x},{z}");
            }
            Assert.That(quiet,Is.GreaterThan(10));Assert.That(covered,Is.GreaterThan(100));
            foreach(float count in species)Assert.That(count,Is.GreaterThan(1),"Every habitat channel must occur.");
            Call(plan,"Validate");
        }
        [TestCase(0)][TestCase(33)][TestCase(42)][TestCase(99)]
        public void RiverBanksMeetWaterInsteadOfExposingDryGroundBelowItsSurface(int seed)
        {
            var plan=Plan(seed);
            var rivers=(IEnumerable)plan.GetType().GetProperty("Rivers").GetValue(plan);
            foreach(var river in rivers)
            {
                var points=((IEnumerable)Field(river,"Points")).Cast<Vector3>().ToArray();float width=(float)Field(river,"HalfWidth");
                for(int i=2;i<points.Length-2;i+=4)
                {
                    var point=(points[i]+points[i+1])*.5f;var direction=(points[i+1]-points[i]).normalized;
                    var side=new Vector3(-direction.z,0,direction.x).normalized;
                    foreach(float sign in new[]{-1f,1f})
                    {
                        var bank=point+side*(sign*(width+2));
                        if((float)Call(plan,"RouteDistance",bank.x,bank.z)<20)continue;
                        Assert.That((float)Call(plan,"Height",bank.x,bank.z),Is.GreaterThanOrEqualTo(point.y-.04f),$"seed {seed} bank {bank}");
                    }
                }
            }
            Call(plan,"Validate");
        }
        [Test]
        public void NewCompositionInputsRoundTripAndRejectTheOldGenerator()
        {
            var settings=Activator.CreateInstance(T("WoodlandSettings"));
            var restored=JsonUtility.FromJson(JsonUtility.ToJson(settings),settings.GetType());
            Assert.That(JsonUtility.ToJson(restored),Is.EqualTo(JsonUtility.ToJson(settings)));
            settings.GetType().GetField("version").SetValue(settings,4);
            Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(settings,"Validate"));
            settings=Activator.CreateInstance(T("WoodlandSettings"));settings.GetType().GetField("groveScale").SetValue(settings,float.NaN);
            Assert.Throws<System.Reflection.TargetInvocationException>(()=>Call(settings,"Validate"));
        }
        [Test]
        public void TreeLodsDoNotContributeOverlappingGeometryToNativeGi()
        {
            var preset=AssetDatabase.LoadMainAssetAtPath("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            foreach(var tree in (GameObject[])Field(preset,"trees"))
                foreach(var group in tree.GetComponentsInChildren<LODGroup>())
                {
                    int contributingLevels=group.GetLODs().Count(l=>l.renderers.Any(r=>r!=null && r.enabled && (r.renderingLayerMask & ~2u)!=0));
                    Assert.That(contributingLevels,Is.EqualTo(1),tree.name+" has overlapping GI LOD geometry.");
                    Assert.That(group.GetLODs().Last().renderers.All(r=>r==null || (r.renderingLayerMask & ~2u)==0),Is.True,"Distant cards are visual silhouettes, not additional GI occluders.");
                }
        }
        [Test]
        public void AllGroundCoverChannelsHaveRootMeshesAndStableMaterialPasses()
        {
            var preset=AssetDatabase.LoadMainAssetAtPath("Assets/Topaz/Presentation/Rendering/Environment/Woodland.asset");
            Call(preset,"ValidateContent");var entries=((IEnumerable)Field(preset,"groundCover")).Cast<object>().ToArray();
            Assert.That(entries.Length,Is.EqualTo(4));
            var meshes=entries.Select(e=>((GameObject)Field(e,"prefab")).GetComponent<MeshFilter>().sharedMesh).ToArray();
            Assert.That(meshes.Distinct().Count(),Is.EqualTo(4));
            foreach(var entry in entries)
            {
                var prefab=(GameObject)Field(entry,"prefab");var material=prefab.GetComponent<MeshRenderer>().sharedMaterial;
                Assert.That(material.IsKeywordEnabled("LOD_FADE_CROSSFADE"),Is.False,"Terrain details have no LODGroup fade factor; forcing this keyword hides grass.");
                Assert.That(material.enableInstancing,Is.True);Assert.That(material.FindPass("DepthOnly"),Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));
                Assert.That(material.FindPass("MotionVectors"),Is.GreaterThanOrEqualTo(0));
                Assert.That(material.GetShaderPassEnabled("MotionVectors"),Is.True,"Wind deformation must reach temporal history.");
                Assert.That(prefab.GetComponent<MeshFilter>().sharedMesh.bounds.min.y,Is.EqualTo(0).Within(.001));
            }
        }
    }
}
