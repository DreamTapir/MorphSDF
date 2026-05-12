#ifndef MORPH_SDF_HLSL_INCLUDE_RAWBUFFER
#define MORPH_SDF_HLSL_INCLUDE_RAWBUFFER

// ========== UInt ByteAddressBuffer ==========

inline uint LoadUInt(ByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load(address);
}

inline uint2 LoadUInt2(ByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load2(address);
}

inline uint3 LoadUInt3(ByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load3(address);
}

inline uint4 LoadUInt4(ByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load4(address);
}

// ========== UInt RWByteAddressBuffer ==========

inline uint LoadUInt(RWByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load(address);
}

inline uint2 LoadUInt2(RWByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load2(address);
}

inline uint3 LoadUInt3(RWByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load3(address);
}

inline uint4 LoadUInt4(RWByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    uint address = index * stride + offset;
    return buffer.Load4(address);
}

// ========== Int ByteAddressBuffer ==========

inline int LoadInt(ByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    return asint(LoadUInt(buffer, index, stride, offset));
}

inline int2 LoadInt2(ByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    return asint(LoadUInt2(buffer, index, stride, offset));
}

inline int3 LoadInt3(ByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    return asint(LoadUInt3(buffer, index, stride, offset));
}

inline int4 LoadInt4(ByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    return asint(LoadUInt4(buffer, index, stride, offset));
}

// ========== Int RWByteAddressBuffer ==========

inline int LoadInt(RWByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    return asint(LoadUInt(buffer, index, stride, offset));
}

inline int2 LoadInt2(RWByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    return asint(LoadUInt2(buffer, index, stride, offset));
}

inline int3 LoadInt3(RWByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    return asint(LoadUInt3(buffer, index, stride, offset));
}

inline int4 LoadInt4(RWByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    return asint(LoadUInt4(buffer, index, stride, offset));
}

// ========== Float ByteAddressBuffer ==========

inline float LoadFloat(ByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    return asfloat(LoadUInt(buffer, index, stride, offset));
}

inline float2 LoadFloat2(ByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    return asfloat(LoadUInt2(buffer, index, stride, offset));
}

inline float3 LoadFloat3(ByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    return asfloat(LoadUInt3(buffer, index, stride, offset));
}

inline float4 LoadFloat4(ByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    return asfloat(LoadUInt4(buffer, index, stride, offset));
}

// ========== Float RWByteAddressBuffer ==========

inline float LoadFloat(RWByteAddressBuffer buffer, uint index, uint stride = 4u, uint offset = 0)
{
    return asfloat(LoadUInt(buffer, index, stride, offset));
}

inline float2 LoadFloat2(RWByteAddressBuffer buffer, uint index, uint stride = 8u, uint offset = 0)
{
    return asfloat(LoadUInt2(buffer, index, stride, offset));
}

inline float3 LoadFloat3(RWByteAddressBuffer buffer, uint index, uint stride = 12u, uint offset = 0)
{
    return asfloat(LoadUInt3(buffer, index, stride, offset));
}

inline float4 LoadFloat4(RWByteAddressBuffer buffer, uint index, uint stride = 16u, uint offset = 0)
{
    return asfloat(LoadUInt4(buffer, index, stride, offset));
}

// ========== Store UInt RWByteAddressBuffer ==========

inline void Store(RWByteAddressBuffer buffer, uint value, uint index, uint stride = 4u, uint offset = 0)
{
    buffer.Store(index * stride + offset, value);
}

inline void Store(RWByteAddressBuffer buffer, uint2 value, uint index, uint stride = 8u, uint offset = 0)
{
    buffer.Store2(index * stride + offset, value);
}

inline void Store(RWByteAddressBuffer buffer, uint3 value, uint index, uint stride = 12u, uint offset = 0)
{
    buffer.Store3(index * stride + offset, value);
}

inline void Store(RWByteAddressBuffer buffer, uint4 value, uint index, uint stride = 16u, uint offset = 0)
{
    buffer.Store4(index * stride + offset, value);
}

// ========== Store Int RWByteAddressBuffer ==========

inline void Store(RWByteAddressBuffer buffer, int value, uint index, uint stride = 4u, uint offset = 0)
{
    buffer.Store(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, int2 value, uint index, uint stride = 8u, uint offset = 0)
{
    buffer.Store2(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, int3 value, uint index, uint stride = 12u, uint offset = 0)
{
    buffer.Store3(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, int4 value, uint index, uint stride = 16u, uint offset = 0)
{
    buffer.Store4(index * stride + offset, asuint(value));
}

// ========== Store Float RWByteAddressBuffer ==========

inline void Store(RWByteAddressBuffer buffer, float value, uint index, uint stride = 4u, uint offset = 0)
{
    buffer.Store(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, float2 value, uint index, uint stride = 8u, uint offset = 0)
{
    buffer.Store2(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, float3 value, uint index, uint stride = 12u, uint offset = 0)
{
    buffer.Store3(index * stride + offset, asuint(value));
}

inline void Store(RWByteAddressBuffer buffer, float4 value, uint index, uint stride = 16u, uint offset = 0)
{
    buffer.Store4(index * stride + offset, asuint(value));
}

#endif
