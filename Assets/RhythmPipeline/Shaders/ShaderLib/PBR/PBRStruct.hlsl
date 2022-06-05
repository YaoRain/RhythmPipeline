#ifndef RHYTHM_PBR_STRUCT_INCLUDE
#define RHYTHM_PBR_STRUCT_INCLUDE
#include "Assets/RhythmPipeline/Shaders/ShaderLib/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/packing.hlsl"
UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_DEFINE_INSTANCED_PROP(float, _OcclusionStrength)
UNITY_DEFINE_INSTANCED_PROP(float,_EmissionStrength)
UNITY_DEFINE_INSTANCED_PROP(float4,_EmissionColor)
UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
#if defined(_NORMAL_MAP)
UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalMapScale)
#endif
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
CBUFFER_START(_CustomLight)
int _DirectionalLightCount;
float4 _DirectionalLightRadiantItensitys[MAX_DIRECTIONAL_LIGHT_COUNT];
float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
float4x4 _DirectionalShadowMatrices[MAX_DIRECTIONAL_LIGHT_COUNT];
float _ShadowStrength;
CBUFFER_END

struct Attributes
{
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : VAR_POSITION;
    float2 baseUV : VAR_BASE_UV;
    float3 normalWS : VAR_NORMAL;
    float4 tangentWS : VAR_TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Light
{
    float3 radiantItensity;
    float3 direction;
    float4x4 directionalShadowMatrix;
};

struct Surface
{
    float3 normal;
    float3 viewDirection;
    float3 positionWS;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
    float occlusion;
};

struct BRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float3 NormalTangentToWorld (float3 normalTS, float3 normalWS, float4 tangentWS) {
    float3x3 tangentToWorld =
        CreateTangentToWorld(normalWS, tangentWS.xyz, tangentWS.w);
    return TransformTangentToWorld(normalTS, tangentToWorld);
}
float3 DecodeNormal (float4 sample, float scale) 
{
    #if defined(UNITY_NO_DXT5nm)
    return UnpackNormalRGB(sample, scale);
    #else
    return UnpackNormalmapRGorAG(sample, scale);
    #endif
}

#endif