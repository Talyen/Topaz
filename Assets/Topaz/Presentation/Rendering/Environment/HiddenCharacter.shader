Shader "Topaz/Hidden Character"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            Name "Hidden Character"
            ZWrite Off ZTest Greater Cull Back Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; UNITY_VERTEX_OUTPUT_STEREO };
            float4 _TopazSilhouetteSubjects[8];
            int _TopazSilhouetteCount;
            Varyings Vert(Attributes input)
            {
                UNITY_SETUP_INSTANCE_ID(input);Varyings output;UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.world=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.world);return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float shown=0;float player=0;
                for(int i=0;i<_TopazSilhouetteCount;i++)
                {
                    float3 delta=input.world-_TopazSilhouetteSubjects[i].xyz;
                    float eligible=step(length(delta),_TopazSilhouetteSubjects[i].w);
                    shown=max(shown,eligible);if(i==0)player=eligible;
                }
                clip(shown-.5);
                return half4(lerp(half3(.9,.36,.18),half3(.95,.82,.48),player),.45);
            }
            ENDHLSL
        }
    }
}
