using UnityEngine;

namespace Topaz.LoopStudy
{
    public sealed class HomeNightLight : MonoBehaviour
    {
        Light _light;
        WorldSession _session;

        void Awake()
        {
            _light = gameObject.AddComponent<Light>();
            _light.type = LightType.Point;
            _light.range = 4f;
            _light.intensity = 1.2f;
            _light.color = new Color(1f, .72f, .42f);
            _light.shadows = LightShadows.None;
            _session = FindFirstObjectByType<WorldSession>();
        }

        void Update()
        {
            if (_light == null || _session == null) return;
            double hour = _session.WorldHours % WorldClock.HoursPerDay;
            _light.enabled = hour >= 18d || hour < 6d;
        }
    }
}
