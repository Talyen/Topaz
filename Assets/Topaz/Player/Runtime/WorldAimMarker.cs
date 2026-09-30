using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.Rendering;

namespace Topaz.Player
{
    /// <summary>World-space targeting feedback at the actual spell/shot destination.</summary>
    [DefaultExecutionOrder(110)]
    public sealed class WorldAimMarker : MonoBehaviour
    {
        PlayerController player;
        WorldSession session;
        PlayerCombat combat;
        LineRenderer ring;
        Material material;
        readonly RaycastHit[] hits=new RaycastHit[32];
        public void Bind(PlayerController target)
        {
            player=target;session=target.GetComponent<WorldSession>();combat=target.GetComponent<PlayerCombat>();
            var marker=new GameObject("World Aim Marker");marker.transform.SetParent(transform,false);marker.layer=2;
            ring=marker.AddComponent<LineRenderer>();ring.useWorldSpace=true;ring.loop=true;ring.positionCount=32;
            ring.widthMultiplier=.045f;ring.shadowCastingMode=ShadowCastingMode.Off;ring.receiveShadows=false;
            material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.color=new Color(.95f,.8f,.35f);
            ring.sharedMaterial=material;ring.enabled=false;
        }
        void LateUpdate()
        {
            if(player==null || ring==null)return;
            var weapon=combat.CurrentWeapon;
            bool ranged=weapon?.CrossbowAttack!=null,spell=weapon?.GroundSpell!=null;
            bool show=session.HasActivePair && !session.MenuOpen && !session.IsBuilding && !session.BlockMovement &&
                (ranged || spell) && player.HasAimSurface && !(spell && combat.IsAttackLocked);
            ring.enabled=show;if(!show)return;
            Vector3 center=player.AimPointOnGround;
            Vector3 offset=Vector3.ProjectOnPlane(center-player.transform.position,Vector3.up);
            if(offset.magnitude>player.AimRange)
            {center=player.transform.position+offset.normalized*player.AimRange;center=player.ResolveGroundPoint(center);}
            float radius=spell?weapon.GroundSpell.Radius:.22f;
            bool blocked=false;
            Vector3 origin=player.transform.position+Vector3.up;
            Vector3 endpoint=ranged?player.ProjectileAimPoint:center+Vector3.up;
            Vector3 direction=endpoint-origin;int count=Physics.RaycastNonAlloc(origin,direction.normalized,hits,direction.magnitude,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];
                if(hit.collider.transform.IsChildOf(player.transform) || hit.collider.GetComponentInParent<EnemyCombatant>()!=null)continue;
                blocked=true;break;
            }
            material.color=blocked?new Color(1,.35f,.15f):new Color(.95f,.8f,.35f);
            for(int i=0;i<32;i++)
            {
                float angle=i*Mathf.PI*2/32;
                Vector3 p=center+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius);
                // Conform terrain tells; preserve the selected supported floor/bridge elevation.
                p.y=Mathf.Max(center.y,Topaz.Generation.WoodlandRegion.GroundHeight(p))+.08f;
                ring.SetPosition(i,p);
            }
        }
        void OnDisable(){if(ring!=null)ring.enabled=false;}
        void OnDestroy(){if(material!=null)Destroy(material);}
    }
}
