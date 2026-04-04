using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using System.Threading;
#endif

namespace MorphSDF
{
    /// <summary>
    /// Computes Signed Distance Fields (SDF) from meshes.
    /// It achieves high-speed performance through GPU parallelization using the PBA+ algorithm.
    /// </summary>
    [Serializable]
    public class MeshToSDF : IDisposable
    {
        #region Static
        public static int MaxTextureSize = 1024;
        private static class PropertyID
        {
            public static int ObjectToSdfLocalMatrix = Shader.PropertyToID("_ObjectToSdfLocalMatrix");
            public static int Origin = Shader.PropertyToID("_Origin");
            public static int Resolution = Shader.PropertyToID("_Resolution");
            public static int CellSize = Shader.PropertyToID("_CellSize");
            public static int Voxel = Shader.PropertyToID("_Voxel");
            public static int Input = Shader.PropertyToID("_Input");
            public static int Output = Shader.PropertyToID("_Output");
            public static int Sdf = Shader.PropertyToID("_Sdf");
        }

        private static class Label
        {
            public static string MeshToSDF = "MeshToSDF";
            public static string Initialize = "Initialize";
            public static string MarkSurfaceVoxels = "MarkSurfaceVoxels";
            public static string BidirectionalRaycast = "BidirectionalRaycast";
            public static string Volume = "Volume";
            public static string FloodZ = "FloodZ";
            public static string MaurerAxis = "MaurerAxis";
            public static string ColorAxis = "ColorAxis";
            public static string SignedDistance = "SignedDistance";
        }
        #endregion
        
        #region Properties
        public RenderTexture SDF { get; private set; }
        #endregion

        #region Fields
        private readonly IMesh _mesh;
        private readonly Dictionary<string, int> _kernels = new();
        private readonly ComputeShader _cs;
        
        private CommandBuffer _cmb;
        private GraphicsBuffer _voxel;
        private GraphicsBuffer _input;
        private GraphicsBuffer _output;
        
        private readonly int[] _resolution;
        private readonly float _cellSize;
        private readonly Vector3 _origin;

        private bool _autoBakeMesh = false;
        private bool _initialized = false;
        private bool _disposed = false;
        #endregion

        public MeshToSDF(SkinnedMeshRenderer skinnedMeshRenderer, int voxelPerMeter, Bounds bounds, TextureFormat format = TextureFormat.RFloat, bool autoBakeMesh = true) : this(new SkinnedMeshHandler(skinnedMeshRenderer), voxelPerMeter, bounds, format)
        {
            _autoBakeMesh = autoBakeMesh;
        }
        public MeshToSDF(MeshFilter meshFilter, int voxelPerMeter, Bounds bounds, TextureFormat format = TextureFormat.RFloat) : this(new MeshFilterHandler(meshFilter), voxelPerMeter, bounds, format) {}
        public MeshToSDF(Mesh mesh, int voxelPerMeter, Bounds bounds, TextureFormat format = TextureFormat.RFloat) : this(new MeshHandler(mesh), voxelPerMeter, bounds, format) {}
        
        public MeshToSDF(IMesh mesh, int voxelPerMeter, Bounds bounds, TextureFormat format = TextureFormat.RFloat)
        {
            _disposed = false;
            _mesh = mesh;
            _cs = Resources.Load<ComputeShader>(nameof(MeshToSDF));
            _cmb = new CommandBuffer{ name = nameof(MeshToSDF) };
            
            var resolution = new Vector3Int(
                Mathf.Clamp(Mathf.CeilToInt(bounds.size.x * voxelPerMeter), 1, MaxTextureSize),
                Mathf.Clamp(Mathf.CeilToInt(bounds.size.y * voxelPerMeter), 1, MaxTextureSize),
                Mathf.Clamp(Mathf.CeilToInt(bounds.size.z * voxelPerMeter), 1, MaxTextureSize)
            );
            _resolution = new []{ resolution.x, resolution.y, resolution.z };
            _origin = bounds.center - bounds.extents;
            _cellSize = 1f / voxelPerMeter;
            var length = resolution.x * resolution.y * resolution.z;
            
            RegisterKernel(Label.Initialize);
            RegisterKernel(Label.MarkSurfaceVoxels);
            RegisterKernel(Label.BidirectionalRaycast);
            RegisterKernel(Label.Volume);
            RegisterKernel(Label.FloodZ);
            RegisterKernel(Label.MaurerAxis);
            RegisterKernel(Label.ColorAxis);
            RegisterKernel(Label.SignedDistance);
            
            _voxel = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, sizeof(uint)){ name = $"{nameof(MeshToSDF)}_Voxel"};
            _input = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, sizeof(int)){ name = $"{nameof(MeshToSDF)}_Input" };
            _output = new GraphicsBuffer(GraphicsBuffer.Target.Structured, length, sizeof(int)) { name = $"{nameof(MeshToSDF)}_Output"};
            var textureFormat = format == TextureFormat.RFloat ? RenderTextureFormat.RFloat : RenderTextureFormat.RHalf;
            SDF = new RenderTexture(_resolution[0], _resolution[1], 0, textureFormat)
            {
                enableRandomWrite = true,
                useMipMap = false,
                dimension = TextureDimension.Tex3D,
                volumeDepth = _resolution[2],
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                name = $"{nameof(MeshToSDF)}_SDF"
            };
            SDF.Create();
            
            _initialized = true;
        }

        #region Private
        private void RegisterKernel(string kernelName)
        {
            _kernels[kernelName] = _cs.FindKernel(kernelName);
        }

        private (int x, int y, int z) GetThreadSize(int kernelId)
        {
            _cs.GetKernelThreadGroupSizes(kernelId, out uint x, out uint y, out uint z);
            return ((int)x, (int)y, (int)z);
        }
        
        private int GetGroupSize(int desired, int threadNum)
        {
            return Mathf.Max(1, (desired + threadNum - 1) / threadNum);
        }
        
        private void Dispatch(CommandBuffer cb, string kernelName, int x, int y = 1, int z = 1)
        {
            int kernelId = _kernels[kernelName];
            var ts = GetThreadSize(kernelId);
            cb.BeginSample(kernelName);
            cb.DispatchCompute(_cs, kernelId, GetGroupSize(x, ts.x), GetGroupSize(y, ts.y), GetGroupSize(z, ts.z));
            cb.EndSample(kernelName);
        }

        private void Dispatch(CommandBuffer cb, string kernelName, int[] size)
        {
            Dispatch(cb, kernelName, size[0], size[1], size[2]);
        }
        
        private void SwapResolutionXY(CommandBuffer cb)
        {
            (_resolution[0], _resolution[1]) = (_resolution[1], _resolution[0]);
            cb.SetComputeIntParams(_cs, PropertyID.Resolution, _resolution);
        }

        private void ComputeMaurerAxis(CommandBuffer cb)
        {
            int kernel = _kernels[Label.MaurerAxis];
            cb.SetComputeBufferParam(_cs, kernel, PropertyID.Input, _input);
            cb.SetComputeBufferParam(_cs, kernel, PropertyID.Output, _output);
            Dispatch(cb, Label.MaurerAxis, _resolution[0], _resolution[2]);
            (_input, _output) = (_output, _input);
        }

        private void ComputeColorAxis(CommandBuffer cb)
        {
            int kernel = _kernels[Label.ColorAxis];
            cb.SetComputeBufferParam(_cs, kernel, PropertyID.Input, _input);
            cb.SetComputeBufferParam(_cs, kernel, PropertyID.Output, _output);
            _cs.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            cb.BeginSample(Label.ColorAxis);
            cb.DispatchCompute(_cs, kernel, GetGroupSize(_resolution[0], (int)x), _resolution[2], 1);
            cb.EndSample(Label.ColorAxis);
            (_input, _output) = (_output, _input);
        }
        #endregion

        #region Public
        public T GetHandler<T>() where T : class, IMesh
        {
            var handler = _mesh as T;
            return handler;
        }
        
        public void SetCommand(CommandBuffer cmb)
        {
            if (!_initialized || _disposed) return;

            if (_autoBakeMesh && _mesh is SkinnedMeshHandler handler)
            {
                handler.BakeMesh();
            }
            
            cmb.BeginSample(Label.MeshToSDF);
            
            cmb.SetComputeVectorParam(_cs, PropertyID.Origin, _origin);
            cmb.SetComputeFloatParam(_cs, PropertyID.CellSize, _cellSize);
            cmb.SetComputeIntParams(_cs, PropertyID.Resolution, _resolution);
            
            int kernel = _kernels[Label.Initialize];
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Voxel, _voxel);
            Dispatch(cmb, Label.Initialize, _resolution);

            kernel = _kernels[Label.MarkSurfaceVoxels];
            _mesh.SetParams(cmb);
            cmb.SetComputeMatrixParam(_cs, PropertyID.ObjectToSdfLocalMatrix, _mesh.ObjectToSdfLocalMatrix);
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Voxel, _voxel);
            Dispatch(cmb, Label.MarkSurfaceVoxels, _mesh.TriangleCount);
            
            kernel = _kernels[Label.BidirectionalRaycast];
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Voxel, _voxel);
            Dispatch(cmb, Label.BidirectionalRaycast, _resolution[0], _resolution[1]);
            
            kernel = _kernels[Label.Volume];
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Voxel, _voxel);
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Output, _input);
            Dispatch(cmb, Label.Volume, _resolution);
            
            kernel = _kernels[Label.FloodZ];
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Input, _input);
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Output, _output);
            Dispatch(cmb, Label.FloodZ, _resolution[0], _resolution[1]);
            (_input, _output) = (_output, _input);
            
            ComputeMaurerAxis(cmb);
            ComputeColorAxis(cmb);
            SwapResolutionXY(cmb);
            ComputeMaurerAxis(cmb);
            ComputeColorAxis(cmb);
            SwapResolutionXY(cmb);
            
            kernel = _kernels[Label.SignedDistance];
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Voxel, _voxel);
            cmb.SetComputeBufferParam(_cs, kernel, PropertyID.Input, _input);
            cmb.SetComputeTextureParam(_cs, kernel, PropertyID.Sdf, SDF);
            Dispatch(cmb, Label.SignedDistance, _resolution);
            
            cmb.EndSample(Label.MeshToSDF);
        }
                
        public void BakeSDF()
        {
            if (!_initialized || _disposed) return;
            
            _cmb.Clear();
            _cmb.SetExecutionFlags(CommandBufferExecutionFlags.None);
            
            SetCommand(_cmb);
            Graphics.ExecuteCommandBuffer(_cmb);
        }

        public void BakeSDFAsync(ComputeQueueType queueType)
        {
            if (!_initialized || _disposed) return;
            
            _cmb.Clear();
            _cmb.SetExecutionFlags(CommandBufferExecutionFlags.AsyncCompute);
            
            SetCommand(_cmb);
            Graphics.ExecuteCommandBufferAsync(_cmb, queueType);
        }
        
        public async Awaitable<bool> BakeSDFAsync(ComputeQueueType queueType, CancellationToken token)
        {
            if (!_initialized || _disposed) return false;

            BakeSDFAsync(queueType);
            
            GraphicsFence fence = Graphics.CreateGraphicsFence
            (
                GraphicsFenceType.AsyncQueueSynchronisation, 
                SynchronisationStageFlags.ComputeProcessing
            );

            while (!fence.passed)
            {
                await Awaitable.NextFrameAsync(); 
        
                if (_disposed || token.IsCancellationRequested) return false;
            }

            return true;
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_disposed) return;

            _mesh?.Dispose();
                
            _cmb?.Release();
            _cmb = null;
                
            _voxel?.Release();
            _input?.Release();
            _output?.Release();
                
            if (SDF != null)
            {
                SDF.Release();
                if (Application.isPlaying) UnityEngine.Object.Destroy(SDF);
                else UnityEngine.Object.DestroyImmediate(SDF);
            }

            _initialized = false;
            _disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}