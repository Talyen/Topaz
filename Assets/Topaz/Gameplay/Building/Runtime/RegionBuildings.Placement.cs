using System.Linq;
using Topaz.Combat;
using Topaz.Generation;
using UnityEngine;
namespace Topaz.Gameplay
{
    public sealed partial class RegionBuildings
    {
        WoodlandRegion region;
        Vector3 Origin => region != null ? region.regionOffset : Vector3.zero;
        Vector3 Position(StructureStateRecord r) => Origin + new Vector3(r.x, r.y, r.z);
        public bool TryMoveAt(string instanceId, Vector3 position, int rotation = 0)
        {
            if (Active) return false;
            var record = _records.Find(r => r.instanceId == instanceId);
            if (record == null) return false;
            float grid = BuildingSettings.Current.grid;
            position.x = Mathf.Round((position.x-Origin.x)/grid)*grid+Origin.x;
            position.z = Mathf.Round((position.z-Origin.z)/grid)*grid+Origin.z;
            position.y = PlacementHeight(position,record.definitionId);
            int turns = ((rotation % 4) + 4) % 4;
            if (!CanPlace(record.definitionId,position,turns,record)) return false;
            MoveRecord(record,position,turns);
            _session.Commit();
            return true;
        }
        void MoveRecord(StructureStateRecord record, Vector3 position, int turns)
        {
            var previous=Position(record);
            record.x=position.x-Origin.x; record.y=position.y-Origin.y; record.z=position.z-Origin.z;
            record.quarterTurns=turns;
            _session.InvalidateStorageIndex();
            _session.RefreshBuiltGround(previous,Footprint(record.definitionId));
            _session.RefreshBuiltGround(position,Footprint(record.definitionId));
            if (_visuals.TryGetValue(record.instanceId,out var visual))
                visual.transform.SetPositionAndRotation(position,Quaternion.Euler(0,turns*90,0));
            _session.RefreshHomeGatherables();
            _session.RebuildRegionNavigation();
        }
        public bool TryPlaceAt(string id, Vector3 position, int rotation = 0)
        {
            if (Active || !BeginPlacement(id)) return false;
            float grid = BuildingSettings.Current.grid;
            _position = new Vector3(Mathf.Round((position.x-Origin.x)/grid)*grid+Origin.x, 0,
                Mathf.Round((position.z-Origin.z)/grid)*grid+Origin.z);
            _position.y = PlacementHeight(_position,id);
            _quarterTurns = ((rotation % 4) + 4) % 4;
            bool valid = CanPlace(id,_position,_quarterTurns);
            if (valid) ConfirmPlacement();
            bool placed = valid && !Active;
            if (!placed) Cancel();
            return placed;
        }
        float PlacementHeight(Vector3 point, string id)
        {
            float height = WoodlandRegion.GroundHeight(point);
            if (id != BuildCatalog.Floor && id != BuildCatalog.Camp)
                foreach (var floor in _records)
                    if (floor.definitionId == BuildCatalog.Floor &&
                        Mathf.Abs(Position(floor).x-point.x) <= .8f && Mathf.Abs(Position(floor).z-point.z) <= .8f)
                        return Position(floor).y;
            return height;
        }
        bool TerrainAllows(string id, Vector3 position, float radius, StructureStateRecord moving)
        {
            if (region == null || region.Wilderness == null || !region.Streaming.IsReadyAt(position)) return false;
            var settings = BuildingSettings.Current;
            Vector3 local = position - Origin;
            float extent = WildernessPlan.HalfSize - radius - 8;
            if (Mathf.Abs(local.x) > extent || Mathf.Abs(local.z) > extent) return false;
            float reserved = id == BuildCatalog.Camp ? settings.campRadius + settings.enemyClearance : radius + 2;
            foreach (var site in region.Wilderness.Discoveries)
                if (Vector2.Distance(new Vector2(local.x,local.z),new Vector2(site.X,site.Z)) < reserved + 9) return false;
            bool supported = id != BuildCatalog.Floor && id != BuildCatalog.Camp && _records.Any(r =>
                r != moving && r.definitionId == BuildCatalog.Floor &&
                Mathf.Abs(Position(r).x-position.x) <= .8f && Mathf.Abs(Position(r).z-position.z) <= .8f);
            if (!supported)
            {
                float low = float.MaxValue, high = float.MinValue;
                foreach (var offset in new[] { Vector3.zero, new Vector3(-radius,0,-radius), new Vector3(radius,0,-radius), new Vector3(-radius,0,radius), new Vector3(radius,0,radius) })
                {
                    Vector3 site = position + offset;
                    float h = WoodlandRegion.GroundHeight(site);
                    low = Mathf.Min(low,h); high = Mathf.Max(high,h);
                }
                if (high-low > settings.maximumHeightDifference || Mathf.Atan2(high-low, radius*2)*Mathf.Rad2Deg > settings.maximumSlope) return false;
            }
            foreach (var actor in FindObjectsByType<EnemyCombatant>(FindObjectsSortMode.None))
                if (actor.IsAlive && Vector3.Distance(actor.transform.position, position) <
                    (id == BuildCatalog.Camp ? settings.campRadius + settings.enemyClearance : radius + 1)) return false;
            foreach (var collider in Physics.OverlapBox(position + Vector3.up*.7f, new Vector3(radius,.6f,radius), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
            {
                if (collider is TerrainCollider || collider.transform.IsChildOf(transform) ||
                    collider.transform.IsChildOf(player.transform)) continue;
                // Building rules below handle owned structures and active resource footprints.
                if (_visuals.Values.Any(v => v != null && collider.transform.IsChildOf(v.transform))) continue;
                return false;
            }
            return true;
        }
        GameObject CreateCamp()
        {
            var prefab=BuildingSettings.Current.VisualFor(BuildCatalog.Camp);
            if(prefab!=null)return Instantiate(prefab,transform,false);
            var go = new GameObject("Campfire");
            go.transform.SetParent(transform, false);
            for (int i=0;i<8;i++)
            {
                var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stone.transform.SetParent(go.transform,false);
                var renderer=stone.GetComponent<Renderer>();
                renderer.sharedMaterial=BuildingSettings.Current.surfaceMaterial;
                var tint=new MaterialPropertyBlock();tint.SetColor("_BaseColor",new Color(.32f,.35f,.38f));renderer.SetPropertyBlock(tint);
                float angle = i * Mathf.PI / 4;
                stone.transform.localPosition = new Vector3(Mathf.Cos(angle)*.5f,.12f,Mathf.Sin(angle)*.5f);
                stone.transform.localScale = new Vector3(.3f,.24f,.3f);
            }
            var ember=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ember.name="Embers";ember.transform.SetParent(go.transform,false);
            ember.transform.localPosition=Vector3.up*.2f;ember.transform.localScale=new Vector3(.5f,.2f,.5f);
            Destroy(ember.GetComponent<Collider>());
            ember.GetComponent<Renderer>().sharedMaterial=BuildingSettings.Current.emberMaterial;
            var light = go.AddComponent<Light>(); light.type = LightType.Point;
            light.color = new Color(1,.5f,.15f); light.range = 7; light.intensity = 2;
            go.AddComponent<Campfire>();
            return go;
        }
    }
}
