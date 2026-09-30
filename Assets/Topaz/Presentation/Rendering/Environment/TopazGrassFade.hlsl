#ifndef TOPAZ_GRASS_FADE_INCLUDED
#define TOPAZ_GRASS_FADE_INCLUDED
#include "TopazCutaway.hlsl"
float4 _TopazGrassDistance; // near density fade, fully hidden before native detail culling
void TopazFoliageFade_float(float GroundCover,float3 World,out float CrossFade)
{
    #if defined(SHADERPASS) && (SHADERPASS == SHADERPASS_FORWARD || SHADERPASS == SHADERPASS_DEPTHONLY || SHADERPASS == SHADERPASS_DEPTHNORMALS || SHADERPASS == SHADERPASS_MOTION_VECTORS)
    TopazClipWorld(World);
    #endif
    CrossFade=1;
    if(GroundCover>.5 && _TopazGrassDistance.y>_TopazGrassDistance.x)
    {
        float3 anchor=TransformObjectToWorld(float3(0,0,0));
        float distanceToCamera=distance(anchor,GetCameraPositionWS());
        float density=1-smoothstep(_TopazGrassDistance.x,_TopazGrassDistance.y,distanceToCamera);
        // Stable per-clump ordering spreads disappearance over distance without screen-space sparkle.
        float order=frac(sin(dot(floor(anchor.xz*16),float2(12.9898,78.233)))*43758.5453);
        CrossFade=saturate((density-order)*8/max(.001,1-order));
    }
}
void TopazFoliageFade_half(half GroundCover,half3 World,out half CrossFade)
{float fade;TopazFoliageFade_float(GroundCover,World,fade);CrossFade=fade;}
#endif
