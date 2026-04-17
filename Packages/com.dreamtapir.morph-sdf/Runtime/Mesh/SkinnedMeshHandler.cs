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
                var unscaledWorldToSdfLocal = Matrix4x4.TRS(_center.position, _center.rotation, Vector3.one).inverse;
                return unscaledWorldToSdfLocal * _skinnedMeshRenderer.transform.localToWorldMatrix;
            }
        }
        #endregion

        #region Fields
        private readonly SkinnedMeshRenderer _skinnedMeshRenderer;
        private readonly Transform _center;
        #endregion

        public SkinnedMeshHandler(SkinnedMeshRenderer skinnedMeshRenderer, Transform center = null, int stream = 0) :  base(() =>
        {
            if (skinnedMeshRenderer.sharedMesh == null)
            {
                Debug.LogError("SharedMesh is null.");
                return null;
            }

            var mesh = new Mesh();
            mesh.MarkDynamic();
            skinnedMeshRenderer.BakeMesh(mesh);
            return mesh;
        }, stream)
        {
            _skinnedMeshRenderer = skinnedMeshRenderer;
            _center = center ?? _skinnedMeshRenderer.rootBone;
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

        public void SetActive(bool enable)
        {
            _skinnedMeshRenderer.enabled = enable;
        }
        
        #region IDisposable
        public override void Dispose()
        {
            if (Mesh != null)
            {
                Object.Destroy(Mesh);
            }

            base.Dispose();
        }
        #endregion
    }
}