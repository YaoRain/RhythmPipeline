Shader "RhythmRP/Post/ToneMapping"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
    }

    SubShader
    {
        Tags { "RenderType"="PostProcessing"}
        LOD 100

        Pass
        {
            Name "Tone Mapping"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            #include "Assets/RhythmPipeline/Shaders/ShaderLib/Common.hlsl"
            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 baseUV : TEXCOORD0;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 baseUV : TEXCOORD0;
            };
            Varyings Vertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformWorldToHClip(TransformObjectToWorld (input.positionOS));
                output.baseUV = input.baseUV;
                return output;
            }
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float3 ACESToneMapping(float3 color, float adapted_lum) 
            { 	
                   const float A = 2.51f; 	
                   const float B = 0.03f; 	
                   const float C = 2.43f; 	
                   const float D = 0.59f; 	
                   const float E = 0.14f;  	
                   return (color * (A * color + B)) / (color * (C * color + D) + E); 
            }
            float4 Fragment(Varyings input) : SV_TARGET
            {
                float4 mainTexColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
                float3 toneMappingColor = ACESToneMapping(mainTexColor.rgb, 0);

                //float3 gammaColor = pow(mainTexColor, 1.0/2.2);
                
                return float4(toneMappingColor, 1);
            }
            ENDHLSL
        }
    }
}