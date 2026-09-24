using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Harvest Node")]
    public sealed class HarvestDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "resource.tree.wood";
        [SerializeField] ItemDefinition yieldItem;
        [SerializeField] string requiredToolId = "axe";
        [SerializeField, Min(1)] int chopsRequired = 3;
        [SerializeField, Min(1)] int yieldCount = 6;
        [SerializeField, Min(1)] int loggingExperiencePerChop = 1;
        [SerializeField, Min(1)] int regrowthDays = 3;

        public string StableId => stableId;
        public ItemDefinition YieldItem => yieldItem;
        public string RequiredToolId => string.IsNullOrEmpty(requiredToolId) ? "axe" : requiredToolId;
        public int ChopsRequired => chopsRequired;
        public int YieldCount => yieldCount;
        public int LoggingExperiencePerChop => loggingExperiencePerChop;
        public int RegrowthDays => regrowthDays;
    }
}
