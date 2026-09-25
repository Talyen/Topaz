using System;
using Topaz.FeelStudy;
using Topaz.AnimationStudy;
using Topaz.LoopStudy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.CombatStudy
{
    /// <summary>One deliberate sword swing with a visible windup, strike, and recovery.</summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        enum Phase { Ready, Windup, Active, Recovery }
        enum Tool { Sword, Axe, Pickaxe }

        [SerializeField] MeleeAttackDefinition attack;
        [SerializeField] MeleeAttackDefinition axeAttack;
        [SerializeField] MeleeAttackDefinition pickaxeAttack;
        [SerializeField] InputActionAsset controls;
        [SerializeField] FeelStudyPlayer movement;
        [SerializeField] EnemyCombatant enemy;
        [SerializeField] HarvestTree tree;
        [SerializeField] Transform swordPivot;
        [SerializeField] Transform axePivot;
        [SerializeField] Transform pickaxePivot;
        [SerializeField] LineRenderer swingArc;
        [SerializeField] Material swordArcMaterial;
        [SerializeField] Material axeArcMaterial;
        [SerializeField] AudioSource weaponAudio;
        [SerializeField] float autoGatherCombatRadius = 2.35f;

        InputAction _attackAction;
        InputAction _blockAction;
        WorldSession _worldSession;
        GroundSpellAbility _staffSpell;
        CrossbowPlayerAbility _crossbow;
        Phase _phase;
        Tool _equippedTool;
        Tool _strikeTool;
        WeaponDefinition _strikeWeapon;
        float _phaseEnd;
        bool _attackRequested;
        bool _impactPlayed;
        bool _guardHeld;
        bool _guardNeedsRelease;
        float _guardActiveAt;
        float _rageUntil;
        float _dodgeBonusStarts;
        float _dodgeBonusEnds;
        bool _dodgeStrike;
        bool _hitDuringStrike;
        bool _staminaEnhanced;
        HarvestTree _harvestTarget;
        MiningRock _miningTarget;
        Tool? _restoreToolAfterHarvest;
        Vector3 _lockedDirection = Vector3.forward;

        public event Action<EnemyCombatant, WeaponDefinition> WeaponHit;

        public bool CanStartDodge => (_staffSpell == null || !_staffSpell.IsCasting) &&
            (_crossbow == null || !_crossbow.IsAiming) &&
            (_phase == Phase.Ready || _phase == Phase.Recovery);
        public bool CanStartHarvest => _phase == Phase.Ready &&
            (_crossbow == null || !_crossbow.IsBusy) && !IsGuarding && !movement.IsDodging &&
            !movement.IsAirborne && (_worldSession == null || !_worldSession.SuppressAttack);
        public bool IsAttackLocked => _phase != Phase.Ready || _staffSpell?.IsCasting == true ||
            _staffSpell?.IsRecovering == true || _crossbow?.IsBusy == true;
        public bool IsCrossbowAiming => _crossbow?.IsAiming == true;
        public bool IsCrossbowReloading => _crossbow?.IsReloading == true;
        public float CrossbowReloadProgress => _crossbow?.ReloadProgress ?? 1f;
        public bool IsStrikeActive => _phase == Phase.Active;
        public bool IsHarvesting => (_harvestTarget != null || _miningTarget != null) &&
            _phase != Phase.Ready;
        public WeaponDefinition CurrentWeapon => _equippedTool != Tool.Sword ? null :
            _phase == Phase.Ready ? _worldSession?.CurrentWeapon : _strikeWeapon;
        string StrikeSkill => _strikeTool == Tool.Axe ? SkillIds.Logging :
            _strikeTool == Tool.Pickaxe ? SkillIds.Mining :
            _strikeWeapon?.Skill == WeaponSkill.Axes ? SkillIds.Axes : SkillIds.Swords;
        float AttackScale => _strikeTool != Tool.Sword ? 1f :
            Mathf.Pow(0.9f, _worldSession?.Stats.AttackSpeed ?? 0);
        float PhaseScale(Phase phase)
        {
            int level = _worldSession?.SkillLevel(StrikeSkill) ?? 1;
            float scale = AttackScale;
            if (_strikeTool == Tool.Axe || _strikeTool == Tool.Pickaxe)
            {
                scale *= 1f - (_worldSession?.SkillHandling(StrikeSkill) ?? 0f) * (level - 1);
                if (_worldSession?.HasTalent(StrikeSkill,
                    _strikeTool == Tool.Axe ? "logging.quick-chop" : "mining.quick-strike") == true)
                    scale *= 1f - _worldSession.TalentAmount(StrikeSkill,
                        _strikeTool == Tool.Axe ? "logging.quick-chop" : "mining.quick-strike");
            }
            else if (StrikeSkill == SkillIds.Swords)
                scale *= 1f - (_worldSession?.SkillHandling(StrikeSkill) ?? 0f) * (level - 1);
            else if (phase == Phase.Recovery)
                scale *= 1f - (_worldSession?.SkillHandling(StrikeSkill) ?? 0f) * (level - 1);
            return phase == Phase.Recovery && _staminaEnhanced
                ? scale * SurvivalRules.RecoveryMultiplier : scale;
        }
        public float AttackAnimationSeconds => IsCrossbowAiming ? _crossbow.AimSeconds :
            IsCrossbowReloading ? _crossbow.ReloadSeconds :
            CurrentWeapon?.GroundSpell != null
            ? CurrentWeapon.GroundSpell.WarningSeconds + CurrentWeapon.GroundSpell.RecoverySeconds
            : CurrentAttack.WindupSeconds * PhaseScale(Phase.Windup) +
              CurrentAttack.ActiveSeconds * PhaseScale(Phase.Active) +
              CurrentAttack.RecoverySeconds * PhaseScale(Phase.Recovery);
        public Vector3 LockedDirection => CurrentWeapon?.CrossbowAttack != null
            ? movement.AimDirection : _lockedDirection;
        public string EquippedToolId => _equippedTool == Tool.Axe ? "axe" :
            _equippedTool == Tool.Pickaxe ? "pickaxe" :
            _worldSession == null ? "sword" : CurrentWeapon == null ? null :
            CurrentWeapon.Skill == WeaponSkill.Axes ? "combat-axe" :
            CurrentWeapon.Skill == WeaponSkill.Staff ? "staff" :
            CurrentWeapon.Skill == WeaponSkill.Crossbows ? "crossbow" : "sword";
        public string EquippedToolName => _equippedTool == Tool.Axe ? "Logging Axe" :
            _equippedTool == Tool.Pickaxe ? "Pickaxe" :
            _worldSession == null ? "Sword" : !_worldSession.HasWeapon ? "Unarmed" :
            CurrentWeapon?.Skill == WeaponSkill.Axes ? "Two-Handed Axe" :
            CurrentWeapon?.Skill == WeaponSkill.Staff ? "Crypt Staff" :
            CurrentWeapon?.Skill == WeaponSkill.Crossbows ? "Crossbow" : "Sword";
        public bool IsGuarding => _guardHeld && !_guardNeedsRelease && _phase == Phase.Ready &&
            !movement.IsDodging && !movement.IsAirborne && _worldSession != null &&
            _worldSession.HasShield && !_worldSession.SuppressAttack;
        public bool IsGuardRaised => IsGuarding && Time.time >= _guardActiveAt;
        public bool HasShield => _worldSession != null && _worldSession.HasShield;
        public float GuardMovementMultiplier => IsGuarding
            ? .55f + (_worldSession?.SkillHandling(SkillIds.Shield) ?? 0f) *
                ((_worldSession?.ShieldLevel ?? 1) - 1) +
                (_worldSession?.TalentAmount(SkillIds.Shield, "shield.mobile-guard") ?? 0f)
            : 1f;
        MeleeAttackDefinition CurrentAttack => _strikeTool == Tool.Axe && axeAttack != null
            ? axeAttack : _strikeTool == Tool.Pickaxe && pickaxeAttack != null
                ? pickaxeAttack : _strikeWeapon?.Attack ?? _worldSession?.CurrentWeapon?.Attack ?? attack;
        public float MovementMultiplier => _staffSpell?.IsCasting == true
            ? (_worldSession?.HasTalent(SkillIds.Staff, "staff.mobile-casting") == true ? .55f : .30f)
            : _staffSpell?.IsRecovering == true ? .75f :
            IsCrossbowAiming ? .45f : IsCrossbowReloading ? .75f :
            _phase == Phase.Windup || _phase == Phase.Active
            ? (_strikeWeapon?.TwoHanded == true ? 0.30f :
                _strikeWeapon?.Skill == WeaponSkill.Swords &&
                _worldSession?.HasTalent(SkillIds.Swords, "swords.footwork") == true
                    ? _worldSession.TalentAmount(SkillIds.Swords, "swords.footwork") : .45f) :
            _phase == Phase.Recovery ? (_strikeWeapon?.TwoHanded == true ? 0.60f : 0.75f) : 1f;

        void Awake()
        {
            if (attack == null || controls == null || movement == null || swingArc == null)
            {
                Debug.LogError("Player combat is missing a required reference.", this);
                enabled = false;
                return;
            }
            _worldSession = GetComponent<WorldSession>();
            _staffSpell = GetComponent<GroundSpellAbility>();
            _crossbow = GetComponent<CrossbowPlayerAbility>();
            if (weaponAudio == null) weaponAudio = GetComponent<AudioSource>();
            InputActionMap map = controls.FindActionMap("Player", true);
            _attackAction = map.FindAction("Attack", true);
            _blockAction = map.FindAction("Block");
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
            if (axePivot != null) axePivot.gameObject.SetActive(false);
            if (pickaxePivot != null) pickaxePivot.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (_attackAction != null) _attackAction.performed += OnAttackPerformed;
        }

        void OnDisable()
        {
            if (_phase != Phase.Ready) EndAttack();
            _crossbow?.Cancel();
            if (_attackAction != null) _attackAction.performed -= OnAttackPerformed;
            _guardHeld = false;
        }

        void OnAttackPerformed(InputAction.CallbackContext context) => _attackRequested = true;
        public bool HasHarvestTool(string toolId) => toolId == "axe" && axeAttack != null &&
            (_worldSession == null || _worldSession.HasAxe);

        public bool SetManualTool(string toolId)
        {
            if (IsAttackLocked || (toolId != "sword" && toolId != "axe" &&
                toolId != "pickaxe")) return false;
            _equippedTool = toolId == "axe" ? Tool.Axe :
                toolId == "pickaxe" ? Tool.Pickaxe : Tool.Sword;
            _rageUntil = 0f;
            return true;
        }

        public void ClearTemporaryProgression()
        {
            _rageUntil = 0f;
            _dodgeBonusEnds = 0f;
            _crossbow?.Cancel();
        }

        public void OnDamagedByEnemy()
        {
            if (_equippedTool == Tool.Sword && _worldSession?.CurrentWeapon?.Skill == WeaponSkill.Axes &&
                _worldSession.HasTalent(SkillIds.Axes, "axes.rage"))
                _rageUntil = Time.time + _worldSession.TalentDuration(SkillIds.Axes, "axes.rage");
        }

        public bool TryStartMining(MiningRock target)
        {
            if (!CanStartHarvest || target == null || !target.IsAvailable ||
                pickaxeAttack == null || (_worldSession != null && !_worldSession.HasPickaxe))
                return false;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f ||
                direction.sqrMagnitude > pickaxeAttack.Range * pickaxeAttack.Range) return false;
            _restoreToolAfterHarvest = _equippedTool;
            _miningTarget = target;
            _equippedTool = Tool.Pickaxe;
            BeginAttack(direction);
            return true;
        }

        public bool TryStartHarvest(HarvestTree target, string toolId)
        {
            if (!CanStartHarvest || target == null || !target.IsAvailable ||
                !HasHarvestTool(toolId) || target.RequiredToolId != toolId) return false;
            Vector3 direction = target.transform.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f ||
                direction.sqrMagnitude > axeAttack.Range * axeAttack.Range) return false;
            _restoreToolAfterHarvest = _equippedTool;
            _harvestTarget = target;
            _equippedTool = Tool.Axe;
            BeginAttack(direction);
            return true;
        }

        public void CancelActiveAttack()
        {
            if (_phase != Phase.Ready) EndAttack();
            _staffSpell?.Cancel();
            _crossbow?.Cancel();
        }

        void Update()
        {
            bool pressed = _blockAction != null && _blockAction.IsPressed();
            if (!pressed) _guardNeedsRelease = false;
            if (pressed && !_guardHeld) _guardActiveAt = Time.time + .12f -
                (_worldSession?.TalentAmount(SkillIds.Shield, "shield.quick-raise") ?? 0f);
            _guardHeld = pressed;
            if (_phase == Phase.Ready && _attackRequested && !IsGuarding &&
                !movement.IsDodging &&
                (_worldSession == null || !_worldSession.SuppressAttack))
            {
                bool ranged = _equippedTool == Tool.Sword &&
                    _worldSession?.CurrentWeapon?.CrossbowAttack != null;
                bool staff = !ranged && _equippedTool == Tool.Sword &&
                    _worldSession?.CurrentWeapon?.GroundSpell != null;
                bool gathering = !staff && !ranged && TryStartAutoGather();
                if (ranged) _crossbow?.TryAim(_worldSession.CurrentWeapon.CrossbowAttack);
                else if (staff) TryCastStaff();
                else if (!gathering && (_equippedTool != Tool.Sword || _worldSession == null ||
                    _worldSession.HasWeapon && _worldSession.CurrentWeapon?.Attack != null))
                    BeginAttack(movement.AimDirection);
            }
            _attackRequested = false;

            switch (_phase)
            {
                case Phase.Windup when Time.time >= _phaseEnd:
                    _phase = Phase.Active;
                    _phaseEnd = Time.time + CurrentAttack.ActiveSeconds * PhaseScale(Phase.Active);
                    if (_strikeWeapon?.SwingClip != null) weaponAudio?.PlayOneShot(
                        _strikeWeapon.SwingClip);
                    Strike();
                    break;
                case Phase.Active when Time.time >= _phaseEnd:
                    _phase = Phase.Recovery;
                    float recovery = CurrentAttack.RecoverySeconds;
                    if (_hitDuringStrike && StrikeSkill == SkillIds.Swords &&
                        _worldSession?.HasTalent(SkillIds.Swords, "swords.flow") == true)
                        recovery = Mathf.Max(.05f, recovery -
                            _worldSession.TalentAmount(SkillIds.Swords, "swords.flow"));
                    _phaseEnd = Time.time + recovery * PhaseScale(Phase.Recovery);
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
            _crossbow?.OnDodgeStarted();
            _guardNeedsRelease = true;
            _dodgeBonusStarts = Time.time + movement.DodgeSeconds;
            _dodgeBonusEnds = _dodgeBonusStarts +
                (_worldSession?.TalentDuration(SkillIds.Swords, "swords.dodge-strike") ?? 0f);
            if (_phase == Phase.Recovery) EndAttack();
            if (_staffSpell?.IsRecovering == true) _staffSpell.Cancel();
        }

        void TryCastStaff()
        {
            WeaponDefinition weapon = _worldSession?.CurrentWeapon;
            if (weapon?.GroundSpell == null || _staffSpell == null || !_staffSpell.CanCast)
                return;
            float range = weapon.GroundSpell.Range +
                (_worldSession.HasTalent(SkillIds.Staff, "staff.far-sigil") ? 1f : 0f);
            Vector3 center = movement.UsingStickAim
                ? transform.position + movement.AimDirection * Mathf.Min(4.5f, range)
                : movement.AimPointOnGround;
            Vector3 offset = center - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > range * range) center = transform.position +
                offset.normalized * range;
            Vector3 origin = transform.position + Vector3.up;
            Vector3 direction = center + Vector3.up - origin;
            float length = direction.magnitude;
            float nearestWall = length;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction.normalized, length,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<EnemyCombatant>() != null ||
                    hit.collider.GetComponentInParent<PlayerVitality>() != null) continue;
                nearestWall = Mathf.Min(nearestWall, hit.distance);
            }
            if (nearestWall < length)
                center = origin + direction.normalized * Mathf.Max(0f, nearestWall - .3f);
            int damage = Mathf.Max(1, _worldSession.Stats.Attack - 2) +
                Mathf.RoundToInt(_worldSession.SkillOutputBonus(SkillIds.Staff));
            if (_worldSession.HasTalent(SkillIds.Staff, "staff.dodge-focus") &&
                Time.time >= _dodgeBonusStarts && Time.time <= _dodgeBonusEnds)
                damage++;
            _staffSpell.Begin(center, false, damage,
                _worldSession.HasTalent(SkillIds.Staff, "staff.wide-circle") ? .25f : 0f);
        }

        public bool TryBlock(Vector3 attackerPosition)
        {
            if (!IsGuardRaised) return false;
            Vector3 incoming = attackerPosition - transform.position;
            incoming.y = 0f;
            bool blocked = incoming.sqrMagnitude > 0.001f &&
                Vector3.Angle(movement.AimDirection, incoming) <=
                60f + (_worldSession?.TalentAmount(SkillIds.Shield,
                    "shield.broad-guard") ?? 0f) * .5f;
            if (blocked) GetComponent<PlayerAppearance>()?.ShowShieldImpact();
            return blocked;
        }

        bool TryStartAutoGather()
        {
            if (_worldSession == null || !CanStartHarvest || EnemyInMeleeRange())
                return false;
            if (!_worldSession.TryFindGatherTarget(transform.position,
                axeAttack != null && HasHarvestTool("axe") ? axeAttack.Range : 0f,
                pickaxeAttack != null && _worldSession.HasPickaxe ? pickaxeAttack.Range : 0f,
                out HarvestTree targetTree, out MiningRock targetRock)) return false;
            return targetTree != null
                ? TryStartHarvest(targetTree, targetTree.RequiredToolId)
                : TryStartMining(targetRock);
        }

        bool EnemyInMeleeRange()
        {
            float radius = Mathf.Max(autoGatherCombatRadius,
                _worldSession?.CurrentWeapon?.Attack?.Range ?? attack.Range);
            float radiusSquared = radius * radius;
            foreach (EnemyCombatant candidate in
                     FindObjectsByType<EnemyCombatant>())
            {
                if (candidate == null || !candidate.IsAlive) continue;
                Vector3 distance = candidate.transform.position - transform.position;
                distance.y = 0f;
                if (distance.sqrMagnitude <= radiusSquared) return true;
            }
            return false;
        }

        void BeginAttack(Vector3 direction)
        {
            _strikeTool = _equippedTool;
            _strikeWeapon = _strikeTool == Tool.Sword ? _worldSession?.CurrentWeapon : null;
            float staminaCost = _strikeTool == Tool.Sword
                ? _strikeWeapon?.Skill == WeaponSkill.Axes ? 30f : 25f : 20f;
            _staminaEnhanced = _worldSession?.TryExert(staminaCost) == true;
            _dodgeStrike = StrikeSkill == SkillIds.Swords &&
                _worldSession?.HasTalent(SkillIds.Swords, "swords.dodge-strike") == true &&
                Time.time >= _dodgeBonusStarts && Time.time <= _dodgeBonusEnds;
            if (_dodgeStrike) _dodgeBonusEnds = 0f;
            _hitDuringStrike = false;
            _impactPlayed = false;
            _lockedDirection = direction;
            _lockedDirection.y = 0f;
            if (_lockedDirection.sqrMagnitude < 0.01f) _lockedDirection = Vector3.forward;
            _lockedDirection.Normalize();
            _phase = Phase.Windup;
            _phaseEnd = Time.time + CurrentAttack.WindupSeconds * PhaseScale(Phase.Windup);
            swingArc.enabled = true;
            if (swordArcMaterial != null && axeArcMaterial != null)
                swingArc.sharedMaterial = _strikeTool != Tool.Sword ? axeArcMaterial : swordArcMaterial;
            if (_strikeTool == Tool.Axe)
            {
                if (axePivot != null) axePivot.gameObject.SetActive(true);
            }
            else if (_strikeTool == Tool.Pickaxe)
            {
                if (pickaxePivot != null) pickaxePivot.gameObject.SetActive(true);
            }
            else if (swordPivot != null) swordPivot.gameObject.SetActive(true);
            DrawArc();
        }

        void Strike()
        {
            if (_strikeTool == Tool.Axe)
            {
                (_harvestTarget != null ? _harvestTarget : tree)?.TryChop(
                    transform.position, _lockedDirection, CurrentAttack.Range,
                    CurrentAttack.ArcDegrees);
                return;
            }
            if (_strikeTool == Tool.Pickaxe)
            {
                if (_miningTarget != null)
                    _miningTarget.TryStrike(transform.position, _lockedDirection,
                        CurrentAttack.Range, CurrentAttack.ArcDegrees);
                else _worldSession?.TryManualMine(transform.position, _lockedDirection,
                    CurrentAttack.Range, CurrentAttack.ArcDegrees);
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
            float arc = CurrentAttack.ArcDegrees +
                (StrikeSkill == SkillIds.Swords
                    ? _worldSession?.TalentAmount(SkillIds.Swords, "swords.wide-cut") ?? 0f
                    : 0f);
            if (Vector3.Angle(_lockedDirection, toEnemy) <= arc * 0.5f)
            {
                int before = target.CurrentHealth;
                int damage = Mathf.Max(1, _worldSession?.Stats.Attack ?? CurrentAttack.Damage) +
                    Mathf.RoundToInt(_worldSession?.SkillOutputBonus(StrikeSkill) ?? 0f);
                if (_dodgeStrike) damage += Mathf.RoundToInt(
                    _worldSession?.TalentAmount(SkillIds.Swords, "swords.dodge-strike") ?? 0f);
                if (StrikeSkill == SkillIds.Axes && before <= target.MaximumHealth / 2 &&
                    _worldSession?.HasTalent(SkillIds.Axes, "axes.finishing-blow") == true)
                    damage += Mathf.RoundToInt(_worldSession.TalentAmount(
                        SkillIds.Axes, "axes.finishing-blow"));
                if (StrikeSkill == SkillIds.Axes && Time.time < _rageUntil &&
                    _worldSession?.HasTalent(SkillIds.Axes, "axes.rage") == true)
                    damage = Mathf.RoundToInt(damage * (1f +
                        _worldSession.TalentAmount(SkillIds.Axes, "axes.rage")));
                target.TakeDirectedDamage(damage, transform.position);
                int effectiveDamage = before - target.CurrentHealth;
                if (effectiveDamage > 0)
                {
                    _hitDuringStrike = true;
                    if (!_impactPlayed && _strikeWeapon?.ImpactClip != null)
                    {
                        weaponAudio?.PlayOneShot(_strikeWeapon.ImpactClip);
                        _impactPlayed = true;
                    }
                    float stagger = _strikeWeapon?.StaggerSeconds ?? 0f;
                    if (StrikeSkill == SkillIds.Axes &&
                        _worldSession?.HasTalent(SkillIds.Axes, "axes.heavy-impact") == true)
                        stagger += _worldSession.TalentAmount(SkillIds.Axes,
                            "axes.heavy-impact");
                    target.StaggerFromWeapon(stagger);
                    if (StrikeSkill == SkillIds.Axes &&
                        _worldSession?.HasTalent(SkillIds.Axes, "axes.bleed") == true)
                        target.ApplyBleed(
                            Mathf.RoundToInt(_worldSession.TalentAmount(SkillIds.Axes, "axes.bleed")),
                            _worldSession.TalentDuration(SkillIds.Axes, "axes.bleed"),
                            _worldSession.TalentInterval(SkillIds.Axes, "axes.bleed"));
                    _worldSession?.RecordWeaponHit(_strikeWeapon?.Skill ?? WeaponSkill.Swords,
                        effectiveDamage, target.SourceLevel);
                    WeaponHit?.Invoke(target, _strikeWeapon);
                }
            }
        }

        void EndAttack()
        {
            _phase = Phase.Ready;
            _staminaEnhanced = false;
            swingArc.enabled = false;
            if (swordPivot != null) swordPivot.gameObject.SetActive(false);
            if (axePivot != null) axePivot.gameObject.SetActive(false);
            if (pickaxePivot != null) pickaxePivot.gameObject.SetActive(false);
            if (_restoreToolAfterHarvest.HasValue)
                _equippedTool = _restoreToolAfterHarvest.Value;
            _restoreToolAfterHarvest = null;
            _harvestTarget = null;
            _miningTarget = null;
            _strikeWeapon = null;
        }

        void DrawArc()
        {
            const int segments = 14;
            Vector3 center = transform.position;
            center.y = Topaz.VisualStudy.GroundSurface.Height(center) + .16f;
            swingArc.positionCount = segments + 3;
            swingArc.SetPosition(0, center);
            for (int i = 0; i <= segments; i++)
            {
                float arc = CurrentAttack.ArcDegrees +
                    (StrikeSkill == SkillIds.Swords
                        ? _worldSession?.TalentAmount(SkillIds.Swords, "swords.wide-cut") ?? 0f
                        : 0f);
                float degrees = Mathf.Lerp(-arc * 0.5f,
                    arc * 0.5f,
                    (float)i / segments);
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.up) * _lockedDirection;
                swingArc.SetPosition(i + 1, center + direction * CurrentAttack.Range);
            }
            swingArc.SetPosition(segments + 2, center);
        }

        void AnimateTool()
        {
            if (_phase == Phase.Ready) return;
            Transform pivot = _strikeTool == Tool.Axe ? axePivot :
                _strikeTool == Tool.Pickaxe ? pickaxePivot : swordPivot;
            if (pivot == null) return;
            float degrees = _phase switch
            {
                Phase.Windup => -55f,
                Phase.Active => Mathf.Lerp(-55f, 55f,
                    1f - Mathf.Clamp01((_phaseEnd - Time.time) /
                        (CurrentAttack.ActiveSeconds * PhaseScale(Phase.Active)))),
                _ => 55f
            };
            pivot.localRotation = Quaternion.Euler(0f, degrees, 0f);
        }
    }
}
