#ifndef TOPAZ_SKY_INCLUDED
#define TOPAZ_SKY_INCLUDED
float4 _TopazSkySun,_TopazSkyMoon,_TopazSkyWeather,_TopazSkyLight;
float4 _TopazFogColor,_TopazVisibility,_TopazSkyZenith,_TopazSkyHorizon;
TEXTURECUBE(_TopazAlpineBackdrop);
SAMPLER(sampler_TopazAlpineBackdrop);
float _TopazHasBackdrop;
float TopazCloudHash(float2 p)
{
    float3 q=frac(float3(p.x,p.y,p.x)*.1031);
    q+=dot(q,q.yzx+33.33);
    return frac((q.x+q.y)*q.z);
}
float TopazCloudNoise(float2 p)
{
    float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
    return lerp(lerp(TopazCloudHash(cell),TopazCloudHash(cell+float2(1,0)),f.x),
        lerp(TopazCloudHash(cell+float2(0,1)),TopazCloudHash(cell+1),f.x),f.y);
}
float TopazCloudShape(float2 p)
{return TopazCloudNoise(p)*.57+TopazCloudNoise(p*2.03+7)*.28+TopazCloudNoise(p*4.11+19)*.15;}
float3 TopazSkyColor(float3 direction,float3 sky,float3 horizon)
{
    float3 d=normalize(direction);
    float3 color=lerp(horizon,sky,pow(saturate(d.y),.55));
    float sun=saturate(dot(d,_TopazSkySun.xyz));
    color+=_TopazSkyLight.rgb*(pow(sun,24)*.16+pow(sun,1800)*2.5)*_TopazSkySun.w;
    float moon=saturate(dot(d,_TopazSkyMoon.xyz));
    color+=float3(.65,.78,1)*(pow(moon,100)*.035+pow(moon,4000)*1.8)*_TopazSkyMoon.w;
    if(_TopazHasBackdrop>.5)
    {
        // Encoded from owned mountain silhouettes: shaded form, height, coverage.
        float3 mountain=SAMPLE_TEXTURECUBE(_TopazAlpineBackdrop,sampler_TopazAlpineBackdrop,d).rgb;
        float3 rock=lerp(_TopazFogColor.rgb*.40,horizon*.86,mountain.r);
        float3 snow=lerp(_TopazFogColor.rgb*.8,horizon*.98,mountain.r);
        float3 ridge=lerp(rock,snow,smoothstep(.65,.94,mountain.g)*.7);
        color=lerp(color,ridge,saturate(mountain.b)*.9);
    }
    if(d.y>.01)
    {
        float2 p=d.xz/max(.32,d.y+.25)*1.6+float2(.0011,.0004)*_TopazSkyWeather.y;
        float shape=TopazCloudShape(p),coverage=lerp(.5,.26,_TopazSkyWeather.x);
        float cloud=smoothstep(coverage,coverage+.20,shape)*smoothstep(.01,.14,d.y);
        float gradient=shape-TopazCloudShape(p+_TopazSkySun.xz*.12);
        float3 shadow=lerp(float3(.035,.055,.09),horizon*.85,_TopazSkySun.w);
        float3 light=lerp(float3(.10,.14,.21),float3(.92,.94,.93),_TopazSkySun.w)*lerp(1,.72,_TopazSkyWeather.x);
        color=lerp(color,lerp(shadow,light,saturate(.4+gradient*3+(shape-coverage)*1.1)),cloud*.78);
    }
    return lerp(color,_TopazFogColor.rgb,(1-smoothstep(.02,.16,d.y))*_TopazVisibility.z);
}
#endif
