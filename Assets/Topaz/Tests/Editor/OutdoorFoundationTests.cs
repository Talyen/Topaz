using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class OutdoorFoundationTests
    {
        static Type T(string name)=>Type.GetType("Topaz.Generation."+name+", Assembly-CSharp",true);
        static object F(object o,string name)=>o.GetType().GetField(name).GetValue(o);
        static object P(object o,string name)=>o.GetType().GetProperty(name).GetValue(o);
        static object Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name).Invoke(o,args);
        static object Plan(int seed)=>Activator.CreateInstance(T("WildernessPlan"),new object[]{seed});
        static object Chunk(int x,int z)=>Activator.CreateInstance(T("WildernessPlan").GetNestedType("Chunk"),new object[]{x,z});
        static string Ids(IEnumerable entries)=>string.Join("|",entries.Cast<object>().Select(s=>F(s,"Id")+":"+F(s,"X")+":"+F(s,"Z")));
        [Test]
        public void ProfileRejectsUnsupportedBoundsAndContentInsteadOfReinterpretingSaves()
        {
            foreach(var value in new[]{("worldSize",(object)2048),("profileId",(object)"unknown"),("contentId",(object)"changed"),("version",(object)6),("version",(object)7),("profileId",(object)"alpine-1024-v7")})
            {
                var settings=Activator.CreateInstance(T("WoodlandSettings"));settings.GetType().GetField(value.Item1).SetValue(settings,value.Item2);
                Assert.Throws<TargetInvocationException>(()=>Activator.CreateInstance(T("WildernessPlan"),new object[]{42,settings}));
            }
            var world=Activator.CreateInstance(Type.GetType("Topaz.Gameplay.TopazWorldData, Assembly-CSharp",true));
            var copy=JsonUtility.FromJson(JsonUtility.ToJson(world),world.GetType());
            Assert.That(JsonUtility.ToJson(F(copy,"generationSettings")),Is.EqualTo(JsonUtility.ToJson(F(world,"generationSettings"))));
        }
        [TestCase(0),TestCase(42),TestCase(99)]
        public void PrimaryRoutesBranchThroughTheNetworkInsteadOfMakingAStarterHub(int seed)
        {
            var settings=T("WoodlandSettings").GetMethod("LargeWorld").Invoke(null,null);
            var plan=Activator.CreateInstance(T("WildernessPlan"),new object[]{seed,settings});
            var routes=((IEnumerable)P(plan,"Routes")).Cast<object>().Take(6).ToArray();
            var existing=new HashSet<Vector3>();
            for(int i=0;i<routes.Length;i++)
            {
                var points=((IEnumerable)F(routes[i],"Points")).Cast<Vector3>().ToArray();
                if(i==0)Assert.That(points[0],Is.EqualTo(Vector3.zero));
                else
                {
                    Assert.That(new Vector2(points[0].x,points[0].z).magnitude,Is.GreaterThanOrEqualTo(63.99f));
                    Assert.That(existing.Contains(points[0]),Is.True,"Each branch joins an earlier route.");
                }
                foreach(var point in points)existing.Add(point);
            }
        }
        [Test, Category("Stress")]
        public void LargeProfileValidatesHundredSeedsAndOuterTileSeams() => ValidateLargeProfiles(Enumerable.Range(0,100));

        [Test]
        public void LargeProfileValidatesRepresentativeSeedsAndOuterTileSeams() => ValidateLargeProfiles(new[]{0,1,7,42,63,99});

        static void ValidateLargeProfiles(IEnumerable<int> seeds)
        {
            var settings=T("WoodlandSettings").GetMethod("LargeWorld").Invoke(null,null);
            var restored=JsonUtility.FromJson(JsonUtility.ToJson(settings),settings.GetType());
            Assert.That(F(restored,"worldSize"),Is.EqualTo(2048));
            foreach(int seed in seeds)
            {
                var plan=Activator.CreateInstance(T("WildernessPlan"),new object[]{seed,restored});
                Call(plan,"Validate");
                foreach(int z in new[]{-8,-1,0,7})foreach(int x in new[]{-8,-1,0,6})
                {
                    var left=(float[,])Call(plan,"Heights",Chunk(x,z),17);
                    var right=(float[,])Call(plan,"Heights",Chunk(x+1,z),17);
                    for(int n=0;n<17;n++)Assert.That(left[n,16],Is.EqualTo(right[n,0]),"seed "+seed);
                }
                Assert.That(Call(plan,"Contains",1023f,1023f),Is.True);
                Assert.That(Call(plan,"Contains",1024f,0f),Is.False);
            }
        }
        [Test]
        public void RunestoneGatewayKeepsControllerClearanceThroughItsActualOpening()
        {
            var root=UnityEditor.PrefabUtility.LoadPrefabContents("Assets/Topaz/Presentation/Art/World/Viking Runestones.prefab");
            try
            {
                var arch=root.transform.Find("Rune Approach Arch");Assert.That(arch,Is.Not.Null);
                var colliders=arch.GetComponentsInChildren<Collider>();Assert.That(colliders,Is.Not.Empty);
                Physics.SyncTransforms();
                foreach(float x in new[]{-.45f,0,.45f})foreach(float y in new[]{.4f,1,1.7f})
                foreach(var collider in colliders)
                {
                    Assert.That(collider.enabled,Is.True);
                    Assert.That(collider.Raycast(new Ray(new Vector3(x,y,-20),Vector3.forward),out _,25),Is.False,
                        $"The passage clips controller clearance at x={x}, y={y}: {collider.name}.");
                }
            }
            finally {UnityEditor.PrefabUtility.UnloadPrefabContents(root);}
        }
        [Test]
        public void RendererHasNativePostProcessingResourcesAndNoCameraDithering()
        {
            var renderer=UnityEditor.AssetDatabase.LoadMainAssetAtPath("Assets/Topaz/Presentation/Rendering/Environment/Woodland Renderer.asset");
            var data=new UnityEditor.SerializedObject(renderer);
            Assert.That(data.FindProperty("postProcessData").objectReferenceValue,Is.Not.Null,"A camera flag alone cannot enable the native post stack.");
            var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Topaz/World/Scenes/Bootstrap.unity");
            foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
            {
                var component=camera.GetComponent("UniversalAdditionalCameraData");
                if(component!=null)Assert.That(new UnityEditor.SerializedObject(component).FindProperty("m_Dithering").boolValue,Is.False);
            }
        }
        [TestCase("DistantGround.shadergraph")]
        [TestCase("ShelteredLit.shader")]
        [TestCase("ShallowWater.shader")]
        [TestCase("OutdoorRain.shader")]
        [TestCase("OutdoorSky.shader")]
        public void OutdoorMaterialPassesHaveCompatibleSrpBuffers(string file)
        {
            var shader=UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/Topaz/Presentation/Rendering/Environment/"+file);
            Assert.That(shader,Is.Not.Null);
            var method=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityCode",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
            Assert.That(method,Is.Not.Null,"The installed Unity 6.6 shader validator must be available.");
            var material=new Material(shader);bool previous=UnityEditor.ShaderUtil.allowAsyncCompilation;
            bool batching=UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching;
            var preview=new UnityEditor.PreviewRenderUtility();
            try
            {
                UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=true;
                UnityEditor.ShaderUtil.allowAsyncCompilation=false;
                // Batch EditMode has no material preview; explicitly compile passes before the inspector query.
                for(int pass=0;pass<material.passCount;pass++)UnityEditor.ShaderUtil.CompilePass(material,pass,true);
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);var mesh=cube.GetComponent<MeshFilter>().sharedMesh;UnityEngine.Object.DestroyImmediate(cube);
                preview.camera.transform.position=new Vector3(0,0,-3);preview.camera.transform.rotation=Quaternion.identity;
                preview.BeginPreview(new Rect(0,0,64,64),GUIStyle.none);
                preview.DrawMesh(mesh,Matrix4x4.identity,material,0);preview.Render(true);preview.EndPreview();
                material.SetPass(0);
                int code=(int)method.Invoke(null,new object[]{shader,0});
                var reason=typeof(UnityEditor.ShaderUtil).GetMethod("GetSRPBatcherCompatibilityIssueReason",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
                Assert.That(code,Is.Zero,file+": "+reason.Invoke(null,new object[]{shader,0,code}));
            }
            finally{preview.Cleanup();UnityEditor.ShaderUtil.allowAsyncCompilation=previous;UnityEngine.Rendering.GraphicsSettings.useScriptableRenderPipelineBatching=batching;UnityEngine.Object.DestroyImmediate(material);}
        }
        [Test]
        public void SurfaceWeightsAreNormalizedAndSharedAtTileCorners()
        {
            var plan=Plan(42);
            for(float z=-512;z<=512;z+=32)for(float x=-512;x<=512;x+=32)
            {
                var weights=(Vector4)Call(plan,"SurfaceWeights",x,z);
                Assert.That(weights.x+weights.y+weights.z+weights.w,Is.EqualTo(1).Within(.00001));
                Assert.That(Mathf.Min(weights.x,weights.y,weights.z,weights.w),Is.GreaterThanOrEqualTo(0));
            }
        }
        [Test]
        public void CoordinateIdentityDoesNotUseTheCurrentCulture()
        {
            var original=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                var plan=Plan(42);var chunk=Chunk(-1,-1);
                string[] expected=((IEnumerable)Call(plan,"Resources",chunk)).Cast<object>().Select(s=>(string)F(s,"Id")).ToArray();
                var culture=(System.Globalization.CultureInfo)original.Clone();culture.NumberFormat.NegativeSign="~";
                System.Globalization.CultureInfo.CurrentCulture=culture;
                var changed=Plan(42);
                CollectionAssert.AreEqual(expected,((IEnumerable)Call(changed,"Resources",chunk)).Cast<object>().Select(s=>(string)F(s,"Id")));
                Assert.That(P(chunk,"Id"),Is.EqualTo("chunk.-1.-1"));
            }
            finally{System.Globalization.CultureInfo.CurrentCulture=original;}
        }
        [Test]
        public void HalfOpenBoundsAndFeatureOwnershipCoverNegativeCorners()
        {
            var plan=Plan(-12345);
            foreach(float x in new[]{-512f,-128f,-.01f,0f,127.99f,511.99f})foreach(float z in new[]{-512f,-.01f,0f,511.99f})
                Assert.That(Call(plan,"Contains",x,z),Is.True);
            Assert.That(Call(plan,"Contains",512f,0f),Is.False);Assert.That(Call(plan,"Contains",0f,-512.01f),Is.False);
            foreach(var w in (IEnumerable)P(plan,"Waters"))
                Assert.That(F(P(w,"Owner"),"X"),Is.EqualTo(Mathf.FloorToInt((float)F(w,"X")/128)));
        }
        [Test]
        public void ReversedTileOrderAndDecorationDensityPreserveRequiredIdentity()
        {
            var plan=Plan(42);var expected=new Dictionary<string,string>();
            for(int z=-4;z<4;z++)for(int x=-4;x<4;x++)expected[x+","+z]=Ids((IEnumerable)Call(plan,"Resources",Chunk(x,z)));
            var settings=Activator.CreateInstance(T("WoodlandSettings"));settings.GetType().GetField("decorationCount").SetValue(settings,0);
            var sparse=Activator.CreateInstance(T("WildernessPlan"),new[]{(object)42,settings});
            for(int z=3;z>=-4;z--)for(int x=3;x>=-4;x--)
            {
                Call(plan,"Decorations",Chunk(x,z),900);
                Assert.That(Ids((IEnumerable)Call(sparse,"Resources",Chunk(x,z))),Is.EqualTo(expected[x+","+z]));
            }
        }
        [TestCase(0)][TestCase(1)][TestCase(7)][TestCase(42)][TestCase(63)][TestCase(99)]
        public void RequiredPlacesHaveConnectedRoutesShallowsAndAllThreeEnvironments(int seed)
        {
            var plan=Plan(seed);Call(plan,"Validate");
            var sites=((IEnumerable)F(plan,"Discoveries")).Cast<object>().ToArray();
            var routes=((IEnumerable)P(plan,"Routes")).Cast<object>().ToArray();
            Assert.That(routes.Length,Is.GreaterThan(sites.Length));
            CollectionAssert.Contains(sites.Select(s=>F(s,"Id")),"landmark.0");
            CollectionAssert.Contains(sites.Select(s=>F(s,"Id")),"camp.0");
            CollectionAssert.Contains(sites.Select(s=>F(s,"Id")),"resource-site.0");
            Assert.That((float)F(Call(plan,"Biomes",0f,0f),"Meadow"),Is.GreaterThan(.9));
            Assert.That((float)F(Call(plan,"Biomes",F(sites[0],"X"),F(sites[0],"Z")),"Highland"),Is.GreaterThan(.35));
            Assert.That((float)F(Call(plan,"Biomes",F(sites[1],"X"),F(sites[1],"Z")),"Woodland"),Is.GreaterThan(.5));
            foreach(var water in (IEnumerable)P(plan,"Waters"))
            {
                float x=(float)F(water,"X"),z=(float)F(water,"Z");
                Assert.That((float)Call(plan,"WaterDepth",x,z),Is.InRange(.05f,.55f));
                Assert.That((float)Call(plan,"WaterSurface",x,z)-(float)Call(plan,"Height",x,z),Is.EqualTo((float)Call(plan,"WaterDepth",x,z)).Within(.0001));
            }
        }
        [Test]
        public void NeighborTileDecorationCannotOccupyAResourceOrRouteClearance()
        {
            var plan=Plan(42);var resources=new List<object>();var decor=new List<object>();
            for(int z=-1;z<=1;z++)for(int x=-1;x<=1;x++)resources.AddRange(((IEnumerable)Call(plan,"Resources",Chunk(x,z))).Cast<object>());
            for(int z=-1;z<=0;z++)for(int x=-1;x<=0;x++)decor.AddRange(((IEnumerable)Call(plan,"Decorations",Chunk(x,z),1000)).Cast<object>());
            foreach(var d in decor)
            {
                float x=(float)F(d,"X"),z=(float)F(d,"Z"),radius=(int)F(d,"Kind")==0?3:(int)F(d,"Kind")==1?3:1;
                Assert.That(Call(plan,"Reserved",x,z,radius+3),Is.False);
                foreach(var r in resources)Assert.That(Vector2.Distance(new Vector2(x,z),new Vector2((float)F(r,"X"),(float)F(r,"Z"))),Is.GreaterThanOrEqualTo(radius+5));
            }
        }
    }
}
