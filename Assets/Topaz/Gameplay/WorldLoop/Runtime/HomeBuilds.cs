using System;
using System.Collections.Generic;
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

        [SerializeField] SafeZone home;
        [SerializeField] FeelStudyPlayer player;
        [SerializeField] Transform workbench;
        [SerializeField] Transform restPoint;
        [SerializeField] Transform gate;
        [SerializeField] Transform campfire;
        [SerializeField] StorageChest chest;
        [SerializeField] GameObject pathTemplate;
        [SerializeField] GameObject anvilTemplate;

        readonly Dictionary<string, GameObject> _visuals = new Dictionary<string, GameObject>();
        WorldSession _session;
        List<StructureStateRecord> _records;
        GameObject _preview;
        Renderer[] _previewRenderers;
        MaterialPropertyBlock _previewColor;
        string _placingId;
        bool _editing;
        StructureStateRecord _selected;
        Vector3 _position;
        float _readyAt;

        public bool Active => _placingId != null || _editing;
        public bool IsEditing => _editing;
        public string PlacingId => _placingId;

        public void Bind(WorldSession session, List<StructureStateRecord> records)
        {
            Cancel();
            foreach (GameObject visual in _visuals.Values)
                if (visual != null) Destroy(visual);
            _visuals.Clear();
            _session = session;
            _records = records;
            foreach (StructureStateRecord record in records)
                if (Template(record.definitionId) != null) Show(record);
        }

        public bool BeginPlacement(string id)
        {
            if (_session == null || _session.CurrentRegionId != TopazSaveData.HomeRegion ||
                Template(id) == null || _session.IsPlacing || Active) return false;
            _placingId = id;
            _preview = Instantiate(Template(id), transform);
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
            _session.CloseHomeMenus();
            _session.ShowHomeStatus(id == PathId ?
                "Place a Stone path slab. Confirm or cancel." :
                "Place a Blacksmith's Anvil. Confirm or cancel.");
            return true;
        }

        public bool BeginEdit()
        {
            if (_session == null || _session.CurrentRegionId != TopazSaveData.HomeRegion ||
                _session.IsPlacing || Active) return false;
            _editing = true;
            _readyAt = Time.time + 0.15f;
            _session.CloseHomeMenus();
            _session.ShowHomeStatus("Aim at a path or Anvil to remove and refund it. Cancel to finish.");
            return true;
        }

        public void Tick(bool confirm, bool cancel)
        {
            if (cancel) { Cancel(); return; }
            Vector3 aim = player.AimPointOnGround;
            _position = new Vector3(Mathf.Round(aim.x / 0.75f) * 0.75f, 0f,
                Mathf.Round(aim.z / 0.75f) * 0.75f);
            if (_placingId != null)
            {
                _preview.transform.position = _position;
                bool valid = CanPlace(_placingId, _position);
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
                    if (Template(record.definitionId) == null) continue;
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
                if (confirm && _selected != null && Time.time >= _readyAt) RemoveSelected();
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
            if (_selected != null && _visuals.TryGetValue(_selected.instanceId, out GameObject visual) &&
                visual != null) visual.transform.localScale = Vector3.one;
            _selected = null;
        }

        bool CanPlace(string id, Vector3 position)
        {
            float radius = id == AnvilId ? 0.9f : 0.36f;
            if (!home.Contains(position) ||
                (position - home.transform.position).magnitude > home.Radius - radius)
                return false;
            if (Near(position, player.transform, radius + 0.5f) ||
                Near(position, workbench, radius + 0.65f) ||
                Near(position, restPoint, radius + 0.65f) ||
                Near(position, gate, radius + 0.8f) ||
                Near(position, campfire, radius + 0.7f) ||
                (chest != null && chest.IsPlaced && Near(position, chest.transform, radius + 0.65f)))
                return false;
            foreach (StructureStateRecord record in _records)
            {
                if (Template(record.definitionId) == null) continue;
                float otherRadius = record.definitionId == AnvilId ? 0.9f : 0.36f;
                if ((position - new Vector3(record.x, 0f, record.z)).sqrMagnitude <
                    (radius + otherRadius) * (radius + otherRadius)) return false;
            }
            return true;
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
            if (!CanPlace(_placingId, _position))
            {
                _session.ShowHomeStatus("Choose a clear spot inside the home boundary.");
                return;
            }
            int stone = _placingId == AnvilId ? 6 : 1;
            int iron = _placingId == AnvilId ? 2 : 0;
            if (!_session.TrySpendHomeMaterials(stone, iron))
            {
                _session.ShowHomeStatus("Not enough Stone or Iron in the backpack and home chest.");
                return;
            }
            var record = new StructureStateRecord
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = _placingId,
                x = _position.x,
                z = _position.z
            };
            _records.Add(record);
            Show(record);
            string label = _placingId == AnvilId ? "Blacksmith's Anvil" : "Stone path";
            Cancel();
            _session.Commit();
            _session.ShowHomeStatus(label + " placed.");
        }

        void RemoveSelected()
        {
            StructureStateRecord record = _selected;
            if (!_records.Remove(record)) return;
            _selected = null;
            if (_visuals.TryGetValue(record.instanceId, out GameObject visual) && visual != null)
                Destroy(visual);
            _visuals.Remove(record.instanceId);
            Vector3 position = new Vector3(record.x, 0f, record.z);
            _session.RefundHomeMaterial(_session.StoneItem,
                record.definitionId == AnvilId ? 6 : 1, position);
            if (record.definitionId == AnvilId)
                _session.RefundHomeMaterial(_session.IronItem, 2, position);
            _session.Commit();
            _session.ShowHomeStatus("Build removed; materials refunded.");
        }

        GameObject Template(string id) => id == PathId ? pathTemplate :
            id == AnvilId ? anvilTemplate : null;

        void Show(StructureStateRecord record)
        {
            GameObject visual = Instantiate(Template(record.definitionId), transform);
            visual.name = record.definitionId + " " + record.instanceId;
            visual.transform.position = new Vector3(record.x, 0f, record.z);
            visual.SetActive(true);
            _visuals.Add(record.instanceId, visual);
        }
    }
}
