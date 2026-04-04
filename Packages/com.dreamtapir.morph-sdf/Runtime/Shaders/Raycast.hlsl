#ifndef MORPH_SDF_HLSL_INCLUDE_RAYCAST
#define MORPH_SDF_HLSL_INCLUDE_RAYCAST

#include "Packages/com.dreamtapir.morph-sdf/Runtime/Shaders/Common.hlsl"

/*
 * brief Approximate volume
 */
[numthreads(BLOCK_X, BLOCK_Y, 1)]
void BidirectionalRaycast(uint3 dispatch_thread_id : SV_DispatchThreadID)
{
    if (any(dispatch_thread_id.xy >= (uint2)_Resolution.xy)) return;
    
    const int3 id = int3(dispatch_thread_id.xy, 0);
    const int plane = _Resolution.x * _Resolution.y;

    int index = IDToIndex(id, _Resolution);

    bool is_inside = false;
    bool was_surface = false;

    // Sweep down
    for (int i = 0; i < _Resolution.z; i++)
    {
        uint raw = _Voxel[index];
        float val = asfloat(IFloatFlip3(raw));

        if (abs(val) < 1e-5f) 
        {
            if (!was_surface) 
            {
                is_inside = !is_inside;
                was_surface = true;
            }
        }
        else 
        {
            was_surface = false;

            float sign = is_inside ? -1.0f : 1.0f;
            _Voxel[index] = FloatFlip3(sign);
        }

        index += plane;
    }

    is_inside = false;
    was_surface = false;
    
    // Sweep up
    for (int j = _Resolution.z - 1; j >= 0; j--)
    {
        index -= plane;
        
        uint raw = _Voxel[index];
        float val = asfloat(IFloatFlip3(raw));
    
        if (abs(val) < 1e-5f) 
        {
            if (!was_surface) 
            {
                is_inside = !is_inside;
                was_surface = true;
            }
        }
        else 
        {
            was_surface = false;

            bool fwd_is_inside = val < 0.0f;
            bool final_inside = fwd_is_inside && is_inside;
    
            float sign = final_inside ? -1.0f : 1.0f;
            _Voxel[index] = FloatFlip3(sign);
        }
    }
}

#endif