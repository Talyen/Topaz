using System.Collections;
using System.Linq;
using Topaz.Combat;
using Topaz.Generation;
using UnityEngine;

namespace Topaz.Gameplay
{
    public sealed partial class WorldSession
    {
        public string CurrentAreaLabel => _world?.graph?.Find(CurrentRegionId)?.label ?? "Wilderness";
        public bool IsAreaTraveling => _traveling;
        void ShowHomeFurniture(bool show)
        {homeCampfire.gameObject.SetActive(show);if(workbench!=null)workbench.gameObject.SetActive(show);if(gearRack!=null)gearRack.gameObject.SetActive(show);}
        void CaptureAreaEnemies()
        {
            if(_world==null || !_world.generationSettings.boundedAreas)return;
            _world.enemyStates.RemoveAll(e=>e.areaId==CurrentRegionId);
            foreach(var enemy in _boundEnemies)
            {
                if(enemy==null || !enemy.IsAlive)continue;
                var point=enemy.transform.position;
                _world.enemyStates.Add(new EnemyStateRecord {spawnId=enemy.SpawnId,areaId=CurrentRegionId,health=enemy.CurrentHealth,x=point.x,y=point.y,z=point.z});
            }
        }
        public bool RequestAreaTravel(string exitId)
        {
            if(_world?.graph==null || _traveling || _fastTraveling || _resting || _recovering || MenuOpen || !ActiveRegion.Streaming.InitialReady)return false;
            var exit=_world.graph.Find(CurrentRegionId)?.exits.Find(e=>e.id==exitId);
            if(exit==null || Vector3.Distance(transform.position,ActiveRegion.Wilderness.ExitPosition(exit)+Vector3.up*transform.position.y)>InteractionRadius+1)return false;
            Commit();FlushSave();if(!_savingEnabled)return false;
            StartCoroutine(TravelToArea(exit.destinationAreaId,exit.destinationExitId,null));return true;
        }
        IEnumerator TravelToArea(string areaId,string arrivalExitId,Vector3? campPoint)
        {
            // One transaction owns retirement and publication. Save the origin before mutating visit identity.
            var origin=new AreaLocation(CurrentRegionId,transform.position);
            _traveling=true;homeBuilds.Cancel();ExitPlacement();hud.ClosePanels();combat.CancelActiveAttack();
            float resumeScale=Time.timeScale;
            Time.timeScale=0;
            foreach(var bolt in FindObjectsByType<CrossbowBolt>(FindObjectsSortMode.None))Destroy(bolt.gameObject);
            foreach(var spell in FindObjectsByType<GroundSpellAbility>(FindObjectsSortMode.None))spell.Cancel();
            try
            {
            yield return FadeTrail(true);
            bool arrived=false;
            yield return LoadArea(areaId,arrivalExitId,campPoint,success=>arrived=success);
            if(!arrived)
            {
                bool recovered=false;
                yield return LoadArea(origin.areaId,null,origin.position,success=>recovered=success);
                if(!recovered){_generationFailed=true;hud.ShowStatus("Area loading failed. Earlier progress is safe. Return to the title and retry.");}
            }
            hud.HideAreaLoading();
            yield return FadeTrail(false);
            _traveling=false;
            Time.timeScale=resumeScale;
            if(arrived || !_generationFailed){Commit();FlushSave();}
            if(!arrived)hud.ShowStatus("Travel could not finish. Your departure point was retained.");
            }
            finally{Time.timeScale=resumeScale;_traveling=false;hud?.HideAreaLoading();}
        }
        IEnumerator LoadArea(string areaId,string arrivalExitId,Vector3? point,System.Action<bool> completed)
        {
            var record=_world.graph.Find(areaId);
            if(record==null){completed(false);yield break;}
            var region=ActiveRegion;
            hud.ShowAreaLoading(record.label,"Preparing landscape");
            ClearBoundEnemies();
            yield return region.ReleaseArea(this);
            ShowHomeFurniture(areaId==TopazSaveData.HomeRegion);
            region.regionId=areaId;_data.regionId=areaId;
            var settings=AreaPlan.ForArea(record,_world.generationSettings);
            var exit=record.exits.Find(e=>e.id==arrivalExitId);
            Vector3 arrival=point??(exit!=null?exit.Position(settings.worldSize/2-56):Vector3.zero);
            homeBuilds.Bind(this,_world,areaId);InvalidateStorageIndex();
            bool started=false;
            try{region.Generate(_world,arrival);started=true;}
            catch(System.Exception error){Debug.LogException(error);}
            if(!started){completed(false);yield break;}
            while(!region.Streaming.InitialReady && region.Streaming.Failure==null)
            {hud.ShowAreaLoading(record.label,region.Streaming.PreparationStage);yield return null;}
            if(!point.HasValue && exit!=null)arrival=region.Wilderness.ArrivalPosition(exit);
            if(region.Streaming.Failure!=null || !TrySafeArrival(arrival,out arrival)){completed(false);yield break;}
            Teleport(arrival);RefreshPickupVisibility();UpdateWeather(true);
            if(Camera.main!=null)Camera.main.GetComponent<Topaz.Player.PlayerCamera>()?.SnapAfterAreaTravel();
            if(Camera.main!=null && Camera.main.TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var cameraData))cameraData.resetHistory=true;
            look.BeginWorldPresentation(WorldHours);look.SetWorldHours(WorldHours);
            if(!_visit.discoveredAreaIds.Contains(areaId))_visit.discoveredAreaIds.Add(areaId);
            _generationFailed=false;completed(true);
        }
        public string DiscoveredAreaConnections()
        {
            if(_world?.graph==null || _visit==null)return "";
            var lines=new System.Text.StringBuilder("\n\nWILDERNESS ROUTES\n");
            foreach(var area in _world.graph.areas.Where(a=>_visit.discoveredAreaIds.Contains(a.id)))
            {
                lines.Append(area.id==CurrentRegionId?"● ":"· ").Append(area.label).Append(": ");
                lines.AppendLine(string.Join(", ",area.exits.Select(e=>_visit.discoveredAreaIds.Contains(e.destinationAreaId)?_world.graph.Find(e.destinationAreaId).label:"Unexplored passage")));
            }
            return lines.ToString();
        }
    }
}
