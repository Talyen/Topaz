#ifndef TOPAZ_SURFACE_INCLUDED
#define TOPAZ_SURFACE_INCLUDED
float4 _TopazWind;
float _TopazWetness;
void TopazSurface_float(float3 Position, float Dissolve, out float3 Moved, out float Smoothness, out float Alpha, out float3 Glow)
{
    float sway = sin(_TopazWind.w * 1.8 + Position.y * 1.4 + Position.x) * saturate(Position.y * 0.15) * 0.12;
    Moved = Position + float3(_TopazWind.x, 0, _TopazWind.z) * sway;
    Smoothness = lerp(0.2, 0.8, saturate(_TopazWetness));
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
#endif
