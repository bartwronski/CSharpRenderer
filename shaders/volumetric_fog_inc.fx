
#define VOLUME_WIDTH  160.0 // GlobalDefine
#define VOLUME_HEIGHT 90.0  // GlobalDefine
#define VOLUME_DEPTH  128.0 // GlobalDefine

#define FOG_RANGE g_VolumetricFogRange
#define DEPTH_PACK_EXPONENT 2.0

float3 ScreenCoordsFromThreadID(uint3 threadID)
{
    return ((float3)threadID.xyz)*float3(2.0f / VOLUME_WIDTH, -2.0f / VOLUME_HEIGHT, 1.0f / VOLUME_DEPTH) + float3(-1, 1, 0);
}

float3 ScreenCoordsFromThreadID(float3 threadID)
{
    return (threadID.xyz + 0.5f)*float3(2.0f / VOLUME_WIDTH, -2.0f / VOLUME_HEIGHT, 1.0f / VOLUME_DEPTH) + float3(-1, 1, 0);
}

float3 WorldPositionFromCoords(float2 screenPos, float linearDepth)
{
    float3 eyeRay = (g_EyeXAxis.xyz * screenPos.xxx +
        g_EyeYAxis.xyz * screenPos.yyy +
        g_EyeZAxis.xyz);

    float3 viewRay = eyeRay * linearDepth;
    float3 worldPos = g_WorldEyePos.xyz + viewRay;

    return worldPos;
}

float3 GetViewVector(float2 screenPos, float linearDepth)
{
    float3 eyeRay = (g_EyeXAxis.xyz * screenPos.xxx +
        g_EyeYAxis.xyz * screenPos.yyy +
        g_EyeZAxis.xyz);

    float3 viewRay = eyeRay * linearDepth;

    return viewRay;
}

float VolumeZPosToDepth(float volumePosZ)
{
    return pow(abs(volumePosZ), DEPTH_PACK_EXPONENT) * FOG_RANGE;
}

float DepthToVolumeZPos(float depth)
{
    return pow(abs(depth / FOG_RANGE), 1.0f / DEPTH_PACK_EXPONENT);
}

float3 VolumeTextureSpaceFromScreenSpace(float3 volumeSS)
{
    return saturate(volumeSS) * float3((VOLUME_WIDTH - 1) / VOLUME_WIDTH, (VOLUME_HEIGHT - 1) / VOLUME_HEIGHT, (VOLUME_DEPTH - 1) / VOLUME_DEPTH) + float3(0.5f / VOLUME_WIDTH, 0.5f / VOLUME_HEIGHT, 0.5f / VOLUME_DEPTH);
}

float GetPhaseFunction(in float cosPhi, float gFactor)
{
    float gFactor2 = gFactor*gFactor;
    return (1 - gFactor2) / pow(abs(1 + gFactor2 - 2 * gFactor * cosPhi), 1.5f) * (1.0f / 4.0f * PI);
}

float4 GetRotatedPhaseFunctionSH(float3 dir, float g)
{
    // Returns properly rotated spherical harmonics (from Henyey-Greenstein Zonal SH expansion) 
    // for given view vector direction.
    return float4(1.0f, dir.y, dir.z, dir.x) * float4(1.0f, g, g, g);
}
