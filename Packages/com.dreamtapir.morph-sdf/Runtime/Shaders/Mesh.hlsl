#ifndef MORPH_SDF_HLSL_INCLUDE_MESH
#define MORPH_SDF_HLSL_INCLUDE_MESH

ByteAddressBuffer _VertexBuffer;
ByteAddressBuffer _IndexBuffer;

int _IndexFormat16Bit;
int _Stride;
int _ColorByteOffset;
int _UvByteOffset;
int _PositionByteOffset;
int _NormalByteOffset;
int _TangentByteOffset;

inline float2 LoadFloat2(uint index, ByteAddressBuffer buffer, int stride, int offset)
{
    return asfloat(buffer.Load2(index * stride + offset));
}

inline float3 LoadFloat3(uint index, ByteAddressBuffer buffer, int stride, int offset)
{
    return asfloat(buffer.Load3(index * stride + offset));
}

inline float4 LoadFloat4(uint index, ByteAddressBuffer buffer, int stride, int offset)
{
    return asfloat(buffer.Load4(index * stride + offset));
}

inline float4 GetVertexColor(uint index)
{
    return LoadFloat4(index, _VertexBuffer, _Stride, _ColorByteOffset);
}

inline float2 GetVertexUV(uint index)
{
    return LoadFloat2(index, _VertexBuffer, _Stride, _UvByteOffset);
}

inline float3 GetVertexPosition(uint index)
{
    return LoadFloat3(index, _VertexBuffer, _Stride, _PositionByteOffset);
}

inline float3 GetVertexNormal(uint index)
{
    return LoadFloat3(index, _VertexBuffer, _Stride, _NormalByteOffset);
}

inline float4 GetVertexTangent(uint index)
{
    return LoadFloat4(index, _VertexBuffer, _Stride, _TangentByteOffset);
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