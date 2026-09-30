using UnityEngine;

namespace Topaz
{
    /// <summary>Coherent review recipes; never loaded by normal gameplay.</summary>
    public sealed class VisualLabLook
    {
        public string Name;
        public int Shading;
        public Color Sun,Ambient,Shadow,Tint,Ground,Rock,Water;
        public float Light,Wrap,Bands,Contrast,Saturation,Exposure,Bloom,Softness;
        public bool Gi,DoF;
        public Vector3 Angle;
        public static VisualLabLook[] All => new[]
        {
            Make("01 Clean faceted",0,"FFF0D0","A3B4CB","8799B1","FFFFFF",1.6f,.1f,4,4,2,0,.05f,false),
            Make("02 Matte adventure",1,"FFE7B0","9AA9B8","687B94","FFF6DB",1.3f,.4f,4,7,8,.1f,.08f,false),
            Make("03 Luminous painterly",1,"FFD58E","849EC0","596F9C","FFF4D6",2.2f,.55f,4,10,15,.1f,.22f,true),
            Make("04 Soft storybook",2,"FFE3B3","B9C2C0","879BBA","FFF5E4",1.4f,.7f,5,3,9,.2f,.12f,false),
            Make("05 Bold cel fantasy",2,"FFD887","A3BDDA","4B6890","FFFFFF",2,.15f,3,14,20,.1f,.07f,false),
            Make("06 Ink and color",2,"FFE6B3","89A0B7","344D71","EEDFD0",1.7f,.05f,2,20,-6,.1f,.03f,false),
            Make("07 Moody woodland",0,"DADAB3","667C91","364B62","CDDDC9",1.2f,.15f,4,13,-8,0,.1f,true),
            Make("08 Golden cinematic",0,"FFC279","7D97B4","485E83","FFE9CC",2.5f,.25f,4,13,5,.15f,.3f,true),
            Make("09 Miniature diorama",1,"FFEFD0","AABFD1","758DB3","FFF4DF",1.8f,.6f,4,8,4,.1f,.13f,true),
            Make("10 Faceted illustration",3,"FFE8B3","A7B5C0","677D91","EEE9CF",1.6f,.4f,4,6,-2,.15f,.03f,false),
            Make("11 Lush enchanted",1,"FFF0B6","90BAC0","527C91","F0FFDE",2,.65f,4,6,24,.1f,.25f,true),
            Make("12 Silver fairy tale",2,"CDE5FF","879ABD","48558B","DEDDF0",1.5f,.5f,4,8,3,.15f,.24f,true)
        };
        static Color Hex(string value){ColorUtility.TryParseHtmlString("#"+value,out var c);return c;}
        static VisualLabLook Make(string name,int shading,string sun,string ambient,string shadow,string tint,float light,float wrap,float bands,float contrast,float saturation,float exposure,float bloom,bool gi)
        {
            return new VisualLabLook {Name=name,Shading=shading,Sun=Hex(sun),Ambient=Hex(ambient),Shadow=Hex(shadow),Tint=Hex(tint),Light=light,Wrap=wrap,Bands=bands,Contrast=contrast,Saturation=saturation,Exposure=exposure,Bloom=bloom,Gi=gi,
                Ground=Hex(shading==3?"718C60":name.StartsWith("07")?"506A56":"789363"),Rock=Hex(name.StartsWith("12")?"8B90AA":"929284"),Water=Hex(name.StartsWith("07")?"304C5D":"398B91"),
                Angle=name.StartsWith("08")?new Vector3(24,-35,0):name.StartsWith("12")?new Vector3(42,125,0):new Vector3(48,-35,0),DoF=name.StartsWith("09"),Softness=name.StartsWith("06")?.2f:.7f};
        }
    }
}
