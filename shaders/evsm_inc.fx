

// Source of inspiration and code snippets: http://mynameismjp.wordpress.com/2013/09/10/shadow-maps/

static const float positiveExponent = 30.0f;
static const float negativeExponent = 10.0f;
static const float2 exponents = float2(positiveExponent, negativeExponent);

float2 warpDepth(float depth)
{
    float pos = exp(exponents.x * depth);
    depth = 2.0f * depth - 1.0f;
    float neg = -exp(-exponents.y * depth);
    return float2(pos, neg);
}

float chebyshev(float2 moments, float mean, float minVariance)
{
    // Compute variance
    float variance = moments.y - (moments.x * moments.x);
    variance = max(variance, minVariance);

    // Compute probabilistic upper bound
    float d = mean - moments.x;
    float pMax = variance / (variance + (d * d));

    // One-tailed Chebyshev
    return (mean <= moments.x ? 1.0f : pMax);
}

float calculateShadowEVSM(float4 moments, float smSpaceDepth)
{
    float2 posMoments = float2(moments.x, moments.z);
    float2 negMoments = float2(moments.y, moments.w);
    float2 warpedDepth = warpDepth(smSpaceDepth);

    float2 depthScale = 0.0001f * exponents * warpedDepth;
    float2 minVariance = depthScale * depthScale;
    float posResult = chebyshev(posMoments, warpedDepth.x, minVariance.x);
    float negResult = chebyshev(negMoments, warpedDepth.y, minVariance.y);
    return min(posResult, negResult);
}

float calculateShadowESM(float4 moments, float smSpaceDepth)
{
    float2 warpedDepth = warpDepth(smSpaceDepth);

    return saturate(moments.x / warpedDepth.x);
}