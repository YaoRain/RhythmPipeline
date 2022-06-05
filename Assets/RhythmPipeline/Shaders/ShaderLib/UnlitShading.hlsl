#ifndef UNLIT_PBR_SHADING_INCLUDE
#define UNLIT_PBR_SHADING_INCLUDE
#include "Common.hlsl"
#include "CommonStruct.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float4 UnlitFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    float4 outColor = baseColor * mainTexColor;
    float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);

    #if defined(_CLIPPING)
        clip(outColor.a - cutoff);
    #endif
    
    return outColor;
}

#endif