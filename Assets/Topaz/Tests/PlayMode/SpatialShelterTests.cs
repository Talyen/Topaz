using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class SpatialShelterTests : TopazInputTestFixture
    {
        static Type T(string name) => Type.GetType("Topaz.Gameplay." + name + ", Assembly-CSharp", true);
        static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method).Invoke(target,args);
        static object P(object target, string property) => target.GetType().GetProperty(property).GetValue(target);
        static GameObject Box(Transform parent, Vector3 position, Vector3 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent,false); go.transform.localPosition = position; go.transform.localScale = size;
            return go;
        }
        [UnityTest]
        public IEnumerator RoofWallsOpeningsAndNeighborGeometryHaveIndependentRainAndFill()
        {
            var root = new GameObject("Shelter test"); root.transform.position = new Vector3(1500,100,1500);
            var field = root.AddComponent(T("SpatialShelter"));
            var walls = new GameObject("Committed walls"); walls.transform.SetParent(root.transform,false);
            Box(walls.transform,new Vector3(-2,1.5f,0),new Vector3(.15f,3,4));
            Box(walls.transform,new Vector3(2,1.5f,0),new Vector3(.15f,3,4));
            Box(walls.transform,new Vector3(0,1.5f,2),new Vector3(4,3,.15f));
            // Split front wall: an actual window, not a bounding box over the opening.
            Box(walls.transform,new Vector3(0,.35f,-2),new Vector3(4,.7f,.15f));
            Box(walls.transform,new Vector3(0,2.75f,-2),new Vector3(4,.5f,.15f));
            Physics.SyncTransforms(); Call(field,"Register",walls);
            Vector3 point = root.transform.position + Vector3.up;
            Vector2 open = (Vector2)Call(field,"Sample",point);
            Assert.That(open.x,Is.EqualTo(1)); Assert.That(open.y,Is.LessThan(1));
            var roof = Box(root.transform,new Vector3(0,3,0),new Vector3(4.3f,.2f,4.3f));
            Physics.SyncTransforms();
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.EqualTo(1),"Uncommitted previews do not enter shelter.");
            Call(field,"Register",roof);
            Vector2 covered = (Vector2)Call(field,"Sample",point);
            Assert.That(covered.x,Is.Zero); Assert.That(covered.y,Is.LessThan(open.y));
            Assert.That(((Vector2)Call(field,"Sample",point + Vector3.back*3)).x,Is.EqualTo(1));
            Call(field,"Unregister",walls); walls.SetActive(false);
            Assert.That(((Vector2)Call(field,"Sample",point)).y,Is.GreaterThan(covered.y),"Wall removal admits fill without changing overhead cover.");
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.Zero);
            Call(field,"Unregister",roof); roof.SetActive(false);
            Assert.That((Vector2)Call(field,"Sample",point),Is.EqualTo(Vector2.one));
            roof.SetActive(true); Physics.SyncTransforms(); Call(field,"Register",roof);
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.Zero,"Reload reconstructs neighboring roof cover.");
            UnityEngine.Object.Destroy(root); yield return null;
        }
        [UnityTest]
        public IEnumerator CacheWorkResidencyInvalidationAndCleanupAreBounded()
        {
            var root = new GameObject("Bounded shelter test");
            var field = root.AddComponent(T("SpatialShelter"));
            for (int i=0;i<32;i++) { Call(field,"Refresh",Vector3.zero); Assert.That((int)P(field,"LastSampleCount"),Is.LessThanOrEqualTo(512)); }
            Assert.That(P(field,"PendingSamples"),Is.EqualTo(0)); Assert.That(P(field,"CachedSamples"),Is.EqualTo(16384));
            Call(field,"Refresh",Vector3.zero); Assert.That(P(field,"LastSampleCount"),Is.EqualTo(0));
            Call(field,"Invalidate",new Bounds(new Vector3(1000,0,1000),Vector3.one));
            Assert.That(P(field,"PendingSamples"),Is.EqualTo(0),"Distant changes do not dirty local samples.");
            Call(field,"Invalidate",new Bounds(Vector3.zero,Vector3.one));
            Assert.That((int)P(field,"PendingSamples"),Is.GreaterThan(0)); Assert.That((int)P(field,"PendingSamples"),Is.LessThan(16384));
            for (int i=0;i<20;i++) { Call(field,"Refresh",Vector3.right*i*16); Assert.That((int)P(field,"CachedSamples"),Is.LessThanOrEqualTo(16384)); }
            var texture = (Texture3D)P(field,"Texture");
            UnityEngine.Object.Destroy(root); yield return null;
            Assert.That(texture == null,Is.True);
            Assert.That(Shader.GetGlobalVector("_TopazShelterOrigin").w == 0 || Shader.GetGlobalTexture("_TopazShelter") != texture,Is.True,"A surviving world may publish its own valid field after this owner is destroyed.");
        }
        [UnityTest]
        public IEnumerator CutawayKeepsCollisionAndShadowsAndRestoresAuthoredMode()
        {
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var player = new GameObject("Cutaway player");
            var renderer = roof.GetComponent<Renderer>(); renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
            var cutaway = (Behaviour)roof.AddComponent(T("HomeRoofVisibility")); Call(cutaway,"Bind",player.transform);
            yield return null;
            Assert.That(renderer.enabled,Is.True); Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.ShadowsOnly));
            Assert.That(roof.GetComponent<Collider>().enabled,Is.True);
            player.transform.position = Vector3.right*8; yield return null;
            Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.TwoSided));
            player.transform.position = Vector3.zero; yield return null; cutaway.enabled=false;
            Assert.That(renderer.shadowCastingMode,Is.EqualTo(ShadowCastingMode.TwoSided));
            UnityEngine.Object.Destroy(roof); UnityEngine.Object.Destroy(player); yield return null;
        }
        [UnityTest]
        public IEnumerator SavedConstructionUnloadsReloadsAndRemovalClearsShelter()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap"); yield return WaitForWilderness();
            var session=GameObject.Find("Player").GetComponent("WorldSession");
            var builds=(Component)BuildingTestActions.Field(session,"homeBuilds");
            var world=P(session,"ActiveWorld");
            var records=(IList)world.GetType().GetField("structures").GetValue(world);
            var record=Activator.CreateInstance(T("StructureStateRecord"));
            void Set(string n,object v)=>record.GetType().GetField(n).SetValue(record,v);
            Set("instanceId","test.shelter.roof"); Set("definitionId","structure.home.roof");
            Set("regionId",P(session,"CurrentRegionId")); Set("x",0f); Set("y",50f); Set("z",0f); records.Add(record);
            Call(builds,"Bind",session,world,P(session,"CurrentRegionId"));
            var field=P(builds,"Shelter"); var point=new Vector3(0,51,0);
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.Zero);
            Call(builds,"RefreshLoadedStructures",new Vector3(500,0,500));
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.EqualTo(1));
            Call(builds,"RefreshLoadedStructures",Vector3.zero); Physics.SyncTransforms();
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.Zero);
            // Removing a committed record must invalidate the already populated GPU field.
            for(int n=0;n<32;n++) Call(field,"Refresh",new Vector3(0,50,0));
            builds.GetType().GetField("_selected",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(builds,record);
            builds.GetType().GetMethod("RemoveSelected",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(builds,null);
            Assert.That(((Vector2)Call(field,"Sample",point)).x,Is.EqualTo(1));
            Assert.That((int)P(field,"PendingSamples"),Is.GreaterThan(0));
            Assert.That(records.Contains(record),Is.False);
        }
        [UnityTest]
        public IEnumerator WorldRainUsesRoofCollisionAndShelterReleasesOnReload()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap"); yield return WaitForWilderness();
            var rain = GameObject.Find("Weather Rain").GetComponent<ParticleSystem>();
            Assert.That(rain.collision.enabled,Is.True); Assert.That(rain.collision.lifetimeLoss.constant,Is.EqualTo(1));
            var shelter = UnityEngine.Object.FindAnyObjectByType(T("SpatialShelter"));
            Assert.That(shelter,Is.Not.Null);
            yield return null;
            var texture = (Texture3D)P(shelter,"Texture");
            yield return SceneManager.LoadSceneAsync("Bootstrap"); yield return WaitForWilderness(); yield return null;
            Assert.That(texture==null,Is.True);
            Assert.That(UnityEngine.Object.FindObjectsByType(T("SpatialShelter"),FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }
    }
}
