using System.Collections.Generic;
using UnityEngine;

namespace Topaz.Gameplay
{
    /// <summary>Visual-only sky visibility from committed construction. No camp or save authority.</summary>
    public sealed class SpatialShelter : MonoBehaviour
    {
        public const int Width = 32, Height = 16, SamplesPerFrame = 512;
        public const float Spacing = .5f, RayLength = 6f;
        static readonly Vector3[] Directions = {
            Vector3.up, new Vector3(1,.5f,0).normalized, new Vector3(-1,.5f,0).normalized,
            new Vector3(0,.5f,1).normalized, new Vector3(0,.5f,-1).normalized,
            new Vector3(1,1,1).normalized, new Vector3(-1,1,1).normalized,
            new Vector3(1,1,-1).normalized, new Vector3(-1,1,-1).normalized };
        readonly Dictionary<GameObject, Collider[]> _geometry = new();
        readonly Dictionary<Vector3Int, Color> _cache = new();
        readonly List<Vector3Int> _remove = new();
        readonly Queue<int> _pending = new();
        readonly Color[] _pixels = new Color[Width * Height * Width];
        readonly bool[] _queued = new bool[Width * Height * Width];
        Texture3D _texture;
        Transform _focus;
        Vector3Int _origin;
        bool _initialized;
        public int PendingSamples => _pending.Count;
        public int CachedSamples => _cache.Count;
        public int LastSampleCount { get; private set; }
        public Texture3D Texture => _texture;

        public void Initialize(Transform focus) { _focus = focus; }

        public void Register(GameObject root)
        {
            if (root == null) return;
            _geometry[root] = root.GetComponentsInChildren<Collider>(true);
            Invalidate(BoundsOf(root));
        }
        public void Unregister(GameObject root)
        {
            if (root == null) return;
            Bounds bounds = BoundsOf(root);
            _geometry.Remove(root);
            Invalidate(bounds);
        }
        public Bounds BoundsOf(GameObject root)
        {
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            if (_geometry.TryGetValue(root, out var colliders))
                foreach (var collider in colliders) if (collider != null) bounds.Encapsulate(collider.bounds);
            return bounds;
        }
        public void Invalidate(Bounds bounds)
        {
            bounds.Expand(RayLength * 2 + Spacing * 2);
            _remove.Clear();
            foreach (var pair in _cache) if (bounds.Contains((Vector3)pair.Key * Spacing)) _remove.Add(pair.Key);
            foreach (var key in _remove) _cache.Remove(key);
            if (!_initialized) return;
            for (int i = 0; i < _pixels.Length; i++)
                if (bounds.Contains((Vector3)Key(i) * Spacing)) Enqueue(i);
        }
        public Vector2 Sample(Vector3 position)
        {
            int visible = 0;
            bool rain = true;
            for (int i = 0; i < Directions.Length; i++)
            {
                bool blocked = Blocked(position + Vector3.up * .04f, Directions[i]);
                if (!blocked) visible++;
                if (i == 0) rain = !blocked;
            }
            return new Vector2(rain ? 1 : 0, Mathf.Lerp(.22f, 1f, visible / (float)Directions.Length));
        }
        bool Blocked(Vector3 position, Vector3 direction)
        {
            var ray = new Ray(position, direction);
            foreach (var pair in _geometry)
            {
                if (pair.Key == null || !pair.Key.activeInHierarchy) continue;
                foreach (var collider in pair.Value)
                    if (collider != null && collider.enabled && !collider.isTrigger &&
                        collider.bounds.IntersectRay(ray, out float distance) && distance <= RayLength &&
                        collider.Raycast(ray, out _, RayLength)) return true;
            }
            return false;
        }
        Vector3Int Key(int index) => _origin + new Vector3Int(index % Width,
            index / Width % Height, index / (Width * Height));
        void Enqueue(int i) { if (!_queued[i]) { _queued[i] = true; _pending.Enqueue(i); } }
        void Recenter(Vector3 position)
        {
            // Shift by two metres, retaining overlapping world-space samples.
            var origin = new Vector3Int(Mathf.FloorToInt(position.x / 2) * 4 - Width / 2,
                Mathf.FloorToInt(position.y / 2) * 4 - 2, Mathf.FloorToInt(position.z / 2) * 4 - Width / 2);
            if (_initialized && origin == _origin) return;
            _origin = origin; _initialized = true;
            _pending.Clear(); System.Array.Clear(_queued, 0, _queued.Length);
            _remove.Clear();
            foreach (var pair in _cache)
            {
                var p = pair.Key - _origin;
                if (p.x < 0 || p.x >= Width || p.y < 0 || p.y >= Height || p.z < 0 || p.z >= Width) _remove.Add(pair.Key);
            }
            foreach (var key in _remove) _cache.Remove(key);
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (_cache.TryGetValue(Key(i), out Color color)) _pixels[i] = color;
                else { _pixels[i] = Color.white; Enqueue(i); }
            }
        }
        void LateUpdate()
        {
            if (_focus == null) return;
            Refresh(_focus.position);
        }
        public void Refresh(Vector3 focus)
        {
            if (_texture == null)
                _texture = new Texture3D(Width, Height, Width, TextureFormat.RGBA32, false)
                { name = "Topaz local shelter", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Recenter(focus);
            LastSampleCount = 0;
            bool changed = _pending.Count > 0;
            while (_pending.Count > 0 && LastSampleCount < SamplesPerFrame)
            {
                int i = _pending.Dequeue(); _queued[i] = false;
                var key = Key(i);
                var sample = Sample((Vector3)key * Spacing);
                _pixels[i] = new Color(sample.x, sample.y, 1, 1);
                _cache[key] = _pixels[i]; LastSampleCount++;
            }
            if (changed) { _texture.SetPixels(_pixels); _texture.Apply(false); }
            Shader.SetGlobalTexture("_TopazShelter", _texture);
            Shader.SetGlobalVector("_TopazShelterOrigin", new Vector4(_origin.x * Spacing, _origin.y * Spacing, _origin.z * Spacing, 1));
            Shader.SetGlobalVector("_TopazShelterSize", new Vector4(Width * Spacing, Height * Spacing, Width * Spacing, Spacing));
        }
        void OnDisable() { Shader.SetGlobalVector("_TopazShelterOrigin", Vector4.zero); }
        void OnDestroy() { if (_texture != null) Destroy(_texture); }
    }
}
