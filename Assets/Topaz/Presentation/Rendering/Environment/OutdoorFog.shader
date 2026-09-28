Shader "Topaz/Outdoor Fog"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Height Distance Fog"
            ZWrite Off ZTest Always Cull Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "TopazFog.hlsl"
            half4 Frag(Varyings input):SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float depth=SampleSceneDepth(input.texcoord);
                #if UNITY_REVERSED_Z
                // Only the cleared depth is sky. A near-zero epsilon exposes real terrain
                // in the final metres of a short far clip, after it should be fully fogged.
                if(depth<=0)return 0;
                #else
                if(depth>=1)return 0;
                depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
                #endif
                float3 world=ComputeWorldSpacePosition(input.texcoord,depth,UNITY_MATRIX_I_VP);
                return half4(TopazFogTarget(world),TopazFogAmount(world));
            }
            ENDHLSL
        }
    }
}
