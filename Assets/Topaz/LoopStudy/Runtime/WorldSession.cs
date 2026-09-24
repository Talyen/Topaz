using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Topaz.CombatStudy;
using Topaz.Expedition;
using Topaz.FeelStudy;
using Topaz.Menus;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Topaz.LoopStudy
{
    /// <summary>Coordinates the one-resource expedition and one-chest home loop.</summary>
    public sealed class WorldSession : MonoBehaviour
    {
#if UNITY_EDITOR
        public static string EditorTestSaveDirectory { get; set; }
#endif
        [SerializeField] ItemDefinition wood;
        [SerializeField] HarvestTree tree;
        [SerializeField] RecipeDefinition chestRecipe;
        [SerializeField] StructureDefinition chestDefinition;
        [SerializeField] StorageChest chest;
        [SerializeField] SafeZone home;
        [SerializeField] Transform workbench;
        [SerializeField] Transform restPoint;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] PlayerCombat combat;
        [SerializeField] InputActionAsset controls;
        [SerializeField] LoopHud hud;
        [SerializeField] GameObject chestPreview;
        [SerializeField] Renderer previewRenderer;
        [SerializeField] GameObject pickupPrefab;
        [SerializeField] GameMenus menus;
        [SerializeField] Transform homeGate;

        const float InteractionRadius = 1.4f;
        const float PlacementStep = 0.75f;
        const int ExpeditionWoodReward = 3;
        const string ExpeditionSceneName = "Expedition";
        public const int BackpackCapacity = 16;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color ValidColor = new Color(0.36f, 0.95f, 0.57f);
        static readonly Color InvalidColor = new Color(0.96f, 0.38f, 0.34f);

        SaveRepository _repository;
        TopazSaveData _data;
        InventorySlots _backpack;
        InputAction _placeAction;
        InputAction _cancelAction;
        InputAction _inventoryAction;
        MaterialPropertyBlock _previewProperties;
        Vector3 _previewPosition;
        float _placementReadyAt;
        bool _placeRequested;
        bool _cancelRequested;
        bool _inventoryRequested;
        bool _placing;
        bool _savingEnabled = true;
        string _saveProblem;
        bool _traveling;
        ExpeditionSceneBootstrap _expedition;

        public int CurrentDay => _data?.day ?? 1;
        public string CurrentRegionId => _data?.regionId ?? TopazSaveData.HomeRegion;
        public bool ExpeditionCacheClaimed => _data != null && _data.expeditionCacheClaimed;
        public int WoodCount => _data == null ? 0 : CountWood();
        public IReadOnlyList<ItemStackRecord> BackpackSlots => _backpack?.Slots;
        public IReadOnlyList<ItemStackRecord> ChestSlots => chest?.Inventory?.Slots;
        public bool BackpackHasItems => _backpack != null && _backpack.Slots.Any(slot => slot.count > 0);
        public bool ChestHasItems => chest?.Inventory != null && chest.Inventory.Slots.Any(slot => slot.count > 0);
        public int LoggingExperience => _data?.loggingExperience ?? 0;
        public int LoggingLevel => 1 + LoggingExperience / 10;
        public int SwordsExperience => _data?.swordsExperience ?? 0;
        public int SwordsLevel => 1 + SwordsExperience / 10;
        public int PickupCount => _data?.pickups.Count ?? 0;
        public bool PendingChest => _data != null && _data.pendingChest;
        public int ChestCost => chestRecipe != null ? chestRecipe.IngredientCount : 0;
        public bool CanCraftChest => _data != null && !_data.pendingChest && !ChestPlaced &&
            WoodCount >= ChestCost;
        public string EquippedToolName => combat != null ? combat.EquippedToolName : "Sword";
        public bool ChestPlaced => chest != null && chest.IsPlaced;
        public int ChestWood => chest?.Inventory?.Count(wood.StableId) ?? 0;
        public int ChestCapacity => chest?.SlotCapacity ?? 0;
        public bool IsPlacing => _placing;
        public bool MenuOpen => (hud != null && hud.MenuOpen) || (menus != null && menus.BlockGameplay);
        public bool BlockMovement => MenuOpen || _traveling;
        public bool SuppressAttack => MenuOpen || _placing || _traveling;
        public string SaveProblem => _saveProblem;

        void Awake()
        {
            _previewProperties = new MaterialPropertyBlock();
            if (controls == null || wood == null || tree == null || chest == null ||
                chestRecipe == null || chestDefinition == null || home == null ||
                movement == null || combat == null || hud == null || pickupPrefab == null ||
                menus == null || homeGate == null)
            {
                Debug.LogError("World session is missing a required reference.", this);
                enabled = false;
                return;
            }

            bool runningTests = Application.isEditor || Environment.GetCommandLineArgs().Any(arg =>
                arg.Equals("-runTests", StringComparison.OrdinalIgnoreCase));
            string directory = runningTests
                ? Path.Combine(Application.temporaryCachePath, "TopazTest-" + Guid.NewGuid().ToString("N"))
                : Application.persistentDataPath;
#if UNITY_EDITOR
            if (runningTests && !string.IsNullOrEmpty(EditorTestSaveDirectory))
                directory = EditorTestSaveDirectory;
#endif
            _repository = new SaveRepository(directory);
            try
            {
                _data = _repository.Load();
                _backpack = new InventorySlots(_data.backpackSlots, BackpackCapacity);
            }
            catch (Exception error)
            {
                _data = new TopazSaveData();
                _backpack = new InventorySlots(_data.backpackSlots, BackpackCapacity);
                _savingEnabled = false;
                _saveProblem = "Save could not be read; saving is disabled to protect it.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
            }

            InputActionMap map = controls.FindActionMap("Player", true);
            _placeAction = map.FindAction("Place", true);
            _cancelAction = map.FindAction("Cancel", true);
            _inventoryAction = map.FindAction("Inventory", true);
        }

        void OnEnable()
        {
            if (_placeAction != null) _placeAction.performed += OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed += OnCancelPerformed;
            if (_inventoryAction != null) _inventoryAction.performed += OnInventoryPerformed;
        }

        void OnDisable()
        {
            if (_placeAction != null) _placeAction.performed -= OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
            if (_inventoryAction != null) _inventoryAction.performed -= OnInventoryPerformed;
        }

        void Start()
        {
            bool restoreExpedition = _data.regionId == TopazSaveData.ExpeditionRegion;
            _traveling = restoreExpedition;
            Teleport(restoreExpedition ? home.transform.position :
                new Vector3(_data.playerX, 0f, _data.playerZ));

            tree.Bind(this);
            StructureStateRecord placed = _data.structures.FirstOrDefault(record =>
                record.definitionId == chestDefinition.StableId);
            chest.Bind(placed);
            combat.EquipTool(_data.equippedTool, false);
            foreach (PickupStateRecord pickup in _data.pickups)
            {
                ItemDefinition item = ResolveItem(pickup.itemId);
                if (item == null)
                {
                    Debug.LogWarning($"[Topaz] Unknown saved pickup item: {pickup.itemId}", this);
                    continue;
                }
                CreatePickup(pickup, item);
            }
            if (chestPreview != null) chestPreview.SetActive(false);
            hud.Bind(this);
            if (restoreExpedition) StartCoroutine(EnterExpedition(true));
            else Commit();
        }

        void Update()
        {
            if (_savingEnabled && _repository?.BackgroundError != null)
            {
                _savingEnabled = false;
                _saveProblem = "Saving failed; the existing save was left untouched.";
                Debug.LogError($"[Topaz] {_saveProblem} {_repository.BackgroundError.Message}", this);
                hud?.Refresh();
            }
            if (_inventoryRequested && !_placing && !menus.BlockGameplay) hud.ToggleInventoryPanel();
            if (_cancelRequested)
            {
                if (_placing) ExitPlacement();
                else menus.HandleEscape();
            }

            if (_placing)
            {
                UpdatePreview();
                if (_placeRequested && Time.time >= _placementReadyAt)
                    TryPlaceChest();
            }

            _placeRequested = false;
            _cancelRequested = false;
            _inventoryRequested = false;
            hud?.Tick();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                Commit();
                FlushSave();
            }
        }

        void OnApplicationQuit()
        {
            Commit();
            FlushSave();
        }

        void FlushSave()
        {
            if (!_savingEnabled || _repository == null) return;
            try { _repository.Flush(); }
            catch (Exception error)
            {
                _savingEnabled = false;
                _saveProblem = "Saving failed; the existing save was left untouched.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
            }
        }

        void OnPlacePerformed(InputAction.CallbackContext context) => _placeRequested = true;
        void OnCancelPerformed(InputAction.CallbackContext context) => _cancelRequested = true;
        void OnInventoryPerformed(InputAction.CallbackContext context) => _inventoryRequested = true;

        public NodeStateRecord GetOrCreateNodeState(string objectId)
        {
            NodeStateRecord state = _data.nodes.FirstOrDefault(node => node.objectId == objectId);
            if (state != null) return state;
            state = new NodeStateRecord { objectId = objectId };
            _data.nodes.Add(state);
            return state;
        }

        public void RecordLoggingChop(int experience)
        {
            if (experience <= 0) return;
            _data.loggingExperience += experience;
            hud.ShowStatus($"+{experience} Logging XP");
        }

        public void RecordSwordHit(int damage)
        {
            if (damage <= 0) return;
            _data.swordsExperience += damage;
            Commit();
            hud.ShowStatus($"+{damage} Swords XP");
        }

        public void DropHarvest(HarvestDefinition definition, Vector3 position)
        {
            if (definition == null || definition.YieldItem == null) return;
            var record = new PickupStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = definition.YieldItem.StableId,
                count = definition.YieldCount,
                x = position.x,
                z = position.z
            };
            _data.pickups.Add(record);
            CreatePickup(record, definition.YieldItem);
            hud.ShowStatus($"{record.count} {definition.YieldItem.DisplayName} dropped. Walk near it to collect.");
        }

        public void TryCollect(WorldPickup pickup)
        {
            if (pickup == null || pickup.State == null || pickup.Item == null) return;
            int accepted = _backpack.Add(pickup.Item, pickup.State.count);
            if (accepted <= 0) return;
            pickup.State.count -= accepted;
            if (pickup.State.count == 0)
            {
                _data.pickups.Remove(pickup.State);
                Destroy(pickup.gameObject);
            }
            Commit();
            hud.ShowStatus($"+{accepted} {pickup.Item.DisplayName}");
        }

        void CreatePickup(PickupStateRecord record, ItemDefinition item)
        {
            GameObject instance = Instantiate(pickupPrefab);
            instance.name = $"{item.DisplayName} Pickup";
            WorldPickup pickup = instance.GetComponent<WorldPickup>();
            pickup.Bind(this, record, item);
        }

        public bool TryInteract()
        {
            if (_traveling) return true;
            if (menus.BlockGameplay) return true;
            if (_placing) return true;
            if (MenuOpen)
            {
                hud.ClosePanels();
                return true;
            }

            if (_data.regionId == TopazSaveData.ExpeditionRegion && _expedition != null)
            {
                if (DistanceSquared(_expedition.Departure) < InteractionRadius * InteractionRadius)
                {
                    StartCoroutine(ReturnHome(false));
                    return true;
                }
                if (DistanceSquared(_expedition.SupplyCache) < InteractionRadius * InteractionRadius)
                {
                    TryClaimExpeditionCache();
                    return true;
                }
                return false;
            }
            if (_data.regionId == TopazSaveData.HomeRegion &&
                DistanceSquared(homeGate) < InteractionRadius * InteractionRadius)
            {
                StartCoroutine(EnterExpedition(false));
                return true;
            }

            float nearest = InteractionRadius * InteractionRadius;
            int choice = 0;
            if (workbench != null && DistanceSquared(workbench) < nearest)
            {
                nearest = DistanceSquared(workbench);
                choice = 1;
            }
            if (restPoint != null && DistanceSquared(restPoint) < nearest)
            {
                nearest = DistanceSquared(restPoint);
                choice = 2;
            }
            if (chest.IsPlaced && DistanceSquared(chest.transform) < nearest)
                choice = 3;

            switch (choice)
            {
                case 1:
                    if (PendingChest) EnterPlacement();
                    else hud.ShowCraftPanel();
                    return true;
                case 2:
                    AdvanceDay();
                    return true;
                case 3:
                    hud.ShowChestPanel();
                    return true;
                default:
                    return false;
            }
        }

        public void ReturnHomeAfterDefeat()
        {
            if (_data != null && _data.regionId == TopazSaveData.ExpeditionRegion && !_traveling)
                StartCoroutine(ReturnHome(true));
        }

        bool TryClaimExpeditionCache()
        {
            if (_expedition == null || _data.expeditionCacheClaimed) return false;
            if (!_expedition.CacheUnlocked)
            {
                hud.ShowStatus("Defeat the guardian before taking the supplies.");
                return false;
            }
            if (_backpack.SpaceFor(wood) < ExpeditionWoodReward)
            {
                hud.ShowStatus("Make room for 3 Wood before opening the cache.");
                return false;
            }

            _backpack.Add(wood, ExpeditionWoodReward);
            _data.expeditionCacheClaimed = true;
            _expedition.HideClaimedCache();
            Commit();
            hud.ShowStatus("Guarded supplies collected: +3 Wood. Return home to craft.");
            return true;
        }

        IEnumerator EnterExpedition(bool restoring)
        {
            _traveling = true;
            hud.ClosePanels();
            Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
            if (!scene.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(ExpeditionSceneName, LoadSceneMode.Additive);
                if (load == null)
                {
                    RestoreHomeAfterTravelFailure();
                    yield break;
                }
                yield return load;
            }

            _expedition = FindAnyObjectByType<ExpeditionSceneBootstrap>();
            if (_expedition == null || _expedition.Arrival == null ||
                _expedition.Departure == null || _expedition.SupplyCache == null)
            {
                RestoreHomeAfterTravelFailure();
                yield break;
            }
            _expedition.Bind(GetComponent<PlayerVitality>(), home, _data.expeditionCacheClaimed);
            Teleport(restoring ? new Vector3(_data.playerX, 0f, _data.playerZ) :
                _expedition.Arrival.position);
            _data.regionId = TopazSaveData.ExpeditionRegion;
            _traveling = false;
            Commit();
            hud.ShowStatus(restoring ? "Expedition resumed." :
                "Expedition clearing. The trail marker returns you home at any time.");
        }

        IEnumerator ReturnHome(bool afterDefeat)
        {
            _traveling = true;
            Teleport(afterDefeat ? home.transform.position : homeGate.position + Vector3.forward * 1.7f);
            _data.regionId = TopazSaveData.HomeRegion;
            Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
            _expedition = null;
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            _traveling = false;
            Commit();
            hud.ShowStatus(afterDefeat ? "Returned home to recover." :
                "Returned home with your supplies.");
        }

        void RestoreHomeAfterTravelFailure()
        {
            Debug.LogError("Expedition clearing could not be loaded; returning home.", this);
            _expedition = null;
            Teleport(home.transform.position);
            _data.regionId = TopazSaveData.HomeRegion;
            _traveling = false;
            Commit();
            hud.ShowStatus("Expedition is unavailable; you returned home.");
        }

        void Teleport(Vector3 position)
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = position;
            if (controller != null) controller.enabled = true;
            movement.ResetMotion();
        }

        public string ContextPrompt()
        {
            if (_traveling) return "Traveling between regions...";
            if (_placing) return "Place chest: aim and click / A   •   Esc / B cancels";
            if (MenuOpen) return "E or Esc / B closes the panel";
            if (_data.regionId == TopazSaveData.ExpeditionRegion)
            {
                if (_expedition == null) return "Entering the clearing...";
                if (DistanceSquared(_expedition.Departure) < InteractionRadius * InteractionRadius)
                    return "E / X: return to the homestead";
                if (DistanceSquared(_expedition.SupplyCache) < InteractionRadius * InteractionRadius)
                    return _data.expeditionCacheClaimed ? "Supply cache is empty" :
                        _expedition.CacheUnlocked ? "E / X: take guarded Wood" :
                        "Defeat the guardian to open the supply cache";
                return "Find the guarded cache, or return by the trail marker";
            }
            if (DistanceSquared(homeGate) < InteractionRadius * InteractionRadius)
                return "E / X: travel to the expedition clearing";
            float nearest = InteractionRadius * InteractionRadius;
            string prompt = "";
            if (workbench != null && DistanceSquared(workbench) < nearest)
            {
                nearest = DistanceSquared(workbench);
                prompt = PendingChest ? "E / X: place crafted chest" : "E / X: craft at workbench";
            }
            if (restPoint != null && DistanceSquared(restPoint) < nearest)
            {
                nearest = DistanceSquared(restPoint);
                prompt = "E / X: rest until next day";
            }
            if (chest.IsPlaced && DistanceSquared(chest.transform) < nearest)
                prompt = "E / X: open storage chest";
            if (prompt.Length == 0 && tree.IsAvailable &&
                (tree.transform.position - transform.position).sqrMagnitude < 12f)
                prompt = "Equip axe (2 / Y), aim, and chop (click / RT)";
            if (prompt.Length == 0) prompt = "I / Start: backpack";
            return prompt;
        }

        public bool TryCraftChest()
        {
            if (_data.pendingChest || chest.IsPlaced || chestRecipe.Ingredient == null ||
                chestRecipe.Result != chestDefinition ||
                CountWood() < chestRecipe.IngredientCount)
            {
                hud.ShowStatus($"Need {ChestCost} Wood and an empty chest plot.");
                return false;
            }

            _backpack.Remove(chestRecipe.Ingredient.StableId, chestRecipe.IngredientCount);
            _data.pendingChest = true;
            Commit();
            hud.ClosePanels();
            EnterPlacement();
            hud.ShowStatus("Chest crafted. Place it within the blue home boundary.");
            return true;
        }

        public void DepositAllItems()
        {
            if (!chest.IsPlaced) return;
            int deposited = _backpack.TransferAllTo(chest.Inventory, ResolveItem);
            if (deposited == 0) return;
            Commit();
            hud.ShowStatus($"Stored {deposited} items.");
        }

        public void WithdrawAllItems()
        {
            if (!chest.IsPlaced) return;
            int withdrawn = chest.Inventory.TransferAllTo(_backpack, ResolveItem);
            if (withdrawn == 0) return;
            Commit();
            hud.ShowStatus($"Took {withdrawn} items.");
        }

        public void ToolChanged(string toolId)
        {
            if (_data == null) return;
            _data.equippedTool = toolId;
            Commit();
        }

        public void Commit()
        {
            if (_data == null || _repository == null || !_savingEnabled || _traveling) return;
            _data.playerX = transform.position.x;
            _data.playerZ = transform.position.z;
            try { _repository.QueueSave(_data); }
            catch (Exception error)
            {
                _savingEnabled = false;
                _saveProblem = "Saving failed; the existing save was left untouched.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
            }
            hud?.Refresh();
        }

        void EnterPlacement()
        {
            if (!PendingChest || chest.IsPlaced || !home.Contains(transform.position)) return;
            _placing = true;
            _placeRequested = false;
            _placementReadyAt = Time.time + 0.15f;
            hud.ClosePanels();
            chestPreview.SetActive(true);
            UpdatePreview();
        }

        void ExitPlacement()
        {
            _placing = false;
            chestPreview.SetActive(false);
            hud.ShowStatus("Chest remains crafted. Return to the workbench to place it.");
        }

        void UpdatePreview()
        {
            Vector3 point = movement.AimPointOnGround;
            _previewPosition = new Vector3(Mathf.Round(point.x / PlacementStep) * PlacementStep,
                0f, Mathf.Round(point.z / PlacementStep) * PlacementStep);
            chestPreview.transform.position = _previewPosition;
            _previewProperties.SetColor(BaseColor, ValidPlacement() ? ValidColor : InvalidColor);
            previewRenderer.SetPropertyBlock(_previewProperties);
        }

        void TryPlaceChest()
        {
            if (!ValidPlacement())
            {
                hud.ShowStatus("Choose a clear spot inside the home boundary.");
                return;
            }

            var placed = new StructureStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = chestDefinition.StableId,
                x = _previewPosition.x,
                z = _previewPosition.z
            };
            _data.structures.Add(placed);
            _data.pendingChest = false;
            _placing = false;
            chestPreview.SetActive(false);
            chest.Bind(placed);
            Commit();
            hud.ShowStatus("Storage chest placed. Use E / X near it.");
        }

        bool ValidPlacement()
        {
            if (!home.Contains(_previewPosition) ||
                (_previewPosition - home.transform.position).magnitude > home.Radius - 0.65f)
                return false;
            if ((_previewPosition - transform.position).sqrMagnitude < 1.2f * 1.2f)
                return false;
            if (workbench != null && (_previewPosition - workbench.position).sqrMagnitude < 1.1f * 1.1f)
                return false;
            if (restPoint != null && (_previewPosition - restPoint.position).sqrMagnitude < 1.1f * 1.1f)
                return false;
            return !chest.IsPlaced;
        }

        void AdvanceDay()
        {
            _data.day++;
            tree.RefreshForDay();
            Commit();
            hud.ShowStatus($"Day {_data.day}. The tree regrows after three days.");
        }

        ItemDefinition ResolveItem(string id) => id == wood.StableId ? wood : null;

        public string ItemName(string id) => ResolveItem(id)?.DisplayName ?? id;

        int CountWood() => _backpack.Count(wood.StableId);

        float DistanceSquared(Transform target)
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
