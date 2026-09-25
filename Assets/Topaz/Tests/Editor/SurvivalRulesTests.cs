using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Topaz.Tests
{
    public sealed class SurvivalRulesTests
    {
        static Type Find(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("Topaz.LoopStudy." + name))
            .First(type => type != null);

        static object Call(Type type, string name, params object[] args) =>
            type.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, args);

        static object Field(object instance, string name) =>
            instance.GetType().GetField(name).GetValue(instance);

        static void Set(object instance, string name, object value) =>
            instance.GetType().GetField(name).SetValue(instance, value);

        [Test]
        public void EmptyStaminaNeverBlocksAnActionAndFoodCannotBeWasted()
        {
            Type rules = Find("SurvivalRules");
            object character = Activator.CreateInstance(Find("TopazCharacterData"));
            Set(character, "stamina", 20f);
            Assert.That(Call(rules, "Spend", character, 25f), Is.False);
            Assert.That(Field(character, "stamina"), Is.EqualTo(20f));
            Assert.That(Call(rules, "Eat", character, 1), Is.True);
            Assert.That(Call(rules, "Eat", character, 1), Is.False);
            Assert.That(Call(rules, "Eat", character, 2), Is.True);
            Assert.That(Call(rules, "Eat", character, 1), Is.False);
            Assert.That(Field(character, "foodHours"), Is.EqualTo(24d));
            Assert.That(Call(rules, "Regeneration", character), Is.EqualTo(30f));
        }

        [Test]
        public void EightHourSkipExpiresShortFoodAndRestStartsAfterSkip()
        {
            Type rules = Find("SurvivalRules");
            object character = Activator.CreateInstance(Find("TopazCharacterData"));
            Call(rules, "Eat", character, 1);
            Call(rules, "Advance", character, 8d);
            Assert.That(Field(character, "foodHours"), Is.EqualTo(4d));
            Call(rules, "Rest", character);
            Assert.That(Field(character, "restedHours"), Is.EqualTo(24d));
            Assert.That(Field(character, "stamina"), Is.EqualTo(125f));
            Call(rules, "Advance", character, 4d);
            Assert.That(Field(character, "foodTier"), Is.Zero);
            Call(rules, "Advance", character, 20d);
            Assert.That((float)Field(character, "stamina"), Is.LessThanOrEqualTo(100f));
        }

        [Test]
        public void VersionElevenMigrationGivesExistingCharactersFullStamina()
        {
            Type profileType = Find("TopazProfileData");
            object profile = Activator.CreateInstance(profileType);
            object character = profileType.GetMethod("CreateCharacter")
                .Invoke(profile, new object[] { "rogue" });
            object world = profileType.GetMethod("CreateWorld").Invoke(profile, null);
            profileType.GetMethod("GetOrCreateVisit").Invoke(profile,
                new[] { Field(character, "id"), Field(world, "id") });
            Set(profile, "version", 11);
            Set(character, "stamina", 0f);
            profileType.GetMethod("MigrateFromVersion11").Invoke(profile, null);
            Assert.That(Field(profile, "version"), Is.EqualTo(12));
            Assert.That(Field(character, "stamina"), Is.EqualTo(100f));
        }
    }
}
