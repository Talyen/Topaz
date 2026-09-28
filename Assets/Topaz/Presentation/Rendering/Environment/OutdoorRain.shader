Shader "Topaz/Outdoor Rain"
{
    Properties { _BaseMap("Streak",2D)="white"{} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "TopazFog.hlsl"
            #include "TopazShelter.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            CBUFFER_END
            struct Attributes{float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Varyings{float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;half4 color:COLOR;UNITY_VERTEX_OUTPUT_STEREO};
            Varyings Vert(Attributes input){Varyings o;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=TransformObjectToWorld(input.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);o.uv=TRANSFORM_TEX(input.uv,_BaseMap);o.color=input.color;return o;}
            half4 Frag(Varyings input):SV_Target
            {
                half4 color=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,input.uv)*input.color;
                color.a *= smoothstep(.5,.95,TopazShelterAt(input.world).x);
                color.rgb=TopazFogColor(color.rgb,input.world);return color;
            }
            ENDHLSL
        }
    }
}
