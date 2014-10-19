// VertexShader: VertexScene, entry: VShader
// VertexShader: VertexShadow, entry: VertexShadow
// PixelShader:  PixelScene, entry: PShader
// PixelShader:  PixelSceneSimple, entry: PShader, defines: SIMPLE
// PixelShader:  DepthNormalPrepass, entry: PShader, defines: PREPASS
// PixelShader:  Sky, entry: Sky

Texture2D<float> shadowMap                  : register(t0);
Texture2D<float> ssaoTex                    : register(t1);
Texture2D<float4> ssrTex                    : register(t2);
Texture2D<float4> albedoTex                 : register(t4);
Texture2D<float4> bumpTex                   : register(t9);

Texture3D<float4> giVolumeTexR              : register(t5);
Texture3D<float4> giVolumeTexG              : register(t6);
Texture3D<float4> giVolumeTexB              : register(t7);

Texture3D<float4> volumetricFogTexture      : register(t8);
TextureCube<float4> cubeEnvTexture          : register(t11);



#include "constants.fx"
#include "evsm_inc.fx"
#include "volumetric_fog_inc.fx"
#include "derivative_mapping_inc.fx"

struct VS_INPUT
{
    float4 position             : POSITION;
    float3 normal               : NORMAL;
    float2 uv                   : TEXCOORD0;
};

struct VS_OUTPUT
{
    float4 position             : SV_POSITION;
    float3 normal               : NORMAL;
    float3 worldSpacePosition   : TEXCOORD0;
    float2 uv                   : TEXCOORD1;
};

struct VS_OUTPUT_SHADOW
{
    float4 position             : SV_POSITION;
};


VS_OUTPUT VShader(VS_INPUT i )
{
    VS_OUTPUT vo;
    float4 position = i.position;
    position.w = 1.0f;
    float4 screenSpacePosition = mul(g_ViewProjMatrix, position);
    vo.position = screenSpacePosition;
    vo.normal = i.normal;
    vo.worldSpacePosition = position.xyz;
    vo.uv = i.uv;
    return vo;
}

VS_OUTPUT_SHADOW VertexShadow(VS_INPUT i)
{
    VS_OUTPUT_SHADOW vo;
    float4 position = i.position;
    position.w = 1.0f;
    float4 screenSpacePosition = mul(g_ShadowViewProjMatrix, position);

    vo.position = screenSpacePosition;

    return vo;
}

#include "optimized-ggx.hlsl"

struct LightEvaluation
{
    float3 diffuse;
    float3 specular;
};


LightEvaluation CalculateLighting(float3 lightVector, float3 normalVector, float3 viewVector, float roughness)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float3 NdotL = saturate(dot(lightVector,normalVector));

    // PI for Lambert energy conservation    
    lightEval.diffuse = NdotL / PI;

    // for baking GI etc. - diffuse only
#ifndef SIMPLE
    lightEval.specular = LightingFuncGGX_OPT5(normalVector, viewVector, lightVector, roughness, g_SpecularReflectance);
#endif

    return lightEval;
}


LightEvaluation EvaluateSun(float3 worldSpacePos, float3 normal, float3 viewVector, float roughness)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float4 lightSpacePos = mul(g_ShadowViewProjMatrix, float4(worldSpacePos,1));
    lightSpacePos /= lightSpacePos.w;

    float4 smSample = shadowMap.SampleLevel(linearSampler, lightSpacePos.xy*float2(0.5, -0.5) + 0.5, 0.0f);
    float shadow = calculateShadowEVSM(smSample, lightSpacePos.z);

    lightEval = CalculateLighting(g_LightDir.xyz, normal, viewVector, roughness);
    
    float3 finalColorAttenuation = shadow*g_LightBrightness*g_LightColor.rgb;
    lightEval.diffuse *= finalColorAttenuation;
    lightEval.specular *= finalColorAttenuation;

    return lightEval;
}

LightEvaluation EvaluateAmbient(float3 worldSpacePos, float3 normal, float3 viewVector, float2 fullScreenUV, float roughness)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float4 shCosineResponse = SH2CosineResponse(normal) / PI;

    float3 volumeTexPosition = (worldSpacePos + normal * 0.5f - g_WorldBoundsMin.xyz) * g_WorldBoundsInvRange.xyz * GI_VOLUME_TEX_SCALE + GI_VOLUME_TEX_BIAS;
    float4 shRed = giVolumeTexR.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shGreen = giVolumeTexG.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shBlue = giVolumeTexB.SampleLevel(linearSampler, volumeTexPosition, 0);

    lightEval.diffuse.r = max(dot(shRed, shCosineResponse), 0.0f);
    lightEval.diffuse.g = max(dot(shGreen, shCosineResponse), 0.0f);
    lightEval.diffuse.b = max(dot(shBlue, shCosineResponse), 0.0f);

#ifndef SIMPLE
    float3 cubeVec = -reflect(viewVector, normal);
    float calculatedMip = cubeEnvTexture.CalculateLevelOfDetail(linearSampler, cubeVec);
    lightEval.specular.rgb = cubeEnvTexture.SampleLevel(linearSampler, cubeVec, max(RoughnessToMipLevel(roughness), calculatedMip)).rgb;

    float4 ssr = ssrTex.SampleLevel(linearSampler, GetBilateralUV(fullScreenUV), 0);
    lightEval.specular.rgb = lerp(lightEval.specular.rgb, ssr.rgb, ssr.aaa);
    float NoV = saturate(dot(normal, viewVector));

    float2 envBRDF = EnvLightingBRDF(roughness, NoV);
    lightEval.specular *= envBRDF.xxx * g_SpecularReflectance + envBRDF.yyy;
#endif

    return lightEval;
}

LightEvaluation EvaluateLights(float3 worldSpacePos, float3 normal, float3 viewVector, float roughness)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float3 pointLightVector = g_LocalPointLightPosition.xyz - worldSpacePos.xyz;
    float pointLightVectorDistSqr = 0.1f + dot(pointLightVector, pointLightVector); // bias / fake "area" light size
    float reverseVectorLen = rsqrt(pointLightVectorDistSqr);
    float pointLightAttenuation = reverseVectorLen * reverseVectorLen;
    float3 finalColorAttenuation = pointLightAttenuation * g_LocalPointLightColor.rgb * g_PointLightBrightness;
    lightEval = CalculateLighting(pointLightVector * reverseVectorLen, normal, viewVector, roughness);
    
    lightEval.diffuse *= finalColorAttenuation;
    lightEval.specular *= finalColorAttenuation;

    return lightEval;
}

float4 PShader(VS_OUTPUT i) : SV_Target
{
    float3 normal = normalize(i.normal.xyz);
    float4 worldSpacePos = float4(i.worldSpacePosition,1);

    float3 bumpMapSample = bumpTex.Sample(anisoWrapSampler, i.uv).rgb;
    float roughness = g_Roughness;
    float3 albedo = GammaToLinear(albedoTex.Sample(anisoWrapSampler, i.uv).rgb);

    float2 fullScreenUV = i.position.xy * g_ScreenSize.zw;

#ifndef NOSSAO
    normal = CalculateSurfaceNormal(worldSpacePos.xyz, normal, -(bumpMapSample.rg - 0.5f) * 4, i.uv);
#endif

#ifdef PREPASS
    return float4(normal, roughness);
#else

#ifdef SIMPLE
    float ssao = 1.0f;
    float4 fog = float4(0,0,0,1);
#else
    float ssao = ssaoTex.SampleLevel(linearSampler, GetBilateralUV(fullScreenUV), 0);

    float3 volumeFogUVW = VolumeTextureSpaceFromScreenSpace(float3(fullScreenUV, DepthToVolumeZPos(LinearizeDepth(i.position.z))));
    float4 fog = volumetricFogTexture.SampleLevel(linearSampler, volumeFogUVW, 0);
#endif

    float3 viewVector = normalize(g_WorldEyePos.xyz - worldSpacePos.xyz);

    LightEvaluation finalLight = EvaluateSun(worldSpacePos.xyz, normal, viewVector, roughness);
    LightEvaluation ambientLight = EvaluateAmbient(worldSpacePos.xyz, normal, viewVector, fullScreenUV, roughness);

    finalLight.diffuse  += ambientLight.diffuse  * ssao;
    finalLight.specular += ambientLight.specular;

    LightEvaluation localLightsLight = EvaluateLights(worldSpacePos.xyz, normal, viewVector, roughness);

#ifndef SIMPLE    
    finalLight.diffuse += localLightsLight.diffuse;
    finalLight.specular += localLightsLight.specular;
#endif

    float3 lighting = finalLight.diffuse * albedo + finalLight.specular;

    return float4(lighting * fog.aaa + fog.rgb, 1.0f);
#endif // !defined PREPASS
}

float4 Sky(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 fog = volumetricFogTexture.SampleLevel(linearSampler, VolumeTextureSpaceFromScreenSpace(float3(i.position.xy * g_ScreenSize.zw, DepthToVolumeZPos(10000.0f))), 0);
    return float4(float3(1.0f, 1.0f, 1.5f) * fog.aaa + fog.rgb, 1.0f); // aaaa clean me
}