#ifndef __INCLUDE_CONSTANTS
#define __INCLUDE_CONSTANTS

#include "sampler_states.fx"
#include "shader_fast_math.fx"

cbuffer GlobalViewportBuffer : register(b0) // Global
{
    float4x4    viewProjMatrixPrevFrame;

    float4      screenSize;
    float4      screenSizeHalfRes;

    float4      worldEyePos;

    float4      reprojectInfo;
    float4      reprojectInfoFromInt;   // added half pixel offset to center it

    float4      worldBoundsMin;
    float4      worldBoundsMax;
    float4      worldBoundsInvRange;

    float       zNear;
    float       zFar;

    float       temporalAA;             // Param, Default: 1.0, Range:0.0-1.0, Linear
};

cbuffer CurrentViewport : register(b1)
{
    float4x4    viewMatrix;
    float4x4    projMatrix;
    float4x4    viewProjMatrix;
    float4x4    invViewProjMatrix;
};

cbuffer ForwardPassBuffer : register(b2)
{
    float4x4    shadowViewProjMatrix;
    float4x4    shadowInvViewProjMatrix;
    float4      lightDir;

    /// Lighting
    float       lightBrightness;        // Param, Default: 2.0, Range:0.0-4.0, Gamma
};

cbuffer PostEffects : register(b3)
{
    /// Bokeh
    float       cocScale;               // Scripted
    float       cocBias;                // Scripted
    float       focusPlane;             // Param, Default: 2.0, Range:0.0-10.0, Linear
    float       dofCoCScale;            // Param, Default: 0.0, Range:0.0-32.0, Linear
    float       debugBokeh;             // Param, Default: 0.0, Range:0.0-1.0, Linear
    /* BEGINSCRIPT
    focusPlaneShifted = focusPlane + zNear
    cameraCoCScale = dofCoCScale * screenSize_y / 720.0             -- depends on focal length & aperture, rescale it to screen res
    cocBias = cameraCoCScale * (1.0 - focusPlaneShifted / zNear)
    cocScale = cameraCoCScale * focusPlaneShifted * (zFar - zNear) / (zFar * zNear)
    ENDSCRIPT */
};

struct VS_OUTPUT_POSTFX
{
    float4 position             : SV_POSITION;
    float2 uv                   : TEXCOORD0;
};

static const float3 LUMINANCE_VECTOR = float3(0.299f, 0.587f, 0.114f);

float CalculateCoc(float postProjDepth)
{
    float CoC = cocScale * postProjDepth + cocBias;
    return CoC;
}

float LinearizeDepth(float depth)
{
    // unoptimal
    return -zFar * zNear / (depth * (zFar - zNear) - zFar);
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