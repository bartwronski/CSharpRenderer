// PixelShader: DownsampleColorCoC, entry: DownsampleColorCoC
// VertexShader: VertexFullScreenDofGrid, entry: VShader
// PixelShader: BokehSprite, entry: BokehSprite
// PixelShader: ResolveBokeh, entry: ResolveBokeh
// PixelShader: ResolveBokehDebug, entry: ResolveBokeh, defines: DEBUG_BOKEH

#include "constants.fx"

Texture2D<float4> InputTexture                  : register(t0);
Texture2D<float>  InputTextureDepth             : register(t1);
Texture2D<float4> InputTextureBokeh             : register(t2);
Texture2D<float4> InputTextureDownscaledColor   : register(t3);
Texture2D<float4> InputTextureBokehShape        : register(t4);

float4 DownsampleColorCoC(VS_OUTPUT_POSTFX i) : SV_Target
{
    float3 textureSample = InputTexture.SampleLevel( linearSampler, i.uv, 0 ).rgb;

    float4 depth = InputTextureDepth.GatherRed(pointSampler, i.uv);
    float coc = CalculateCoc(min(max(depth.x, depth.y), max(depth.z, depth.w)));
    
    return float4(textureSample, coc);
}

struct VS_OUTPUT_BOKEH
{
    float4 position             : SV_POSITION;
    float4 colorAlpha           : TEXCOORD0;
    float2 uv                   : TEXCOORD1;
};


VS_OUTPUT_BOKEH VShader(uint id : SV_VERTEXID)
{
    uint quadIndex = id / 4;
    uint vertexIndex = id % 4;

    float screenWidth = g_ScreenSizeHalfRes.x;
    float screenWidthRcp = g_ScreenSizeHalfRes.z;

    float quadIndexAsFloat = quadIndex;

    float pixelY = floor(quadIndex * screenWidthRcp);
    float pixelX = quadIndex - pixelY * screenWidth;

    float4 colorAndDepth = InputTexture.Load(uint3(pixelX, pixelY, 0));

    VS_OUTPUT_BOKEH vo;
    float2 position2D;
    position2D.x = (vertexIndex % 2) ? 1.0f : 0.0f;
    position2D.y = (vertexIndex & 2) ? 1.0f : 0.0f;
    vo.uv = position2D;

    // make the scale not biased in any direction
    position2D -= 0.5f;

    float near = colorAndDepth.a < 0.0f ? -1.0f : 0.0f;
    float cocScale = abs(colorAndDepth.a);

    // multiply by bokeh size + clamp max to not kill the bw
    float size = min(cocScale, 32.0f);
    position2D *= size;

    // rebias
    position2D += 0.5f;

    position2D += float2(pixelX, pixelY);

    // "texture space" coords
    position2D *= g_ScreenSizeHalfRes.zw;

    // screen space coords, near goes right, far goes left
    position2D = position2D * float2(1.0f, -2.0f) + float2(near, 1.0f);

    vo.position.xy = position2D;
    vo.position.z = 0.0f;

    // if in focus, cull it out
    vo.position.w = (cocScale < 1.0f) ? -1.0f : 1.0f;

    vo.colorAlpha = float4(colorAndDepth.rgb, 1.0f * rcp (size * size));

    return vo;
}

float4 BokehSprite(VS_OUTPUT_BOKEH i) : SV_Target
{
    float3 bokehSample = InputTextureBokehShape.Sample(linearSampler, i.uv).rgb;
    float bokehLuminance = dot(bokehSample, float3(0.299f, 0.587f, 0.114f));
    return float4(bokehSample * i.colorAlpha.rgb, bokehLuminance) * i.colorAlpha.aaaa;
}

float4 ResolveBokeh(VS_OUTPUT_POSTFX i) : SV_Target
{
    float4 farPlaneColor = InputTextureBokeh.SampleLevel(linearSampler, i.uv * float2(0.5f, 1.0f) + float2(0.5f, 0.0f), 0);
    float4 nearPlaneColor = InputTextureBokeh.SampleLevel(linearSampler, i.uv * float2(0.5f, 1.0f), 0);

    float4 origColor = InputTexture.SampleLevel(pointSampler, i.uv, 0);
    float4 downsampledColor = InputTextureDownscaledColor.SampleLevel(linearSampler, i.uv, 0);

    float coc = downsampledColor.a;
    
    float3 farColor = farPlaneColor.rgb / max(farPlaneColor.aaa,0.0001f);
    float3 nearColor = nearPlaneColor.rgb / max(nearPlaneColor.aaa,0.0001f);

#ifdef DEBUG_BOKEH
    farColor = float3(1.0f, 0.0f, 0.0f);
    nearColor = float3(0.0f, 0.0f, 1.0f);
    downsampledColor.rgb = float3(0.0f, 1.0f, 0.0f);
    origColor.rgb = float3(1.0f, 1.0f, 1.0f);
#endif
    
    // we must take into account the fact that we avoided drawing sprites of size 1 (optimization), only bigger - both for near and far
    float3 blendedFarFocus = lerp(downsampledColor.rgb, farColor, saturate(coc - 2.0f));
    
    // this one is hack to smoothen the transition - we blend between low res and high res in < 1 half res pixel transition zone
    blendedFarFocus = lerp(origColor.rgb, blendedFarFocus, saturate(0.5f * coc-1.0f));
    
    // we have 2 factors: 
    // 1. one is scene CoC - if it is supposed to be totally blurry, but feature was thin,
    // we will have an artifact and cannot do anything about it :( as we do not know fragments behind contributing to it
    // 2. second one is accumulated, scattered bokeh intensity. Note "magic" number of 8.0f - to have it proper, I would have to 
    // calculate true coverage per mip of bokeh texture - "normalization factor" - or the texture itself should be float/HDR normalized to impulse response. 
    // For the demo purpose I hardcoded some value.
    float3 finalColor = lerp(blendedFarFocus, nearColor, saturate(saturate(-coc - 1.0f) + nearPlaneColor.aaa * 8.0f));

    return float4(finalColor, 1.0f);
}