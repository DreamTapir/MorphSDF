using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MorphSDF
{
    public class MeshHandler : IMesh
    {
        #region Static
        protected static class PropertyID
        {
            public static  int VertexBuffer = Shader.PropertyToID("_VertexBuffer");
            public static  int IndexBuffer = Shader.PropertyToID("_IndexBuffer");
            public static  int IndexFormat16Bit = Shader.PropertyToID("_IndexFormat16Bit");
            public static  int Stride = Shader.PropertyToID("_Stride");
            public static  int ColorByteOffset = Shader.PropertyToID("_ColorByteOffset");
            public static  int UvByteOffset = Shader.PropertyToID("_UvByteOffset");
            public static  int PositionByteOffset = Shader.PropertyToID("_PositionByteOffset");
            public static  int NormalByteOffset = Shader.PropertyToID("_NormalByteOffset");
            public static  int TangentByteOffset = Shader.PropertyToID("_TangentByteOffset");
        }
        #endregion
        
        #region Properties
        public virtual int VertexCount => Mesh.vertexCount;
        public virtual int IndexCount => IndexBuffer?.count ?? 0;
        public virtual int TriangleCount => IndexBuffer?.count / 3 ?? 0;
        public virtual Matrix4x4 ObjectToSdfLocalMatrix => Matrix4x4.identity;
        #endregion

        #region Fields
        protected readonly bool IndexFormat16Bit;
        protected readonly int Stride;
        protected readonly int ColorByteOffset;
        protected readonly int UVByteOffset;
        protected readonly int PositionByteOffset;
        protected readonly int NormalByteOffset;
        protected readonly int TangentByteOffset;
        protected readonly int Stream;
        
        protected Mesh Mesh;
        protected GraphicsBuffer VertexBuffer;
        protected GraphicsBuffer IndexBuffer;
        
        protected bool Initialized = false;
        protected bool Disposed = false;
        #endregion

        public MeshHandler(Func<Mesh> func, int stream = 0) : this(func(), stream) {}

        public MeshHandler(Mesh mesh, int stream = 0)
        {
            Stream = stream;
            Mesh = mesh;
            Mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            Mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            VertexBuffer = Mesh.GetVertexBuffer(Stream);
            IndexBuffer = Mesh.GetIndexBuffer();
            
            IndexFormat16Bit = Mesh.indexFormat == IndexFormat.UInt16;
            Stride = Mesh.GetVertexBufferStride(0);

            ColorByteOffset = Mesh.GetAttributeOffset(VertexAttribute.Color);
            UVByteOffset = Mesh.GetAttributeOffset(VertexAttribute.TexCoord0);
            PositionByteOffset = Mesh.GetAttributeOffset(VertexAttribute.Position);
            NormalByteOffset = Mesh.GetAttributeOffset(VertexAttribute.Normal);
            TangentByteOffset = Mesh.GetAttributeOffset(VertexAttribute.Tangent);
            Initialized = true;
        }
        
        public virtual void SetParams(CommandBuffer cmb)
        {
            if (!Initialized || Disposed) return;
            
            cmb.SetGlobalBuffer(PropertyID.VertexBuffer, VertexBuffer);
            cmb.SetGlobalBuffer(PropertyID.IndexBuffer, IndexBuffer);
            cmb.SetGlobalInt(PropertyID.IndexFormat16Bit, IndexFormat16Bit ? 1 : 0);
            cmb.SetGlobalInt(PropertyID.Stride, Stride);
            cmb.SetGlobalInt(PropertyID.ColorByteOffset, ColorByteOffset);
            cmb.SetGlobalInt(PropertyID.UvByteOffset, UVByteOffset);
            cmb.SetGlobalInt(PropertyID.PositionByteOffset, PositionByteOffset);
            cmb.SetGlobalInt(PropertyID.NormalByteOffset, NormalByteOffset);
            cmb.SetGlobalInt(PropertyID.TangentByteOffset, TangentByteOffset);
        }
        
        protected virtual void ReleaseBuffer()
        {
            VertexBuffer?.Release();
            VertexBuffer = null;
            IndexBuffer?.Release();
            IndexBuffer = null;
        }

        #region IDisposable
        ~MeshHandler()
        {
            Dispose();
        }
        
        public virtual void Dispose()
        {
            ReleaseBuffer();

            Initialized = false;
            Disposed = true;
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
