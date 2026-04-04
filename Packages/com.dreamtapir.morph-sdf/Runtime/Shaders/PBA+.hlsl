#ifndef MORPH_SDF_HLSL_INCLUDE_PBA_PLUS
#define MORPH_SDF_HLSL_INCLUDE_PBA_PLUS

/*

Project homepage: http://www.comp.nus.edu.sg/~tants/pba.html
Github: https://github.com/PRIArobotics/Parallel-Banding-Algorithm-plus-py/tree/master

*/

#include "Packages/com.dreamtapir.morph-sdf/Runtime/Shaders/Common.hlsl"

// Sites 	 : ENCODE(x, y, z, 0, 0)
// Not sites : ENCODE(0, 0, 0, 1, 0) or MARKER
inline int Encode(int x, int y, int z, int a, int b)
{
	return x << 20 | y << 10 | z | a << 31 | b << 30;
}

inline int Encode(int3 id, int a, int b)
{
	return Encode(id.x, id.y, id.z, a, b);
}

inline void Decode(int value, out int x, out int y, out int z)
{
	x = (value >> 20) & 0x3ff;
	y = (value >> 10) & 0x3ff;
	z = value & 0x3ff;
}

inline bool NotSite(int value)
{
	return (value >> 31) & 1;
}

inline bool HasNext(int value)
{
	return  (value >> 30) & 1;
}

inline int GetZ(int value)
{
	return NotSite(value) ? MAX_INT : value & 0x3ff;
}

inline int3 GetPosition(int value)
{
	return int3((value >> 20) & 0x3ff, (value >> 10) & 0x3ff, GetZ(value));
}

bool dominate(int x_1, int y_1, int z_1, int x_2, int y_2, int z_2, int x_3, int y_3, int z_3, int x_0, int z_0)
{
	int k_1 = y_2 - y_1, k_2 = y_3 - y_2;

	return ((y_1 + y_2) * k_1 + ((x_2 - x_1) * (x_1 + x_2 - (x_0 << 1)) + (z_2 - z_1) * (z_1 + z_2 - (z_0 << 1)))) * k_2 >
			((y_2 + y_3) * k_2 + ((x_3 - x_2) * (x_2 + x_3 - (x_0 << 1)) + (z_3 - z_2) * (z_2 + z_3 - (z_0 << 1)))) * k_1;
}

groupshared int transpose_block[BLOCK_SIZE][BLOCK_SIZE];

[numthreads(BLOCK_X, BLOCK_Y, 1)]
void FloodZ(uint3 dispatch_thread_id : SV_DispatchThreadID) 
{
	if (any(dispatch_thread_id >= (uint3)_Resolution)) return;
	
	const int3 id = int3(dispatch_thread_id.xy, 0); 
    const int plane = _Resolution.x * _Resolution.y;
	
    int index = IDToIndex(id, _Resolution); 
    int pixel_1 = 0, pixel_2 = 0;  

    pixel_1 = Encode(0,0,0,1,0); 

    // Sweep down
    for (int i = 0; i < _Resolution.z; i++, index += plane)
    {
        pixel_2 = _Input[index];

        if (!NotSite(pixel_2))
        {
            pixel_1 = pixel_2;
        }

        _Output[index] = pixel_1;
    }

	int dist_1 = 0, dist_2 = 0, n_z = 0;

	index -= plane + plane;

    // Sweep up
    for (int j = _Resolution.z - 2; j >= 0; j--, index -= plane)
    {
        n_z = GetZ(pixel_1);
        dist_1 = abs(n_z - (id.z + j));

        pixel_2 = _Output[index];
        n_z = GetZ(pixel_2);
        dist_2 = abs(n_z - (id.z + j));

        if (dist_2 < dist_1)
        {
            pixel_1 = pixel_2;
        }

        _Output[index] = pixel_1;
    }
}

[numthreads(BLOCK_X, BLOCK_Y, 1)]
void MaurerAxis(uint3 dispatch_thread_id : SV_DispatchThreadID) 
{
	if (any(dispatch_thread_id.xy >= (uint2)_Resolution.xz)) return;
	
	int3 t = int3(dispatch_thread_id.x, 0, dispatch_thread_id.y);
    int y_last = 0;
    int x_1 = 0, y_1 = 0, z_1 = 0, x2 = 0, y_2 = 0, z_2 = 0, n_x = 0, n_y = 0, n_z = 0;
    int p = Encode(0,0,0,1,0), s_1 = Encode(0,0,0,1,0), s_2 = Encode(0,0,0,1,0);
    int flag = 0;

    for (t.y = 0; t.y < _Resolution.y; ++t.y)
    {
    	int index = IDToIndex(t, _Resolution);
        p = _Input[index];

        if (NotSite(p)) continue;

    	for (int limit = 0; limit < _Resolution.y; limit++)
    	{
    		if (!HasNext(s_2)) break;
    		
    		Decode(s_1, x_1, y_1, z_1);
    		Decode(s_2, x2, y_2, z_2);
    		Decode(p, n_x, n_y, n_z);

    		if (!dominate(x_1, y_2, z_1, x2, y_last, z_2, n_x, t.y, n_z, t.x, t.z))
    			break;

    		y_last = y_2;
    		s_2 = s_1;
    		y_2 = y_1;

    		if (HasNext(s_2))
    		{
    			s_1 = _Output[IDToIndex(t.x, y_2, t.z, _Resolution)];
    		}
    	}

        Decode(p, n_x, n_y, n_z);
        s_1 = s_2;
        s_2 = Encode(n_x, y_last, n_z, 0, flag);
        y_2 = y_last;
        y_last = t.y;

        _Output[index] = s_2;

        flag = 1;
    }

    if (NotSite(p))
    {
        _Output[IDToIndex(t.x, t.y - 1, t.z, _Resolution)] = Encode(0, y_last, 0, 1, flag); 
    }
}

[numthreads(BLOCK_X, BLOCK_Y, 1)]
void ColorAxis(uint3 group_thread_id : SV_GroupThreadID, uint3 group_id : SV_GroupID) 
{
	const int t_id = group_thread_id.y;
	const int t_x = group_id.x * BLOCK_X + group_thread_id.x; 
	const int t_z = group_id.y;
	const bool is_valid = (t_x < _Resolution.x) && (t_z < _Resolution.z);
 
    int x_1 = 0, y_1 = 0, z_1 = 0, x_2 = 0, y_2 = 0, z_2 = 0;
    int last_1 = Encode(0,0,0,1,0), last_2 = Encode(0,0,0,1,0), y_last = _Resolution.y - 1;
    int dx = 0, dy = 0, dz = 0, best = 0, dist = 0;

	if (is_valid)
	{
		last_2 = _Input[IDToIndex(t_x, y_last, t_z, _Resolution)]; 
		Decode(last_2, x_2, y_2, z_2);

		if (NotSite(last_2))
		{
			y_last = y_2;
			if(HasNext(last_2))
			{
				last_2 = _Input[IDToIndex(t_x, y_last, t_z, _Resolution)];
				Decode(last_2, x_2, y_2, z_2);
			}
		}

		if (HasNext(last_2))
		{
			last_1 = _Input[IDToIndex(t_x, y_2, t_z, _Resolution)];
			Decode(last_1, x_1, y_1, z_1);
		}
	}

	int y_start = 0, y_end = 0, n_step = (_Resolution.y + BLOCK_X - 1) / (uint)BLOCK_X;
	for (int step = 0; step < n_step; ++step)
	{
		y_start = _Resolution.y - step * BLOCK_X - 1;
		y_end = _Resolution.y - (step + 1) * BLOCK_X;
		
		if (is_valid)
		{
			for (int t_y = y_start - t_id; t_y >= y_end; t_y -= BLOCK_Y)
			{
				dx = x_2 - t_x;
				dy = y_last - t_y;
				dz = z_2 - t_z;
				best = dx * dx + dy * dy + dz * dz;

				while (HasNext(last_2))
				{
					dx = x_1 - t_x;
					dy = y_2 - t_y;
					dz = z_1 - t_z;
					dist = dx * dx + dy * dy + dz * dz;

					if(dist > best) break;

					best = dist;
					y_last = y_2;
					last_2 = last_1;
					Decode(last_2, x_2, y_2, z_2);

					if (HasNext(last_2))
					{
						last_1 = _Input[IDToIndex(t_x, y_2, t_z, _Resolution)];
						Decode(last_1, x_1, y_1, z_1); 
					}
				}

				transpose_block[group_thread_id.x][t_y - y_end] = Encode(y_last, x_2, z_2, NotSite(last_2), 0);
			}
		}

		GroupMemoryBarrierWithGroupSync();

	    if(t_z < _Resolution.z && !group_thread_id.y)
	    {
	    	const int out_x = y_end + group_thread_id.x;
	    	const int out_y = group_id.x * BLOCK_X;
    
	    	int index = (t_z * _Resolution.x + out_y) * _Resolution.y + out_x;
	    	
	    	for (int i = 0; i < BLOCK_X; i++, index += _Resolution.y)
	    	{
	    		if (out_y + i < _Resolution.x && out_x >= 0 && out_x < _Resolution.y)
	    		{
	    			_Output[index] = transpose_block[i][group_thread_id.x];
	    		}
	    	}
	    }

		GroupMemoryBarrierWithGroupSync();
	}
}

[numthreads(8, 8, 8)]
void SignedDistance(uint3 dispatch_thread_id : SV_DispatchThreadID)
{
	if (any(dispatch_thread_id >= (uint3)_Resolution)) return;
	
	const int index = IDToIndex(dispatch_thread_id, _Resolution);
	const uint raw = _Voxel[index];
	float s = sign(asfloat(IFloatFlip3(raw)));

	_Sdf[dispatch_thread_id] = s * (float)distance(GetPosition(_Input[index]), (int3)dispatch_thread_id) * _CellSize;
}

#endif