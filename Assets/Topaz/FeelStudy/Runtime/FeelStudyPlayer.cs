using UnityEngine;
using UnityEngine.InputSystem;
using Topaz.CombatStudy;
using Topaz.LoopStudy;

namespace Topaz.FeelStudy
{
    /// <summary>Responsive movement and aim shared by the combat graybox.</summary>
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(CharacterController))]
    public sealed class FeelStudyPlayer : MonoBehaviour
    {
        [SerializeField] InputActionAsset controls;
        [SerializeField] Camera viewCamera;
        [SerializeField] Transform visualRoot;
        [SerializeField] Renderer bodyRenderer;
        [SerializeField] Transform aimMarker;
        [SerializeField] PracticeNode[] practiceNodes;

        [SerializeField] float travelSpeed = 5.5f;
        [SerializeField] float dodgeSpeed = 12f;
        [SerializeField] float dodgeSeconds = 0.18f;
        [SerializeField] float dodgeCooldownSeconds = 0.65f;
        [SerializeField] float interactionRadius = 2.2f;

        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
        static readonly Color NormalColor = new Color(0.24f, 0.76f, 0.84f);
        static readonly Color DodgeColor = new Color(0.88f, 0.98f, 1f);
        static readonly Color HitColor = new Color(1f, 0.30f, 0.25f);

        MaterialPropertyBlock _bodyProperties;
        CharacterController _controller;
        PlayerCombat _combat;
        WorldSession _worldSession;
        InputActionMap _playerMap;
        InputAction _move;
        InputAction _aimPointer;
        InputAction _aimStick;
        InputAction _dodge;
        InputAction _interact;

        Vector3 _aimDirection = Vector3.forward;
        Vector3 _dodgeDirection;
        float _verticalVelocity;
        float _dodgeUntil;
        float _nextDodgeAt;
        bool _usingStickAim = true;
        Vector2 _previousPointerPosition;
        bool _hasPointerPosition;
        Color _appliedColor;
        float _hitUntil;
        bool _dodgeRequested;
        bool _interactRequested;

        public Vector3 AimDirection => _aimDirection;
        public Vector3 AimPointOnGround { get; private set; }
        public bool IsDodging => Time.time < _dodgeUntil;
        public bool IsInvulnerable => IsDodging;

        public void ShowHit() => _hitUntil = Time.time + 0.22f;

        public void ResetMotion()
        {
            _verticalVelocity = 0f;
            _dodgeUntil = 0f;
            _dodgeRequested = false;
        }

        void Awake()
        {
            _bodyProperties = new MaterialPropertyBlock();
            _controller = GetComponent<CharacterController>();
            _combat = GetComponent<PlayerCombat>();
            _worldSession = GetComponent<WorldSession>();
            if (controls == null || viewCamera == null || visualRoot == null || bodyRenderer == null || aimMarker == null)
            {
                Debug.LogError("Feel study player is missing a required reference.", this);
                enabled = false;
                return;
            }

            _playerMap = controls.FindActionMap("Player", true);
            _move = _playerMap.FindAction("Move", true);
            _aimPointer = _playerMap.FindAction("AimPointer", true);
            _aimStick = _playerMap.FindAction("AimStick", true);
            _dodge = _playerMap.FindAction("Dodge", true);
            _interact = _playerMap.FindAction("Interact", true);
            _usingStickAim = Mouse.current == null;
        }

        void OnEnable()
        {
            if (_dodge != null) _dodge.performed += OnDodgePerformed;
            if (_interact != null) _interact.performed += OnInteractPerformed;
            _playerMap?.Enable();
        }

        void OnDisable()
        {
            if (_dodge != null) _dodge.performed -= OnDodgePerformed;
            if (_interact != null) _interact.performed -= OnInteractPerformed;
            _playerMap?.Disable();
        }

        void OnDodgePerformed(InputAction.CallbackContext context) => _dodgeRequested = true;

        void OnInteractPerformed(InputAction.CallbackContext context) => _interactRequested = true;

        void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime <= 0f) return;

            Vector2 moveInput = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            if (_worldSession != null && _worldSession.BlockMovement) moveInput = Vector2.zero;
            Vector3 moveDirection = ScreenRelative(moveInput);
            UpdateAim();

            if (_dodgeRequested && Time.time >= _nextDodgeAt &&
                (_combat == null || _combat.CanStartDodge) &&
                (_worldSession == null || (!_worldSession.BlockMovement && !_worldSession.IsPlacing)))
            {
                _dodgeDirection = moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : _aimDirection;
                _dodgeUntil = Time.time + dodgeSeconds;
                _nextDodgeAt = _dodgeUntil + dodgeCooldownSeconds;
                _combat?.OnDodgeStarted();
            }
            _dodgeRequested = false;

            bool dodging = IsDodging;
            float movementMultiplier = _combat != null ? _combat.MovementMultiplier : 1f;
            Vector3 horizontal = dodging ? _dodgeDirection * dodgeSpeed :
                moveDirection * travelSpeed * movementMultiplier;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity - 24f * deltaTime;
            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * deltaTime);

            Vector3 facing = _combat != null && _combat.IsAttackLocked
                ? _combat.LockedDirection : _aimDirection;
            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation,
                Quaternion.LookRotation(facing, Vector3.up),
                900f * deltaTime);
            aimMarker.position = transform.position + _aimDirection * 4.2f + Vector3.up * 0.08f;

            Color tint = dodging ? DodgeColor : Time.time < _hitUntil ? HitColor : NormalColor;
            if (tint != _appliedColor)
            {
                _bodyProperties.SetColor(BaseColor, tint);
                bodyRenderer.SetPropertyBlock(_bodyProperties);
                _appliedColor = tint;
            }

            PracticeNode nearest = null;
            float nearestDistanceSq = interactionRadius * interactionRadius;
            foreach (PracticeNode node in practiceNodes)
            {
                if (node == null || !node.isActiveAndEnabled) continue;
                float distanceSq = (node.transform.position - transform.position).sqrMagnitude;
                if (distanceSq >= nearestDistanceSq) continue;
                nearest = node;
                nearestDistanceSq = distanceSq;
            }

            foreach (PracticeNode node in practiceNodes)
                if (node != null && node.isActiveAndEnabled) node.SetNearby(node == nearest);
            if (_interactRequested && (_worldSession == null || !_worldSession.TryInteract()) &&
                nearest != null) nearest.Interact();
            _interactRequested = false;
        }

        Vector3 ScreenRelative(Vector2 input)
        {
            Vector3 right = viewCamera.transform.right;
            Vector3 forward = viewCamera.transform.forward;
            right.y = 0f;
            forward.y = 0f;
            right.Normalize();
            forward.Normalize();
            return right * input.x + forward * input.y;
        }

        void UpdateAim()
        {
            Vector2 pointerPosition = _aimPointer.ReadValue<Vector2>();
            if (_hasPointerPosition && (pointerPosition - _previousPointerPosition).sqrMagnitude > 1f)
                _usingStickAim = false;
            _previousPointerPosition = pointerPosition;
            _hasPointerPosition = true;

            Vector2 stick = _aimStick.ReadValue<Vector2>();
            if (stick.sqrMagnitude > 0.04f)
            {
                _usingStickAim = true;
                Vector3 direction = ScreenRelative(stick);
                if (direction.sqrMagnitude > 0.01f) _aimDirection = direction.normalized;
            }

            if (_usingStickAim)
            {
                AimPointOnGround = transform.position + _aimDirection * 2.2f;
                AimPointOnGround = new Vector3(AimPointOnGround.x, 0f, AimPointOnGround.z);
                return;
            }

            Ray ray = viewCamera.ScreenPointToRay(pointerPosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance))
            {
                AimPointOnGround = transform.position + _aimDirection * 2.2f;
                return;
            }

            AimPointOnGround = ray.GetPoint(distance);
            Vector3 directionToPointer = AimPointOnGround - transform.position;
            directionToPointer.y = 0f;
            if (directionToPointer.sqrMagnitude > 0.04f)
                _aimDirection = directionToPointer.normalized;
        }
    }
}
