#ifndef TOPAZ_CUTAWAY_INCLUDED
#define TOPAZ_CUTAWAY_INCLUDED
float4 _TopazReveal;
float4 _TopazTargetReveal;
float4 _TopazRevealDirection;
float _TopazRevealEnabled;
float TopazRevealMask(float3 world,float4 endpoint)
{
    float3 delta=world-endpoint.xyz;
    float depth=dot(delta,_TopazRevealDirection.xyz);
    float radial=length(delta-_TopazRevealDirection.xyz*depth);
    // Preserve supporting surfaces and geometry behind the playable endpoint.
    float front=1-smoothstep(-1,-.6,depth);
    float above=smoothstep(.12,.35,world.y-endpoint.y);
    return (1-smoothstep(endpoint.w,endpoint.w+.45,radial))*front*above*step(.01,endpoint.w);
}
void TopazClipWorld(float3 world)
{
    if((GetMeshRenderingLayer() & 0x80000000u)!=0)return;
    float reveal=max(TopazRevealMask(world,_TopazReveal),TopazRevealMask(world,_TopazTargetReveal))*_TopazRevealEnabled;
    // Stable world-space dissolve shared by color/depth/normals/motion; no frame-random sparkle.
    float order=frac(sin(dot(floor(world*48),float3(12.9898,78.233,37.719)))*43758.5453);
    clip(1-reveal-max(.001,order));
}
void TopazClipScreen(float4 positionCS)
{
    float depth=positionCS.z;
    #if !UNITY_REVERSED_Z
    depth=lerp(UNITY_NEAR_CLIP_VALUE,1,depth);
    #endif
    float3 world=ComputeWorldSpacePosition(positionCS.xy/_ScaledScreenParams.xy,depth,UNITY_MATRIX_I_VP);
    TopazClipWorld(world);
}
#endif
