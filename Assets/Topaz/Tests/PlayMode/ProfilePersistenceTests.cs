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
                object loadedFirstVisit = Invoke(loaded, "Visit", Id(rogue), Id(firstWorld));
                object loadedSecondVisit = Invoke(loaded, "Visit", Id(rogue), Id(secondWorld));
                Assert.That((float)Get(loadedFirstVisit, "playerX"), Is.EqualTo(4f));
                Assert.That((float)Get(loadedSecondVisit, "playerX"), Is.EqualTo(9f));
                Assert.That((float)Get(Invoke(loaded, "Visit", Id(knight), Id(firstWorld)),
                    "playerX"), Is.EqualTo(-3f));
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
