using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Topaz.Gameplay;
using Topaz.Combat;
using Topaz.Player;
using Topaz.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Topaz
{
    public sealed partial class WoodlandSmokeCapture
    {
        [Serializable] sealed class IsometricReport
        {
            public string note="Temporary smoke profile; explicit render requests and capture IO are visual/behavior evidence, not performance certification.";
            public int seed,errors;
            public bool independentAim,screenRelativeMovement,fixedHeading,cursorGroundTarget,placementDoesNotJump,cancelDoesNotDodge,dodgeInWorld,staffFacing;
            public float travelled,aimError,terrainObstruction;
            public Vector3 bank;
            public List<string> views=new List<string>();
        }
        IEnumerator IsometricReview(WorldSession session,string directory)
        {
            var report=new IsometricReport{seed=session.ActiveWorld.seed};
            m0Session=session;m0Data=(TopazSaveData)typeof(WorldSession).GetField("_data",M0Fields).GetValue(session);
            m0Hour=12;m0Look=FindAnyObjectByType<VisualLookController>();m0Camera=Camera.main.GetComponent<PlayerCamera>();
            m0InputConfigured=true;m0PreviousBackground=InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            m0Pad=InputSystem.AddDevice<Gamepad>();InputSystem.EnableDevice(m0Pad);
            var player=session.GetComponent<PlayerController>();var camera=Camera.main;
            m0Camera.SetZoom(30);
            yield return new WaitForSecondsRealtime(2);
            Quaternion orientation=camera.transform.rotation;Vector3 start=player.transform.position;
            var forward=Vector3.ProjectOnPlane(camera.transform.forward,Vector3.up).normalized;
            var right=Vector3.ProjectOnPlane(camera.transform.right,Vector3.up).normalized;
            InputSystem.QueueStateEvent(m0Pad,new GamepadState{leftStick=Vector2.up,rightStick=Vector2.right});
            yield return new WaitForSecondsRealtime(.35f);
            report.travelled=Vector3.Dot(player.transform.position-start,forward);
            report.screenRelativeMovement=report.travelled>.2f;
            report.independentAim=Vector3.Dot(player.AimDirection,right)>.95f;
            report.fixedHeading=Quaternion.Angle(orientation,camera.transform.rotation)<.01f;
            InputSystem.QueueStateEvent(m0Pad,new GamepadState());yield return new WaitForSecondsRealtime(.5f);
            var mouse=InputSystem.AddDevice<Mouse>();Vector3 desired=player.transform.position+right*2;
            desired.y=Topaz.Generation.WoodlandRegion.GroundHeight(desired);
            InputSystem.QueueStateEvent(mouse,new MouseState{position=(Vector2)camera.WorldToScreenPoint(desired),delta=new Vector2(20,0)});
            yield return new WaitForSecondsRealtime(.15f);
            report.aimError=Vector3.Distance(player.AimPointOnGround,desired);
            report.cursorGroundTarget=!player.UsingStickAim && player.HasAimSurface && report.aimError<.2f;
            if(session.BeginHomeEdit())
            {
                InputSystem.QueueStateEvent(m0Pad,new GamepadState().WithButton(GamepadButton.South));
                yield return new WaitForSecondsRealtime(.12f);
                report.placementDoesNotJump=!player.IsAirborne;
                InputSystem.QueueStateEvent(m0Pad,new GamepadState());yield return null;
                InputSystem.QueueStateEvent(m0Pad,new GamepadState().WithButton(GamepadButton.East));
                yield return new WaitForSecondsRealtime(.12f);
                report.cancelDoesNotDodge=!player.IsDodging && !session.IsBuilding;
                InputSystem.QueueStateEvent(m0Pad,new GamepadState());
            }
            yield return new WaitForSecondsRealtime(.1f);
            InputSystem.QueueStateEvent(m0Pad,new GamepadState().WithButton(GamepadButton.East));
            yield return new WaitForSecondsRealtime(.05f);
            report.dodgeInWorld=player.IsDodging && !session.MenuOpen;
            InputSystem.QueueStateEvent(m0Pad,new GamepadState());
            yield return new WaitForSecondsRealtime(.5f);
            for(int i=0;i<session.BackpackSlots.Count;i++)
                if(session.BackpackSlots[i].itemId=="gear.crypt.staff" && session.BackpackSlots[i].count>0)
                {
                    if(session.TryEquipFromBackpack(i))
                    {
                        InputSystem.QueueStateEvent(m0Pad,new GamepadState{rightStick=Vector2.right,rightTrigger=1});
                        yield return new WaitForSecondsRealtime(.12f);
                        var combat=session.GetComponent<PlayerCombat>();
                        report.staffFacing=combat.IsAttackLocked && Vector3.Dot(combat.LockedDirection,player.AimDirection)>.99f;
                        InputSystem.QueueStateEvent(m0Pad,new GamepadState());
                        yield return new WaitForSecondsRealtime(1.5f);
                    }
                    break;
                }
            SaveIsometricFrame(directory,"refuge-day.png");report.views.Add("refuge-day.png");
            // Capture a short ordinary departure with actual timestamps and fixed heading.
            string motion=Path.Combine(directory,"motion-isometric");Directory.CreateDirectory(motion);
            var timeline=new MotionReport{mode="isometric",width=alpineTarget.width,height=alpineTarget.height};
            float began=Time.realtimeSinceStartup,next=began;
            InputSystem.QueueStateEvent(m0Pad,new GamepadState{leftStick=Vector2.up});
            while(Time.realtimeSinceStartup-began<6)
            {
                yield return null;
                if(Time.realtimeSinceStartup<next)continue;next=Time.realtimeSinceStartup+1/20f;
                string name="frame-"+timeline.frames.Count.ToString("D4")+".png";
                SaveIsometricFrame(motion,name);
                timeline.frames.Add(new MotionFrame{file=name,seconds=Time.realtimeSinceStartup-began,player=player.transform.position,camera=camera.transform.position});
            }
            InputSystem.QueueStateEvent(m0Pad,new GamepadState());
            File.WriteAllText(Path.Combine(motion,"motion.json"),JsonUtility.ToJson(timeline,true));
            // The bank at this representative local route exercises terrain, tree and player visibility together.
            Vector3 bank=new Vector3(33,0,31);float obstruction=0;
            var plan=session.ActiveRegion.Wilderness;
            foreach(var route in plan.Routes)foreach(var point in route.Points)
            {
                if(new Vector2(point.x,point.z).sqrMagnitude>180*180 || plan.WaterDepth(point.x,point.z)>.05f)continue;
                Vector3 foot=point;foot.y=plan.Height(point.x,point.z);
                for(float t=2;t<28;t+=2)
                {
                    Vector3 rayPoint=foot+Vector3.up*1.3f-camera.transform.forward*t;
                    float score=plan.Height(rayPoint.x,rayPoint.z)-rayPoint.y;
                    if(score>obstruction){obstruction=score;bank=foot;}
                }
            }
            report.bank=bank;report.terrainObstruction=obstruction;
            yield return session.ActiveRegion.Streaming.PrepareDestination(bank);
            bank.y=Topaz.Generation.WoodlandRegion.GroundHeight(bank);MovePlayer(session,bank);
            yield return new WaitForSecondsRealtime(2);
            SaveIsometricFrame(directory,"bank-day.png");report.views.Add("bank-day.png");
            m0Hour=22;yield return new WaitForSecondsRealtime(2);
            SaveIsometricFrame(directory,"bank-night.png");report.views.Add("bank-night.png");
            report.errors=runtimeErrors;
            File.WriteAllText(Path.Combine(directory,"isometric.json"),JsonUtility.ToJson(report,true));
            StopAlpineRenderLoop();
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--topaz-smoke-quit")>=0)Application.Quit(report.errors==0?0:1);
        }
        void SaveIsometricFrame(string directory,string name)
        {
            var prior=RenderTexture.active;var texture=new Texture2D(alpineTarget.width,alpineTarget.height,TextureFormat.RGB24,false);
            try
            {
                RenderTexture.active=alpineTarget;texture.ReadPixels(new Rect(0,0,alpineTarget.width,alpineTarget.height),0,0);texture.Apply();
                File.WriteAllBytes(Path.Combine(directory,name),texture.EncodeToPNG());
            }
            finally{RenderTexture.active=prior;Destroy(texture);}
        }
    }
}
