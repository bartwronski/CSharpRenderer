#ifndef __INCLUDE_CONSTANTS
#define __INCLUDE_CONSTANTS

#include "sampler_states.fx"
#include "shader_fast_math.fx"

cbuffer GlobalFrameBuffer : register(b0) // Global
{
    float4      g_FrameRandoms;
    float       g_Time;
    int         g_FrameNumber;
    float       g_GPUDebugOn;
    float       g_GPUDebugOverridePositionEnable;
    float4      g_GPUDebugOverridePositionXYZ;
};

cbuffer GlobalViewportBuffer : register(b1) // Global
{
    float4x4    g_ViewProjMatrixPrevFrame;

    float4      g_ScreenSize;
    float4      g_ScreenSizeHalfRes;

    float4      g_WorldEyePos;
    float4      g_EyeXAxis;
    float4      g_EyeYAxis;
    float4      g_EyeZAxis;

    float4      g_ReprojectInfo;
    float4      g_ReprojectInfoFromInt;   // added half pixel offset to center it

    float4      g_WorldBoundsMin;
    float4      g_WorldBoundsMax;
    float4      g_WorldBoundsInvRange;

    float       g_zNear;
    float       g_zFar;

    float       g_TemporalAA;             // Param, Default: 1.0, Range:0.0-1.0, Linear
    float       g_FrameJitter;
    float       g_ReprojectDepthScale;    // Scripted
    float       g_ReprojectDepthBias;     // Scripted

    /* BEGINSCRIPT
    g_ReprojectDepthScale = (g_zFar - g_zNear) / (-g_zFar * g_zNear)
    g_ReprojectDepthBias = g_zFar / (g_zFar * g_zNear)
    ENDSCRIPT */
};

#include "shader_debug_inc.fx"

cbuffer CurrentViewport : register(b2)
{
    float4x4    g_ViewMatrix;
    float4x4    g_ProjMatrix;
    float4x4    g_ViewProjMatrix;
    float4x4    g_InvViewProjMatrix;
};

cbuffer ForwardPassBuffer : register(b3)
{
    float4x4    g_ShadowViewProjMatrix;
    float4x4    g_ShadowInvViewProjMatrix;
    float4      g_LightDir;
    float4      g_LightColor;
    float4      g_LocalPointLightPosition;
    float4      g_LocalPointLightColor;

    /// Lighting
    float       g_LightBrightness;        // Param, Default: 2.0, Range:0.0-4.0, Gamma
    float       g_PointLightBrightness;   // Param, Default: 4.0, Range:0.0-16.0, Gamma
    float       g_Roughness;              // Param, Default: 0.7, Range:0.02-0.98, Linear
    float       g_SpecularReflectance;    // Param, Default: 0.03, Range:0.01-0.98, Linear
    float       g_SpecularGlossiness;     // Scripted

    /* BEGINSCRIPT
    a2 = g_Roughness * g_Roughness
    specPower = (1 / a2 - 1.0)*2.0
    g_SpecularGlossiness = math.log(specPower, 2) / 19.0
    ENDSCRIPT */

};

cbuffer PostEffects : register(b4)
{
    /// Bokeh
    float       g_CocScale;                     // Scripted
    float       g_CocBias;                      // Scripted
    float       g_FocusPlane;                   // Param, Default: 2.0, Range:0.0-10.0, Linear
    float       g_DofCoCScale;                  // Param, Default: 0.0, Range:0.0-32.0, Linear
    float       g_DebugBokeh;                   // Param, Default: 0.0, Range:0.0-1.0, Linear

    float       g_VolumetricFogRange;           // Param, Default: 100.0, Range:1.0-256.0, Linear
    float       g_VolumetricFogScattering;      // Param, Default: 1.0, Range:0.0-10.0, Linear
    float       g_VolumetricFogPhaseAniso;      // Param, Default: 0.4, Range:0.0-0.99, Linear
    float       g_VolumetricFogReprojectionOn;  // Param, Default: 1.0, Range:0.0-1.0, Linear
    float       g_VolumetricFogFinalScattering; // Scripted

    /* BEGINSCRIPT
    focusPlaneShifted = g_FocusPlane + g_zNear
    cameraCoCScale = g_DofCoCScale * g_ScreenSize_y / 720.0             -- depends on focal length & aperture, rescale it to screen res
    g_CocBias = cameraCoCScale * (1.0 - focusPlaneShifted / g_zNear)
    g_CocScale = cameraCoCScale * focusPlaneShifted * (g_zFar - g_zNear) / (g_zFar * g_zNear)
    g_VolumetricFogFinalScattering = g_VolumetricFogScattering * 0.06
    ENDSCRIPT */
};

struct VS_OUTPUT_POSTFX
{
    float4 position             : SV_POSITION;
    float2 uv                   : TEXCOORD0;
};

static const float3 LUMINANCE_VECTOR = float3(0.299f, 0.587f, 0.114f);


#define GI_VOLUME_RESOLUTION_X 64.0 // GlobalDefine
#define GI_VOLUME_RESOLUTION_Y 32.0 // GlobalDefine
#define GI_VOLUME_RESOLUTION_Z 64.0 // GlobalDefine

#define GI_VOLUME_TEX_SCALE float3((GI_VOLUME_RESOLUTION_X-1)/GI_VOLUME_RESOLUTION_X, (GI_VOLUME_RESOLUTION_Y-1)/GI_VOLUME_RESOLUTION_Y, (GI_VOLUME_RESOLUTION_Z-1)/GI_VOLUME_RESOLUTION_Z)
#define GI_VOLUME_TEX_BIAS float3(0.5f/GI_VOLUME_RESOLUTION_X, 0.5f/GI_VOLUME_RESOLUTION_Y, 0.5f/GI_VOLUME_RESOLUTION_Z)


float CalculateCoc(float postProjDepth)
{
    float CoC = g_CocScale * postProjDepth + g_CocBias;
    return CoC;
}

float LinearizeDepth(float depth)
{
    return rcp(depth * g_ReprojectDepthScale + g_ReprojectDepthBias);
    //return -g_zFar * g_zNear / (depth * (g_zFar - g_zNear) - g_zFar);
}

float3 GammaToLinear(float3 input)
{
    return pow(max(input,0.0f), 2.2f);
}

float3 LinearToGamma(float3 input)
{
    return pow(max(input,0.0f), 1.0f/2.2f);
}

float rand(float2 co)
{
    return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * (43758.5453));
}

static const float PI = 3.14159265f;
static const float SH0 = 0.5f * sqrt(1.0f / PI);
static const float SH1 = 0.5f * sqrt(3.0f / PI);

static const float SH0COS = PI;
static const float SH1COS = PI * (2.0f / 3.0f);

float4 SH2DirectionResponse(float3 dir)
{
    return float4(SH0, SH1, SH1, SH1) * float4(1.0f, dir.y, dir.z, dir.x);
}

// integrates over hemisphere, so divide by PI for normalization
float4 SH2CosineResponse(float3 dir)
{
    return SH2DirectionResponse(dir) * float4(SH0COS, SH1COS, SH1COS, SH1COS);
}

#endif