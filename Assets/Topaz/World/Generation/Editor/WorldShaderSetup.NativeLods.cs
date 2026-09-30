using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Topaz.Generation.Editor
{
    public static partial class WorldShaderSetup
    {
        // Generated private derivative: keep vendor sources unchanged and preserve their material vocabulary.
        public static Shader NativeFoliageLods(bool grass=false)
        {
            string path="Assets/Topaz/Presentation/Art/World/GroundCover/PNB "+(grass?"Grass":"Native")+" Fade.shadergraph";
            string fingerprint=FoliageSourceFingerprint()+" grass="+grass;
            if(File.Exists(path) && AssetImporter.GetAtPath(path)?.userData==fingerprint)
                return AssetDatabase.LoadAssetAtPath<Shader>(path);
            var graph=New("GraphData");
            T("Serialization.MultiJson").GetMethod("Deserialize").MakeGenericMethod(T("GraphData")).Invoke(null,
                new[]{graph,File.ReadAllText("Assets/Synty/PNB_Core/Shaders/Foliage.shadergraph"),null,(object)true});
            // All curated foliage disables these features. Literal constants let shader compilation
            // remove their texture/noise branches instead of evaluating uniform material toggles.
            var unused=new[]{"_Enable_Emission","_Enable_Frosting","_Enable_Pulse","_Enable_Back_Face_Lighting"};
            var properties=((IEnumerable)graph.GetType().GetProperty("properties").GetValue(graph)).Cast<object>().ToArray();
            foreach(var property in properties)
                if(unused.Contains((string)property.GetType().GetProperty("referenceName").GetValue(property)) ||
                    (grass && new[]{"_Enable_Trunk_Normal","_Leaf_Flat_Color","_Use_Color_Noise"}.Contains((string)property.GetType().GetProperty("referenceName").GetValue(property))))
                {
                    string reference=(string)property.GetType().GetProperty("referenceName").GetValue(property);
                    bool value=grass && (reference=="_Leaf_Flat_Color" || reference=="_Use_Color_Noise");
                    Set(property,"value",property.GetType().GetProperty("value").PropertyType==typeof(bool)?(object)value:(value?1f:0f));
                    Call(graph,"RemoveGraphInput",property);
                }
            foreach(var target in ((IEnumerable)graph.GetType().GetProperty("activeTargets").GetValue(graph)).Cast<object>())
                if(target.GetType().Name=="UniversalTarget")
                {
                    Set(target,"supportsLodCrossFade",!grass);
                    var mode=target.GetType().GetProperty("additionalMotionVectorMode");
                    mode.SetValue(target,System.Enum.Parse(mode.PropertyType,grass?"Custom":"TimeBased"));
                }
            var nodes=(IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("CustomFunctionNode")).Invoke(graph,null);
            foreach(var node in nodes.Cast<object>().ToArray())
                if((string)node.GetType().GetProperty("functionName").GetValue(node)=="getLODCrossFade")
                {
                    // Native signed dither handles tree LODs; the native detail system needs a separate distance fade.
                    Set(node,"functionName","TopazFoliageFade");
                    Set(node,"sourceType",System.Enum.Parse(T("Drawing.HlslSourceType"),"File"));
                    Set(node,"functionSource",AssetDatabase.AssetPathToGUID(Root+"TopazGrassFade.hlsl"));
                    Slot(node,1,"GroundCover",false,false);
                    Slot(node,2,"World",false,true);
                    var world=New("PositionNode");Call(graph,"AddNode",world,true);
                    Call(graph,"Connect",Ref(world,0),Ref(node,2));
                    var property=New("Internal.Vector1ShaderProperty");Set(property,"displayName","Topaz Ground Cover");
                    Set(property,"overrideReferenceName","_TopazGroundCover");Call(graph,"AddGraphInput",property,-1);
                    var propNode=New("PropertyNode");Call(graph,"AddNode",propNode,true);Set(propNode,"property",property);
                    Call(graph,"Connect",Ref(propNode,0),Ref(node,1));
                }
            if(grass)ConfigureGrassMotion(graph);
            var json=(string)T("Serialization.MultiJson").GetMethod("Serialize").Invoke(null,new[]{graph});
            if(!File.Exists(path)||File.ReadAllText(path)!=json)
            {File.WriteAllText(path,json);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);}
            var importer=AssetImporter.GetAtPath(path);importer.userData=fingerprint;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Shader>(path);
        }
        static void ConfigureGrassMotion(object graph)
        {
            var node=New("CustomFunctionNode");Set(node,"functionName","TopazGrassMotion");
            Set(node,"sourceType",System.Enum.Parse(T("Drawing.HlslSourceType"),"File"));
            Set(node,"functionSource",AssetDatabase.AssetPathToGUID(Root+"TopazSurface.hlsl"));
            Slot(node,0,"Position",false,true);Slot(node,1,"Moved",true,true);Slot(node,2,"Motion",true,true);Call(graph,"AddNode",node,true);
            var position=New("PositionNode");Call(graph,"AddNode",position,true);
            Set(position,"spacePopup",System.Activator.CreateInstance(T("Drawing.Controls.PopupList"),new object[]{new[]{"Object","View","World","Tangent","Absolute World"},0}));
            Call(graph,"Connect",Ref(position,0),Ref(node,0));
            var blocks=((IEnumerable)graph.GetType().GetMethod("GetNodes").MakeGenericMethod(T("BlockNode")).Invoke(graph,null)).Cast<object>().ToArray();
            var vertex=blocks.First(b=>(string)b.GetType().GetProperty("name").GetValue(b)=="VertexDescription.Position");
            Call(graph,"Connect",Ref(node,1),Ref(vertex,0));
            var type=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UnityEditor.Rendering.Universal.ShaderGraph.UniversalBlockFields")).First(t=>t!=null);
            var descriptor=type.GetNestedType("VertexDescription",BindingFlags.Public|BindingFlags.NonPublic).GetField("MotionVector").GetValue(null);
            var motion=New("BlockNode");Call(motion,"Init",descriptor);
            Call(graph,"AddBlock",motion,graph.GetType().GetProperty("vertexContext").GetValue(graph),-1);
            Call(graph,"Connect",Ref(node,2),Ref(motion,0));
        }
        static string FoliageSourceFingerprint()
        {
            using var hash=System.Security.Cryptography.SHA256.Create();
            string source=File.ReadAllText("Assets/Synty/PNB_Core/Shaders/Foliage.shadergraph")+
                File.ReadAllText("Assets/Topaz/World/Generation/Editor/WorldShaderSetup.NativeLods.cs");
            return "Topaz foliage source "+System.BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(source)));
        }
    }
}
