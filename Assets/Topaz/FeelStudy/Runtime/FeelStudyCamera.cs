using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.FeelStudy
{
    /// <summary>Fixed-angle camera with bounded aim look-ahead and smooth zoom.</summary>
    [RequireComponent(typeof(Camera))]
    public sealed class FeelStudyCamera : MonoBehaviour
    {
        [SerializeField] FeelStudyPlayer target;
        [SerializeField] InputActionAsset controls;
        [SerializeField] float distance = 22f;
        [SerializeField] float aimLookAhead = 1.35f;
        [SerializeField] float followSmoothSeconds = 0.14f;
        [SerializeField] float zoomSmoothSeconds = 0.12f;
        [SerializeField] float minimumZoom = 5.5f;
        [SerializeField] float maximumZoom = 13f;

        Camera _camera;
        InputAction _zoomWheel;
        InputAction _zoomIn;
        InputAction _zoomOut;
        Vector3 _followVelocity;
        float _zoomVelocity;
        float _desiredZoom;

        public float CurrentZoom => _desiredZoom;

        public void SetZoom(float size)
        {
            _desiredZoom = Mathf.Clamp(size, minimumZoom, maximumZoom);
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            if (target == null || controls == null)
            {
                Debug.LogError("Feel study camera is missing a required reference.", this);
                enabled = false;
                return;
            }

            InputActionMap map = controls.FindActionMap("Player", true);
            _zoomWheel = map.FindAction("ZoomWheel", true);
            _zoomIn = map.FindAction("ZoomIn", true);
            _zoomOut = map.FindAction("ZoomOut", true);
            _desiredZoom = _camera.orthographicSize;
        }

        void LateUpdate()
        {
            float scroll = _zoomWheel.ReadValue<float>();
            if (Mathf.Abs(scroll) > 0.01f) _desiredZoom -= Mathf.Sign(scroll);
            if (_zoomIn.WasPressedThisFrame()) _desiredZoom -= 1f;
            if (_zoomOut.WasPressedThisFrame()) _desiredZoom += 1f;
            _desiredZoom = Mathf.Clamp(_desiredZoom, minimumZoom, maximumZoom);

            Vector3 focus = target.transform.position + target.AimDirection * aimLookAhead;
            Vector3 desiredPosition = focus - transform.forward * distance;
            transform.position = Vector3.SmoothDamp(
                transform.position, desiredPosition, ref _followVelocity, followSmoothSeconds);
            _camera.orthographicSize = Mathf.SmoothDamp(
                _camera.orthographicSize, _desiredZoom, ref _zoomVelocity, zoomSmoothSeconds);
        }
    }
}
