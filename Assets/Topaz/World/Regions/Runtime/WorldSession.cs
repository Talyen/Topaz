using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Topaz.Combat;
using Topaz.Characters;
using Topaz.Expedition;
using Topaz.Player;
using Topaz.Menus;
using Topaz.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Topaz.Gameplay
{
    /// <summary>Owns continuous-world gameplay transactions and persistent player changes.</summary>
    public sealed partial class WorldSession : MonoBehaviour
    {
#if UNITY_EDITOR
        public static string EditorTestSaveDirectory { get; set; }
        public static int? EditorTestSeed { get; set; }
#endif
        [SerializeField] ItemDefinition wood;
        [SerializeField] ItemDefinition stone;
        [SerializeField] ItemDefinition iron;
        [SerializeField] ItemDefinition boneFragments;
        [SerializeField] GameObject bonePickupVisual;
        [SerializeField] ItemDefinition pickaxeItem;
        [SerializeField] SkillDefinition[] skillDefinitions;
        [SerializeField] MiningRock[] rocks;
        [SerializeField] RegionBuildings homeBuilds;
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
        [SerializeField] PlayerController movement;
        [SerializeField] PlayerCombat combat;
        [SerializeField] InputActionAsset controls;
        [SerializeField] LoopHud hud;
        [SerializeField] GameObject chestPreview;
        [SerializeField] Renderer previewRenderer;
        [SerializeField] GameObject pickupPrefab;
        [SerializeField] GameMenus menus;
        [SerializeField] Transform homeGate;
        [UnityEngine.Serialization.FormerlySerializedAs("cryptStaff")]
        [SerializeField] ItemDefinition staff;
        [UnityEngine.Serialization.FormerlySerializedAs("cryptCrossbow")]
        [SerializeField] ItemDefinition crossbow;
        [SerializeField] VisualLookController look;
        [SerializeField] PlayerAppearance appearance;
        [SerializeField] PlayerLantern lantern;

        const float InteractionRadius = 1.4f;
        const float PlacementStep = 0.75f;
        const int ExpeditionWoodReward = 3;
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
        bool _generationFailed;
        bool _fastTraveling;
        bool _resting;
        bool _recovering;
        bool _applicationPaused;
        float _lastExertionAt = -1000f;
        bool _clockWasActive;
        float _activeSecondsSinceSave;
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
        public TopazWorldData ActiveWorld => _world;

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
            _visit == null ? Array.Empty<CampfireTravelCatalog.Destination>() :
            Destinations(_world).Where(d => _visit.discoveredCampfireIds.Contains(d.stableId)).ToArray();
        public bool IsRecovering => _recovering;
        public bool IsResting => _resting;
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
        public bool IsAtHome => _data != null && CampSafety.IsProtected(transform.position);
        public int PickupCount => _data?.pickups.Count ?? 0;
        public bool PendingChest => _data != null && _data.pendingChest;
        public int ChestCost => chestRecipe != null ? chestRecipe.IngredientCount : 0;
        public bool CanCraftChest => _data != null && !homeBuilds.Active && HomeWoodCount >= 3;
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
        public bool BlockMovement => (ActiveRegion?.Streaming != null && !ActiveRegion.Streaming.IsReadyAt(transform.position)) || _generationFailed || MenuOpen || _traveling || _resting || _recovering;
        public bool SuppressAttack => (ActiveRegion?.Streaming != null && !ActiveRegion.Streaming.InitialReady) || _generationFailed || MenuOpen || _placing ||
            (homeBuilds != null && homeBuilds.Active) || _traveling || _resting || _recovering;
        public string SaveProblem => _saveProblem;
        public string WorldProblem => ActiveRegion?.Streaming?.Failure==null ? null : "World loading failed. Return to the title and retry.";

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
                staff == null || crossbow == null || look == null ||
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
                (arg.Equals("-runTests", StringComparison.OrdinalIgnoreCase) || arg == "--topaz-smoke"));
            string directory = runningTests
                ? Path.Combine(Application.temporaryCachePath, "TopazTest-" + Guid.NewGuid().ToString("N"))
                : Path.Combine(Application.persistentDataPath, "Alpine-v6");
#if UNITY_EDITOR
            if (runningTests && !string.IsNullOrEmpty(EditorTestSaveDirectory))
                directory = EditorTestSaveDirectory;
#endif
            if(Environment.GetCommandLineArgs().Contains("--topaz-smoke"))
            {
                string key=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--topaz-smoke-profile="))?.Substring(22);
                if(!string.IsNullOrEmpty(key) && key.All(c=>char.IsLetterOrDigit(c)||c=='-'))
                    directory=Path.Combine(Application.temporaryCachePath,"TopazDiagnostic-"+key);
            }
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
            campSuppressed.Clear();
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
            if (!HasLastPair) InitializeGeneration(world);
            TopazVisitData visit = _profile.GetOrCreateVisit(character.id, world.id);
            BindPair(character, world, visit);
            if (_generationFailed) return;
            Commit();
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
            if (newWorld) InitializeGeneration(world);
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
            foreach (GameObject pickup in _spawnedPickups)
                if (pickup != null)
                {
                    pickup.SetActive(false);
                    Destroy(pickup);
                }
            _spawnedPickups.Clear();
            chest.Bind(null);
            hud.ShowStatus("Preparing alpine wilderness…");
            yield return null;
            BindPair(character, world, visit);
            if (_generationFailed) { completed?.Invoke(false); yield break; }
            _traveling = false;
            Commit();
            FlushSave();
            _traveling = false;
            completed?.Invoke(_savingEnabled);
        }

        void BindPair(TopazCharacterData character, TopazWorldData world, TopazVisitData visit)
        {
            if (!PrepareGeneratedRegion(SceneManager.GetSceneByName("Bootstrap"),world, new Vector3(visit.playerX,0,visit.playerZ))) return;
            _character = character;
            _lastExertionAt = -1000f;
            _character.EnsureSkills();
            UnlockMasteredTalents();
            combat.ClearTemporaryProgression();
            _world = world;
            InvalidateStorageIndex();
            _visit = visit;
            if (ResolveDestination(visit.lastCampfireId, world) == null) visit.lastCampfireId = Campfire.HomeId;
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
            look.SetInterior(false);
            Teleport(new Vector3(visit.playerX, 0f, visit.playerZ));
            BindGatherables(SceneManager.GetSceneByName("Bootstrap"));
            homeBuilds.Bind(this, world, TopazSaveData.HomeRegion);
            RefreshGatherables();
            combat.SetManualTool(character.selectedTool);
            _openChest = homeBuilds.Chests.FirstOrDefault();
            if (!appearance.Apply(character.appearanceId))
                appearance.Apply(CharacterLooks.Rogue);
            lantern.SetLit(character.lanternOn);
            RefreshPickupVisibility();
            if (chestPreview != null) chestPreview.SetActive(false);
            _eventWeatherOverride = null;
            _regionWeatherOverride = null;
            _regionWeatherId = null;
            ApplyIsolatedWeatherPreview();
            look.BeginWorldPresentation(_data.worldHours);
            look.SetWorldHours(_data.worldHours);
            lantern.SetWorldHours(_data.worldHours);
            UpdateWeather(true);
            foreach(var actor in ActiveRegion.Streaming.GetComponentsInChildren<EnemyCombatant>(true)) RegisterEnemy(actor);
            hud.Bind(this);
            if(arrivalRoutine!=null)StopCoroutine(arrivalRoutine);
            if(!ActiveRegion.Streaming.InitialReady)arrivalRoutine=StartCoroutine(RevealWilderness());
            _activeSecondsSinceSave = 0f;
            _clockWasActive = false;
        }

        Coroutine arrivalRoutine;
        IEnumerator RevealWilderness()
        {
            look.SetRestFade(1);
            while(ActiveRegion.Streaming!=null && !ActiveRegion.Streaming.InitialReady && ActiveRegion.Streaming.Failure==null)
            {hud.ShowStatus("Preparing your wilderness…");yield return null;}
            yield return FadeTrail(false);
            arrivalRoutine=null;
        }

        void ApplyIsolatedWeatherPreview()
        {
            string[] args = Environment.GetCommandLineArgs();
            if (!args.Any(arg => (arg.Equals("-runTests", StringComparison.OrdinalIgnoreCase) || arg == "--topaz-smoke")))
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
                _weatherPresentation?.SetActiveWorld(false);
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

                if (_placeRequested && Time.time >= _placementReadyAt)
                    homeBuilds.Tick(true, false);
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
            bool active = (ActiveRegion?.Streaming == null || ActiveRegion.Streaming.InitialReady) && !_applicationPaused && !_traveling && !_resting && !_recovering &&
                !MenuOpen &&
                Time.timeScale > 0f;
            _weatherPresentation?.SetActiveWorld(active);
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
            lantern.SetWorldHours(_data.worldHours);
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
            string resolved = WeatherSchedule.Resolve(ambient, region, _eventWeatherOverride);
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
            node.scene.name == "Bootstrap";

        readonly List<Topaz.Generation.DiscoveryCache> discoveryCaches=new List<Topaz.Generation.DiscoveryCache>();
        public void RegisterDiscoveryCache(Topaz.Generation.DiscoveryCache cache)
        {if(!discoveryCaches.Contains(cache))discoveryCaches.Add(cache);}
        public void UnregisterDiscoveryCache(Topaz.Generation.DiscoveryCache cache)=>discoveryCaches.Remove(cache);
        public bool TryClaimDiscoverySupplies(string id)
        {
            if(_data==null || string.IsNullOrEmpty(id))return false;
            var state=GetOrCreateNodeState(id);
            if(state.readyAtWorldHours>WorldHours){hud.ShowStatus("This cache is empty for now.");return false;}
            if(_backpack.SpaceFor(wood)<ExpeditionWoodReward){hud.ShowStatus("Make room for 3 Wood before opening the cache.");return false;}
            _backpack.Add(wood,ExpeditionWoodReward);state.readyAtWorldHours=WorldHours+CacheRefillHours;
            Commit();hud.ShowStatus("Supplies collected: +3 Wood.");return true;
        }

        readonly List<HarvestTree> loadedTrees=new List<HarvestTree>();
        readonly List<MiningRock> loadedRocks=new List<MiningRock>();
        public void RegisterGatherable(HarvestTree node)
        { node.Bind(this);if(!loadedTrees.Contains(node))loadedTrees.Add(node); }
        public void RegisterGatherable(MiningRock node)
        { node.Bind(this);if(!loadedRocks.Contains(node))loadedRocks.Add(node); }
        public void RegisterGatherable(ForagePlant node)
        { node.Bind(this);if(!_foragePlants.Contains(node))_foragePlants.Add(node); }

        void BindGatherables(Scene scene)
        {
            if (!scene.isLoaded) return;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (HarvestTree node in root.GetComponentsInChildren<HarvestTree>())
                    RegisterGatherable(node);
                foreach (MiningRock node in root.GetComponentsInChildren<MiningRock>())
                    RegisterGatherable(node);
                foreach (ForagePlant node in root.GetComponentsInChildren<ForagePlant>())
                {
                    node.Bind(this);
                    if (!_foragePlants.Contains(node)) _foragePlants.Add(node);
                }
                foreach (RestSpot bed in root.GetComponentsInChildren<RestSpot>(true))
                    if (!_restSpots.Contains(bed)) _restSpots.Add(bed);
            }
        }

        public void PrepareStreamedStructures(Vector3 focus) => homeBuilds.RefreshLoadedStructures(focus);

        public void RefreshStreamedWorld()
        {
            if (_world == null) return;
            homeBuilds.RefreshLoadedStructures(transform.position);
            _foragePlants.RemoveAll(p => p == null);
            BindGatherables(SceneManager.GetSceneByName("Bootstrap"));
            RefreshGatherables();
            RefreshPickupVisibility();
        }
        public void UnbindStreamedWorld(GameObject root)
        {
            foreach (var enemy in root.GetComponentsInChildren<EnemyCombatant>(true))
            {
                enemy.Defeated -= OnEnemyDefeated;
                _boundEnemies.Remove(enemy);campSuppressed.Remove(enemy);
            }
            _foragePlants.RemoveAll(p => p == null || p.transform.IsChildOf(root.transform));
            loadedTrees.RemoveAll(p => p == null || p.transform.IsChildOf(root.transform));
            loadedRocks.RemoveAll(p => p == null || p.transform.IsChildOf(root.transform));
            // Terrain residency is removed before this callback; retire independent loot visuals
            // now rather than waiting through scenery pooling and the next navigation refresh.
            RefreshPickupVisibility();
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
            else if (CampSafety.IsProtected(enemy.transform.position)) { campSuppressed.Add(enemy); enemy.gameObject.SetActive(false); }
        }

        readonly HashSet<EnemyCombatant> campSuppressed = new HashSet<EnemyCombatant>();

        void ClearBoundEnemies()
        {
            foreach (EnemyCombatant enemy in _boundEnemies)
                if (enemy != null) enemy.Defeated -= OnEnemyDefeated;
            _boundEnemies.Clear();
            campSuppressed.Clear();
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
            switch(enemy.Species)
            {
                case EnemySpecies.Skeleton:
                    DropItem(boneFragments,1,position+Vector3.left*.35f);
                    break;
                case EnemySpecies.Goblin:
                    DropItem(UnityEngine.Random.value<.7f?wood:stone,1,position+Vector3.left*.35f);
                    break;
                case EnemySpecies.Raider:
                    if(UnityEngine.Random.value<.4f)DropItem(iron,1,position+Vector3.left*.35f);
                    break;
                case EnemySpecies.Troll:
                    DropItem(iron,2,position+Vector3.left*.35f);
                    break;
            }
            ItemDefinition extra = null;
            if(enemy.Species==EnemySpecies.Skeleton || enemy.Species==EnemySpecies.Raider)
            switch (enemy.LootRole)
            {
                case SkeletonLootRole.Warrior:
                    extra = ResolveItem(UnityEngine.Random.value < .5f
                        ? EquipmentState.Sword : EquipmentState.Shield);
                    break;
                case SkeletonLootRole.Rogue:
                    extra = crossbow;
                    break;
                case SkeletonLootRole.Mage:
                    extra = staff;
                    break;
            }
            if (extra != null) DropItem(extra, 1, position + Vector3.right * .35f);
            Commit();
        }

        void TickEnemyReturns()
        {
            if (_world == null || Time.unscaledTime < _nextEnemyCheck) return;
            _nextEnemyCheck = Time.unscaledTime + 1f;
            Camera camera = Camera.main;
            foreach (EnemyCombatant enemy in _boundEnemies)
            {
                if (enemy == null) continue;
                bool protectedSpawn = CampSafety.IsProtected(enemy.transform.position);
                if (campSuppressed.Contains(enemy))
                {
                    if (protectedSpawn) continue;
                    campSuppressed.Remove(enemy); enemy.gameObject.SetActive(true);
                }
                if (!enemy.IsDown) continue;
                EnemyRespawnRecord pending = _world.enemyRespawns.Find(value =>
                    value.spawnId == enemy.SpawnId);
                if (pending == null || WorldHours < pending.readyAtWorldHours ||
                    CampSafety.IsProtected(enemy.transform.position) ||
                    !SpawnOutOfView(enemy.transform.position, camera)) continue;
                enemy.ResetForRecovery();
                if (enemy.IsDown) continue;
                _world.enemyRespawns.Remove(pending);
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
                _spawnedPickups.Remove(pickup.gameObject);
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
            if (item.WorldVisual != null)
                pickup.OverrideVisual(item.WorldVisual);
            else if (item == staff || item == crossbow)
                pickup.OverrideVisual(item.Weapon?.HeldModel);
            else if (item == boneFragments)
                pickup.OverrideVisual(bonePickupVisual);
            instance.SetActive(string.IsNullOrEmpty(record.regionId) ||
                record.regionId == _data.regionId);
        }

        void RefreshPickupVisibility()
        {
            if(_data==null || ActiveRegion?.Streaming==null)return;
            bool Loaded(PickupStateRecord record)=>record!=null &&
                ActiveRegion.Streaming.HasTerrainAt(new Vector3(record.x,0,record.z));
            var present=new HashSet<string>();
            for(int i=_spawnedPickups.Count-1;i>=0;i--)
            {
                var instance=_spawnedPickups[i];var pickup=instance!=null?instance.GetComponent<WorldPickup>():null;
                if(pickup==null || !Loaded(pickup.State))
                {
                    if(instance!=null){instance.SetActive(false);Destroy(instance);}
                    _spawnedPickups.RemoveAt(i);
                }
                else present.Add(pickup.State.instanceId);
            }
            foreach(var record in _data.pickups)
                if(Loaded(record) && !present.Contains(record.instanceId))
                {
                    var item=ResolveItem(record.itemId);
                    if(item!=null)CreatePickup(record,item);
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
                case InteractionKind.SupplyCache:
                    candidate.anchor?.GetComponentInParent<Topaz.Generation.DiscoveryCache>()?.Open();
                    return true;
                case InteractionKind.Workbench:
                    hud.ShowCraftPanel();
                    return true;
                case InteractionKind.Rest:
                    StartCoroutine(Rest());
                    return true;
                case InteractionKind.Chest:
                    _openChest = homeBuilds.NearestChest(transform.position, InteractionRadius);
                    hud.ShowChestPanel();
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
                    return false;
            }
        }

        enum InteractionKind { None, SupplyCache,
            Chest, Workbench, Rest, Harvest, Mine,
            GearRack, CampfireTravel, Anvil, Forage }

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
            foreach(var cache in discoveryCaches)
                if(cache!=null && cache.isActiveAndEnabled)Consider(ref choice,ref nearest,InteractionKind.SupplyCache,cache.transform,cache.Stocked?"Open cache":"Inspect empty cache");
            StorageChest nearestChest = homeBuilds.NearestChest(transform.position,
                InteractionRadius);
            if (nearestChest != null)
                Consider(ref choice, ref nearest, InteractionKind.Chest,
                    nearestChest.transform, "Open chest");
            if (IsAtHome && workbench != null)
            {
                Consider(ref choice, ref nearest, InteractionKind.Workbench, workbench,
                    "Build");
            }
            if (IsAtHome) Consider(ref choice, ref nearest, InteractionKind.GearRack, gearRack, "Inspect gear");
            Consider(ref choice, ref nearest, InteractionKind.Rest,
                homeBuilds.NearestRest(transform.position, InteractionRadius), "Rest");
            foreach (var fire in CurrentCampfires()) ConsiderCampfire(ref choice, ref nearest, fire);
            Consider(ref choice, ref nearest, InteractionKind.Anvil,
                homeBuilds.NearestAnvil(transform.position, InteractionRadius), "Forge gear");
            if (includeGatherables && combat.CanStartHarvest)
            {
                foreach(var node in loadedTrees)
                    if(node!=null && node.IsAvailable && combat.HasHarvestTool(node.RequiredToolId))
                        Consider(ref choice,ref nearest,InteractionKind.Harvest,node.transform,"Chop",node);
                if(HasPickaxe)foreach(var node in loadedRocks)
                    if(node!=null && node.IsAvailable)
                        Consider(ref choice,ref nearest,InteractionKind.Mine,node.transform,"Mine",null,node);
            }
            return choice;
        }

        void Consider(ref InteractionCandidate choice, ref float nearest,
            InteractionKind kind, Transform target, string verb, HarvestTree harvestTarget = null,
            MiningRock miningTarget = null, ForagePlant forageTarget = null)
        {
            if (target == null || !target.gameObject.activeInHierarchy) return;
            Vector3 toward=target.position-transform.position;
            if (Mathf.Abs(toward.y)>1.8f) return;
            toward.y=0;
            if(toward.sqrMagnitude>.12f && Vector3.Dot(toward.normalized,movement.AimDirection)<.1f)return;
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

        public bool TryFastTravel(string destinationId)
        {
            Campfire source = NearbyCampfire();
            CampfireTravelCatalog.Destination destination = ResolveDestination(destinationId);
            if (source == null || destination == null || _visit == null ||
                !_visit.discoveredCampfireIds.Contains(destinationId) ||
                source.StableId == destinationId || _traveling || _resting || _recovering ||
                hud == null || !hud.TravelOpen) return false;
            Commit();
            StartCoroutine(FastTravel(destination));
            return true;
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

        public bool RecoverAfterDefeat(PlayerVitality vitality)
        {
            if (_data == null || _recovering || vitality == null) return false;
            StartCoroutine(RecoverAtCampfire(vitality));
            return true;
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

        void Teleport(Vector3 position)
        {
            position.y = Topaz.Generation.WoodlandRegion.GroundHeight(position) + .08f;
            combat?.CancelActiveAttack();
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = position;
            if (controller != null) controller.enabled = true;
            movement.ResetMotion();
        }

        void InitializeGeneration(TopazWorldData world)
        {
#if UNITY_EDITOR
            if(Application.isEditor && EditorTestSeed.HasValue)world.seed=EditorTestSeed.Value;
#endif
            foreach(var root in SceneManager.GetSceneByName("Bootstrap").GetRootGameObjects())
            foreach(var region in root.GetComponentsInChildren<Topaz.Generation.WoodlandRegion>(true))
                if(region.preset!=null)world.generationSettings=region.preset.settings.Copy();
            foreach(var arg in Environment.GetCommandLineArgs())
                if(arg.StartsWith("--topaz-seed=") && int.TryParse(arg.Substring(13),out int seed))world.seed=seed;
            world.generationSettings.Validate();
            world.generatorVersion=world.generationSettings.version;
        }

        public bool PrepareGeneratedRegion(Scene scene, TopazWorldData world = null, Vector3 start = default)
        {
            world ??= _world;
            if (!scene.isLoaded || world == null) return false;
            try
            {
                foreach (var root in scene.GetRootGameObjects())
                foreach (var region in root.GetComponentsInChildren<Topaz.Generation.WoodlandRegion>(true))
                    region.Generate(world, start);
                _generationFailed=false;
                return true;
            }
            catch(Exception error)
            {
                _generationFailed=true;
                _saveProblem="World generation could not finish: "+error.Message;
                hud?.ShowStatus(_saveProblem);
                Debug.LogError("[Topaz] "+_saveProblem,this);
                return false;
            }
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
            _data != null &&
            homeBuilds != null && homeBuilds.BlocksResource(position, radius);
        public void RefreshHomeGatherables() => RefreshGatherables();
        public void RefreshBuiltGround(Vector3 point,float radius) => ActiveRegion?.Streaming?.RefreshBuiltArea(new Bounds(point,new Vector3(radius*2+3,200,radius*2+3)));
        public bool BeginHomeEdit() => homeBuilds.BeginEdit();
        public bool BeginHomeMove() => homeBuilds.BeginMove();
        public void RotateHomeBuild() => homeBuilds.Rotate();
        public bool CanForge(HomeForgeCatalog.Recipe recipe)
        {
            ItemDefinition item = ResolveItem(recipe.ItemId);
            return _backpack != null && item != null &&
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
            foreach (InventorySlots inventory in NearbyStorage())
            {
                if (remaining <= 0) break;
                remaining -= inventory.Remove(item.StableId, remaining);
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
            NearbyStorage()
                .Sum(value => value.Count(item.StableId));

        readonly WorldStorageIndex storageIndex = new WorldStorageIndex();
        bool storageIndexDirty=true;
        int storageIndexedCount=-1;
        public void InvalidateStorageIndex()=>storageIndexDirty=true;
        IEnumerable<InventorySlots> NearbyStorage()
        {
            if (_world == null) yield break;
            if(storageIndexDirty || storageIndexedCount!=_world.structures.Count)
            {
                storageIndex.Rebuild(_world.structures);
                storageIndexedCount=_world.structures.Count;storageIndexDirty=false;
            }
            foreach (var record in storageIndex.Nearby(transform.position.x, transform.position.z))
                yield return new InventorySlots(record.slots, 12);
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
                regionId = CurrentRegionId,
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

        public bool TryCraftChest() => homeBuilds.BeginPlacement(BuildCatalog.Chest);

        void ExitPlacement()
        {
            _placing = false;
            chestPreview.SetActive(false);
            homeBuilds.Cancel();
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
            lantern.SetWorldHours(_data.worldHours);
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
            string skill = item == staff ? SkillIds.Staff :
                item == crossbow ? SkillIds.Crossbows : null;
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
