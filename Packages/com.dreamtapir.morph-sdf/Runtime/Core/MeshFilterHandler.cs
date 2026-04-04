using UnityEngine;

namespace MorphSDF
{
    public class MeshFilterHandler : MeshHandler
    {
        #region Properties
        public override Matrix4x4 ObjectToSdfLocalMatrix
        {
            get
            {
                var unscaledWorldToSdfLocal = Matrix4x4.TRS(_meshFilter.transform.position, _meshFilter.transform.rotation, Vector3.one).inverse;
                return unscaledWorldToSdfLocal *  _meshFilter.transform.localToWorldMatrix;
            }
        }
        #endregion

        #region Fields
        private readonly MeshFilter _meshFilter;
        #endregion

        public MeshFilterHandler(MeshFilter meshFilter, int stream = 0) : base(meshFilter.sharedMesh, stream)
        {
            _meshFilter = meshFilter;
        }
    }
}