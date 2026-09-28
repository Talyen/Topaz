using Topaz.Characters;
using UnityEngine;

namespace Topaz.Player
{
    /// <summary>Presentation for the player's permanent lantern; the saved switch is independent of midday light attenuation.</summary>
    public sealed class PlayerLantern : MonoBehaviour
    {
        [SerializeField] Transform visualRoot;
        [SerializeField] PlayerController movement;
        [SerializeField] GameObject lanternModel;
        [SerializeField] Vector3 lightOffset = new Vector3(0f, 1.05f, 0f);
        [SerializeField] float lightRange = 8.5f;
        [SerializeField] float lightIntensity = 2f;

        Light _light;
        Transform _mount;
        Transform _sway;
        LanternVisual _visual;
        bool _isOn;
        float _timeStrength = 1f;

        public bool IsOn => _isOn;

        void Awake()
        {
            if (visualRoot == null || movement == null || lanternModel == null)
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

            CharacterVisual bindings = modelRoot.GetComponentInChildren<CharacterVisual>(true);
            Transform hips = bindings != null ? bindings.LanternAnchor : null;
            if (hips == null)
            {
                Debug.LogError("Player model is missing its authored lantern anchor for its lantern.", modelRoot);
                return;
            }

            _mount = new GameObject("Lantern Hip Mount").transform;
            _mount.SetParent(hips, false);
            _mount.localPosition = Vector3.zero;
            _sway = new GameObject("Lantern Sway").transform;
            _sway.SetParent(_mount, false);

            GameObject model = Instantiate(lanternModel, _sway);
            model.name = "Carried Lantern";
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            _visual = model.GetComponent<LanternVisual>();
            if (_visual == null)
                Debug.LogError("Carried lantern prefab is missing its visual binding.", model);
            else ApplyLight();
        }

        public void SetLit(bool lit)
        {
            _isOn = lit;
            ApplyLight();
        }

        public void SetWorldHours(double hours)
        {
            float hour=(float)((hours%24+24)%24);
            // Full warmth through morning/evening/night; blend down 10-11 and back up 14-15.
            _timeStrength=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(10,11,hour))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(14,15,hour)));
            ApplyLight();
        }
        void ApplyLight()
        {
            bool visible=_isOn && _timeStrength>.001f;
            if(_light!=null){_light.intensity=lightIntensity*_timeStrength;_light.enabled=visible;}
            _visual?.SetLit(visible);
            if(_light!=null && _visual!=null)_light.transform.position=_visual.LightPosition;
        }
        void LateUpdate()
        {
            if(_light!=null && _visual!=null)_light.transform.position=_visual.LightPosition;
            if (_sway == null || movement == null || Time.deltaTime <= 0f) return;
            float amount = Mathf.Clamp01(movement.PlanarSpeed / Mathf.Max(0.01f, movement.TravelSpeed));
            float swing = Mathf.Sin(Time.time * 8f) * 6f * amount;
            Quaternion target = Quaternion.Euler(swing, 0f, swing * 0.45f);
            _sway.localRotation = Quaternion.Slerp(_sway.localRotation, target,
                1f - Mathf.Exp(-10f * Time.deltaTime));
        }
    }
}
