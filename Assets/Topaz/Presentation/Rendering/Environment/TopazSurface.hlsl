#ifndef TOPAZ_SURFACE_INCLUDED
#define TOPAZ_SURFACE_INCLUDED
#include "TopazShelter.hlsl"
float4 _TopazWind;
float4 _TopazPreviousWind;
float3 TopazBend(float3 p,float4 wind)
{
    float mask=saturate(p.y*.4);
    float3 anchor=TransformObjectToWorld(float3(0,0,0));
    float phase=dot(anchor.xz,float2(.17,.23));
    float primary=sin(wind.w*1.2+p.x*.7+p.z*.4+phase)*mask*.16;
    float secondary=sin(wind.w*2.6+p.y*1.3+phase)*mask*mask*.035;
    return p+TransformWorldToObjectDir(float3(wind.x,0,wind.z),false)*(primary+secondary);
}
float _TopazWetness;
void TopazSurface_float(float3 Position, float Dissolve, out float3 Moved, out float Smoothness, out float Alpha, out float3 Glow)
{
    Moved = TopazBend(Position,_TopazWind);
    Smoothness = lerp(0.2, 0.55, saturate(_TopazWetness) * TopazShelterAt(Position).x);
    float pattern = frac(sin(dot(Position, float3(12.9898, 78.233, 35.719))) * 43758.5453);
    Alpha = step(saturate(Dissolve), pattern);
    Glow = float3(2, 0.5, 0.05) * (1 - smoothstep(0, 0.08, abs(pattern - Dissolve))) * step(0.001, Dissolve);
}
void TopazSurface_half(half3 Position, half Dissolve, out half3 Moved, out half Smoothness, out half Alpha, out half3 Glow)
{
    float3 m, g; float s,a;
    TopazSurface_float(Position,Dissolve,m,s,a,g);
    Moved=m; Smoothness=s; Alpha=a; Glow=g;
}
void TopazFoliage_float(float3 Position,float Dissolve,out float3 Moved,out float Smoothness,out float Alpha,out float3 Glow,out float3 Motion)
{
    TopazSurface_float(Position,Dissolve,Moved,Smoothness,Alpha,Glow);
    Smoothness=lerp(.15,.38,saturate(_TopazWetness) * TopazShelterAt(TransformObjectToWorld(Position)).x);
    Motion=Moved-TopazBend(Position,_TopazPreviousWind);
}
void TopazFoliage_half(half3 Position,half Dissolve,out half3 Moved,out half Smoothness,out half Alpha,out half3 Glow,out half3 Motion)
{
    float3 m,g,v;float s,a;TopazFoliage_float(Position,Dissolve,m,s,a,g,v);Moved=m;Smoothness=s;Alpha=a;Glow=g;Motion=v;
}
float3 TopazGrassBend(float3 p,float4 wind)
{
    float3 anchor=TransformObjectToWorld(float3(0,0,0));
    float phase=dot(anchor.xz,float2(.21,.16));
    float root=saturate(p.y);root*=root;
    float sway=(sin(wind.w*1.15+phase)*.18+sin(wind.w*2.6+phase*1.7)*.035)*root;
    return p+TransformWorldToObjectDir(float3(wind.x,0,wind.z),false)*sway;
}
void TopazGrassMotion_float(float3 Position,out float3 Moved,out float3 Motion)
{
    Moved=TopazGrassBend(Position,_TopazWind);
    Motion=Moved-TopazGrassBend(Position,_TopazPreviousWind);
}
void TopazGrassMotion_half(half3 Position,out half3 Moved,out half3 Motion)
{float3 m,v;TopazGrassMotion_float(Position,m,v);Moved=m;Motion=v;}
#endif
