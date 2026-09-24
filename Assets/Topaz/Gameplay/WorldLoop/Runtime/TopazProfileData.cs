using System;
using System.Collections.Generic;
using System.Linq;

namespace Topaz.LoopStudy
{
    /// <summary>Stable look IDs are independent of menu labels and model asset paths.</summary>
    public static class CharacterLooks
    {
        public const string Rogue = "rogue";
        public const string HoodedRogue = "rogue.hooded";
        public const string Knight = "knight";
        public const string Ranger = "ranger";
        public const string Mage = "mage";
        public const string Barbarian = "barbarian";

        public static readonly string[] All =
            { Rogue, HoodedRogue, Knight, Ranger, Mage, Barbarian };

        public static string Label(string id)
        {
            switch (id)
            {
                case HoodedRogue: return "Hooded Rogue";
                case Knight: return "Knight";
                case Ranger: return "Ranger";
                case Mage: return "Mage";
                case Barbarian: return "Barbarian";
                default: return "Rogue";
            }
        }

        public static string SupportedOrRogue(string id) => All.Contains(id) ? id : Rogue;
    }

    /// <summary>One local collection, with separate Character, World, and Visit records.</summary>
    [Serializable]
    public sealed class TopazProfileData
    {
        public const int CurrentVersion = 1;
        public int version = CurrentVersion;
        public List<TopazCharacterData> characters = new List<TopazCharacterData>();
        public List<TopazWorldData> worlds = new List<TopazWorldData>();
        public List<TopazVisitData> visits = new List<TopazVisitData>();
        public string lastCharacterId;
        public string lastWorldId;

        public TopazCharacterData Character(string id) =>
            characters.Find(value => value.id == id);

        public TopazWorldData World(string id) => worlds.Find(value => value.id == id);

        public TopazVisitData Visit(string characterId, string worldId) =>
            visits.Find(value => value.characterId == characterId && value.worldId == worldId);

        public TopazCharacterData CreateCharacter(string appearanceId)
        {
            string look = CharacterLooks.SupportedOrRogue(appearanceId);
            int ordinal = characters.Count(value =>
                CharacterLooks.SupportedOrRogue(value.appearanceId) == look) + 1;
            var character = new TopazCharacterData
            {
                id = Guid.NewGuid().ToString("N"),
                appearanceId = look,
                label = CharacterLooks.Label(look) + " " + ordinal
            };
            characters.Add(character);
            return character;
        }

        public TopazWorldData CreateWorld()
        {
            var world = new TopazWorldData
            {
                id = Guid.NewGuid().ToString("N"),
                label = "World " + (worlds.Count + 1)
            };
            worlds.Add(world);
            return world;
        }

        public TopazVisitData GetOrCreateVisit(string characterId, string worldId)
        {
            TopazVisitData visit = Visit(characterId, worldId);
            if (visit != null) return visit;
            visit = new TopazVisitData { characterId = characterId, worldId = worldId };
            visits.Add(visit);
            return visit;
        }

        public static TopazProfileData FromLegacy(TopazSaveData old)
        {
            if (old == null) throw new ArgumentNullException(nameof(old));
            var profile = new TopazProfileData();
            var character = new TopazCharacterData
            {
                id = "legacy-character",
                label = "Rogue 1",
                appearanceId = CharacterLooks.Rogue,
                loggingExperience = old.loggingExperience,
                swordsExperience = old.swordsExperience,
                equippedTool = old.equippedTool,
                pendingChest = old.pendingChest,
                backpackSlots = old.backpackSlots
            };
            var world = new TopazWorldData
            {
                id = "legacy-world",
                label = "World 1",
                worldHours = old.worldHours,
                expeditionCacheClaimed = old.expeditionCacheClaimed,
                nodes = old.nodes,
                structures = old.structures,
                pickups = old.pickups
            };
            profile.characters.Add(character);
            profile.worlds.Add(world);
            profile.visits.Add(new TopazVisitData
            {
                characterId = character.id,
                worldId = world.id,
                regionId = old.regionId,
                playerX = old.playerX,
                playerZ = old.playerZ
            });
            profile.lastCharacterId = character.id;
            profile.lastWorldId = world.id;
            profile.Validate();
            return profile;
        }

        public void Validate()
        {
            if (version != CurrentVersion || characters == null || worlds == null || visits == null)
                throw new ArgumentException("Character and World collection format is invalid.");
            var characterIds = new HashSet<string>();
            foreach (TopazCharacterData character in characters)
            {
                if (character == null || string.IsNullOrEmpty(character.id) ||
                    !characterIds.Add(character.id) || string.IsNullOrEmpty(character.label) ||
                    character.backpackSlots == null || character.backpackSlots.Count > 16 ||
                    character.loggingExperience < 0 || character.swordsExperience < 0)
                    throw new ArgumentException("Character record is invalid.");
                ValidateItems(character.backpackSlots);
            }
            var worldIds = new HashSet<string>();
            foreach (TopazWorldData world in worlds)
            {
                if (world == null || string.IsNullOrEmpty(world.id) || !worldIds.Add(world.id) ||
                    string.IsNullOrEmpty(world.label) || world.nodes == null ||
                    world.structures == null || world.pickups == null ||
                    double.IsNaN(world.worldHours) || double.IsInfinity(world.worldHours) ||
                    world.worldHours < WorldClock.StartingHour)
                    throw new ArgumentException("World record is invalid.");
                foreach (StructureStateRecord structure in world.structures)
                {
                    if (structure == null || structure.slots == null || structure.slots.Count > 12)
                        throw new ArgumentException("World storage is invalid.");
                    ValidateItems(structure.slots);
                }
                foreach (PickupStateRecord pickup in world.pickups)
                    if (pickup == null || string.IsNullOrEmpty(pickup.instanceId) ||
                        string.IsNullOrEmpty(pickup.itemId) || pickup.count < 1)
                        throw new ArgumentException("World pickup is invalid.");
                foreach (NodeStateRecord node in world.nodes)
                    if (node == null || double.IsNaN(node.readyAtWorldHours) ||
                        double.IsInfinity(node.readyAtWorldHours) || node.readyAtWorldHours < 0)
                        throw new ArgumentException("World node is invalid.");
            }
            var pairs = new HashSet<string>();
            foreach (TopazVisitData visit in visits)
                if (visit == null || !characterIds.Contains(visit.characterId) ||
                    !worldIds.Contains(visit.worldId) ||
                    !pairs.Add(visit.characterId + "/" + visit.worldId) ||
                    (visit.regionId != TopazSaveData.HomeRegion &&
                     visit.regionId != TopazSaveData.ExpeditionRegion) ||
                    float.IsNaN(visit.playerX) || float.IsNaN(visit.playerZ) ||
                    float.IsInfinity(visit.playerX) || float.IsInfinity(visit.playerZ))
                    throw new ArgumentException("Character visit is invalid.");
            if (!string.IsNullOrEmpty(lastCharacterId) || !string.IsNullOrEmpty(lastWorldId))
                if (Visit(lastCharacterId, lastWorldId) == null)
                    throw new ArgumentException("Last Character and World pair is invalid.");
        }

        static void ValidateItems(List<ItemStackRecord> slots)
        {
            foreach (ItemStackRecord slot in slots)
                if (slot == null || slot.count < 0 ||
                    (slot.count > 0 && string.IsNullOrEmpty(slot.itemId)))
                    throw new ArgumentException("Stored item is invalid.");
        }
    }

    [Serializable]
    public sealed class TopazCharacterData
    {
        public string id;
        public string label;
        public string appearanceId = CharacterLooks.Rogue;
        public int loggingExperience;
        public int swordsExperience;
        public string equippedTool = "sword";
        public bool pendingChest;
        public List<ItemStackRecord> backpackSlots = new List<ItemStackRecord>();
    }

    [Serializable]
    public sealed class TopazWorldData
    {
        public string id;
        public string label;
        public double worldHours = WorldClock.StartingHour;
        public bool expeditionCacheClaimed;
        public List<NodeStateRecord> nodes = new List<NodeStateRecord>();
        public List<StructureStateRecord> structures = new List<StructureStateRecord>();
        public List<PickupStateRecord> pickups = new List<PickupStateRecord>();
    }

    [Serializable]
    public sealed class TopazVisitData
    {
        public string characterId;
        public string worldId;
        public string regionId = TopazSaveData.HomeRegion;
        public float playerX;
        public float playerZ;
    }
}
