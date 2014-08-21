// PixelShader: GIInitSH, entry: GIInitSH
// ComputeShader: InjectSHIntoVolume, entry: InjectSHIntoVolume
// ComputeShader: CopyGI, entry: CopyGI

#include "constants.fx"

Texture2DArray<float4> InputTextureArray        : register(t0);
Texture2D<float4>      InputTextureSH           : register(t1);

Texture2D<float4>      InputTextureSHR          : register(t2);
Texture2D<float4>      InputTextureSHG          : register(t3);
Texture2D<float4>      InputTextureSHB          : register(t4);

Texture3D<float4>      InputTextureSHVolumeR    : register(t5);
Texture3D<float4>      InputTextureSHVolumeG    : register(t6);
Texture3D<float4>      InputTextureSHVolumeB    : register(t7);

RWTexture3D<float4>    OutputTextureR           : register(u0);
RWTexture3D<float4>    OutputTextureG           : register(u1);
RWTexture3D<float4>    OutputTextureB           : register(u2);

struct OutputSH
{
    float4 shRed   : SV_Target0;
    float4 shGreen : SV_Target1;
    float4 shBlue  : SV_Target2;
};

cbuffer GIConstantBuffer : register(b8)
{
    /// Position
    float4       g_InjectPosition;
}

static const float fSize = 128.0f;
static const float fPicSize = 1.0f / fSize;


float4 EvaluateDirSH(float x, float y, int face)
{
    float ix, iy, iz;
    switch (face)
    {
    case 0: // Positive X
        iz = 1.0f - (2.0f * x + 1.0f) * fPicSize;
        iy = 1.0f - (2.0f * y + 1.0f) * fPicSize;
        ix = 1.0f;
        break;

    case 1: // Negative X
        iz = -1.0f + (2.0f * x + 1.0f) * fPicSize;
        iy = 1.0f - (2.0f * y + 1.0f) * fPicSize;
        ix = -1;
        break;

    case 2: // Positive Y
        iz = -1.0f + (2.0f * y + 1.0f) * fPicSize;
        iy = 1.0f;
        ix = -1.0f + (2.0f * x + 1.0f) * fPicSize;
        break;

    case 3: // Negative Y
        iz = 1.0f - (2.0f * y + 1.0f) * fPicSize;
        iy = -1.0f;
        ix = -1.0f + (2.0f * x + 1.0f) * fPicSize;
        break;

    case 4: // Positive Z
        iz = 1.0f;
        iy = 1.0f - (2.0f * y + 1.0f) * fPicSize;
        ix = -1.0f + (2.0f * x + 1.0f) * fPicSize;
        break;

    case 5: // Negative Z
        iz = -1.0f;
        iy = 1.0f - (2.0f * y + 1.0f) * fPicSize;
        ix = 1.0f - (2.0f * x + 1.0f) * fPicSize;
        break;
    }

    float3 dir = normalize(float3(ix, iy, iz));
    float4 sh = SH2CosineResponse(dir) / PI; // wrap cosine lighting to avoid negative lobe

    return sh;
}

OutputSH GIInitSH(VS_OUTPUT_POSTFX i)
{
    OutputSH output;

    output.shRed = 0.0f;
    output.shGreen = 0.0f;
    output.shBlue = 0.0f;

    float2 position = i.position.xy;

    float fB = -1.0f + 1.0f / fSize;
    float fS = (2.0f*(1.0f - 1.0f / fSize) / (fSize - 1.0f));
    
    float x = position.x;
    float y = position.y;

    const float fV = y*fS + fB;
    const float fU = x*fS + fB;

    const float fDiffSolid = 4.0f / ((1.0f + fU*fU + fV*fV)*sqrt(1.0f + fU*fU + fV*fV));
    const float normalization = 0.9999555523386868f; // see comment at bottom of this file where it comes from

    [unroll]
    for (int face = 0; face < 6; ++face)
    {
        float4 sh = EvaluateDirSH(x, y, face);
        float3 color = InputTextureArray.Load(int4(x, y, face, 0)).rgb;

        output.shRed += fDiffSolid * color.rrrr * sh;
        output.shGreen += fDiffSolid * color.gggg * sh;
        output.shBlue += fDiffSolid * color.bbbb * sh;
    }

    output.shRed *= normalization;
    output.shGreen *= normalization;
    output.shBlue *= normalization;

    return output;
}

[numthreads(8, 8, 1)]
void InjectSHIntoVolume(uint2 dispatchThreadID : SV_DispatchThreadID, uint2 groupThreadID : SV_GroupThreadID, uint2 groupID : SV_GroupID)
{
    if (all(dispatchThreadID == uint2(0,0)))
    {
        float4 origSHR = 0.0f;
        float4 origSHG = 0.0f;
        float4 origSHB = 0.0f;

        [unroll]
        for (int y = 0; y < 8; ++y)
        [unroll]
        for (int x = 0; x < 8; ++x)
        {
            origSHR += InputTextureSHR.SampleLevel(pointSampler, float2(0.0f, 0.0f), 0, int2(x, y));
            origSHG += InputTextureSHG.SampleLevel(pointSampler, float2(0.0f, 0.0f), 0, int2(x, y));
            origSHB += InputTextureSHB.SampleLevel(pointSampler, float2(0.0f, 0.0f), 0, int2(x, y));
        }

        origSHR *= 1.0f / 64.0f;
        origSHG *= 1.0f / 64.0f;
        origSHB *= 1.0f / 64.0f;

        OutputTextureR[uint3(g_InjectPosition.xyz)] = origSHR;
        OutputTextureG[uint3(g_InjectPosition.xyz)] = origSHG;
        OutputTextureB[uint3(g_InjectPosition.xyz)] = origSHB;
    }
}


[numthreads(8, 8, 1)]
void CopyGI(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    OutputTextureR[dispatchThreadID] = InputTextureSHVolumeR[dispatchThreadID];
    OutputTextureG[dispatchThreadID] = InputTextureSHVolumeG[dispatchThreadID];
    OutputTextureB[dispatchThreadID] = InputTextureSHVolumeB[dispatchThreadID];
}


/*
Note, normalization factor was calculated from following python script:

import math

fSize = 128
fB = -1.0 + 1.0/fSize
fS = 2.0 * (1.0-1.0/fSize)/(fSize-1.0)

fWt = 0

for y in range(0,fSize):
    fV = y*fS + fB
    for x in range(0,fSize):
        fU = x*fS + fB
        fDiffSolid = 4.0/((1.0 + fU*fU + fV*fV)*pow(1.0 + fU*fU+fV*fV, 0.5))
        fWt += fDiffSolid

print (4.0 * 3.1415 / fWt * fSize * fSize / 6)
*/