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
        public WoodlandRegion ActiveRegion => FindInScene<WoodlandRegion>(SceneManager.GetSceneByName(
            CurrentRegionId == TopazSaveData.HomeRegion ? "Bootstrap" : ExpeditionSceneName));

        IEnumerable<CampfireTravelCatalog.Destination> Destinations(TopazWorldData world)
        {
            if (_travelCatalog != null)
                foreach (var d in _travelCatalog.Destinations)
                    if (d.regionId == TopazSaveData.HomeRegion || d.regionId == TopazSaveData.ExpeditionRegion) yield return d;
            if (world == null) yield break;
            foreach (var s in world.structures)
                if (s.definitionId == BuildCatalog.Camp)
                    yield return new CampfireTravelCatalog.Destination {
                        stableId = s.instanceId, regionId = s.regionId,
                        sceneName = s.regionId == TopazSaveData.HomeRegion ? "Bootstrap" : ExpeditionSceneName,
                        label = string.IsNullOrEmpty(s.label) ? "Camp " + s.instanceId.Substring(0, 6) : s.label };
        }
        CampfireTravelCatalog.Destination ResolveDestination(string id, TopazWorldData world = null) =>
            Destinations(world ?? _world).FirstOrDefault(d => d.stableId == id);

        IEnumerable<Campfire> CurrentCampfires()
        {
            var fixedFire = IsAtHome ? homeCampfire : _expedition?.Campfire;
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
            region.navigation.BuildNavMesh();
        }

        IEnumerator FastTravel(CampfireTravelCatalog.Destination destination)
        {
            _traveling = _fastTraveling = true;
            homeBuilds.Cancel();
            hud.ClosePanels(); combat.CancelActiveAttack();
            yield return FadeTrail(true);
            string previous = CurrentRegionId;
            var targetScene = SceneManager.GetSceneByName(destination.sceneName);
            bool loadedHere = !targetScene.isLoaded;
            if (loadedHere)
            {
                AsyncOperation load = null;
                try { load = LoadRegionSceneAsync(destination.sceneName); }
                catch (Exception e) { Debug.LogWarning("Camp travel unavailable: " + e.Message); }
                if (load != null) yield return load;
                targetScene = SceneManager.GetSceneByName(destination.sceneName);
            }
            bool ready = targetScene.isLoaded && PrepareGeneratedRegion(targetScene);
            if (ready)
            {
                _data.regionId = destination.regionId;
                BindRegionBuildings();
            }
            Campfire target = ready ? CurrentCampfires().FirstOrDefault(f => f.StableId == destination.stableId)
                ?? FindCampfire(targetScene, destination.stableId) : null;
            if (target == null || !target.HasArrival)
            {
                _data.regionId = previous;
                BindRegionBuildings();
                if (loadedHere && targetScene.isLoaded) yield return SceneManager.UnloadSceneAsync(targetScene);
                _generationFailed = false;
                yield return FadeTrail(false);
                _traveling = _fastTraveling = false;
                hud.ShowStatus("That destination is unavailable.");
                yield break;
            }
            ClearBoundEnemies();
            if (destination.regionId == TopazSaveData.ExpeditionRegion)
            {
                _expedition = FindInScene<ExpeditionSceneBootstrap>(targetScene);
                _expedition?.Bind(this, GetComponent<PlayerVitality>(), home, ExpeditionCacheClaimed);
                BindGatherables(targetScene);
                foreach (var enemy in targetScene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<EnemyCombatant>(true))) RegisterEnemy(enemy);
            }
            if (destination.regionId == TopazSaveData.HomeRegion) GetComponent<PlayerVitality>()?.PreserveHealthOnHomeArrival();
            Teleport(target.ArrivalPosition);
            _visit.lastCampfireId = target.StableId;
            look.SetInterior(false);
            if (destination.regionId == TopazSaveData.HomeRegion)
            {
                var old = SceneManager.GetSceneByName(ExpeditionSceneName);
                _expedition = null;
                if (old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            }
            RefreshPickupVisibility(); UpdateWeather(true);
            _traveling = false; Commit();
            yield return FadeTrail(false);
            _fastTraveling = false;
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
