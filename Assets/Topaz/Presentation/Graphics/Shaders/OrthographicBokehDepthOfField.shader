// Adapts Unity URP's BokehDepthOfField CoC pass for orthographic cameras.
// Unity Technologies ApS; Unity Companion License:
// https://unity.com/legal/licenses/unity-companion-license
Shader "Hidden/Topaz/OrthographicBokehDepthOfField"
{
    HLSLINCLUDE
        #pragma target 4.5

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

        half4 _SourceSize;
        half4 _CoCParams;

        half FragCoC(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float rawDepth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _SourceSize.xy * uv).x;
            float eyeDepth = unity_OrthoParams.w > 0.5
                ? LinearDepthToEyeDepth(rawDepth)
                : LinearEyeDepth(rawDepth, _ZBufferParams);

            half coc = (1.0 - _CoCParams.x / eyeDepth) * _CoCParams.y;
            half nearCoC = clamp(coc, -1.0, 0.0);
            half farCoC = saturate(coc);
            // URP stores Bokeh CoC in R8. Break up coherent quantization bands
            // on Topaz's broad flat ground before the final camera dither.
            float dither = (InterleavedGradientNoise(input.positionCS.xy, 0) - 0.5) / 255.0;
            return saturate((farCoC + nearCoC + 1.0) * 0.5 + dither);
        }
    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bokeh Depth Of Field CoC"
            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCoC
            ENDHLSL
        }

        UsePass "Hidden/Universal Render Pipeline/BokehDepthOfField/BOKEH DEPTH OF FIELD PREFILTER"
        UsePass "Hidden/Universal Render Pipeline/BokehDepthOfField/BOKEH DEPTH OF FIELD BLUR"
        UsePass "Hidden/Universal Render Pipeline/BokehDepthOfField/BOKEH DEPTH OF FIELD POST BLUR"
        UsePass "Hidden/Universal Render Pipeline/BokehDepthOfField/BOKEH DEPTH OF FIELD COMPOSITE"
    }
}
