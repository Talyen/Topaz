using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Topaz.Generation.Editor
{
    /// <summary>Author standard Shader Graph assets through the installed 17.6 graph model.</summary>
    public static class WorldShaderSetup
    {
        const string Root="Assets/Topaz/Presentation/Rendering/Environment/";
        static Type T(string name)=>AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.ShaderGraph."+name)).First(t=>t!=null);
        static object New(string name)=>Activator.CreateInstance(T(name),true);
        static object Call(object obj,string name,params object[] args)=>obj.GetType().GetMethods().First(m=>m.Name==name&&m.GetParameters().Length==args.Length&&!m.IsGenericMethod).Invoke(obj,args);
        static void Set(object obj,string name,object value)=>obj.GetType().GetProperty(name,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance).SetValue(obj,value);
        static object Ref(object node,int slot)=>Call(node,"GetSlotReference",slot);
        static void Slot(object node,int id,string name,bool output,bool vector)
        {
            var type=T(vector?"Vector3MaterialSlot":"Vector1MaterialSlot");
            var ctor=type.GetConstructors().First(c=>c.GetParameters().Length>5);
            var parameters=ctor.GetParameters();var args=new object[parameters.Length];
            args[0]=id;args[1]=name;args[2]=name;args[3]=Enum.Parse(parameters[3].ParameterType,output?"Output":"Input");args[4]=vector?(object)Vector3.zero:0f;
            for(int i=5;i<args.Length;i++)args[i]=parameters[i].DefaultValue;
            Call(node,"AddSlot",ctor.Invoke(args),true,true);
        }
        public static void ConfigureFoliageObjectSpace()
        {
            string path=Root+"Foliage.shadergraph";
            var graph=New("GraphData");
            T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,new[]{graph,File.ReadAllText(path),null,(object)true});
            var nodes=(IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("PositionNode")).Invoke(graph,null);
            foreach(var node in nodes)Set(node,"spacePopup",Activator.CreateInstance(T("Drawing.Controls.PopupList"),new object[]{new[]{"Object","View","World","Tangent","Absolute World"},0}));
            string json=(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph});
            File.WriteAllText(path,json);AssetDatabase.ImportAsset(path);
        }
        [MenuItem("Topaz/Migration/Configure Shader Graph Templates")]
        public static void Apply()
        {
            string source=File.ReadAllText(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.shadergraph").resolvedPath+"/GraphTemplates/Cross Pipeline/0_Lit Basic.shadergraph");
            foreach(string name in new[]{"Foliage","Wet Surface","Emission Dissolve"})
            {
                var graph=New("GraphData");
                T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,new[]{graph,source,null,(object)true});
                var targets=((IEnumerable)graph.GetType().GetProperty("activeTargets").GetValue(graph)).Cast<object>().ToArray();
                foreach(var target in targets)if(!target.GetType().FullName.Contains("UniversalTarget"))Call(graph,"SetTargetInactive",target,false);
                foreach(var target in targets)if(target.GetType().Name=="UniversalTarget")Set(target,"alphaClip",true);
                var node=New("CustomFunctionNode");Set(node,"functionName","TopazSurface");Set(node,"sourceType",Enum.Parse(T("Drawing.HlslSourceType"),"File"));Set(node,"functionSource",AssetDatabase.AssetPathToGUID(Root+"TopazSurface.hlsl"));
                Slot(node,0,"Position",false,true);Slot(node,1,"Moved",true,true);Slot(node,2,"Smoothness",true,false);Slot(node,3,"Alpha",true,false);Slot(node,4,"Glow",true,true);Slot(node,5,"Dissolve",false,false);
                Call(graph,"AddNode",node,true);
                var position=New("PositionNode");Call(graph,"AddNode",position,true);Call(graph,"Connect",Ref(position,0),Ref(node,0));
                var property=New("Internal.Vector1ShaderProperty");Set(property,"displayName","Dissolve");Set(property,"overrideReferenceName","_TopazDissolve");Call(graph,"AddGraphInput",property,-1);
                var propNode=New("PropertyNode");Call(graph,"AddNode",propNode,true);Set(propNode,"property",property);Call(graph,"Connect",Ref(propNode,0),Ref(node,5));
                var nodes=(IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("BlockNode")).Invoke(graph,null);
                foreach(var block in nodes)
                {
                    string label=(string)block.GetType().GetProperty("name").GetValue(block);
                    int slot=label=="VertexDescription.Position"&&name=="Foliage"?1:label=="SurfaceDescription.Smoothness"?2:label=="SurfaceDescription.Alpha"?3:label=="SurfaceDescription.Emission"&&name=="Emission Dissolve"?4:-1;
                    if(slot>=0)Call(graph,"Connect",Ref(node,slot),Ref(block,0));
                }
                string json=(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph});
                string path=Root+name+".shadergraph";File.WriteAllText(path,json);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var material=new Material(AssetDatabase.LoadAssetAtPath<Shader>(path));AssetDatabase.CreateAsset(material,Root+name+" Template.mat");
            }
            AssetDatabase.SaveAssets();
        }
    }
}
