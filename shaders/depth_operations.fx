
// PixelShader: ResolveMotionVectors, entry: ResolveMotionVectors
// PixelShader: LinearizeDepth, entry: PShaderLinearizeDepth
// PixelShader: DownsampleDepthNormals, entry: PShaderDownsampleDepthNormals
// PixelShader: OutputBilateralOffsets, entry: PShaderOutputBilateralOffsets
// PixelShader: BilateralUpsample, entry: PShaderBilateralUpsample

#include "constants.fx"

Texture2D<float4> InputTexture                  : register(t0);
Texture2D<float4> InputTextureNormals           : register(t1);
Texture2D<float>  InputTextureLowRes            : register(t2);

float PShaderLinearizeDepth(VS_OUTPUT_POSTFX i) : SV_Target
{
    float textureSample = InputTexture.SampleLevel(pointSampler, i.uv, 0).r;
    return LinearizeDepth(textureSample);
}


float4 ResolveMotionVectors(VS_OUTPUT_POSTFX i) : SV_Target
{
    float textureSampleDepth = InputTexture.SampleLevel(pointSampler, i.uv, 0).r;
    float2 screenPos = float2(i.uv * float2(2, -2) + float2(-1, 1));
    float4 coordsPrevFrame = mul(g_ReprojectProjToPrevFrame, float4(screenPos, textureSampleDepth, 1.0f));
    coordsPrevFrame /= coordsPrevFrame.wwww;
    float2 uvMotionVector = coordsPrevFrame.xy - screenPos;
    uvMotionVector = uvMotionVector * float2(0.5f, -0.5f);

    return float4(uvMotionVector, 0.0f, 0.0f);
}

struct OutputDepthNormals
{
    float  depth   : SV_Target0;
    float4 normal  : SV_Target1;
};

OutputDepthNormals PShaderDownsampleDepthNormals(VS_OUTPUT_POSTFX i)
{ 
    OutputDepthNormals outDN;
    float4 textureSamples = InputTexture.GatherRed(pointSampler, i.uv, 0);
    float minDepth = min(min(textureSamples.r, textureSamples.g), min(textureSamples.b, textureSamples.a));
    float4 textureWeights = textureSamples * rcp(max(minDepth, 0.0000001f));
    textureWeights = saturate(1.0f - 10.0f * abs(textureWeights - 1.0f));

    float4 normalSampleCurrent = 0.0f;
    float4 normalSampleAccum = 0.0f;
    float roughnessAccum = 0.0f;

    normalSampleCurrent = InputTextureNormals.SampleLevel(pointSampler, i.uv + g_ScreenSize.zw * GATHER_UOFFSET_X - g_ScreenSize.zw * 0.5f, 0);
    normalSampleAccum += float4(normalSampleCurrent.rgb, 1.0f) * textureWeights.x;
    roughnessAccum += normalSampleCurrent.a * textureWeights.x;

    normalSampleCurrent = InputTextureNormals.SampleLevel(pointSampler, i.uv + g_ScreenSize.zw * GATHER_UOFFSET_Y - g_ScreenSize.zw * 0.5f, 0);
    normalSampleAccum += float4(normalSampleCurrent.rgb, 1.0f) * textureWeights.y;
    roughnessAccum += normalSampleCurrent.a * textureWeights.y;

    normalSampleCurrent = InputTextureNormals.SampleLevel(pointSampler, i.uv + g_ScreenSize.zw * GATHER_UOFFSET_Z - g_ScreenSize.zw * 0.5f, 0);
    normalSampleAccum += float4(normalSampleCurrent.rgb, 1.0f) * textureWeights.z;
    roughnessAccum += normalSampleCurrent.a * textureWeights.z;

    normalSampleCurrent = InputTextureNormals.SampleLevel(pointSampler, i.uv + g_ScreenSize.zw * GATHER_UOFFSET_W - g_ScreenSize.zw * 0.5f, 0);
    normalSampleAccum += float4(normalSampleCurrent.rgb, 1.0f) * textureWeights.w;
    roughnessAccum += normalSampleCurrent.a * textureWeights.w;

    float rcpWeight = rcp(max(normalSampleAccum.a, 0.00001f));
    normalSampleAccum *= rcpWeight;
    roughnessAccum *= rcpWeight;

    outDN.depth = minDepth;

    // todo: best-fit normals?
    outDN.normal = float4(normalize(normalSampleAccum.rgb) * 0.5f + 0.5f, roughnessAccum);

    return outDN;
}

void ProcessQuad(float4 lowResSamples, float origDepth, float2 quadOffsets, inout float2 offsets, inout float minDepthDiff)
{
    float4 depthDiff = abs(lowResSamples - origDepth);

    if (depthDiff.x < minDepthDiff)
    {
        minDepthDiff = depthDiff.x;
        offsets = GATHER_SOFFSET_X * 0.5f + quadOffsets;
    }

    if (depthDiff.y < minDepthDiff)
    {
        minDepthDiff = depthDiff.y;
        offsets = GATHER_SOFFSET_Y * 0.5f + quadOffsets;
    }

    if (depthDiff.z < minDepthDiff)
    {
        minDepthDiff = depthDiff.z;
        offsets = GATHER_SOFFSET_Z * 0.5f + quadOffsets;
    }

    if (depthDiff.w < minDepthDiff)
    {
        minDepthDiff = depthDiff.w;
        offsets = GATHER_SOFFSET_W * 0.5f + quadOffsets;
    }
}

float2 PShaderOutputBilateralOffsets(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 lowResSamples = InputTextureLowRes.GatherRed(pointSampler, i.uv);
    float currentDepth = InputTexture.SampleLevel(pointSampler, i.uv, 0).r;
    float4 depthDiff = abs(lowResSamples - currentDepth);
    uint2 vpos = (uint2)i.position.xy;
    float2 fullResOffsets = float2((vpos.x & 1) ? 0.25 : -0.25, (vpos.y & 1) ? 0.25 : -0.25);
    float4 depthDiffNormalized = depthDiff * rcp(max(currentDepth, 0.000001f));
    const float acceptanceThreshold = 0.03f;

    bool4 test = depthDiffNormalized < acceptanceThreshold;
    // first, check if bilinear is ok
    if (all(test))
        return float2(0.0f, 0.0f);

    // then check edges if bilinear on 1 edge is ok
    // w z
    // x y
    if (all(test.wz))
    {
        return float2(0.0f, -0.5f + fullResOffsets.y) * BILAT_PACK;
    }
    if (all(test.xy))
    {
        return float2(0.0f, 0.5f + fullResOffsets.y) * BILAT_PACK;
    }
    if (all(test.wx))
    {
        return float2(-0.5f + fullResOffsets.x, 0.0f) * BILAT_PACK;
    }
    if (all(test.zy))
    {
        return float2(0.5f + fullResOffsets.x, 0.0f) * BILAT_PACK;
    }
    

    float smallestDepthDiff = 10000.0f;
    float2 smallResOffsets = 0.0f;
    
    // finally, find closest point
    ProcessQuad(lowResSamples, currentDepth, float2(0.0f, 0.0f), smallResOffsets, smallestDepthDiff);

#if 0
    // optional, high cost - find in larger neighbourhood
    lowResSamples = InputTextureLowRes.GatherRed(pointSampler, i.uv, int2(-2,0));
    ProcessQuad(lowResSamples, currentDepth, float2(-2.0f, 0.0f), smallResOffsets, smallestDepthDiff);
    lowResSamples = InputTextureLowRes.GatherRed(pointSampler, i.uv, int2(0,-2));
    ProcessQuad(lowResSamples, currentDepth, float2(0.0f, -2.0f), smallResOffsets, smallestDepthDiff);
    lowResSamples = InputTextureLowRes.GatherRed(pointSampler, i.uv, int2(2,0));
    ProcessQuad(lowResSamples, currentDepth, float2(2.0f, 0.0f), smallResOffsets, smallestDepthDiff);
    lowResSamples = InputTextureLowRes.GatherRed(pointSampler, i.uv, int2(0,2));
    ProcessQuad(lowResSamples, currentDepth, float2(0.0f, 2.0f), smallResOffsets, smallestDepthDiff);
#endif

    return (fullResOffsets + smallResOffsets) * BILAT_PACK;
}

float4 PShaderBilateralUpsample(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 upSampledSignal = InputTexture.SampleLevel(linearSampler, GetBilateralUV(i.uv), 0);
    return upSampledSignal;
}
