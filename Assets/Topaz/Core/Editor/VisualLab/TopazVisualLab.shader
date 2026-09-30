Shader "Topaz/Review/Visual Lab"
{
    Properties
    {
        _BaseMap("Palette",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _ShadowColor("Shadow color",Color)=(.35,.45,.65,1)
        _Wrap("Light wrap",Range(0,1))=.4
        _Bands("Shading bands",Float)=4
        _Mode("Shading model",Float)=1
        _VertexColor("Ground color",Float)=0
        _Gloss("Highlight",Float)=.08
        _GroundTextures("Painted review atlas",2D)="white"{}
        _PaintedGround("Painted ground/rock",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            TEXTURE2D(_GroundTextures);SAMPLER(sampler_GroundTextures);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST,_BaseColor,_ShadowColor;
            float _Wrap,_Bands,_Mode,_VertexColor,_Gloss,_PaintedGround;
            CBUFFER_END
            struct A {float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float4 c:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct V {float4 p:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float2 uv:TEXCOORD2;float4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            V Vert(A i)
            {UNITY_SETUP_INSTANCE_ID(i);V o;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(i.n);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.color=i.c;return o;}
            half3 Tile(float2 uv,float2 quadrant)
            {return SAMPLE_TEXTURE2D(_GroundTextures,sampler_GroundTextures,quadrant+(frac(uv)*.496+.002)).rgb;}
            half4 Frag(V i):SV_Target
            {
                half3 base=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
                base*=lerp(half3(1,1,1),i.color.rgb,_VertexColor);
                if(_PaintedGround>.5)
                {
                    float3 n=normalize(i.normal);float3 w=pow(abs(n),4);w/=max(.001,w.x+w.y+w.z);
                    half3 rock=Tile(i.world.yz*.2,float2(0,0))*w.x+Tile(i.world.xz*.2,float2(0,0))*w.y+Tile(i.world.xy*.2,float2(0,0))*w.z;
                    if(_PaintedGround>1.5)base=rock;
                    else
                    {
                        float path=min(abs(i.world.z+4-sin(i.world.x*.16)*1.8),abs(i.world.x-4-sin(i.world.z*.15)*1.5));
                        float bank=abs(i.world.x+9+sin(i.world.z*.13)*1.2);
                        half3 moss=Tile(i.world.xz*.2,float2(0,.5));half3 dirt=Tile(i.world.xz*.2,float2(.5,.5));half3 shore=Tile(i.world.xz*.2,float2(.5,0));
                        base=lerp(dirt,moss,smoothstep(1.2,2.7,path));base=lerp(shore,base,smoothstep(3,5.8,bank));base=lerp(rock,base,smoothstep(.45,.9,n.y));
                    }
                }
                Light light=GetMainLight(TransformWorldToShadowCoord(i.world));
                float3 n=normalize(i.normal);float diffuse=saturate((dot(n,light.direction)+_Wrap)/(1+_Wrap));
                if(_Mode>1.5 && _Mode<2.5)diffuse=floor(diffuse*max(1,_Bands-1)+.5)/max(1,_Bands-1);
                if(_Mode>2.5)diffuse=lerp(.32,1,saturate(n.y*.55+n.x*.2+n.z*.15+.35));
                float shade=diffuse*lerp(.3,1,light.shadowAttenuation);
                half3 ambient=SampleSH(n);
                half3 color=base*(lerp(_ShadowColor.rgb,half3(1,1,1),shade)*(.45+light.color*.55)+ambient*.25);
                float3 view=normalize(GetWorldSpaceViewDir(i.world));float spec=pow(saturate(dot(n,normalize(light.direction+view))),32)*_Gloss*light.shadowAttenuation;
                return half4(color+light.color*spec,1);
            }
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
}
