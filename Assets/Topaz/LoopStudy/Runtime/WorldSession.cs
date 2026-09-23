using System;
using System.IO;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.LoopStudy
{
    /// <summary>Coordinates the one-resource expedition and one-chest home loop.</summary>
    public sealed class WorldSession : MonoBehaviour
    {
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

        const float InteractionRadius = 1.4f;
        const float PlacementStep = 0.75f;
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color ValidColor = new Color(0.36f, 0.95f, 0.57f);
        static readonly Color InvalidColor = new Color(0.96f, 0.38f, 0.34f);

        SaveRepository _repository;
        TopazSaveData _data;
        InputAction _placeAction;
        InputAction _cancelAction;
        MaterialPropertyBlock _previewProperties;
        Vector3 _previewPosition;
        float _placementReadyAt;
        bool _placeRequested;
        bool _cancelRequested;
        bool _placing;
        bool _savingEnabled = true;
        string _saveProblem;

        public int CurrentDay => _data?.day ?? 1;
        public int WoodCount => _data == null ? 0 : CountWood();
        public int LoggingExperience => _data?.loggingExperience ?? 0;
        public int LoggingLevel => 1 + LoggingExperience / 10;
        public bool PendingChest => _data != null && _data.pendingChest;
        public int ChestCost => chestRecipe != null ? chestRecipe.IngredientCount : 0;
        public bool CanCraftChest => _data != null && !_data.pendingChest && !ChestPlaced &&
            WoodCount >= ChestCost;
        public string EquippedToolName => combat != null ? combat.EquippedToolName : "Sword";
        public bool ChestPlaced => chest != null && chest.IsPlaced;
        public int ChestWood => chest?.WoodStored ?? 0;
        public int ChestCapacity => chest?.WoodCapacity ?? 0;
        public bool IsPlacing => _placing;
        public bool MenuOpen => hud != null && hud.MenuOpen;
        public bool BlockMovement => MenuOpen;
        public bool SuppressAttack => MenuOpen || _placing;
        public string SaveProblem => _saveProblem;

        void Awake()
        {
            _previewProperties = new MaterialPropertyBlock();
            if (controls == null || wood == null || tree == null || chest == null ||
                chestRecipe == null || chestDefinition == null || home == null ||
                movement == null || combat == null || hud == null)
            {
                Debug.LogError("World session is missing a required reference.", this);
                enabled = false;
                return;
            }

            bool runningTests = Environment.GetCommandLineArgs().Any(arg =>
                arg.Equals("-runTests", StringComparison.OrdinalIgnoreCase));
            string directory = runningTests
                ? Path.Combine(Application.temporaryCachePath, "TopazTest-" + Guid.NewGuid().ToString("N"))
                : Application.persistentDataPath;
            _repository = new SaveRepository(directory);
            try { _data = _repository.Load(); }
            catch (Exception error)
            {
                _data = new TopazSaveData();
                _savingEnabled = false;
                _saveProblem = "Save could not be read; saving is disabled to protect it.";
                Debug.LogError($"[Topaz] {_saveProblem} {error.Message}", this);
            }

            InputActionMap map = controls.FindActionMap("Player", true);
            _placeAction = map.FindAction("Place", true);
            _cancelAction = map.FindAction("Cancel", true);
        }

        void OnEnable()
        {
            if (_placeAction != null) _placeAction.performed += OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed += OnCancelPerformed;
        }

        void OnDisable()
        {
            if (_placeAction != null) _placeAction.performed -= OnPlacePerformed;
            if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
        }

        void Start()
        {
            CharacterController controller = GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            transform.position = new Vector3(_data.playerX, 0f, _data.playerZ);
            if (controller != null) controller.enabled = true;
            movement.ResetMotion();

            tree.Bind(this);
            StructureStateRecord placed = _data.structures.FirstOrDefault(record =>
                record.definitionId == chestDefinition.StableId);
            chest.Bind(placed);
            combat.EquipTool(_data.equippedTool, false);
            if (chestPreview != null) chestPreview.SetActive(false);
            hud.Bind(this);
            Commit();
        }

        void Update()
        {
            if (_cancelRequested)
            {
                if (_placing) ExitPlacement();
                else if (MenuOpen) hud.ClosePanels();
            }

            if (_placing)
            {
                UpdatePreview();
                if (_placeRequested && Time.time >= _placementReadyAt)
                    TryPlaceChest();
            }

            _placeRequested = false;
            _cancelRequested = false;
            hud?.Refresh();
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Commit();
        }

        void OnApplicationQuit() => Commit();

        void OnPlacePerformed(InputAction.CallbackContext context) => _placeRequested = true;
        void OnCancelPerformed(InputAction.CallbackContext context) => _cancelRequested = true;

        public NodeStateRecord GetOrCreateNodeState(string objectId)
        {
            NodeStateRecord state = _data.nodes.FirstOrDefault(node => node.objectId == objectId);
            if (state != null) return state;
            state = new NodeStateRecord { objectId = objectId };
            _data.nodes.Add(state);
            return state;
        }

        public void CompleteHarvest(HarvestDefinition definition)
        {
            if (definition == null || definition.YieldItem == null) return;
            AddWood(definition.YieldCount);
            _data.loggingExperience += definition.LoggingExperience;
            Commit();
            hud.ShowStatus($"+{definition.YieldCount} Wood   +{definition.LoggingExperience} Logging XP");
        }

        public bool TryInteract()
        {
            if (_placing) return true;
            if (MenuOpen)
            {
                hud.ClosePanels();
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

        public string ContextPrompt()
        {
            if (_placing) return "Place chest: aim and click / A   •   Esc / B cancels";
            if (MenuOpen) return "E or Esc / B closes the panel";
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

            AddWood(-chestRecipe.IngredientCount);
            _data.pendingChest = true;
            Commit();
            hud.ClosePanels();
            EnterPlacement();
            hud.ShowStatus("Chest crafted. Place it within the blue home boundary.");
            return true;
        }

        public void DepositAllWood()
        {
            if (!chest.IsPlaced) return;
            int deposited = chest.Deposit(CountWood());
            if (deposited == 0) return;
            AddWood(-deposited);
            Commit();
            hud.ShowStatus($"Stored {deposited} Wood.");
        }

        public void WithdrawAllWood()
        {
            if (!chest.IsPlaced) return;
            int withdrawn = chest.WithdrawAll();
            if (withdrawn == 0) return;
            AddWood(withdrawn);
            Commit();
            hud.ShowStatus($"Took {withdrawn} Wood.");
        }

        public void ToolChanged(string toolId)
        {
            if (_data == null) return;
            _data.equippedTool = toolId;
            Commit();
        }

        public void Commit()
        {
            if (_data == null || _repository == null || !_savingEnabled) return;
            _data.playerX = transform.position.x;
            _data.playerZ = transform.position.z;
            try { _repository.Save(_data); }
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

        void AddWood(int amount)
        {
            ItemStackRecord stack = _data.backpack.FirstOrDefault(item => item.itemId == wood.StableId);
            if (stack == null)
            {
                stack = new ItemStackRecord { itemId = wood.StableId };
                _data.backpack.Add(stack);
            }
            stack.count = Mathf.Max(0, stack.count + amount);
        }

        int CountWood() => _data.backpack.FirstOrDefault(item => item.itemId == wood.StableId)?.count ?? 0;

        float DistanceSquared(Transform target)
        {
            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude;
        }
    }
}
