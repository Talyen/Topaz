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
            "Topaz.Gameplay.TopazProfileData, Assembly-CSharp", true);
        static readonly Type RepositoryType = Type.GetType(
            "Topaz.Gameplay.ProfileRepository, Assembly-CSharp", true);
        static readonly Type StackType = Type.GetType(
            "Topaz.Gameplay.ItemStackRecord, Assembly-CSharp", true);
        static readonly Type StructureType = Type.GetType(
            "Topaz.Gameplay.StructureStateRecord, Assembly-CSharp", true);

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
                object rogue = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
                object knight = Invoke(profile, "CreateCharacter", "viking.warrior.male.1");
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
                Assert.That(Get(Invoke(loaded, "CreateCharacter", "viking.warrior.male.1"), "label"),
                    Is.EqualTo("Warrior I (Male) 2"));
            });
        }

        [Test]
        public void DeletingWorldRemovesEveryVisitThereAndPreservesOtherWorlds()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
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
                object rogue = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
                object knight = Invoke(profile, "CreateCharacter", "viking.warrior.male.1");
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
                Assert.That(Slots(characters[0], "backpackSlots").Count, Is.EqualTo(3));
                Assert.That(Slots(characters[1], "backpackSlots").Count, Is.EqualTo(2));
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
                object rogue = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
                object knight = Invoke(profile, "CreateCharacter", "viking.warrior.male.1");
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
        public void LanternStartsOnAndPersistsEachCharactersExplicitChoice()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object rogue = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
                object knight = Invoke(profile, "CreateCharacter", "viking.warrior.male.1");
                object world = Invoke(profile, "CreateWorld");
                Invoke(profile, "GetOrCreateVisit", Id(rogue), Id(world));
                Invoke(profile, "GetOrCreateVisit", Id(knight), Id(world));
                Set(profile, "lastCharacterId", Id(rogue));
                Set(profile, "lastWorldId", Id(world));
                Assert.That((bool)Get(rogue,"lanternOn"),Is.True,"New characters carry an enabled lantern.");
                Set(knight, "lanternOn", false);

                object repository = Activator.CreateInstance(RepositoryType, directory);
                Invoke(repository, "Save", profile);
                IList characters = Slots(Invoke(repository, "Load"), "characters");
                Assert.That((bool)Get(characters[0], "lanternOn"), Is.True);
                Assert.That((bool)Get(characters[1], "lanternOn"), Is.False);
                Assert.That(Slots(characters[0], "backpackSlots").Count, Is.EqualTo(2),
                    "The permanent lantern must not use a transferable stack slot.");

                string path = Path.Combine(directory, "topaz-collection.json");
                string oldJson = File.ReadAllText(path).Replace("\"lanternOn\": true,", "");
                File.WriteAllText(path, oldJson);
                object oldCollection = Invoke(repository, "Load");
                Assert.That((bool)Get(Slots(oldCollection, "characters")[0], "lanternOn"),
                    Is.True, "Missing lantern fields use the enabled default; an explicit off remains off.");
            });
        }

        [Test]
        public void EmptyEquipmentSlotsRoundTripAfterTwoHandedEquipAndUnequip()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object staffUser = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
                object unarmed = Invoke(profile, "CreateCharacter", "viking.warrior.male.1");
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
                Assert.That((string)Get(Slots(restoredStaffUser, "backpackSlots")[2], "itemId"),
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
        public void BackupRestoresLastCompleteCollection()
        {
            WithDirectory(directory =>
            {
                object profile = Activator.CreateInstance(ProfileType);
                object character = Invoke(profile, "CreateCharacter", "viking.villager.male.1");
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
