#ifndef TOPAZ_SHELTER_INCLUDED
#define TOPAZ_SHELTER_INCLUDED
TEXTURE3D(_TopazShelter);
SAMPLER(sampler_TopazShelter);
float4 _TopazShelterOrigin;
float4 _TopazShelterSize;
float2 TopazShelterAt(float3 world)
{
    if (_TopazShelterOrigin.w < .5) return float2(1,1);
    float3 uv = (world - _TopazShelterOrigin.xyz + _TopazShelterSize.www * .5) / _TopazShelterSize.xyz;
    if (any(uv < 0) || any(uv > 1)) return float2(1,1);
    float edge = saturate(min(min(uv.x,1-uv.x),min(uv.z,1-uv.z))*16);
    return lerp(float2(1,1), SAMPLE_TEXTURE3D_LOD(_TopazShelter,sampler_TopazShelter,uv,0).rg,edge);
}
void TopazShelter_float(float3 World, out float Exposure, out float Fill)
{
    float2 sample = TopazShelterAt(World); Exposure=sample.x;
    // Lit graphs must not multiply already visibility-resolved diffuse GI by sky visibility.
    #if defined(_SCREEN_SPACE_IRRADIANCE) && !defined(_SURFACE_TYPE_TRANSPARENT)
        Fill=1;
    #else
        Fill=sample.y;
    #endif
}
void TopazShelter_half(half3 World, out half Exposure, out half Fill)
{ float e,f; TopazShelter_float(World,e,f); Exposure=e; Fill=f; }
#endif
