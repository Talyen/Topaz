using System;

namespace Topaz.LoopStudy
{
    /// <summary>Stable, World-owned home definitions. Costs are initial Mac review values.</summary>
    public static class HomeBuildCatalog
    {
        public const string Floor = "structure.home.stone-floor";
        public const string Wall = "structure.home.timber-wall";
        public const string Doorway = "structure.home.timber-doorway";
        public const string Roof = "structure.home.roof";
        public const string Bed = "structure.home.bed";
        public const string Bedroll = "structure.home.bedroll";
        public const string Chest = "structure.storage_chest";
        public const string Lantern = "structure.home.lantern";
        public const string Table = "structure.home.table";

        public readonly struct Entry
        {
            public readonly string Id;
            public readonly string Label;
            public readonly int Wood;
            public readonly int Stone;
            public readonly int Iron;
            public readonly bool Structural;

            public Entry(string id, string label, int wood, int stone, int iron,
                bool structural = false)
            {
                Id = id;
                Label = label;
                Wood = wood;
                Stone = stone;
                Iron = iron;
                Structural = structural;
            }
        }

        public static readonly Entry[] Entries =
        {
            new Entry(Floor, "Stone floor", 0, 1, 0, true),
            new Entry(Wall, "Timber wall", 1, 0, 0, true),
            new Entry(Doorway, "Working doorway", 2, 0, 1, true),
            new Entry(Roof, "Low roof", 1, 0, 0, true),
            new Entry(Chest, "Storage chest", 3, 0, 0),
            new Entry(Bed, "Bed", 4, 0, 0),
            new Entry(Lantern, "Lantern", 1, 0, 1),
            new Entry(Table, "Table", 2, 0, 0),
            new Entry(HomeBuilds.PathId, "Stone path", 0, 1, 0),
            new Entry(HomeBuilds.AnvilId, "Blacksmith's Anvil", 0, 6, 2)
        };

        public static Entry? Find(string id)
        {
            foreach (Entry entry in Entries)
                if (entry.Id == id) return entry;
            return null;
        }
    }
}
