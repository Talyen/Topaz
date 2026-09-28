using System;
using System.Collections.Generic;
using UnityEngine.Profiling;

namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class GpuPassTiming
        {
            public string name;
            public int samples;
            public double meanMs;
        }
        sealed class GpuPassProbe : IDisposable
        {
            readonly List<(Recorder recorder,bool prior,GpuPassTiming timing)> entries=new List<(Recorder,bool,GpuPassTiming)>();
            public GpuPassProbe(bool enabled)
            {
                if(!enabled)return;
                var names=new List<string>();Sampler.GetNames(names);
                foreach(string name in names)
                {
                    if(!(name.Contains("Surface Cache")||name.Contains("Shadow")||name.Contains("Opaque")||
                        name.Contains("Depth")||name.Contains("Temporal")||name.Contains("Bokeh")||name.Contains("UberPost")||name.Contains("MotionVector")))continue;
                    var recorder=Sampler.Get(name).GetRecorder();if(!recorder.isValid)continue;
                    entries.Add((recorder,recorder.enabled,new GpuPassTiming{name=name}));recorder.enabled=true;
                }
            }
            public void Sample()
            {
                foreach(var entry in entries)
                    if(entry.recorder.gpuSampleBlockCount>0)
                    {entry.timing.samples++;entry.timing.meanMs+=entry.recorder.gpuElapsedNanoseconds/1e6;}
            }
            public List<GpuPassTiming> Results()
            {
                var result=new List<GpuPassTiming>();
                foreach(var entry in entries)
                    if(entry.timing.samples>0){entry.timing.meanMs/=entry.timing.samples;result.Add(entry.timing);}
                result.Sort((a,b)=>b.meanMs.CompareTo(a.meanMs));return result;
            }
            public void Dispose(){foreach(var entry in entries)entry.recorder.enabled=entry.prior;}
        }
    }
}
