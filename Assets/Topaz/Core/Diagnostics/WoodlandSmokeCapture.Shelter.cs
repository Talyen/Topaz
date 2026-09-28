using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using UnityEngine;

namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        void M0SetConditions(double hour, string weather)
        {
            m0Hour=hour; m0Weather=weather;
            m0Session.SetEventWeatherOverride(weather);
            FindAnyObjectByType<Topaz.Rendering.WeatherPresentation>().SetCondition(weather,true);
            m0Session.GetComponent<Topaz.Player.PlayerLantern>().SetLit(false);
        }
        IEnumerator ShelterReady()
        {
            yield return null;
            var field=FindAnyObjectByType<SpatialShelter>();
            float deadline=Time.realtimeSinceStartup+15;
            while(field!=null&&field.PendingSamples>0)
            {
                if(Time.realtimeSinceStartup>deadline)throw new InvalidOperationException("Shelter capture cache did not settle.");
                yield return null;
            }
        }
        IEnumerator ShelterFixedShot(string directory,string name,M0Report report,Vector3 position,Vector3 target,bool hideAvatar)
        {
            yield return ShelterReady();
            var camera=Camera.main;var brain=camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();
            var adapter=m0Camera;bool brainEnabled=brain!=null&&brain.enabled;
            var renderers=m0Session.GetComponentsInChildren<Renderer>();
            var visible=renderers.Select(r=>r.enabled).ToArray();
            try
            {
                if(brain!=null)brain.enabled=false;m0Camera=null;
                if(hideAvatar)foreach(var renderer in renderers)renderer.enabled=false;
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));
                yield return new WaitForSecondsRealtime(1);
                yield return M0Shot(directory,name,report);
            }
            finally
            {
                for(int i=0;i<renderers.Length;i++)if(renderers[i]!=null)renderers[i].enabled=visible[i];
                m0Camera=adapter;if(brain!=null)brain.enabled=brainEnabled;
            }
        }
        IEnumerator ShelterSteps(WorldSession session, string directory, M0Report report)
        {
            yield return new WaitForSecondsRealtime(8);
            m0Camera.SetZoom(3.5f);
            var builds = (RegionBuildings)typeof(WorldSession).GetField("homeBuilds", M0Fields).GetValue(session);
            Vector3 center = new Vector3(-9,0,-9);
            center.y = Topaz.Generation.WoodlandRegion.GroundHeight(center);
            var records = new List<StructureStateRecord>();
            StructureStateRecord Add(string id, float x, float z, int turns=0)
            {
                var record = new StructureStateRecord { instanceId="m4."+records.Count, definitionId=id,
                    regionId=session.CurrentRegionId, x=center.x+x, y=center.y, z=center.z+z, quarterTurns=turns };
                records.Add(record); session.ActiveWorld.structures.Add(record); return record;
            }
            for(int x=-1;x<=1;x++) for(int z=-1;z<=1;z++) Add(BuildCatalog.Floor,x*1.5f,z*1.5f);
            for(int i=-1;i<=1;i++)
            {
                Add(BuildCatalog.Wall,-2.25f,i*1.5f,1); Add(BuildCatalog.Wall,2.25f,i*1.5f,1);
                Add(BuildCatalog.Wall,i*1.5f,2.25f); Add(i==0?BuildCatalog.Doorway:BuildCatalog.Wall,i*1.5f,-2.25f);
            }
            void Rebind() { builds.Bind(session,session.ActiveWorld,session.CurrentRegionId); session.RefreshBuiltGround(center,4); Physics.SyncTransforms(); }
            Rebind();
            M0SetConditions(12,WeatherSchedule.Rain);
            MovePlayer(session,center); m0Facing=Vector3.back;
            yield return new WaitForSecondsRealtime(2);
            yield return ShelterReady();
            yield return M0Shot(directory,"m4-walls-no-roof",report);
            yield return ShelterFixedShot(directory,"m4-walls-overview",report,center+new Vector3(5,6,5),center+Vector3.up,false);
            var roofs = new List<StructureStateRecord>();
            for(int x=-1;x<=1;x++) for(int z=-1;z<=1;z++) roofs.Add(Add(BuildCatalog.Roof,x*1.5f,z*1.5f));
            Rebind(); yield return new WaitForSecondsRealtime(2);
            yield return ShelterReady();
            yield return M0Shot(directory,"m4-roof-cutaway-rain",report);
            yield return ShelterFixedShot(directory,"m4-roof-overview-rain",report,center+new Vector3(5,6,5),center+Vector3.up,false);
            builds.Shelter.enabled=false;
            yield return new WaitForSecondsRealtime(1);
            yield return ShelterFixedShot(directory,"m4-roof-fill-disabled-control",report,center+new Vector3(5,6,5),center+Vector3.up,false);
            builds.Shelter.enabled=true;
            yield return new WaitForSecondsRealtime(1);
            var field=builds.Shelter;
            var samples=new List<string> { "center covered="+field.Sample(center+Vector3.up), "outside="+field.Sample(center+Vector3.back*4+Vector3.up) };
            // Actual gameplay camera approach keeps outside rain visible while the player is covered.
            MovePlayer(session,center+Vector3.back*1.4f); yield return new WaitForSecondsRealtime(2);
            yield return M0Shot(directory,"m4-doorway-rain",report);
            yield return ShelterFixedShot(directory,"m4-interior-through-door",report,center+new Vector3(0,1.3f,.6f),center+new Vector3(0,1.3f,-6),true);
            var windowWall=records.First(r=>r.definitionId==BuildCatalog.Wall && Mathf.Approximately(r.x,center.x-2.25f) && Mathf.Approximately(r.z,center.z));
            session.ActiveWorld.structures.Remove(windowWall); Rebind();
            var window=new GameObject("M4 split window fixture"); window.transform.position=center+Vector3.left*2.25f;
            void WindowPart(Vector3 p,Vector3 size)
            {
                var cube=GameObject.CreatePrimitive(PrimitiveType.Cube);cube.transform.SetParent(window.transform,false);
                cube.transform.localPosition=p;cube.transform.localScale=size;
                cube.GetComponent<Renderer>().sharedMaterial=BuildingSettings.Current.surfaceMaterial;
            }
            WindowPart(new Vector3(0,.4f,0),new Vector3(.17f,.8f,1.5f));
            WindowPart(new Vector3(0,1.9f,0),new Vector3(.17f,.2f,1.5f));
            WindowPart(new Vector3(0,1.3f,-.65f),new Vector3(.17f,1,.2f));
            WindowPart(new Vector3(0,1.3f,.65f),new Vector3(.17f,1,.2f));
            Physics.SyncTransforms();builds.Shelter.Register(window);
            MovePlayer(session,center);m0Facing=Vector3.left;
            yield return ShelterFixedShot(directory,"m4-interior-through-window",report,center+new Vector3(-.5f,1.3f,0),center+new Vector3(-6,1.3f,0),true);
            builds.Shelter.Unregister(window);Destroy(window);session.ActiveWorld.structures.Add(windowWall);Rebind();
            MovePlayer(session,center+Vector3.back*7); m0Facing=Vector3.forward;
            yield return new WaitForSecondsRealtime(2);
            yield return M0Shot(directory,"m4-exterior-roof-rain",report);
            MovePlayer(session,center); m0Facing=Vector3.back;
            M0SetConditions(24,WeatherSchedule.Clear); yield return new WaitForSecondsRealtime(2);
            yield return ShelterFixedShot(directory,"m4-night-no-torch",report,center+new Vector3(5,6,5),center+Vector3.up,false);
            var lamp = new GameObject("M4 interior torch"); lamp.transform.position=center+new Vector3(.5f,1.2f,1);
            var light=lamp.AddComponent<Light>(); light.type=LightType.Point;light.range=6;light.intensity=2.5f;light.color=new Color(1,.55f,.22f);
            yield return new WaitForSecondsRealtime(1);
            yield return ShelterFixedShot(directory,"m4-night-torch",report,center+new Vector3(5,6,5),center+Vector3.up,false);
            Destroy(lamp);
            M0SetConditions(12,WeatherSchedule.Rain);
            var wall=records.First(r=>r.definitionId==BuildCatalog.Wall && Mathf.Approximately(r.x,center.x-2.25f));
            session.ActiveWorld.structures.Remove(wall); Rebind();
            yield return new WaitForSecondsRealtime(2);
            yield return M0Shot(directory,"m4-wall-removed",report);
            foreach(var roof in roofs) session.ActiveWorld.structures.Remove(roof);
            Rebind(); yield return new WaitForSecondsRealtime(2);
            samples.Add("roof removed="+builds.Shelter.Sample(center+Vector3.up));
            yield return M0Shot(directory,"m4-roof-removed",report);
            File.WriteAllLines(Path.Combine(directory,"shelter-samples.txt"),samples);
        }
    }
}
