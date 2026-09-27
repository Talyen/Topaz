using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Topaz.Combat;
using Topaz.Expedition;
using Topaz.Generation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Topaz.Gameplay
{
    public sealed partial class WorldSession
    {
        WoodlandRegion activeWilderness;
        public WoodlandRegion ActiveRegion => activeWilderness != null ? activeWilderness :
            activeWilderness = FindInScene<WoodlandRegion>(SceneManager.GetSceneByName("Bootstrap"));

        IEnumerable<CampfireTravelCatalog.Destination> Destinations(TopazWorldData world)
        {
            yield return new CampfireTravelCatalog.Destination {stableId=Campfire.HomeId,regionId=TopazSaveData.WildernessRegion,sceneName="Bootstrap",label="First Hearth"};
            if (world == null) yield break;
            foreach (var s in world.structures)
                if (s.definitionId == BuildCatalog.Camp)
                    yield return new CampfireTravelCatalog.Destination {
                        stableId = s.instanceId, regionId = s.regionId,
                        sceneName = "Bootstrap",
                        label = string.IsNullOrEmpty(s.label) ? "Camp " + s.instanceId.Substring(0, 6) : s.label };
        }
        CampfireTravelCatalog.Destination ResolveDestination(string id, TopazWorldData world = null) =>
            Destinations(world ?? _world).FirstOrDefault(d => d.stableId == id);

        IEnumerable<Campfire> CurrentCampfires()
        {
            var fixedFire = homeCampfire;
            if (fixedFire != null) yield return fixedFire;
            foreach (var fire in homeBuilds.Campfires) if (fire != null) yield return fire;
        }
        Campfire NearbyCampfire() => _data == null ? null :
            CurrentCampfires().FirstOrDefault(f => f.Contains(transform.position));

        void CheckCampfire()
        {
            var fire = NearbyCampfire();
            if (fire == null || _visit == null || (_visit.lastCampfireId == fire.StableId &&
                _visit.discoveredCampfireIds.Contains(fire.StableId))) return;
            DiscoverCamp(fire.StableId);
            _visit.lastCampfireId = fire.StableId;
            fire.ShowActivation();
            Commit();
            hud.ShowStatus("Campfire discovered. Return point set.");
        }
        public void DiscoverCamp(string id)
        {
            if (_visit != null && !_visit.discoveredCampfireIds.Contains(id)) _visit.discoveredCampfireIds.Add(id);
        }
        public void ForgetCamp(string id)
        {
            foreach (var visit in _profile.visits.Where(v => v.worldId == ActiveWorldId))
            {
                visit.discoveredCampfireIds.Remove(id);
                if (visit.lastCampfireId == id) visit.lastCampfireId = Campfire.HomeId;
                if (!visit.discoveredCampfireIds.Contains(Campfire.HomeId)) visit.discoveredCampfireIds.Add(Campfire.HomeId);
            }
        }
        void BindRegionBuildings()
        {
            homeBuilds.Bind(this, _world, CurrentRegionId);
            _openChest = homeBuilds.Chests.FirstOrDefault();
            RefreshGatherables();
        }
        public void RebuildRegionNavigation()
        {
            var region = ActiveRegion;
            if (region?.navigation == null) return;
            Physics.SyncTransforms();
            region.Streaming?.InvalidateNavigation();
        }

        IEnumerator FastTravel(CampfireTravelCatalog.Destination destination)
        {
            _traveling = _fastTraveling = true;
            homeBuilds.Cancel();
            hud.ClosePanels(); combat.CancelActiveAttack();
            yield return FadeTrail(true);
            Vector3 destinationPosition;
            if (destination.stableId == Campfire.HomeId) destinationPosition = homeCampfire.ArrivalPosition;
            else
            {
                var record=_world.structures.Find(s=>s.instanceId==destination.stableId && s.definitionId==BuildCatalog.Camp);
                if(record==null)
                {
                    yield return FadeTrail(false);_traveling=_fastTraveling=false;
                    hud.ShowStatus("That camp is no longer available.");yield break;
                }
                destinationPosition=new Vector3(record.x,record.y,record.z)+Quaternion.Euler(0,record.quarterTurns*90,0)*Vector3.forward*1.25f;
            }
            var streaming=ActiveRegion.Streaming;
            yield return streaming.PrepareDestination(destinationPosition);
            if(!streaming.IsReadyAt(destinationPosition) || !TrySafeArrival(destinationPosition,out destinationPosition))
            {
                yield return streaming.PrepareDestination(transform.position);
                yield return FadeTrail(false);_traveling=_fastTraveling=false;
                hud.ShowStatus("Camp travel could not finish. Please retry.");yield break;
            }
            Teleport(destinationPosition);
            _visit.lastCampfireId=destination.stableId;
            look.SetInterior(false);
            RefreshPickupVisibility(); UpdateWeather(true);
            _traveling = false; Commit();
            yield return FadeTrail(false);
            _fastTraveling = false;
        }

        bool TrySafeArrival(Vector3 intended,out Vector3 result)
        {
            foreach(float radius in new[]{0f,1f,2f})for(int i=0;i<(radius==0?1:8);i++)
            {
                float angle=i*Mathf.PI/4;var point=intended+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                if(!ActiveRegion.Streaming.IsReadyAt(point))continue;
                point.y=WoodlandRegion.GroundHeight(point);
                bool blocked=false;
                foreach(var collider in Physics.OverlapCapsule(point+Vector3.up*.4f,point+Vector3.up*1.6f,.28f,~0,QueryTriggerInteraction.Ignore))
                    if(!(collider is TerrainCollider) && !collider.transform.IsChildOf(transform) && !homeBuilds.IsFloorCollider(collider)){blocked=true;break;}
                if(!blocked){result=point;return true;}
            }
            result=intended;return false;
        }

        IEnumerator RecoverAtCampfire(PlayerVitality vitality)
        {
            _recovering = true;
            homeBuilds.Cancel(); ExitPlacement(); hud.ClosePanels(); combat.CancelActiveAttack();
            Commit();
            yield return new WaitForSecondsRealtime(.22f);
            var destination = ResolveDestination(ReturnCampfireId) ?? ResolveDestination(Campfire.HomeId);
            yield return FastTravel(destination);
            if (CurrentRegionId != destination.regionId || ReturnCampfireId != destination.stableId)
                yield return FastTravel(ResolveDestination(Campfire.HomeId));
            vitality.RestoreAfterRecovery();
            _data.worldHours += WorldClock.RestHours;
            SurvivalRules.Advance(_character, WorldClock.RestHours);
            _character.stamina = SurvivalRules.Maximum(_character);
            RefreshGatherables(); look.SetWorldHours(WorldHours); UpdateWeather(true);
            _recovering = false; Commit();
            hud.ShowStatus("Recovered at the Campfire.");
        }
    }
}
