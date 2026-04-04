Shader "MorphSDF/VolumeRender"
{
    Properties
    {
        [HideInInspector] _Sdf("SDF Texture", 3D) = "" {}
        _VolumeSteps("Volume Steps", Int) = 100
        _AlphaStrength("Alpha Strength", Float) = 0.01
        [Enum(Volume,0,Slice,1,SDF,2)] _RenderMode("Render Mode", Int) = 0
        [Enum(X,0,Y,1,Z,2)] _SliceAxis("Slice Axis", Int) = 2
        _SliceDepth("Slice Depth", Float) = 0.5
    }
    HLSLINCLUDE
    #include "UnityCG.cginc"
    
    struct appdata
    {
        uint vertexID : SV_VertexID;
    };
    
    struct v2g
    {
        float dummy : TEXCOORD0;
    };
    
    v2g vert (appdata v)
    {
        v2g o;
        o.dummy = 0;
        return o;
    }
    ENDHLSL
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct g2f
            {
                float3 uvw : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            sampler3D _Sdf;
            int _RenderMode;
            int _SliceAxis;
            float _SliceDepth;
            int _VolumeSteps;
            float _AlphaStrength;
            
            float3 Hsv2Rgb(float3 c)
            {
	            float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
	            return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
            }
            
            float3 CalcStrengthColor(float3 val)
            {
                float len = length(val);
                return Hsv2Rgb(float3(1.0 - saturate(len), saturate(2.0 - clamp(len, 0.0, 1.25)), len));
            }

            [maxvertexcount(36)]
            void geom(point v2g input[1], inout TriangleStream<g2f> outStream)
            {
                static const float3 positions[8] = {
                    float3(-0.5, -0.5, -0.5), float3( 0.5, -0.5, -0.5),
                    float3(-0.5,  0.5, -0.5), float3( 0.5,  0.5, -0.5),
                    float3(-0.5, -0.5,  0.5), float3( 0.5, -0.5,  0.5),
                    float3(-0.5,  0.5,  0.5), float3( 0.5,  0.5,  0.5)
                };
                
                static const int indices[36] = {
                    0,2,1, 1,2,3, // Back
                    4,5,6, 5,7,6, // Front
                    0,1,4, 1,5,4, // Bottom
                    2,6,3, 3,6,7, // Top
                    0,4,2, 4,6,2, // Left
                    1,3,5, 3,7,5  // Right
                };

                for (int i = 0; i < 36; i += 3)
                {
                    for(int j = 0; j < 3; j++)
                    {
                        g2f o;
                        float3 localPos = positions[indices[i + j]];
                        
                        o.uvw = localPos + 0.5f;
                        float4 worldPos = mul(unity_ObjectToWorld, float4(localPos, 1.0));
                        o.worldPos = worldPos.xyz;
                        o.vertex = mul(UNITY_MATRIX_VP, worldPos);
                        
                        outStream.Append(o);
                    }
                    outStream.RestartStrip();
                }
            }

            float4 frag (g2f i) : SV_Target
            {
                float3 ray_start_uvw = i.uvw;
                float3 view_dir_world;
                
                if (unity_OrthoParams.w == 0.0)
                {
                    view_dir_world = normalize(i.worldPos - _WorldSpaceCameraPos);
                }
                else
                {
                    view_dir_world = normalize(mul((float3x3)unity_CameraToWorld, float3(0, 0, 1)));
                }

                float3 ray_dir_uvw = normalize(mul((float3x3)unity_WorldToObject, view_dir_world));
                
                float3 t_bounds;
                t_bounds.x = (ray_dir_uvw.x > 0.0) ? (1.0 - ray_start_uvw.x) / ray_dir_uvw.x : -ray_start_uvw.x / ray_dir_uvw.x;
                t_bounds.y = (ray_dir_uvw.y > 0.0) ? (1.0 - ray_start_uvw.y) / ray_dir_uvw.y : -ray_start_uvw.y / ray_dir_uvw.y;
                t_bounds.z = (ray_dir_uvw.z > 0.0) ? (1.0 - ray_start_uvw.z) / ray_dir_uvw.z : -ray_start_uvw.z / ray_dir_uvw.z;
                float max_t = min(t_bounds.x, min(t_bounds.y, t_bounds.z));
                
                if (_RenderMode == 1)
                {
                    float t = -1.0;
                    if (_SliceAxis == 0 && abs(ray_dir_uvw.x) > 0.0001) t = (_SliceDepth - ray_start_uvw.x) / ray_dir_uvw.x;
                    if (_SliceAxis == 1 && abs(ray_dir_uvw.y) > 0.0001) t = (_SliceDepth - ray_start_uvw.y) / ray_dir_uvw.y;
                    if (_SliceAxis == 2 && abs(ray_dir_uvw.z) > 0.0001) t = (_SliceDepth - ray_start_uvw.z) / ray_dir_uvw.z;
                    if (t >= 0.0 && t <= max_t)
                    {
                        float3 hit_uvw = ray_start_uvw + ray_dir_uvw * t;
                        float sdf_val = tex3D(_Sdf, hit_uvw).r;
                        return float4(CalcStrengthColor(sdf_val), 1.0);
                    }
                    discard;
                }

                float step_size = max_t / _VolumeSteps;
                float3 ray_uvw = ray_start_uvw;
                float4 color = 0;
                float prev_sdf = tex3D(_Sdf, ray_uvw).r;
                
                [loop]
                for (int s = 0; s < _VolumeSteps; s++)
                {
                    if (_RenderMode == 2)
                    {
                        float current_sdf = tex3D(_Sdf, ray_uvw).r;
                        if (current_sdf <= 0.0)
                        {
                            float t_refine = (prev_sdf > 0.0) ? prev_sdf / (prev_sdf - current_sdf + 0.0001) : 0.0;
                            float3 hit_uvw = ray_uvw - ray_dir_uvw * step_size * (1.0 - t_refine);
                            float3 e = float3(0.01, 0, 0); 
                            float3 n = normalize(float3(
                                tex3D(_Sdf, hit_uvw + e.xyy).r - tex3D(_Sdf, hit_uvw - e.xyy).r,
                                tex3D(_Sdf, hit_uvw + e.yxy).r - tex3D(_Sdf, hit_uvw - e.yxy).r,
                                tex3D(_Sdf, hit_uvw + e.yyx).r - tex3D(_Sdf, hit_uvw - e.yyx).r
                            ));
                            float3 light_dir = normalize(float3(1, 1.5, -1));
                            float diff = max(0.0, dot(n, light_dir)) * 0.7 + 0.3;
                            
                            return float4(diff.xxx, 1.0);
                        }
                        prev_sdf = current_sdf;
                    }
                    else 
                    {
                        const float sdf_val = tex3D(_Sdf, ray_uvw).r;
                        const float3 sample_rgb = CalcStrengthColor(sdf_val);
                        const float sample_alpha = abs(sdf_val) * _AlphaStrength; 
                        float4 sample_color = float4(sample_rgb, sample_alpha);

                        sample_color.rgb *= sample_color.a;
                        color += sample_color * (1.0 - color.a);

                        if (color.a >= 0.99) break;
                    }

                    ray_uvw += ray_dir_uvw * step_size;
                }

                if (_RenderMode == 2) discard;
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            struct g2f
            {
                float4 vertex : SV_POSITION;
            };

            [maxvertexcount(24)]
            void geom(point v2g input[1], inout LineStream<g2f> outStream)
            {
                static const float3 positions[8] = {
                    float3(-0.5, -0.5, -0.5), float3( 0.5, -0.5, -0.5),
                    float3(-0.5,  0.5, -0.5), float3( 0.5,  0.5, -0.5),
                    float3(-0.5, -0.5,  0.5), float3( 0.5, -0.5,  0.5),
                    float3(-0.5,  0.5,  0.5), float3( 0.5,  0.5,  0.5)
                };
                
                static const int indices[24] = {
                    0,1, 1,3, 3,2, 2,0, // Back
                    4,5, 5,7, 7,6, 6,4, // Front
                    0,4, 1,5, 2,6, 3,7  // Connections
                };

                for (int i = 0; i < 24; i += 2)
                {
                    g2f o1, o2;
                    
                    float4 w1 = mul(unity_ObjectToWorld, float4(positions[indices[i]], 1.0));
                    float4 w2 = mul(unity_ObjectToWorld, float4(positions[indices[i+1]], 1.0));
                    
                    o1.vertex = mul(UNITY_MATRIX_VP, w1);
                    o2.vertex = mul(UNITY_MATRIX_VP, w2);
                    
                    outStream.Append(o1);
                    outStream.Append(o2);
                    outStream.RestartStrip(); // 2頂点ごとに線を切断
                }
            }

            float4 frag (g2f i) : SV_Target
            {
                return float4(0.0, 1.0, 0.0, 1.0);
            }
            ENDHLSL
        }   
    }
}