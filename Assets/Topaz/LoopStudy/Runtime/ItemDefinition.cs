using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Item")]
    public sealed class ItemDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "material.wood";
        [SerializeField] string displayName = "Wood";
        [SerializeField, Min(1)] int maxStack = 20;

        public string StableId => stableId;
        public string DisplayName => displayName;
        public int MaxStack => maxStack;
    }
}
