using System.Collections;
using Topaz.CombatStudy;
using Topaz.LoopStudy;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.Crypt
{
    /// <summary>References and persistent switches for the authored home crypt.</summary>
    public sealed class CryptSceneBootstrap : MonoBehaviour
    {
        [SerializeField] NavMeshSurface surface;
        [SerializeField] Transform arrival;
        [SerializeField] Transform departure;
        [SerializeField] Transform lever;
        [SerializeField] GameObject shortcutBarrier;
        [SerializeField] Transform materialCache;
        [SerializeField] GameObject cacheVisual;
        [SerializeField] Campfire campfire;
        [SerializeField] SafeZone campfireSafeZone;
        [SerializeField] EnemyCombatant[] ordinaryEnemies;
        [SerializeField] EnemyCombatant mage;
        [SerializeField] EnemyCombatant rogue;
        [SerializeField] Light ritualLight;

        WorldSession _session;
        bool _bound;

        public Transform Arrival => arrival;
        public Transform Departure => departure;
        public Transform Lever => lever;
        public Transform MaterialCache => materialCache;
        public Campfire Campfire => campfire;
        public EnemyCombatant Mage => mage;
        public EnemyCombatant Rogue => rogue;

        public bool InMageArena(Vector3 position) => mage != null &&
            Vector3.Distance(new Vector3(position.x, 0f, position.z),
                new Vector3(mage.transform.position.x, 0f, mage.transform.position.z)) < 7f;

        public void Bind(WorldSession session, PlayerVitality player, SafeZone home)
        {
            if (_bound) return;
            if (session == null || player == null || home == null || surface == null ||
                arrival == null || departure == null || lever == null ||
                shortcutBarrier == null || materialCache == null || cacheVisual == null ||
                campfire == null || campfireSafeZone == null || mage == null ||
                rogue == null || ritualLight == null ||
                ordinaryEnemies == null)
            {
                Debug.LogError("Crypt scene is missing a required reference.", this);
                return;
            }
            _bound = true;
            _session = session;
            SetShortcutOpen(session.CryptShortcutOpen);
            SetCacheStocked(!session.CryptCacheClaimed);
            SetMageAlive(!session.CryptMageDefeated);
            foreach (EnemyCombatant enemy in ordinaryEnemies)
                if (enemy != null) enemy.BindTarget(player, campfireSafeZone);
            mage.BindTarget(player, campfireSafeZone);
            StartCoroutine(ActivateEnemies());
        }

        IEnumerator ActivateEnemies()
        {
            for (int frame = 0; frame < 90; frame++)
            {
                bool ready = surface.navMeshData != null;
                foreach (EnemyCombatant enemy in ordinaryEnemies)
                    if (enemy != null && !NavMesh.SamplePosition(enemy.transform.position,
                        out _, 2f, NavMesh.AllAreas)) ready = false;
                if (!NavMesh.SamplePosition(mage.transform.position,
                    out _, 2f, NavMesh.AllAreas)) ready = false;
                if (ready)
                {
                    foreach (EnemyCombatant enemy in ordinaryEnemies)
                        if (enemy != null)
                        {
                            enemy.gameObject.SetActive(true);
                            _session.RegisterEnemy(enemy);
                        }
                    mage.gameObject.SetActive(true);
                    _session.RegisterEnemy(mage);
                    yield break;
                }
                yield return null;
            }
            Debug.LogError("Crypt enemies could not find their baked NavMesh.", this);
        }

        public void SetShortcutOpen(bool open) => shortcutBarrier.SetActive(!open);

        public void SetCacheStocked(bool stocked) => cacheVisual.SetActive(true);

        public void SetMageAlive(bool alive) => ritualLight.enabled = alive;
    }
}
