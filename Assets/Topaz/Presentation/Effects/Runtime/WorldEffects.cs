using System.Collections.Generic;
using System.Linq;
using Topaz.Combat;
using Topaz.Gameplay;
using UnityEngine;
using UnityEngine.VFX;
namespace Topaz.Rendering
{
    /// <summary>Visual-only VFX Graph bindings; no damage or persistence is derived from particles.</summary>
    public sealed class WorldEffects : MonoBehaviour
    {
        public VisualEffectAsset rain, embers, impact;
        WorldSession session;
        PlayerCombat combat;
        readonly Dictionary<Campfire,VisualEffect> fires=new Dictionary<Campfire,VisualEffect>();
        readonly List<GameObject> bursts=new List<GameObject>();
        float nextScan;
        ParticleSystem motes;
        Material moteMaterial;
        Texture2D moteTexture;
        VisualLookController look;
        bool wasDodging, wasStriking;
        public int ActiveBurstCount => bursts.FindAll(b=>b!=null).Count;
        void Start()
        {
            session=FindAnyObjectByType<WorldSession>();combat=session!=null?session.GetComponent<PlayerCombat>():null;
            if(combat!=null)combat.WeaponHit+=OnHit;
            look=FindAnyObjectByType<VisualLookController>();
            var source=Resources.Load<Material>("TopazEffectsParticles");
            if(session!=null && source!=null)
            {
                var go=new GameObject("Woodland pollen",typeof(ParticleSystem));go.transform.SetParent(transform,false);
                motes=go.GetComponent<ParticleSystem>();motes.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=motes.main;main.loop=true;main.maxParticles=96;main.startLifetime=8;main.startSpeed=.08f;main.startSize=new ParticleSystem.MinMaxCurve(.035f,.07f);main.startColor=new Color(1,.85f,.42f,.35f);main.simulationSpace=ParticleSystemSimulationSpace.World;
                var emission=motes.emission;emission.rateOverTime=6;
                var shape=motes.shape;shape.shapeType=ParticleSystemShapeType.Box;shape.scale=new Vector3(18,5,18);
                var velocity=motes.velocityOverLifetime;velocity.enabled=true;velocity.x=.12f;velocity.y=.05f;velocity.z=.06f;
                var color=motes.colorOverLifetime;color.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.2f),new GradientAlphaKey(0,1)});color.color=gradient;
                moteTexture=new Texture2D(32,32,TextureFormat.RGBA32,true){name="Soft pollen",wrapMode=TextureWrapMode.Clamp};
                var pixels=new Color[1024];for(int y=0;y<32;y++)for(int x=0;x<32;x++){float radius=new Vector2((x-15.5f)/16,(y-15.5f)/16).magnitude;pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-radius),2));}
                moteTexture.SetPixels(pixels);moteTexture.Apply();moteMaterial=new Material(source);moteMaterial.SetTexture("_BaseMap",moteTexture);
                go.GetComponent<ParticleSystemRenderer>().sharedMaterial=moteMaterial;motes.Play();
            }
            // WeatherPresentation owns the sole rain emitter (shared height-fog shader).
            if(session==null)Emit(new Vector3(0,2,-3),Vector3.up,1,"magic");
        }
        VisualEffect Create(VisualEffectAsset asset,string label,Transform parent)
        {
            if(asset==null)return null;var go=new GameObject(label);go.transform.SetParent(parent,false);var fx=go.AddComponent<VisualEffect>();fx.visualEffectAsset=asset;return fx;
        }
        void Update()
        {
            if(motes!=null && session!=null && look!=null)
            {
                motes.transform.position=session.transform.position+Vector3.up*2;
                var emission=motes.emission;emission.rateOverTime=6*look.AmbientParticles*(1-look.State.Rain);
                if(session.MenuOpen){if(motes.isPlaying)motes.Pause();}else if(motes.isPaused)motes.Play();
            }
            if(session!=null)
            {
                var movement=session.GetComponent<Topaz.Player.PlayerController>();
                bool dodging=movement.IsDodging, striking=combat!=null&&combat.IsStrikeActive;
                if(dodging&&!wasDodging)Emit(session.transform.position+Vector3.up*.2f,Vector3.up,.6f,"dodge");
                if(striking&&!wasStriking)Emit(session.transform.position+Vector3.up+movement.AimDirection, movement.AimDirection,.7f,"swing");
                wasDodging=dodging;wasStriking=striking;
            }

            if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+1;
            foreach(var key in fires.Keys.Where(k=>k==null).ToArray())fires.Remove(key);
            foreach(var fire in FindObjectsByType<Campfire>())if(!fires.ContainsKey(fire))
            {var fx=Create(embers,"Campfire Embers",fire.transform);if(fx!=null)fx.transform.localPosition=Vector3.up*.2f;fires.Add(fire,fx);}
            bursts.RemoveAll(b=>b==null);
        }
        void OnHit(EnemyCombatant target,WeaponDefinition weapon)
        {if(target!=null)Emit(target.transform.position+Vector3.up,(target.transform.position-combat.transform.position).normalized,weapon?.TwoHanded==true?1.5f:1,"impact");}
        public void Emit(Vector3 position,Vector3 direction,float intensity,string surface)
        {
            bursts.RemoveAll(b=>b==null);if(bursts.Count>=8)return;
            var fx=Create(impact,surface,transform);if(fx==null)return;
            fx.transform.SetPositionAndRotation(position,direction.sqrMagnitude>.001f?Quaternion.LookRotation(direction):Quaternion.identity);
            fx.transform.localScale=Vector3.one*Mathf.Clamp(intensity,.2f,2);fx.Play();bursts.Add(fx.gameObject);Destroy(fx.gameObject,2);
        }
        void OnDestroy(){if(combat!=null)combat.WeaponHit-=OnHit;if(moteMaterial!=null)Destroy(moteMaterial);if(moteTexture!=null)Destroy(moteTexture);}
    }
}
