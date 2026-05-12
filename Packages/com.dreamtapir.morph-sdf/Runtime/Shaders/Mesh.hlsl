#ifndef MORPH_SDF_HLSL_INCLUDE_MESH
#define MORPH_SDF_HLSL_INCLUDE_MESH

#include "Packages/com.dreamtapir.morph-sdf/Runtime/Shaders/RawBuffer.hlsl"

ByteAddressBuffer _VertexBuffer;
ByteAddressBuffer _IndexBuffer;

int _IndexFormat16Bit;
int _Stride;
int _ColorByteOffset;
int _UvByteOffset;
int _PositionByteOffset;
int _NormalByteOffset;
int _TangentByteOffset;

inline float4 GetVertexColor(uint index)
{
    return LoadFloat4(_VertexBuffer, index, (uint)_Stride, (uint)_ColorByteOffset);
}

inline float2 GetVertexUV(uint index)
{
    return LoadFloat2(_VertexBuffer, index, (uint)_Stride, (uint)_UvByteOffset);
}

inline float3 GetVertexPosition(uint index)
{
    return LoadFloat3(_VertexBuffer, index, (uint)_Stride, (uint)_PositionByteOffset);
}

inline float3 GetVertexNormal(uint index)
{
    return LoadFloat3(_VertexBuffer, index, (uint)_Stride, (uint)_NormalByteOffset);
}

inline float4 GetVertexTangent(uint index)
{
    return LoadFloat4(_VertexBuffer, index, (uint)_Stride, (uint)_TangentByteOffset);
}

uint3 GetIndices(uint start_index)
{
    if (_IndexFormat16Bit == 1)
    {
        const uint word_index = start_index >> 1u;
        const uint byte_offset = word_index << 2u;
        const uint2 data = _IndexBuffer.Load2(byte_offset);
        const uint offset = start_index & 1u;
        
        if (offset == 0u)
        {
            return uint3(data.x & 0xffff, data.x >> 16, data.y & 0xffff);
        }
        
        return uint3(data.x >> 16, data.y & 0xffff, data.y >> 16);
    }
    
    return _IndexBuffer.Load3(start_index << 2u);
}

#endif