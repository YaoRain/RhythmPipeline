Shader "RhythmRP/Rhythm_Scene_PBR"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        
        [Toggle(_METALLIC_MAP)] _useMatellicMap("Use Matellic Map", Float) = 0
        [Toggle(_METALLIC_MAP_WITH_GLOSS)] _matellicMapWithGloss("Matellic Map With Gloss", Float) = 0
        _Metallic("Metallic", Range(0,1)) = 0
        _MetallicGlossMap("Metallic And Gloss Map", 2D) = "white" {}
        
        [Toggle(_ROUGHNESS_MAP)] _useRoughnessMap("Use Roughness Map", Float) = 0
        _RoughnessMap("Roughness Map", 2D) = "white"{}
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        
        _DetailNormalMapScale("Normal Scale", Range(0.0, 2.0)) = 1.0
        [Normal] _DetailNormalMap("Normal Map", 2D) = "bump" {}
        
        _OcclusionStrength("Occlusion Strength", Range(0.0, 1.0)) = 1.0
        _OcclusionMap("Occlusion", 2D) = "white" {}
        
        _EmissionStrength("Emission Strength", Range(0.0, 1.0)) = 0.0
        _EmissionColor("Emission Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _EmmissionMap("Emission", 2D) = "white" {}
        
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode" = "RhythmPBR"}
        LOD 100

        Pass
        {
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _NORMAL_MAP on
            #pragma shader_feature _AO_MAP on
            #pragma shader_feature _ROUGHNESS_MAP
            #pragma shader_feature _METALLIC_MAP
            #pragma shader_feature _METALLIC_MAP_WITH_GLOSS
            #pragma shader_feature _MANY_LIGHT
            #pragma shader_feature _VSM
            #pragma 
            
            #pragma vertex Vertex
            #pragma fragment PBRShading
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/PBR/PBRVertexShader.hlsl"
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/PBR/PBRShading.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING

            
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/ShadowCaster.hlsl"
            
            ENDHLSL
        }
    }
}
