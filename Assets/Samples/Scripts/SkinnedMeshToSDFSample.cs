using System;
using System.Threading;
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
        [SerializeField] private int _bakeType = 0;
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
        private volatile bool _processing = false;
        private readonly CancellationTokenSource _cts = new();
        
        private Matrix4x4 SdfLocalToWorldMatrix => Matrix4x4.TRS(_centerTransform.position, _centerTransform.rotation, _size);
        
        private void Start()
        {
            var skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

            _skinnedMeshHandler = new SkinnedMeshHandler(skinnedMeshRenderer, _centerTransform);
            _sdfBaker = new MeshToSDF(_skinnedMeshHandler, _voxelPerMeter, new Bounds(Vector3.zero, _size), _format);
            _volumeRender = new VolumeRender(SdfLocalToWorldMatrix);

        }

        private async void Update()
        {
            switch (_bakeType)
            {
                case 0:
                    _skinnedMeshHandler?.BakeMesh();
                    _sdfBaker?.BakeSDF();
                    RenderVolume();
                    break;
                case 1:
                    _skinnedMeshHandler?.BakeMesh();
                    _sdfBaker?.BakeSDFAsync(_queueType);
                    RenderVolume();
                    break;
                case 2:
                    if (_processing) return;

                    try
                    {
                        _processing = true;

                        _skinnedMeshHandler?.BakeMesh();
                        var success = await _sdfBaker?.BakeSDFAsync(_queueType, _cts.Token);

                        _processing = false;

                        if (!success) return;
                        RenderVolume();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    finally
                    {
                        _processing = false;
                    }
                    break;
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
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = _fontSize,
            };

            GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = _fontSize,
            };

            GUILayout.BeginArea(new Rect(20, 20, Screen.width / 2, 100), GUI.skin.box);
            GUILayout.Label("0: BakeSDF, 1: BakeSDFAsync: 2: Awaitable BakeSDFAsync", labelStyle);
            Int32.TryParse(GUILayout.TextField(_bakeType.ToString(), fieldStyle), out _bakeType);
            GUILayout.EndArea();
        }
    }
}