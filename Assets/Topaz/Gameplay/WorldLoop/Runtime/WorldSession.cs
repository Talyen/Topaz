using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Topaz.CombatStudy;
using Topaz.AnimationStudy;
using Topaz.Expedition;
using Topaz.FeelStudy;
using Topaz.Menus;
using Topaz.VisualStudy;
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
        [SerializeField] VisualLookController look;
        [SerializeField] PlayerAppearance appearance;

        const float InteractionRadius = 1.4f;
        const float PlacementStep = 0.75f;
        const int ExpeditionWoodReward = 3;
        const string ExpeditionSceneName = "Expedition";
        public const int BackpackCapacity = 16;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color ValidColor = new Color(0.36f, 0.95f, 0.57f);
        static readonly Color InvalidColor = new Color(0.96f, 0.38f, 0.34f);

        ProfileRepository _repository;
        TopazProfileData _profile;
        TopazCharacterData _character;
        TopazWorldData _world;
        TopazVisitData _visit;
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
        bool _resting;
        bool _applicationPaused;
        bool _clockWasActive;
        float _activeSecondsSinceSave;
        ExpeditionSceneBootstrap _expedition;
        readonly List<GameObject> _spawnedPickups = new List<GameObject>();

        public IReadOnlyList<TopazCharacterData> Characters => _profile?.characters;
        public IReadOnlyList<TopazWorldData> Worlds => _profile?.worlds;
        public bool HasLastPair => _profile != null &&
            _profile.Visit(_profile.lastCharacterId, _profile.lastWorldId) != null;
        public string LastCharacterId => _profile?.lastCharacterId;
        public string LastWorldId => _profile?.lastWorldId;
        public string LastAppearanceId => _profile?.Character(_profile.lastCharacterId)?.appearanceId
            ?? CharacterLooks.Rogue;
        public bool HasActivePair => _data != null;
        public string ActiveCharacterId => _character?.id;
        public string ActiveWorldId => _world?.id;

        public int CurrentDay => _data == null ? 1 :
            1 + (int)Math.Floor((_data.worldHours - WorldClock.StartingHour) / WorldClock.HoursPerDay);
        public double WorldHours => _data?.worldHours ?? WorldClock.StartingHour;
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
        public bool BlockMovement => MenuOpen || _traveling || _resting;
        public bool SuppressAttack => MenuOpen || _placing || _traveling || _resting;
        public string SaveProblem => _saveProblem;

        void Awake()
        {
            _previewProperties = new MaterialPropertyBlock();
            if (controls == null || wood == null || tree == null || chest == null ||
                chestRecipe == null || chestDefinition == null || home == null ||
                movement == null || combat == null || hud == null || pickupPrefab == null ||
                menus == null || homeGate == null || look == null || appearance == null)
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
            _repository = new ProfileRepository(directory);
            try
            {
                _profile = _repository.Load();
            }
            catch (Exception error)
            {
                _profile = new TopazProfileData();
                _savingEnabled = false;
                _saveProblem = "Characters and Worlds could not be read. Your data was left untouched.";
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
            if (_resting) look?.SetRestFade(0f);
            _resting = false;
        }

        public void EnterEditorTestPair()
        {
            if (_data != null || !_savingEnabled) return;
            TopazCharacterData character = HasLastPair
                ? _profile.Character(_profile.lastCharacterId)
                : _profile.CreateCharacter(CharacterLooks.Rogue);
            TopazWorldData world = HasLastPair
                ? _profile.World(_profile.lastWorldId)
                : _profile.CreateWorld();
            TopazVisitData visit = _profile.GetOrCreateVisit(character.id, world.id);
            BindPair(character, world, visit);
            if (visit.regionId == TopazSaveData.ExpeditionRegion)
                StartCoroutine(EnterExpedition(true));
            else Commit();
        }

        /// <summary>Finish the active pair before entering another Character and World.</summary>
        public IEnumerator EnterPair(string characterId, string worldId, string newAppearanceId,
            bool newWorld, Action<bool> completed)
        {
            if (!_savingEnabled || _profile == null)
            {
                completed?.Invoke(false);
                yield break;
            }
            if (_data != null)
            {
                Commit();
                FlushSave();
                if (!_savingEnabled)
                {
                    completed?.Invoke(false);
                    yield break;
                }
            }
            if ((string.IsNullOrEmpty(newAppearanceId) && _profile.Character(characterId) == null) ||
                (!newWorld && _profile.World(worldId) == null))
            {
                completed?.Invoke(false);
                yield break;
            }
            string previousCharacterId = _profile.lastCharacterId;
            string previousWorldId = _profile.lastWorldId;
            bool createdCharacter = !string.IsNullOrEmpty(newAppearanceId);
            TopazCharacterData character = createdCharacter
                ? _profile.CreateCharacter(newAppearanceId) : _profile.Character(characterId);
            TopazWorldData world = newWorld ? _profile.CreateWorld() : _profile.World(worldId);
            TopazVisitData visit = _profile.Visit(character.id, world.id);
            bool createdVisit = visit == null;
            if (createdVisit) visit = _profile.GetOrCreateVisit(character.id, world.id);
            _profile.lastCharacterId = character.id;
            _profile.lastWorldId = world.id;
            try { _repository.Save(_profile); }
            catch (Exception error)
            {
                if (createdVisit) _profile.visits.Remove(visit);
                if (createdCharacter) _profile.characters.Remove(character);
                if (newWorld) _profile.worlds.Remove(world);
                _profile.lastCharacterId = previousCharacterId;
                _profile.lastWorldId = previousWorldId;
                _savingEnabled = false;
                _saveProblem = "Character and World changes could not be stored. Previous data is safe.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
                completed?.Invoke(false);
                yield break;
            }

            StopAllCoroutines();
            _traveling = true;
            Scene oldExpedition = SceneManager.GetSceneByName(ExpeditionSceneName);
            if (oldExpedition.isLoaded) yield return SceneManager.UnloadSceneAsync(oldExpedition);
            _expedition = null;
            foreach (GameObject pickup in _spawnedPickups)
                if (pickup != null)
                {
                    pickup.SetActive(false);
                    Destroy(pickup);
                }
            _spawnedPickups.Clear();
            chest.Bind(null);
            BindPair(character, world, visit);
            if (visit.regionId == TopazSaveData.ExpeditionRegion)
                yield return EnterExpedition(true);
            else
            {
                _traveling = false;
                Commit();
            }
            FlushSave();
            _traveling = false;
            completed?.Invoke(_savingEnabled);
        }

        void BindPair(TopazCharacterData character, TopazWorldData world, TopazVisitData visit)
        {
            _character = character;
            _world = world;
            _visit = visit;
            _profile.lastCharacterId = character.id;
            _profile.lastWorldId = world.id;
            _data = new TopazSaveData
            {
                worldHours = world.worldHours,
                loggingExperience = character.loggingExperience,
                swordsExperience = character.swordsExperience,
                equippedTool = character.equippedTool,
                pendingChest = character.pendingChest,
                backpackSlots = character.backpackSlots,
                expeditionCacheClaimed = world.expeditionCacheClaimed,
                nodes = world.nodes,
                structures = world.structures,
                pickups = world.pickups,
                regionId = visit.regionId,
                playerX = visit.playerX,
                playerZ = visit.playerZ
            };
            _backpack = new InventorySlots(_data.backpackSlots, BackpackCapacity);
            _placing = false;
            _resting = false;
            look.SetRestFade(0f);
            bool restoreExpedition = visit.regionId == TopazSaveData.ExpeditionRegion;
            Teleport(restoreExpedition ? home.transform.position :
                new Vector3(visit.playerX, 0f, visit.playerZ));
            tree.Bind(this);
            StructureStateRecord placed = world.structures.FirstOrDefault(record =>
                record.definitionId == chestDefinition.StableId);
            chest.Bind(placed);
            combat.EquipTool(character.equippedTool, false);
            if (!appearance.Apply(character.appearanceId))
                appearance.Apply(CharacterLooks.Rogue);
            foreach (PickupStateRecord pickup in world.pickups)
            {
                ItemDefinition item = ResolveItem(pickup.itemId);
                if (item != null) CreatePickup(pickup, item);
                else Debug.LogWarning($"[Topaz] Unknown saved pickup item: {pickup.itemId}", this);
            }
            if (chestPreview != null) chestPreview.SetActive(false);
            look.SetWorldHours(world.worldHours);
            hud.Bind(this);
            _activeSecondsSinceSave = 0f;
            _clockWasActive = false;
        }

        void Update()
        {
            if (_savingEnabled && _repository?.BackgroundError != null)
            {
                _savingEnabled = false;
                _saveProblem = "Recent progress could not be stored. Earlier progress is safe.";
                Debug.LogError($"[Topaz] {_saveProblem} {_repository.BackgroundError.Message}", this);
                hud?.Refresh();
            }
            if (_data == null)
            {
                if (_cancelRequested) menus.HandleEscape();
                _placeRequested = _cancelRequested = _inventoryRequested = false;
                return;
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
            TickWorldClock();
        }

        void TickWorldClock()
        {
            if (_data == null) return;
            bool active = !_applicationPaused && !_traveling && !_resting && !MenuOpen &&
                Time.timeScale > 0f;
            if (!active)
            {
                if (_clockWasActive) Commit();
                _clockWasActive = false;
                return;
            }

            _clockWasActive = true;
            _data.worldHours = WorldClock.Advance(_data.worldHours, Time.deltaTime);
            look.SetWorldHours(_data.worldHours);
            _activeSecondsSinceSave += Time.deltaTime;
            if (_activeSecondsSinceSave >= 30f)
            {
                _activeSecondsSinceSave = 0f;
                Commit();
            }
        }

        void OnApplicationPause(bool paused)
        {
            _applicationPaused = paused;
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
                _saveProblem = "Recent progress could not be stored. Earlier progress is safe.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
            }
        }

        public void FlushCurrent() => FlushSave();

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
            hud.ShowStatus($"{record.count} {definition.YieldItem.DisplayName} dropped.");
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
            _spawnedPickups.Add(instance);
            instance.name = $"{item.DisplayName} Pickup";
            WorldPickup pickup = instance.GetComponent<WorldPickup>();
            pickup.Bind(this, record, item);
        }

        public bool TryInteract()
        {
            if (_traveling || _resting) return true;
            if (menus.BlockGameplay) return true;
            if (_placing) return true;
            if (MenuOpen)
            {
                hud.ClosePanels();
                return true;
            }
            if (combat.IsHarvesting) return true;
            InteractionCandidate candidate = CurrentInteraction();
            switch (candidate.kind)
            {
                case InteractionKind.ReturnHome:
                    StartCoroutine(ReturnHome(false));
                    return true;
                case InteractionKind.SupplyCache:
                    TryClaimExpeditionCache();
                    return true;
                case InteractionKind.Travel:
                    StartCoroutine(EnterExpedition(false));
                    return true;
                case InteractionKind.Workbench:
                    if (PendingChest) EnterPlacement();
                    else hud.ShowCraftPanel();
                    return true;
                case InteractionKind.Rest:
                    StartCoroutine(Rest());
                    return true;
                case InteractionKind.Chest:
                    hud.ShowChestPanel();
                    return true;
                case InteractionKind.Harvest:
                    combat.TryStartHarvest(candidate.harvestTarget,
                        candidate.harvestTarget.RequiredToolId);
                    return true;
                default:
                    if (_data.regionId == TopazSaveData.ExpeditionRegion &&
                        _expedition != null && _expedition.SupplyCache != null &&
                        DistanceSquared(_expedition.SupplyCache) < InteractionRadius * InteractionRadius)
                    {
                        TryClaimExpeditionCache();
                        return true;
                    }
                    return false;
            }
        }

        enum InteractionKind { None, ReturnHome, SupplyCache, Travel, Chest, Workbench, Rest, Harvest }

        struct InteractionCandidate
        {
            public InteractionKind kind;
            public Transform anchor;
            public string verb;
            public HarvestTree harvestTarget;

            public InteractionCandidate(InteractionKind kind, Transform anchor, string verb,
                HarvestTree harvestTarget = null)
            {
                this.kind = kind;
                this.anchor = anchor;
                this.verb = verb;
                this.harvestTarget = harvestTarget;
            }
        }

        public bool TryGetInteraction(out Transform anchor, out string verb)
        {
            anchor = null;
            verb = null;
            if (_data == null || _traveling || _resting || _placing || MenuOpen ||
                combat.IsAttackLocked || movement.IsDodging || movement.IsAirborne) return false;
            InteractionCandidate candidate = CurrentInteraction();
            if (candidate.kind == InteractionKind.None) return false;
            anchor = candidate.anchor;
            verb = candidate.verb;
            return anchor != null;
        }

        InteractionCandidate CurrentInteraction()
        {
            if (_data == null) return default;
            float nearest = InteractionRadius * InteractionRadius;
            InteractionCandidate choice = default;
            if (_data.regionId == TopazSaveData.ExpeditionRegion && _expedition != null)
            {
                Consider(ref choice, ref nearest, InteractionKind.ReturnHome,
                    _expedition.Departure, "Return home");
                if (CanClaimExpeditionCache)
                    Consider(ref choice, ref nearest, InteractionKind.SupplyCache,
                        _expedition.SupplyCache, "Open cache");
                return choice;
            }
            if (_data.regionId != TopazSaveData.HomeRegion) return default;
            Consider(ref choice, ref nearest, InteractionKind.Travel, homeGate, "Travel");
            if (chest != null && chest.IsPlaced)
                Consider(ref choice, ref nearest, InteractionKind.Chest,
                    chest.transform, "Open chest");
            if (workbench != null)
            {
                Consider(ref choice, ref nearest, InteractionKind.Workbench, workbench,
                    ChestPlaced ? "Inspect" : PendingChest ? "Place chest" : "Craft");
            }
            Consider(ref choice, ref nearest, InteractionKind.Rest, restPoint, "Rest");
            if (tree != null && tree.IsAvailable &&
                combat.HasHarvestTool(tree.RequiredToolId) && combat.CanStartHarvest)
                Consider(ref choice, ref nearest, InteractionKind.Harvest,
                    tree.transform, "Chop", tree);
            return choice;
        }

        bool CanClaimExpeditionCache => _expedition != null &&
            !_data.expeditionCacheClaimed && _expedition.CacheUnlocked &&
            _backpack.SpaceFor(wood) >= ExpeditionWoodReward;

        void Consider(ref InteractionCandidate choice, ref float nearest,
            InteractionKind kind, Transform target, string verb, HarvestTree harvestTarget = null)
        {
            if (target == null) return;
            float distance = DistanceSquared(target);
            if (distance >= nearest) return;
            nearest = distance;
            Transform anchor = target.Find("Interaction Anchor") ?? target;
            choice = new InteractionCandidate(kind, anchor, verb, harvestTarget);
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
            hud.ShowStatus("Guarded supplies collected: +3 Wood.");
            return true;
        }

        IEnumerator EnterExpedition(bool restoring)
        {
            if (!restoring) Commit();
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
            hud.ShowStatus(restoring ? "Expedition resumed." : "Expedition clearing entered.");
        }

        IEnumerator ReturnHome(bool afterDefeat)
        {
            Commit();
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
            combat?.CancelActiveAttack();
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = position;
            if (controller != null) controller.enabled = true;
            movement.ResetMotion();
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
            hud.ShowStatus("Chest crafted.");
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
            _character.loggingExperience = _data.loggingExperience;
            _character.swordsExperience = _data.swordsExperience;
            _character.equippedTool = _data.equippedTool;
            _character.pendingChest = _data.pendingChest;
            _world.worldHours = _data.worldHours;
            _world.expeditionCacheClaimed = _data.expeditionCacheClaimed;
            _visit.regionId = _data.regionId;
            _visit.playerX = _data.playerX;
            _visit.playerZ = _data.playerZ;
            try { _repository.QueueSave(_profile); }
            catch (Exception error)
            {
                _savingEnabled = false;
                _saveProblem = "Recent progress could not be stored. Earlier progress is safe.";
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
            hud.ShowStatus("Storage chest placed.");
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

        IEnumerator Rest()
        {
            _resting = true;
            const float fadeSeconds = .3f;
            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(1f);
            _data.worldHours += WorldClock.RestHours;
            tree.RefreshForTime();
            look.SetWorldHours(_data.worldHours);
            Commit();
            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(1f - Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(0f);
            _resting = false;
        }

        ItemDefinition ResolveItem(string id) => id == wood.StableId ? wood : null;

        public string ItemName(string id) => ResolveItem(id)?.DisplayName ?? id;
        public int ItemStackLimit(string id) => ResolveItem(id)?.MaxStack ?? 1;
        public Sprite ItemJournalIcon(string id) => ResolveItem(id)?.JournalIcon;

        int CountWood() => _backpack.Count(wood.StableId);

        float DistanceSquared(Transform target)
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
