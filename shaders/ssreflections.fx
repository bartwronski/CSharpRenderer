// PixelShader:  SSReflectionsRaytrace, entry: SSReflectionsRaytrace
// PixelShader:  SSReflectionsBlur, entry: SSReflectionsBlur

#include "constants.fx"

Texture2D<float>  InputTextureLinearDepth       : register(t0);
Texture2D<float4> InputTextureNormals           : register(t1);
Texture2D<float4> InputTextureColor             : register(t2);
Texture2D<float2> InputTextureMotion            : register(t3);
Texture2D<float4> InputTexturePrevFrame         : register(t4);

#include "optimized-ggx.hlsl"

float4 SSReflectionsRaytrace(VS_OUTPUT_POSTFX i) : SV_Target
{
    float2 ssCoords = i.uv * float2(2, -2) + float2(-1,1);
    float3 worldCamRay = normalize(ssCoords.xxx * g_EyeXAxis.xyz + ssCoords.yyy * g_EyeYAxis.xyz + g_EyeZAxis.xyz);
    float depth = InputTextureLinearDepth.SampleLevel(pointSampler, i.uv, 0);
    float4 normalSample = InputTextureNormals.SampleLevel(pointSampler, i.uv, 0);
    float roughness = normalSample.w;

    // hashing / randomizing / magic
    uint phase = (uint)g_FrameNumber % 11;
    uint2 positionInt = (uint2)i.position.xy;
    uint basePos = (3 * positionInt.x ^ positionInt.y + positionInt.x * positionInt.y) + phase;
    float2 randoms = float2(RandomNumBuffer[basePos + 0], RandomNumBuffer[basePos + 1]);
    float3 normalWS = normalSample.xyz * 2.0f - 1.0f;
    // note: we do it in half res and blur additionally, so remap roughness to smaller range; this is not really correct, just approximation
    normalWS = ImportanceSampleGGX(randoms, roughness * 0.5f, normalWS);
   
    float3 reflectedRay = reflect(worldCamRay,normalWS);

    // note: following can be optimized a lot, this is "clear" version
    float3 worldPos = g_WorldEyePos.xyz + worldCamRay * depth;
    float3 wsRayBegin = worldPos;
    float3 wsRayEnd = worldPos + reflectedRay;
    float4 ssRayBegin = mul(g_ViewProjMatrix, float4(wsRayBegin, 1));
    float4 ssRayEnd = mul(g_ViewProjMatrix, float4(wsRayEnd, 1));
    ssRayBegin.w = rcp(ssRayBegin.w);
    ssRayEnd.w = rcp(ssRayEnd.w);
    float3 ssRay = 0.0f;
    ssRay.xy = ssRayEnd.xy * ssRayEnd.ww - ssRayBegin.xy * ssRayBegin.ww;
    ssRay.xy *= float2(0.5f, -0.5f); // ss to ts
    ssRay.z = ssRayEnd.w - ssRayBegin.w;
    
    // normalize to 1 pixel in texture space
    float2 t = ssRay.xy * g_ScreenSizeHalfRes.xy;
    ssRay *= rsqrt(dot(t, t));

    float3 currentPos = float3(i.uv, rcp(depth));
    // basic offset to avoid self-intersection
    currentPos += 1.5f * ssRay;

    // larger steps
    ssRay *= 2.0f;
    currentPos += ssRay * (g_FrameRandoms.x + rand(i.uv)) * 0.5f;

    float3 maxIter = 0.0f;
    float3 rayRcp = rcp(ssRay.xyz);

    // find collision with screen borders and camera plane
    maxIter.xy = max((1.0f - currentPos.xy) * rayRcp.xy, -currentPos.xy * rayRcp.xy);
    maxIter.z = (rayRcp.z < 0.0f) ? (- rayRcp.z * currentPos.z) : 1000.0f;
    
    uint maxIterInt = min(min(maxIter.x, maxIter.y), maxIter.z);
    maxIterInt /= 4;
    
    uint iterCount = 0;
    while (iterCount++ < maxIterInt)
    {
        float4 currentPosXYOne = currentPos.xyxy + ssRay.xyxy * float4(1, 1, 2, 2);
        float4 currentPosXYTwo = currentPos.xyxy + ssRay.xyxy * float4(3, 3, 4, 4);
        float4 currentPosZ = currentPos.zzzz + ssRay.zzzz * float4(1, 2, 3, 4);
        float4 currDepth;
        currDepth.x = InputTextureLinearDepth.SampleLevel(pointSampler, currentPosXYOne.xy, 0).x;
        currDepth.y = InputTextureLinearDepth.SampleLevel(pointSampler, currentPosXYOne.zw, 0).x;
        currDepth.z = InputTextureLinearDepth.SampleLevel(pointSampler, currentPosXYTwo.xy, 0).x;
        currDepth.w = InputTextureLinearDepth.SampleLevel(pointSampler, currentPosXYTwo.zw, 0).x;

        currentPos.xyz = float3(currentPosXYTwo.zw, currentPosZ.w);

        float4 comparison = currDepth * currentPosZ;
        bool4 test = (comparison < 1.01f) && (comparison > 0.7f);
        [branch]
        if (any(test))
        {
            currentPos.xy = test.z ? currentPosXYTwo.xy : currentPos.xy;
            currentPos.xy = test.y ? currentPosXYOne.zw : currentPos.xy;
            currentPos.xy = test.x ? currentPosXYOne.xy : currentPos.xy;

            break;
        }
    }
    
    [branch]
    if (iterCount < maxIterInt)
    {
        float2 uv = currentPos.xy;
        float2 motionVector = InputTextureMotion.SampleLevel(linearSampler, uv, 0);
        uv += motionVector;
        float4 samp = InputTextureColor.SampleLevel(linearSampler, uv, 0);
        float vignette = 1.0f - dot(ssCoords, ssCoords);
        samp.a = vignette;
        return samp;
    }

    return 0.0f;
}

// hlsl array
static const uint SAMPLE_NUM = 19;
static const float2 POISSON_SAMPLES[SAMPLE_NUM] =
{
    //float2(0.0f, 0.0f),
    float2(-0.913637417379f, -0.39201712298f),
    float2(0.597109626106f, 0.772134734643f),
    float2(0.832961355927f, 0.0999080784288f),
    float2(0.0507237968516f, -0.986328460427f),
    float2(0.831213944606f, -0.554988666148f),
    float2(-0.433961834033f, 0.830242645908f),
    float2(-0.374497057982f, -0.722719996686f),
    float2(-0.0845827262662f, 0.364290041594f),
    float2(-0.758577248372f, 0.0126646127217f),
    float2(0.267715036916f, -0.363466095304f),
    float2(0.291318297389f, 0.368761920866f),
    float2(-0.473999302807f, -0.303207622949f),
    float2(0.516504756772f, -0.827533722604f),
    float2(0.191397162978f, 0.757781091116f),
    float2(-0.942805497434f, 0.325320891913f),
    float2(0.503718277072f, -0.142206433619f),
    float2(0.92639800923f, -0.218479153066f),
    float2(0.80364886067f, 0.508373493172f),
    float2(-0.498046279304f, 0.291619367277f),
};
float4 SSReflectionsBlur(VS_OUTPUT_POSTFX i) : SV_Target
{
    uint2 pos = i.position.xy;
    float depth = InputTextureLinearDepth.SampleLevel(pointSampler, i.uv, 0);
    float depthRcp = rcp(depth);

    float4 normalSample = InputTextureNormals.SampleLevel(pointSampler, i.uv, 0);
    float roughness = normalSample.w;

    // Not physically based... but smooths out noise
    float2 radius = 7.0f * roughness * g_ScreenSizeHalfRes.zw;
    float rotationAngle = (rand(i.uv.xy) + g_FrameRandoms.y) * 2.0f * PI;
    float cosA = cos(rotationAngle);
    float sinA = sin(rotationAngle);
    float4 rotationMatrix = float4(cosA, -sinA, sinA, cosA) * radius.xxyy;

    float4 reflections = InputTextureColor.SampleLevel(pointSampler, i.uv, 0);
    float weightNormalization = 1.0f;

    for (uint x = 0; x < SAMPLE_NUM; ++x)
    {
        float2 smpPos = i.uv + float2(dot(POISSON_SAMPLES[x].xx, rotationMatrix.xy), dot(POISSON_SAMPLES[x].yy, rotationMatrix.zw));
        float smpDepth = InputTextureLinearDepth.SampleLevel(pointSampler, smpPos, 0);
        float weight = saturate(1.0f - 30.0f * abs(smpDepth - depth) * depthRcp);
        reflections += InputTextureColor.SampleLevel(pointSampler, smpPos, 0) * weight;
        weightNormalization += weight;
    }
    
    reflections *= rcp(weightNormalization);

    // quad bilat blur
    if (abs(ddx_fine(depth)) < 0.2f)
    {
        reflections -= ddx_fine(reflections) * ((pos.x & 1) - 0.5);
    }

    if (abs(ddy_fine(depth)) < 0.2f)
    {
        reflections -= ddy_fine(reflections) * ((pos.y & 1) - 0.5);
    }

    // temporal reprojection code
    float2 motionVectorCurrentPoint = InputTextureMotion.SampleLevel(linearSampler, i.uv, 0);
    float2 reprojectUV = i.uv + motionVectorCurrentPoint;
    float2 motionVectorSamplePoint = InputTextureMotion.SampleLevel(linearSampler, reprojectUV, 0);
    float4 sampReflectionsAccum = InputTexturePrevFrame.SampleLevel(linearSampler, reprojectUV, 0);
    float2 motionDiff = motionVectorSamplePoint - motionVectorCurrentPoint;

    float rejection = 1000.0f * dot(motionVectorCurrentPoint, motionVectorCurrentPoint) +  // just motion
        dot(abs(motionDiff), 512.0f) + // or motion diff
        abs(dot(reprojectUV.xy - saturate(reprojectUV.xy), 100.0f));// + // or sampling offscreen
        saturate(1.0f - sampReflectionsAccum.w); // or invalid history
    reflections = lerp(sampReflectionsAccum, reflections, saturate(0.07f + rejection));

    return reflections;
}