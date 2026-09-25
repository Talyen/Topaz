using System;
using System.Collections.Generic;
using System.Linq;
using Topaz.CombatStudy;
using Topaz.FeelStudy;
using UnityEngine;

namespace Topaz.LoopStudy
{
    /// <summary>World-owned home decoration, placement, and full material refunds.</summary>
    public sealed class HomeBuilds : MonoBehaviour
    {
        public const string PathId = "structure.stone_path";
        public const string AnvilId = "structure.blacksmith_anvil";
        const string StarterBedrollId = "home.starter-bedroll";

        [SerializeField] SafeZone home;
        [SerializeField] FeelStudyPlayer player;
        [SerializeField] Transform workbench;
        [SerializeField] Transform restPoint;
        [SerializeField] Transform gate;
        [SerializeField] Transform cryptGate;
        [SerializeField] Transform gearRack;
        [SerializeField] Transform campfire;
        [SerializeField] StorageChest chest;
        [SerializeField] GameObject pathTemplate;
        [SerializeField] GameObject anvilTemplate;
        [SerializeField] GameObject bedTemplate;
        [SerializeField] GameObject tableTemplate;
        [SerializeField] GameObject lanternTemplate;
        [SerializeField] Collider finalHomeArea;

        readonly Dictionary<string, GameObject> _visuals = new Dictionary<string, GameObject>();
        readonly Dictionary<string, StorageChest> _chests = new Dictionary<string, StorageChest>();
        readonly List<HarvestTree> _trees = new List<HarvestTree>();
        readonly List<MiningRock> _rocks = new List<MiningRock>();
        readonly List<GameObject> _hearthDecor = new List<GameObject>();
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
        LineRenderer _boundary;
        Vector3 _startingBedrollPosition;

        public bool Active => _placingId != null || _editing;
        public bool IsEditing => _editing;
        public string PlacingId => _placingId;
        public int CampfireTier => _world?.campfireTier ?? 1;
        public IEnumerable<StorageChest> Chests => _chests.Values;
        public IReadOnlyList<StructureStateRecord> Records => _records;

        void Awake()
        {
            if (restPoint != null) _startingBedrollPosition = restPoint.position;
            var line = new GameObject("Build boundary");
            line.transform.SetParent(transform, false);
            _boundary = line.AddComponent<LineRenderer>();
            _boundary.useWorldSpace = true;
            _boundary.loop = true;
            _boundary.positionCount = 64;
            _boundary.startWidth = _boundary.endWidth = .07f;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader != null)
            {
                _boundary.material = new Material(shader) { color =
                    new Color(.94f, .78f, .52f, .8f) };
            }
            _boundary.enabled = false;
        }

        public void Bind(WorldSession session, TopazWorldData world)
        {
            Cancel();
            foreach (GameObject visual in _visuals.Values)
                if (visual != null && visual != chest.gameObject && visual != restPoint.gameObject)
                    Destroy(visual);
            _visuals.Clear();
            _chests.Clear();
            chest.Bind(null);
            restPoint.gameObject.SetActive(false);
            _session = session;
            _world = world;
            _records = world.structures;
            _trees.Clear();
            _trees.AddRange(FindObjectsByType<HarvestTree>(FindObjectsSortMode.None));
            _rocks.Clear();
            _rocks.AddRange(FindObjectsByType<MiningRock>(FindObjectsSortMode.None));
            if (!world.starterBedrollInitialized)
            {
                _records.Add(new StructureStateRecord
                {
                    instanceId = StarterBedrollId,
                    definitionId = HomeBuildCatalog.Bedroll,
                    x = _startingBedrollPosition.x,
                    z = _startingBedrollPosition.z
                });
                world.starterBedrollInitialized = true;
            }
            foreach (StructureStateRecord record in _records)
                if (Known(record.definitionId)) Show(record);
            RefreshHearth();
            UpdateBoundary();
        }

        public bool BeginPlacement(string id)
        {
            if (_session == null || _session.CurrentRegionId != TopazSaveData.HomeRegion ||
                HomeBuildCatalog.Find(id) == null || _session.IsPlacing || Active) return false;
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
            foreach (Light light in _preview.GetComponentsInChildren<Light>())
                light.enabled = false;
            foreach (HomeDoor door in _preview.GetComponentsInChildren<HomeDoor>())
                door.enabled = false;
            foreach (HomeRoofVisibility roof in _preview.GetComponentsInChildren<HomeRoofVisibility>())
                roof.enabled = false;
            foreach (HomeNightLight nightLight in _preview.GetComponentsInChildren<HomeNightLight>())
                nightLight.enabled = false;
            _session.CloseHomeMenus();
            _session.ShowHomeStatus("Place " + HomeBuildCatalog.Find(id).Value.Label +
                ". Rotate, confirm, or cancel.");
            UpdateBoundary();
            return true;
        }

        public bool BeginEdit() => BeginEdit(false);
        public bool BeginMove() => BeginEdit(true);

        bool BeginEdit(bool moving)
        {
            if (_session == null || _session.CurrentRegionId != TopazSaveData.HomeRegion ||
                _session.IsPlacing || Active) return false;
            _editing = true;
            _moving = moving;
            _readyAt = Time.time + 0.15f;
            _session.CloseHomeMenus();
            _session.ShowHomeStatus(moving ? "Aim at a build to move it." :
                "Aim at a build to remove and refund it.");
            UpdateBoundary();
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
            _position = new Vector3(Mathf.Round(aim.x / 0.75f) * 0.75f, 0f,
                Mathf.Round(aim.z / 0.75f) * 0.75f);
            if (_placingId != null)
            {
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
                    float candidate = (new Vector3(record.x, 0f, record.z) - aim).sqrMagnitude;
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
            UpdateBoundary();
        }

        bool CanPlace(string id, Vector3 position, int quarterTurns,
            StructureStateRecord moving = null)
        {
            float radius = Footprint(id);
            Vector3 fromCenter = position - home.transform.position;
            fromCenter.y = 0f;
            if (CampfireTier < 3 && fromCenter.magnitude >
                (CampfireTier == 1 ? home.Radius : 9f) - radius) return false;
            if (CampfireTier == 3 && finalHomeArea != null &&
                (finalHomeArea.ClosestPoint(position) - position).sqrMagnitude > .04f)
                return false;
            if (CampfireTier == 3 && finalHomeArea == null &&
                !UnityEngine.AI.NavMesh.SamplePosition(position, out _, .5f,
                    UnityEngine.AI.NavMesh.AllAreas)) return false;
            if (Near(position, player.transform, radius + 0.5f) ||
                Near(position, workbench, radius + 0.65f) ||
                Near(position, gate, radius + 1.5f) ||
                Near(position, cryptGate, radius + 1.5f) ||
                Near(position, gearRack, radius + .9f) ||
                Near(position, campfire, radius + 1f))
                return false;
            foreach (HarvestTree tree in _trees)
                if (tree != null && tree.IsAvailable && Near(position, tree.transform, radius + .45f))
                    return false;
            foreach (MiningRock rock in _rocks)
                if (rock != null && rock.IsAvailable && Near(position, rock.transform, radius + .7f))
                    return false;
            if (id == HomeBuildCatalog.Wall || id == HomeBuildCatalog.Doorway)
            {
                bool supported = _records.Any(record => record != moving &&
                    record.definitionId == HomeBuildCatalog.Floor &&
                    WallAtFloorEdge(position, quarterTurns, record));
                if (!supported) return false;
            }
            if (id == HomeBuildCatalog.Roof && !_records.Any(record => record != moving &&
                (record.definitionId == HomeBuildCatalog.Wall ||
                 record.definitionId == HomeBuildCatalog.Doorway) &&
                (new Vector3(record.x, 0f, record.z) - position).sqrMagnitude <= 2.5f))
                return false;
            foreach (StructureStateRecord record in _records)
            {
                if (record == moving || !Known(record.definitionId)) continue;
                float separation = (position - new Vector3(record.x, 0f, record.z)).magnitude;
                if (id == HomeBuildCatalog.Floor || id == HomeBuildCatalog.Roof)
                {
                    if (record.definitionId == id && separation < 1.4f) return false;
                    continue;
                }
                if (record.definitionId == HomeBuildCatalog.Floor ||
                    record.definitionId == HomeBuildCatalog.Roof) continue;
                if (separation < radius + Footprint(record.definitionId)) return false;
            }
            return true;
        }

        static bool WallAtFloorEdge(Vector3 point, int rotation, StructureStateRecord floor)
        {
            float dx = Mathf.Abs(point.x - floor.x);
            float dz = Mathf.Abs(point.z - floor.z);
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
                _session.ShowHomeStatus("Choose a clear, supported spot in the unlocked Home area.");
                return;
            }
            if (_selected != null)
            {
                _selected.x = _position.x;
                _selected.z = _position.z;
                _selected.quarterTurns = _quarterTurns;
                if (_visuals.TryGetValue(_selected.instanceId, out GameObject moved))
                    moved.transform.SetPositionAndRotation(_position,
                        Quaternion.Euler(0f, _quarterTurns * 90f, 0f));
                Cancel();
                _session.Commit();
                _session.ShowHomeStatus("Build moved.");
                return;
            }
            HomeBuildCatalog.Entry entry = HomeBuildCatalog.Find(_placingId).Value;
            if (!_session.TrySpendHomeMaterials(entry.Wood, entry.Stone, entry.Iron))
            {
                _session.ShowHomeStatus("Not enough materials in the backpack and home chests.");
                return;
            }
            var record = new StructureStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = _placingId,
                x = _position.x,
                z = _position.z,
                quarterTurns = _quarterTurns
            };
            _records.Add(record);
            Show(record);
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
            _selected = null;
            if (_chests.TryGetValue(record.instanceId, out StorageChest stored))
            {
                foreach (ItemStackRecord slot in record.slots)
                    if (slot != null && slot.count > 0)
                        _session.DropHomeItem(slot.itemId, slot.count,
                            new Vector3(record.x, 0f, record.z));
                _chests.Remove(record.instanceId);
                stored.Bind(null);
                if (stored != chest) Destroy(stored.gameObject);
                _session.OnChestRemoved(stored);
            }
            if (_visuals.TryGetValue(record.instanceId, out GameObject visual) && visual != null)
            {
                if (visual == restPoint.gameObject) visual.SetActive(false);
                else if (visual != chest.gameObject) Destroy(visual);
            }
            _visuals.Remove(record.instanceId);
            _session.RefreshHomeGatherables();
            Vector3 position = new Vector3(record.x, 0f, record.z);
            HomeBuildCatalog.Entry? entry = HomeBuildCatalog.Find(record.definitionId);
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
            if (id == HomeBuildCatalog.Chest) return Instantiate(chest.gameObject, transform);
            if (id == HomeBuildCatalog.Bedroll) return Instantiate(restPoint.gameObject, transform);
            if (id == HomeBuildCatalog.Bed && bedTemplate != null)
                return Instantiate(bedTemplate, transform);
            if (id == HomeBuildCatalog.Table && tableTemplate != null)
                return Instantiate(tableTemplate, transform);
            if (id == HomeBuildCatalog.Lantern && lanternTemplate != null)
                return Instantiate(lanternTemplate, transform);
            if (Template(id) != null) return Instantiate(Template(id), transform);
            return HomeBuildVisuals.Create(id, transform, player.transform);
        }

        void Show(StructureStateRecord record)
        {
            GameObject visual;
            if (record.definitionId == HomeBuildCatalog.Chest)
            {
                StorageChest instance = _chests.Count == 0 ? chest : Instantiate(chest, transform);
                instance.Bind(record);
                _chests.Add(record.instanceId, instance);
                visual = instance.gameObject;
            }
            else if (record.definitionId == HomeBuildCatalog.Bedroll &&
                     record.instanceId == StarterBedrollId)
            {
                visual = restPoint.gameObject;
                visual.SetActive(true);
            }
            else visual = CreateVisual(record.definitionId);
            if (record.instanceId != StarterBedrollId)
                visual.name = record.definitionId + " " + record.instanceId;
            visual.transform.SetPositionAndRotation(new Vector3(record.x, 0f, record.z),
                Quaternion.Euler(0f, record.quarterTurns * 90f, 0f));
            visual.SetActive(true);
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

        public void RegisterLegacyChest(StructureStateRecord record)
        {
            if (record == null || record.definitionId != HomeBuildCatalog.Chest ||
                _visuals.ContainsKey(record.instanceId)) return;
            Show(record);
        }

        public Transform NearestRest(Vector3 position, float radius)
        {
            Transform result = null;
            float best = radius * radius;
            foreach (StructureStateRecord record in _records)
            {
                if (record.definitionId != HomeBuildCatalog.Bedroll &&
                    record.definitionId != HomeBuildCatalog.Bed) continue;
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
                (new Vector3(record.x, 0f, record.z) - position).sqrMagnitude <
                (radius + Footprint(record.definitionId)) * (radius + Footprint(record.definitionId)));

        public bool UpgradeCampfire()
        {
            if (_world == null || CampfireTier >= 3 || Active ||
                _session.CurrentRegionId != TopazSaveData.HomeRegion) return false;
            int wood = CampfireTier == 1 ? 9 : 24;
            int stone = CampfireTier == 1 ? 6 : 24;
            int iron = CampfireTier == 1 ? 0 : 8;
            if (!_session.TrySpendHomeMaterials(wood, stone, iron))
            {
                _session.ShowHomeStatus("Not enough materials for the campfire upgrade.");
                return false;
            }
            _world.campfireTier++;
            RefreshHearth();
            _session.Commit();
            _session.ShowHomeStatus(CampfireTier == 3 ?
                "The whole Home area is open for building." : "The Home building circle expanded.");
            UpdateBoundary();
            return true;
        }

        static bool Known(string id) => id == HomeBuildCatalog.Bedroll ||
            HomeBuildCatalog.Find(id).HasValue;

        static float Footprint(string id) => id == HomeBuildCatalog.Floor ||
            id == HomeBuildCatalog.Roof ? .72f : id == AnvilId ? .9f :
            id == HomeBuildCatalog.Wall || id == HomeBuildCatalog.Doorway ? .25f : .45f;

        void UpdateBoundary()
        {
            if (_boundary == null) return;
            _boundary.enabled = Active && CampfireTier < 3;
            if (!_boundary.enabled) return;
            float radius = CampfireTier == 1 ? home.Radius : 9f;
            int count = _boundary.positionCount;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2f / count;
                _boundary.SetPosition(i, home.transform.position +
                    new Vector3(Mathf.Cos(angle) * radius, .05f,
                        Mathf.Sin(angle) * radius));
            }
        }

        void RefreshHearth()
        {
            foreach (GameObject decoration in _hearthDecor)
                if (decoration != null) Destroy(decoration);
            _hearthDecor.Clear();
            if (campfire == null || CampfireTier < 2) return;
            Transform source = campfire.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "Hearth Stone");
            int stones = CampfireTier == 2 ? 6 : 12;
            float radius = CampfireTier == 2 ? .95f : 1.35f;
            for (int i = 0; i < stones; i++)
            {
                GameObject visual = source != null ? Instantiate(source.gameObject, campfire) :
                    GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Campfire upgrade stone";
                visual.transform.SetParent(campfire, false);
                float angle = i * Mathf.PI * 2f / stones;
                visual.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius,
                    .08f, Mathf.Sin(angle) * radius);
                visual.transform.localRotation = Quaternion.Euler(0f,
                    angle * Mathf.Rad2Deg, 0f);
                visual.SetActive(true);
                _hearthDecor.Add(visual);
            }
        }
    }
}
