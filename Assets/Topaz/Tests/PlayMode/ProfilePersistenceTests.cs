using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Topaz.Tests
{
    public sealed class ProfilePersistenceTests
    {
        static readonly Type ProfileType = Type.GetType(
            "Topaz.LoopStudy.TopazProfileData, Assembly-CSharp", true);
        static readonly Type RepositoryType = Type.GetType(
            "Topaz.LoopStudy.ProfileRepository, Assembly-CSharp", true);
        static readonly Type StackType = Type.GetType(
            "Topaz.LoopStudy.ItemStackRecord, Assembly-CSharp", true);
        static readonly Type StructureType = Type.GetType(
            "Topaz.LoopStudy.StructureStateRecord, Assembly-CSharp", true);

        [Test]
        public void FreshCollectionDoesNotWriteBeforeACharacterEntersAWorld()
        {
            WithDirectory(directory =>
            {
                object repository = Activator.CreateInstance(RepositoryType, directory);
                object profile = Invoke(repository, "Load");
                Assert.That(Slots(profile, "characters").Count, Is.Zero);
                Assert.That(Slots(profile, "worlds").Count, Is.Zero);
                Assert.That(File.Exists(Path.Combine(directory, "topaz-collection.json")), Is.False);
            });
        }

        [Test]
        public void DeletingCharacterRemovesOnlyTheirVisitsAndPersistsTheCollection()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object knight = Invoke(profile, "CreateCharacter", "knight");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(knight), Id(world));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(world));

                Assert.That(Invoke(profile, "DeleteCharacter", Id(rogue)), Is.EqualTo(true));
                Assert.That(Invoke(profile, "DeleteCharacter", Id(rogue)), Is.EqualTo(false));
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                object loaded = Invoke(repository, "Load");
                Assert.That(Slots(loaded, "characters").Count, Is.EqualTo(1));
                Assert.That(Slots(loaded, "worlds").Count, Is.EqualTo(1));
                Assert.That(Slots(loaded, "visits").Count, Is.EqualTo(1));
                Assert.That(Invoke(loaded, "Visit", Id(knight), Id(world)), Is.Not.Null);
                Assert.That(string.IsNullOrEmpty((string)Get(loaded, "lastCharacterId")), Is.True);
                Assert.That(string.IsNullOrEmpty((string)Get(loaded, "lastWorldId")), Is.True);
                Assert.That(Get(Invoke(loaded, "CreateCharacter", "knight"), "label"),
                    Is.EqualTo("Knight 2"));
            });
        }

        [Test]
        public void DeletingWorldRemovesEveryVisitThereAndPreservesOtherWorlds()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object first = Invoke(profile, "CreateWorld");
                object second = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(first));
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(second));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(first));

                Assert.That(Invoke(profile, "DeleteWorld", Id(first)), Is.EqualTo(true));
                Assert.That(Invoke(profile, "DeleteWorld", Id(first)), Is.EqualTo(false));
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                object loaded = Invoke(repository, "Load");
                Assert.That(Slots(loaded, "characters").Count, Is.EqualTo(1));
                Assert.That(Slots(loaded, "worlds").Count, Is.EqualTo(1));
                Assert.That(Slots(loaded, "visits").Count, Is.EqualTo(1));
                Assert.That(Invoke(loaded, "Visit", Id(rogue), Id(second)), Is.Not.Null);
                Assert.That(string.IsNullOrEmpty((string)Get(loaded, "lastCharacterId")), Is.True);
                Assert.That(string.IsNullOrEmpty((string)Get(loaded, "lastWorldId")), Is.True);
                Assert.That(Get(Invoke(loaded, "CreateWorld"), "label"),
                    Is.EqualTo("World 1"));
            });
        }

        [Test]
        public void CharacterInventoryAndWorldStorageRemainIndependentAcrossVisits()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object knight = Invoke(profile, "CreateCharacter", "knight");
                object firstWorld = Invoke(profile, "CreateWorld");
                object secondWorld = Invoke(profile, "CreateWorld");
                object firstVisit = Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(firstWorld));
                object secondVisit = Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(secondWorld));
                object knightVisit = Invoke(profile, "GetOrCreateVisit", Id(knight), Id(firstWorld));
                Set(firstVisit, "playerX", 4f);
                Set(secondVisit, "playerX", 9f);
                Set(knightVisit, "playerX", -3f);
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(firstWorld));

                Slots(rogue, "backpackSlots").Add(Stack("material.wood", 2));
                Set(Get(rogue, "equipment"), "handsId", "gear.gloves.swift");
                Slots(firstWorld, "claimedGearIds").Add("gear.gloves.swift");
                object chest = Activator.CreateInstance(StructureType);
                Set(chest, "instanceId", "chest-1");
                Set(chest, "definitionId", "structure.storage_chest");
                Slots(chest, "slots").Add(Stack("material.wood", 3));
                Slots(firstWorld, "structures").Add(chest);

                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                object loaded = Invoke(repository, "Load");
                IList characters = Slots(loaded, "characters");
                IList worlds = Slots(loaded, "worlds");
                Assert.That(characters.Count, Is.EqualTo(2));
                Assert.That(worlds.Count, Is.EqualTo(2));
                Assert.That(Slots(characters[0], "backpackSlots").Count, Is.EqualTo(1));
                Assert.That(Slots(characters[1], "backpackSlots").Count, Is.Zero);
                Assert.That(Slots(worlds[0], "structures").Count, Is.EqualTo(1));
                Assert.That(Slots(worlds[1], "structures").Count, Is.Zero);
                Assert.That((string)Get(Get(characters[0], "equipment"), "handsId"),
                    Is.EqualTo("gear.gloves.swift"),
                    "Equipment travels with the Character into another World.");
                Assert.That((string)Get(Get(characters[1], "equipment"), "handsId"),
                    Is.EqualTo("gear.gloves.starter"));
                Assert.That(Slots(worlds[0], "claimedGearIds").Count, Is.EqualTo(1));
                Assert.That(Slots(worlds[1], "claimedGearIds").Count, Is.Zero);
                object loadedFirstVisit = Invoke(loaded, "Visit", Id(rogue), Id(firstWorld));
                object loadedSecondVisit = Invoke(loaded, "Visit", Id(rogue), Id(secondWorld));
                Assert.That((float)Get(loadedFirstVisit, "playerX"), Is.EqualTo(4f));
                Assert.That((float)Get(loadedSecondVisit, "playerX"), Is.EqualTo(9f));
                Assert.That((float)Get(Invoke(loaded, "Visit", Id(knight), Id(firstWorld)),
                    "playerX"), Is.EqualTo(-3f));
            });
        }

        [Test]
        public void CampfireDiscoveryAndReturnPointBelongToOneVisit()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object knight = Invoke(profile, "CreateCharacter", "knight");
                object world = Invoke(profile, "CreateWorld");
                object rogueVisit = Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(knight), Id(world));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(world));
                Slots(rogueVisit, "discoveredCampfireIds").Add("campfire.expedition.clearing");
                Set(rogueVisit, "lastCampfireId", "campfire.expedition.clearing");

                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                object loaded = Invoke(repository, "Load");
                object loadedRogue = Invoke(loaded, "Visit", Id(rogue), Id(world));
                object loadedKnight = Invoke(loaded, "Visit", Id(knight), Id(world));
                Assert.That((string)Get(loadedRogue, "lastCampfireId"),
                    Is.EqualTo("campfire.expedition.clearing"));
                Assert.That(Slots(loadedRogue, "discoveredCampfireIds").Count, Is.EqualTo(2));
                Assert.That((string)Get(loadedKnight, "lastCampfireId"),
                    Is.EqualTo("campfire.home"));
                Assert.That(Slots(loadedKnight, "discoveredCampfireIds").Count, Is.EqualTo(1));
            });
        }

        [Test]
        public void VersionOneCollectionMigratesToTheHomeCampfire()
        {
            WithDirectory(directory =>
            {
                const string old = "{\"version\":1,\"characters\":[{\"id\":\"rogue\"," +
                    "\"label\":\"Rogue 1\",\"appearanceId\":\"rogue\"," +
                    "\"backpackSlots\":[]}],\"worlds\":[{\"id\":\"world\"," +
                    "\"label\":\"World 1\",\"worldHours\":8,\"nodes\":[]," +
                    "\"structures\":[],\"pickups\":[]}],\"visits\":[{" +
                    "\"characterId\":\"rogue\",\"worldId\":\"world\"," +
                    "\"regionId\":\"expedition.clearing\",\"playerX\":100," +
                    "\"playerZ\":-10}],\"lastCharacterId\":\"rogue\"," +
                    "\"lastWorldId\":\"world\"}";
                File.WriteAllText(Path.Combine(directory, "topaz-collection.json"), old);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                object loaded = Invoke(repository, "Load");
                object visit = Invoke(loaded, "Visit", "rogue", "world");
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That((string)Get(visit, "lastCampfireId"), Is.EqualTo("campfire.home"));
                Assert.That(Slots(visit, "discoveredCampfireIds").Count, Is.EqualTo(1));
                Assert.That((float)Get(visit, "playerX"), Is.EqualTo(0f));
                Assert.That((float)Get(visit, "playerZ"), Is.EqualTo(-28f),
                    "Migration must retain the saved point in the moved Graveyard.");
                Invoke(repository, "Save", loaded);
                Assert.That((int)Get(Invoke(repository, "Load"), "version"), Is.EqualTo(12));
            });
        }

        [Test]
        public void LanternStateBelongsToTheCharacterAndOlderCollectionsStartUnlit()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object knight = Invoke(profile, "CreateCharacter", "knight");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(knight), Id(world));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(world));
                Set(rogue, "lanternOn", true);

                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                IList characters = Slots(Invoke(repository, "Load"), "characters");
                Assert.That((bool)Get(characters[0], "lanternOn"), Is.True);
                Assert.That((bool)Get(characters[1], "lanternOn"), Is.False);
                Assert.That(Slots(characters[0], "backpackSlots").Count, Is.Zero,
                    "The permanent lantern must not use a transferable stack slot.");

                string path = Path.Combine(directory, "topaz-collection.json");
                string oldJson = File.ReadAllText(path).Replace("\"lanternOn\": true,", "");
                File.WriteAllText(path, oldJson);
                object oldCollection = Invoke(repository, "Load");
                Assert.That((bool)Get(Slots(oldCollection, "characters")[0], "lanternOn"),
                    Is.False, "Collections written before the lantern existed start unlit.");
            });
        }

        [Test]
        public void EmptyEquipmentSlotsRoundTripAfterTwoHandedEquipAndUnequip()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object staffUser = Invoke(profile, "CreateCharacter", "rogue");
                object unarmed = Invoke(profile, "CreateCharacter", "knight");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(staffUser), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(unarmed), Id(world));
                Set(profile, "lastCharacterId", Id(staffUser));
                Set(profile, "lastWorldId", Id(world));

                object staffEquipment = Get(staffUser, "equipment");
                Set(staffEquipment, "weaponId", "gear.crypt.staff");
                Set(staffEquipment, "offhandId", null);
                Slots(staffUser, "backpackSlots").Add(Stack("gear.shield.starter", 1));
                object emptyEquipment = Get(unarmed, "equipment");
                foreach (string field in new[] { "weaponId", "toolId", "offhandId",
                    "headId", "bodyId", "handsId", "bootsId" })
                    Set(emptyEquipment, field, null);

                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string saved = File.ReadAllText(Path.Combine(directory, "topaz-collection.json"));
                Assert.That(saved, Does.Contain("\"offhandId\": \"\""),
                    "Unity serializes an unequipped string field as empty.");
                object loaded = Invoke(repository, "Load");
                object restoredStaffUser = Slots(loaded, "characters")[0];
                Assert.That((string)Get(Get(restoredStaffUser, "equipment"), "weaponId"),
                    Is.EqualTo("gear.crypt.staff"));
                Assert.That(string.IsNullOrEmpty((string)Get(Get(restoredStaffUser,
                    "equipment"), "offhandId")), Is.True);
                Assert.That((string)Get(Slots(restoredStaffUser, "backpackSlots")[0], "itemId"),
                    Is.EqualTo("gear.shield.starter"));
                object restoredEmpty = Get(Slots(loaded, "characters")[1], "equipment");
                foreach (string field in new[] { "weaponId", "toolId", "offhandId",
                    "headId", "bodyId", "handsId", "bootsId" })
                    Assert.That(string.IsNullOrEmpty((string)Get(restoredEmpty, field)), Is.True);
                Invoke(repository, "Save", loaded);
                Assert.That(Invoke(repository, "Load"), Is.Not.Null);
            });
        }

        [Test]
        public void LegacyProgressImportsOnceAndOriginalRemainsUntouched()
        {
            WithDirectory(directory =>
            {
                const string old = "{\"version\":4,\"day\":2,\"loggingExperience\":7," +
                    "\"swordsExperience\":3,\"equippedTool\":\"axe\",\"playerX\":4," +
                    "\"playerZ\":5,\"regionId\":\"home\",\"backpackSlots\":[{" +
                    "\"itemId\":\"material.wood\",\"count\":2}],\"nodes\":[]," +
                    "\"structures\":[],\"pickups\":[]}";
                string legacyPath = Path.Combine(directory, "topaz-save.json");
                File.WriteAllText(legacyPath, old);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                object first = Invoke(repository, "Load");
                object second = Invoke(repository, "Load");
                Assert.That(Slots(first, "characters").Count, Is.EqualTo(1));
                Assert.That(Slots(second, "characters").Count, Is.EqualTo(1));
                Assert.That(Slots(second, "worlds").Count, Is.EqualTo(1));
                object character = Slots(second, "characters")[0];
                object world = Slots(second, "worlds")[0];
                Assert.That((int)Get(character, "loggingExperience"), Is.EqualTo(7));
                Assert.That((string)Get(character, "equippedTool"), Is.EqualTo("axe"));
                Assert.That((float)Get(Invoke(second, "Visit", Id(character), Id(world)),
                    "playerX"), Is.EqualTo(4f));
                Assert.That(File.ReadAllText(legacyPath), Is.EqualTo(old));
            });
        }

        [Test]
        public void VersionTwoCollectionAddsStarterGearWithoutChangingProgress()
        {
            WithDirectory(directory =>
            {
                const string old = "{\"version\":2,\"characters\":[{\"id\":\"rogue\"," +
                    "\"label\":\"Rogue 1\",\"appearanceId\":\"rogue\"," +
                    "\"loggingExperience\":7,\"backpackSlots\":[]}]," +
                    "\"worlds\":[{\"id\":\"world\",\"label\":\"World 1\"," +
                    "\"worldHours\":8,\"nodes\":[],\"structures\":[],\"pickups\":[]}]," +
                    "\"visits\":[{\"characterId\":\"rogue\",\"worldId\":\"world\"," +
                    "\"regionId\":\"home\",\"lastCampfireId\":\"campfire.home\"," +
                    "\"discoveredCampfireIds\":[\"campfire.home\"]}]," +
                    "\"lastCharacterId\":\"rogue\",\"lastWorldId\":\"world\"}";
                File.WriteAllText(Path.Combine(directory, "topaz-collection.json"), old);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                object loaded = Invoke(repository, "Load");
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                object character = Slots(loaded, "characters")[0];
                Assert.That((int)Get(character, "loggingExperience"), Is.EqualTo(7));
                Assert.That((string)Get(Get(character, "equipment"), "offhandId"),
                    Is.EqualTo("gear.shield.starter"));
                Assert.That(Slots(Slots(loaded, "worlds")[0], "claimedGearIds").Count, Is.Zero);
            });
        }

        [Test]
        public void VersionThreeCollectionAddsAxesWithoutChangingSwordsOrEquipment()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                Set(character, "swordsExperience", 7);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                string old = File.ReadAllText(path).Replace("\"version\": 12", "\"version\": 3")
                    .Replace("\"axesExperience\": 0,", "");
                File.WriteAllText(path, old);

                object loaded = Invoke(repository, "Load");
                object restored = Slots(loaded, "characters")[0];
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That((int)Get(restored, "axesExperience"), Is.Zero);
                Assert.That((int)Get(restored, "swordsExperience"), Is.EqualTo(7));
                Assert.That((string)Get(Get(restored, "equipment"), "weaponId"),
                    Is.EqualTo("gear.sword.starter"));
            });
        }

        [Test]
        public void VersionFourCollectionAddsMiningWithoutChangingExistingProgress()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                Set(character, "loggingExperience", 7);
                Set(character, "axesExperience", 5);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                string old = File.ReadAllText(path).Replace("\"version\": 12", "\"version\": 4")
                    .Replace("\"miningExperience\": 0,", "")
                    .Replace("\"selectedTool\": \"sword\",", "");
                File.WriteAllText(path, old);
                object loaded = Invoke(repository, "Load");
                object restored = Slots(loaded, "characters")[0];
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That((int)Get(restored, "miningExperience"), Is.Zero);
                Assert.That((string)Get(restored, "selectedTool"), Is.EqualTo("sword"));
                Assert.That((string)Get(restored, "pickaxeId"),
                    Is.EqualTo("gear.pickaxe.starter"));
                Assert.That((int)Get(restored, "loggingExperience"), Is.EqualTo(7));
                Assert.That((int)Get(restored, "axesExperience"), Is.EqualTo(5));
            });
        }

        [Test]
        public void CorruptCollectionCannotBeReplacedByLegacyImport()
        {
            WithDirectory(directory =>
            {
                string current = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(current, "{broken");
                File.WriteAllText(Path.Combine(directory, "topaz-save.json"), "{\"version\":5}");
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Assert.Throws<TargetInvocationException>(() => Invoke(repository, "Load"));
                Assert.That(File.ReadAllText(current), Is.EqualTo("{broken"));
            });
        }

        [Test]
        public void VersionFiveProgressMigratesWithoutLosingLevelOrOverflow()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                Set(character, "loggingExperience", 15);
                Set(character, "swordsExperience", 120);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace("\"version\": 12", "\"version\": 5"));

                object loaded = Invoke(repository, "Load");
                object restored = Slots(loaded, "characters")[0];
                object logging = Invoke(restored, "Skill", "logging");
                object swords = Invoke(restored, "Skill", "swords");
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That((int)Get(logging, "experienceCenti"), Is.EqualTo(1600));
                Assert.That((int)logging.GetType().GetProperty("Level").GetValue(logging),
                    Is.EqualTo(2));
                Assert.That((int)Get(swords, "experienceCenti"), Is.GreaterThan(16200));
                Assert.That((int)swords.GetType().GetProperty("Level").GetValue(swords),
                    Is.EqualTo(10));
            });
        }

        [Test]
        public void VersionSixAddsStaffProgressWithoutChangingExistingTalents()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                object swords = Invoke(character, "Skill", "swords");
                Set(swords, "experienceCenti", 2200);
                Slots(swords, "learnedTalentIds").Add("swords.footwork");
                Slots(swords, "activeTalentIds").Add("swords.footwork");
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace("\"version\": 12", "\"version\": 6"));

                object loaded = Invoke(repository, "Load");
                object restored = Slots(loaded, "characters")[0];
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That(Invoke(restored, "Skill", "staff"), Is.Not.Null);
                Assert.That((int)Get(Invoke(restored, "Skill", "swords"), "experienceCenti"),
                    Is.EqualTo(2200));
                Assert.That(Slots(Invoke(restored, "Skill", "swords"),
                    "activeTalentIds").Count, Is.EqualTo(1));
                Assert.That(Slots(restored, "discoveredSkillIds").Count, Is.Zero);
            });
        }

        [Test]
        public void VersionSevenAddsCrossbowsWithoutChangingCharacterOrWorldProgress()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                Slots(character, "skills").Remove(Invoke(character, "Skill", "crossbows"));
                Slots(character, "discoveredSkillIds").Add("staff");
                Set(Invoke(character, "Skill", "swords"), "experienceCenti", 2200);
                Set(world, "cryptMageDefeated", true);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace("\"version\": 12", "\"version\": 7"));

                object loaded = Invoke(repository, "Load");
                object restored = Slots(loaded, "characters")[0];
                object restoredWorld = Slots(loaded, "worlds")[0];
                Assert.That((int)Get(loaded, "version"), Is.EqualTo(12));
                Assert.That(Get(Invoke(restored, "Skill", "crossbows"),
                    "experienceCenti"), Is.EqualTo(0));
                Assert.That(Get(Invoke(restored, "Skill", "swords"),
                    "experienceCenti"), Is.EqualTo(2200));
                Assert.That(Slots(restored, "discoveredSkillIds"), Does.Contain("staff"));
                Assert.That(Get(restoredWorld, "cryptMageDefeated"), Is.True);
                Assert.That(Get(restoredWorld, "cryptRogueCrossbowAwarded"), Is.False);
            });
        }

        [Test]
        public void OneWorldCrossbowClaimDoesNotMoveCharacterOwnedGear()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "rogue");
                object knight = Invoke(profile, "CreateCharacter", "knight");
                object world = Invoke(profile, "CreateWorld");
                object secondWorld = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(knight), Id(world));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(world));
                Set(world, "cryptRogueCrossbowAwarded", true);
                Slots(rogue, "backpackSlots").Add(Stack("gear.crypt.crossbow", 1));
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);

                object loaded = Invoke(repository, "Load");
                object savedRogue = Invoke(loaded, "Character", Id(rogue));
                object savedKnight = Invoke(loaded, "Character", Id(knight));
                Assert.That(Get(Invoke(loaded, "World", Id(world)),
                    "cryptRogueCrossbowAwarded"), Is.True);
                Assert.That(Get(Invoke(loaded, "World", Id(secondWorld)),
                    "cryptRogueCrossbowAwarded"), Is.False);
                Assert.That(Get(Slots(savedRogue, "backpackSlots")[0], "itemId"),
                    Is.EqualTo("gear.crypt.crossbow"));
                Assert.That(Slots(savedKnight, "backpackSlots").Count, Is.Zero);
            });
        }

        [Test]
        public void VersionEightClearingSaveMovesIntoGraveyardWithoutLosingGroundLoot()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                object visit = Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                Set(visit, "regionId", "expedition.clearing");
                Set(visit, "playerX", 104f);
                Set(visit, "playerZ", -10f);
                Set(world, "expeditionCacheClaimed", true);
                Set(world, "cryptCacheClaimed", true);
                Type pickupType = Type.GetType(
                    "Topaz.LoopStudy.PickupStateRecord, Assembly-CSharp", true);
                object pickup = Activator.CreateInstance(pickupType);
                Set(pickup, "instanceId", "old-drop");
                Set(pickup, "itemId", "material.wood");
                Set(pickup, "regionId", "expedition.clearing");
                Set(pickup, "count", 1);
                Set(pickup, "x", 103f);
                Set(pickup, "z", 4f);
                Slots(world, "pickups").Add(pickup);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace("\"version\": 12", "\"version\": 8"));

                object migrated = Invoke(repository, "Load");
                object savedVisit = Slots(migrated, "visits")[0];
                object savedWorld = Slots(migrated, "worlds")[0];
                object savedPickup = Slots(savedWorld, "pickups")[0];
                Assert.That(Get(savedVisit, "playerX"), Is.EqualTo(-4f));
                Assert.That(Get(savedVisit, "playerZ"), Is.EqualTo(-28f));
                Assert.That(Get(savedPickup, "x"), Is.EqualTo(-3f));
                Assert.That(Get(savedPickup, "z"), Is.EqualTo(-42f));
                Assert.That(Get(savedWorld, "graveyardCacheReadyAt"), Is.EqualTo(0d));
                Assert.That(Get(savedWorld, "cryptCacheReadyAt"), Is.EqualTo(0d));
            });
        }

        [Test]
        public void VersionTenGraveyardLocationsMoveWithTheAuthoredScene()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                object visit = Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(visit, "regionId", "expedition.clearing");
                Set(visit, "playerX", 2f);
                Set(visit, "playerZ", -9f);
                Type pickupType = Type.GetType(
                    "Topaz.LoopStudy.PickupStateRecord, Assembly-CSharp", true);
                object pickup = Activator.CreateInstance(pickupType);
                Set(pickup, "instanceId", "edge-drop");
                Set(pickup, "itemId", "material.wood");
                Set(pickup, "regionId", "expedition.clearing");
                Set(pickup, "count", 1);
                Set(pickup, "x", 3f);
                Set(pickup, "z", -30f);
                Slots(world, "pickups").Add(pickup);
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                string path = Path.Combine(directory, "topaz-collection.json");
                File.WriteAllText(path, File.ReadAllText(path)
                    .Replace("\"version\": 12", "\"version\": 10"));

                object migrated = Invoke(repository, "Load");
                object savedVisit = Slots(migrated, "visits")[0];
                object savedPickup = Slots(Slots(migrated, "worlds")[0], "pickups")[0];
                Assert.That(Get(savedVisit, "playerX"), Is.EqualTo(2f));
                Assert.That(Get(savedVisit, "playerZ"), Is.EqualTo(-19f));
                Assert.That(Get(savedPickup, "x"), Is.EqualTo(3f));
                Assert.That(Get(savedPickup, "z"), Is.EqualTo(-40f));
                Invoke(repository, "Save", migrated);
                Assert.That(Get(Slots(Invoke(repository, "Load"), "visits")[0], "playerZ"),
                    Is.EqualTo(-19f), "Saving the migration must not shift it twice.");
            });
        }

        [Test]
        public void BackupRestoresLastCompleteCollection()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "rogue");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(character), Id(world));
                Set(profile, "lastCharacterId", Id(character));
                Set(profile, "lastWorldId", Id(world));
                object repository = Activator.CreateInstance(RepositoryType, directory);
                Set(character, "loggingExperience", 3);
                Invoke(repository, "Save", profile);
                Set(character, "loggingExperience", 9);
                Invoke(repository, "Save", profile);
                File.WriteAllText(Path.Combine(directory, "topaz-collection.json"), "corrupt");
                object loaded = Invoke(repository, "Load");
                Assert.That((int)Get(Slots(loaded, "characters")[0], "loggingExperience"),
                    Is.EqualTo(3));
                Assert.That(Directory.GetFiles(directory, "topaz-collection.json.corrupt-*").Length,
                    Is.EqualTo(1), "The unreadable primary must be preserved for recovery.");
                Assert.That((int)Get(Slots(Invoke(repository, "Load"), "characters")[0],
                    "loggingExperience"), Is.EqualTo(3));
            });
        }

        static object Stack(string id, int count)
        {
            object stack = Activator.CreateInstance(StackType);
            Set(stack, "itemId", id);
            Set(stack, "count", count);
            return stack;
        }

        static string Id(object record) => (string)Get(record, "id");
        static IList Slots(object record, string name) => (IList)Get(record, name);
        static object Get(object record, string name) => record.GetType().GetField(name).GetValue(record);
        static void Set(object record, string name, object value) =>
            record.GetType().GetField(name).SetValue(record, value);
        static object Invoke(object record, string name, params object[] arguments) =>
            record.GetType().GetMethod(name).Invoke(record, arguments);

        static void WithDirectory(Action<string> action)
        {
            string directory = Path.Combine(Path.GetTempPath(),
                "TopazProfileTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try { action(directory); }
            finally { Directory.Delete(directory, true); }
        }
    }
}
