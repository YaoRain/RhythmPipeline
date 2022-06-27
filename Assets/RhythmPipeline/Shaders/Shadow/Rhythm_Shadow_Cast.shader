Shader "RhythmRP/Rhythm_Shadow_Cast"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode" = "RhythmPBR"}
        LOD 100

        Pass
        {
            Name "Shadow Caster"
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
        
        Pass
        {
            
            Name "VSM Caster"

            ZWrite On
            ZTest LEqual
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma shader_feature _CLIPPING
            
            #pragma vertex VSM_Vertex
            #pragma fragment VSM_Fragment
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/ShadowCaster.hlsl"
            
            ENDHLSL
        }
        
        Pass
        {
            Name "VSM V Blur Pass"

            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex Blur_Vertex
            #pragma fragment FragBlurV
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/ShadowCaster.hlsl"
            
            ENDHLSL 
        }
        
        Pass
        {
            Name "VSM H Blur Pass"
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex Blur_Vertex
            #pragma fragment FragBlurH
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/ShadowCaster.hlsl"
            
            ENDHLSL 
        }
    }
}
