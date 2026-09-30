using System;
using System.Collections;
using System.IO;
using System.Linq;
using Topaz.Gameplay;
using Topaz.Player;
using Topaz.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class AlpineViewRecord
        {
            public string view,condition,shading;
            public double hour;
            public Vector3 player,camera,cameraEuler;
            public int width,height,lightingStyle;
            public float fieldOfView,renderScale;
        }
        [Serializable] sealed class AlpineViewReport
        {
            public int seed,generator;
            public string settings;
            public SurfaceCacheLighting.Status gi;
            public AlpineViewRecord[] views;
        }
        RenderTexture alpineTarget,alpinePreviousTarget;
        Camera alpineCamera;
        Coroutine alpinePump;
        void StartAlpineRenderLoop()
        {
            int width=1600;
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--topaz-review-width=") && int.TryParse(arg.Substring(21),out int value))width=Mathf.Clamp(value,640,2560);
            alpineTarget=new RenderTexture(width,width*9/16,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);alpineTarget.Create();
            alpinePump=StartCoroutine(AlpineRenderLoop());
        }
        IEnumerator AlpineRenderLoop()
        {
            while(alpineTarget!=null)
            {
                yield return null;
                var current=Camera.main;if(current==null)continue;
                if(alpineCamera!=current)
                {
                    if(alpineCamera!=null)alpineCamera.targetTexture=alpinePreviousTarget;
                    alpineCamera=current;alpinePreviousTarget=current.targetTexture;
                }
                current.targetTexture=alpineTarget;
                RenderPipeline.SubmitRenderRequest(current,new RenderPipeline.StandardRequest{destination=alpineTarget});
            }
        }
        void StopAlpineRenderLoop()
        {
            if(alpinePump!=null)StopCoroutine(alpinePump);alpinePump=null;
            if(alpineCamera!=null)alpineCamera.targetTexture=alpinePreviousTarget;alpineCamera=null;
            if(alpineTarget!=null){alpineTarget.Release();Destroy(alpineTarget);alpineTarget=null;}
        }
        // Explicit render requests work without a visible swapchain. This is visual/PSO evidence, not frame pacing.
        IEnumerator CaptureAlpineViews(WorldSession session,string directory)
        {
            var stream=session.ActiveRegion.Streaming;var plan=stream.Plan;
            var camera=Camera.main;var target=alpineTarget;
            m0Session=session;m0Look=FindAnyObjectByType<VisualLookController>();m0Camera=camera.GetComponent<PlayerCamera>();
            m0Data=(TopazSaveData)typeof(WorldSession).GetField("_data",M0Fields).GetValue(session);
            m0Camera.enabled=false;m0Hour=12;
            if(Environment.GetCommandLineArgs().Contains("--topaz-dof-gentle")){m0Look.SetSetting(20,8);m0Look.SetSetting(21,45);}
            if(Environment.GetCommandLineArgs().Contains("--topaz-tone-neutral"))
            {
                var volume=(VolumeProfile)typeof(VisualLookController).GetField("profile",M0Fields).GetValue(m0Look);
                if(volume.TryGet<Tonemapping>(out var tone))tone.mode.Override(TonemappingMode.Neutral);
            }
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("-weather-preview-hour=") && double.TryParse(arg.Substring(22),System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out double value))m0Hour=value;
            bool matrix=Environment.GetCommandLineArgs().Contains("--topaz-visual-matrix");
            var conditions=matrix?new[]{("day",12d,"clear"),("golden",17d,"clear"),("rain",12d,"rain"),("night",0d,"clear")}:new[]{("day",m0Hour,"clear")};
            bool shadingStudy=Environment.GetCommandLineArgs().Contains("--topaz-material-study");
            ShadingStudy shading=shadingStudy?new ShadingStudy(session.gameObject):null;
            var variants=shadingStudy?new[]{"faceted","soft","rich"}:new[]{"authored"};
            var records=new System.Collections.Generic.List<AlpineViewRecord>();
            var states=new GraphicsStateCollection();bool tracing=states.BeginTrace(),completed=false;
            session.SetEventWeatherOverride("clear");
            var views=new[]{("hearth",Vector3.zero,Vector3.forward),
                ("forest",plan.Routes[1].Points[plan.Routes[1].Points.Count-3],(plan.Routes[1].Points.Last()-plan.Routes[1].Points[plan.Routes[1].Points.Count-3]).normalized),
                ("mountain",plan.Routes[5].Points[plan.Routes[5].Points.Count-3],(plan.Routes[5].Points.Last()-plan.Routes[5].Points[plan.Routes[5].Points.Count-3]).normalized),
                ("river",plan.Crossings[0].Position-plan.Crossings[0].Forward*12,plan.Crossings[0].Forward)};
            if(Environment.GetCommandLineArgs().Contains("--topaz-review-view=landmark"))
                views=new[]{("landmark",plan.Routes[0].Points[plan.Routes[0].Points.Count-2],
                    (plan.Routes[0].Points.Last()-plan.Routes[0].Points[plan.Routes[0].Points.Count-2]).normalized)};
            foreach(var arg in Environment.GetCommandLineArgs())if(arg.StartsWith("--topaz-review-view="))
                views=views.Where(v=>v.Item1==arg.Substring(20)).ToArray();
            if(shadingStudy)views=new[]{("portrait",Vector3.zero,Vector3.back),("forest",plan.Routes[1].Points[plan.Routes[1].Points.Count-3],(plan.Routes[1].Points.Last()-plan.Routes[1].Points[plan.Routes[1].Points.Count-3]).normalized)};
            try
            {
                foreach(var view in views)
                {
                    yield return stream.PrepareDestination(view.Item2);
                    if(stream.Failure!=null)throw new InvalidOperationException(stream.Failure);
                    var p=view.Item2;p.y=plan.Height(p.x,p.z);MovePlayer(session,p);
                    // Seat the capsule even when an unfocused review player has paused its ordinary simulation.
                    session.GetComponent<CharacterController>().Move(Vector3.down*.25f);Physics.SyncTransforms();
                    m0Camera.SetZoom(view.Item1=="portrait"?24:30);
                    m0Facing=view.Item3;m0Camera.LookAtPoint(p+view.Item3*20);camera.targetTexture=target;
                    foreach(var condition in conditions)
                    {
                    for(int variant=0;variant<variants.Length;variant++)
                    {
                    shading?.Apply(variant);
                    m0Hour=condition.Item2;session.SetEventWeatherOverride(condition.Item3);
                    FindAnyObjectByType<WeatherPresentation>().SetCondition(condition.Item3,true);
                    // Settle the native GI cache and temporal history after each move/lighting change.
                    for(int frame=0;frame<80;frame++)
                    {yield return null;}
                    var previous=RenderTexture.active;RenderTexture.active=target;
                    var picture=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                    try{picture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);picture.Apply();File.WriteAllBytes(Path.Combine(directory,view.Item1+(matrix?"-"+condition.Item1:"")+(shadingStudy?"-"+variants[variant]:"")+".png"),picture.EncodeToPNG());}
                    finally{RenderTexture.active=previous;Destroy(picture);}
                    records.Add(new AlpineViewRecord{view=view.Item1,condition=condition.Item1,shading=variants[variant],hour=m0Hour,player=p,camera=camera.transform.position,cameraEuler=camera.transform.eulerAngles,width=target.width,height=target.height,fieldOfView=camera.fieldOfView,renderScale=((UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline).renderScale,lightingStyle=m0Look.CurrentLightingStyle});
                    }
                }
                }
                completed=true;
            }
            finally
            {
                File.WriteAllText(Path.Combine(directory,"views.json"),JsonUtility.ToJson(new AlpineViewReport{seed=plan.Seed,generator=Topaz.Generation.WildernessPlan.Version,views=records.ToArray(),settings=JsonUtility.ToJson(typeof(VisualLookController).GetField("settings",M0Fields).GetValue(m0Look),true),gi=m0Look.GlobalIllumination},true));
                shading?.Dispose();
                m0Camera.enabled=true;m0Camera=null;m0Session=null;m0Data=null;m0Look=null;
                StopAlpineRenderLoop();
                if(tracing){states.EndTrace();states.SaveToFile(Path.Combine(directory,"Alpine-"+SystemInfo.graphicsDeviceType+".graphicsstate"));}
                File.WriteAllText(Path.Combine(directory,"render-note.txt"),$"Standalone explicit renders; platform={Application.platform}; API={SystemInfo.graphicsDeviceType}; seed={plan.Seed}; completed={completed}; tracing={tracing}; states={states.totalGraphicsStateCount}; errors={runtimeErrors}. Not a presentation-timing benchmark.");
                Destroy(states);
            }
        }
    }
}
