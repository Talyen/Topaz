#ifndef TOPAZ_SHELTER_LIGHTING_INCLUDED
#define TOPAZ_SHELTER_LIGHTING_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/BRDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging3D.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GlobalIllumination.hlsl"
#include "TopazShelter.hlsl"

// Keep native material/SSAO occlusion. Shelter attenuates the ambient reflection,
// but SCGI already resolves diffuse visibility from actual geometry.
half3 TopazShelterGI(BRDFData brdf, BRDFData coat, float coatMask, half3 gi,
    half occlusion, float3 position, half3 normal, half3 view, float2 screenUV)
{
    half fill = TopazShelterAt(position).y;
    half3 color = GlobalIllumination(brdf, coat, coatMask, gi, occlusion * fill, position, normal, view, screenUV);
#if defined(_SCREEN_SPACE_IRRADIANCE) && !defined(_SURFACE_TYPE_TRANSPARENT)
    if (!IsOnlyAOLightingFeatureEnabled())
    {
        half3 diffuse = gi * brdf.diffuse;
        #if defined(_CLEARCOAT) || defined(_CLEARCOATMAP)
            half fresnel = kDielectricSpec.x + kDielectricSpec.a * Pow4(1.0 - saturate(dot(normal, view)));
            diffuse *= 1.0 - fresnel * coatMask;
        #endif
        color += diffuse * occlusion * (1.0h - fill);
    }
#endif
    return color;
}
#define GlobalIllumination TopazShelterGI
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#undef GlobalIllumination

half4 TopazShelterPBR(InputData inputData, SurfaceData surfaceData)
{
    return UniversalFragmentPBR(inputData, surfaceData);
}
half4 TopazShelterPBR(InputData inputData, half3 albedo, half metallic, half3 specular,
    half smoothness, half occlusion, half3 emission, half alpha)
{
    float2 shelter = TopazShelterAt(inputData.positionWS);
    // Terrain's current wet-layer response spans .04 to .38; shelter removes only that added wet gloss.
    smoothness = lerp(min(smoothness,.04h),smoothness,shelter.x);
    return UniversalFragmentPBR(inputData,albedo,metallic,specular,smoothness,occlusion,emission,alpha);
}
#define UniversalFragmentPBR TopazShelterPBR
#endif
