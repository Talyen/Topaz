using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Topaz.Gameplay;
namespace Topaz.Combat
{
    /// <summary>Active-region camp protection. No reward, respawn deadline, or world state is changed here.</summary>
    public sealed class CampSafety : MonoBehaviour
    {
        static readonly HashSet<CampSafety> active = new HashSet<CampSafety>();
        public float Radius => BuildingSettings.Current.campRadius;
        void OnEnable() => active.Add(this);
        void OnDisable() => active.Remove(this);
        void OnDestroy() => active.Remove(this);
        void Start()
        {
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Capsule;
            obstacle.radius = Radius;
            obstacle.height = 8;
            obstacle.center = Vector3.up * 2;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() => active.Clear();
        public static bool IsProtected(Vector3 point)
        {
            foreach (var camp in active)
                if (camp != null && HorizontalSquared(point - camp.transform.position) < camp.Radius * camp.Radius) return true;
            return false;
        }
        public static bool BlocksAttack(Vector3 from, Vector3 to)
        {
            foreach (var camp in active)
                if (camp != null && Intersects(from, to, camp.transform.position, camp.Radius)) return true;
            return false;
        }
        public static bool Intersects(Vector3 from, Vector3 to, Vector3 center, float radius)
        {
            from.y = to.y = center.y = 0;
            Vector3 segment = to - from;
            float t = segment.sqrMagnitude < .0001f ? 0 : Mathf.Clamp01(Vector3.Dot(center - from, segment) / segment.sqrMagnitude);
            return (from + t * segment - center).sqrMagnitude < radius * radius;
        }
        static float HorizontalSquared(Vector3 d) => d.x*d.x + d.z*d.z;
    }
}
