namespace Topaz.LoopStudy
{
    public static class HomeForgeCatalog
    {
        public const string Sword = "gear.sword.forged";
        public const string Helm = "gear.helm.forged";
        public const string Axe = "gear.axe.forged";

        public readonly struct Recipe
        {
            public readonly string ItemId;
            public readonly string Label;
            public readonly int Wood;
            public readonly int Stone;
            public readonly int Iron;
            public Recipe(string itemId, string label, int wood, int stone, int iron)
            {
                ItemId = itemId;
                Label = label;
                Wood = wood;
                Stone = stone;
                Iron = iron;
            }
        }

        public static readonly Recipe[] Recipes =
        {
            new Recipe(Sword, "Forged sword  +1 Attack", 2, 0, 3),
            new Recipe(Helm, "Iron helm  +1 Armor", 0, 3, 3),
            new Recipe(Axe, "Reinforced axe  +1 Logging", 3, 0, 2)
        };
    }
}
