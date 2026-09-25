using UnityEngine;

namespace Topaz.VisualStudy
{
    /// <summary>Finds the walkable surface under a character for floor-level presentation.</summary>
    public static class GroundSurface
    {
        static readonly RaycastHit[] Hits = new RaycastHit[16];

        public static float Height(Vector3 position)
        {
            int count = Physics.RaycastNonAlloc(position + Vector3.up * .6f,
                Vector3.down, Hits, 1.4f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            float bestDistance = float.PositiveInfinity;
            float height = position.y;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = Hits[i];
                if (hit.normal.y < .7f || hit.collider.GetComponentInParent<Topaz.FeelStudy.FeelStudyPlayer>() != null ||
                    hit.collider.GetComponentInParent<Topaz.CombatStudy.EnemyCombatant>() != null)
                    continue;
                float distance = Mathf.Abs(hit.point.y - position.y);
                if (distance > .45f || distance >= bestDistance) continue;
                bestDistance = distance;
                height = hit.point.y;
            }
            return height;
        }
    }
}
