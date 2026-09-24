using UnityEngine;
using UnityEngine.AI;

namespace Topaz.CombatStudy
{
    /// <summary>One NavMesh-driven practice enemy with an aimed, avoidable attack tell.</summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyCombatant : MonoBehaviour
    {
        enum State { Idle, Pursuit, Windup, Recovery, Down }

        [SerializeField] EnemyDefinition definition;
        [SerializeField] PlayerVitality target;
        [SerializeField] SafeZone safeZone;
        [SerializeField] Transform visualRoot;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Collider bodyCollider;
        [SerializeField] LineRenderer telegraph;
        [SerializeField] bool keepVisualOnDefeat;
        [SerializeField] bool respawns = true;
        [SerializeField] Color normalBodyTint = new Color(0.87f, 0.36f, 0.31f);

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color HitColor = new Color(1f, 0.92f, 0.72f);

        NavMeshAgent _agent;
        MaterialPropertyBlock _properties;
        Vector3 _spawnPosition;
        Vector3 _strikeDirection = Vector3.forward;
        State _state;
        float _phaseEnd;
        float _nextPathUpdate;
        float _flashUntil;
        bool _flashing;

        public int CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public bool IsAttacking => _state == State.Windup || _state == State.Recovery;
        public bool IsWindingUp => _state == State.Windup;
        public bool HasHitReaction => Time.time < _flashUntil;
        public float HitReactionSeconds => 0.3f;
        public float AttackAnimationSeconds => definition.TelegraphSeconds + definition.RecoverySeconds;
        public float AttackWindupSeconds => definition.TelegraphSeconds;
        public float AttackRecoverySeconds => definition.RecoverySeconds;
        public float TravelSpeed => definition.TravelSpeed;

        public void BindTarget(PlayerVitality player, SafeZone home)
        {
            target = player;
            safeZone = home;
        }

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _properties = new MaterialPropertyBlock();
            _spawnPosition = transform.position;
            if (definition == null || target == null || safeZone == null ||
                visualRoot == null || bodyRenderer == null || telegraph == null)
            {
                Debug.LogError("Practice enemy is missing a required reference.", this);
                enabled = false;
                return;
            }

            CurrentHealth = definition.Health;
            _agent.speed = definition.TravelSpeed;
            _agent.stoppingDistance = definition.StrikeRange * 0.8f;
            _agent.updateRotation = false;
            telegraph.enabled = false;
        }

        void Update()
        {
            if (_state == State.Down)
            {
                if (respawns && Time.time >= _phaseEnd) Respawn();
                return;
            }

            if (_flashing && Time.time >= _flashUntil)
            {
                _flashing = false;
                SetColor(normalBodyTint);
            }

            if (!_agent.isOnNavMesh) return;
            if (safeZone.Contains(target.transform.position))
            {
                ReturnToSpawn();
                return;
            }

            switch (_state)
            {
                case State.Windup:
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

            if (distanceSq <= definition.StrikeRange * definition.StrikeRange)
            {
                BeginWindup(toTarget);
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

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || !IsAlive) return;
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

        void BeginWindup(Vector3 toTarget)
        {
            _state = State.Windup;
            _phaseEnd = Time.time + definition.TelegraphSeconds;
            _agent.isStopped = true;
            _agent.ResetPath();
            _strikeDirection = toTarget.sqrMagnitude > 0.01f ? toTarget.normalized : transform.forward;
            visualRoot.rotation = Quaternion.LookRotation(_strikeDirection, Vector3.up);
            DrawTelegraph();
            telegraph.enabled = true;
        }

        void Strike()
        {
            telegraph.enabled = false;
            _state = State.Recovery;
            _phaseEnd = Time.time + definition.RecoverySeconds;
            if (safeZone.Contains(target.transform.position)) return;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= definition.StrikeRange * definition.StrikeRange &&
                Vector3.Angle(_strikeDirection, toTarget) <= definition.StrikeArcDegrees * 0.5f)
                target.TryTakeDamage(definition.Damage);
        }

        void ReturnToSpawn()
        {
            if (_state == State.Windup || _state == State.Recovery) telegraph.enabled = false;
            _state = State.Idle;
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

        void Fall()
        {
            _state = State.Down;
            _phaseEnd = respawns ? Time.time + 3f : float.PositiveInfinity;
            telegraph.enabled = false;
            if (_agent.isOnNavMesh) _agent.ResetPath();
            _agent.enabled = false;
            if (!keepVisualOnDefeat) visualRoot.gameObject.SetActive(false);
            if (bodyCollider != null) bodyCollider.enabled = false;
        }

        void Respawn()
        {
            if (!NavMesh.SamplePosition(_spawnPosition, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                _phaseEnd = Time.time + 1f;
                return;
            }
            _agent.enabled = true;
            if (!_agent.Warp(hit.position)) Debug.LogWarning("Practice enemy could not return to its NavMesh.", this);
            visualRoot.gameObject.SetActive(true);
            if (bodyCollider != null) bodyCollider.enabled = true;
            CurrentHealth = definition.Health;
            _state = State.Idle;
            SetColor(normalBodyTint);
        }

        void DrawTelegraph()
        {
            const int segments = 14;
            Vector3 center = transform.position + Vector3.up * 0.09f;
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
