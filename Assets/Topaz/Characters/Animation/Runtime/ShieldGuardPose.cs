using Topaz.CombatStudy;
using UnityEngine;

namespace Topaz.AnimationStudy
{
    /// <summary>Raises the shared left arm after the base Generic-rig animation is evaluated.</summary>
    [DefaultExecutionOrder(100)]
    public sealed class ShieldGuardPose : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;
        Transform _upperArm;
        Transform _lowerArm;
        Renderer[] _shieldRenderers;
        MaterialPropertyBlock _impactProperties;
        float _impactUntil;
        float _blend;

        public void Bind(PlayerCombat owner) => combat = owner;
        public void Impact() => _impactUntil = Time.time + 0.16f;

        void Awake()
        {
            CharacterVisual visual = GetComponentInParent<CharacterVisual>();
            if (visual == null) visual = GetComponentInChildren<CharacterVisual>(true);
            combat = GetComponentInParent<PlayerCombat>();
            _upperArm = visual != null ? visual.GuardUpperArm : null;
            _lowerArm = visual != null ? visual.GuardLowerArm : null;
            _shieldRenderers = visual != null && visual.Shield != null ?
                visual.Shield.GetComponentsInChildren<Renderer>(true) : System.Array.Empty<Renderer>();
            _impactProperties = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (combat == null || _upperArm == null || _lowerArm == null) return;
            _blend = Mathf.MoveTowards(_blend, combat.IsGuarding ? 1f : 0f,
                Time.deltaTime / 0.12f);
            _upperArm.localRotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(-38f, -20f, 22f), _blend);
            _lowerArm.localRotation *= Quaternion.Slerp(Quaternion.identity,
                Quaternion.Euler(-28f, 0f, -12f), _blend);
            _impactProperties.SetColor("_BaseColor", Time.time < _impactUntil
                ? new Color(1f, .91f, .66f) : Color.white);
            foreach (Renderer shield in _shieldRenderers)
                if (shield != null) shield.SetPropertyBlock(_impactProperties);
        }
    }
}
