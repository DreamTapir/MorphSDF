using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MorphSDF
{
    public interface IMesh : IDisposable
    {
        int VertexCount { get; }
        int IndexCount { get; }
        int TriangleCount { get; }
        Matrix4x4 ObjectToSdfLocalMatrix { get; }

        void SetParams(CommandBuffer cmb);
    }
}
