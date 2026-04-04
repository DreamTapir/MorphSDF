#ifndef MORPH_SDF_HLSL_INCLUDE_SAT
#define MORPH_SDF_HLSL_INCLUDE_SAT

#include "Packages/com.dreamtapir.morph-sdf/Runtime/Shaders/Mesh.hlsl"
#include "Packages/com.dreamtapir.morph-sdf/Runtime/Shaders/Common.hlsl"

inline int3 GetSdfCoordinates(float3 world_position)
{
    float3 sdf_position = (world_position - _Origin) / _CellSize;
    return (int3)sdf_position;
}

inline float3 GetSdfCellPosition(int3 grid_position)
{
    float3 cell_center = float3(grid_position.x, grid_position.y, grid_position.z);
    cell_center += 0.5f;
    cell_center *= _CellSize;
    cell_center += _Origin;
    return cell_center;
}

bool TriangleBoxIntersect(float3 box_center, float3 box_extents, float3 v_0, float3 v_1, float3 v_2)
{
    const float3 tv_0 = v_0 - box_center;
    const float3 tv_1 = v_1 - box_center;
    const float3 tv_2 = v_2 - box_center;

    const float3 e_0 = tv_1 - tv_0;
    const float3 e_1 = tv_2 - tv_1;
    const float3 e_2 = tv_0 - tv_2;

    // AABB
    const float3 t_min = min(min(tv_0, tv_1), tv_2);
    const float3 t_max = max(max(tv_0, tv_1), tv_2);
    if (t_min.x > box_extents.x || t_max.x < -box_extents.x) return false;
    if (t_min.y > box_extents.y || t_max.y < -box_extents.y) return false;
    if (t_min.z > box_extents.z || t_max.z < -box_extents.z) return false;

    // Triangle Collision Test
    const float3 normal = cross(e_0, e_1);
    const float d = dot(normal, tv_0);
    const float r = box_extents.x * abs(normal.x) + box_extents.y * abs(normal.y) + box_extents.z * abs(normal.z);
    if (d > r || d < -r) return false;

    // 9 Edge Cross Test
    const float3 axis[3] = { float3(1,0,0), float3(0,1,0), float3(0,0,1) };
    const float3 edges[3] = { e_0, e_1, e_2 };
    
    [unroll]
    for (int i = 0; i < 3; ++i)
    {
        [unroll]
        for (int j = 0; j < 3; ++j)
        {
            float3 a = cross(axis[i], edges[j]);
            float3 abs_a = abs(a);
            
            float p_0 = dot(tv_0, a);
            float p_1 = dot(tv_1, a);
            float p_2 = dot(tv_2, a);
            float min_p = min(p_0, min(p_1, p_2));
            float max_p = max(p_0, max(p_1, p_2));
            
            float box_r = box_extents.x * abs_a.x + box_extents.y * abs_a.y + box_extents.z * abs_a.z;
            
            if (min_p > box_r || max_p < -box_r) return false;
        }
    }

    return true;
}

/*
 * brief Separating Axid Theorem
 */
[numthreads(TRIANGLE_THREAD_GROUP_SIZE, 1, 1)]
void MarkSurfaceVoxels(uint group_index : SV_GroupIndex, uint3 group_id : SV_GroupID)
{
    const uint triangle_index = (group_id.x * TRIANGLE_THREAD_GROUP_SIZE + group_index) * 3;
    const uint3 indices = GetIndices(triangle_index);
    const float3 tri_0 = mul(_ObjectToSdfLocalMatrix, float4(GetVertexPosition(indices.x), 1)).xyz;
    const float3 tri_1 = mul(_ObjectToSdfLocalMatrix, float4(GetVertexPosition(indices.y), 1)).xyz;
    const float3 tri_2 = mul(_ObjectToSdfLocalMatrix, float4(GetVertexPosition(indices.z), 1)).xyz;
		
    // Polygon AABB
    const float3 margin = (float3)_CellSize * 0.5f;
    const float3 aabb_min = min(tri_0, min(tri_1, tri_2)) - margin;
    const float3 aabb_max = max(tri_0, max(tri_1, tri_2)) + margin;

    const int3 grid_min = max(0, GetSdfCoordinates(aabb_min) - GRID_MARGIN);
    const int3 grid_max = min(_Resolution - 1, GetSdfCoordinates(aabb_max) + GRID_MARGIN);

    const float3 box_extents = margin;
    const uint zero_as_uint = FloatFlip3(0.0f);

    for (int z = grid_min.z; z <= grid_max.z; ++z)
    {
        for (int y = grid_min.y; y <= grid_max.y; ++y)
        {
            for (int x = grid_min.x; x <= grid_max.x; ++x)
            {
                const int3 grid_cell_coord = int3(x, y, z);
                const float3 cell_position = GetSdfCellPosition(grid_cell_coord);

                if (TriangleBoxIntersect(cell_position, box_extents, tri_0, tri_1, tri_2))
                {
                    int grid_cell_index = IDToIndex(grid_cell_coord, _Resolution);
                    InterlockedMin(_Voxel[grid_cell_index], zero_as_uint);
                }
            }
        }
    }
}

#endif