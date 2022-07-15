#ifndef PBR_LIGHTING_FUNC
#define PBR_LIGHTING_FUNC
#include "PBRStruct.hlsl"

#define MIN_REFLECTIVITY 0.04
float OneMinusReflectivity(float metallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range * (1.0 - metallic);
}

BRDF GetBRDF(Surface surface)
{
    BRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color,  surface.metallic);
    float perceptualRoughness = PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

float Square(float f) {return f * f;}
float SpecularStrength (Surface surface, BRDF brdf, Light light)
{
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1,lh2) * normalization);
}

float3 DirectBRDF(Surface surface, BRDF brdf, Light light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

Light GetDirectionLight(int index)
{
    Light light;
    light.radiantItensity = _DirectionalLightRadiantItensitys[index];
    light.direction = _DirectionalLightDirections[index];
    light.directionalShadowMatrix = _DirectionalShadowMatrices[index];
    return light;
}
// TODO : 目前Irradiance只有来自直接光照的，为了实现全局光照，还需要加入间接光照
float3 GetIrradiance(Surface surface, Light light)
{
    return saturate(dot(surface.normal, light.direction)) * light.radiantItensity;
}

TEXTURE2D(_DirShadowMap);
#define SHADOW_SAMPLER sampler_linear_clamp_compare
SAMPLER_CMP(sampler_linear_clamp_compare);


// TODO : 实现正确的PCSS
float GetVisibilityFromShadow(Surface surface, Light light)
{
    float3 direction[14];
    direction[0] = float3(1, 0, 0);
    direction[1] = float3(-1, 0, 0);
    direction[2] = float3 (0, 1, 0);
    direction[3] = float3(0, -1, 0);
    direction[4] = float3(0, 0, 1);
    direction[5] = float3(0, 0, -1);
    direction[6] = float3(1, 1, 1);
    direction[7] = float3(1, -1, 1);
    direction[8] = float3(1, -1, -1);
    direction[9] = float3(1, 1, -1);
    direction[10] = float3(-1, 1, 1);
    direction[11] = float3(-1, -1, 1);
    direction[12] = float3(-1, -1, -1);
    direction[13] = float3(-1, 1, -1);
    /*
    {
        float3(1, 0, 0), float3(-1, 0, 0), float3 (0, 1, 0), float3(0, -1, 0), float3(0, 0, 1), float3(0, 0, -1),
        float3(1, 1, 1), float3(1, -1, 1), float3(1, -1, -1), float3(1, 1, -1),
        float3(-1, 1, 1), float3(-1, -1, 1), float3(-1, -1, -1), float3(-1, 1, -1)
    };*/
    float distWS = 0.2f;
    float visibility = 0;
    for(int i = 0; i < 14 ; i++)
    {
        float3 samplePoint = surface.positionWS + direction[i] * distWS;
        float3 positionSS = mul(light.directionalShadowMatrix, float4(samplePoint, 1.0)).xyz;
        // TODO : 实现PCSS。通过surface在 shadow space 的坐标，采样shadow map。
        visibility += SAMPLE_TEXTURE2D_SHADOW(_DirShadowMap, SHADOW_SAMPLER, positionSS);
    }
    visibility /= 14;
    return visibility;
}

TEXTURE2D(_VarianceShadowMapping);
SAMPLER(sampler_VarianceShadowMapping);

float3 GetVSM_UV_And_Depth(float3 positionWS, Light light)
{
    float4 positionCS = mul(light.directionalShadowMatrix, float4(positionWS, 1.0));
    float2 uv = positionCS.xy;
    float depth = positionCS.z;
    return float3(uv, depth);
}

// 切比雪夫不等式
float Chebychev(float o2, float avg, float t)
{
    return o2 / (o2 + (t-avg)*(t-avg));
}
float CalcuO2(float E_x, float E_x2)
{
    return E_x2 - E_x * E_x;
}

const int MAX_MIP_LEVEL = 9;
// TODO : 实现VSSM
float GetVisibilityFromVSM(Surface surface, Light light)
{
    float visibility;
    float3 vsmUV_AndDepth = GetVSM_UV_And_Depth(surface.positionWS, light);
    
    float pixelLightDepth = vsmUV_AndDepth.z;
    float2 shadowMapUV = vsmUV_AndDepth.xy;
    // VSSM block search, 用于确定切比雪夫在哪个mip上是准确的。
    float2 vsmDepthMip = float2(0.0, 0.0);
    float E_x = 0;
    for(int i = 0; i <= 1; i++)
    {
        vsmDepthMip = SAMPLE_TEXTURE2D_LOD(_VarianceShadowMapping, sampler_VarianceShadowMapping, shadowMapUV, i);
        E_x = vsmDepthMip.r;
        if(pixelLightDepth >= E_x) break;
    }

    //vsmDepthMip = SAMPLE_TEXTURE2D_LOD(_VarianceShadowMapping, sampler_VarianceShadowMapping, shadowMapUV, 3)
    float E_x2 = vsmDepthMip.g;
    float o2 = CalcuO2(E_x, E_x2);
    o2 = abs(o2) < 0.000001 ? 0.000001 : o2;
    float p0 = Chebychev(o2, E_x, pixelLightDepth);
    //float p0 = pixelLightDepth > E_x? Chebychev(o2, E_x, pixelLightDepth) : 0;
    return max(0.0, min(p0, 1.0));
    
    float zUnocc = pixelLightDepth;
    float pUnocc = Chebychev(o2, E_x, pixelLightDepth); // 不是blocker的概率
    float blockerAvgDepth = (E_x - pUnocc * zUnocc) / (1.0 - pUnocc);
    blockerAvgDepth = max(0, blockerAvgDepth);

    float penumbraSize = ((pixelLightDepth - blockerAvgDepth) / blockerAvgDepth) * 20.0f;
    
    
    float mipLevel = clamp(log2(penumbraSize), 0.0, 7.0);
    vsmDepthMip =  SAMPLE_TEXTURE2D_LOD(_VarianceShadowMapping, sampler_VarianceShadowMapping, shadowMapUV, mipLevel);
    E_x = vsmDepthMip.r;
    E_x2 = vsmDepthMip.g;
    o2 = CalcuO2(E_x, E_x2);

    // p 为shading point 深度大于平均深度（可见）的概率
    float p = pixelLightDepth > E_x? Chebychev(o2, E_x, pixelLightDepth) : 0;
    p = Chebychev(o2, E_x, pixelLightDepth);
    
    visibility = p;

    //visibility = pixelLightDepth > Ex ? 1 : 0;
    //visibility += SAMPLE_TEXTURE2D_SHADOW(_VarianceShadowMapping, SHADOW_SAMPLER, positionSS);
    return visibility;
}

// TODO:接合occlusion和shadow二者计算visibility
float GetVisibility(Surface surface, Light light)
{
    float visibility = 1.0;
    #if defined(_MANY_LIGHT)
    visibility = GetVisibilityFromShadow(surface, light);
    #endif
    
    #if defined(_VSM)
    visibility = GetVisibilityFromVSM(surface, light);
    #endif
    
    #if defined(_AO_MAP)
    visibility *= surface.occlusion;
    #endif
    return visibility;
}

TEXTURE2D(_WorldPos);
SAMPLER(sampler_WorldPos);
TEXTURE2D(_Flux);
SAMPLER(sampler_Flux);
TEXTURE2D(_WorldNormal);
SAMPLER(sampler_WorldNormal);
#define _RsmSampleCount 64
#define _RsmSize 2048.0
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Hammersley.hlsl"
// TODO : 有空处理下自照亮问题
float3 RSMSading(Surface surface, Light light)
{
    float3 worldPos = surface.positionWS;
    float3 N = surface.normal;
    float3 indirect = float3(0, 0, 0);
    float3 rec_uv = mul(light.directionalShadowMatrix, float4(worldPos, 1.0)).xyz;
    
    float3 vsmUV_AndDepth = GetVSM_UV_And_Depth(surface.positionWS, light);
    float pixelLightDepth = vsmUV_AndDepth.z;
    
    for(uint idx=0;idx<_RsmSampleCount;idx++)
    {
        float2 Xi = Hammersley2d(idx, _RsmSampleCount);
        float ss1 = Xi.x*sin(Xi.y*2.0*3.1415926);
        float ss2 = Xi.x*cos(Xi.y*2.0*3.1415926);
        float2 sample_coord=rec_uv + float2(ss1,ss2) * (1.0/_RsmSize)*10;
        float weight=Xi.x*Xi.y;
        #if defined(_VSM)
            float vsmDepth = SAMPLE_TEXTURE2D_LOD(_VarianceShadowMapping, sampler_VarianceShadowMapping, sample_coord, 0);
            if(abs(vsmDepth - pixelLightDepth) < 0.005)
            {
                continue;
            }
        #endif
        float3 vpl_normal=normalize(SAMPLE_TEXTURE2D(_WorldNormal,sampler_WorldNormal,sample_coord).xyz);
        float3 vpl_worldPos=SAMPLE_TEXTURE2D(_WorldPos,sampler_WorldPos,sample_coord).xyz;
        float3 vpl_flux=SAMPLE_TEXTURE2D(_Flux,sampler_Flux,sample_coord);

        float3 indirect_result = (vpl_flux*max(0, dot(vpl_normal, worldPos-vpl_worldPos))*max(0, dot(N, vpl_worldPos-worldPos)))/pow(length(worldPos-vpl_worldPos),2.0);
        indirect_result*=(weight);
        indirect+=indirect_result;
    }
    
    return indirect;
}

float3 Shading(Surface surface, BRDF brdf)
{
    float3 shadingResult = 0;
    for(int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light =  GetDirectionLight(i);
        float3 directLightingResult = GetIrradiance(surface, light) * DirectBRDF(surface, brdf, light) * GetVisibility(surface, light);
        float3 indirectLightingResult = float3(0, 0, 0);
        #if defined(_RSM)
        indirectLightingResult = RSMSading(surface, light) ;//* DirectBRDF(surface, brdf, light);
        #endif
        shadingResult += directLightingResult + indirectLightingResult;
    }
    return shadingResult;
}

#endif