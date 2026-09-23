using UnityEngine;

namespace Topaz.CombatStudy
{
    /// <summary>Temporary home boundary for the combat study, not a building system.</summary>
    public sealed class SafeZone : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] float radius = 2.6f;

        public float Radius => radius;

        public bool Contains(Vector3 position)
        {
            Vector3 horizontal = position - transform.position;
            horizontal.y = 0f;
            return horizontal.sqrMagnitude <= radius * radius;
        }
    }
}
