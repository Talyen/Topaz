using UnityEngine;

namespace Topaz.LoopStudy
{
    [CreateAssetMenu(menuName = "Topaz/Loop/Recipe")]
    public sealed class RecipeDefinition : ScriptableObject
    {
        [SerializeField] string stableId = "recipe.storage_chest";
        [SerializeField] ItemDefinition ingredient;
        [SerializeField, Min(1)] int ingredientCount = 3;
        [SerializeField] StructureDefinition result;

        public string StableId => stableId;
        public ItemDefinition Ingredient => ingredient;
        public int IngredientCount => ingredientCount;
        public StructureDefinition Result => result;
    }
}
