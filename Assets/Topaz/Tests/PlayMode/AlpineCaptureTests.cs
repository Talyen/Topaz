using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
namespace Topaz.Tests
{
    public sealed class AlpineCaptureTests : TopazInputTestFixture
    {
        static object P(object o,string n)=>o.GetType().GetProperty(n).GetValue(o);
        static object F(object o,string n)=>o.GetType().GetField(n).GetValue(o);
        static object Call(object o,string n,params object[] a)=>o.GetType().GetMethod(n).Invoke(o,a);
        [UnityTest,Category("Stress")]
        public IEnumerator RenderAlpineReviewAndTraceGraphicsStates()
        {
            yield return SceneManager.LoadSceneAsync("Bootstrap");yield return WaitForWilderness();
            var player=GameObject.Find("Player");var session=player.GetComponent("WorldSession");var stream=(Component)P(P(session,"ActiveRegion"),"Streaming");var plan=P(stream,"Plan");
            var clock=session.GetType().GetField("_data",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(session);clock.GetType().GetField("worldHours").SetValue(clock,12d);
            var look=UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c=>c.GetType().Name=="VisualLookController");Call(look,"BeginWorldPresentation",12d);Call(look,"SetWorldHours",12d);
            string directory="TestResults/artifacts/viking-alpine-scene";Directory.CreateDirectory(directory);
            var routes=((IEnumerable)P(plan,"Routes")).Cast<object>().ToArray();
            Vector3[] Points(int n)=>((IEnumerable)F(routes[n],"Points")).Cast<Vector3>().ToArray();
            var destinations=new[]{("hearth",Vector3.zero,Vector3.forward),
                ("forest",Points(1)[Points(1).Length-3],(Points(1).Last()-Points(1)[Points(1).Length-3]).normalized),
                ("mountain",Points(5)[Points(5).Length-3],(Points(5).Last()-Points(5)[Points(5).Length-3]).normalized)};
            var states=new GraphicsStateCollection();Assert.That(states.BeginTrace(),Is.True);
            var camera=Camera.main;var prior=camera.targetTexture;var target=new RenderTexture(1600,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);target.Create();
            try
            {
                foreach(var view in destinations)
                {
                    yield return (IEnumerator)Call(stream,"PrepareDestination",view.Item2);
                    var point=view.Item2;point.y=(float)Call(plan,"Height",point.x,point.z);BuildingTestActions.Teleport(player,point);
                    Call(camera.GetComponent("PlayerCamera"),"LookAtPoint",point+view.Item3*20);
                    camera.targetTexture=target;
                    for(int frame=0;frame<16;frame++){yield return null;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});}
                    Capture(target,Path.Combine(directory,view.Item1+".png"));
                }
                var crossings=((IEnumerable)P(plan,"Crossings")).Cast<object>().ToArray();Assert.That(crossings.Length,Is.GreaterThan(0));
                var position=(Vector3)F(crossings[0],"Position");var forward=(Vector3)F(crossings[0],"Forward");position-=forward*12;
                yield return (IEnumerator)Call(stream,"PrepareDestination",position);position.y=(float)Call(plan,"Height",position.x,position.z);BuildingTestActions.Teleport(player,position);
                Call(camera.GetComponent("PlayerCamera"),"LookAtPoint",position+forward*20);
                foreach(string weather in new[]{"clear","rain"})
                {
                    Call(session,"SetEventWeatherOverride",weather);
                    var weatherView=UnityEngine.Object.FindObjectsByType<MonoBehaviour>().First(c=>c.GetType().Name=="WeatherPresentation");Call(weatherView,"SetCondition",weather,true);
                    for(int frame=0;frame<32;frame++){yield return null;RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});}
                    Capture(target,Path.Combine(directory,"river-"+weather+".png"));
                }
                // Exercise the real controller over the authored bridge, without jump/teleport assistance.
                var pad=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Gamepad>();
                try
                {
                    var end=(Vector3)F(crossings[0],"Position")+forward*12;float deadline=Time.realtimeSinceStartup+18;
                    while(Time.realtimeSinceStartup<deadline)
                    {
                        var delta=end-player.transform.position;delta.y=0;if(delta.magnitude<1)break;
                        var ahead=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;var right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
                        Set(pad.leftStick,new Vector2(Vector3.Dot(delta.normalized,right),Vector3.Dot(delta.normalized,ahead)));yield return null;
                    }
                    var remaining=end-player.transform.position;remaining.y=0;Assert.That(remaining.magnitude,Is.LessThan(1),$"The bridge must support ordinary walking from bank to bank. Start={position}; stopped={player.transform.position}; target={end}; depth={Call(plan,"WaterDepth",player.transform.position.x,player.transform.position.z)}; ready={Call(stream,"IsReadyAt",player.transform.position)}; nearby={string.Join(",",Physics.OverlapSphere(player.transform.position+forward*.5f,.7f).Select(c=>c.name))}");
                }
                finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(pad);}
                Assert.That(P(stream,"Failure"),Is.Null);
            }
            finally
            {
                camera.targetTexture=prior;target.Release();UnityEngine.Object.Destroy(target);states.EndTrace();
                states.SaveToFile(Path.GetFullPath(Path.Combine(directory,"Alpine-"+SystemInfo.graphicsDeviceType+".graphicsstate")));
                File.WriteAllText(Path.Combine(directory,"render-note.txt"),"Explicit 1600x900 Unity render requests in Play Mode. Not a standalone frame-rate benchmark. Graphics states: "+states.totalGraphicsStateCount);
                UnityEngine.Object.Destroy(states);
            }
        }
        static void Capture(RenderTexture target,string path)
        {
            var prior=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=prior;UnityEngine.Object.Destroy(image);}
        }
    }
}
