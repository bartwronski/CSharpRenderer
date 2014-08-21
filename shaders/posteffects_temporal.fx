// PixelShader:  ResolveTemporalMotionBased, entry: ResolveTemporalMotionBased
// PixelShader:  ResolveTemporalHistoryAccumulation, entry: ResolveTemporalMotionBased, defines: HISTORY_ACCUMULATION

#include "constants.fx"

Texture2D<float4> InputTextureCurrent           : register(t0);
Texture2D<float4> InputTextureHistory           : register(t1);
Texture2D<float2> InputTextureMotionVectors     : register(t2);
Texture2D<float2> InputTextureMotionVectorsPrev : register(t3);


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
    // magic number, works well in high fps, 50+
    float finalWeight = saturate(0.25f + weight);
#else
    float finalWeight = 0.5f + 0.5f * weight;
#endif

    return float4(lerp(textureSamplePrevFrame, textureSampleColor, finalWeight), 0.0f);
}
