// VertexShader: VertexScene, entry: VShader
// VertexShader: VertexShadow, entry: VertexShadow
// PixelShader:  PixelScene, entry: PShader
// PixelShader:  PixelSceneNoSSAO, entry: PShader, defines: NOSSAO

Texture2D<float> shadowMap                  : register(t0);
Texture2D<float> ssaoTex                    : register(t1);
Texture2D<float4> albedoTex                 : register(t4);

Texture3D<float4> giVolumeTexR              : register(t5);
Texture3D<float4> giVolumeTexG              : register(t6);
Texture3D<float4> giVolumeTexB              : register(t7);

#include "constants.fx"
#include "evsm_inc.fx"

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
    float4 screenSpacePosition = mul(viewProjMatrix, position);
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
    float4 screenSpacePosition = mul(shadowViewProjMatrix, position);

    vo.position = screenSpacePosition;

    return vo;
}

float3 CalculateLighting(float3 albedoGammaSpace, float3 lightVector, float3 normalVector, float3 lightAttenuation, float3 ambient)
{
    float3 albedo = GammaToLinear(albedoGammaSpace);
    float3 NdotL = dot(lightVector,normalVector);

    float3 directLighting = saturate(NdotL) * lightAttenuation;

    return albedo * (directLighting + ambient);
}

float4 PShader(VS_OUTPUT i) : SV_Target
{
    float3 normal = normalize(i.normal.xyz);
    float4 worldSpacePos = float4(i.worldSpacePosition,1);
    float4 lightSpacePos = mul(shadowViewProjMatrix,worldSpacePos);
    lightSpacePos/= lightSpacePos.w;

    float4 smSample = shadowMap.SampleLevel(linearSampler, lightSpacePos.xy*float2(0.5, -0.5) + 0.5, 0.0f);
    float shadow = calculateShadowEVSM(smSample, lightSpacePos.z);

#ifdef NOSSAO
    float ssao = 1.0f;
#else
    float ssao = ssaoTex.Load(int3(i.position.xy, 0));
#endif

    float3 gi = 0.0f;
    float4 shCosineResponse = SH2CosineResponse(normal);
    float3 volumeTexPosition = (i.worldSpacePosition + i.normal.xyz * 0.5f - worldBoundsMin.xyz) * worldBoundsInvRange.xyz * float3(63.0f / 64.0f, 31.0f / 32.0f, 63.0f / 64.0f);
    float4 shRed = giVolumeTexR.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shGreen = giVolumeTexG.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shBlue = giVolumeTexB.SampleLevel(linearSampler, volumeTexPosition, 0);

    gi.r = max(dot(shRed, shCosineResponse) / PI, 0.0f);
    gi.g = max(dot(shGreen, shCosineResponse) / PI, 0.0f);
    gi.b = max(dot(shBlue, shCosineResponse) / PI, 0.0f);

    float3 albedo = albedoTex.Sample(linearWrapSampler, i.uv ).xyz;

    float3 lighting = CalculateLighting(albedo, lightDir.xyz, normal, shadow*lightBrightness, gi * ssao);
    
    return float4(lighting,1.0f);
}
