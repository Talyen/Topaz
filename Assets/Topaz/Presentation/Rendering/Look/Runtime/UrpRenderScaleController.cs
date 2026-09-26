using UnityEngine;
using System.Linq;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Topaz.Rendering
{
    /// <summary>Optional GPU-timing-based STP scale. Never scales down for a CPU bottleneck.</summary>
    public sealed class UrpRenderScaleController : MonoBehaviour
    {
        readonly FrameTiming[] timing=new FrameTiming[1];
        UniversalRenderPipelineAsset original, runtime;
        bool native=true;
        float nextCheck;
        public float Scale=>runtime!=null?runtime.renderScale:1;
        void Awake()
        {
            original=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if(original==null){enabled=false;return;}
            runtime=Instantiate(original);runtime.name="Runtime Woodland Quality";
            var args=System.Environment.GetCommandLineArgs();
            if(args.Contains("--topaz-gpu-occlusion"))runtime.gpuResidentDrawerEnableOcclusionCullingInCameras=true;
            if(args.Contains("--topaz-no-gpu-occlusion"))runtime.gpuResidentDrawerEnableOcclusionCullingInCameras=false;
            QualitySettings.renderPipeline=runtime;
        }
        public void SetNative(bool value)
        {
            native=value;
            if(runtime==null)return;
            runtime.renderScale=1;runtime.upscalingFilter=native?UpscalingFilterSelection.Auto:UpscalingFilterSelection.STP;
        }
        void Update()
        {
            if(native||runtime==null)return;
            FrameTimingManager.CaptureFrameTimings();
            if(Time.unscaledTime<nextCheck)return;nextCheck=Time.unscaledTime+.5f;
            if(FrameTimingManager.GetLatestTimings(1,timing)==0||timing[0].gpuFrameTime<=0)return;
            double hz=System.Math.Clamp(Screen.currentResolution.refreshRateRatio.value,60,120);
            double budget=1000/hz;
            float change=timing[0].gpuFrameTime>budget*1.05?-.05f:timing[0].gpuFrameTime<budget*.8?.025f:0;
            runtime.renderScale=Mathf.Clamp(runtime.renderScale+change,.7f,1);
        }
        void OnDestroy(){if(QualitySettings.renderPipeline==runtime)QualitySettings.renderPipeline=original;if(runtime!=null)Destroy(runtime);}
    }
}
