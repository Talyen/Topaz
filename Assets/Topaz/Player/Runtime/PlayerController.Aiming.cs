using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Topaz.Player
{
    public sealed partial class PlayerController
    {
        Vector3 projectilePoint;
        float stickReach = 4.5f;
        public bool HasAimSurface { get; private set; }
        public Vector3 ProjectileDirection
        {
            get {var delta=projectilePoint-(transform.position+Vector3.up);return delta.sqrMagnitude>.001f ? delta.normalized : _aimDirection;}
        }
        public Vector3 ResolveGroundPoint(Vector3 point)
        {
            Vector3 origin=point;origin.y=Mathf.Max(point.y,transform.position.y)+20;
            point.y=PickSurface(new Ray(origin,Vector3.down),40,out var hit,false)
                ? hit.point.y : Topaz.Generation.WoodlandRegion.GroundHeight(point);
            return point;
        }
        public Vector3 ProjectileAimPoint => projectilePoint;
        public float AimRange => _worldSession?.CurrentWeapon?.GroundSpell != null
            ? _worldSession.CurrentWeapon.GroundSpell.Range + (_worldSession.HasTalent(SkillIds.Staff,"staff.far-sigil") ? 1 : 0)
            : _worldSession?.IsBuilding == true ? 6 : _worldSession?.CurrentWeapon?.CrossbowAttack?.Range ?? 4.5f;

        void UpdateAim(Vector3 moveDirection)
        {
            if(_worldSession != null && (_worldSession.BlockMovement || _worldSession.GameplayInputConsumed))return;
            Vector2 pointer=_aimPointer.ReadValue<Vector2>();
            Vector2 stick=_aimStick.ReadValue<Vector2>();
            var mouse=Mouse.current;var pad=Gamepad.current;
            bool pointerMoved=_hasPointerPosition && (pointer-_previousPointerPosition).sqrMagnitude>9;
            bool mouseAction=mouse!=null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || pointerMoved);
            bool padAction=pad!=null && (pad.leftStick.ReadValue().sqrMagnitude>.15f || stick.sqrMagnitude>.04f ||
                pad.rightTrigger.wasPressedThisFrame || pad.leftTrigger.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame ||
                pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame);
            if(padAction)_usingStickAim=true;
            if(mouseAction || (Keyboard.current?.anyKey.wasPressedThisFrame==true && mouse!=null))_usingStickAim=false;
            _previousPointerPosition=pointer;_hasPointerPosition=true;
            Vector3 point;
            if(_usingStickAim)
            {
                if(stick.sqrMagnitude>.04f)
                {
                    _aimDirection=ScreenRelative(stick.normalized);
                    // Magnitude adjusts ground targeting, while facing remains a unit vector.
                    stickReach=Mathf.Lerp(.75f,AimRange,Mathf.InverseLerp(.2f,1,stick.magnitude));
                }
                else if(moveDirection.sqrMagnitude>.01f && (_combat==null || (!_combat.IsAttackLocked && !_combat.IsGuarding)))
                    _aimDirection=moveDirection.normalized;
                point=transform.position+_aimDirection*Mathf.Min(stickReach,AimRange);
                HasAimSurface=PickSurface(new Ray(point+Vector3.up*20,Vector3.down),40,out var hit,false);
                point.y=HasAimSurface ? hit.point.y : Topaz.Generation.WoodlandRegion.GroundHeight(point);
                projectilePoint=point+Vector3.up;
            }
            else
            {
                var ray=viewCamera.ScreenPointToRay(pointer);
                HasAimSurface=PickSurface(ray,250,out var hit,true);
                if(HasAimSurface)
                {
                    var enemy=hit.collider.GetComponentInParent<EnemyCombatant>();
                    point=enemy!=null ? enemy.transform.position : hit.point;
                    projectilePoint=enemy!=null ? hit.collider.bounds.center : point+Vector3.up;
                }
                else
                {
                    var plane=new Plane(Vector3.up,transform.position);
                    point=plane.Raycast(ray,out float entry)?ray.GetPoint(entry):transform.position+_aimDirection*4.5f;
                    projectilePoint=point+Vector3.up;
                }
                Vector3 facing=Vector3.ProjectOnPlane(point-transform.position,Vector3.up);
                if(facing.sqrMagnitude>.01f)_aimDirection=facing.normalized;
            }
            AimPointOnGround=point;
        }

        bool PickSurface(Ray ray,float length,out RaycastHit closest,bool allowTargets)
        {
            closest=default;float nearest=float.MaxValue;
            int count=Physics.RaycastNonAlloc(ray,aimHits,length,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=aimHits[i];
                if(hit.collider.transform.IsChildOf(transform) || hit.distance>=nearest)continue;
                // Previews use Ignore Raycast; visual cutaways do not remove physical collision.
                if(SceneryCutaway.HiddenForPicking(hit.collider,hit.point))continue;
                var enemy=hit.collider.GetComponentInParent<EnemyCombatant>();
                if(enemy!=null && (!allowTargets || !enemy.IsAlive))continue;
                if(hit.collider.GetComponentInParent<HarvestTree>()!=null || hit.collider.GetComponentInParent<MiningRock>()!=null)
                {if(!allowTargets)continue;}
                else if(enemy==null && hit.normal.y<.45f)continue;
                nearest=hit.distance;closest=hit;
            }
            return nearest<float.MaxValue;
        }
    }
}
