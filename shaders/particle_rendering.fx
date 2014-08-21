// VertexShader: VertexParticle, entry: VShader
// PixelShader:  PixelParticle, entry: PShader

#include "constants.fx"

struct Particle
{
    float4 color;
    float3 worldPosition;
    float2 screenSize;
};

StructuredBuffer<Particle> ParticleBuffer : register(t0);

struct VS_OUTPUT
{
    float4 position                             : SV_POSITION;
    float4 color                                : TEXCOORD0;
    nointerpolation float3 worldSpacePosition   : TEXCOORD1;
};


VS_OUTPUT VShader(uint id : SV_VERTEXID)
{
    uint particleIndex = id / 4;
    uint vertexIndex = id % 4;

    float2 position2D;
    position2D.x = (vertexIndex % 2) ? 1.0f : -1.0f;
    position2D.y = (vertexIndex & 2) ? -1.0f : 1.0f;

    Particle particle = ParticleBuffer[particleIndex];

    float3 particlePosition = particle.worldPosition;

    VS_OUTPUT vo;
    float4 position = float4(particlePosition, 1.0f);
    position2D *= particle.screenSize; 
    position.xyz += position2D.xxx * float3(g_ViewMatrix[0].xyz);
    position.xyz += position2D.yyy * float3(g_ViewMatrix[1].xyz);
    float4 screenSpacePosition = mul(g_ViewProjMatrix, position);

    vo.position = screenSpacePosition;
    vo.worldSpacePosition = particlePosition.xyz;

    vo.color = particle.color;

    return vo;
}


float4 PShader(VS_OUTPUT i) : SV_Target
{
    return i.color;
}
