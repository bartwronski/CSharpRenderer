// ComputeShader: CalculateDensity, entry: CalculateDensity
// ComputeShader: LitFogVolume, entry: LitFogVolume
// ComputeShader: ComputeScattering, entry: ComputeScattering

#include "constants.fx"
#include "perlin_noise_inc.fx"
#include "evsm_inc.fx"
#include "volumetric_fog_inc.fx"

RWTexture3D<float> OutDensityTexture            : register(u0);
RWTexture3D<float4> OutLightingTexture          : register(u1);
RWTexture3D<float4> OutScatteringTexture        : register(u2);

Texture3D<float4> InputTexture : register(t0);
Texture2D<float>  ShadowMap    : register(t1);

Texture3D<float4> giVolumeTexR              : register(t5);
Texture3D<float4> giVolumeTexG              : register(t6);
Texture3D<float4> giVolumeTexB              : register(t7);

Texture3D<float4> previousFrameAccumulation : register(t8);

float CalculateDensityFunction(float3 worldSpacePos)
{
    // some procedural effect
    return turbulence(worldSpacePos * 0.35f + float3(g_Time * 0.1f, 0, 0), 2) * exp(-worldSpacePos.y * 0.5f);
}

[numthreads(8, 4, 4)]
void CalculateDensity(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    float3 screenCoords = ScreenCoordsFromThreadID(dispatchThreadID);
    float linearDepth = VolumeZPosToDepth(screenCoords.z);
    float3 worldSpacePos = WorldPositionFromCoords(screenCoords.xy, linearDepth);

    OutDensityTexture[dispatchThreadID] = CalculateDensityFunction(worldSpacePos);
}

// some generated Poisson-like distribution, check out my: https://github.com/bartwronski/PoissonSamplingGenerator
static const uint SAMPLE_NUM = 8;
static const float2 POISSON_SAMPLES[SAMPLE_NUM] =
{
    float2(0.228132254148f, 0.67232428631f),
    float2(0.848556554824f, 0.135723477704f),
    float2(0.74820789575f, 0.63965073852f),
    float2(0.472544801767f, 0.351474129111f),
    float2(0.962881642535f, 0.387342871273f),
    float2(0.0875977149838f, 0.896250211998f),
    float2(0.203231652569f, 0.12436704431f),
    float2(0.56452806916f, 0.974024350484f),
};

float3 GetSunLightingRadiance(float3 worldPosition, float3 viewDir, float anisotropy)
{
    float4 lightSpacePos = mul(g_ShadowViewProjMatrix, float4(worldPosition, 1));
    lightSpacePos /= lightSpacePos.w;

    float4 smSample = ShadowMap.SampleLevel(linearSampler, lightSpacePos.xy*float2(0.5, -0.5) + 0.5, 0.0f);
    float shadow = saturate(calculateShadowESM(smSample, lightSpacePos.z)*1.1f - 0.1f); // bias a bit to make sure we reach 1 (ESM precision...)
    float sunPhaseFunction = GetPhaseFunction(dot(g_LightDir.xyz, viewDir), anisotropy);
    return shadow * g_LightBrightness.xxx * g_LightColor.rgb * sunPhaseFunction;
}

float3 GetAmbient(float3 worldPosition, float3 viewDir, float anisotropy)
{
    float3 volumeTexPosition = (worldPosition - g_WorldBoundsMin.xyz) * g_WorldBoundsInvRange.xyz * GI_VOLUME_TEX_SCALE + GI_VOLUME_TEX_BIAS;
    float4 shRed = giVolumeTexR.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shGreen = giVolumeTexG.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shBlue = giVolumeTexB.SampleLevel(linearSampler, volumeTexPosition, 0);
    float4 shPhaseFunction = GetRotatedPhaseFunctionSH(viewDir, anisotropy);

    float3 lighting = 0.0f;
    lighting.r = max(dot(shPhaseFunction, shRed), 0);
    lighting.g = max(dot(shPhaseFunction, shGreen), 0);
    lighting.b = max(dot(shPhaseFunction, shBlue), 0);
    
    return lighting;
}

float3 GetLocalLightsRadiance(float3 worldPosition, float3 viewDir, float anisotropy)
{
    float3 pointLightVector = g_LocalPointLightPosition.xyz - worldPosition.xyz;
    float pointLightVectorLenSquared = 0.1f + dot(pointLightVector, pointLightVector); // bias / fake "area" light to avoid singularity
    float reverseVectorLen = rsqrt(pointLightVectorLenSquared);
    float pointLightPhaseFunction = GetPhaseFunction(dot(pointLightVector * reverseVectorLen, viewDir), anisotropy);
    float pointLightAttenuation = reverseVectorLen * reverseVectorLen;

    return pointLightAttenuation*g_LocalPointLightColor.rgb * g_PointLightBrightness * pointLightPhaseFunction;
}

[numthreads(4, 4, 4)]
void LitFogVolume(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    bool reprojectionOn = g_VolumetricFogReprojectionOn > 0.5f;

    float2 currFrameJitter = reprojectionOn ? (POISSON_SAMPLES[(g_FrameNumber + dispatchThreadID.x + dispatchThreadID.y * 2) % SAMPLE_NUM] - 0.5f) : 0.0f;

    float3 screenCoords = ScreenCoordsFromThreadID(max((float3)dispatchThreadID + float3(currFrameJitter, 0.0f), 0.0f));
    float linearDepth = VolumeZPosToDepth(screenCoords.z);
    float layerThickness = VolumeZPosToDepth(screenCoords.z + 1.0f / VOLUME_DEPTH) - linearDepth;

    // TEMPORAL part, use coords with no offset for reprojection!
    float3 screenCoordsRepr = ScreenCoordsFromThreadID((float3)dispatchThreadID);
    float3 worldSpacePosRepr = WorldPositionFromCoords(screenCoordsRepr.xy, linearDepth);
    float4 coordsPrevFrame = mul(g_ViewProjMatrixPrevFrame, float4(worldSpacePosRepr, 1.0f));
    float reprojectionValid = reprojectionOn;
    reprojectionValid *= coordsPrevFrame.w > 0.0f;
    coordsPrevFrame.xyz /= coordsPrevFrame.www; // TODO optimize, divide of z is unnecessary and then no need to linearize, wrote this way for clarity
    coordsPrevFrame.xy = coordsPrevFrame.xy * float2(0.5f, -0.5f) + float2(0.5f, 0.5f);
    coordsPrevFrame.xyz = float3(coordsPrevFrame.xy, DepthToVolumeZPos(LinearizeDepth(coordsPrevFrame.z)));
    reprojectionValid *= dot(coordsPrevFrame.xyz - saturate(coordsPrevFrame.xyz), 1) == 0.0f;
    reprojectionValid *= 0.85f;
    float4 fogPrevFrame = previousFrameAccumulation.SampleLevel(linearSampler, VolumeTextureSpaceFromScreenSpace(coordsPrevFrame.xyz), 0);
    // end TEMPORAL part


    float3 worldSpacePos = WorldPositionFromCoords(screenCoords.xy, linearDepth);

    float dustDensity = CalculateDensityFunction(worldSpacePos); //InputTexture[dispatchThreadID].x;
    float scattering = g_VolumetricFogFinalScattering * (0.1f + dustDensity) * layerThickness;
    float absorbtion = 0.0f * layerThickness;
    float anisotropy = g_VolumetricFogPhaseAniso;
    float3 fogAlbedo = float3(1.0f, 1.0f, 0.9f);
    float3 viewDir = normalize(worldSpacePos - g_WorldEyePos.xyz);

    float3 lighting = 0.0f;

    lighting += GetSunLightingRadiance(worldSpacePos, viewDir, anisotropy);
    lighting += GetAmbient(worldSpacePos, viewDir, anisotropy);
    lighting += GetLocalLightsRadiance(worldSpacePos, viewDir, anisotropy);

    lighting *= fogAlbedo;

    float4 finalOutValue = float4(lighting * scattering, scattering + absorbtion);

    float4 fogWithReprojection = lerp(finalOutValue, fogPrevFrame, reprojectionValid);
    OutLightingTexture[dispatchThreadID].rgba = fogWithReprojection;
}

// One step of numerical solution to the light scattering equation 
float4 AccumulateScattering(in float4 colorAndDensityFront, in float4 colorAndDensityBack)
{
    // rgb = light in-scattered accumulated so far, a = accumulated scattering coefficient    
    float3 light = colorAndDensityFront.rgb + saturate(exp(-colorAndDensityFront.a)) * colorAndDensityBack.rgb;
    return float4(light.rgb, colorAndDensityFront.a + colorAndDensityBack.a);
}

// Writing out final scattering values
void WriteOutput(in uint3 pos, in float4 colorAndDensity)
{
    // final value rgb = light in-scattered accumulated so far, a = scene light extinction caused by out-scattering
    float4 finalValue = float4(colorAndDensity.rgb, saturate(exp(-colorAndDensity.a)));
    OutScatteringTexture[pos].rgba = finalValue;
}


[numthreads(8, 8, 1)]
void ComputeScattering(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    float4 currentSliceValue = InputTexture[uint3(dispatchThreadID.xy, 0)].rgba;
    WriteOutput(uint3(dispatchThreadID.xy, 0), currentSliceValue);

    [loop]
    for (uint z = 1; z < (uint)VOLUME_DEPTH; z++)
    {
        float4 nextValue = InputTexture[uint3(dispatchThreadID.xy, z)].rgba;
        currentSliceValue = AccumulateScattering(currentSliceValue, nextValue);
        WriteOutput(uint3(dispatchThreadID.xy, z), currentSliceValue);
    }

}

