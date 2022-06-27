#ifndef SHADOW_CASTER_INCLUDE
#define SHADOW_CASTER_INCLUDE
#include "Common.hlsl"
#include "CommonStruct.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

Varyings Vertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.positionCS = TransformWorldToHClip(TransformObjectToWorld (input.positionOS));
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

Varyings Blur_Vertex(Attributes input)
{
    Varyings output;
    output.positionCS = TransformWorldToHClip(TransformObjectToWorld (input.positionOS));
    output.baseUV = input.baseUV;
    return output;
}

void Fragment(Varyings input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseColor * baseMap;

    #if defined(_CLIPPING)
    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
}

Varyings VSM_Vertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    
    output.positionCS = TransformWorldToHClip(TransformObjectToWorld (input.positionOS));
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

float2 VSM_Fragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
    
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    float4 base = baseColor * baseMap;

    #if defined(_CLIPPING)
    //clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif
    // 写入 depth和depth^2
    float depth = input.positionCS.z;
    return float2(depth, depth*depth);
}
float2 _SourceTex_TexelSize;
float2 FragBlurV(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float texelSize = _SourceTex_TexelSize.y;
    // float texelSize = 1.0 / 1024.0;
    float2 uv = input.baseUV;

    // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
    float2 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0.0, texelSize * 3.23076923));
    float2 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0.0, texelSize * 1.38461538));
    float2 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv                                      );
    float2 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texelSize * 1.38461538));
    float2 c4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texelSize * 3.23076923));
    
    float2 color = c0 * 0.07027027 + c1 * 0.31621622
                + c2 * 0.22702703
                + c3 * 0.31621622 + c4 * 0.07027027;
    return color;
}

float2 FragBlurH(Varyings input) : SV_TARGET
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
    float texelSize = _SourceTex_TexelSize.x;
    //float texelSize = 1.0 / 1024.0;
    float2 uv = input.baseUV;

    // 9-tap gaussian blur on the downsampled source
    float2 c0 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 4.0, 0.0));
    float2 c1 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 3.0, 0.0));
    float2 c2 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 2.0, 0.0));
    float2 c3 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 1.0, 0.0));
    float2 c4 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv                               );
    float2 c5 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 1.0, 0.0));
    float2 c6 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 2.0, 0.0));
    float2 c7 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 3.0, 0.0));
    float2 c8 = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 4.0, 0.0));

    float2 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
                + c4 * 0.22702703
                + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

    return color;
}



#endif