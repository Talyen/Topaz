using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using Topaz.Gameplay;

namespace Topaz.Player
{
    /// <summary>Fixed world-space isometric follow; aiming never rotates the view.</summary>
    [DefaultExecutionOrder(-20)]
    [RequireComponent(typeof(Camera))]
    public sealed class PlayerCamera : MonoBehaviour
    {
        [SerializeField] PlayerController target;
        [SerializeField] InputActionAsset controls;
        [SerializeField] float worldYaw = 45;
        [SerializeField] float downwardPitch = 50;
        [SerializeField] float fieldOfView = 35;
        [SerializeField] float minimumZoom = 24;
        [SerializeField] float maximumZoom = 38;
        [SerializeField] float lookAhead = 1.5f;
        [SerializeField] Vector3 followDamping = new Vector3(.12f, .2f, .12f);
        float distance = 30, requestedDistance = 30, groundY;
        Transform pivot, listener;
        CinemachineCamera rig;
        CinemachineFollow follow;
        WorldSession session;
        Camera view;
        Vector3 previousTargetPosition, lead, lastSafeLens;
        bool hasSafeLens;
        bool hasTargetPosition, pendingWarp;
        public void ResetFollow() => pendingWarp=true;
        InputAction wheel, zoomIn, zoomOut;
        public float CurrentZoom => requestedDistance;
        public void SetZoom(float value) => requestedDistance = Mathf.Clamp(value, minimumZoom, maximumZoom);
        public void SnapAfterAreaTravel() => UpdatePivot(true);
        public void SetVisibilityDistance(float value)
        {
            if(view==null)view=GetComponent<Camera>();
            view.farClipPlane=value;
            if(rig!=null)rig.Lens.FarClipPlane=value;
        }
        // Diagnostic callers retain this API, but it no longer changes the gameplay heading.
        public void LookAtPoint(Vector3 point) { UpdatePivot(false); }
        void Awake()
        {
            if (target == null || controls == null) { enabled=false; return; }
            view=GetComponent<Camera>();
            view.orthographic=false; view.fieldOfView=fieldOfView; view.nearClipPlane=.1f;
            if (!TryGetComponent<CinemachineBrain>(out _)) gameObject.AddComponent<CinemachineBrain>();
            session=target.GetComponent<WorldSession>();
            var map=controls.FindActionMap("Player",true);
            wheel=map.FindAction("ZoomWheel",true); zoomIn=map.FindAction("ZoomIn",true); zoomOut=map.FindAction("ZoomOut",true);
            pivot=new GameObject("Isometric Follow Target").transform;
            rig=new GameObject("Isometric Camera").AddComponent<CinemachineCamera>();
            rig.Follow=pivot; rig.Lens.FieldOfView=fieldOfView; rig.Lens.NearClipPlane=.1f;rig.Lens.FarClipPlane=view.farClipPlane;
            follow=rig.gameObject.AddComponent<CinemachineFollow>();
            follow.TrackerSettings.BindingMode=BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping=followDamping;
            listener=target.transform.Find("Player Hearing");
            var visibility=gameObject.GetComponent<SceneryCutaway>() ?? gameObject.AddComponent<SceneryCutaway>();
            visibility.Bind(target,view);
            var marker=gameObject.GetComponent<WorldAimMarker>() ?? gameObject.AddComponent<WorldAimMarker>();marker.Bind(target);
            UpdatePivot(true);
        }
        void Update()
        {
            bool blocked=session!=null && (session.BlockMovement || !session.HasActivePair);
            Cursor.lockState=!blocked && Application.isFocused ? CursorLockMode.Confined : CursorLockMode.None;
            Cursor.visible=true;
            if(!blocked && (session==null || !session.GameplayInputConsumed) && Time.deltaTime>0 && (Application.isFocused || Application.isBatchMode))
            {
                float scroll=wheel.ReadValue<float>();
                if(Mathf.Abs(scroll)>.01f)SetZoom(requestedDistance-Mathf.Sign(scroll)*2);
                if(zoomIn.WasPressedThisFrame())SetZoom(requestedDistance-2);
                if(zoomOut.WasPressedThisFrame())SetZoom(requestedDistance+2);
            }
        }
        void LateUpdate() => UpdatePivot(false);
        void UpdatePivot(bool force)
        {
            if(pivot==null || target==null)return;
            Vector3 position=target.transform.position;
            Vector3 delta=position-previousTargetPosition;
            bool warp=force || pendingWarp || !hasTargetPosition || delta.sqrMagnitude>16*16;
            pendingWarp=false;
            if(warp)
            {
                groundY=position.y;lead=Vector3.zero;distance=requestedDistance;hasSafeLens=false;
                rig.OnTargetObjectWarped(pivot,delta);rig.PreviousStateIsValid=false;
                if(TryGetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>(out var data))data.resetHistory=true;
            }
            else
            {
                if(!target.IsAirborne || position.y<groundY-.5f)groundY=Mathf.Lerp(groundY,position.y,1-Mathf.Exp(-Time.deltaTime/.2f));
                groundY=Mathf.Min(groundY,position.y+3); // A long fall must remain in frame.
                Vector3 desired=Vector3.ClampMagnitude(target.PlanarVelocity*.25f,lookAhead);
                lead=Vector3.Lerp(lead,desired,1-Mathf.Exp(-Time.deltaTime/.18f));
                distance=Mathf.Lerp(distance,requestedDistance,1-Mathf.Exp(-Time.deltaTime/.18f));
            }
            hasTargetPosition=true;previousTargetPosition=position;
            Quaternion rotation=Quaternion.Euler(downwardPitch,worldYaw,0);
            Vector3 anchor=new Vector3(position.x,groundY+1.3f,position.z)+lead;
            // Keep the lens outside occupied solids; intervening scenery is handled by cutaways.
            float safeDistance=distance;bool found=false;
            for(float candidate=distance;candidate<=maximumZoom+8;candidate+=1)
            {
                Vector3 lens=anchor-rotation*Vector3.forward*candidate;
                float terrain=Topaz.Generation.WoodlandRegion.GroundHeight(lens);
                if(lens.y>terrain+.5f && !Physics.CheckSphere(lens,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                {safeDistance=candidate;found=true;break;}
            }
            if(!found && !hasSafeLens)
            {
                // Initial arrival can face a taller bank. Search outward on the same viewing axis.
                for(float candidate=maximumZoom+9;candidate<=maximumZoom+80;candidate+=2)
                {
                    Vector3 lens=anchor-rotation*Vector3.forward*candidate;
                    if(lens.y>Topaz.Generation.WoodlandRegion.GroundHeight(lens)+.5f &&
                        !Physics.CheckSphere(lens,.25f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    {safeDistance=candidate;found=true;break;}
                }
            }
            if(found){lastSafeLens=anchor-rotation*Vector3.forward*safeDistance;hasSafeLens=true;}
            pivot.position=anchor;
            follow.FollowOffset=hasSafeLens ? lastSafeLens-anchor : -(rotation*Vector3.forward)*safeDistance;
            if(view.transform.position.y<Topaz.Generation.WoodlandRegion.GroundHeight(view.transform.position)+.25f ||
                Physics.CheckSphere(view.transform.position,.2f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))rig.PreviousStateIsValid=false;
            rig.transform.rotation=rotation;
            if(warp){view.transform.SetPositionAndRotation(anchor+follow.FollowOffset,rotation);}
            if(listener!=null)listener.rotation=rotation;
        }
        void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        void OnDestroy()
        {
            if(pivot!=null)Destroy(pivot.gameObject);if(rig!=null)Destroy(rig.gameObject);
        }
    }
}
