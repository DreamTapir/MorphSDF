using UnityEngine;

namespace MorphSDF
{
    public class SkinnedMeshHandler : MeshHandler, ISkinnedMeshHandler
    {
        #region Properties
        public override Matrix4x4 ObjectToSdfLocalMatrix
        {
            get
            {
                var unscaledWorldToSdfLocal = Matrix4x4.TRS(_skinnedMeshRenderer.rootBone.position, _skinnedMeshRenderer.rootBone.rotation, Vector3.one).inverse;
                return unscaledWorldToSdfLocal * _skinnedMeshRenderer.transform.localToWorldMatrix;
            }
        }
        #endregion

        #region Fields
        private readonly SkinnedMeshRenderer _skinnedMeshRenderer;
        #endregion

        public SkinnedMeshHandler(SkinnedMeshRenderer skinnedMeshRenderer, int stream = 0) : base(new Mesh(), stream)
        {
            if (skinnedMeshRenderer == null || skinnedMeshRenderer.sharedMesh == null)
            {
                Debug.LogError("Renderer or SharedMesh is null.");
                return;
            }
            
            _skinnedMeshRenderer = skinnedMeshRenderer;
            Mesh.MarkDynamic();
            _skinnedMeshRenderer.BakeMesh(Mesh);
            UpdateBufferHandles();
        }
        
        private void UpdateBufferHandles()
        {
            ReleaseBuffer();

            VertexBuffer = Mesh.GetVertexBuffer(Stream);
            IndexBuffer = Mesh.GetIndexBuffer();
        }
        
        public void BakeMesh()
        {
            if (!Initialized || Disposed) return;

            _skinnedMeshRenderer.BakeMesh(Mesh);
            UpdateBufferHandles();
        }

        public void SetRender(bool enable)
        {
            _skinnedMeshRenderer.enabled = enable;
        }
        
        #region IDisposable
        public override void Dispose()
        {
            if (Mesh != null)
            {
                UnityEngine.Object.Destroy(Mesh);
            }

            base.Dispose();
        }
        #endregion
    }
}