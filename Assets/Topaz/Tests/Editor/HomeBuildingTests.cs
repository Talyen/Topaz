using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Topaz.Tests
{
    public sealed class HomeBuildingTests
    {
        static Type Runtime(string name) => Type.GetType("Topaz.Gameplay." + name +
            ", Assembly-CSharp", true);
        static object New(string name) => Activator.CreateInstance(Runtime(name));
        static object Field(object value, string name) => value.GetType()
            .GetField(name).GetValue(value);
        static void Set(object value, string name, object data) => value.GetType()
            .GetField(name).SetValue(value, data);
        static object Call(object value, string name, params object[] args) => value.GetType()
            .GetMethod(name).Invoke(value, args);

        [Test]
        public void TwoChestRecordsKeepSeparateContentsAndUnknownIds()
        {
            object first = New("StructureStateRecord");
            object second = New("StructureStateRecord");
            Type inventoryType = Runtime("InventorySlots");
            object firstSlots = Activator.CreateInstance(inventoryType, Field(first, "slots"), 12);
            object secondSlots = Activator.CreateInstance(inventoryType, Field(second, "slots"), 12);
            Type itemType = Runtime("ItemDefinition");
            UnityEngine.Object wood = AssetDatabase.LoadAssetAtPath(
                "Assets/Topaz/Gameplay/Inventory/Definitions/Wood.asset", itemType);
            Assert.That(Call(firstSlots, "Add", wood, 9), Is.EqualTo(9));
            Assert.That(Call(secondSlots, "Add", wood, 3), Is.EqualTo(3));
            IList rawSecond = (IList)Field(second, "slots");
            Set(rawSecond[5], "itemId", "future.item");
            Set(rawSecond[5], "count", 1);
            string woodId = (string)itemType.GetProperty("StableId").GetValue(wood);
            Assert.That(Call(firstSlots, "Count", woodId), Is.EqualTo(9));
            Assert.That(Call(secondSlots, "Count", woodId), Is.EqualTo(3));
            Assert.That(Field(rawSecond[5], "itemId"), Is.EqualTo("future.item"));
        }

        [Test]
        public void ForgeAssetsHaveStableIdsAndStrongerStats()
        {
            Type itemType = Runtime("ItemDefinition");
            string folder = "Assets/Topaz/Gameplay/Progression/Definitions/Equipment/";
            foreach (var expected in new[]
            {
                (file: "gear.sword.forged.asset", id: "gear.sword.forged", stat: "Attack"),
                (file: "gear.helm.forged.asset", id: "gear.helm.forged", stat: "Armor"),
                (file: "gear.axe.forged.asset", id: "gear.axe.forged", stat: "Logging")
            })
            {
                UnityEngine.Object item = AssetDatabase.LoadAssetAtPath(folder + expected.file,
                    itemType);
                Assert.That(item, Is.Not.Null);
                Assert.That(itemType.GetProperty("StableId").GetValue(item),
                    Is.EqualTo(expected.id));
                object stats = itemType.GetProperty("Stats").GetValue(item);
                Assert.That(stats.GetType().GetField(expected.stat).GetValue(stats),
                    Is.EqualTo(2));
            }
        }
    }
}
