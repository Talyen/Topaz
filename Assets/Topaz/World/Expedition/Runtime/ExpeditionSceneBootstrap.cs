using System.Collections;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.Expedition
{
    /// <summary>Owns the clearing's authored entry, cache, and local combat setup.</summary>
    public sealed class ExpeditionSceneBootstrap : MonoBehaviour
    {
        [SerializeField] NavMeshSurface surface;
        [SerializeField] Transform arrival;
        [SerializeField] Transform departure;
        [SerializeField] Transform cryptEntrance;
        [SerializeField] Transform supplyCache;
        [SerializeField] GameObject cacheVisual;
        [SerializeField] Campfire campfire;
        [SerializeField] EnemyCombatant[] enemies;
        [SerializeField] EnemyCombatant guardian;

        bool _bound;
        bool _enemiesReady;
        WorldSession _session;

        public Transform Arrival => arrival;
        public Transform Departure => departure;
        public Transform CryptEntrance => cryptEntrance;
        public Transform SupplyCache => supplyCache;
        public Campfire Campfire => campfire;
        public bool CacheUnlocked => _enemiesReady;

        public void Bind(WorldSession session, PlayerVitality player, SafeZone home,
            bool cacheClaimed)
        {
            if (_bound) return;
            if (session == null || surface == null || arrival == null || departure == null ||
                cryptEntrance == null || supplyCache == null ||
                cacheVisual == null || campfire == null || enemies == null || guardian == null ||
                player == null || home == null)
            {
                Debug.LogError("Expedition clearing is missing a required reference.", this);
                return;
            }

            _bound = true;
            _session = session;
            SetCacheStocked(!cacheClaimed);
            foreach (EnemyCombatant enemy in enemies)
            {
                if (enemy == null) continue;
                enemy.BindTarget(player, home);
            }
            StartCoroutine(ActivateEnemies());
        }

        public void SetCacheStocked(bool stocked) => cacheVisual.SetActive(true);

        IEnumerator ActivateEnemies()
        {
            for (int frame = 0; frame < 60; frame++)
            {
                bool ready = surface.navMeshData != null;
                foreach (EnemyCombatant enemy in enemies)
                    if (enemy != null && !NavMesh.SamplePosition(enemy.transform.position,
                        out _, 2f, NavMesh.AllAreas)) ready = false;
                if (ready)
                {
                    foreach (EnemyCombatant enemy in enemies)
                        if (enemy != null)
                        {
                            enemy.gameObject.SetActive(true);
                            _session.RegisterEnemy(enemy);
                        }
                    _enemiesReady = true;
                    yield break;
                }
                yield return null;
            }
            Debug.LogError("Expedition enemies could not find their baked NavMesh.", this);
        }
    }
}
