using System;
using System.Collections;
using System.Linq;
using System.IO;
using System.Reflection;
using UnityEditor;
namespace Topaz.Generation.Editor
{
    public static partial class WorldShaderSetup
    {
        public static void ConfigureDistantGround()
        {
            string path=Root+"DistantGround.shadergraph";
            if(File.Exists(path))return;
            var graph=New("GraphData");
            string source=File.ReadAllText(UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.shadergraph").resolvedPath+"/GraphTemplates/Cross Pipeline/0_Lit Basic.shadergraph");
            T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,new[]{graph,source,null,(object)true});
            var targets=((IEnumerable)graph.GetType().GetProperty("activeTargets").GetValue(graph)).Cast<object>().ToArray();
            foreach(var target in targets)if(target.GetType().Name!="UniversalTarget")Call(graph,"SetTargetInactive",target,false);
            var color=New("VertexColorNode");Call(graph,"AddNode",color,true);
            var rough=New("Vector1Node");Call(graph,"AddNode",rough,true);
            var blocks=((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("BlockNode")).Invoke(graph,null)).Cast<object>();
            foreach(var block in blocks)
            {
                string name=(string)block.GetType().GetProperty("name").GetValue(block);
                if(name=="SurfaceDescription.BaseColor")Call(graph,"Connect",Ref(color,0),Ref(block,0));
                if(name=="SurfaceDescription.Smoothness")Call(graph,"Connect",Ref(rough,0),Ref(block,0));
            }
            File.WriteAllText(path,(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph}));AssetDatabase.ImportAsset(path);
        }
        public static void ConfigureOutdoorMotion()
        {
            string path=Root+"Foliage.shadergraph";var graph=New("GraphData");
            T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,new[]{graph,File.ReadAllText(path),null,(object)true});
            var targets=((IEnumerable)graph.GetType().GetProperty("activeTargets").GetValue(graph)).Cast<object>();
            foreach(var target in targets)
            {
                if(target.GetType().Name!="UniversalTarget")continue;
                var property=target.GetType().GetProperty("additionalMotionVectorMode");
                property.SetValue(target,Enum.Parse(property.PropertyType,"Custom"));
            }
            var nodes=((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("CustomFunctionNode")).Invoke(graph,null)).Cast<object>();
            var node=nodes.First(n=>((string)n.GetType().GetProperty("functionName").GetValue(n)).StartsWith("Topaz"));
            Set(node,"functionName","TopazFoliage");Slot(node,6,"Motion",true,true);
            var blocks=((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("BlockNode")).Invoke(graph,null)).Cast<object>();
            var block=blocks.FirstOrDefault(n=>(string)n.GetType().GetProperty("name").GetValue(n)=="VertexDescription.MotionVector");
            if(block==null)
            {
                var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalBlockFields")).First(t=>t!=null);
                var descriptor=type.GetNestedType("VertexDescription",BindingFlags.Public|BindingFlags.NonPublic).GetField("MotionVector").GetValue(null);
                block=New("BlockNode");Call(block,"Init",descriptor);
                var context=graph.GetType().GetProperty("vertexContext").GetValue(graph);Call(graph,"AddBlock",block,context,-1);
            }
            Call(graph,"Connect",Ref(node,6),Ref(block,0));
            File.WriteAllText(path,(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph}));AssetDatabase.ImportAsset(path);
        }
    }
}
