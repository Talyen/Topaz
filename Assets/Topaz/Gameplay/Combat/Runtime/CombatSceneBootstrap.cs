using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.CombatStudy
{
    /// <summary>Enables the enemy only after its authored NavMesh is registered.</summary>
    public sealed class CombatSceneBootstrap : MonoBehaviour
    {
        [SerializeField] NavMeshSurface surface;
        [SerializeField] GameObject enemy;

        IEnumerator Start()
        {
            if (surface == null || enemy == null)
            {
                Debug.LogError("Combat scene bootstrap is missing a reference.", this);
                yield break;
            }

            for (int frame = 0; frame < 60; frame++)
            {
                if (surface.navMeshData != null &&
                    NavMesh.SamplePosition(enemy.transform.position, out _, 2f, NavMesh.AllAreas))
                {
                    enemy.SetActive(true);
                    yield break;
                }
                yield return null;
            }

            Debug.LogError("The practice enemy could not find its baked NavMesh.", this);
        }
    }
}
