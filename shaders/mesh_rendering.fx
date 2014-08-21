// VertexShader: VertexScene, entry: VShader
// VertexShader: VertexShadow, entry: VertexShadow
// PixelShader:  PixelScene, entry: PShader
// PixelShader:  PixelSceneSimple, entry: PShader, defines: SIMPLE
// PixelShader:  DepthNormalPrepass, entry: PShader, defines: PREPASS
// PixelShader:  Sky, entry: Sky

Texture2D<float> shadowMap                  : register(t0);
Texture2D<float> ssaoTex                    : register(t1);
Texture2D<float4> albedoTex                 : register(t4);
Texture2D<float4> bumpTex                   : register(t9);

Texture3D<float4> giVolumeTexR              : register(t5);
Texture3D<float4> giVolumeTexG              : register(t6);
Texture3D<float4> giVolumeTexB              : register(t7);

Texture3D<float4> volumetricFogTexture      : register(t8);



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

// Grabbed from Black Ops 2 by Dimitar Lazarov, http://blog.selfshadow.com/publications/s2013-shading-course/lazarov/s2013_pbs_black_ops_2_notes.pdf
float EnvironmentBRDF(float g, float NoV, float rf0)
{
    float4 t = float4(1 / 0.96, 0.475, (0.0275 - 0.25 * 0.04) / 0.96, 0.25);
    t *= float4(g, g, g, g);
    t += float4(0, 0, (0.015 - 0.75 * 0.04) / 0.96, 0.75);
    float a0 = t.x * min(t.y, exp2(-9.28 * NoV)) + t.z;
    float a1 = t.w;
    return saturate(a0 + rf0 * (a1 - a0));
}

struct LightEvaluation
{
    float3 diffuse;
    float3 specular;
};


LightEvaluation CalculateLighting(float3 lightVector, float3 normalVector, float3 viewVector)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float3 NdotL = saturate(dot(lightVector,normalVector));

    // PI for Lambert energy conservation    
    lightEval.diffuse = NdotL / PI;

    // for baking GI etc. - diffuse only
#ifndef SIMPLE
    lightEval.specular = LightingFuncGGX_OPT2(normalVector, viewVector, lightVector, g_Roughness, g_SpecularReflectance);
#endif

    return lightEval;
}


LightEvaluation EvaluateSun(float3 worldSpacePos, float3 normal, float3 viewVector)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float4 lightSpacePos = mul(g_ShadowViewProjMatrix, float4(worldSpacePos,1));
    lightSpacePos /= lightSpacePos.w;

    float4 smSample = shadowMap.SampleLevel(linearSampler, lightSpacePos.xy*float2(0.5, -0.5) + 0.5, 0.0f);
    float shadow = calculateShadowEVSM(smSample, lightSpacePos.z);

    lightEval = CalculateLighting(g_LightDir.xyz, normal, viewVector);
    
    float3 finalColorAttenuation = shadow*g_LightBrightness*g_LightColor.rgb;
    lightEval.diffuse *= finalColorAttenuation;
    lightEval.specular *= finalColorAttenuation;

    return lightEval;
}

LightEvaluation EvaluateAmbient(float3 worldSpacePos, float3 normal, float3 viewVector)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float4 shCosineResponse = SH2CosineResponse(normal) / PI;
    float4 shReflectResponse = SH2DirectionResponse(-reflect(viewVector, normal));

    float3 volumeTexPosition = (worldSpacePos + normal * 0.5f - g_WorldBoundsMin.xyz) * g_WorldBoundsInvRange.xyz * GI_VOLUME_TEX_SCALE + GI_VOLUME_TEX_BIAS;
    float4 shRed = giVolumeTexR.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shGreen = giVolumeTexG.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shBlue = giVolumeTexB.SampleLevel(linearSampler, volumeTexPosition, 0);

    lightEval.diffuse.r = max(dot(shRed, shCosineResponse), 0.0f);
    lightEval.diffuse.g = max(dot(shGreen, shCosineResponse), 0.0f);
    lightEval.diffuse.b = max(dot(shBlue, shCosineResponse), 0.0f);

#ifndef SIMPLE
    lightEval.specular.r = max(dot(shRed, shReflectResponse), 0.0f);
    lightEval.specular.g = max(dot(shGreen, shReflectResponse), 0.0f);
    lightEval.specular.b = max(dot(shBlue, shReflectResponse), 0.0f);

    float NoV = saturate(dot(normal, viewVector));
    lightEval.specular *= EnvironmentBRDF(g_SpecularGlossiness, NoV, g_SpecularReflectance);
#endif

    return lightEval;
}

LightEvaluation EvaluateLights(float3 worldSpacePos, float3 normal, float3 viewVector)
{
    LightEvaluation lightEval = (LightEvaluation)0.0f;

    float3 pointLightVector = g_LocalPointLightPosition.xyz - worldSpacePos.xyz;
    float pointLightVectorDistSqr = 0.1f + dot(pointLightVector, pointLightVector); // bias / fake "area" light size
    float reverseVectorLen = rsqrt(pointLightVectorDistSqr);
    float pointLightAttenuation = reverseVectorLen * reverseVectorLen;
    float3 finalColorAttenuation = pointLightAttenuation * g_LocalPointLightColor.rgb * g_PointLightBrightness;
    lightEval = CalculateLighting(pointLightVector * reverseVectorLen, normal, viewVector);
    
    lightEval.diffuse *= finalColorAttenuation;
    lightEval.specular *= finalColorAttenuation;

    return lightEval;
}


float4 PShader(VS_OUTPUT i) : SV_Target
{
    float3 normal = normalize(i.normal.xyz);
    float4 worldSpacePos = float4(i.worldSpacePosition,1);

    float3 bumpMapSample = bumpTex.Sample(linearWrapSampler, i.uv).rgb;
    float3 albedo = GammaToLinear(albedoTex.Sample(linearWrapSampler, i.uv).rgb);

#ifdef PREPASS
    return float4(0,0,0,0);
#else

#ifndef NOSSAO
    normal = CalculateSurfaceNormal(worldSpacePos.xyz, normal, -(bumpMapSample.rg - 0.5f) * 2, i.uv);
#endif

#ifdef SIMPLE
    float ssao = 1.0f;
    float4 fog = float4(0,0,0,1);
#else
    float ssao = ssaoTex.Load(int3(i.position.xy, 0));

    float3 volumeFogUVW = VolumeTextureSpaceFromScreenSpace(float3(i.position.xy * g_ScreenSize.zw, DepthToVolumeZPos(LinearizeDepth(i.position.z))));
    float4 fog = volumetricFogTexture.SampleLevel(linearSampler, volumeFogUVW, 0);
#endif

    float3 viewVector = normalize(g_WorldEyePos.xyz - worldSpacePos.xyz);

    LightEvaluation finalLight = EvaluateSun(worldSpacePos.xyz, normal, viewVector);
    LightEvaluation ambientLight = EvaluateAmbient(worldSpacePos.xyz, normal, viewVector);

    finalLight.diffuse  += ambientLight.diffuse  * ssao;
    finalLight.specular += ambientLight.specular * ssao;

    LightEvaluation localLightsLight = EvaluateLights(worldSpacePos.xyz, normal, viewVector);

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