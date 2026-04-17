using UnityEngine;
using UnityEngine.Rendering;

namespace MorphSDF.Sample
{
    public class SkinnedMeshToSDFSample : MonoBehaviour
    {
        [Header("SDF Settings")]
        [SerializeField] private TextureFormat _format = TextureFormat.RFloat;
        [SerializeField] private int _voxelPerMeter = 64;
        [SerializeField] private Vector3 _size = Vector3.one;
        [SerializeField] private Transform _centerTransform;
        [SerializeField] private bool _computeAsync = false;
        [SerializeField] private ComputeQueueType _queueType = ComputeQueueType.Background;

        [Header("Debug")]
        [SerializeField] private bool _renderMesh = true;
        [SerializeField] private bool _renderVolume = true;
        [SerializeField] private VolumeRenderParams _volumeRenderParams;
        
        [Header("UI")]
        [SerializeField] private int _fontSize = 20;
        
        private MeshToSDF _sdfBaker;
        private SkinnedMeshHandler _skinnedMeshHandler;
        private VolumeRender _volumeRender;
        
        private Matrix4x4 SdfLocalToWorldMatrix => Matrix4x4.TRS(_centerTransform.position, _centerTransform.rotation, _size);
        
        private void Start()
        {
            var skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

            _skinnedMeshHandler = new SkinnedMeshHandler(skinnedMeshRenderer, _centerTransform);
            _sdfBaker = new MeshToSDF(_skinnedMeshHandler, _voxelPerMeter, new Bounds(Vector3.zero, _size), _format);
            _volumeRender = new VolumeRender(SdfLocalToWorldMatrix);

        }

        private void Update()
        {
            Bake();
            RenderVolume();
        }

        private void Bake()
        {
            _skinnedMeshHandler?.BakeMesh();

            if (_computeAsync)
            {
                _sdfBaker?.BakeSDFAsync(_queueType);
            }
            else
            {
                _sdfBaker?.BakeSDF();
            }
        }

        private void RenderVolume()
        {
            _skinnedMeshHandler?.SetActive(_renderMesh);
            _volumeRender.SetActive(_renderVolume);
            if (!_renderVolume) return;
            _volumeRender.LocalToWorld = SdfLocalToWorldMatrix;
            _volumeRender.SetParams(_sdfBaker.SDF, in _volumeRenderParams);
        }

        private void OnDestroy()
        {
            _skinnedMeshHandler?.Dispose();
            _sdfBaker?.Dispose();
            _volumeRender?.Dispose();
        }

        private void OnGUI()
        {
            var rect = new Rect(_fontSize / 2f, _fontSize / 2f, Screen.width / 2f, _fontSize * 1.5f);
            var boxSize = 10;
            
            GUIStyle style = new GUIStyle(GUI.skin.toggle)
            {
                fontSize = _fontSize,
                alignment = TextAnchor.MiddleLeft
            };

            style.padding = new RectOffset(
                _fontSize + boxSize,
                style.padding.right,
                style.padding.top,
                style.padding.bottom
            );

            _computeAsync = GUI.Toggle(rect, _computeAsync, "Compute Async", style);
        }
    }
}