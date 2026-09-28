Shader "Topaz/Shallow Water"
{
    Properties { _ShallowColor("Shallows",Color)=(.18,.40,.35,1) _DeepColor("Depth",Color)=(.07,.22,.26,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "Forward Water"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "TopazFog.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _ShallowColor,_DeepColor;
            CBUFFER_END
            float4 _TopazWind;
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float depth:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input){Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.depth=input.uv.x;return o;}
            half4 Frag(Varyings input):SV_Target
            {
                float phase=_TopazWind.w;
                float3 normal=normalize(float3(sin(input.world.x*.7+phase*.6)*.035,1,cos(input.world.z*.85+phase*.4)*.035));
                Light light=GetMainLight();float3 view=GetWorldSpaceNormalizeViewDir(input.world);
                float3 baseColor=lerp(_ShallowColor.rgb,_DeepColor.rgb,saturate(input.depth*2));
                float3 color=baseColor*(max(SampleSH(normal),float3(.18,.18,.18))+light.color*saturate(dot(normal,light.direction))*.65);
                color+=light.color*pow(saturate(dot(normal,normalize(light.direction+view))),80)*.22;
                float fresnel=pow(1-saturate(dot(normal,view)),4);
                color=lerp(color,_TopazFogColor.rgb,.06+fresnel*.24);
                float shoreline=(1-smoothstep(.04,.24,input.depth))*smoothstep(0,.035,input.depth);
                float ripple=.5+.5*sin(input.depth*70-phase*.8+input.world.x*.2+input.world.z*.13);
                color=lerp(color,color+float3(.09,.12,.10),shoreline*ripple*.3);
                return half4(TopazFogColor(color,input.world),smoothstep(0,.12,input.depth)*(.65+fresnel*.15));
            }
            ENDHLSL
        }
    }
}
