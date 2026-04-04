#ifndef MORPH_SDF_HLSL_INCLUDE_COMMON
#define MORPH_SDF_HLSL_INCLUDE_COMMON

float4x4 _ObjectToSdfLocalMatrix;
float3 _Origin;
float _CellSize;
int3 _Resolution;

RWStructuredBuffer<uint> _Voxel;

/*
 * | 0          | 0               | 00 0000 0000 | 0000 0000 00 | 00 0000 0000 |
 * | not a site | no next element | x coordinate | y coordinate | z coordinate |
 * | 1-bits     | 1-bits          | 10-bits      | 10-bits      | 10-bits      |
 */
StructuredBuffer<int> _Input;
RWStructuredBuffer<int> _Output;

RWTexture3D<float> _Sdf;

#ifndef MARKER
#define MARKER -2147483648
#endif
#ifndef MAX_INT
#define MAX_INT 2147483647
#endif
#ifndef MAX_FLT
#define MAX_FLT 3.402823466e+38
#endif

#define BLOCK_X 32
#define BLOCK_Y 4
#define BLOCK_SIZE 32

#define TRIANGLE_THREAD_GROUP_SIZE 128
#define GRID_MARGIN int3(1, 1, 1)

inline int IDToIndex(int x, int y, int z, int3 size)
{
    return x + y * size.x + z * size.x * size.y;
}

inline int IDToIndex(int3 id, int3 size)
{
    return id.x + id.y * size.x + id.z * size.x * size.y;
}

inline uint FloatFlip3(float fl)
{
    uint f = asuint(fl);
    return (f << 1) | (f >> 31);
}

inline uint IFloatFlip3(uint f2)
{
    return (f2 >> 1) | (f2 << 31);
}

#endif
