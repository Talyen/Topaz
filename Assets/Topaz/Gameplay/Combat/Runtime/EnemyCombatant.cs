using System;
using UnityEngine;
using UnityEngine.AI;

namespace Topaz.CombatStudy
{
    public enum SkeletonLootRole { Minion, Warrior, Rogue, Mage }

    /// <summary>One NavMesh-driven practice enemy with an aimed, avoidable attack tell.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyCombatant : MonoBehaviour
    {
        enum State { Idle, Pursuit, Windup, Recovery, Down }

        [SerializeField] EnemyDefinition definition;
        [SerializeField] PlayerVitality target;
        [SerializeField] SafeZone safeZone;
        [SerializeField] Transform visualRoot;
        Renderer bodyRenderer;
        [SerializeField] Collider bodyCollider;
        [SerializeField] LineRenderer telegraph;
        [SerializeField] bool keepVisualOnDefeat;
        [SerializeField] string spawnId;
        [SerializeField] SkeletonLootRole lootRole;
        [SerializeField] Color normalBodyTint = new Color(0.87f, 0.36f, 0.31f);
        [SerializeField] Transform[] rangedPositions;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color HitColor = new Color(1f, 0.92f, 0.72f);

        NavMeshAgent _agent;
        MaterialPropertyBlock _properties;
        Vector3 _spawnPosition;
        Quaternion _spawnRotation;
        Vector3 _strikeDirection = Vector3.forward;
        State _state;
        float _phaseEnd;
        float _nextPathUpdate;
        float _flashUntil;
        bool _flashing;
        float _guardStaggerUntil;
        float _bleedUntil;
        float _nextBleedTick;
        GroundSpellAbility _groundSpell;
        bool _castingGroundSpell;
        float _bleedInterval;
        int _bleedDamage;
        int _rangedPositionIndex;
        bool _rangedRepositioning;
        bool _rangedAimLocked;
        AudioSource _rangedAudio;

        public int CurrentHealth { get; private set; }
        public string SpawnId => spawnId;
        public SkeletonLootRole LootRole => lootRole;
        public event Action<EnemyCombatant> Defeated;
        public int SourceLevel => definition != null ? definition.SourceLevel : 1;
        public int MaximumHealth => definition != null ? definition.Health : 0;
        public bool IsAlive => CurrentHealth > 0;
        public bool IsAttacking => Time.time >= _guardStaggerUntil &&
            (_state == State.Windup || _state == State.Recovery);
        public bool IsWindingUp => _state == State.Windup;
        public bool IsDown => _state == State.Down;
        public bool HasHitReaction => Time.time < _flashUntil || Time.time < _guardStaggerUntil;
        public float HitReactionSeconds => 0.3f;
        public float AttackAnimationSeconds => AttackWindupSeconds + AttackRecoverySeconds;
        public float AttackWindupSeconds => definition.CrossbowAttack != null
            ? definition.CrossbowAttack.WindupSeconds : definition.TelegraphSeconds;
        public float AttackRecoverySeconds => definition.CrossbowAttack != null
            ? definition.CrossbowAttack.RecoverySeconds : definition.RecoverySeconds;
        public float TravelSpeed => definition.TravelSpeed;

        public void BindTarget(PlayerVitality player, SafeZone home)
        {
            target = player;
            safeZone = home;
        }

        void Awake()
        {
            if (visualRoot != null)
            {
                var visual = visualRoot.GetComponentInChildren<Topaz.AnimationStudy.CharacterVisual>(true);
                if (visual != null) bodyRenderer = visual.BodyRenderer;
            }
            _agent = GetComponent<NavMeshAgent>();
            _groundSpell = GetComponent<GroundSpellAbility>();
            _rangedAudio = GetComponent<AudioSource>();
            _properties = new MaterialPropertyBlock();
            _spawnPosition = transform.position;
            _spawnRotation = visualRoot != null ? visualRoot.rotation : transform.rotation;
            if (definition == null || target == null || safeZone == null ||
                visualRoot == null || bodyRenderer == null || telegraph == null)
            {
                Debug.LogError("Practice enemy is missing a required reference.", this);
                enabled = false;
                return;
            }

            CurrentHealth = definition.Health;
            _agent.speed = definition.TravelSpeed;
            _agent.stoppingDistance = definition.CrossbowAttack != null
                ? .1f : definition.StrikeRange * .8f;
            _agent.updateRotation = false;
            telegraph.enabled = false;
        }

        void Update()
        {
            if (_state == State.Down)
            {
                return;
            }
            while (_bleedUntil > 0f && Time.time >= _nextBleedTick &&
                   _nextBleedTick <= _bleedUntil)
            {
                _nextBleedTick += _bleedInterval;
                TakeDamage(_bleedDamage);
                if (!IsAlive) return;
            }
            if (Time.time >= _bleedUntil) _bleedUntil = 0f;

            if (_flashing && Time.time >= _flashUntil)
            {
                _flashing = false;
                SetColor(normalBodyTint);
            }

            if (!_agent.isOnNavMesh) return;
            if (target.GetComponent<Topaz.LoopStudy.WorldSession>()?.IsAtHome == true)
            {
                ReturnToSpawn();
                return;
            }

            switch (_state)
            {
                case State.Windup:
                    if (definition.CrossbowAttack != null && !_rangedAimLocked)
                    {
                        Vector3 aim = target.transform.position - transform.position;
                        aim.y = 0f;
                        if (aim.sqrMagnitude > .01f)
                        {
                            _strikeDirection = aim.normalized;
                            visualRoot.rotation = Quaternion.LookRotation(_strikeDirection);
                            DrawTelegraph();
                        }
                        if (Time.time >= _phaseEnd - .2f) _rangedAimLocked = true;
                    }
                    if (Time.time >= _phaseEnd) Strike();
                    return;
                case State.Recovery:
                    if (Time.time >= _phaseEnd)
                    {
                        _state = State.Pursuit;
                        _agent.isStopped = false;
                    }
                    return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            float distanceSq = toTarget.sqrMagnitude;
            if (distanceSq > definition.DetectionRange * definition.DetectionRange)
            {
                ReturnToSpawn();
                return;
            }

            if (definition.CrossbowAttack != null)
            {
                UpdateRanged(distanceSq);
                return;
            }

            if (distanceSq <= definition.StrikeRange * definition.StrikeRange)
            {
                BeginWindup(toTarget);
                return;
            }
            if (definition.GroundSpell != null && _groundSpell != null &&
                distanceSq <= definition.GroundSpell.Range * definition.GroundSpell.Range &&
                _groundSpell.CanCast && HasLineOfSight(target.transform.position))
            {
                BeginGroundSpell(target.transform.position);
                return;
            }

            _state = State.Pursuit;
            _agent.isStopped = false;
            if (Time.time >= _nextPathUpdate)
            {
                _agent.SetDestination(target.transform.position);
                _nextPathUpdate = Time.time + 0.2f;
            }
            if (_agent.velocity.sqrMagnitude > 0.01f)
                visualRoot.rotation = Quaternion.RotateTowards(visualRoot.rotation,
                    Quaternion.LookRotation(_agent.velocity.normalized, Vector3.up), 540f * Time.deltaTime);
        }

        void UpdateRanged(float distanceSq)
        {
            CrossbowAttackDefinition attack = definition.CrossbowAttack;
            if (_rangedRepositioning && rangedPositions != null &&
                rangedPositions.Length > 0)
            {
                Transform position = rangedPositions[_rangedPositionIndex % rangedPositions.Length];
                _agent.isStopped = false;
                if (Time.time >= _nextPathUpdate)
                {
                    _agent.SetDestination(position.position);
                    _nextPathUpdate = Time.time + .2f;
                }
                if (Vector3.Distance(transform.position, position.position) > .45f) return;
                _rangedRepositioning = false;
                _agent.ResetPath();
            }
            _agent.isStopped = true;
            if (distanceSq <= attack.Range * attack.Range &&
                HasLineOfSight(target.transform.position))
                BeginWindup(target.transform.position - transform.position);
        }

        public void TakeDamage(int amount) => TakeDirectedDamage(amount, Vector3.zero);

        public void TakeDirectedDamage(int amount, Vector3 attackerPosition)
        {
            if (amount <= 0 || !IsAlive) return;
            if (definition.Shielded && _state != State.Windup &&
                _state != State.Recovery && attackerPosition != Vector3.zero)
            {
                Vector3 fromAttacker = attackerPosition - transform.position;
                fromAttacker.y = 0f;
                if (fromAttacker.sqrMagnitude > .01f &&
                    Vector3.Angle(visualRoot.forward, fromAttacker) <= 55f) return;
            }
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            if (CurrentHealth == 0)
            {
                Fall();
                return;
            }
            _flashUntil = Time.time + HitReactionSeconds;
            _flashing = true;
            SetColor(HitColor);
        }

        public void ApplyBleed(int damage, float duration, float interval)
        {
            if (!IsAlive || damage <= 0 || duration <= 0f || interval <= 0f) return;
            if (_bleedUntil <= Time.time) _nextBleedTick = Time.time + interval;
            _bleedDamage = damage;
            _bleedInterval = interval;
            _bleedUntil = Time.time + duration;
        }

        public void StaggerAfterBlock(float extraSeconds = 0f)
        {
            if (!IsAlive) return;
            if (_castingGroundSpell) _groundSpell?.Cancel();
            _castingGroundSpell = false;
            _guardStaggerUntil = Time.time + HitReactionSeconds;
            _state = State.Recovery;
            _phaseEnd = Mathf.Max(_phaseEnd, Time.time + definition.RecoverySeconds + extraSeconds);
            telegraph.enabled = false;
            if (_agent.isOnNavMesh) _agent.isStopped = true;
        }

        public void StaggerFromWeapon(float seconds)
        {
            if (!IsAlive || definition.StaggerImmune || seconds <= 0f) return;
            if (_castingGroundSpell) _groundSpell?.Cancel();
            _castingGroundSpell = false;
            _guardStaggerUntil = Time.time + seconds;
            _state = State.Recovery;
            _phaseEnd = _guardStaggerUntil;
            telegraph.enabled = false;
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
            }
        }

        void BeginWindup(Vector3 toTarget)
        {
            _castingGroundSpell = false;
            _state = State.Windup;
            _phaseEnd = Time.time + AttackWindupSeconds;
            _rangedAimLocked = false;
            _agent.isStopped = true;
            _agent.ResetPath();
            _strikeDirection = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
            visualRoot.rotation = Quaternion.LookRotation(_strikeDirection, Vector3.up);
            DrawTelegraph();
            telegraph.enabled = true;
        }

        void BeginGroundSpell(Vector3 center)
        {
            _state = State.Windup;
            _phaseEnd = Time.time + definition.GroundSpell.WarningSeconds;
            _castingGroundSpell = true;
            _agent.isStopped = true;
            _agent.ResetPath();
            Vector3 facing = center - transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > .01f)
                visualRoot.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
            _groundSpell.Begin(center, true, definition.Damage);
        }

        bool HasLineOfSight(Vector3 position)
        {
            Vector3 origin = transform.position + Vector3.up;
            Vector3 targetPoint = position + Vector3.up;
            Vector3 direction = targetPoint - origin;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, direction.normalized,
                direction.magnitude, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<EnemyCombatant>() != null ||
                    hit.collider.GetComponentInParent<PlayerVitality>() != null) continue;
                return false;
            }
            return true;
        }

        void Strike()
        {
            telegraph.enabled = false;
            _state = State.Recovery;
            _phaseEnd = Time.time + AttackRecoverySeconds;
            if (_castingGroundSpell)
            {
                _castingGroundSpell = false;
                return; // The shared spell resolves its own radius and damage.
            }
            if (target.GetComponent<Topaz.LoopStudy.WorldSession>()?.IsAtHome == true) return;

            if (definition.CrossbowAttack != null)
            {
                CrossbowAttackDefinition attack = definition.CrossbowAttack;
                Vector3 origin = transform.position + Vector3.up + _strikeDirection * .5f;
                CrossbowBolt bolt = Instantiate(attack.BoltPrefab);
                bolt.Launch(attack, origin, _strikeDirection, definition.Damage,
                    enemyOwner: this);
                if (attack.FireClip != null) _rangedAudio?.PlayOneShot(attack.FireClip);
                if (rangedPositions != null && rangedPositions.Length > 0)
                {
                    _rangedPositionIndex = (_rangedPositionIndex + 1) % rangedPositions.Length;
                    _rangedRepositioning = true;
                }
                return;
            }

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= definition.StrikeRange * definition.StrikeRange &&
                Vector3.Angle(_strikeDirection, toTarget) <= definition.StrikeArcDegrees * 0.5f)
                target.TryTakeDirectedDamage(definition.Damage, transform.position, this);
        }

        void ReturnToSpawn()
        {
            if (_castingGroundSpell) _groundSpell?.Cancel();
            _castingGroundSpell = false;
            if (_state == State.Windup || _state == State.Recovery) telegraph.enabled = false;
            _state = State.Idle;
            _rangedRepositioning = false;
            _agent.isStopped = false;
            if (Time.time >= _nextPathUpdate)
            {
                if ((transform.position - _spawnPosition).sqrMagnitude > 0.25f)
                    _agent.SetDestination(_spawnPosition);
                else
                    _agent.ResetPath();
                _nextPathUpdate = Time.time + 0.3f;
            }
        }

        void Fall(bool notify = true)
        {
            _groundSpell?.Cancel();
            _castingGroundSpell = false;
            _bleedUntil = 0f;
            _state = State.Down;
            _phaseEnd = float.PositiveInfinity;
            telegraph.enabled = false;
            if (_agent.isOnNavMesh) _agent.ResetPath();
            _agent.enabled = false;
            if (!keepVisualOnDefeat) visualRoot.gameObject.SetActive(false);
            if (bodyCollider != null) bodyCollider.enabled = false;
            if (notify) Defeated?.Invoke(this);
        }

        public void SetDefeatedForPersistence()
        {
            if (_state == State.Down) return;
            CurrentHealth = 0;
            Fall(false);
        }

        public void ResetForRecovery()
        {
            _groundSpell?.Cancel();
            _castingGroundSpell = false;
            _bleedUntil = 0f;
            if (!NavMesh.SamplePosition(_spawnPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                Debug.LogWarning("Enemy recovery reset could not find its NavMesh.", this);
                return;
            }
            telegraph.enabled = false;
            if (!_agent.enabled) _agent.enabled = true;
            _agent.isStopped = false;
            if (_agent.isOnNavMesh) _agent.ResetPath();
            if (!_agent.Warp(hit.position))
            {
                Debug.LogWarning("Enemy recovery reset could not reach its spawn.", this);
                return;
            }
            visualRoot.rotation = _spawnRotation;
            visualRoot.gameObject.SetActive(true);
            if (bodyCollider != null) bodyCollider.enabled = true;
            CurrentHealth = definition.Health;
            _state = State.Idle;
            _rangedRepositioning = false;
            _rangedPositionIndex = 0;
            _phaseEnd = 0f;
            _nextPathUpdate = 0f;
            _flashUntil = 0f;
            _flashing = false;
            SetColor(normalBodyTint);
        }

        void DrawTelegraph()
        {
            if (definition.CrossbowAttack != null)
            {
                Vector3 lineStart = transform.position;
                lineStart.y = Topaz.VisualStudy.GroundSurface.Height(lineStart) + .18f;
                telegraph.positionCount = 2;
                telegraph.SetPosition(0, lineStart);
                float length = definition.CrossbowAttack.Range;
                Vector3 origin = transform.position + Vector3.up;
                foreach (RaycastHit hit in Physics.RaycastAll(origin, _strikeDirection,
                             length, Physics.DefaultRaycastLayers,
                             QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.GetComponentInParent<EnemyCombatant>() != null ||
                        hit.collider.GetComponentInParent<PlayerVitality>() != null) continue;
                    length = Mathf.Min(length, hit.distance);
                }
                telegraph.SetPosition(1, lineStart + _strikeDirection * length);
                return;
            }
            const int segments = 14;
            Vector3 center = transform.position;
            center.y = Topaz.VisualStudy.GroundSurface.Height(center) + .18f;
            telegraph.positionCount = segments + 3;
            telegraph.SetPosition(0, center);
            for (int i = 0; i <= segments; i++)
            {
                float degrees = Mathf.Lerp(-definition.StrikeArcDegrees * 0.5f,
                    definition.StrikeArcDegrees * 0.5f, (float)i / segments);
                Vector3 direction = Quaternion.AngleAxis(degrees, Vector3.up) * _strikeDirection;
                telegraph.SetPosition(i + 1, center + direction * definition.StrikeRange);
            }
            telegraph.SetPosition(segments + 2, center);
        }

        void SetColor(Color color)
        {
            _properties.SetColor(BaseColor, color);
            bodyRenderer.SetPropertyBlock(_properties);
        }
    }
}
