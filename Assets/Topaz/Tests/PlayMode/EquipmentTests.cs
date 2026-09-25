using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Topaz.Tests
{
    public sealed class EquipmentTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator StarterAndHomeSidegradeStayWithCharacterAndClaimOncePerWorld()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Assert.That(Stat(session, "Attack"), Is.EqualTo(5));
            Assert.That(Stat(session, "Armor"), Is.EqualTo(2));
            Assert.That(Stat(session, "MoveSpeed"), Is.EqualTo(1));
            Assert.That(Stat(session, "Logging"), Is.EqualTo(1));
            Assert.That((bool)Property(session, "HasShield"), Is.True);

            Teleport(player, GameObject.Find("Equipment Rack").transform.position + Vector3.back);
            yield return null;
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.gloves.swift"), Is.True);
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.gloves.swift"), Is.False);
            var pack = (System.Collections.IList)Property(session, "BackpackSlots");
            int index = -1;
            for (int i = 0; i < pack.Count; i++)
                if ((string)Field(pack[i], "itemId") == "gear.gloves.swift") index = i;
            Assert.That(index, Is.GreaterThanOrEqualTo(0));
            Assert.That((bool)Invoke(session, "TryEquipFromBackpack", index), Is.True);
            Assert.That(Stat(session, "Attack"), Is.EqualTo(4));
            Assert.That(Stat(session, "AttackSpeed"), Is.EqualTo(1));
            Assert.That((string)Field(pack[index], "itemId"), Is.EqualTo("gear.gloves.starter"));

            Invoke(session, "FlushCurrent");
            object repository = Field(session, "_repository");
            object loaded = Invoke(repository, "Load");
            object character = Invoke(loaded, "Character", Property(session, "ActiveCharacterId"));
            object world = Invoke(loaded, "World", Property(session, "ActiveWorldId"));
            Assert.That((string)Field(Field(character, "equipment"), "handsId"),
                Is.EqualTo("gear.gloves.swift"));
            Assert.That(((System.Collections.IList)Field(world, "claimedGearIds")).Count,
                Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ShieldBlocksRepeatedFrontalHitsAfterRaiseButNotRearHit()
        {
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component vitality = player.GetComponent("PlayerVitality");
            Component movement = player.GetComponent("FeelStudyPlayer");
            yield return TopazTestTravel.EnterGraveyard(player);
            Component enemy = GameObject.Find("Scout A").GetComponent("EnemyCombatant");
            Vector3 screenRight = Vector3.ProjectOnPlane(Camera.main.transform.right,
                Vector3.up).normalized;
            Teleport(player, GameObject.Find("Scout A").transform.position - screenRight * 1.35f);
            Set(mouse.rightButton, 1f);
            yield return new WaitForSeconds(.18f);
            Vector3 aim = (Vector3)Property(movement, "AimDirection");
            Component combat = player.GetComponent("PlayerCombat");
            Assert.That((bool)Property(combat, "IsGuardRaised"), Is.True,
                $"held={Field(combat, "_guardHeld")}, block={Field(combat, "_blockAction")}, " +
                $"airborne={Property(movement, "IsAirborne")}, shield={Property(session, "HasShield")}, " +
                $"suppress={Property(session, "SuppressAttack")}");
            for (int i = 0; i < 2; i++)
            {
                Assert.That((bool)Invoke(vitality, "TryTakeDirectedDamage", 4,
                    player.transform.position + aim * 2f, enemy), Is.False);
                Assert.That((int)Property(vitality, "CurrentHealth"), Is.EqualTo(6));
            }
            Assert.That((int)Property(session, "ShieldExperience"), Is.EqualTo(5));
            Assert.That((bool)Invoke(vitality, "TryTakeDirectedDamage", 4,
                player.transform.position - aim * 2f, enemy), Is.True);
            Assert.That((int)Property(vitality, "CurrentHealth"), Is.EqualTo(4));
            Assert.That((int)Property(session, "ShieldExperience"), Is.EqualTo(5));
            Set(mouse.rightButton, 0f);
        }

        [UnityTest]
        public IEnumerator EmptySlotsDisableTheirActionsAndKeepGearInBackpack()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            Component session = GameObject.Find("Player").GetComponent("WorldSession");
            System.Type slot = System.Type.GetType("Topaz.LoopStudy.EquipmentSlot, Assembly-CSharp");
            foreach (string name in new[] { "Weapon", "Tool", "Offhand" })
                Assert.That((bool)Invoke(session, "TryUnequip", System.Enum.Parse(slot, name)), Is.True);
            Assert.That((bool)Property(session, "HasWeapon"), Is.False);
            Assert.That((bool)Property(session, "HasAxe"), Is.False);
            Assert.That((bool)Property(session, "HasShield"), Is.False);
            var pack = (System.Collections.IList)Property(session, "BackpackSlots");
            int gearCount = 0;
            foreach (object stack in pack)
                if ((int)Field(stack, "count") == 1) gearCount++;
            Assert.That(gearCount, Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator FullBackpackLeavesRackRewardAndUnequipDoesNotLoseGear()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            object wood = Field(session, "wood");
            Assert.That((int)Invoke(backpack, "Add", wood, 320), Is.EqualTo(320));
            Teleport(player, GameObject.Find("Equipment Rack").transform.position + Vector3.back);
            yield return null;
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.gloves.swift"), Is.False);
            Assert.That((bool)Invoke(session, "RackHas", "gear.gloves.swift"), Is.True);
            System.Type slot = System.Type.GetType("Topaz.LoopStudy.EquipmentSlot, Assembly-CSharp");
            Assert.That((bool)Invoke(session, "TryUnequip", System.Enum.Parse(slot, "Hands")), Is.False);
            Assert.That((string)Field(Property(session, "Equipped"), "handsId"),
                Is.EqualTo("gear.gloves.starter"));
        }

        [UnityTest]
        public IEnumerator TwoHandedEquipMovesShieldAtomicallyAndReverseSwapKeepsEveryItem()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Teleport(player, GameObject.Find("Equipment Rack").transform.position + Vector3.back);
            yield return null;
            const string axeId = "gear.axe.twohanded.starter";
            Assert.That((bool)Invoke(session, "TryClaimRackItem", axeId), Is.True);
            object backpack = Field(session, "_backpack");
            var pack = (System.Collections.IList)Property(session, "BackpackSlots");
            int axeIndex = -1;
            for (int i = 0; i < pack.Count; i++)
                if ((string)Field(pack[i], "itemId") == axeId) axeIndex = i;
            Assert.That(axeIndex, Is.GreaterThanOrEqualTo(0));
            object wood = Field(session, "wood");
            Assert.That((int)Invoke(backpack, "Add", wood, 300), Is.EqualTo(300));

            Assert.That((bool)Invoke(session, "TryEquipFromBackpack", axeIndex), Is.False);
            Assert.That((string)Field(Property(session, "Equipped"), "weaponId"),
                Is.EqualTo("gear.sword.starter"));
            Assert.That((bool)Property(session, "HasShield"), Is.True);
            Assert.That((int)Invoke(backpack, "Count", axeId), Is.EqualTo(1));

            Assert.That((int)Invoke(backpack, "Remove", "material.wood", 20), Is.EqualTo(20));
            Assert.That((bool)Invoke(session, "TryEquipFromBackpack", axeIndex), Is.True);
            Assert.That((string)Field(Property(session, "Equipped"), "weaponId"), Is.EqualTo(axeId));
            Assert.That((bool)Property(session, "HasShield"), Is.False);
            Assert.That((int)Invoke(backpack, "Count", "gear.shield.starter"), Is.EqualTo(1));
            Assert.That((int)Invoke(backpack, "Count", "gear.sword.starter"), Is.EqualTo(1));

            int shieldIndex = -1;
            for (int i = 0; i < pack.Count; i++)
                if ((string)Field(pack[i], "itemId") == "gear.shield.starter") shieldIndex = i;
            Assert.That((bool)Invoke(session, "TryEquipFromBackpack", shieldIndex), Is.True);
            Assert.That((bool)Property(session, "HasShield"), Is.True);
            Assert.That((string)Field(Property(session, "Equipped"), "weaponId"), Is.Null);
            Assert.That((int)Invoke(backpack, "Count", axeId), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator SpareGearTransfersThroughChestOneItemAtATime()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            object backpack = Field(session, "_backpack");
            object wood = Field(session, "wood");
            Invoke(backpack, "Add", wood, 3);
            Assert.That((bool)Invoke(session, "TryCraftChest"), Is.True);
            FieldInfo preview = session.GetType().GetField("_previewPosition",
                BindingFlags.Instance | BindingFlags.NonPublic);
            preview.SetValue(session, new Vector3(0f, 0f, 1.5f));
            session.GetType().GetMethod("TryPlaceChest", BindingFlags.Instance |
                BindingFlags.NonPublic).Invoke(session, null);
            Assert.That((bool)Property(session, "ChestPlaced"), Is.True);

            Teleport(player, GameObject.Find("Equipment Rack").transform.position + Vector3.back);
            yield return null;
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.boots.agile"), Is.True);
            var pack = (System.Collections.IList)Property(session, "BackpackSlots");
            int packIndex = -1;
            for (int i = 0; i < pack.Count; i++)
                if ((string)Field(pack[i], "itemId") == "gear.boots.agile") packIndex = i;
            Assert.That((bool)Invoke(session, "TryTransferGear", true, packIndex), Is.True);
            Assert.That((int)Field(pack[packIndex], "count"), Is.Zero);
            var chest = (System.Collections.IList)Property(session, "ChestSlots");
            int chestIndex = -1;
            for (int i = 0; i < chest.Count; i++)
                if ((string)Field(chest[i], "itemId") == "gear.boots.agile") chestIndex = i;
            Assert.That(chestIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That((bool)Invoke(session, "TryTransferGear", false, chestIndex), Is.True);
            Assert.That((int)Field(chest[chestIndex], "count"), Is.Zero);
            Assert.That((int)Invoke(backpack, "Count", "gear.boots.agile"), Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator EquipmentJournalOpensWithPortraitAndControllerFocus()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject canvas = GameObject.Find("Loop HUD");
            Component hud = canvas.GetComponent("LoopHud");
            Component view = canvas.GetComponent("EquipmentJournalView");
            Assert.That(view, Is.Not.Null);
            Invoke(view, "Show");
            Assert.That((bool)Property(view, "IsOpen"), Is.True);
            Assert.That((bool)Property(hud, "MenuOpen"), Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.Not.Null);
            UnityEngine.UI.Image portrait = (UnityEngine.UI.Image)Field(view, "portrait");
            Assert.That(portrait.sprite, Is.Not.Null);
            Invoke(hud, "ClosePanels");
            Assert.That((bool)Property(hud, "MenuOpen"), Is.False);
        }

        [UnityTest]
        public IEnumerator EveryAppearanceHasAVisibleLeftHandShield()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component appearance = player.GetComponent("PlayerAppearance");
            foreach (string look in new[] { "rogue", "rogue.hooded", "knight", "ranger",
                "mage", "barbarian" })
            {
                Assert.That((bool)Invoke(appearance, "Apply", look), Is.True, look);
                yield return null;
                Transform shield = null;
                foreach (Transform bone in player.GetComponentsInChildren<Transform>(true))
                    if (bone.name == "handslot.l")
                    {
                        Transform candidate = bone.Find("Held Shield");
                        if (candidate != null && candidate.gameObject.activeInHierarchy)
                            shield = candidate;
                    }
                Assert.That(shield, Is.Not.Null, look + " must show its left-hand shield");
            }
        }

        [UnityTest]
        public IEnumerator SidegradesChangeSwingAndTravelTimingWithoutChangingOtherStats()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");
            yield return null;
            GameObject player = GameObject.Find("Player");
            Component session = player.GetComponent("WorldSession");
            Component combat = player.GetComponent("PlayerCombat");
            Component movement = player.GetComponent("FeelStudyPlayer");
            float starterSwing = (float)Property(combat, "AttackAnimationSeconds");
            float starterTravel = (float)Property(movement, "TravelSpeed");
            Teleport(player, GameObject.Find("Equipment Rack").transform.position + Vector3.back);
            yield return null;
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.gloves.swift"), Is.True);
            Assert.That((bool)Invoke(session, "TryClaimRackItem", "gear.boots.agile"), Is.True);
            var pack = (System.Collections.IList)Property(session, "BackpackSlots");
            for (int i = 0; i < pack.Count; i++)
                if ((string)Field(pack[i], "itemId") == "gear.gloves.swift" ||
                    (string)Field(pack[i], "itemId") == "gear.boots.agile")
                    Assert.That((bool)Invoke(session, "TryEquipFromBackpack", i), Is.True);
            Assert.That((float)Property(combat, "AttackAnimationSeconds"),
                Is.EqualTo(starterSwing * .9f).Within(.01f));
            Assert.That((float)Property(movement, "TravelSpeed"),
                Is.EqualTo(starterTravel - .25f).Within(.01f));
            Assert.That(Stat(session, "Dodge"), Is.EqualTo(1));
        }

        static int Stat(Component session, string name) =>
            (int)Field(Property(session, "Stats"), name);

        static object Property(object owner, string name) => owner.GetType()
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public).GetValue(owner);

        static object Field(object owner, string name) => owner.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .GetValue(owner);

        static object Invoke(object owner, string name, params object[] args) => owner.GetType()
            .GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(owner, args);

        static void Teleport(GameObject player, Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            controller.enabled = false;
            player.transform.position = position;
            controller.enabled = true;
        }
    }
}
