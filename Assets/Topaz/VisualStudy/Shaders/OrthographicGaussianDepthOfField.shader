Shader "Hidden/Topaz/OrthographicGaussianDepthOfField"
{
    HLSLINCLUDE
        #pragma target 3.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float4 _SourceSize;
        float3 _CoCParams;

        half FragCoC(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float rawDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _SourceSize.xy * uv).x;

            // URP's Gaussian CoC pass uses perspective depth conversion for every camera.
            // Orthographic depth is already linear, so use URP's own conversion helper.
            float eyeDepth = unity_OrthoParams.w > 0.5
                ? LinearDepthToEyeDepth(rawDepth)
                : LinearEyeDepth(rawDepth, _ZBufferParams);
            return saturate((eyeDepth - _CoCParams.x) / max(_CoCParams.y - _CoCParams.x, 0.0001));
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Gaussian Depth Of Field CoC"
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCoC
            ENDHLSL
        }

        UsePass "Hidden/Universal Render Pipeline/GaussianDepthOfField/GAUSSIAN DEPTH OF FIELD PREFILTER"
        UsePass "Hidden/Universal Render Pipeline/GaussianDepthOfField/GAUSSIAN DEPTH OF FIELD BLUR HORIZONTAL"
        UsePass "Hidden/Universal Render Pipeline/GaussianDepthOfField/GAUSSIAN DEPTH OF FIELD BLUR VERTICAL"
        UsePass "Hidden/Universal Render Pipeline/GaussianDepthOfField/GAUSSIAN DEPTH OF FIELD COMPOSITE"
    }
}
