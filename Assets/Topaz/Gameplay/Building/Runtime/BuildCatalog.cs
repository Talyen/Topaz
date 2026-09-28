using System;

namespace Topaz.Gameplay
{
    /// <summary>Stable, World-owned home definitions. Costs are initial Mac review values.</summary>
    public static class BuildCatalog
    {
        public const string Camp = "structure.campfire";
        public const string Floor = "structure.home.stone-floor";
        public const string Wall = "structure.home.timber-wall";
        public const string Doorway = "structure.home.timber-doorway";
        public const string Roof = "structure.home.roof";
        public const string Bed = "structure.home.bed";
        public const string Bedroll = "structure.home.bedroll";
        public const string Chest = "structure.storage_chest";
        public const string Lantern = "structure.home.lantern";
        public const string Table = "structure.home.table";

        public const string TimberFloor = "structure.viking.floor";
        public const string HalfWall = "structure.viking.half-wall";
        public const string Beam = "structure.viking.beam";
        public const string Fence = "structure.viking.fence";
        public const string Bench = "structure.viking.bench";
        public const string Chair = "structure.viking.chair";
        public const string Shelf = "structure.viking.shelf";
        public const string WeaponRack = "structure.viking.weapon-rack";
        public static bool IsFloor(string id) => id == Floor || id == TimberFloor;
        public static bool IsWall(string id) => id == Wall || id == Doorway || id == HalfWall;
        public static bool IsRoofSupport(string id) => id == Wall || id == Doorway || id == Beam;
        public static bool IsShelter(string id) => IsWall(id) || id == Roof;

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

        public static Entry[] Entries => new[]
        {
            new Entry(TimberFloor, "Timber floor", 1, 0, 0, true),
            new Entry(HalfWall, "Log half-wall", 1, 0, 0, true),
            new Entry(Beam, "Timber pillar", 1, 0, 0, true),
            new Entry(Fence, "Timber fence", 1, 0, 0),
            new Entry(Bench, "Bench", 2, 0, 0),
            new Entry(Chair, "Chair", 1, 0, 0),
            new Entry(Shelf, "Shelf", 2, 0, 0),
            new Entry(WeaponRack, "Weapon rack", 2, 0, 0),
            new Entry(Camp, "Campfire", BuildingSettings.Current.campWood, BuildingSettings.Current.campStone, 0),
            new Entry(Bedroll, "Bedroll", 2, 0, 0),
            new Entry(Floor, "Stone floor", 0, 1, 0, true),
            new Entry(Wall, "Timber wall", 1, 0, 0, true),
            new Entry(Doorway, "Working doorway", 2, 0, 1, true),
            new Entry(Roof, "Low roof", 1, 0, 0, true),
            new Entry(Chest, "Storage chest", 3, 0, 0),
            new Entry(Bed, "Bed", 4, 0, 0),
            new Entry(Lantern, "Lantern", 1, 0, 1),
            new Entry(Table, "Table", 2, 0, 0),
            new Entry(RegionBuildings.PathId, "Stone path", 0, 1, 0),
            new Entry(RegionBuildings.AnvilId, "Blacksmith's Anvil", 0, 6, 2)
        };

        public static Entry? Find(string id)
        {
            foreach (Entry entry in Entries)
                if (entry.Id == id) return entry;
            return null;
        }
    }
}
