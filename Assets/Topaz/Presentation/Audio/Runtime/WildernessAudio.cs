using Topaz.Gameplay;
using Topaz.Player;
using UnityEngine;

namespace Topaz.Audio
{
    /// <summary>Presentation-only footsteps and streamed ambience; never advances simulation or consumes random world state.</summary>
    public sealed class WildernessAudio : MonoBehaviour
    {
        public AudioClip[] grassSteps = System.Array.Empty<AudioClip>();
        public AudioClip[] gravelSteps = System.Array.Empty<AudioClip>();
        public AudioClip woodlandDay;
        public AudioClip woodlandWind;
        PlayerController movement;
        WorldSession session;
        AudioSource steps;
        TopazAudioOutput day, wind;
        Vector3 previous;
        float travelled;
        int step;
        void Awake()
        {
            movement=GetComponent<PlayerController>();session=GetComponent<WorldSession>();previous=transform.position;
            steps=Source("Footsteps",null,false,.13f,TopazAudioOutput.Category.Effects).GetComponent<AudioSource>();
            day=Source("Woodland birds",woodlandDay,true,0,TopazAudioOutput.Category.Ambience);
            wind=Source("Woodland wind",woodlandWind,true,0,TopazAudioOutput.Category.Ambience);
        }
        TopazAudioOutput Source(string label,AudioClip clip,bool loop,float volume,TopazAudioOutput.Category category)
        {
            var go=new GameObject(label,typeof(AudioSource));go.transform.SetParent(transform,false);
            var source=go.GetComponent<AudioSource>();source.clip=clip;source.loop=loop;source.playOnAwake=false;source.dopplerLevel=0;source.spatialBlend=0;
            var output=go.AddComponent<TopazAudioOutput>();output.Configure(category,volume);
            if(clip!=null)source.Play();return output;
        }
        void Update()
        {
            if(session==null||movement==null)return;
            float daylight=Mathf.Clamp01(Mathf.Sin(((float)(session.WorldHours%24)-6)/24*Mathf.PI*2));
            bool wet=session.CurrentWeather=="rain" || session.CurrentWeather=="storm";
            float presence=session.HasActivePair?1:.45f;
            day.SetBaseVolume(Mathf.MoveTowards(day.BaseVolume,daylight*(wet?.025f:.065f)*presence,Time.unscaledDeltaTime*.035f));
            wind.SetBaseVolume(Mathf.MoveTowards(wind.BaseVolume,(wet?.07f:.035f)*presence,Time.unscaledDeltaTime*.035f));
            Vector3 delta=transform.position-previous;previous=transform.position;delta.y=0;
            if(!session.HasActivePair||session.BlockMovement||movement.IsAirborne||movement.IsDodging||delta.sqrMagnitude>9)
            {travelled=0;return;}
            travelled+=delta.magnitude;
            float stride=movement.PlanarSpeed>3?1.45f:1f;
            if(travelled<stride)return;travelled%=stride;
            bool hard=session.ActiveRegion?.Wilderness!=null && session.ActiveRegion.Wilderness.Slope(transform.position.x,transform.position.z)>.2f;
            if(Physics.Raycast(transform.position+Vector3.up,Vector3.down,out var hit,2.5f,~0,QueryTriggerInteraction.Ignore))
                hard|=hit.collider.GetComponentInParent<RegionBuildings>()!=null;
            var clips=hard&&gravelSteps.Length>0?gravelSteps:grassSteps;
            if(clips.Length==0)return;
            steps.pitch=.96f+(step%4)*.025f;steps.PlayOneShot(clips[step++%clips.Length]);
        }
    }
}
