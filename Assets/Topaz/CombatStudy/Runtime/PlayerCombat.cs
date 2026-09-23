using Topaz.FeelStudy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.CombatStudy
{
    /// <summary>One deliberate sword swing with a visible windup, strike, and recovery.</summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        enum Phase { Ready, Windup, Active, Recovery }

        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] InputActionAsset controls;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] EnemyCombatant enemy;
        [SerializeField] Transform swordPivot;
        [SerializeField] LineRenderer swingArc;

        InputAction _attackAction;
        Phase _phase;
        float _phaseEnd;
        bool _attackRequested;
        Vector3 _lockedDirection = Vector3.forward;

        public bool CanStartDodge => _phase == Phase.Ready || _phase == Phase.Recovery;
        public bool IsAttackLocked => _phase != Phase.Ready;
        public Vector3 LockedDirection => _lockedDirection;
        public float MovementMultiplier => _phase == Phase.Windup || _phase == Phase.Active
            ? 0.45f : _phase == Phase.Recovery ? 0.75f : 1f;

        void Awake()
        {
            if (attack == null || controls == null || movement == null || swingArc == null)
            {
                Debug.LogError("Player combat is missing a required reference.", this);
                enabled = false;
                return;
            }
            _attackAction = controls.FindActionMap("Player", true).FindAction("Attack", true);
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_attackAction != null) _attackAction.performed += OnAttackPerformed;
        }

        void OnDisable()
        {
            if (_attackAction != null) _attackAction.performed -= OnAttackPerformed;
        }

        void OnAttackPerformed(InputAction.CallbackContext context) => _attackRequested = true;

        void Update()
        {
            if (_phase == Phase.Ready && _attackRequested && !movement.IsDodging)
                BeginAttack();
            _attackRequested = false;

            switch (_phase)
            {
                case Phase.Windup when Time.time >= _phaseEnd:
                    _phase = Phase.Active;
                    _phaseEnd = Time.time + attack.ActiveSeconds;
                    Strike();
                    break;
                case Phase.Active when Time.time >= _phaseEnd:
                    _phase = Phase.Recovery;
                    _phaseEnd = Time.time + attack.RecoverySeconds;
                    swingArc.enabled = false;
                    break;
                case Phase.Recovery when Time.time >= _phaseEnd:
                    EndAttack();
                    break;
            }

            if (_phase == Phase.Windup || _phase == Phase.Active)
                DrawArc();
            AnimateSword();
        }

        public void OnDodgeStarted()
        {
            if (_phase == Phase.Recovery) EndAttack();
        }

        void BeginAttack()
        {
            _lockedDirection = movement.AimDirection;
            _lockedDirection.y = 0f;
            if (_lockedDirection.sqrMagnitude < 0.01f) _lockedDirection = Vector3.forward;
            _lockedDirection.Normalize();
            _phase = Phase.Windup;
            _phaseEnd = Time.time + attack.WindupSeconds;
            swingArc.enabled = true;
            if (swordPivot != null) swordPivot.gameObject.SetActive(true);
            DrawArc();
        }

        void Strike()
        {
            if (enemy == null || !enemy.IsAlive) return;
            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > attack.Range * attack.Range ||
                toEnemy.sqrMagnitude < 0.001f) return;
            if (Vector3.Angle(_lockedDirection, toEnemy) <= attack.ArcDegrees * 0.5f)
                enemy.TakeDamage(attack.Damage);
        }

        void EndAttack()
        {
            _phase = Phase.Ready;
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
        }

        void DrawArc()
        {
            const int segments = 14;
            Vector3 center = transform.position + Vector3.up * 0.07f;
            swingArc.positionCount = segments + 3;
            swingArc.SetPosition(0, center);
            for (int i = 0; i <= segments; i++)
            {
                float degrees = Mathf.Lerp(-attack.ArcDegrees * 0.5f, attack.ArcDegrees * 0.5f,
                    (float)i / segments);
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.up) * _lockedDirection;
                swingArc.SetPosition(i + 1, center + direction * attack.Range);
            }
            swingArc.SetPosition(segments + 2, center);
        }

        void AnimateSword()
        {
            if (swordPivot == null || _phase == Phase.Ready) return;
            float degrees = _phase switch
            {
                Phase.Windup => -55f,
                Phase.Active => Mathf.Lerp(-55f, 55f,
                    1f - Mathf.Clamp01((_phaseEnd - Time.time) / attack.ActiveSeconds)),
                _ => 55f
            };
            swordPivot.localRotation = Quaternion.Euler(0f, degrees, 0f);
        }
    }
}
