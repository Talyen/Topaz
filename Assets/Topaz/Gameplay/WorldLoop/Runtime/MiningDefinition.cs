using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Mining Node")]
    public sealed class MiningDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "resource.rock.starter";
        [SerializeField, Min(1)] int sourceLevel = 1;
        [SerializeField, Min(1)] int workRequired = 2;
        [SerializeField, Min(1)] int experience = 10;
        [SerializeField, Min(1)] int stoneYield = 3;
        [SerializeField, Min(1)] int ironYield = 1;
        [SerializeField, Min(1)] int regrowthHours = 72;

        public string StableId => stableId;
        public int SourceLevel => sourceLevel;
        public int WorkRequired => workRequired;
        public int Experience => experience;
        public int StoneYield => stoneYield;
        public int IronYield => ironYield;
        public int RegrowthHours => regrowthHours;
    }
}
