
struct DebugInformation
{
    float4 source;
    float4 value;
};

AppendStructuredBuffer<DebugInformation> g_DebugBuffer : register(u7);

#define DEBUG_FILTER_CHECK_ON (g_GPUDebugOn > 0.5f)

#define DEBUG_FILTER_CHECK_FORCE (g_GPUDebugOverridePositionEnable > 0.5f)

#define DEBUG_FILTER_CHECK_FORCE_VPOS_INTERNAL(_pos) (DEBUG_FILTER_CHECK_FORCE && (_pos.x == (g_GPUDebugOverridePositionXYZ.x + 0.5f)) && (_pos.y == (g_GPUDebugOverridePositionXYZ.y + 0.5f)))

#define DEBUG_FILTER_CHECK_FORCE_TID_INTERNAL(_pos)  (DEBUG_FILTER_CHECK_FORCE && ((float)(_pos.x) == g_GPUDebugOverridePositionXYZ.x) && ((float)(_pos.y) == g_GPUDebugOverridePositionXYZ.y ) && ((float)(_pos.z) == g_GPUDebugOverridePositionXYZ.z ))

#define DEBUG_FILTER_VPOS(_pos, _x, _y) (DEBUG_FILTER_CHECK_ON && (DEBUG_FILTER_CHECK_FORCE_VPOS_INTERNAL(_pos) || (!DEBUG_FILTER_CHECK_FORCE && (_pos.x == (_x + 0.5f)) && (_pos.y == (_y + 0.5f)) ) ) )
#define DEBUG_FILTER_TID(_pos, _x, _y, _z) (DEBUG_FILTER_CHECK_ON && (DEBUG_FILTER_CHECK_FORCE_TID_INTERNAL(_pos) || (!DEBUG_FILTER_CHECK_FORCE && (_pos.x == _x) && (_pos.y == _y) && (_pos.z == _z))))

#define DEBUG_FILTER_CHECK_FORCE_VPOS(_pos) (DEBUG_FILTER_CHECK_ON && (DEBUG_FILTER_CHECK_FORCE_VPOS_INTERNAL(_pos)))
#define DEBUG_FILTER_CHECK_FORCE_TID(_pos)  (DEBUG_FILTER_CHECK_ON && (DEBUG_FILTER_CHECK_FORCE_TID_INTERNAL(_pos)))

void DebugInfo(float3 pos, float4 data)
{
    DebugInformation di;
    di.source = float4(pos,4.0f);
    di.value = data;
    g_DebugBuffer.Append(di);
}

void DebugInfo(float3 pos, float3 data)
{
    DebugInformation di;
    di.source = float4(pos, 3.0f);
    di.value = float4(data, 0.0f);
    g_DebugBuffer.Append(di);
}

void DebugInfo(float3 pos, float2 data)
{
    DebugInformation di;
    di.source = float4(pos, 2.0f);
    di.value = float4(data, 0.0f, 0.0f);
    g_DebugBuffer.Append(di);
}

void DebugInfo(float3 pos, float data)
{
    DebugInformation di;
    di.source = float4(pos, 1.0f);
    di.value = float4(data,0.0f,0.0f,0.0f);
    g_DebugBuffer.Append(di);
}
