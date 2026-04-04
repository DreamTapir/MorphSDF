using UnityEngine;
using UnityEngine.Rendering;

namespace MorphSDF.Editor
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class StaticSdfGenerator : MonoBehaviour
    {
        [SerializeField] private int _voxelPerMeter = 64;
        [SerializeField] private Vector3 _center = Vector3.zero;
        [SerializeField] private Vector3 _size = Vector3.one;
        [SerializeField] private TextureFormat _format = TextureFormat.RFloat;
        
        [Header("Debug")]
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

        #region MonoBehaviour

        private void OnDestroy()
        {
            DisposeResources();
        }
        #endregion

        public void Bake()
        {
            DisposeResources();

            _meshFilter ??= GetComponent<MeshFilter>();
            if (_meshFilter.sharedMesh == null) return;

            _baker = new MeshToSDF(_meshFilter, _voxelPerMeter, new Bounds(_center, _size), _format);
            _render = new VolumeRender(SdfLocalToWorldMatrix);

            _baker.BakeSDF();
            _sdf = _baker.SDF;
            RenderPreview();
        }
        
        private void DisposeResources()
        {
            _baker?.Dispose();
            _baker = null;
            _render?.Dispose();
            _render = null;
        }

        private void RenderPreview()
        {
            if (_render != null && _sdf != null)
            {
                _render.SetParams(_sdf, _volumeSteps, _alphaStrength, _previewMode, _sliceAxis, _sliceDepth);
            }
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
        
        private void OnDrawGizmos()
        {
            // var matrix = Gizmos.matrix;
            // var color = Gizmos.color;
            // Gizmos.matrix = SdfLocalToWorldMatrix;
            // Gizmos.color = Color.green;
            // Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
            // Gizmos.color = color;
            // Gizmos.matrix = matrix;
        }
#endif
    }
}