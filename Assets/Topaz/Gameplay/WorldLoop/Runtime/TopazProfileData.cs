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
        public const int CurrentVersion = 8;
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
            int ordinal = 1;
            string labelPrefix = CharacterLooks.Label(look) + " ";
            while (characters.Any(value => value.label == labelPrefix + ordinal)) ordinal++;
            var character = new TopazCharacterData
            {
                id = Guid.NewGuid().ToString("N"),
                appearanceId = look,
                label = labelPrefix + ordinal
            };
            characters.Add(character);
            character.EnsureSkills();
            return character;
        }

        public TopazWorldData CreateWorld()
        {
            int ordinal = 1;
            while (worlds.Any(value => value.label == "World " + ordinal)) ordinal++;
            var world = new TopazWorldData
            {
                id = Guid.NewGuid().ToString("N"),
                label = "World " + ordinal
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

        public bool DeleteCharacter(string id)
        {
            TopazCharacterData character = Character(id);
            if (character == null) return false;
            characters.Remove(character);
            visits.RemoveAll(value => value.characterId == id);
            if (lastCharacterId == id)
            {
                lastCharacterId = null;
                lastWorldId = null;
            }
            return true;
        }

        public bool DeleteWorld(string id)
        {
            TopazWorldData world = World(id);
            if (world == null) return false;
            worlds.Remove(world);
            visits.RemoveAll(value => value.worldId == id);
            if (lastWorldId == id)
            {
                lastCharacterId = null;
                lastWorldId = null;
            }
            return true;
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
            character.MigrateSkills();
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
                    character.equipment == null ||
                    character.loggingExperience < 0 || character.swordsExperience < 0 ||
                    character.axesExperience < 0 || character.miningExperience < 0 ||
                    character.skills == null ||
                    character.discoveredSkillIds == null ||
                    character.discoveredSkillIds.Any(string.IsNullOrEmpty) ||
                    character.discoveredSkillIds.Distinct().Count() != character.discoveredSkillIds.Count ||
                    character.pickaxeId == string.Empty ||
                    (character.selectedTool != "sword" && character.selectedTool != "axe" &&
                     character.selectedTool != "pickaxe"))
                    throw new ArgumentException("Character record is invalid.");
                var skillIds = new HashSet<string>();
                foreach (SkillProgressRecord skill in character.skills)
                {
                    if (skill == null || string.IsNullOrEmpty(skill.skillId) ||
                        !skillIds.Add(skill.skillId) || skill.experienceCenti < 0 ||
                        skill.learnedTalentIds == null || skill.activeTalentIds == null ||
                        skill.activeTalentIds.Count > 2 ||
                        skill.learnedTalentIds.Any(string.IsNullOrEmpty) ||
                        skill.activeTalentIds.Any(id => !skill.learnedTalentIds.Contains(id)) ||
                        skill.learnedTalentIds.Distinct().Count() != skill.learnedTalentIds.Count ||
                        skill.activeTalentIds.Distinct().Count() != skill.activeTalentIds.Count)
                        throw new ArgumentException("Character skill progress is invalid.");
                }
                ValidateItems(character.backpackSlots);
                // Unequipping and two-handed weapons leave slots empty. JsonUtility
                // reads those fields back as "", which still means unequipped.
            }
            var worldIds = new HashSet<string>();
            foreach (TopazWorldData world in worlds)
            {
                if (world == null || string.IsNullOrEmpty(world.id) || !worldIds.Add(world.id) ||
                    string.IsNullOrEmpty(world.label) || world.nodes == null ||
                    world.structures == null || world.pickups == null ||
                    world.claimedGearIds == null ||
                    double.IsNaN(world.worldHours) || double.IsInfinity(world.worldHours) ||
                    world.worldHours < WorldClock.StartingHour)
                    throw new ArgumentException("World record is invalid.");
                if (world.claimedGearIds.Any(string.IsNullOrEmpty) ||
                    world.claimedGearIds.Distinct().Count() != world.claimedGearIds.Count)
                    throw new ArgumentException("World gear claims are invalid.");
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
                     visit.regionId != TopazSaveData.ExpeditionRegion &&
                     visit.regionId != TopazSaveData.CryptRegion) ||
                    float.IsNaN(visit.playerX) || float.IsNaN(visit.playerZ) ||
                    float.IsInfinity(visit.playerX) || float.IsInfinity(visit.playerZ) ||
                    string.IsNullOrEmpty(visit.lastCampfireId) ||
                    visit.discoveredCampfireIds == null ||
                    !visit.discoveredCampfireIds.Contains(visit.lastCampfireId) ||
                    visit.discoveredCampfireIds.Any(string.IsNullOrEmpty) ||
                    visit.discoveredCampfireIds.Distinct().Count() !=
                        visit.discoveredCampfireIds.Count)
                    throw new ArgumentException("Character visit is invalid.");
            if (!string.IsNullOrEmpty(lastCharacterId) || !string.IsNullOrEmpty(lastWorldId))
                if (Visit(lastCharacterId, lastWorldId) == null)
                    throw new ArgumentException("Last Character and World pair is invalid.");
        }

        public void MigrateFromVersion1()
        {
            if (version != 1 || visits == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazVisitData visit in visits)
            {
                if (visit == null) continue;
                visit.lastCampfireId = Campfire.HomeId;
                visit.discoveredCampfireIds = new List<string> { Campfire.HomeId };
            }
            version = 2;
            MigrateFromVersion2();
        }

        public void MigrateFromVersion2()
        {
            if (version != 2 || characters == null || worlds == null || visits == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.equipment = new EquipmentState();
            }
            foreach (TopazWorldData world in worlds)
            {
                if (world == null) throw new ArgumentException("World record is invalid.");
                world.claimedGearIds = new List<string>();
            }
            version = 3;
            MigrateFromVersion3();
        }

        public void MigrateFromVersion3()
        {
            if (version != 3 || characters == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.axesExperience = 0;
            }
            version = 4;
            MigrateFromVersion4();
        }

        public void MigrateFromVersion4()
        {
            if (version != 4 || characters == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.miningExperience = 0;
                character.selectedTool = "sword";
                character.pickaxeId = "gear.pickaxe.starter";
            }
            version = 5;
            MigrateFromVersion5();
        }

        public void MigrateFromVersion5()
        {
            if (version != 5 || characters == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.MigrateSkills();
            }
            version = 6;
            MigrateFromVersion6();
        }

        public void MigrateFromVersion6()
        {
            if (version != 6 || characters == null || worlds == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.EnsureSkills();
                character.discoveredSkillIds = new List<string>();
            }
            version = 7;
            MigrateFromVersion7();
        }

        public void MigrateFromVersion7()
        {
            if (version != 7 || characters == null || worlds == null)
                throw new ArgumentException("Character and World collection cannot be migrated.");
            foreach (TopazCharacterData character in characters)
            {
                if (character == null) throw new ArgumentException("Character record is invalid.");
                character.EnsureSkills();
            }
            version = CurrentVersion;
            Validate();
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
        public int axesExperience;
        public int miningExperience;
        public List<SkillProgressRecord> skills = new List<SkillProgressRecord>();
        public List<string> discoveredSkillIds = new List<string>();
        public string pickaxeId = "gear.pickaxe.starter";
        public string selectedTool = "sword";
        public string equippedTool = "sword";
        public EquipmentState equipment = new EquipmentState();
        public bool lanternOn; // Missing in older collections, so the lantern starts off.
        public bool pendingChest;
        public List<ItemStackRecord> backpackSlots = new List<ItemStackRecord>();

        public SkillProgressRecord Skill(string id) => skills?.Find(value => value.skillId == id);

        public void EnsureSkills()
        {
            if (skills == null) skills = new List<SkillProgressRecord>();
            foreach (string id in SkillIds.All)
                if (Skill(id) == null) skills.Add(new SkillProgressRecord { skillId = id });
        }

        public void MigrateSkills()
        {
            EnsureSkills();
            Skill(SkillIds.Swords).experienceCenti = SkillProgression.FromLegacy(swordsExperience);
            Skill(SkillIds.Axes).experienceCenti = SkillProgression.FromLegacy(axesExperience);
            Skill(SkillIds.Logging).experienceCenti = SkillProgression.FromLegacy(loggingExperience);
            Skill(SkillIds.Mining).experienceCenti = SkillProgression.FromLegacy(miningExperience);
        }
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
        public List<string> claimedGearIds = new List<string>();
        public bool cryptShortcutOpen;
        public bool cryptCacheClaimed;
        public bool cryptMageDefeated;
        public bool cryptRogueCrossbowAwarded;
    }

    [Serializable]
    public sealed class TopazVisitData
    {
        public string characterId;
        public string worldId;
        public string regionId = TopazSaveData.HomeRegion;
        public float playerX;
        public float playerZ;
        public string lastCampfireId = Campfire.HomeId;
        public List<string> discoveredCampfireIds = new List<string> { Campfire.HomeId };
    }
}
