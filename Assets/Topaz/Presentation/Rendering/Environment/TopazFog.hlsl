#ifndef TOPAZ_FOG_INCLUDED
#define TOPAZ_FOG_INCLUDED
float4 _TopazFogParameters; // density, height falloff, base height, clear foreground
#include "TopazSky.hlsl"
float TopazFogAmount(float3 worldPosition)
{
    float3 cameraPosition=GetCameraPositionWS();
    float distanceToCamera=length(worldPosition-cameraPosition);
    float a=clamp(-(cameraPosition.y-_TopazFogParameters.z)*_TopazFogParameters.y,-4.0,4.0);
    float b=clamp(-(worldPosition.y-_TopazFogParameters.z)*_TopazFogParameters.y,-4.0,4.0);
    float difference=b-a;
    float average=abs(difference)<.001?exp(a):(exp(b)-exp(a))/difference;
    float heightFog=saturate(1-exp(-_TopazFogParameters.x*max(0,distanceToCamera-_TopazFogParameters.w)*average));
    float horizonFog=_TopazVisibility.z>0?smoothstep(_TopazVisibility.x,_TopazVisibility.y,distanceToCamera):0;
    return 1-(1-heightFog)*(1-horizonFog);
}
float3 TopazFogTarget(float3 worldPosition)
{
    float3 direction=worldPosition-GetCameraPositionWS();
    float horizon=_TopazVisibility.z>0?smoothstep(_TopazVisibility.x,_TopazVisibility.y,length(direction)):0;
    if(horizon<=0)return _TopazFogColor.rgb;
    // Full haze converges to the actual sky along this ray, so a shorter clip distance leaves no skyline seam.
    return lerp(_TopazFogColor.rgb,TopazSkyColor(direction,_TopazSkyZenith.rgb,_TopazSkyHorizon.rgb),horizon);
}
float3 TopazFogColor(float3 color,float3 worldPosition)
{ return lerp(color,TopazFogTarget(worldPosition),TopazFogAmount(worldPosition)); }
void TopazRain_float(float3 Position,float3 UV,out float3 Color,out float Alpha)
{
    Color=TopazFogColor(float3(.48,.59,.67),Position);
    Alpha=saturate(sin(UV.x*3.14159)*sin(UV.y*3.14159))*.3;
}
void TopazRain_half(half3 Position,half3 UV,out half3 Color,out half Alpha)
{float3 c;float a;TopazRain_float(Position,UV,c,a);Color=c;Alpha=a;}
#endif
