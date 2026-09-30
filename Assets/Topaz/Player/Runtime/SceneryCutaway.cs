using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;

namespace Topaz.Player
{
    /// <summary>One gameplay camera's bounded visibility mask. Physics and shelter remain authoritative.</summary>
    [DefaultExecutionOrder(90)]
    public sealed class SceneryCutaway : MonoBehaviour
    {
        [SerializeField] float explorationRadius=3;
        [SerializeField] float combatRadius=5;
        [SerializeField] float transitionSeconds=.2f;
        [SerializeField] Material groundMaterial;
        Mesh capMesh;
        MeshRenderer capRenderer;
        readonly Vector3[] capVertices=new Vector3[17*17];
        Vector3 capCenter=new Vector3(float.PositiveInfinity,0,0);
        float nextCapUpdate;
        static SceneryCutaway active;
        static readonly int Reveal=Shader.PropertyToID("_TopazReveal");
        static readonly int TargetReveal=Shader.PropertyToID("_TopazTargetReveal");
        static readonly int Direction=Shader.PropertyToID("_TopazRevealDirection");
        static readonly int Enabled=Shader.PropertyToID("_TopazRevealEnabled");
        static readonly int Silhouettes=Shader.PropertyToID("_TopazSilhouetteSubjects");
        static readonly int SubjectCount=Shader.PropertyToID("_TopazSilhouetteCount");
        readonly Vector4[] subjects=new Vector4[8];
        PlayerController player;
        WorldSession session;
        Camera view;
        float radius,amount;
        Vector3 center,secondary;
        float secondaryRadius;
        public float RevealRadius=>radius;
        public void Bind(PlayerController target,Camera camera)
        {
            player=target;view=camera;session=target.GetComponent<WorldSession>();active=this;radius=explorationRadius;
            if(groundMaterial!=null)
            {
                var cap=new GameObject("Terrain Cutaway Interior");cap.transform.SetParent(transform,false);cap.layer=2;
                capMesh=new Mesh{name="Local terrain reveal cap"};capMesh.MarkDynamic();
                var uv=new Vector2[capVertices.Length];var triangles=new int[16*16*6];int index=0;
                for(int z=0;z<17;z++)for(int x=0;x<17;x++)uv[z*17+x]=new Vector2(x,z)*.5f;
                for(int z=0;z<16;z++)for(int x=0;x<16;x++)
                {int a=z*17+x;triangles[index++]=a;triangles[index++]=a+17;triangles[index++]=a+1;triangles[index++]=a+1;triangles[index++]=a+17;triangles[index++]=a+18;}
                capMesh.vertices=capVertices;capMesh.uv=uv;capMesh.triangles=triangles;
                cap.AddComponent<MeshFilter>().sharedMesh=capMesh;capRenderer=cap.AddComponent<MeshRenderer>();capRenderer.sharedMaterial=groundMaterial;
                capRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;capRenderer.receiveShadows=true;
                capRenderer.renderingLayerMask=0x80000002u;
            }
        }
        void LateUpdate()
        {
            if(player==null || view==null)return;
            bool showing=session.HasActivePair && !session.MenuOpen && session.ActiveRegion?.Streaming?.InitialReady==true;
            float blend=1-Mathf.Exp(-Time.unscaledDeltaTime/Mathf.Max(.01f,transitionSeconds));
            amount=Mathf.Lerp(amount,showing?1:0,blend);
            UpdateGroundCap(showing);
            int count=0;bool combat=false;
            subjects[count++]=new Vector4(player.transform.position.x,player.transform.position.y,player.transform.position.z,3);
            foreach(var enemy in EnemyCombatant.Active)
            {
                if(enemy==null || !enemy.IsAlive || !enemy.IsEngaged)continue;
                Vector3 p=enemy.transform.position;
                if((p-player.transform.position).sqrMagnitude>10*10)continue;
                combat=true;
                if(count<subjects.Length)subjects[count++]=new Vector4(p.x,p.y,p.z,2.5f);
            }
            radius=Mathf.Lerp(radius,combat?combatRadius:explorationRadius,blend);
            center=player.transform.position;
            Vector3 desired=center;float desiredRadius=0;
            if(session.IsBuilding && player.HasAimSurface && (player.AimPointOnGround-center).sqrMagnitude<=7*7)
            {desired=player.AimPointOnGround;desiredRadius=1.8f;}
            else if(session.TryGetInteraction(out var anchor,out _))
            {desired=anchor.position;desired.y=center.y;desiredRadius=1.5f;}
            if(secondaryRadius<.02f)secondary=desired;
            else secondary=Vector3.Lerp(secondary,desired,blend);
            secondaryRadius=Mathf.Lerp(secondaryRadius,desiredRadius,blend);
            Shader.SetGlobalVector(Reveal,new Vector4(center.x,center.y,center.z,radius));
            Shader.SetGlobalVector(TargetReveal,new Vector4(secondary.x,secondary.y,secondary.z,secondaryRadius));
            Shader.SetGlobalVector(Direction,view.transform.forward);
            Shader.SetGlobalFloat(Enabled,amount);
            Shader.SetGlobalVectorArray(Silhouettes,subjects);
            Shader.SetGlobalInt(SubjectCount,showing?count:0);
        }
        void UpdateGroundCap(bool showing)
        {
            if(capRenderer==null)return;
            capRenderer.enabled=showing;if(!showing)return;
            Vector3 p=player.transform.position;
            if((p-capCenter).sqrMagnitude<.35f*.35f && Time.unscaledTime<nextCapUpdate)return;
            capCenter=p;nextCapUpdate=Time.unscaledTime+1;
            // A heightfield has no interior. This visual-only surface fills revealed foreground
            // banks below the player's support plane, without creating collision or changing terrain.
            for(int z=0;z<17;z++)for(int x=0;x<17;x++)
            {
                Vector3 world=new Vector3(p.x+x-8,0,p.z+z-8);
                world.y=Mathf.Min(p.y-.04f,Topaz.Generation.WoodlandRegion.GroundHeight(world)-.04f);
                capVertices[z*17+x]=world;
            }
            capRenderer.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            capMesh.vertices=capVertices;capMesh.RecalculateNormals();capMesh.RecalculateBounds();
        }
        void OnEnable(){if(player!=null)active=this;}
        void OnDestroy(){if(capMesh!=null)Destroy(capMesh);}
        public static bool HiddenForPicking(Collider collider,Vector3 point)
        {
            if(collider.gameObject.layer==2)return true;
            if(collider.GetComponentInParent<EnemyCombatant>()!=null)return false;
            var roof=collider.GetComponentInParent<HomeRoofVisibility>();
            if(roof!=null && roof.IsCutAway)return true;
            if(active==null || active.amount<.5f)return false;
            // Harvestable trunk/rock colliders remain selectable; their decorative crowns do not.
            if(collider.GetComponentInParent<HarvestTree>()!=null || collider.GetComponentInParent<MiningRock>()!=null)return false;
            return InReveal(point,active.center,active.radius,active.view.transform.forward) ||
                InReveal(point,active.secondary,active.secondaryRadius,active.view.transform.forward);
        }
        public static bool InReveal(Vector3 point,Vector3 endpoint,float radius,Vector3 forward)
        {
            Vector3 delta=point-endpoint;float depth=Vector3.Dot(delta,forward);
            return radius>.01f && point.y>endpoint.y+.12f && depth<-.6f &&
                (delta-forward*depth).sqrMagnitude<radius*radius;
        }
        void OnDisable()
        {
            if(capRenderer!=null)capRenderer.enabled=false;
            if(active!=this)return;
            active=null;Shader.SetGlobalFloat(Enabled,0);Shader.SetGlobalInt(SubjectCount,0);
        }
    }
}
