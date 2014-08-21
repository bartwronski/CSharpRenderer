/*
FX implementation of Ken Perlin's "Improved Noise"
sgg 6/26/04
http://mrl.nyu.edu/~perlin/noise/
*/

Texture2D<float>  permSamplerTex             : register(t50);
Texture2D<float4> permSampler2dTex           : register(t51);
Texture2D<float4> gradSamplerTex             : register(t52);
Texture2D<float4> permGradSamplerTex         : register(t53);
Texture2D<float4> gradSampler4dTex           : register(t54);
Texture2D<float4> permGrad4dSamplerTex       : register(t55);

float3 fade(float3 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10); // new curve
    //	return t * t * (3 - 2 * t); // old curve
}

float4 fade(float4 t)
{
    return t * t * t * (t * (t * 6 - 15) + 10); // new curve
    //	return t * t * (3 - 2 * t); // old curve
}

float perm(float x)
{
    return permSamplerTex.SampleLevel(pointWrapSampler, float2(x,0.5f), 0);
}

float4 perm2d(float2 p)
{
    return permSampler2dTex.SampleLevel(pointWrapSampler, p, 0);
}

float grad(float x, float3 p)
{
    return dot(gradSamplerTex.SampleLevel(pointWrapSampler, float2(x * 16, 0.5f), 0).xyz, p);
}

float gradperm(float x, float3 p)
{
    return dot(permGradSamplerTex.SampleLevel(pointWrapSampler, float2(x, 0.5f), 0).xyz, p);
}

// optimized version
float inoise(float3 p)
{
    float3 P = fmod(floor(p), 256.0);	// FIND UNIT CUBE THAT CONTAINS POINT
    p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
    float3 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z.

    P = P / 256.0;
    const float one = 1.0 / 256.0;

    // HASH COORDINATES OF THE 8 CUBE CORNERS
    float4 AA = perm2d(P.xy) + P.z;

    // AND ADD BLENDED RESULTS FROM 8 CORNERS OF CUBE
    return lerp(lerp(lerp(gradperm(AA.x, p),
        gradperm(AA.z, p + float3(-1, 0, 0)), f.x),
        lerp(gradperm(AA.y, p + float3(0, -1, 0)),
        gradperm(AA.w, p + float3(-1, -1, 0)), f.x), f.y),

        lerp(lerp(gradperm(AA.x + one, p + float3(0, 0, -1)),
        gradperm(AA.z + one, p + float3(-1, 0, -1)), f.x),
        lerp(gradperm(AA.y + one, p + float3(0, -1, -1)),
        gradperm(AA.w + one, p + float3(-1, -1, -1)), f.x), f.y), f.z);
}


// 4d versions
float grad(float x, float4 p)
{
    return dot(gradSampler4dTex.SampleLevel(pointWrapSampler, x, 0), p);
}

float gradperm(float x, float4 p)
{
    return dot(permGrad4dSamplerTex.SampleLevel(pointWrapSampler, x, 0), p);
}


// 4D noise
float inoise(float4 p)
{
    float4 P = fmod(floor(p), 256.0);	// FIND UNIT HYPERCUBE THAT CONTAINS POINT
        p -= floor(p);                      // FIND RELATIVE X,Y,Z OF POINT IN CUBE.
    float4 f = fade(p);                 // COMPUTE FADE CURVES FOR EACH OF X,Y,Z, W
        P = P / 256.0;
    const float one = 1.0 / 256.0;

    // HASH COORDINATES OF THE 16 CORNERS OF THE HYPERCUBE
    float A = perm(P.x) + P.y;
    float AA = perm(A) + P.z;
    float AB = perm(A + one) + P.z;
    float B = perm(P.x + one) + P.y;
    float BA = perm(B) + P.z;
    float BB = perm(B + one) + P.z;

    float AAA = perm(AA) + P.w, AAB = perm(AA + one) + P.w;
    float ABA = perm(AB) + P.w, ABB = perm(AB + one) + P.w;
    float BAA = perm(BA) + P.w, BAB = perm(BA + one) + P.w;
    float BBA = perm(BB) + P.w, BBB = perm(BB + one) + P.w;

    // INTERPOLATE DOWN
    return lerp(
        lerp(lerp(lerp(grad(perm(AAA), p),
        grad(perm(BAA), p + float4(-1, 0, 0, 0)), f.x),
        lerp(grad(perm(ABA), p + float4(0, -1, 0, 0)),
        grad(perm(BBA), p + float4(-1, -1, 0, 0)), f.x), f.y),

        lerp(lerp(grad(perm(AAB), p + float4(0, 0, -1, 0)),
        grad(perm(BAB), p + float4(-1, 0, -1, 0)), f.x),
        lerp(grad(perm(ABB), p + float4(0, -1, -1, 0)),
        grad(perm(BBB), p + float4(-1, -1, -1, 0)), f.x), f.y), f.z),

        lerp(lerp(lerp(grad(perm(AAA + one), p + float4(0, 0, 0, -1)),
        grad(perm(BAA + one), p + float4(-1, 0, 0, -1)), f.x),
        lerp(grad(perm(ABA + one), p + float4(0, -1, 0, -1)),
        grad(perm(BBA + one), p + float4(-1, -1, 0, -1)), f.x), f.y),

        lerp(lerp(grad(perm(AAB + one), p + float4(0, 0, -1, -1)),
        grad(perm(BAB + one), p + float4(-1, 0, -1, -1)), f.x),
        lerp(grad(perm(ABB + one), p + float4(0, -1, -1, -1)),
        grad(perm(BBB + one), p + float4(-1, -1, -1, -1)), f.x), f.y), f.z), f.w);
}

// utility functions

// calculate gradient of noise (expensive!)
float3 inoiseGradient(float3 p, float d)
{
    float f0 = inoise(p);
    float fx = inoise(p + float3(d, 0, 0));
    float fy = inoise(p + float3(0, d, 0));
    float fz = inoise(p + float3(0, 0, d));
    return float3(fx - f0, fy - f0, fz - f0) / d;
}

// fractal sum
float fBm(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float freq = 1.0, amp = 0.5;
    float sum = 0;
    for (int i = 0; i<octaves; i++) {
        sum += inoise(p*freq)*amp;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum;
}

float turbulence(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5)
{
    float sum = 0;
    float freq = 1.0, amp = 1.0;
    for (int i = 0; i<octaves; i++) {
        sum += abs(inoise(p*freq))*amp;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum;
}

// Ridged multifractal
// See "Texturing & Modeling, A Procedural Approach", Chapter 12
float ridge(float h, float offset)
{
    h = abs(h);
    h = offset - h;
    h = h * h;
    return h;
}

float ridgedmf(float3 p, int octaves, float lacunarity = 2.0, float gain = 0.5, float offset = 1.0)
{
    float sum = 0;
    float freq = 1.0, amp = 0.5;
    float prev = 1.0;
    for (int i = 0; i<octaves; i++) {
        float n = ridge(inoise(p*freq), offset);
        sum += n*amp*prev;
        prev = n;
        freq *= lacunarity;
        amp *= gain;
    }
    return sum;
}
