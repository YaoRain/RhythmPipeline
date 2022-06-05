#ifndef VERTEX_SHADER_INCLUDE
#define VERTEX_SHADER_INCLUDE
#include "Common.hlsl"
#include "CommonStruct.hlsl"

Varyings Vertex(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.positionCS = TransformWorldToHClip(TransformObjectToWorld (input.positionOS));
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    return output;
}

#endif