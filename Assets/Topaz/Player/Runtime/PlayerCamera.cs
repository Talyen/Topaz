using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Topaz.Gameplay;

namespace Topaz.Player
{
    /// <summary>Input adapter for Unity Cinemachine's third-person follow and collision handling.</summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] PlayerController target;
        [SerializeField] InputActionAsset controls;
        [SerializeField] float mouseSensitivity = .12f;
        [SerializeField] float stickSensitivity = 140;
        [SerializeField] bool invertY;
        [SerializeField] float minimumZoom = 2;
        [SerializeField] float maximumZoom = 9;
        float distance = 6.5f, yaw, pitch = 14;
        Transform pivot;
        CinemachineCamera rig;
        CinemachineThirdPersonFollow follow;
        WorldSession session;
        InputAction mouseLook, stickLook, wheel, zoomIn, zoomOut;
        public float CurrentZoom => distance;
        public void SetZoom(float value) => distance = Mathf.Clamp(value, minimumZoom, maximumZoom);
        public void ConfigureLook(float mouse, float stick, bool inverted)
        {
            mouseSensitivity = Mathf.Clamp(mouse, .01f, 1); stickSensitivity = Mathf.Clamp(stick, 20, 300); invertY = inverted;
            PlayerPrefs.SetFloat("Camera.Mouse",mouseSensitivity); PlayerPrefs.SetFloat("Camera.Stick",stickSensitivity);
            PlayerPrefs.SetInt("Camera.InvertY",invertY ? 1 : 0);
        }
        public void LookAtPoint(Vector3 point)
        {
            Vector3 direction=point-target.transform.position;
            yaw=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
            UpdatePivot();
            transform.rotation=Quaternion.Euler(pitch,yaw,0);
        }
        void Awake()
        {
            if (target == null || controls == null) { enabled=false; return; }
            var camera = GetComponent<Camera>(); camera.orthographic=false; camera.fieldOfView=58; camera.nearClipPlane=.1f; camera.farClipPlane=900;
            if (!TryGetComponent<CinemachineBrain>(out _)) gameObject.AddComponent<CinemachineBrain>();
            session = target.GetComponent<WorldSession>();
            var map = controls.FindActionMap("Player", true);
            mouseLook=map.FindAction("LookMouse"); stickLook=map.FindAction("AimStick",true);
            wheel=map.FindAction("ZoomWheel",true); zoomIn=map.FindAction("ZoomIn",true); zoomOut=map.FindAction("ZoomOut",true);
            mouseSensitivity=PlayerPrefs.GetFloat("Camera.Mouse",.12f); stickSensitivity=PlayerPrefs.GetFloat("Camera.Stick",140);
            invertY=PlayerPrefs.GetInt("Camera.InvertY",0)!=0;
            pivot = new GameObject("Camera Aim Pivot").transform;
            rig = new GameObject("Third Person Camera").AddComponent<CinemachineCamera>();
            rig.Follow=pivot; rig.Lens.FieldOfView=58;
            follow=rig.gameObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset=new Vector3(.35f,0,0); follow.VerticalArmLength=0; follow.CameraSide=1;
            follow.Damping=new Vector3(.1f,.1f,.1f);
            follow.AvoidObstacles=new CinemachineThirdPersonFollow.ObstacleSettings { Enabled=true, CollisionFilter=~(1<<2), IgnoreTag="Player", CameraRadius=.2f, DampingIntoCollision=0, DampingFromCollision=.2f };
            UpdatePivot();
        }
        void Update()
        {
            bool blocked = session != null && (session.BlockMovement || !session.HasActivePair);
            bool lockCursor=!blocked && Application.isFocused;
            Cursor.lockState=lockCursor ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible=!lockCursor;
            if (!blocked && Time.deltaTime > 0 && (Application.isFocused || Application.isBatchMode))
            {
                Vector2 delta=(mouseLook?.ReadValue<Vector2>() ?? Vector2.zero)*mouseSensitivity + stickLook.ReadValue<Vector2>()*stickSensitivity*Time.deltaTime;
                yaw += delta.x; pitch=Mathf.Clamp(pitch + delta.y*(invertY ? 1 : -1),-25,70);
                float scroll=wheel.ReadValue<float>(); if(Mathf.Abs(scroll)>.01f) SetZoom(distance-Mathf.Sign(scroll)*.5f);
                if(zoomIn.WasPressedThisFrame()) SetZoom(distance-.5f); if(zoomOut.WasPressedThisFrame()) SetZoom(distance+.5f);
            }
            UpdatePivot();
        }
        void LateUpdate() => UpdatePivot();
        void UpdatePivot()
        {
            if(pivot==null || target==null) return;
            pivot.SetPositionAndRotation(target.transform.position+Vector3.up*1.55f,Quaternion.Euler(pitch,yaw,0));
            follow.CameraDistance=distance;
        }
        void OnDestroy()
        {
            if(pivot!=null) Destroy(pivot.gameObject); if(rig!=null) Destroy(rig.gameObject);
            Cursor.lockState=CursorLockMode.None; Cursor.visible=true;
        }
    }
}
