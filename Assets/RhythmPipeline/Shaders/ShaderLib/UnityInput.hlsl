#ifndef RHYTHM_UNITY_INPUT_INCLUDE
#define RHYTHM_UNITY_INPUT_INCLUDE


float3 _WorldSpaceCameraPos;

CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4x4 unity_MatrixPreviousM;
float4x4 unity_MatrixPreviousMI;
float4 unity_LODFade;
real4 unity_WorldTransformParams;
CBUFFER_END

CBUFFER_START(UnityPerFrame)
float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
CBUFFER_END

#endif