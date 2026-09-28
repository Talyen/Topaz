Shader "Topaz/Outdoor Sky"
{
    Properties { _SkyColor("Sky",Color)=(.23,.46,.68,1) _HorizonColor("Horizon",Color)=(.72,.8,.78,1) }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
        Pass
        {
            Cull Off ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _SkyColor,_HorizonColor;
            CBUFFER_END
            #include "TopazSky.hlsl"
            struct Attributes{float4 positionOS:POSITION;};
            struct Varyings{float4 positionCS:SV_POSITION;float3 direction:TEXCOORD0;};
            Varyings Vert(Attributes input){Varyings o;o.positionCS=TransformObjectToHClip(input.positionOS.xyz);o.direction=input.positionOS.xyz;return o;}
            half4 Frag(Varyings input):SV_Target
            {return half4(TopazSkyColor(input.direction,_SkyColor.rgb,_HorizonColor.rgb),1);}
            ENDHLSL
        }
    }
}
