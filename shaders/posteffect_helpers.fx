// VertexShader: VertexFullScreenTriangle, entry: VShader
// VertexShader: VertexFullScreenTriangleMaxZ, entry: VShader, defines: MAX_Z
// PixelShader:  PixelTest, entry: PShader
// PixelShader:  ResolveHDR, entry: ResolvePS
// PixelShader:  Copy, entry: PShaderCopy
// PixelShader:  CopyAlpha, entry: PShaderCopy, defines: ALPHA
// PixelShader:  CopyGamma, entry: PShaderCopy, defines: GAMMA
// PixelShader:  CopyFrac, entry: PShaderCopy, defines: FRAC
// PixelShader:  Difference, entry: PShaderDifference
// PixelShader:  FXAA, entry: FxaaPS
// PixelShader:  Downsample4x4, entry: Downsample4x4
// PixelShader:  Downsample4x4Luminance, entry: Downsample4x4Luminance
// PixelShader:  Downsample4x4CalculateLuminance, entry: Downsample4x4Luminance, defines: CALCULATE_LUMINANCE
// PixelShader:  Downsample2x2, entry: Downsample2x2
// PixelShader:  Downsample2x2Luminance, entry: Downsample2x2Luminance
// ComputeShader: FinalCalculateAverageLuminance, entry: FinalCalculateAverageLuminance

#include "constants.fx"

Texture2D<float4> InputTexture                  : register(t0);
Texture2D<float4> InputTextureTwo               : register(t1);
Texture2D<float>  LuminanceTexture              : register(t1);
RWTexture2D<float> RWTextureLuminance           : register(u0);

VS_OUTPUT_POSTFX VShader(uint id : SV_VERTEXID)
{
    VS_OUTPUT_POSTFX vo;
    vo.position.x = (float)(id / 2) * 4.0f - 1.0f;
    vo.position.y = (float)(id % 2) * 4.0f - 1.0f;
#ifdef MAX_Z
    vo.position.z = 1.0f;
#else
    vo.position.z = 0.0f;
#endif
    vo.position.w = 1.0f;

    vo.uv.x = (float)(id / 2) * 2.0f;
    vo.uv.y = 1.0f - (float)(id % 2) * 2.0f;

    return vo;
}

float4 PShader(VS_OUTPUT_POSTFX i) : SV_Target
{
    return float4(i.uv.xyx,1.0f);
}

float4 PShaderCopy(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 textureSample = InputTexture.SampleLevel(linearSampler, i.uv, 0);

#ifdef GAMMA
    textureSample.rgb = LinearToGamma(textureSample.rgb);
#endif

#ifdef ALPHA
    return textureSample.aaaa;
#elif defined(FRAC)
    return frac(textureSample);
#else
    return textureSample;
#endif
}

float4 PShaderDifference(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 textureSample = 5.0f * abs(InputTexture.SampleLevel(linearSampler, i.uv, 0) - InputTextureTwo.SampleLevel(linearSampler, i.uv, 0));
    return textureSample;
}

static const float A = 0.15;
static const float B = 0.50;
static const float C = 0.10;
static const float D = 0.20;
static const float E = 0.02;
static const float F = 0.30;
static const float W = 11.2;

// http://filmicgames.com/archives/75  
float3 Uncharted2Tonemap(float3 x) 
{
    return ((x*(A*x + C*B) + D*E) / (x*(A*x + B) + D*F)) - E / F;
}

float4 ResolvePS(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 textureSample = InputTexture.SampleLevel(pointSampler, i.uv, 0 );
    float3 color = Uncharted2Tonemap(textureSample.rgb);
    float avgLuminance = LuminanceTexture[uint2(0,0)];
    float3 whiteScale = 1.0f / Uncharted2Tonemap(2.0f * avgLuminance);
    color = color*whiteScale;
    float noise = (rand(i.uv.xy + g_FrameRandoms.xy) + rand(i.uv.yx + g_FrameRandoms.zw) - 1.0f) / 255.0f;

    return float4(max(LinearToGamma(color) + noise, 0.0f), 1.0f);
}

float4 Downsample4x4(VS_OUTPUT_POSTFX i) : SV_Target
{
    // inefficient, quick to write
    uint2 size;
    InputTexture.GetDimensions(size.x, size.y);

    float2 uv = i.uv - float2(rcp((float)size.x), rcp((float)size.y));

    float4 val = 0.0f;
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 0));
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 0));
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 2));
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 2));

    val *= 1.0f / 4.0f;

    return val;
}

float Downsample4x4Luminance(VS_OUTPUT_POSTFX i) : SV_Target
{
    // inefficient, quick to write
    uint2 size;
    InputTexture.GetDimensions(size.x, size.y);

    float2 uv = i.uv - float2(rcp((float)size.x), rcp((float)size.y));

    float val = 0.0f;
#ifdef CALCULATE_LUMINANCE
    // incorrect (should take 4x samples), but faster
    val += max(dot(InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 0)).rgb, LUMINANCE_VECTOR), 0.0001f);
    val += max(dot(InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 0)).rgb, LUMINANCE_VECTOR), 0.0001f);
    val += max(dot(InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 2)).rgb, LUMINANCE_VECTOR), 0.0001f);
    val += max(dot(InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 2)).rgb, LUMINANCE_VECTOR), 0.0001f);
#else
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 0)).r;
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 0)).r;
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 2)).r;
    val += InputTexture.SampleLevel(linearSampler, uv, 0, int2(2, 2)).r;
#endif

    val *= 1.0f / 4.0f;

    return val;
}


float4 Downsample2x2(VS_OUTPUT_POSTFX i) : SV_Target
{
    float2 uv = i.uv;
    return InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 0));
}

float Downsample2x2Luminance(VS_OUTPUT_POSTFX i) : SV_Target
{
    float2 uv = i.uv;
    return InputTexture.SampleLevel(linearSampler, uv, 0, int2(0, 0)).r;
}

[numthreads(8, 8, 1)]
void FinalCalculateAverageLuminance(uint3 dispatchThreadID : SV_DispatchThreadID)
{
    if (all(dispatchThreadID.xy == uint2(0, 0)))
    {
        uint2 size;
        InputTexture.GetDimensions(size.x, size.y);

        float val = 0.0f;
        for (uint y = 0; y < size.y; ++y)
            for (uint x = 0; x < size.x; ++x)
            {
                val += InputTexture[uint2(x, y)].r;
            }

        RWTextureLuminance[uint2(0, 0)] = lerp(RWTextureLuminance[uint2(0, 0)], val, 0.01f);
    }
}


#include "FXAA.hlsl"

float4 FxaaPS(VS_OUTPUT_POSTFX Input) : SV_TARGET
{
    FxaaTex tex = { anisoSampler, InputTexture };

    float3 aaImage = FxaaPixelShader(Input.uv.xy, tex, g_ScreenSize.zw);

    return float4(aaImage,1.0f);
}
