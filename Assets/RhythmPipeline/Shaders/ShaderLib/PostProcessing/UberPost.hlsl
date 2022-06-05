#ifndef PBR_SHADING_INCLUDE
#define PBR_SHADING_INCLUDE
#include "UberPostStruct.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

float4 PBRShading(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 mainTexColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_MainTex, input.baseUV);
    baseColor = baseColor * mainTexColor;
    float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);

    Surface surface;
    surface.normal = normalize(input.normalWS);
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.color = baseColor.rgb;
    surface.alpha = baseColor.a;
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    #if defined(_CLIPPING)
        clip(baseColor.a - cutoff);
    #endif
    
    return float4(1, 1, 1, 1);
}

#endif