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
        VisualEffect rainInstance;
        readonly Dictionary<Campfire,VisualEffect> fires=new Dictionary<Campfire,VisualEffect>();
        readonly List<GameObject> bursts=new List<GameObject>();
        float nextScan;
        bool wasDodging, wasStriking;
        public int ActiveBurstCount => bursts.FindAll(b=>b!=null).Count;
        void Start()
        {
            session=FindAnyObjectByType<WorldSession>();combat=session!=null?session.GetComponent<PlayerCombat>():null;
            if(combat!=null)combat.WeaponHit+=OnHit;
            rainInstance=Create(rain,"Rain",transform);
            if(session==null)Emit(new Vector3(0,2,-3),Vector3.up,1,"magic");
        }
        VisualEffect Create(VisualEffectAsset asset,string label,Transform parent)
        {
            if(asset==null)return null;var go=new GameObject(label);go.transform.SetParent(parent,false);var fx=go.AddComponent<VisualEffect>();fx.visualEffectAsset=asset;return fx;
        }
        void Update()
        {
            if(session!=null)
            {
                var movement=session.GetComponent<Topaz.Player.PlayerController>();
                bool dodging=movement.IsDodging, striking=combat!=null&&combat.IsStrikeActive;
                if(dodging&&!wasDodging)Emit(session.transform.position+Vector3.up*.2f,Vector3.up,.6f,"dodge");
                if(striking&&!wasStriking)Emit(session.transform.position+Vector3.up+movement.AimDirection, movement.AimDirection,.7f,"swing");
                wasDodging=dodging;wasStriking=striking;
            }

            if(rainInstance!=null)
            {
                rainInstance.transform.position=session!=null?session.transform.position:Vector3.zero;
                if(rainInstance.HasFloat("Rate"))rainInstance.SetFloat("Rate",session?.CurrentWeather==WeatherSchedule.Rain?400:0);
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
        void OnDestroy(){if(combat!=null)combat.WeaponHit-=OnHit;}
    }
}
