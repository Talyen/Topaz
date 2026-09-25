using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Topaz.CombatStudy;
using Topaz.AnimationStudy;
using Topaz.Expedition;
using Topaz.Crypt;
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
        [SerializeField] ItemDefinition stone;
        [SerializeField] ItemDefinition iron;
        [SerializeField] ItemDefinition boneFragments;
        [SerializeField] GameObject bonePickupVisual;
        [SerializeField] ItemDefinition pickaxeItem;
        [SerializeField] SkillDefinition[] skillDefinitions;
        [SerializeField] MiningRock[] rocks;
        [SerializeField] HomeBuilds homeBuilds;
        [SerializeField] ItemDefinition[] equipmentItems;
        [SerializeField] Transform gearRack;
        [SerializeField] HarvestTree tree;
        [SerializeField] RecipeDefinition chestRecipe;
        [SerializeField] StructureDefinition chestDefinition;
        [SerializeField] StorageChest chest;
        [SerializeField] SafeZone home;
        [SerializeField] Campfire homeCampfire;
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
        [SerializeField] ItemDefinition cryptStaff;
        [SerializeField] ItemDefinition cryptCrossbow;
        [SerializeField] VisualLookController look;
        [SerializeField] PlayerAppearance appearance;
        [SerializeField] PlayerLantern lantern;

        const float InteractionRadius = 1.4f;
        const float PlacementStep = 0.75f;
        const int ExpeditionWoodReward = 3;
        const string ExpeditionSceneName = "Graveyard";
        const string CryptSceneName = "Crypt";
        const double EnemyReturnHours = 24d;
        const double CacheRefillHours = 72d;
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
        InputAction _rotateBuildAction;
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
        bool _fastTraveling;
        bool _resting;
        bool _recovering;
        bool _applicationPaused;
        float _lastExertionAt = -1000f;
        bool _clockWasActive;
        float _activeSecondsSinceSave;
        ExpeditionSceneBootstrap _expedition;
        CryptSceneBootstrap _crypt;
        WeatherPresentation _weatherPresentation;
        string _weatherCondition = WeatherSchedule.Clear;
        string _eventWeatherOverride;
        string _regionWeatherOverride;
        string _regionWeatherId;
        readonly List<GameObject> _spawnedPickups = new List<GameObject>();
        readonly List<ForagePlant> _foragePlants = new List<ForagePlant>();
        readonly List<RestSpot> _restSpots = new List<RestSpot>();
        readonly List<EnemyCombatant> _boundEnemies = new List<EnemyCombatant>();
        float _nextEnemyCheck;
        float _trailReadyAt;
        StorageChest _openChest;
        CampfireTravelCatalog _travelCatalog;
        ItemDefinition _berries;
        ItemDefinition _mushrooms;
        ItemDefinition _stew;

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
        public string ActiveAppearanceId => _character?.appearanceId ?? CharacterLooks.Rogue;
        public string ActiveWorldId => _world?.id;

        public int CurrentDay => _data == null ? 1 :
            1 + (int)Math.Floor((_data.worldHours - WorldClock.StartingHour) / WorldClock.HoursPerDay);
        public double WorldHours => _data?.worldHours ?? WorldClock.StartingHour;
        public string CurrentWeather => _weatherCondition;
        public string CurrentRegionId => _data?.regionId ?? TopazSaveData.HomeRegion;
        public string ReturnCampfireId => _visit?.lastCampfireId ?? Campfire.HomeId;
        public IReadOnlyList<string> DiscoveredCampfireIds => _visit?.discoveredCampfireIds;
        public bool IsFastTraveling => _fastTraveling;
        public bool IsTravelMenuOpen => hud != null && hud.TravelOpen;
        public string CurrentCampfireId => NearbyCampfire()?.StableId;
        public IReadOnlyList<CampfireTravelCatalog.Destination> TravelDestinations =>
            _visit == null || _travelCatalog == null
                ? Array.Empty<CampfireTravelCatalog.Destination>()
                : _travelCatalog.Destinations.Where(value =>
                    _visit.discoveredCampfireIds.Contains(value.stableId)).ToArray();
        public bool IsRecovering => _recovering;
        public bool IsResting => _resting;
        public bool ExpeditionCacheClaimed => _world != null &&
            _world.graveyardCacheReadyAt > WorldHours;
        public bool CryptShortcutOpen => _world?.cryptShortcutOpen == true;
        public bool CryptCacheClaimed => _world != null &&
            _world.cryptCacheReadyAt > WorldHours;
        public bool CryptMageDefeated => _world?.enemyRespawns?.Any(value =>
            value.spawnId == "crypt.mage") == true;
        public bool CryptRogueCrossbowAwarded => _world?.cryptRogueCrossbowAwarded == true;
        public bool StaffDiscovered => _character?.discoveredSkillIds?.Contains(SkillIds.Staff) == true;
        public bool CrossbowsDiscovered =>
            _character?.discoveredSkillIds?.Contains(SkillIds.Crossbows) == true;
        public int WoodCount => _data == null ? 0 : CountWood();
        public int HomeWoodCount => CountHomeMaterial(wood);
        public IReadOnlyList<ItemStackRecord> BackpackSlots => _backpack?.Slots;
        public float Stamina => _character?.stamina ?? SurvivalRules.BaseStamina;
        public float MaximumStamina => SurvivalRules.Maximum(_character);
        public bool StaminaCueVisible => _character != null &&
            Time.time < _lastExertionAt + 3f;
        public double RestedHoursRemaining => _character?.restedHours ?? 0d;
        public double FoodHoursRemaining => _character?.foodHours ?? 0d;
        public int FoodTier => _character?.foodTier ?? 0;
        public int MushroomsAtFire
        {
            get
            {
                Campfire fire = NearbyCampfire();
                if (fire == null || _backpack == null) return 0;
                StorageChest nearby = homeBuilds.NearestChest(fire.transform.position, 2.2f);
                return _backpack.Count(SurvivalRules.MushroomsId) +
                    (nearby?.Inventory?.Count(SurvivalRules.MushroomsId) ?? 0);
            }
        }
        public EquipmentState Equipped => _character?.equipment;
        public EquipmentStats Stats => _character?.equipment.Total(ResolveItem) ?? default;
        public WeaponDefinition CurrentWeapon => ResolveItem(Equipped?.weaponId)?.Weapon;
        public bool HasWeapon => !string.IsNullOrEmpty(Equipped?.weaponId);
        public bool HasAxe => Equipped != null &&
            (Equipped.toolId == EquipmentState.Axe ||
             Equipped.toolId == HomeForgeCatalog.Axe);
        public bool HasPickaxe => _character != null && pickaxeItem != null &&
            _character.pickaxeId == pickaxeItem.StableId;
        public ItemDefinition StoneItem => stone;
        public ItemDefinition WoodItem => wood;
        public ItemDefinition IronItem => iron;
        public int StoneCount => CountHomeMaterial(stone);
        public int IronCount => CountHomeMaterial(iron);
        public int MiningExperience => SkillExperience(SkillIds.Mining);
        public int MiningLevel => SkillLevel(SkillIds.Mining);
        public string SelectedManualTool => _character?.selectedTool ?? "sword";
        public bool HasShield => Equipped?.offhandId == EquipmentState.Shield &&
            CurrentWeapon?.TwoHanded != true;
        public IReadOnlyList<string> RackItems => new[] { EquipmentState.SwiftGloves,
            EquipmentState.AgileBody, EquipmentState.AgileBoots, EquipmentState.TwoHandedAxe };
        StorageChest CurrentChest => _openChest != null && _openChest.IsPlaced ? _openChest : chest;
        public IReadOnlyList<ItemStackRecord> ChestSlots => CurrentChest?.Inventory?.Slots;
        public bool BackpackHasItems => _backpack != null && _backpack.Slots.Any(slot => slot.count > 0);
        public bool ChestHasItems => CurrentChest?.Inventory != null &&
            CurrentChest.Inventory.Slots.Any(slot => slot.count > 0);
        public bool BackpackHasMaterials => _backpack != null &&
            _backpack.Slots.Any(slot => slot.count > 0 && ResolveMaterial(slot.itemId) != null);
        public bool ChestHasMaterials => CurrentChest?.Inventory != null &&
            CurrentChest.Inventory.Slots.Any(slot => slot.count > 0 &&
                ResolveMaterial(slot.itemId) != null);
        public int LoggingExperience => SkillExperience(SkillIds.Logging);
        public int LoggingLevel => SkillLevel(SkillIds.Logging);
        public int SwordsExperience => SkillExperience(SkillIds.Swords);
        public int SwordsLevel => SkillLevel(SkillIds.Swords);
        public int AxesExperience => SkillExperience(SkillIds.Axes);
        public int AxesLevel => SkillLevel(SkillIds.Axes);
        public int ShieldExperience => SkillExperience(SkillIds.Shield);
        public int ShieldLevel => SkillLevel(SkillIds.Shield);
        public IReadOnlyList<SkillDefinition> SkillDefinitions => skillDefinitions;
        public bool IsAtHome => _data != null && _data.regionId == TopazSaveData.HomeRegion;
        public int PickupCount => _data?.pickups.Count ?? 0;
        public bool PendingChest => _data != null && _data.pendingChest;
        public int ChestCost => chestRecipe != null ? chestRecipe.IngredientCount : 0;
        public bool CanCraftChest => _data != null && !_data.pendingChest && !ChestPlaced &&
            WoodCount >= ChestCost;
        public string EquippedToolName => combat != null ? combat.EquippedToolName : "Sword";
        public bool IsCrossbowEquipped => CurrentWeapon?.Skill == WeaponSkill.Crossbows;
        public float CrossbowReloadProgress => combat?.CrossbowReloadProgress ?? 1f;
        public bool LanternOn => _character != null && _character.lanternOn;
        public bool ChestPlaced => homeBuilds != null &&
            homeBuilds.Chests.Any(value => value != null && value.IsPlaced);
        public int ChestWood => CurrentChest?.Inventory?.Count(wood.StableId) ?? 0;
        public int ChestStone => CurrentChest?.Inventory?.Count(stone.StableId) ?? 0;
        public int ChestIron => CurrentChest?.Inventory?.Count(iron.StableId) ?? 0;
        public int ChestCapacity => CurrentChest?.SlotCapacity ?? 0;
        public bool IsPlacing => _placing;
        public bool MenuOpen => (hud != null && hud.MenuOpen) || (menus != null && menus.BlockGameplay);
        public bool BlockMovement => MenuOpen || _traveling || _resting || _recovering;
        public bool SuppressAttack => MenuOpen || _placing ||
            (homeBuilds != null && homeBuilds.Active) || _traveling || _resting || _recovering;
        public string SaveProblem => _saveProblem;

        void Awake()
        {
            _travelCatalog = Resources.Load<CampfireTravelCatalog>("CampfireTravelCatalog");
            _berries = Resources.Load<ItemDefinition>("RedBerries");
            _mushrooms = Resources.Load<ItemDefinition>("Mushrooms");
            _stew = Resources.Load<ItemDefinition>("MushroomStew");
            _previewProperties = new MaterialPropertyBlock();
            if (controls == null || wood == null || stone == null || iron == null ||
                boneFragments == null || bonePickupVisual == null ||
                _berries == null || _mushrooms == null || _stew == null ||
                pickaxeItem == null || skillDefinitions == null ||
                skillDefinitions.Length != SkillIds.All.Length ||
                skillDefinitions.Any(definition => definition == null) ||
                rocks == null || rocks.Length != 3 ||
                homeBuilds == null || tree == null || chest == null ||
                chestRecipe == null || chestDefinition == null || home == null ||
                homeCampfire == null || homeCampfire.StableId != Campfire.HomeId ||
                movement == null || combat == null || hud == null || pickupPrefab == null ||
                menus == null || homeGate == null ||
                cryptStaff == null || cryptCrossbow == null || look == null ||
                appearance == null || lantern == null || equipmentItems == null ||
                equipmentItems.Length < 13 ||
                gearRack == null)
            {
                Debug.LogError("World session is missing a required reference.", this);
                enabled = false;
                return;
            }
            _weatherPresentation = look.gameObject.GetComponent<WeatherPresentation>();
            if (_weatherPresentation == null)
                _weatherPresentation = look.gameObject.AddComponent<WeatherPresentation>();
            _weatherPresentation.Initialize(look, transform);

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
                Debug.LogError($"[Topaz] {_saveProblem} {error}", this);
            }

            InputActionMap map = controls.FindActionMap("Player", true);
            _placeAction = map.FindAction("Place", true);
            _cancelAction = map.FindAction("Cancel", true);
            _inventoryAction = map.FindAction("Inventory", true);
            _rotateBuildAction = map.FindAction("RotateBuild", true);
        }

        void OnEnable()
        {
            if (_placeAction != null) _placeAction.performed += OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed += OnCancelPerformed;
            if (_inventoryAction != null) _inventoryAction.performed += OnInventoryPerformed;
            if (_rotateBuildAction != null) _rotateBuildAction.performed += OnRotateBuildPerformed;
        }

        void OnDisable()
        {
            if (_placeAction != null) _placeAction.performed -= OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
            if (_inventoryAction != null) _inventoryAction.performed -= OnInventoryPerformed;
            if (_rotateBuildAction != null) _rotateBuildAction.performed -= OnRotateBuildPerformed;
            if (_resting || _recovering) look?.SetRestFade(0f);
            foreach (EnemyCombatant enemy in _boundEnemies)
                if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            _boundEnemies.Clear();
            _resting = false;
            _recovering = false;
            _fastTraveling = false;
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
            else if (visit.regionId == TopazSaveData.CryptRegion)
                StartCoroutine(EnterCrypt(true));
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
            ClearBoundEnemies();
            Scene oldExpedition = SceneManager.GetSceneByName(ExpeditionSceneName);
            if (oldExpedition.isLoaded) yield return SceneManager.UnloadSceneAsync(oldExpedition);
            _expedition = null;
            Scene oldCrypt = SceneManager.GetSceneByName(CryptSceneName);
            if (oldCrypt.isLoaded) yield return SceneManager.UnloadSceneAsync(oldCrypt);
            _crypt = null;
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
            else if (visit.regionId == TopazSaveData.CryptRegion)
                yield return EnterCrypt(true);
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
            _lastExertionAt = -1000f;
            _character.EnsureSkills();
            UnlockMasteredTalents();
            combat.ClearTemporaryProgression();
            _world = world;
            _visit = visit;
            if (visit.lastCampfireId != Campfire.HomeId &&
                visit.lastCampfireId != Campfire.ClearingId &&
                visit.lastCampfireId != Campfire.CryptId)
            {
                visit.lastCampfireId = Campfire.HomeId;
                if (!visit.discoveredCampfireIds.Contains(Campfire.HomeId))
                    visit.discoveredCampfireIds.Add(Campfire.HomeId);
            }
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
            homeBuilds.Cancel();
            _resting = false;
            _recovering = false;
            look.SetRestFade(0f);
            look.SetInterior(visit.regionId == TopazSaveData.CryptRegion);
            bool restoreAway = visit.regionId != TopazSaveData.HomeRegion;
            Teleport(restoreAway ? home.transform.position :
                new Vector3(visit.playerX, 0f, visit.playerZ));
            BindGatherables(SceneManager.GetSceneByName("Bootstrap"));
            homeBuilds.Bind(this, world);
            RefreshGatherables();
            combat.SetManualTool(character.selectedTool);
            _openChest = homeBuilds.Chests.FirstOrDefault();
            if (!appearance.Apply(character.appearanceId))
                appearance.Apply(CharacterLooks.Rogue);
            lantern.SetLit(character.lanternOn);
            foreach (PickupStateRecord pickup in world.pickups)
            {
                ItemDefinition item = ResolveItem(pickup.itemId);
                if (item != null) CreatePickup(pickup, item);
                else Debug.LogWarning($"[Topaz] Unknown saved pickup item: {pickup.itemId}", this);
            }
            if (chestPreview != null) chestPreview.SetActive(false);
            _eventWeatherOverride = null;
            _regionWeatherOverride = null;
            _regionWeatherId = null;
            ApplyIsolatedWeatherPreview();
            look.SetWorldHours(_data.worldHours);
            UpdateWeather(true);
            hud.Bind(this);
            _activeSecondsSinceSave = 0f;
            _clockWasActive = false;
        }

        void ApplyIsolatedWeatherPreview()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Any(arg => arg.Equals("-runTests", StringComparison.OrdinalIgnoreCase)))
                return;
            foreach (string arg in args)
            {
                if (arg.StartsWith("-weather-preview=", StringComparison.OrdinalIgnoreCase))
                {
                    string candidate = arg.Substring("-weather-preview=".Length);
                    if (WeatherSchedule.IsValid(candidate)) _eventWeatherOverride = candidate;
                }
                else if (arg.StartsWith("-weather-preview-hour=", StringComparison.OrdinalIgnoreCase) &&
                    double.TryParse(arg.Substring("-weather-preview-hour=".Length),
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double hour) &&
                    hour >= 0d && hour < WorldClock.HoursPerDay)
                {
                    _data.worldHours = WorldClock.HoursPerDay *
                        Math.Floor(_data.worldHours / WorldClock.HoursPerDay) + hour;
                    if (_data.worldHours < WorldClock.StartingHour)
                        _data.worldHours += WorldClock.HoursPerDay;
                }
            }
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
            if (homeBuilds.Active)
            {
                homeBuilds.Tick(_placeRequested, _cancelRequested);
                _placeRequested = _cancelRequested = _inventoryRequested = false;
                hud?.Tick();
                TickWorldClock();
                CheckCampfire();
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
            CheckCampfire();
        }

        void TickWorldClock()
        {
            if (_data == null) return;
            bool active = !_applicationPaused && !_traveling && !_resting && !_recovering &&
                !MenuOpen &&
                Time.timeScale > 0f;
            if (!active)
            {
                if (_clockWasActive) Commit();
                _clockWasActive = false;
                return;
            }

            _clockWasActive = true;
            double before = _data.worldHours;
            _data.worldHours = WorldClock.Advance(_data.worldHours, Time.deltaTime);
            SurvivalRules.Advance(_character, _data.worldHours - before);
            if (Time.time >= _lastExertionAt + SurvivalRules.RegenerationDelay)
                SurvivalRules.Refill(_character, Time.deltaTime);
            TickEnemyReturns();
            look.SetWorldHours(_data.worldHours);
            UpdateWeather(false);
            _weatherPresentation?.Tick(Time.deltaTime);
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

        /// <summary>Story owners may temporarily force a condition, then pass null to resume.</summary>
        public bool SetEventWeatherOverride(string condition)
        {
            if (condition != null && !WeatherSchedule.IsValid(condition)) return false;
            _eventWeatherOverride = condition;
            UpdateWeather(false);
            return true;
        }

        /// <summary>Region owners may override their active area's presentation.</summary>
        public bool SetRegionWeatherOverride(string regionId, string condition)
        {
            if (condition != null && !WeatherSchedule.IsValid(condition)) return false;
            _regionWeatherId = regionId;
            _regionWeatherOverride = condition;
            UpdateWeather(false);
            return true;
        }

        void UpdateWeather(bool immediate)
        {
            if (_world == null || _data == null) return;
            string region = _regionWeatherId == _data.regionId
                ? _regionWeatherOverride : null;
            string ambient = WeatherSchedule.At(_world.id, _data.worldHours);
            string resolved = _data.regionId == TopazSaveData.CryptRegion
                ? WeatherSchedule.Clear
                : WeatherSchedule.Resolve(ambient, region, _eventWeatherOverride);
            if (resolved == _weatherCondition && !immediate) return;
            _weatherCondition = resolved;
            _weatherPresentation?.SetCondition(resolved, immediate);
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

        public bool DeleteCharacter(string id) => DeleteFromCollection(id, true);

        public bool DeleteWorld(string id) => DeleteFromCollection(id, false);

        bool DeleteFromCollection(string id, bool character)
        {
            if (!_savingEnabled || _profile == null || _traveling || _resting || _recovering ||
                (character ? _profile.Character(id) == null : _profile.World(id) == null))
                return false;

            // A queued active-pair save must finish before the deletion snapshot is written.
            Commit();
            FlushSave();
            if (!_savingEnabled) return false;

            var previousCharacters = new List<TopazCharacterData>(_profile.characters);
            var previousWorlds = new List<TopazWorldData>(_profile.worlds);
            var previousVisits = new List<TopazVisitData>(_profile.visits);
            string previousCharacterId = _profile.lastCharacterId;
            string previousWorldId = _profile.lastWorldId;
            if (character) _profile.DeleteCharacter(id);
            else _profile.DeleteWorld(id);
            try { _repository.Save(_profile); }
            catch (Exception error)
            {
                _profile.characters = previousCharacters;
                _profile.worlds = previousWorlds;
                _profile.visits = previousVisits;
                _profile.lastCharacterId = previousCharacterId;
                _profile.lastWorldId = previousWorldId;
                _savingEnabled = false;
                _saveProblem = "Deletion could not be stored. Previous data is safe.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
                return false;
            }

            if ((character && _character?.id == id) || (!character && _world?.id == id))
            {
                _character = null;
                _world = null;
                _visit = null;
                _data = null;
                _backpack = null;
                _clockWasActive = false;
                chest.Bind(null);
            }
            return true;
        }

        void OnPlacePerformed(InputAction.CallbackContext context) => _placeRequested = true;
        void OnCancelPerformed(InputAction.CallbackContext context) => _cancelRequested = true;
        void OnInventoryPerformed(InputAction.CallbackContext context) => _inventoryRequested = true;
        void OnRotateBuildPerformed(InputAction.CallbackContext context) => homeBuilds?.Rotate();

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
            RecordSkillCompletion(SkillIds.Logging, experience, 1);
        }

        public void RecordMiningStrike()
        {
            RecordSkillCompletion(SkillIds.Mining, 1, 1);
        }

        public void RecordSwordHit(int damage)
        {
            RecordSkillCompletion(SkillIds.Swords, damage, 1);
        }

        public void RecordWeaponHit(WeaponSkill skill, int damage, int sourceLevel = 1)
        {
            RecordSkillCompletion(skill == WeaponSkill.Swords ? SkillIds.Swords :
                    skill == WeaponSkill.Staff ? SkillIds.Staff :
                    skill == WeaponSkill.Crossbows ? SkillIds.Crossbows : SkillIds.Axes,
                damage, sourceLevel);
        }

        public void RecordStaffHit(int damage, int sourceLevel = 1) =>
            RecordSkillCompletion(SkillIds.Staff, damage, sourceLevel);

        public void RecordShieldBlock(int sourceLevel) =>
            AddSkillExperience(SkillIds.Shield, 250, sourceLevel);

        public void RecordSkillCompletion(string skillId, int experience, int sourceLevel) =>
            AddSkillExperience(skillId, experience * 100, sourceLevel);

        void AddSkillExperience(string skillId, int baseCenti, int sourceLevel)
        {
            SkillProgressRecord progress = Progress(skillId);
            if (progress == null) return;
            int award = SkillProgression.Award(baseCenti, progress.Level, sourceLevel);
            if (award <= 0) return;
            int oldLevel = progress.Level;
            progress.experienceCenti = Mathf.Min(SkillProgression.Thresholds[9],
                progress.experienceCenti + award);
            if (progress.Level == 10) UnlockMasteredTalents();
            Commit();
            hud.ShowStatus(progress.Level > oldLevel
                ? $"{Definition(skillId)?.DisplayName ?? skillId} reached level {progress.Level}."
                : $"+{award / 100f:0.##} {Definition(skillId)?.DisplayName ?? skillId} XP");
        }

        SkillProgressRecord Progress(string id) => _character?.Skill(id);
        public int SkillLevel(string id) => Progress(id)?.Level ?? 1;
        public int SkillExperience(string id) => (Progress(id)?.experienceCenti ?? 0) / 100;
        public int SkillExperienceCenti(string id) => Progress(id)?.experienceCenti ?? 0;
        public SkillDefinition Definition(string id)
        {
            if (skillDefinitions == null) return null;
            foreach (SkillDefinition definition in skillDefinitions)
                if (definition != null && definition.StableId == id) return definition;
            return null;
        }
        public bool HasTalent(string skillId, string talentId) =>
            Progress(skillId)?.activeTalentIds.Contains(talentId) == true;
        public float TalentAmount(string skillId, string talentId) =>
            HasTalent(skillId, talentId) ? Definition(skillId)?.Talent(talentId)?.amount ?? 0f : 0f;
        public float TalentDuration(string skillId, string talentId) =>
            HasTalent(skillId, talentId) ?
                Definition(skillId)?.Talent(talentId)?.durationSeconds ?? 0f : 0f;
        public float TalentInterval(string skillId, string talentId) =>
            HasTalent(skillId, talentId) ?
                Definition(skillId)?.Talent(talentId)?.intervalSeconds ?? 0f : 0f;
        public float SkillHandling(string skillId) => Definition(skillId)?.HandlingPerLevel ?? 0f;
        public float SkillMilestoneBenefit(string skillId, int milestone)
        {
            SkillDefinition definition = Definition(skillId);
            return definition == null || SkillLevel(skillId) < milestone ? 0f :
                milestone == 5 ? definition.BenefitAtFive :
                milestone == 10 ? definition.BenefitAtTen : 0f;
        }
        public float SkillOutputBonus(string skillId) =>
            SkillMilestoneBenefit(skillId, 5) + SkillMilestoneBenefit(skillId, 10);
        public bool IsTalentLearned(string skillId, string talentId) =>
            Progress(skillId)?.learnedTalentIds.Contains(talentId) == true;
        public int ActiveTalentCount(string skillId) => Progress(skillId)?.activeTalentIds.Count ?? 0;
        public int SkillChoices(string skillId) => Progress(skillId)?.AvailableChoices ?? 0;

        public bool TryLearnTalent(string skillId, string talentId)
        {
            SkillProgressRecord progress = Progress(skillId);
            SkillDefinition definition = Definition(skillId);
            if (progress == null || definition?.Talent(talentId) == null ||
                progress.Level >= 10 || progress.AvailableChoices <= 0 ||
                progress.learnedTalentIds.Contains(talentId)) return false;
            progress.learnedTalentIds.Add(talentId);
            if (progress.activeTalentIds.Count < (progress.Level >= 5 ? 2 : 1))
                progress.activeTalentIds.Add(talentId);
            Commit();
            return true;
        }

        public bool TrySetTalentActive(string skillId, string talentId, bool active)
        {
            SkillProgressRecord progress = Progress(skillId);
            if (!IsAtHome || progress == null || !progress.learnedTalentIds.Contains(talentId))
                return false;
            if (active && !progress.activeTalentIds.Contains(talentId))
            {
                if (progress.activeTalentIds.Count >= (progress.Level >= 5 ? 2 : 1)) return false;
                progress.activeTalentIds.Add(talentId);
            }
            else if (!active) progress.activeTalentIds.Remove(talentId);
            if (skillId == SkillIds.Axes) combat.ClearTemporaryProgression();
            Commit();
            return true;
        }

        void UnlockMasteredTalents()
        {
            if (_character == null || skillDefinitions == null) return;
            foreach (SkillDefinition definition in skillDefinitions)
            {
                if (definition == null) continue;
                SkillProgressRecord progress = Progress(definition.StableId);
                if (progress == null || progress.Level < 10) continue;
                foreach (TalentDefinition talent in definition.Talents)
                    if (!progress.learnedTalentIds.Contains(talent.id))
                        progress.learnedTalentIds.Add(talent.id);
            }
        }

        public void DropHarvest(HarvestDefinition definition, Vector3 position)
        {
            if (definition == null || definition.YieldItem == null) return;
            int bonus = Mathf.RoundToInt(SkillOutputBonus(SkillIds.Logging) +
                TalentAmount(SkillIds.Logging, "logging.clean-fell"));
            DropItem(definition.YieldItem, definition.YieldCount + bonus, position);
        }

        public void DropMining(Vector3 position)
        {
            DropMining(position, null);
        }

        public void DropMining(Vector3 position, MiningDefinition definition)
        {
            int stones = (definition != null ? definition.StoneYield : 3) +
                Mathf.RoundToInt(SkillMilestoneBenefit(SkillIds.Mining, 5) +
                    TalentAmount(SkillIds.Mining, "mining.stone-lode"));
            int irons = (definition != null ? definition.IronYield : 1) +
                Mathf.RoundToInt(SkillMilestoneBenefit(SkillIds.Mining, 10) +
                    TalentAmount(SkillIds.Mining, "mining.iron-seeker"));
            DropItem(stone, stones, position + Vector3.left * 0.38f);
            DropItem(iron, irons, position + Vector3.right * 0.38f);
            hud.ShowStatus($"{stones} Stone and {irons} Iron dropped.");
        }

        public bool TryManualMine(Vector3 position, Vector3 direction, float range,
            float arcDegrees)
        {
            if (_data == null) return false;
            foreach (MiningRock rock in FindObjectsByType<MiningRock>())
                if (rock != null && IsNodeInCurrentRegion(rock.gameObject) &&
                    rock.TryStrike(position, direction, range, arcDegrees))
                    return true;
            return false;
        }

        public bool TryFindGatherTarget(Vector3 position, float treeRange, float rockRange,
            out HarvestTree targetTree, out MiningRock targetRock)
        {
            targetTree = null;
            targetRock = null;
            if (_data == null) return false;
            float nearest = float.PositiveInfinity;
            foreach (HarvestTree candidate in
                     FindObjectsByType<HarvestTree>())
            {
                if (treeRange <= 0f || !candidate.IsAvailable ||
                    !IsNodeInCurrentRegion(candidate.gameObject) ||
                    !combat.HasHarvestTool(candidate.RequiredToolId)) continue;
                Vector3 distance = candidate.transform.position - position;
                distance.y = 0f;
                float squared = distance.sqrMagnitude;
                if (squared < .001f || squared > treeRange * treeRange || squared >= nearest)
                    continue;
                nearest = squared;
                targetTree = candidate;
                targetRock = null;
            }
            foreach (MiningRock candidate in
                     FindObjectsByType<MiningRock>())
            {
                if (rockRange <= 0f || !candidate.IsAvailable ||
                    !IsNodeInCurrentRegion(candidate.gameObject)) continue;
                Vector3 distance = candidate.transform.position - position;
                distance.y = 0f;
                float squared = distance.sqrMagnitude;
                if (squared < .001f || squared > rockRange * rockRange || squared >= nearest)
                    continue;
                nearest = squared;
                targetTree = null;
                targetRock = candidate;
            }
            return targetTree != null || targetRock != null;
        }

        bool IsNodeInCurrentRegion(GameObject node) => node != null &&
            node.scene.name == (_data.regionId == TopazSaveData.ExpeditionRegion
                ? ExpeditionSceneName : _data.regionId == TopazSaveData.CryptRegion
                    ? CryptSceneName : "Bootstrap");

        void BindGatherables(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (HarvestTree node in root.GetComponentsInChildren<HarvestTree>(true))
                    node.Bind(this);
                foreach (MiningRock node in root.GetComponentsInChildren<MiningRock>(true))
                    node.Bind(this);
                foreach (ForagePlant node in root.GetComponentsInChildren<ForagePlant>(true))
                {
                    node.Bind(this);
                    if (!_foragePlants.Contains(node)) _foragePlants.Add(node);
                }
                foreach (RestSpot bed in root.GetComponentsInChildren<RestSpot>(true))
                    if (!_restSpots.Contains(bed)) _restSpots.Add(bed);
            }
        }

        void RefreshGatherables()
        {
            foreach (ForagePlant node in FindObjectsByType<ForagePlant>(
                FindObjectsInactive.Include))
                node.RefreshForTime();
            foreach (HarvestTree node in
                     FindObjectsByType<HarvestTree>())
                node.RefreshForTime();
            foreach (MiningRock node in
                     FindObjectsByType<MiningRock>())
                node.RefreshForTime();
        }

        void DropItem(ItemDefinition item, int count, Vector3 position)
        {
            if (item == null || count <= 0) return;
            var record = new PickupStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = item.StableId,
                regionId = _data.regionId,
                count = count,
                x = position.x,
                z = position.z
            };
            _data.pickups.Add(record);
            CreatePickup(record, item);
            hud.ShowStatus($"{record.count} {item.DisplayName} dropped.");
        }

        public void RegisterEnemy(EnemyCombatant enemy)
        {
            if (enemy == null || _world == null) return;
            if (string.IsNullOrEmpty(enemy.SpawnId))
            {
                Debug.LogError($"[Topaz/Combat] {enemy.name} has no stable spawn ID.", enemy);
                return;
            }
            if (_boundEnemies.Contains(enemy)) return;
            if (_boundEnemies.Any(other => other != null && other.SpawnId == enemy.SpawnId))
            {
                Debug.LogError($"[Topaz/Combat] Duplicate spawn ID {enemy.SpawnId}.", enemy);
                return;
            }
            _boundEnemies.Add(enemy);
            enemy.Defeated += OnEnemyDefeated;
            EnemyRespawnRecord pending = _world.enemyRespawns.Find(value =>
                value.spawnId == enemy.SpawnId);
            if (pending != null) enemy.SetDefeatedForPersistence();
        }

        void ClearBoundEnemies()
        {
            foreach (EnemyCombatant enemy in _boundEnemies)
                if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            _boundEnemies.Clear();
        }

        void OnEnemyDefeated(EnemyCombatant enemy)
        {
            if (_world == null || _data == null || enemy == null ||
                string.IsNullOrEmpty(enemy.SpawnId)) return;
            EnemyRespawnRecord pending = _world.enemyRespawns.Find(value =>
                value.spawnId == enemy.SpawnId);
            if (pending == null)
            {
                pending = new EnemyRespawnRecord { spawnId = enemy.SpawnId };
                _world.enemyRespawns.Add(pending);
            }
            pending.readyAtWorldHours = WorldHours + EnemyReturnHours;
            Vector3 position = enemy.transform.position;
            DropItem(boneFragments, 1, position + Vector3.left * .35f);
            ItemDefinition extra = null;
            switch (enemy.LootRole)
            {
                case SkeletonLootRole.Warrior:
                    extra = ResolveItem(UnityEngine.Random.value < .5f
                        ? EquipmentState.Sword : EquipmentState.Shield);
                    break;
                case SkeletonLootRole.Rogue:
                    extra = cryptCrossbow;
                    break;
                case SkeletonLootRole.Mage:
                    extra = cryptStaff;
                    break;
            }
            if (extra != null) DropItem(extra, 1, position + Vector3.right * .35f);
            if (enemy.LootRole == SkeletonLootRole.Mage) _crypt?.SetMageAlive(false);
            Commit();
        }

        void TickEnemyReturns()
        {
            if (_world == null || Time.unscaledTime < _nextEnemyCheck) return;
            _nextEnemyCheck = Time.unscaledTime + 1f;
            Camera camera = Camera.main;
            foreach (EnemyCombatant enemy in _boundEnemies)
            {
                if (enemy == null || !enemy.IsDown) continue;
                EnemyRespawnRecord pending = _world.enemyRespawns.Find(value =>
                    value.spawnId == enemy.SpawnId);
                if (pending == null || WorldHours < pending.readyAtWorldHours ||
                    !SpawnOutOfView(enemy.transform.position, camera)) continue;
                enemy.ResetForRecovery();
                if (enemy.IsDown) continue;
                _world.enemyRespawns.Remove(pending);
                if (enemy.LootRole == SkeletonLootRole.Mage) _crypt?.SetMageAlive(true);
                Commit();
            }
        }

        bool SpawnOutOfView(Vector3 position, Camera camera)
        {
            if ((position - transform.position).sqrMagnitude < 12f * 12f) return false;
            if (camera == null) return true;
            Vector3 viewport = camera.WorldToViewportPoint(position + Vector3.up);
            return viewport.z <= 0f || viewport.x < -.1f || viewport.x > 1.1f ||
                viewport.y < -.1f || viewport.y > 1.1f;
        }

        public void TryCollect(WorldPickup pickup)
        {
            if (pickup == null || pickup.State == null || pickup.Item == null) return;
            int accepted = _backpack.Add(pickup.Item, pickup.State.count);
            if (accepted <= 0) return;
            DiscoverWeaponSkill(pickup.Item);
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
            if (item == cryptStaff || item == cryptCrossbow)
                pickup.OverrideVisual(item.Weapon?.HeldModel);
            else if (item == boneFragments)
                pickup.OverrideVisual(bonePickupVisual);
            instance.SetActive(string.IsNullOrEmpty(record.regionId) ||
                record.regionId == _data.regionId);
        }

        void RefreshPickupVisibility()
        {
            foreach (GameObject instance in _spawnedPickups)
            {
                if (instance == null) continue;
                WorldPickup pickup = instance.GetComponent<WorldPickup>();
                instance.SetActive(pickup?.State != null &&
                    (string.IsNullOrEmpty(pickup.State.regionId) ||
                     pickup.State.regionId == _data.regionId));
            }
        }

        public bool TryInteract()
        {
            if (_traveling || _resting || _recovering) return true;
            if (menus.BlockGameplay) return true;
            if (_placing || homeBuilds.Active) return true;
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
                    StartCoroutine(ReturnHome());
                    return true;
                case InteractionKind.ReturnFromCrypt:
                    StartCoroutine(ReturnFromCrypt());
                    return true;
                case InteractionKind.SupplyCache:
                    TryClaimExpeditionCache();
                    return true;
                case InteractionKind.Travel:
                    StartCoroutine(EnterExpedition(false));
                    return true;
                case InteractionKind.EnterCrypt:
                    StartCoroutine(EnterCrypt(false));
                    return true;
                case InteractionKind.CryptLever:
                    _world.cryptShortcutOpen = true;
                    _crypt.SetShortcutOpen(true);
                    Commit();
                    hud.ShowStatus("Shortcut opened.");
                    return true;
                case InteractionKind.CryptCache:
                    TryClaimCryptCache();
                    return true;
                case InteractionKind.Workbench:
                    if (PendingChest) EnterPlacement();
                    else hud.ShowCraftPanel();
                    return true;
                case InteractionKind.Rest:
                    StartCoroutine(Rest());
                    return true;
                case InteractionKind.Chest:
                    _openChest = homeBuilds.NearestChest(transform.position, InteractionRadius);
                    hud.ShowChestPanel();
                    return true;
                case InteractionKind.CampfireUpgrade:
                    hud.ShowCampfirePanel();
                    return true;
                case InteractionKind.CampfireTravel:
                    hud.ShowTravelPanel();
                    return true;
                case InteractionKind.Anvil:
                    hud.ShowSmithingPanel();
                    return true;
                case InteractionKind.GearRack:
                    hud.ShowGearRackPanel();
                    return true;
                case InteractionKind.Harvest:
                    combat.TryStartHarvest(candidate.harvestTarget,
                        candidate.harvestTarget.RequiredToolId);
                    return true;
                case InteractionKind.Mine:
                    combat.TryStartMining(candidate.miningTarget);
                    return true;
                case InteractionKind.Forage:
                    candidate.forageTarget.TryForage();
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

        enum InteractionKind { None, ReturnHome, ReturnFromCrypt, SupplyCache, Travel,
            EnterCrypt, CryptLever, CryptCache, Chest, Workbench, Rest, Harvest, Mine,
            GearRack, CampfireUpgrade, CampfireTravel, Anvil, Forage }

        struct InteractionCandidate
        {
            public InteractionKind kind;
            public Transform anchor;
            public string verb;
            public HarvestTree harvestTarget;
            public MiningRock miningTarget;
            public ForagePlant forageTarget;

            public InteractionCandidate(InteractionKind kind, Transform anchor, string verb,
                HarvestTree harvestTarget = null, MiningRock miningTarget = null,
                ForagePlant forageTarget = null)
            {
                this.kind = kind;
                this.anchor = anchor;
                this.verb = verb;
                this.harvestTarget = harvestTarget;
                this.miningTarget = miningTarget;
                this.forageTarget = forageTarget;
            }
        }

        public bool TryGetInteraction(out Transform anchor, out string verb)
        {
            anchor = null;
            verb = null;
            if (_data == null || _traveling || _resting || _recovering || _placing ||
                homeBuilds.Active || MenuOpen ||
                combat.IsAttackLocked || movement.IsDodging || movement.IsAirborne) return false;
            InteractionCandidate candidate = CurrentInteraction(false);
            if (candidate.kind == InteractionKind.None) return false;
            anchor = candidate.anchor;
            verb = candidate.verb;
            return anchor != null;
        }

        InteractionCandidate CurrentInteraction(bool includeGatherables = true)
        {
            if (_data == null) return default;
            float nearest = InteractionRadius * InteractionRadius;
            InteractionCandidate choice = default;
            foreach (ForagePlant plant in _foragePlants)
                if (plant != null && plant.IsAvailable &&
                    IsNodeInCurrentRegion(plant.gameObject))
                    Consider(ref choice, ref nearest, InteractionKind.Forage,
                        plant.transform, plant.Mushrooms ? "Gather mushrooms" :
                            "Pick red berries", forageTarget: plant);
            foreach (RestSpot bed in _restSpots)
                if (bed != null && IsNodeInCurrentRegion(bed.gameObject))
                    Consider(ref choice, ref nearest, InteractionKind.Rest,
                        bed.transform, "Rest");
            if (_data.regionId == TopazSaveData.ExpeditionRegion && _expedition != null)
            {
                ConsiderCampfire(ref choice, ref nearest, _expedition.Campfire);
                Consider(ref choice, ref nearest, InteractionKind.SupplyCache,
                    _expedition.SupplyCache, ExpeditionCacheClaimed ? "Inspect cache" : "Open cache");
                Consider(ref choice, ref nearest, InteractionKind.EnterCrypt,
                    _expedition.CryptEntrance, "Enter crypt");
                return choice;
            }
            if (_data.regionId == TopazSaveData.CryptRegion && _crypt != null)
            {
                ConsiderCampfire(ref choice, ref nearest, _crypt.Campfire);
                Consider(ref choice, ref nearest, InteractionKind.ReturnFromCrypt,
                    _crypt.Departure, "Exit crypt");
                if (!_world.cryptShortcutOpen)
                    Consider(ref choice, ref nearest, InteractionKind.CryptLever,
                        _crypt.Lever, "Open shortcut");
                Consider(ref choice, ref nearest, InteractionKind.CryptCache,
                    _crypt.MaterialCache, CryptCacheClaimed ? "Inspect cache" : "Open cache");
                return choice;
            }
            if (_data.regionId != TopazSaveData.HomeRegion) return default;
            StorageChest nearestChest = homeBuilds.NearestChest(transform.position,
                InteractionRadius);
            if (nearestChest != null)
                Consider(ref choice, ref nearest, InteractionKind.Chest,
                    nearestChest.transform, "Open chest");
            if (workbench != null)
            {
                Consider(ref choice, ref nearest, InteractionKind.Workbench, workbench,
                    ChestPlaced ? "Inspect" : PendingChest ? "Place chest" : "Craft");
            }
            Consider(ref choice, ref nearest, InteractionKind.GearRack, gearRack, "Inspect gear");
            Consider(ref choice, ref nearest, InteractionKind.Rest,
                homeBuilds.NearestRest(transform.position, InteractionRadius), "Rest");
            ConsiderCampfire(ref choice, ref nearest, homeCampfire);
            Consider(ref choice, ref nearest, InteractionKind.Anvil,
                homeBuilds.NearestAnvil(transform.position, InteractionRadius), "Forge gear");
            if (includeGatherables && tree != null && tree.IsAvailable &&
                combat.HasHarvestTool(tree.RequiredToolId) && combat.CanStartHarvest)
                Consider(ref choice, ref nearest, InteractionKind.Harvest,
                    tree.transform, "Chop", tree);
            if (includeGatherables && combat.CanStartHarvest && HasPickaxe)
                foreach (MiningRock rock in rocks)
                    if (rock != null && rock.IsAvailable)
                        Consider(ref choice, ref nearest, InteractionKind.Mine,
                            rock.transform, "Mine", null, rock);
            return choice;
        }

        void Consider(ref InteractionCandidate choice, ref float nearest,
            InteractionKind kind, Transform target, string verb, HarvestTree harvestTarget = null,
            MiningRock miningTarget = null, ForagePlant forageTarget = null)
        {
            if (target == null) return;
            float distance = DistanceSquared(target);
            if (distance >= nearest) return;
            nearest = distance;
            Transform anchor = target.Find("Interaction Anchor") ?? target;
            choice = new InteractionCandidate(kind, anchor, verb, harvestTarget,
                miningTarget, forageTarget);
        }

        void ConsiderCampfire(ref InteractionCandidate choice, ref float nearest, Campfire fire)
        {
            if (fire != null && fire.Contains(transform.position))
                Consider(ref choice, ref nearest, InteractionKind.CampfireTravel,
                    fire.transform, "Travel / Cook");
        }

        Campfire NearbyCampfire()
        {
            if (_data == null) return null;
            Campfire fire = _data.regionId == TopazSaveData.HomeRegion ? homeCampfire :
                _data.regionId == TopazSaveData.CryptRegion ? _crypt?.Campfire :
                _expedition?.Campfire;
            return fire != null && fire.Contains(transform.position) ? fire : null;
        }

        public bool TryFastTravel(string destinationId)
        {
            Campfire source = NearbyCampfire();
            CampfireTravelCatalog.Destination destination = _travelCatalog?.Find(destinationId);
            if (source == null || destination == null || _visit == null ||
                !_visit.discoveredCampfireIds.Contains(destinationId) ||
                source.StableId == destinationId || _traveling || _resting || _recovering ||
                hud == null || !hud.TravelOpen) return false;
            Commit();
            StartCoroutine(FastTravel(destination));
            return true;
        }

        IEnumerator FastTravel(CampfireTravelCatalog.Destination destination)
        {
            _traveling = true;
            _fastTraveling = true;
            hud.ClosePanels();
            combat.CancelActiveAttack();
            yield return FadeTrail(true);

            Scene targetScene = SceneManager.GetSceneByName(destination.sceneName);
            bool loadedForTravel = !targetScene.isLoaded;
            if (loadedForTravel)
            {
                AsyncOperation load = null;
                try { load = SceneManager.LoadSceneAsync(destination.sceneName,
                    LoadSceneMode.Additive); }
                catch (Exception error) { Debug.LogError($"Campfire travel load failed: {error}", this); }
                if (load != null) yield return load;
                targetScene = SceneManager.GetSceneByName(destination.sceneName);
            }

            Campfire target = FindCampfire(targetScene, destination.stableId);
            ExpeditionSceneBootstrap expeditionTarget = destination.regionId ==
                TopazSaveData.ExpeditionRegion
                    ? FindInScene<ExpeditionSceneBootstrap>(targetScene) : null;
            CryptSceneBootstrap cryptTarget = destination.regionId ==
                TopazSaveData.CryptRegion ? FindInScene<CryptSceneBootstrap>(targetScene) : null;
            bool supported = destination.regionId == TopazSaveData.HomeRegion ||
                (destination.regionId == TopazSaveData.ExpeditionRegion &&
                 expeditionTarget != null) ||
                (destination.regionId == TopazSaveData.CryptRegion && cryptTarget != null);
            if (target == null || target.RegionId != destination.regionId ||
                !target.HasArrival || !supported)
            {
                if (loadedForTravel && targetScene.isLoaded)
                    yield return SceneManager.UnloadSceneAsync(targetScene);
                yield return FadeTrail(false);
                _traveling = _fastTraveling = false;
                hud.ShowStatus("That destination is unavailable.");
                yield break;
            }

            ClearBoundEnemies();
            PlayerVitality vitality = GetComponent<PlayerVitality>();
            if (destination.regionId == TopazSaveData.ExpeditionRegion)
            {
                _expedition = expeditionTarget;
                _expedition.Bind(this, vitality, home, ExpeditionCacheClaimed);
                BindGatherables(targetScene);
            }
            else if (destination.regionId == TopazSaveData.CryptRegion)
            {
                _crypt = cryptTarget;
                _crypt.Bind(this, vitality, home);
            }

            if (destination.regionId == TopazSaveData.HomeRegion)
                vitality?.PreserveHealthOnHomeArrival();
            Teleport(target.ArrivalPosition);
            _data.regionId = destination.regionId;
            _visit.lastCampfireId = destination.stableId;
            look.SetInterior(destination.regionId == TopazSaveData.CryptRegion);
            RefreshPickupVisibility();
            UpdateWeather(false);

            foreach (string sceneName in new[] { ExpeditionSceneName, CryptSceneName })
            {
                if (sceneName == destination.sceneName) continue;
                Scene other = SceneManager.GetSceneByName(sceneName);
                if (other.isLoaded) yield return SceneManager.UnloadSceneAsync(other);
            }
            if (destination.regionId != TopazSaveData.ExpeditionRegion) _expedition = null;
            if (destination.regionId != TopazSaveData.CryptRegion) _crypt = null;
            _traveling = false;
            Commit();
            yield return FadeTrail(false);
            _fastTraveling = false;
        }

        Campfire FindCampfire(Scene scene, string stableId)
        {
            if (!scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Campfire fire in root.GetComponentsInChildren<Campfire>(true))
                if (fire.StableId == stableId) return fire;
            return null;
        }

        T FindInScene<T>(Scene scene) where T : Component
        {
            if (!scene.isLoaded) return null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null) return result;
            }
            return null;
        }

        void CheckCampfire()
        {
            if (_data == null || _traveling || _resting || _recovering) return;
            Campfire campfire = _data.regionId == TopazSaveData.HomeRegion
                ? homeCampfire : _data.regionId == TopazSaveData.CryptRegion
                    ? _crypt?.Campfire : _expedition?.Campfire;
            if (campfire == null || !campfire.Contains(transform.position) ||
                (campfire.StableId == _visit.lastCampfireId &&
                 _visit.discoveredCampfireIds.Contains(campfire.StableId))) return;

            bool discovered = !_visit.discoveredCampfireIds.Contains(campfire.StableId);
            if (discovered) _visit.discoveredCampfireIds.Add(campfire.StableId);
            _visit.lastCampfireId = campfire.StableId;
            campfire.ShowActivation();
            Commit();
            hud.ShowStatus(discovered ? "Campfire discovered. Return point set." :
                "Return point set.");
        }

        public bool RecoverAfterDefeat(PlayerVitality vitality)
        {
            if (_data == null || _recovering || vitality == null) return false;
            StartCoroutine(RecoverAtCampfire(vitality));
            return true;
        }

        IEnumerator RecoverAtCampfire(PlayerVitality vitality)
        {
            _recovering = true;
            _placing = false;
            if (chestPreview != null) chestPreview.SetActive(false);
            hud.ClosePanels();
            combat.CancelActiveAttack();
            Commit(); // Preserve earned progress before a region load or transition can interrupt recovery.

            // The hit beat gives the current reaction time to read before the move is hidden.
            yield return new WaitForSecondsRealtime(.22f);
            const float fadeSeconds = .34f;
            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(1f);

            string targetId = _visit.lastCampfireId;
            string targetRegion = targetId == Campfire.CryptId ? TopazSaveData.CryptRegion :
                targetId == Campfire.ClearingId ? TopazSaveData.ExpeditionRegion :
                TopazSaveData.HomeRegion;
            if (targetRegion != _data.regionId) ClearBoundEnemies();
            if (targetId == Campfire.ClearingId && _expedition == null)
            {
                _traveling = true;
                Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
                if (!scene.isLoaded)
                {
                    AsyncOperation load = SceneManager.LoadSceneAsync(
                        ExpeditionSceneName, LoadSceneMode.Additive);
                    if (load != null) yield return load;
                }
                _expedition = FindAnyObjectByType<ExpeditionSceneBootstrap>();
                if (_expedition != null)
                {
                    _expedition.Bind(this, vitality, home, ExpeditionCacheClaimed);
                    BindGatherables(SceneManager.GetSceneByName(ExpeditionSceneName));
                }
            }

            if (targetId == Campfire.CryptId && _crypt == null)
            {
                _traveling = true;
                Scene cryptScene = SceneManager.GetSceneByName(CryptSceneName);
                if (!cryptScene.isLoaded)
                {
                    AsyncOperation load = SceneManager.LoadSceneAsync(
                        CryptSceneName, LoadSceneMode.Additive);
                    if (load != null) yield return load;
                }
                _crypt = FindAnyObjectByType<CryptSceneBootstrap>();
                _crypt?.Bind(this, vitality, home);
            }

            Campfire target = targetId == Campfire.ClearingId
                ? _expedition?.Campfire : targetId == Campfire.CryptId
                    ? _crypt?.Campfire : homeCampfire;
            if (target == null || target.StableId != targetId)
            {
                target = homeCampfire;
                _visit.lastCampfireId = Campfire.HomeId;
                if (!_visit.discoveredCampfireIds.Contains(Campfire.HomeId))
                    _visit.discoveredCampfireIds.Add(Campfire.HomeId);
            }

            if (target.RegionId == TopazSaveData.HomeRegion)
            {
                Teleport(target.ArrivalPosition);
                _data.regionId = TopazSaveData.HomeRegion;
                Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
                _expedition = null;
                if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
                Scene cryptScene = SceneManager.GetSceneByName(CryptSceneName);
                _crypt = null;
                if (cryptScene.isLoaded) yield return SceneManager.UnloadSceneAsync(cryptScene);
            }
            else
            {
                Teleport(target.ArrivalPosition);
                _data.regionId = target.RegionId;
                Scene other = SceneManager.GetSceneByName(target.RegionId ==
                    TopazSaveData.CryptRegion ? ExpeditionSceneName : CryptSceneName);
                if (other.isLoaded) yield return SceneManager.UnloadSceneAsync(other);
                if (target.RegionId == TopazSaveData.CryptRegion) _expedition = null;
                else _crypt = null;
            }

            RefreshPickupVisibility();
            look.SetInterior(_data.regionId == TopazSaveData.CryptRegion);

            // Defeat advances eight World hours; enemy deadlines remain authoritative.
            vitality.RestoreAfterRecovery();
            _data.worldHours += WorldClock.RestHours;
            SurvivalRules.Advance(_character, WorldClock.RestHours);
            _character.stamina = SurvivalRules.Maximum(_character);
            RefreshGatherables();
            look.SetWorldHours(_data.worldHours);
            UpdateWeather(true);
            _traveling = false;
            Commit();

            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(1f - Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(0f);
            _recovering = false;
            hud.ShowStatus("Recovered at the Campfire.");
        }

        bool TryClaimExpeditionCache()
        {
            if (_expedition == null) return false;
            if (ExpeditionCacheClaimed)
            {
                hud.ShowStatus("The Graveyard cache is empty for now.");
                return false;
            }
            if (_backpack.SpaceFor(wood) < ExpeditionWoodReward)
            {
                hud.ShowStatus("Make room for 3 Wood before opening the cache.");
                return false;
            }

            _backpack.Add(wood, ExpeditionWoodReward);
            _world.graveyardCacheReadyAt = WorldHours + CacheRefillHours;
            _expedition.SetCacheStocked(false);
            Commit();
            hud.ShowStatus("Graveyard supplies collected: +3 Wood.");
            return true;
        }

        bool TryClaimCryptCache()
        {
            if (_crypt == null) return false;
            if (CryptCacheClaimed)
            {
                hud.ShowStatus("The crypt cache is empty for now.");
                return false;
            }
            var trial = new InventorySlots(_backpack.Slots.Select(slot => new ItemStackRecord {
                itemId = slot.itemId, count = slot.count }).ToList(), BackpackCapacity);
            if (trial.Add(stone, 2) != 2 || trial.Add(iron, 1) != 1)
            {
                hud.ShowStatus("Make room for 2 Stone and 1 Iron.");
                return false;
            }
            _backpack.Add(stone, 2);
            _backpack.Add(iron, 1);
            _world.cryptCacheReadyAt = WorldHours + CacheRefillHours;
            _crypt.SetCacheStocked(false);
            Commit();
            hud.ShowStatus("2 Stone and 1 Iron collected.");
            return true;
        }

        IEnumerator EnterExpedition(bool restoring)
        {
            combat.ClearTemporaryProgression();
            if (!restoring) Commit();
            _traveling = true;
            hud.ClosePanels();
            if (!restoring) yield return FadeTrail(true);
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
                _expedition.Departure == null || _expedition.CryptEntrance == null ||
                _expedition.SupplyCache == null ||
                _expedition.Campfire == null)
            {
                RestoreHomeAfterTravelFailure();
                yield break;
            }
            _expedition.Bind(this, GetComponent<PlayerVitality>(), home,
                ExpeditionCacheClaimed);
            BindGatherables(SceneManager.GetSceneByName(ExpeditionSceneName));
            Teleport(restoring ? new Vector3(_data.playerX, 0f, _data.playerZ) :
                _expedition.Arrival.position);
            _data.regionId = TopazSaveData.ExpeditionRegion;
            look.SetInterior(false);
            RefreshPickupVisibility();
            UpdateWeather(false);
            if (!restoring) yield return FadeTrail(false);
            _trailReadyAt = Time.time + 1.5f;
            _traveling = false;
            Commit();
            hud.ShowStatus(restoring ? "Graveyard resumed." : "Graveyard entered.");
        }

        IEnumerator EnterCrypt(bool restoring)
        {
            if (!restoring && (_data.regionId != TopazSaveData.ExpeditionRegion ||
                               _expedition == null)) yield break;
            combat.ClearTemporaryProgression();
            if (!restoring) Commit();
            _traveling = true;
            hud.ClosePanels();
            if (!restoring) yield return FadeTrail(true);
            Scene scene = SceneManager.GetSceneByName(CryptSceneName);
            if (!scene.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(CryptSceneName,
                    LoadSceneMode.Additive);
                if (load == null)
                {
                    RestoreHomeAfterTravelFailure();
                    yield break;
                }
                yield return load;
            }
            _crypt = FindAnyObjectByType<CryptSceneBootstrap>();
            if (_crypt == null || _crypt.Arrival == null || _crypt.Departure == null ||
                _crypt.Campfire == null || _crypt.Lever == null ||
                _crypt.MaterialCache == null)
            {
                RestoreHomeAfterTravelFailure();
                yield break;
            }
            if (!restoring) ClearBoundEnemies();
            _crypt.Bind(this, GetComponent<PlayerVitality>(), home);
            Vector3 arrival = restoring
                ? new Vector3(_data.playerX, 0f, _data.playerZ)
                : _crypt.Arrival.position;
            if (restoring && !CryptMageDefeated && _crypt.InMageArena(arrival))
                arrival = _crypt.Campfire.ArrivalPosition;
            Teleport(arrival);
            _data.regionId = TopazSaveData.CryptRegion;
            look.SetInterior(true);
            RefreshPickupVisibility();
            Scene graveyard = SceneManager.GetSceneByName(ExpeditionSceneName);
            if (graveyard.isLoaded) yield return SceneManager.UnloadSceneAsync(graveyard);
            _expedition = null;
            UpdateWeather(false);
            if (!restoring) yield return FadeTrail(false);
            _traveling = false;
            Commit();
            hud.ShowStatus(restoring ? "Crypt resumed." : "Crypt entered.");
        }

        IEnumerator ReturnHome()
        {
            if (_data.regionId != TopazSaveData.ExpeditionRegion) yield break;
            combat.ClearTemporaryProgression();
            Commit();
            _traveling = true;
            yield return FadeTrail(true);
            Teleport(homeGate.position + Vector3.forward * 1.7f);
            _data.regionId = TopazSaveData.HomeRegion;
            look.SetInterior(false);
            Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
            ClearBoundEnemies();
            _expedition = null;
            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
            RefreshPickupVisibility();
            UpdateWeather(false);
            yield return FadeTrail(false);
            _trailReadyAt = Time.time + 1.5f;
            _traveling = false;
            Commit();
            hud.ShowStatus("Returned home with your supplies.");
        }

        IEnumerator ReturnFromCrypt()
        {
            if (_data.regionId != TopazSaveData.CryptRegion) yield break;
            combat.ClearTemporaryProgression();
            Commit();
            _traveling = true;
            yield return FadeTrail(true);
            Scene scene = SceneManager.GetSceneByName(ExpeditionSceneName);
            if (!scene.isLoaded)
            {
                AsyncOperation load = SceneManager.LoadSceneAsync(ExpeditionSceneName,
                    LoadSceneMode.Additive);
                if (load == null)
                {
                    look.SetRestFade(0f);
                    _traveling = false;
                    hud.ShowStatus("The Graveyard could not be loaded.");
                    yield break;
                }
                yield return load;
            }
            _expedition = FindAnyObjectByType<ExpeditionSceneBootstrap>();
            if (_expedition == null || _expedition.CryptEntrance == null)
            {
                look.SetRestFade(0f);
                _traveling = false;
                hud.ShowStatus("The Graveyard entrance is unavailable.");
                yield break;
            }
            ClearBoundEnemies();
            _expedition.Bind(this, GetComponent<PlayerVitality>(), home,
                ExpeditionCacheClaimed);
            BindGatherables(scene);
            Teleport(_expedition.CryptEntrance.position + Vector3.back * 1.7f);
            _data.regionId = TopazSaveData.ExpeditionRegion;
            look.SetInterior(false);
            Scene cryptScene = SceneManager.GetSceneByName(CryptSceneName);
            if (cryptScene.isLoaded) yield return SceneManager.UnloadSceneAsync(cryptScene);
            _crypt = null;
            RefreshPickupVisibility();
            UpdateWeather(false);
            yield return FadeTrail(false);
            _traveling = false;
            Commit();
            hud.ShowStatus("Returned to the Graveyard.");
        }

        void RestoreHomeAfterTravelFailure()
        {
            Debug.LogError("Graveyard could not be loaded; returning home.", this);
            ClearBoundEnemies();
            _expedition = null;
            _crypt = null;
            Teleport(home.transform.position);
            _data.regionId = TopazSaveData.HomeRegion;
            look.SetInterior(false);
            _traveling = false;
            RefreshPickupVisibility();
            Commit();
            hud.ShowStatus("Graveyard is unavailable; you returned home.");
        }

        IEnumerator FadeTrail(bool toBlack)
        {
            const float seconds = .34f;
            for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
            {
                float amount = Mathf.Clamp01(elapsed / seconds);
                look.SetRestFade(toBlack ? amount : 1f - amount);
                yield return null;
            }
            look.SetRestFade(toBlack ? 1f : 0f);
        }

        public void RequestTrailCrossing(string destinationRegion)
        {
            if (_data == null || _traveling || _resting || _recovering || MenuOpen ||
                Time.time < _trailReadyAt) return;
            if (_data.regionId == TopazSaveData.HomeRegion &&
                destinationRegion == TopazSaveData.ExpeditionRegion)
                StartCoroutine(EnterExpedition(false));
            else if (_data.regionId == TopazSaveData.ExpeditionRegion &&
                     destinationRegion == TopazSaveData.HomeRegion)
                StartCoroutine(ReturnHome());
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
            if (_data.pendingChest || ChestPlaced || chestRecipe.Ingredient == null ||
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
            if (!CurrentChest.IsPlaced) return;
            int deposited = _backpack.TransferAllTo(CurrentChest.Inventory, ResolveMaterial);
            if (deposited == 0) return;
            Commit();
            hud.ShowStatus($"Stored {deposited} items.");
        }

        public void WithdrawAllItems()
        {
            if (!CurrentChest.IsPlaced) return;
            int withdrawn = CurrentChest.Inventory.TransferAllTo(_backpack, ResolveMaterial);
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

        public bool SelectManualTool(string toolId)
        {
            if (_character == null || (toolId != "sword" && toolId != "axe" &&
                toolId != "pickaxe") || (toolId == "axe" && !HasAxe) ||
                (toolId == "pickaxe" && !HasPickaxe) ||
                !combat.SetManualTool(toolId)) return false;
            _character.selectedTool = toolId;
            Commit();
            return true;
        }

        public bool BeginHomeBuild(string id) => homeBuilds.BeginPlacement(id);
        public bool HomeBlocksResource(Vector3 position, float radius) =>
            _data != null && _data.regionId == TopazSaveData.HomeRegion &&
            homeBuilds != null && homeBuilds.BlocksResource(position, radius);
        public void RefreshHomeGatherables() => RefreshGatherables();
        public bool BeginHomeEdit() => homeBuilds.BeginEdit();
        public bool BeginHomeMove() => homeBuilds.BeginMove();
        public void RotateHomeBuild() => homeBuilds.Rotate();
        public int HomeCampfireTier => homeBuilds.CampfireTier;
        public bool UpgradeHomeCampfire() => homeBuilds.UpgradeCampfire();
        public bool CanUpgradeHomeCampfire => IsAtHome && HomeCampfireTier < 3 &&
            HomeWoodCount >= (HomeCampfireTier == 1 ? 9 : 24) &&
            StoneCount >= (HomeCampfireTier == 1 ? 6 : 24) &&
            IronCount >= (HomeCampfireTier == 1 ? 0 : 8);
        public bool CanForge(HomeForgeCatalog.Recipe recipe)
        {
            ItemDefinition item = ResolveItem(recipe.ItemId);
            return IsAtHome && _backpack != null && item != null &&
                homeBuilds.NearestAnvil(transform.position, InteractionRadius) != null &&
                _backpack.SpaceFor(item) >= 1 &&
                HomeWoodCount >= recipe.Wood && StoneCount >= recipe.Stone &&
                IronCount >= recipe.Iron;
        }

        public bool TryForge(HomeForgeCatalog.Recipe recipe)
        {
            if (!CanForge(recipe) || !TrySpendHomeMaterials(recipe.Wood,
                recipe.Stone, recipe.Iron))
            {
                hud.ShowStatus("Need materials, backpack space, and a nearby Anvil.");
                return false;
            }
            ItemDefinition item = ResolveItem(recipe.ItemId);
            _backpack.Add(item, 1);
            Commit();
            hud.ShowStatus(recipe.Label + " crafted.");
            return true;
        }
        public void CloseHomeMenus() => hud.ClosePanels();
        public void ShowHomeStatus(string message) => hud.ShowStatus(message);

        public bool TrySpendHomeMaterials(int stoneCount, int ironCount)
            => TrySpendHomeMaterials(0, stoneCount, ironCount);

        public bool TrySpendHomeMaterials(int woodCount, int stoneCount, int ironCount)
        {
            if (_backpack == null || CountHomeMaterial(wood) < woodCount ||
                StoneCount < stoneCount || IronCount < ironCount)
                return false;
            Spend(wood, woodCount);
            Spend(stone, stoneCount);
            Spend(iron, ironCount);
            return true;
        }

        void Spend(ItemDefinition item, int count)
        {
            if (item == null || count <= 0) return;
            int remaining = count - _backpack.Remove(item.StableId, count);
            foreach (StorageChest candidate in AllHomeChests())
            {
                if (remaining <= 0) break;
                if (candidate != null && candidate.IsPlaced)
                    remaining -= candidate.Inventory.Remove(item.StableId, remaining);
            }
        }

        public void RefundHomeMaterial(ItemDefinition item, int count, Vector3 position)
        {
            if (item == null || count <= 0) return;
            int overflow = count - _backpack.Add(item, count);
            if (overflow > 0) DropItem(item, overflow, position);
        }

        int CountHomeMaterial(ItemDefinition item) => item == null || _backpack == null ? 0 :
            _backpack.Count(item.StableId) +
            AllHomeChests()
                .Sum(value => value.Inventory.Count(item.StableId));

        IEnumerable<StorageChest> AllHomeChests()
        {
            var bound = homeBuilds.Chests.Where(value => value != null && value.IsPlaced)
                .ToList();
            if (chest != null && chest.IsPlaced && !bound.Contains(chest)) bound.Add(chest);
            return bound;
        }

        public void DropHomeItem(string id, int count, Vector3 position)
        {
            ItemDefinition item = ResolveItem(id);
            if (count <= 0 || string.IsNullOrEmpty(id)) return;
            if (item != null) { DropItem(item, count, position); return; }
            // Preserve unknown future item IDs even when their art is unavailable here.
            _data.pickups.Add(new PickupStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                itemId = id,
                regionId = TopazSaveData.HomeRegion,
                count = count,
                x = position.x,
                z = position.z
            });
        }

        public void OnChestRemoved(StorageChest removed)
        {
            if (_openChest == removed) _openChest = homeBuilds.Chests.FirstOrDefault();
        }

        public void ToggleLantern()
        {
            if (_character == null || lantern == null) return;
            _character.lanternOn = !_character.lanternOn;
            lantern.SetLit(_character.lanternOn);
            Commit();
        }

        public void Commit()
        {
            if (_data == null || _repository == null || !_savingEnabled || _traveling) return;
            _data.playerX = transform.position.x;
            _data.playerZ = transform.position.z;
            _character.loggingExperience = LoggingExperience;
            _character.swordsExperience = SwordsExperience;
            _character.axesExperience = AxesExperience;
            _character.miningExperience = MiningExperience;
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
            if (!PendingChest || ChestPlaced || !IsAtHome) return;
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
            homeBuilds.RegisterLegacyChest(placed);
            _openChest = chest;
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
            return !ChestPlaced;
        }

        IEnumerator Rest()
        {
            _resting = true;
            combat.CancelActiveAttack();
            const float fadeSeconds = .3f;
            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(1f);
            _data.worldHours += WorldClock.RestHours;
            SurvivalRules.Advance(_character, WorldClock.RestHours);
            SurvivalRules.Rest(_character);
            GetComponent<PlayerVitality>()?.RestoreHealthAfterRest();
            ResetLocalAreaAfterRest();
            RefreshGatherables();
            look.SetWorldHours(_data.worldHours);
            UpdateWeather(true);
            Commit();
            for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
            {
                look.SetRestFade(1f - Mathf.Clamp01(elapsed / fadeSeconds));
                yield return null;
            }
            look.SetRestFade(0f);
            _resting = false;
        }

        public bool TryExert(float cost)
        {
            if (_character == null) return false;
            _lastExertionAt = Time.time;
            return SurvivalRules.Spend(_character, cost);
        }

        public bool TryCollectForage(bool mushrooms)
        {
            ItemDefinition item = mushrooms ? _mushrooms : _berries;
            if (_backpack == null || item == null || _backpack.SpaceFor(item) < 1)
            {
                hud?.ShowStatus("Make space in the backpack first.");
                return false;
            }
            _backpack.Add(item, 1);
            hud?.ShowStatus(mushrooms ? "Mushrooms gathered." : "Red Berries picked.");
            return true;
        }

        public bool CanEat(string itemId) =>
            _backpack != null && _backpack.Count(itemId) > 0 &&
            SurvivalRules.CanEat(_character,
                itemId == SurvivalRules.StewId ? 2 :
                itemId == SurvivalRules.BerriesId ? 1 : 0);

        public bool TryEat(int slotIndex)
        {
            if (_backpack == null || slotIndex < 0 || slotIndex >= _backpack.Capacity)
                return false;
            ItemStackRecord slot = _backpack.Slots[slotIndex];
            int tier = slot.itemId == SurvivalRules.StewId ? 2 :
                slot.itemId == SurvivalRules.BerriesId ? 1 : 0;
            if (slot.count < 1 || !SurvivalRules.CanEat(_character, tier)) return false;
            _backpack.Remove(slot.itemId, 1);
            SurvivalRules.Eat(_character, tier);
            Commit();
            return true;
        }

        public bool TryCookStew()
        {
            Campfire fire = NearbyCampfire();
            if (fire == null || _backpack == null || _stew == null ||
                MushroomsAtFire < 1) return false;
            StorageChest nearby = homeBuilds.NearestChest(fire.transform.position, 2.2f);
            var trial = new InventorySlots(_backpack.Slots.Select(slot =>
                new ItemStackRecord { itemId = slot.itemId, count = slot.count }).ToList(),
                BackpackCapacity);
            bool fromBackpack = trial.Count(SurvivalRules.MushroomsId) > 0;
            if (fromBackpack) trial.Remove(SurvivalRules.MushroomsId, 1);
            if (trial.SpaceFor(_stew) < 1)
            {
                hud?.ShowStatus("Make space in the backpack first.");
                return false;
            }
            if (fromBackpack) _backpack.Remove(SurvivalRules.MushroomsId, 1);
            else nearby.Inventory.Remove(SurvivalRules.MushroomsId, 1);
            _backpack.Add(_stew, 1);
            Commit();
            hud?.ShowStatus("Mushroom Stew cooked.");
            return true;
        }

        void ResetLocalAreaAfterRest()
        {
            foreach (CrossbowBolt bolt in FindObjectsByType<CrossbowBolt>())
                if (bolt != null) Destroy(bolt.gameObject);
            foreach (EnemyCombatant enemy in _boundEnemies)
            {
                if (enemy == null) continue;
                EnemyRespawnRecord pending = _world.enemyRespawns.Find(value =>
                    value.spawnId == enemy.SpawnId);
                if (pending != null && pending.readyAtWorldHours > WorldHours) continue;
                enemy.ResetForRecovery();
                if (pending != null && !enemy.IsDown) _world.enemyRespawns.Remove(pending);
            }
        }

        ItemDefinition ResolveItem(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id == wood.StableId) return wood;
            if (id == stone.StableId) return stone;
            if (id == iron.StableId) return iron;
            if (id == boneFragments.StableId) return boneFragments;
            if (id == pickaxeItem.StableId) return pickaxeItem;
            if (id == SurvivalRules.BerriesId) return _berries;
            if (id == SurvivalRules.MushroomsId) return _mushrooms;
            if (id == SurvivalRules.StewId) return _stew;
            foreach (ItemDefinition item in equipmentItems)
                if (item != null && item.StableId == id) return item;
            return null;
        }

        ItemDefinition ResolveMaterial(string id)
        {
            ItemDefinition item = ResolveItem(id);
            return item != null && item.EquipmentSlot == EquipmentSlot.None ? item : null;
        }

        public ItemDefinition Item(string id) => ResolveItem(id);
        public bool RackHas(string id) => _world != null && RackItems.Contains(id) &&
            !_world.claimedGearIds.Contains(id);

        public bool TryClaimRackItem(string id)
        {
            if (!RackHas(id) || _backpack == null || _data.regionId != TopazSaveData.HomeRegion ||
                DistanceSquared(gearRack) > InteractionRadius * InteractionRadius) return false;
            ItemDefinition item = ResolveItem(id);
            if (item == null || _backpack.Add(item, 1) != 1)
            {
                hud.ShowStatus("Backpack is full.");
                return false;
            }
            _world.claimedGearIds.Add(id);
            Commit();
            return true;
        }

        public bool TryEquipFromBackpack(int index)
        {
            if (_backpack == null || index < 0 || index >= _backpack.Capacity) return false;
            ItemStackRecord carried = _backpack.Slots[index];
            ItemDefinition item = carried.count == 1 ? ResolveItem(carried.itemId) : null;
            if (item == null || item.EquipmentSlot == EquipmentSlot.None) return false;
            EquipmentSlot slot = item.EquipmentSlot;
            if ((slot == EquipmentSlot.Weapon || slot == EquipmentSlot.Offhand) &&
                combat != null && combat.IsAttackLocked) return false;
            if (slot == EquipmentSlot.Weapon && item.Weapon?.Attack == null &&
                item.Weapon?.GroundSpell == null &&
                item.Weapon?.CrossbowAttack == null) return false;
            string displaced = Equipped.Get(slot);
            if (!string.IsNullOrEmpty(displaced) && ResolveItem(displaced) == null) return false;
            ItemDefinition extra = slot == EquipmentSlot.Weapon && item.Weapon.TwoHanded
                ? ResolveItem(Equipped.offhandId) :
                slot == EquipmentSlot.Offhand && CurrentWeapon?.TwoHanded == true
                    ? ResolveItem(Equipped.weaponId) : null;
            if (extra != null && _backpack.SpaceFor(extra) < 1 &&
                !string.IsNullOrEmpty(displaced))
            {
                hud.ShowStatus("Make space in the backpack first.");
                return false;
            }
            if (extra == null &&
                ((slot == EquipmentSlot.Weapon && item.Weapon.TwoHanded &&
                  !string.IsNullOrEmpty(Equipped.offhandId)) ||
                 (slot == EquipmentSlot.Offhand && CurrentWeapon?.TwoHanded == true)))
                return false; // Unknown saved gear must never be silently lost.
            Equipped.Set(slot, item.StableId);
            carried.itemId = displaced;
            carried.count = string.IsNullOrEmpty(displaced) ? 0 : 1;
            if (extra != null)
            {
                Equipped.Set(slot == EquipmentSlot.Weapon ? EquipmentSlot.Offhand :
                    EquipmentSlot.Weapon, null);
                _backpack.Add(extra, 1);
            }
            if (slot == EquipmentSlot.Weapon || extra?.EquipmentSlot == EquipmentSlot.Weapon)
                combat.ClearTemporaryProgression();
            DiscoverWeaponSkill(item);
            Commit();
            return true;
        }

        public bool TryUnequip(EquipmentSlot slot)
        {
            if (_backpack == null || Equipped == null || slot == EquipmentSlot.None) return false;
            if ((slot == EquipmentSlot.Weapon || slot == EquipmentSlot.Offhand) &&
                combat != null && combat.IsAttackLocked) return false;
            string id = Equipped.Get(slot);
            ItemDefinition item = ResolveItem(id);
            if (item == null || _backpack.Add(item, 1) != 1) return false;
            Equipped.Set(slot, null);
            if (slot == EquipmentSlot.Weapon) combat.ClearTemporaryProgression();
            Commit();
            return true;
        }

        public bool TryTransferGear(bool toChest, int index)
        {
            if (_backpack == null || CurrentChest?.Inventory == null) return false;
            InventorySlots source = toChest ? _backpack : CurrentChest.Inventory;
            InventorySlots destination = toChest ? CurrentChest.Inventory : _backpack;
            if (index < 0 || index >= source.Capacity) return false;
            ItemStackRecord stack = source.Slots[index];
            ItemDefinition item = stack.count == 1 ? ResolveItem(stack.itemId) : null;
            if (item == null || item.EquipmentSlot == EquipmentSlot.None ||
                destination.Add(item, 1) != 1) return false;
            stack.count = 0;
            stack.itemId = null;
            if (!toChest) DiscoverWeaponSkill(item);
            Commit();
            return true;
        }

        void DiscoverWeaponSkill(ItemDefinition item)
        {
            string skill = item == cryptStaff ? SkillIds.Staff :
                item == cryptCrossbow ? SkillIds.Crossbows : null;
            if (skill != null && !_character.discoveredSkillIds.Contains(skill))
                _character.discoveredSkillIds.Add(skill);
        }

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
