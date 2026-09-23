using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.FeelStudy
{
    /// <summary>Temporary movement and aiming study. No combat or progression lives here.</summary>
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

        MaterialPropertyBlock _bodyProperties;
        CharacterController _controller;
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
        bool _wasDodging;
        bool _dodgeRequested;
        bool _interactRequested;

        public Vector3 AimDirection => _aimDirection;
        public bool IsInvulnerable => Time.time < _dodgeUntil;

        void Awake()
        {
            _bodyProperties = new MaterialPropertyBlock();
            _controller = GetComponent<CharacterController>();
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
            Vector3 moveDirection = ScreenRelative(moveInput);
            UpdateAim();

            if (_dodgeRequested && Time.time >= _nextDodgeAt)
            {
                _dodgeDirection = moveDirection.sqrMagnitude > 0.01f ? moveDirection.normalized : _aimDirection;
                _dodgeUntil = Time.time + dodgeSeconds;
                _nextDodgeAt = _dodgeUntil + dodgeCooldownSeconds;
            }
            _dodgeRequested = false;

            bool dodging = Time.time < _dodgeUntil;
            Vector3 horizontal = dodging ? _dodgeDirection * dodgeSpeed : moveDirection * travelSpeed;
            _verticalVelocity = _controller.isGrounded ? -1f : _verticalVelocity - 24f * deltaTime;
            _controller.Move((horizontal + Vector3.up * _verticalVelocity) * deltaTime);

            visualRoot.rotation = Quaternion.RotateTowards(
                visualRoot.rotation,
                Quaternion.LookRotation(_aimDirection, Vector3.up),
                900f * deltaTime);
            aimMarker.position = transform.position + _aimDirection * 4.2f + Vector3.up * 0.08f;

            if (dodging != _wasDodging)
            {
                _bodyProperties.SetColor(BaseColor, dodging ? DodgeColor : NormalColor);
                bodyRenderer.SetPropertyBlock(_bodyProperties);
                _wasDodging = dodging;
            }

            PracticeNode nearest = null;
            float nearestDistanceSq = interactionRadius * interactionRadius;
            foreach (PracticeNode node in practiceNodes)
            {
                if (node == null) continue;
                float distanceSq = (node.transform.position - transform.position).sqrMagnitude;
                if (distanceSq >= nearestDistanceSq) continue;
                nearest = node;
                nearestDistanceSq = distanceSq;
            }

            foreach (PracticeNode node in practiceNodes)
                if (node != null) node.SetNearby(node == nearest);
            if (_interactRequested && nearest != null) nearest.Interact();
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

            if (_usingStickAim) return;

            Ray ray = viewCamera.ScreenPointToRay(pointerPosition);
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            if (!ground.Raycast(ray, out float distance)) return;

            Vector3 directionToPointer = ray.GetPoint(distance) - transform.position;
            directionToPointer.y = 0f;
            if (directionToPointer.sqrMagnitude > 0.04f)
                _aimDirection = directionToPointer.normalized;
        }
    }
}
