using UnityEngine;

namespace MorphSDF.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class StaticSDFBakeTool : MonoBehaviour
    {
        [Header("SDF Settings")]
        [SerializeField] private TextureFormat _format = TextureFormat.RFloat;
        [SerializeField] private int _voxelPerMeter = 64;
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _size = Vector3.one;
        
        [Header("Preview Settings")]
        [SerializeField] private VolumeRender.RenderMode _previewMode = VolumeRender.RenderMode.Volume;
        [SerializeField, Range(0, 2)] private int _sliceAxis = 2; // 0: X軸, 1: Y軸, 2: Z軸
        [SerializeField, Range(0f, 1f)] private float _sliceDepth = 0.5f; // 断面の深さ
        [SerializeField] private int _volumeSteps = 100;
        [SerializeField] private float _alphaStrength = 0.01f;

        [SerializeField, HideInInspector] private RenderTexture _sdf;
        private MeshToSDF _baker;
        private VolumeRender _render;
        private MeshFilter _meshFilter;
        
        private Matrix4x4 SdfLocalToWorldMatrix => Matrix4x4.TRS(transform.position + _center, transform.rotation, _size);

        private void OnValidate()
        {
            UpdatePreview();
        }
        
        private void OnDestroy()
        {
            DisposeResources();
        }
        
        private void DisposeResources()
        {
            _baker?.Dispose();
            _baker = null;
            _render?.Dispose();
            _render = null;
        }

        private void UpdatePreview()
        {
            if (_render != null && _sdf != null)
            {
                _render.LocalToWorld = SdfLocalToWorldMatrix;
                _render.SetParams(_sdf, _volumeSteps, _alphaStrength, _previewMode, _sliceAxis, _sliceDepth);
            }
        }

        public void Bake()
        {
            DisposeResources();

            _meshFilter ??= GetComponent<MeshFilter>();
            if (_meshFilter.sharedMesh == null) return;

            _baker = new MeshToSDF(_meshFilter, _voxelPerMeter, new Bounds(_center, _size), _format);
            _render = new VolumeRender(SdfLocalToWorldMatrix);

            _baker.BakeSDF();
            _sdf = _baker.SDF;
            UpdatePreview();
        }
        
#if UNITY_EDITOR
        public void SetEditorFocus(bool isFocused)
        {
            if (Application.isPlaying) return;

            if (isFocused)
            {
                if (_baker == null) Bake();
            }
            else
            {
                DisposeResources();
            }
        }
#endif
    }
}