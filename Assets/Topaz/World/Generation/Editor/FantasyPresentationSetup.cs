using UnityEditor;
using UnityEngine;

namespace Topaz.Generation.Editor
{
    /// <summary>Authors the color LUT used by the current outdoor presentation.</summary>
    public static class FantasyPresentationSetup
    {
        public static Texture2D CreateFantasyLut()
        {
            const int size=32;
            const string path="Assets/Topaz/Presentation/Effects/Resources/TopazFantasyLut.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(texture==null){texture=new Texture2D(size*size,size,TextureFormat.RGBAHalf,false,true){name="Topaz Fantasy LUT"};AssetDatabase.CreateAsset(texture,path);}
            var pixels=new Color[size*size*size];
            for(int b=0;b<size;b++)for(int g=0;g<size;g++)for(int r=0;r<size;r++)
            {
                var c=new Vector3(r,g,b)/(size-1f);float l=Vector3.Dot(c,new Vector3(.2126f,.7152f,.0722f));
                float shadow=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.04f,.55f,l)))*Mathf.SmoothStep(0,1,l/.08f);
                float highlight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,l))*(1-l);
                // Gently cool the toe and warm highlights while preserving black, white and luminance order.
                c+=new Vector3(-.012f,.009f,.023f)*shadow+new Vector3(.08f,.025f,-.045f)*highlight;
                // Separate yellow-green vegetation from warm earth; protect neutral colors and skin reds.
                float green=Mathf.Clamp01((c.y-Mathf.Max(c.x,c.z))*.9f);
                c.x-=green*.025f;c.z+=green*.035f;
                pixels[g*size*size+b*size+r]=new Color(Mathf.Clamp01(c.x),Mathf.Clamp01(c.y),Mathf.Clamp01(c.z),1);
            }
            texture.SetPixels(pixels);texture.Apply(false,false);texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            EditorUtility.SetDirty(texture);return texture;
        }
    }
}
