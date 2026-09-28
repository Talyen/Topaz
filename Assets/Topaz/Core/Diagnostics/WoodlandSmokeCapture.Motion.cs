using System;
using Topaz.Gameplay;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class MotionFrame { public string file;public double seconds;public Vector3 player,camera; }
        [Serializable] sealed class MotionReport
        {
            public string mode,note="Asynchronous readback, bounded three-slot encoder. Actual timestamps; not a GPU benchmark.";
            public int width,height,dropped;
            public bool verticalFlipApplied;
            public List<MotionFrame> frames=new List<MotionFrame>();
        }
        IEnumerator OutdoorMotion(string mode,Vector3 start,Vector3 target,string directory)
        {
            yield return M0Position(start,(target-start).normalized);
            yield return CaptureMotionFrames(mode,directory,m0Session,4,()=>
            {
                var direction=target-m0Session.transform.position;direction.y=0;
                var camera=Camera.main.transform;
                var forward=Vector3.ProjectOnPlane(camera.forward,Vector3.up).normalized;var right=Vector3.ProjectOnPlane(camera.right,Vector3.up).normalized;
                var stick=direction.magnitude<.6f?Vector2.zero:new Vector2(Vector3.Dot(direction.normalized,right),Vector3.Dot(direction.normalized,forward));
                InputSystem.QueueStateEvent(m0Pad,new GamepadState{leftStick=stick});
            });
            InputSystem.QueueStateEvent(m0Pad,new GamepadState());
        }
        IEnumerator CaptureMotionFrames(string mode,string directory,WorldSession session,float seconds,Action prepareFrame=null)
        {
            string output=Path.Combine(directory,"motion-"+mode);Directory.CreateDirectory(output);
            int width=Screen.width,height=Screen.height;bool flip=SystemInfo.graphicsUVStartsAtTop;var report=new MotionReport{mode=mode,width=width,height=height,verticalFlipApplied=flip};
            var rows=new byte[3][];var buffers=new byte[3][];var targets=new RenderTexture[3];var busy=new int[3];
            var failures=new ConcurrentQueue<string>();var writes=new List<Task>();
            for(int i=0;i<3;i++){rows[i]=new byte[width*4];buffers[i]=new byte[width*height*4];targets[i]=new RenderTexture(width,height,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);targets[i].Create();}
            bool Pending(){for(int i=0;i<3;i++)if(Volatile.Read(ref busy[i])!=0)return true;return false;}
            float began=Time.realtimeSinceStartup,next=began;
            try
            {
                while(Time.realtimeSinceStartup-began<seconds)
                {
                    prepareFrame?.Invoke();
                    yield return new WaitForEndOfFrame();
                    if(Time.realtimeSinceStartup<next)continue;next=Time.realtimeSinceStartup+1/30f;
                    int slot=-1;for(int i=0;i<3;i++)if(Volatile.Read(ref busy[i])==0){slot=i;break;}
                    if(slot<0){report.dropped++;continue;}
                    busy[slot]=1;int capturedSlot=slot;
                    string file="frame-"+report.frames.Count.ToString("D4")+".png";
                    report.frames.Add(new MotionFrame{file=file,seconds=Time.realtimeSinceStartup-began,player=session.transform.position,camera=Camera.main.transform.position});
                    ScreenCapture.CaptureScreenshotIntoRenderTexture(targets[slot]);
                    AsyncGPUReadback.Request(targets[slot],0,TextureFormat.RGBA32,request=>
                    {
                        if(request.hasError){failures.Enqueue("Readback failed: "+file);Interlocked.Exchange(ref busy[capturedSlot],0);return;}
                        request.GetData<byte>().CopyTo(buffers[capturedSlot]);
                        // Unity 6.6 documents EncodeArrayToPNG as thread safe; no scene or texture API is used by this worker.
                        var write=Task.Run(()=>
                        {
                            try
                            {
                                if(flip)for(int y=0;y<height/2;y++)
                                {
                                    int stride=width*4,a=y*stride,b=(height-1-y)*stride;
                                    Buffer.BlockCopy(buffers[capturedSlot],a,rows[capturedSlot],0,stride);
                                    Buffer.BlockCopy(buffers[capturedSlot],b,buffers[capturedSlot],a,stride);
                                    Buffer.BlockCopy(rows[capturedSlot],0,buffers[capturedSlot],b,stride);
                                }
                                File.WriteAllBytes(Path.Combine(output,file),ImageConversion.EncodeArrayToPNG(buffers[capturedSlot],GraphicsFormat.R8G8B8A8_UNorm,(uint)width,(uint)height));
                            }
                            catch(Exception error){failures.Enqueue(error.ToString());}
                            finally{Interlocked.Exchange(ref busy[capturedSlot],0);}
                        });
                        writes.Add(write);
                    });
                }
                while(Pending())yield return null;
                File.WriteAllText(Path.Combine(output,"motion.json"),JsonUtility.ToJson(report,true));
                if(!failures.IsEmpty)throw new InvalidOperationException(string.Join("\n",failures));
            }
            finally
            {
                AsyncGPUReadback.WaitAllRequests();Task.WaitAll(writes.ToArray());
                foreach(var texture in targets){texture.Release();Destroy(texture);}
            }
        }
    }
}
