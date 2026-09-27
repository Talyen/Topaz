using UnityEngine;
namespace Topaz.Gameplay
{
    /// <summary>Optional interactive anchors on an authored building prefab.</summary>
    public sealed class BuildingVisualBinding : MonoBehaviour
    {
        public Transform doorPanel;
        public Collider doorCollider;
        public bool roof;
        public void Bind(Transform player)
        {
            if(doorPanel!=null) gameObject.AddComponent<HomeDoor>().Bind(doorPanel,doorCollider,player);
            if(roof) gameObject.AddComponent<HomeRoofVisibility>().Bind(player);
        }
    }
}
