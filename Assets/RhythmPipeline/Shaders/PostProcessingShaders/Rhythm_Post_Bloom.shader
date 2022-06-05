Shader "RhythmRP/Post/Bloom"
{
    Properties
    {
        _MainTex ("Texture", any) = "" {}
        _BlurDstTex ("Texture", any) = "" {}
    }
    
    HLSLINCLUDE
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
    TEXTURE2D(_BlurDstTex);
    SAMPLER(sampler_BlurDstTex);
    TEXTURE2D(_BloomDstTex);
    SAMPLER(sampler_BloomDstTex);
    
    float4 _SourceTex_TexelSize;
    half4 FragPrefilter(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float2 uv = input.baseUV;
        
        float texelSize = _SourceTex_TexelSize.x;
        half4 A = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(-1.0, -1.0));
        half4 B = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(0.0, -1.0));
        half4 C = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(1.0, -1.0));
        half4 D = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(-0.5, -0.5));
        half4 E = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(0.5, -0.5));
        half4 F = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(-1.0, 0.0));
        half4 G = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
        half4 H = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(1.0, 0.0));
        half4 I = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(-0.5, 0.5));
        half4 J = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(0.5, 0.5));
        half4 K = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(-1.0, 1.0));
        half4 L = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(0.0, 1.0));
        half4 M = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelSize * float2(1.0, 1.0));

        half2 div = (1.0 / 4.0) * half2(0.5, 0.125);

        half4 o = (D + E + I + J) * div.x;
        o += (A + B + G + F) * div.y;
        o += (B + C + H + G) * div.y;
        o += (F + G + L + K) * div.y;
        o += (G + H + M + L) * div.y;
        half3 color = o.xyz;

        // User controlled clamp to limit crazy high broken spec
        float4 ClampMax = float4(1, 1, 1, 1);
        color = min(ClampMax, color);

        float Threshold = 0.9f;
        float ThresholdKnee = 0.7f;
        // Thresholding
        half brightness = Max3(color.r, color.g, color.b);
        half softness = clamp(brightness - Threshold + ThresholdKnee, 0.0, 2.0 * ThresholdKnee);
        softness = (softness * softness) / (4.0 * ThresholdKnee + 1e-4);
        half multiplier = max(brightness - Threshold, softness) / max(brightness, 1e-4);
        color *= multiplier;

        return float4(color, 1.0);
    }

    half4 EncodeHDR(half3 color)
        {
        #if _USE_RGBM
            half4 outColor = EncodeRGBM(color);
        #else
            half4 outColor = half4(color, 1.0);
        #endif

        #if UNITY_COLORSPACE_GAMMA
            return half4(sqrt(outColor.xyz), outColor.w); // linear to γ
        #else
            return outColor;
        #endif
        }
    half3 DecodeHDR(half4 color)
        {
        #if UNITY_COLORSPACE_GAMMA
            color.xyz *= color.xyz; // γ to linear
        #endif

        #if _USE_RGBM
            return DecodeRGBM(color);
        #else
            return color.xyz;
        #endif
        }
    half4 FragBlurH(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float texelSize = _SourceTex_TexelSize.x * 2.0;
        float2 uv = input.baseUV;

        // 9-tap gaussian blur on the downsampled source
        half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 4.0, 0.0)));
        half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 3.0, 0.0)));
        half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 2.0, 0.0)));
        half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(texelSize * 1.0, 0.0)));
        half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv                               ));
        half3 c5 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 1.0, 0.0)));
        half3 c6 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 2.0, 0.0)));
        half3 c7 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 3.0, 0.0)));
        half3 c8 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(texelSize * 4.0, 0.0)));

        half3 color = c0 * 0.01621622 + c1 * 0.05405405 + c2 * 0.12162162 + c3 * 0.19459459
                    + c4 * 0.22702703
                    + c5 * 0.19459459 + c6 * 0.12162162 + c7 * 0.05405405 + c8 * 0.01621622;

        return EncodeHDR(color);
    }

    half4 FragBlurV(Varyings input) : SV_Target
    {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        float texelSize = _SourceTex_TexelSize.y;
        float2 uv = input.baseUV;

        // Optimized bilinear 5-tap gaussian on the same-sized source (9-tap equivalent)
        half3 c0 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0.0, texelSize * 3.23076923)));
        half3 c1 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - float2(0.0, texelSize * 1.38461538)));
        half3 c2 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv                                      ));
        half3 c3 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texelSize * 1.38461538)));
        half3 c4 = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + float2(0.0, texelSize * 3.23076923)));

        half3 color = c0 * 0.07027027 + c1 * 0.31621622
                    + c2 * 0.22702703
                    + c3 * 0.31621622 + c4 * 0.07027027;

        return EncodeHDR(color);
    }
    half3 Upsample(float2 uv)
        {
            half3 highMip = DecodeHDR(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv));

        #if _BLOOM_HQ && !defined(SHADER_API_GLES)
            half3 lowMip = DecodeHDR(SampleTexture2DBicubic(TEXTURE2D_X_ARGS(_SourceTexLowMip, sampler_LinearClamp), uv, _SourceTexLowMip_TexelSize.zwxy, (1.0).xx, unity_StereoEyeIndex));
        #else
            half3 lowMip = DecodeHDR(SAMPLE_TEXTURE2D(_BlurDstTex, sampler_BlurDstTex, uv));
        #endif
            float Scatter = 0.7f;
            return lerp(highMip, lowMip, Scatter);
        }
    ENDHLSL
    SubShader
    {
        Tags { "RenderType"="PostProcessing"}
        LOD 100

        Pass
        {
            Name "Bloom Get Bright Area"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            
            float4 Fragment(Varyings input) : SV_TARGET
            {
                return FragPrefilter(input);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom V Blur"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            float4 Fragment(Varyings input) : SV_TARGET
            {
                return FragBlurV(input);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom H Blur"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            float4 Fragment(Varyings input) : SV_TARGET
            {
                return FragBlurH(input);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Upsample"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            float4 Fragment(Varyings input) : SV_TARGET
            {
                return  float4(Upsample(input.baseUV), 1.0);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Bloom Final Blit"
            
            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            float4 Fragment(Varyings input) : SV_TARGET
            {
                float3 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.baseUV);
                float bloomStrength = 8.0;
                float maxColorF = 0.3;
                float3 maxColor = float3(maxColorF, maxColorF, maxColorF);
                float3 blurColor = SAMPLE_TEXTURE2D(_BlurDstTex, sampler_BlurDstTex, input.baseUV);
                blurColor = min(maxColor, blurColor*bloomStrength);
                
                return  float4(blurColor + baseColor, 1.0);
            }
            ENDHLSL
        }
    }
}