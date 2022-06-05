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

// TODO:接合occlusion和shadow二者计算visibility
float GetVisibility(Surface surface, Light light)
{
    float visibility = GetVisibilityFromShadow(surface, light);
    #if defined(_AO_MAP)
    visibility *= surface.occlusion;
    #endif
    return visibility;
}

float3 Shading(Surface surface, BRDF brdf)
{
    float3 shadingResult = 0;
    for(int i = 0; i < _DirectionalLightCount; i++)
    {
        Light light =  GetDirectionLight(i);
        shadingResult += GetIrradiance(surface, light) * DirectBRDF(surface, brdf, light) * GetVisibility(surface, light);
    }
    return shadingResult;
}

#endif