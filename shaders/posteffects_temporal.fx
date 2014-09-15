// PixelShader:  ResolveTemporalMotionBased, entry: ResolveTemporalMotionBased
// PixelShader:  ResolveTemporalHistoryAccumulation, entry: ResolveTemporalMotionBased, defines: HISTORY_ACCUMULATION

#include "constants.fx"

Texture2D<float4> InputTextureCurrent           : register(t0);
Texture2D<float4> InputTextureHistory           : register(t1);
Texture2D<float2> InputTextureMotionVectors     : register(t2);
Texture2D<float2> InputTextureMotionVectorsPrev : register(t3);


float2 FindLumaMinMaxSurroundings(float2 uv)
{
    float minL = 10000.0f;
    float maxL = 0.0f;

    [unroll]
    for (int y = -1; y < 2; ++y)
    [unroll]
    for (int x = -1; x < 2; ++x)
    {
        float luma = dot(InputTextureCurrent.SampleLevel(pointSampler, uv, 0, int2(x, y)).rgb, LUMINANCE_VECTOR);
        minL = min(minL, luma);
        maxL = max(maxL, luma);
    }
    return float2(minL, maxL);
}

float4 ResolveTemporalMotionBased(VS_OUTPUT_POSTFX i) : SV_Target
{
    float3 textureSampleColor = InputTextureCurrent.SampleLevel(pointSampler, i.uv, 0).rgb;
    float2 motionVector = InputTextureMotionVectors.SampleLevel(pointSampler, i.uv, 0).rg;
    float3 textureSamplePrevFrame = InputTextureHistory.SampleLevel(linearSampler, i.uv + motionVector, 0).rgb;
    float2 motionVectorPrevFrame = InputTextureMotionVectorsPrev.SampleLevel(linearSampler, i.uv + motionVector, 0).rg;

    float2 motionVectorDiff = motionVector - motionVectorPrevFrame;
    
    // magic number, works well in high fps, 50+
    float weightCoeff = 512.0f;
    float weight = saturate(dot(abs(motionVectorDiff), weightCoeff));
    
#ifdef HISTORY_ACCUMULATION
    float2 minMax = FindLumaMinMaxSurroundings(i.uv);
    float lumaAccumulation = max(dot(textureSamplePrevFrame, LUMINANCE_VECTOR), 0.00001f);
    float mult = clamp(lumaAccumulation, minMax.x, minMax.y) * rcp(lumaAccumulation);
    textureSamplePrevFrame *= mult;

    // magic number, works well in high fps, 50+
    float finalWeight = saturate(0.1f + weight);
#else
    float finalWeight = 0.5f + 0.5f * weight;
#endif

    return float4(lerp(textureSamplePrevFrame, textureSampleColor, finalWeight), 0.0f);
}
