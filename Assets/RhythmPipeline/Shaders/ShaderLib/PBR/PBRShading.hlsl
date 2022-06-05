#ifndef PBR_SHADING_INCLUDE
#define PBR_SHADING_INCLUDE
#include "PBRStruct.hlsl"
#include "PBRLightFunc.hlsl"

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_MetallicGlossMap);
SAMPLER(sampler_MetallicGlossMap);
TEXTURE2D(_RoughnessMap);
SAMPLER(sampler_RoughnessMap);
TEXTURE2D(_DetailNormalMap);
SAMPLER(sampler_DetailNormalMap);
TEXTURE2D(_OcclusionMap);
SAMPLER(sampler_OcclusionMap);
TEXTURE2D(_EmmissionMap);
SAMPLER(sampler_EmmissionMap);

float4 PBRShading(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    baseColor = baseColor * baseMap;
    float cutoff = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);

    Surface surface;
    surface.positionWS = input.positionWS;
    #if defined(_NORMAL_MAP)
    float4 normalMap = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailNormalMap, input.baseUV);
    float normalScale = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DetailNormalMapScale);
    float3 normalTS = DecodeNormal(normalMap, normalScale);
    surface.normal = normalize(NormalTangentToWorld(normalTS, input.normalWS, input.tangentWS));
    #else
    surface.normal = normalize(input.normalWS);
    //surface.normal = float3(0, 1, 0);
    #endif
    surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
    surface.color = baseColor.rgb;
    surface.alpha = baseColor.a;
    #if defined(_METALLIC_MAP)
    float4 metallicGlossMap = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, input.baseUV);
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic) * metallicGlossMap.r;
    #else
    surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
    #endif
    
    #if defined(_ROUGHNESS_MAP)
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness) * (1 - SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, input.baseUV).r);
    #elif defined(_METALLIC_MAP)&&defined(_METALLIC_MAP_WITH_GLOSS)
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness) * metallicGlossMap.a;
    #else
    surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
    #endif
    
    surface.occlusion = lerp(1.0, SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.baseUV), _OcclusionStrength);
    #if defined(_CLIPPING)
        clip(baseColor.a - cutoff);
    #endif
    float3 emmission = SAMPLE_TEXTURE2D(_EmmissionMap, sampler_EmmissionMap, input.baseUV).rgb * _EmissionStrength * _EmissionColor.rgb;
    return float4(Shading(surface, GetBRDF(surface)) + emmission, surface.alpha);
    //return baseMap;
}

#endif