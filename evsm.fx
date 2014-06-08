// PixelShader:  ReconstructEVSM, entry: ReconstructEVSM
// PixelShader:  BlurEVSMHorizontal, entry: BlurEVSM, defines: HORIZONTAL
// PixelShader:  BlurEVSMVertical, entry: BlurEVSM

#include "evsm_inc.fx"
#include "constants.fx"

Texture2D<float>  InputTextureDepth             : register(t0);
Texture2D<float4> InputTextureEVSM              : register(t1);

float4 ReconstructEVSM(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 depth = InputTextureDepth.GatherRed(pointSampler, i.uv);
    
    float2 warpedDepth[4];
    warpedDepth[0] = warpDepth(depth.x);
    warpedDepth[1] = warpDepth(depth.y);
    warpedDepth[2] = warpDepth(depth.z);
    warpedDepth[3] = warpDepth(depth.w);
    
    float4 outputEVSM[4];
    outputEVSM[0] = float4(warpedDepth[0], warpedDepth[0] * warpedDepth[0]);
    outputEVSM[1] = float4(warpedDepth[1], warpedDepth[1] * warpedDepth[1]);
    outputEVSM[2] = float4(warpedDepth[2], warpedDepth[2] * warpedDepth[2]);
    outputEVSM[3] = float4(warpedDepth[3], warpedDepth[3] * warpedDepth[3]);

    float4 finalOutput = outputEVSM[0] + outputEVSM[1] + outputEVSM[2] + outputEVSM[3];
    finalOutput *= 0.25f;

    return finalOutput;
}

// generated in Python, 6 normalized weights, 11 samples total
static const float gaussianWeights[6] = {
    0.12801535629105684f,
    0.12299580237376932f,
    0.10908749075572069f,
    0.08931328345781858f,
    0.06750152753344589f,
    0.047094217733717074f };

float4 BlurEVSM(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 sum = 0.0f;
    
    [unroll]
    for (int x = -5; x < 6; ++x)
    {
        // unoptimal, could exploit bilinear filtering
        sum += gaussianWeights[abs(x)] * InputTextureEVSM.SampleLevel(pointSampler, i.uv, 0,
#ifdef HORIZONTAL
            int2(x, 0)
#else
            int2(0, x)
#endif
        );
    }

    return sum;
}