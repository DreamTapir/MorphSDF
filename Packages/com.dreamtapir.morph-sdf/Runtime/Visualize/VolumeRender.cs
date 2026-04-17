using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace MorphSDF
{
    [Serializable]
    public class VolumeRenderParams
    {
        public int VolumeSteps = 100;
        public float AlphaStrength = 0.05f;
        public VolumeRender.RenderMode Mode = VolumeRender.RenderMode.Volume;
        [Range(0, 2)] public int SliceAxis = 2;
        [Range(0f, 1f)] public float SliceDepth = 0.5f;
    }
    
    public class VolumeRender : IDisposable
    {
        public enum RenderMode { Volume, Slice, SDF }

        public Matrix4x4 LocalToWorld { get; set; } = Matrix4x4.identity;

        private CommandBuffer _cmb;
        private Material _mat;
        private MaterialPropertyBlock _mpb;
        private Mesh _mesh;
        private bool _active = false;

        private static readonly int _sdfId = Shader.PropertyToID("_Sdf");
        private static readonly int _volumeStepsId = Shader.PropertyToID("_VolumeSteps");
        private static readonly int _alphaStrengthId = Shader.PropertyToID("_AlphaStrength");
        private static readonly int _renderModeId = Shader.PropertyToID("_RenderMode");
        private static readonly int _sliceAxisId = Shader.PropertyToID("_SliceAxis");
        private static readonly int _sliceDepthId = Shader.PropertyToID("_SliceDepth");
        
        public VolumeRender(Matrix4x4 matrix)
        {
            LocalToWorld = matrix;
            Initialize();
        }
        
        public VolumeRender(Vector3 position, Vector3 scale) : this(position, scale, Quaternion.identity) {}
        
        public VolumeRender(Vector3 position, Vector3 scale, Quaternion rotation)
        {
            LocalToWorld = Matrix4x4.TRS(position, rotation, scale);
            Initialize();
        }
        
        private void OnEndContextRendering(ScriptableRenderContext context, List<Camera> cameras)
        {
            if (!_active || _mat == null || _mesh == null) return;

            _cmb.Clear();
            _cmb.SetExecutionFlags(CommandBufferExecutionFlags.None);
            _cmb.DrawMesh(_mesh, LocalToWorld, _mat, 0, -1, _mpb);
            context.ExecuteCommandBuffer(_cmb);
            context.Submit();
        }

        private void Initialize()
        {
            if (_mat != null) Object.DestroyImmediate(_mat);
            _mat = new Material(Resources.Load<Shader>("VolumeRender"));
            _mpb = new MaterialPropertyBlock();
            _mesh = new Mesh { name = "VolumePoint" };
            _mesh.vertices = new[] { Vector3.zero };
            _mesh.SetIndices(new [] { 0 }, MeshTopology.Points, 0);
            _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.733f);
            _cmb = new CommandBuffer(){name = "VolumeRender"};
            RenderPipelineManager.endContextRendering += OnEndContextRendering;
        }
        
        public void SetParams(Texture texture, int volumeSteps = 100, float alphaStrength = 0.05f, RenderMode mode = RenderMode.Volume, int sliceAxis = 2, float sliceDepth = 0.5f)
        {
            _mpb.SetTexture(_sdfId, texture);
            _mpb.SetInt(_volumeStepsId, volumeSteps);
            _mpb.SetFloat(_alphaStrengthId, alphaStrength);
            _mpb.SetInt(_renderModeId, (int)mode);
            _mpb.SetInt(_sliceAxisId, sliceAxis);
            _mpb.SetFloat(_sliceDepthId, sliceDepth);
        }

        public void SetParams(Texture texture, in VolumeRenderParams renderParams)
        {
            SetParams(texture, renderParams.VolumeSteps, renderParams.AlphaStrength, renderParams.Mode, renderParams.SliceAxis, renderParams.SliceDepth);
        }

        public void SetActive(bool active)
        {
            _active = active;
        }
        
        #region IDisposable
        ~VolumeRender()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                if (_mat != null) Object.DestroyImmediate(_mat);
                if (_mesh != null) Object.DestroyImmediate(_mesh);
                RenderPipelineManager.endContextRendering -= OnEndContextRendering;
                _cmb.Dispose();
                _cmb = null;
            }
            catch (Exception)
            {
                // ignored
            }
        }
        #endregion
    }
}