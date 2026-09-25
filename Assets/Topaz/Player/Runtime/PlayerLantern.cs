using System.Linq;
using UnityEngine;

namespace Topaz.FeelStudy
{
    /// <summary>Presentation for the player's permanent, manually switched lantern.</summary>
    public sealed class PlayerLantern : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] GameObject lanternModel;
        [SerializeField] Material lanternMaterial;
        [SerializeField] Material glassMaterial;
        [SerializeField] Material emberMaterial;
        [SerializeField] Vector3 hipOffset = new Vector3(-0.32f, -0.16f, 0f);
        [SerializeField] float modelScale = 0.32f;
        [SerializeField] Vector3 lightOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField] float lightRange = 8.5f;
        [SerializeField] float lightIntensity = 5f;

        Light _light;
        Transform _mount;
        Transform _sway;
        Renderer _ember;
        bool _isOn;

        public bool IsOn => _isOn;

        void Awake()
        {
            if (visualRoot == null || movement == null || lanternModel == null ||
                lanternMaterial == null || glassMaterial == null || emberMaterial == null)
            {
                Debug.LogError("Player lantern references are incomplete.", this);
                enabled = false;
                return;
            }

            var source = new GameObject("Lantern Light");
            source.transform.SetParent(visualRoot, false);
            source.transform.localPosition = lightOffset;
            _light = source.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.color = new Color(1f, 0.72f, 0.43f);
            _light.range = lightRange;
            _light.intensity = lightIntensity;
            _light.shadows = LightShadows.None;
            _light.enabled = false;
        }

        public void AttachToVisual(Transform modelRoot)
        {
            if (!enabled || modelRoot == null) return;
            if (_mount != null) Destroy(_mount.gameObject);

            Transform hips = modelRoot.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(child => child.name == "hips");
            if (hips == null)
            {
                Debug.LogError("Player model is missing the hips bone for its lantern.", modelRoot);
                return;
            }

            _mount = new GameObject("Lantern Hip Mount").transform;
            _mount.SetParent(hips, false);
            _mount.localPosition = hipOffset;
            _sway = new GameObject("Lantern Sway").transform;
            _sway.SetParent(_mount, false);

            GameObject model = Instantiate(lanternModel, _sway);
            model.name = "Carried Lantern";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one * modelScale;
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogError("Carried lantern model has no renderer.", model);
                return;
            }
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                    materials[i] = materials[i] != null &&
                        materials[i].name.IndexOf("glass", System.StringComparison.OrdinalIgnoreCase) >= 0
                        ? glassMaterial : lanternMaterial;
                renderer.sharedMaterials = materials;
                bounds.Encapsulate(renderer.bounds);
            }

            GameObject ember = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ember.name = "Lantern Ember";
            Destroy(ember.GetComponent<Collider>());
            ember.transform.SetParent(_sway, false);
            ember.transform.position = bounds.center + Vector3.up * bounds.extents.y * 0.2f;
            float scale = Mathf.Max(0.03f, bounds.size.y * 0.15f) /
                Mathf.Max(0.01f, _sway.lossyScale.y);
            ember.transform.localScale = Vector3.one * scale;
            _ember = ember.GetComponent<Renderer>();
            _ember.sharedMaterial = emberMaterial;
            _ember.enabled = _isOn;
        }

        public void SetLit(bool lit)
        {
            _isOn = lit;
            if (_light != null) _light.enabled = lit;
            if (_ember != null) _ember.enabled = lit;
        }

        void LateUpdate()
        {
            if (_sway == null || movement == null || Time.deltaTime <= 0f) return;
            float amount = Mathf.Clamp01(movement.PlanarSpeed / Mathf.Max(0.01f, movement.TravelSpeed));
            float swing = Mathf.Sin(Time.time * 8f) * 6f * amount;
            Quaternion target = Quaternion.Euler(swing, 0f, swing * 0.45f);
            _sway.localRotation = Quaternion.Slerp(_sway.localRotation, target,
                1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
