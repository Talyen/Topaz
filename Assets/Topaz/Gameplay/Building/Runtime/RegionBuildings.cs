using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.Combat;
using Topaz.Player;
using UnityEngine;

namespace Topaz.Gameplay
{
    /// <summary>World-owned home decoration, placement, and full material refunds.</summary>
    public sealed partial class RegionBuildings : MonoBehaviour
    {
        public const string PathId = "structure.stone_path";
        public const string AnvilId = "structure.blacksmith_anvil";
        const string StarterBedrollId = "home.starter-bedroll";

        [SerializeField] SafeZone home;
        [SerializeField] PlayerController player;
        [SerializeField] Transform workbench;
        [SerializeField] Transform restPoint;
        [SerializeField] Transform gate;
        [SerializeField] Transform gearRack;
        [SerializeField] Transform campfire;
        [SerializeField] StorageChest chest;
        [SerializeField] GameObject pathTemplate;
        [SerializeField] GameObject anvilTemplate;
        [SerializeField] GameObject bedTemplate;
        [SerializeField] GameObject tableTemplate;
        [SerializeField] GameObject lanternTemplate;

        readonly Dictionary<string, GameObject> _visuals = new Dictionary<string, GameObject>();
        readonly Dictionary<string, StorageChest> _chests = new Dictionary<string, StorageChest>();
        readonly List<HarvestTree> _trees = new List<HarvestTree>();
        readonly List<MiningRock> _rocks = new List<MiningRock>();
        WorldSession _session;
        TopazWorldData _world;
        List<StructureStateRecord> _records;
        GameObject _preview;
        Renderer[] _previewRenderers;
        MaterialPropertyBlock _previewColor;
        string _placingId;
        bool _editing;
        bool _moving;
        StructureStateRecord _selected;
        Vector3 _position;
        int _quarterTurns;
        float _readyAt;
        Vector3 _startingBedrollPosition;

        public bool Active => _placingId != null || _editing;
        public bool IsEditing => _editing;
        public string PlacingId => _placingId;
        public string RegionId { get; private set; }
        public IEnumerable<Campfire> Campfires => _visuals.Values.Where(v => v != null && v.activeInHierarchy).Select(v => v.GetComponent<Campfire>()).Where(v => v != null);
        public IEnumerable<StorageChest> Chests => _chests.Values;
        public IReadOnlyList<StructureStateRecord> Records => _records;

        void Awake()
        {
            if (restPoint != null) _startingBedrollPosition = restPoint.position;

        }

        public void Bind(WorldSession session, TopazWorldData world, string regionId)
        {
            Cancel();
            foreach (GameObject visual in _visuals.Values)
                if (visual != null && visual != chest.gameObject && visual != restPoint.gameObject)
                { visual.SetActive(false); Destroy(visual); }
            _visuals.Clear();
            _chests.Clear();
            chest.Bind(null);
            chest.gameObject.SetActive(false);
            restPoint.gameObject.SetActive(false);
            _session = session;
            _world = world;
            RegionId = regionId;
            region = FindObjectsByType<Topaz.Generation.WoodlandRegion>(FindObjectsSortMode.None).FirstOrDefault(r => r.regionId == regionId);
            _records = world.structures.Where(r => r.regionId == regionId).ToList();
            _trees.Clear();
            _trees.AddRange(FindObjectsByType<HarvestTree>(FindObjectsSortMode.None));
            _rocks.Clear();
            _rocks.AddRange(FindObjectsByType<MiningRock>(FindObjectsSortMode.None));
            if (!world.starterBedrollInitialized && regionId == TopazSaveData.HomeRegion)
            {
                _records.Add(new StructureStateRecord
                {
                    instanceId = StarterBedrollId,
                    definitionId = BuildCatalog.Bedroll,
                    regionId = RegionId,
                    x = _startingBedrollPosition.x - Origin.x,
                    y = _startingBedrollPosition.y - Origin.y,
                    z = _startingBedrollPosition.z - Origin.z
                });
                world.structures.Add(_records[_records.Count - 1]);
                world.starterBedrollInitialized = true;
            }
            foreach (StructureStateRecord record in _records)
                if (Known(record.definitionId)) Show(record);
            Physics.SyncTransforms();
            if (region?.navigation != null) region.navigation.BuildNavMesh();
            
        }

        public bool BeginPlacement(string id)
        {
            if (_session == null || _session.CurrentRegionId != RegionId ||
                BuildCatalog.Find(id) == null || _session.IsPlacing || Active) return false;
            _placingId = id;
            _quarterTurns = 0;
            _preview = CreateVisual(id);
            _preview.name = id + " Preview";
            _preview.SetActive(true);
            _previewRenderers = _preview.GetComponentsInChildren<Renderer>();
            _previewColor = new MaterialPropertyBlock();
            _readyAt = Time.time + 0.15f;
            foreach (Collider collider in _preview.GetComponentsInChildren<Collider>())
                collider.enabled = false;
            foreach (UnityEngine.AI.NavMeshObstacle obstacle in
                _preview.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>())
                obstacle.enabled = false;
            foreach (var safety in _preview.GetComponentsInChildren<CampSafety>()) safety.enabled = false;
            foreach (var fire in _preview.GetComponentsInChildren<Campfire>()) fire.enabled = false;
            foreach (Light light in _preview.GetComponentsInChildren<Light>())
                light.enabled = false;
            foreach (HomeDoor door in _preview.GetComponentsInChildren<HomeDoor>())
                door.enabled = false;
            foreach (HomeRoofVisibility roof in _preview.GetComponentsInChildren<HomeRoofVisibility>())
                roof.enabled = false;
            foreach (HomeNightLight nightLight in _preview.GetComponentsInChildren<HomeNightLight>())
                nightLight.enabled = false;
            _session.CloseHomeMenus();
            _session.ShowHomeStatus("Place " + BuildCatalog.Find(id).Value.Label +
                ". Rotate, confirm, or cancel.");
            
            return true;
        }

        public bool BeginEdit() => BeginEdit(false);
        public bool BeginMove() => BeginEdit(true);

        bool BeginEdit(bool moving)
        {
            if (_session == null || _session.CurrentRegionId != RegionId ||
                _session.IsPlacing || Active) return false;
            _editing = true;
            _moving = moving;
            _readyAt = Time.time + 0.15f;
            _session.CloseHomeMenus();
            _session.ShowHomeStatus(moving ? "Aim at a build to move it." :
                "Aim at a build to remove and refund it.");
            
            return true;
        }

        public void Rotate()
        {
            if (_placingId != null) _quarterTurns = (_quarterTurns + 1) % 4;
        }

        public void Tick(bool confirm, bool cancel)
        {
            if (cancel) { Cancel(); return; }
            Vector3 aim = player.AimPointOnGround;
            float grid = BuildingSettings.Current.grid;
            _position = new Vector3(Mathf.Round((aim.x-Origin.x) / grid) * grid + Origin.x, 0f,
                Mathf.Round((aim.z-Origin.z) / grid) * grid + Origin.z);
            _position.y = PlacementHeight(_position, _placingId);
            if (_placingId != null)
            {
                if (_preview == null) { Cancel(); return; }
                _preview.transform.SetPositionAndRotation(_position,
                    Quaternion.Euler(0f, _quarterTurns * 90f, 0f));
                bool valid = CanPlace(_placingId, _position, _quarterTurns, _selected);
                _preview.transform.localScale = valid
                    ? Vector3.one : Vector3.one * 0.92f;
                _previewColor.SetColor("_BaseColor", valid
                    ? new Color(0.57f, 0.9f, 0.63f) : new Color(0.98f, 0.48f, 0.43f));
                foreach (Renderer renderer in _previewRenderers)
                    renderer.SetPropertyBlock(_previewColor);
                if (confirm && Time.time >= _readyAt) ConfirmPlacement();
            }
            else if (_editing)
            {
                StructureStateRecord nearest = null;
                float distance = 0.7f * 0.7f;
                foreach (StructureStateRecord record in _records)
                {
                    if (!Known(record.definitionId)) continue;
                    float candidate = (Position(record) - aim).sqrMagnitude;
                    if (candidate >= distance) continue;
                    distance = candidate;
                    nearest = record;
                }
                if (_selected != nearest)
                {
                    if (_selected != null && _visuals.TryGetValue(_selected.instanceId, out GameObject old))
                        old.transform.localScale = Vector3.one;
                    _selected = nearest;
                    if (_selected != null && _visuals.TryGetValue(_selected.instanceId, out GameObject current))
                        current.transform.localScale = Vector3.one * 1.08f;
                }
                if (confirm && _selected != null && Time.time >= _readyAt)
                {
                    if (_moving)
                    {
                        _placingId = _selected.definitionId;
                        _quarterTurns = _selected.quarterTurns;
                        _editing = false;
                        if (_visuals.TryGetValue(_selected.instanceId, out GameObject original))
                            original.transform.localScale = Vector3.one;
                        _preview = CreateVisual(_placingId);
                        _previewRenderers = _preview.GetComponentsInChildren<Renderer>();
                        _previewColor = new MaterialPropertyBlock();
                        foreach (Collider collider in _preview.GetComponentsInChildren<Collider>())
                            collider.enabled = false;
                        foreach (UnityEngine.AI.NavMeshObstacle obstacle in
                            _preview.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>())
                            obstacle.enabled = false;
                        foreach (HomeDoor door in _preview.GetComponentsInChildren<HomeDoor>())
                            door.enabled = false;
                        foreach (HomeRoofVisibility roof in _preview.GetComponentsInChildren<HomeRoofVisibility>())
                            roof.enabled = false;
                        foreach (HomeNightLight nightLight in _preview.GetComponentsInChildren<HomeNightLight>())
                            nightLight.enabled = false;
                        _readyAt = Time.time + .15f;
                        _session.ShowHomeStatus("Choose a new spot. Cancel keeps the old one.");
                    }
                    else RemoveSelected();
                }
            }
        }

        public void Cancel()
        {
            if (_preview != null) Destroy(_preview);
            _preview = null;
            _previewRenderers = null;
            _previewColor = null;
            _placingId = null;
            _editing = false;
            _moving = false;
            if (_selected != null && _visuals.TryGetValue(_selected.instanceId, out GameObject visual) &&
                visual != null) visual.transform.localScale = Vector3.one;
            _selected = null;
            
        }

        bool CanPlace(string id, Vector3 position, int quarterTurns,
            StructureStateRecord moving = null)
        {
            float radius = Footprint(id);
            if (!TerrainAllows(id, position, radius, moving)) return false;
            if (Near(position, player.transform, radius + 0.5f) ||
                (_session.IsAtHome && Near(position, workbench, radius + 0.65f)) ||
                Near(position, gate, radius + 1.5f) ||
                (_session.IsAtHome && Near(position, gearRack, radius + .9f)) ||
                Near(position, campfire, radius + 1f))
                return false;
            foreach (HarvestTree tree in _trees)
                if (tree != null && tree.IsAvailable && Near(position, tree.transform, radius + .45f))
                    return false;
            foreach (MiningRock rock in _rocks)
                if (rock != null && rock.IsAvailable && Near(position, rock.transform, radius + .7f))
                    return false;
            if (id == BuildCatalog.Wall || id == BuildCatalog.Doorway)
            {
                bool supported = _records.Any(record => record != moving &&
                    record.definitionId == BuildCatalog.Floor &&
                    WallAtFloorEdge(position, quarterTurns, record));
                if (!supported) return false;
            }
            if (id == BuildCatalog.Roof && !_records.Any(record => record != moving &&
                (record.definitionId == BuildCatalog.Wall ||
                 record.definitionId == BuildCatalog.Doorway) &&
                (Position(record) - position).sqrMagnitude <= 2.5f))
                return false;
            foreach (StructureStateRecord record in _records)
            {
                if (record == moving || !Known(record.definitionId)) continue;
                float separation = (position - Position(record)).magnitude;
                if (id == BuildCatalog.Floor || id == BuildCatalog.Roof)
                {
                    if (record.definitionId == id && separation < 1.4f) return false;
                    continue;
                }
                if (record.definitionId == BuildCatalog.Floor ||
                    record.definitionId == BuildCatalog.Roof) continue;
                if (separation < radius + Footprint(record.definitionId)) return false;
            }
            return true;
        }

        bool WallAtFloorEdge(Vector3 point, int rotation, StructureStateRecord floor)
        {
            float dx = Mathf.Abs(point.x - Position(floor).x);
            float dz = Mathf.Abs(point.z - Position(floor).z);
            return rotation % 2 == 0 ? dx < .1f && Mathf.Abs(dz - .75f) < .1f :
                dz < .1f && Mathf.Abs(dx - .75f) < .1f;
        }

        static bool Near(Vector3 point, Transform target, float radius)
        {
            if (target == null) return false;
            Vector3 delta = point - target.position;
            delta.y = 0f;
            return delta.sqrMagnitude < radius * radius;
        }

        void ConfirmPlacement()
        {
            if (!CanPlace(_placingId, _position, _quarterTurns, _selected))
            {
                _session.ShowHomeStatus("Choose a clear, supported spot in a clear, level area.");
                return;
            }
            if (_selected != null)
            {
                MoveRecord(_selected, _position, _quarterTurns);
                Cancel();
                _session.Commit();
                _session.ShowHomeStatus("Build moved.");
                return;
            }
            BuildCatalog.Entry entry = BuildCatalog.Find(_placingId).Value;
            if (!_session.TrySpendHomeMaterials(entry.Wood, entry.Stone, entry.Iron))
            {
                _session.ShowHomeStatus("Not enough materials in the backpack and regional chests.");
                return;
            }
            var record = new StructureStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = _placingId,
                regionId = RegionId,
                x = _position.x - Origin.x,
                y = _position.y - Origin.y,
                z = _position.z - Origin.z,
                quarterTurns = _quarterTurns
            };
            _records.Add(record);
            _world.structures.Add(record);
            if (record.definitionId == BuildCatalog.Camp) _session.DiscoverCamp(record.instanceId);
            Show(record);
            _session.RebuildRegionNavigation();
            _session.RefreshHomeGatherables();
            string label = entry.Label;
            Cancel();
            _session.Commit();
            _session.ShowHomeStatus(label + " placed.");
        }

        void RemoveSelected()
        {
            StructureStateRecord record = _selected;
            if (!_records.Remove(record)) return;
            _world.structures.Remove(record);
            if (record.definitionId == BuildCatalog.Camp) _session.ForgetCamp(record.instanceId);
            _selected = null;
            if (_chests.TryGetValue(record.instanceId, out StorageChest stored))
            {
                foreach (ItemStackRecord slot in record.slots)
                    if (slot != null && slot.count > 0)
                        _session.DropHomeItem(slot.itemId, slot.count,
                            Position(record));
                _chests.Remove(record.instanceId);
                stored.Bind(null);
                if (stored != chest) Destroy(stored.gameObject);
                _session.OnChestRemoved(stored);
            }
            if (_visuals.TryGetValue(record.instanceId, out GameObject visual) && visual != null)
            {
                if (visual == restPoint.gameObject) visual.SetActive(false);
                else if (visual != chest.gameObject) { visual.SetActive(false); Destroy(visual); }
            }
            _visuals.Remove(record.instanceId);
            _session.RebuildRegionNavigation();
            _session.RefreshHomeGatherables();
            Vector3 position = Position(record);
            BuildCatalog.Entry? entry = BuildCatalog.Find(record.definitionId);
            if (entry.HasValue)
            {
                _session.RefundHomeMaterial(_session.WoodItem, entry.Value.Wood, position);
                _session.RefundHomeMaterial(_session.StoneItem, entry.Value.Stone, position);
                _session.RefundHomeMaterial(_session.IronItem, entry.Value.Iron, position);
            }
            _session.Commit();
            _session.ShowHomeStatus("Build removed; contents dropped and materials refunded.");
        }

        GameObject Template(string id) => id == PathId ? pathTemplate :
            id == AnvilId ? anvilTemplate : null;

        GameObject CreateVisual(string id)
        {
            if (id == BuildCatalog.Camp) return CreateCamp();
            if (id == BuildCatalog.Chest) return Instantiate(chest.gameObject, transform);
            if (id == BuildCatalog.Bedroll) return Instantiate(restPoint.gameObject, transform);
            if (id == BuildCatalog.Bed && bedTemplate != null)
                return Instantiate(bedTemplate, transform);
            if (id == BuildCatalog.Table && tableTemplate != null)
                return Instantiate(tableTemplate, transform);
            if (id == BuildCatalog.Lantern && lanternTemplate != null)
                return Instantiate(lanternTemplate, transform);
            if (Template(id) != null) return Instantiate(Template(id), transform);
            return BuildVisuals.Create(id, transform, player.transform);
        }

        void Show(StructureStateRecord record)
        {
            GameObject visual;
            if (record.definitionId == BuildCatalog.Chest)
            {
                StorageChest instance = _chests.Count == 0 ? chest : Instantiate(chest, transform);
                instance.Bind(record);
                _chests.Add(record.instanceId, instance);
                visual = instance.gameObject;
            }
            else if (record.definitionId == BuildCatalog.Bedroll &&
                     record.instanceId == StarterBedrollId)
            {
                visual = restPoint.gameObject;
                visual.SetActive(true);
            }
            else visual = CreateVisual(record.definitionId);
            if (record.instanceId != StarterBedrollId)
                visual.name = record.definitionId + " " + record.instanceId;
            visual.transform.SetPositionAndRotation(Position(record),
                Quaternion.Euler(0f, record.quarterTurns * 90f, 0f));
            visual.SetActive(true);
            if (record.definitionId == BuildCatalog.Camp) visual.GetComponent<Campfire>().Configure(record.instanceId, RegionId, record.label);
            _visuals.Add(record.instanceId, visual);
        }

        public StorageChest NearestChest(Vector3 position, float radius)
        {
            StorageChest result = null;
            float best = radius * radius;
            foreach (StorageChest candidate in _chests.Values)
            {
                if (candidate == null || !candidate.IsPlaced) continue;
                Vector3 delta = candidate.transform.position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude >= best) continue;
                best = delta.sqrMagnitude;
                result = candidate;
            }
            return result;
        }

        public Transform NearestRest(Vector3 position, float radius)
        {
            Transform result = null;
            float best = radius * radius;
            foreach (StructureStateRecord record in _records)
            {
                if (record.definitionId != BuildCatalog.Bedroll &&
                    record.definitionId != BuildCatalog.Bed) continue;
                if (!_visuals.TryGetValue(record.instanceId, out GameObject visual) || visual == null)
                    continue;
                Vector3 delta = visual.transform.position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude >= best) continue;
                best = delta.sqrMagnitude;
                result = visual.transform;
            }
            return result;
        }

        public Transform NearestAnvil(Vector3 position, float radius)
        {
            Transform result = null;
            float best = radius * radius;
            foreach (StructureStateRecord record in _records)
            {
                if (record.definitionId != AnvilId ||
                    !_visuals.TryGetValue(record.instanceId, out GameObject visual) ||
                    visual == null) continue;
                Vector3 delta = visual.transform.position - position;
                delta.y = 0f;
                if (delta.sqrMagnitude >= best) continue;
                best = delta.sqrMagnitude;
                result = visual.transform;
            }
            return result;
        }

        public bool BlocksResource(Vector3 position, float radius) => _records != null &&
            _records.Any(record => record.definitionId != PathId &&
                (Position(record) - position).sqrMagnitude <
                (radius + Footprint(record.definitionId)) * (radius + Footprint(record.definitionId)));

        static bool Known(string id) => id == BuildCatalog.Bedroll ||
            BuildCatalog.Find(id).HasValue;

        static float Footprint(string id) => id == BuildCatalog.Floor ||
            id == BuildCatalog.Roof ? .72f : id == AnvilId ? .9f :
            id == BuildCatalog.Wall || id == BuildCatalog.Doorway ? .25f : .45f;

    }
}
