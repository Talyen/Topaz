using Topaz.FeelStudy;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.CombatStudy
{
    /// <summary>One deliberate sword swing with a visible windup, strike, and recovery.</summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        enum Phase { Ready, Windup, Active, Recovery }
        enum Tool { Sword, Axe }

        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] MeleeAttackDefinition axeAttack;
        [SerializeField] InputActionAsset controls;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] EnemyCombatant enemy;
        [SerializeField] HarvestTree tree;
        [SerializeField] Transform swordPivot;
        [SerializeField] Transform axePivot;
        [SerializeField] LineRenderer swingArc;
        [SerializeField] Material swordArcMaterial;
        [SerializeField] Material axeArcMaterial;

        InputAction _attackAction;
        InputAction _equipSword;
        InputAction _equipAxe;
        InputAction _cycleTool;
        WorldSession _worldSession;
        Phase _phase;
        Tool _equippedTool;
        Tool _strikeTool;
        float _phaseEnd;
        bool _attackRequested;
        Vector3 _lockedDirection = Vector3.forward;

        public bool CanStartDodge => _phase == Phase.Ready || _phase == Phase.Recovery;
        public bool IsAttackLocked => _phase != Phase.Ready;
        public bool IsStrikeActive => _phase == Phase.Active;
        public float AttackAnimationSeconds => CurrentAttack.WindupSeconds +
            CurrentAttack.ActiveSeconds + CurrentAttack.RecoverySeconds;
        public Vector3 LockedDirection => _lockedDirection;
        public string EquippedToolId => _equippedTool == Tool.Axe ? "axe" : "sword";
        public string EquippedToolName => _equippedTool == Tool.Axe ? "Axe" : "Sword";
        MeleeAttackDefinition CurrentAttack => _strikeTool == Tool.Axe && axeAttack != null
            ? axeAttack : attack;
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
            _worldSession = GetComponent<WorldSession>();
            InputActionMap map = controls.FindActionMap("Player", true);
            _attackAction = map.FindAction("Attack", true);
            _equipSword = map.FindAction("EquipSword");
            _equipAxe = map.FindAction("EquipAxe");
            _cycleTool = map.FindAction("CycleTool");
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
            if (axePivot != null) axePivot.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_attackAction != null) _attackAction.performed += OnAttackPerformed;
            if (_equipSword != null) _equipSword.performed += OnEquipSword;
            if (_equipAxe != null) _equipAxe.performed += OnEquipAxe;
            if (_cycleTool != null) _cycleTool.performed += OnCycleTool;
        }

        void OnDisable()
        {
            if (_attackAction != null) _attackAction.performed -= OnAttackPerformed;
            if (_equipSword != null) _equipSword.performed -= OnEquipSword;
            if (_equipAxe != null) _equipAxe.performed -= OnEquipAxe;
            if (_cycleTool != null) _cycleTool.performed -= OnCycleTool;
        }

        void OnAttackPerformed(InputAction.CallbackContext context) => _attackRequested = true;
        void OnEquipSword(InputAction.CallbackContext context) => EquipTool("sword");
        void OnEquipAxe(InputAction.CallbackContext context) => EquipTool("axe");
        void OnCycleTool(InputAction.CallbackContext context) => EquipTool(
            _equippedTool == Tool.Sword ? "axe" : "sword");

        public void EquipTool(string toolId, bool persist = true)
        {
            if (_phase != Phase.Ready) return;
            Tool selected = toolId == "axe" ? Tool.Axe : Tool.Sword;
            if (_equippedTool == selected) return;
            _equippedTool = selected;
            if (persist) _worldSession?.ToolChanged(EquippedToolId);
        }

        void Update()
        {
            if (_phase == Phase.Ready && _attackRequested && !movement.IsDodging &&
                (_worldSession == null || !_worldSession.SuppressAttack))
                BeginAttack();
            _attackRequested = false;

            switch (_phase)
            {
                case Phase.Windup when Time.time >= _phaseEnd:
                    _phase = Phase.Active;
                    _phaseEnd = Time.time + CurrentAttack.ActiveSeconds;
                    Strike();
                    break;
                case Phase.Active when Time.time >= _phaseEnd:
                    _phase = Phase.Recovery;
                    _phaseEnd = Time.time + CurrentAttack.RecoverySeconds;
                    swingArc.enabled = false;
                    break;
                case Phase.Recovery when Time.time >= _phaseEnd:
                    EndAttack();
                    break;
            }

            if (_phase == Phase.Windup || _phase == Phase.Active)
                DrawArc();
            AnimateTool();
        }

        public void OnDodgeStarted()
        {
            if (_phase == Phase.Recovery) EndAttack();
        }

        void BeginAttack()
        {
            _strikeTool = _equippedTool;
            _lockedDirection = movement.AimDirection;
            _lockedDirection.y = 0f;
            if (_lockedDirection.sqrMagnitude < 0.01f) _lockedDirection = Vector3.forward;
            _lockedDirection.Normalize();
            _phase = Phase.Windup;
            _phaseEnd = Time.time + CurrentAttack.WindupSeconds;
            swingArc.enabled = true;
            if (swordArcMaterial != null && axeArcMaterial != null)
                swingArc.sharedMaterial = _strikeTool == Tool.Axe ? axeArcMaterial : swordArcMaterial;
            if (_strikeTool == Tool.Axe)
            {
                if (axePivot != null) axePivot.gameObject.SetActive(true);
            }
            else if (swordPivot != null) swordPivot.gameObject.SetActive(true);
            DrawArc();
        }

        void Strike()
        {
            if (_strikeTool == Tool.Axe)
            {
                tree?.TryChop(transform.position, _lockedDirection, CurrentAttack.Range,
                    CurrentAttack.ArcDegrees);
                return;
            }
            if (enemy != null) TryDamage(enemy);
            foreach (EnemyCombatant other in FindObjectsByType<EnemyCombatant>(FindObjectsInactive.Exclude))
                if (other != enemy) TryDamage(other);
        }

        void TryDamage(EnemyCombatant target)
        {
            if (target == null || !target.IsAlive) return;
            Vector3 toEnemy = target.transform.position - transform.position;
            toEnemy.y = 0f;
            if (toEnemy.sqrMagnitude > CurrentAttack.Range * CurrentAttack.Range ||
                toEnemy.sqrMagnitude < 0.001f) return;
            if (Vector3.Angle(_lockedDirection, toEnemy) <= CurrentAttack.ArcDegrees * 0.5f)
            {
                int before = target.CurrentHealth;
                target.TakeDamage(CurrentAttack.Damage);
                _worldSession?.RecordSwordHit(before - target.CurrentHealth);
            }
        }

        void EndAttack()
        {
            _phase = Phase.Ready;
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
            if (axePivot != null) axePivot.gameObject.SetActive(false);
        }

        void DrawArc()
        {
            const int segments = 14;
            Vector3 center = transform.position + Vector3.up * 0.07f;
            swingArc.positionCount = segments + 3;
            swingArc.SetPosition(0, center);
            for (int i = 0; i <= segments; i++)
            {
                float degrees = Mathf.Lerp(-CurrentAttack.ArcDegrees * 0.5f,
                    CurrentAttack.ArcDegrees * 0.5f,
                    (float)i / segments);
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.up) * _lockedDirection;
                swingArc.SetPosition(i + 1, center + direction * CurrentAttack.Range);
            }
            swingArc.SetPosition(segments + 2, center);
        }

        void AnimateTool()
        {
            if (_phase == Phase.Ready) return;
            Transform pivot = _strikeTool == Tool.Axe ? axePivot : swordPivot;
            if (pivot == null) return;
            float degrees = _phase switch
            {
                Phase.Windup => -55f,
                Phase.Active => Mathf.Lerp(-55f, 55f,
                    1f - Mathf.Clamp01((_phaseEnd - Time.time) / CurrentAttack.ActiveSeconds)),
                _ => 55f
            };
            pivot.localRotation = Quaternion.Euler(0f, degrees, 0f);
        }
    }
}
