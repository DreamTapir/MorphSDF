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
            public static readonly int VertexBuffer = Shader.PropertyToID("_VertexBuffer");
            public static readonly int IndexBuffer = Shader.PropertyToID("_IndexBuffer");
            public static readonly int IndexFormat16Bit = Shader.PropertyToID("_IndexFormat16Bit");
            public static readonly int Stride = Shader.PropertyToID("_Stride");
            public static readonly int ColorByteOffset = Shader.PropertyToID("_ColorByteOffset");
            public static readonly int UvByteOffset = Shader.PropertyToID("_UvByteOffset");
            public static readonly int PositionByteOffset = Shader.PropertyToID("_PositionByteOffset");
            public static readonly int NormalByteOffset = Shader.PropertyToID("_NormalByteOffset");
            public static readonly int TangentByteOffset = Shader.PropertyToID("_TangentByteOffset");
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
        
        public virtual void SetParams(CommandBuffer commandBuffer)
        {
            if (!Initialized || Disposed) return;
            
            commandBuffer.SetGlobalBuffer(PropertyID.VertexBuffer, VertexBuffer);
            commandBuffer.SetGlobalBuffer(PropertyID.IndexBuffer, IndexBuffer);
            commandBuffer.SetGlobalInt(PropertyID.IndexFormat16Bit, IndexFormat16Bit ? 1 : 0);
            commandBuffer.SetGlobalInt(PropertyID.Stride, Stride);
            commandBuffer.SetGlobalInt(PropertyID.ColorByteOffset, ColorByteOffset);
            commandBuffer.SetGlobalInt(PropertyID.UvByteOffset, UVByteOffset);
            commandBuffer.SetGlobalInt(PropertyID.PositionByteOffset, PositionByteOffset);
            commandBuffer.SetGlobalInt(PropertyID.NormalByteOffset, NormalByteOffset);
            commandBuffer.SetGlobalInt(PropertyID.TangentByteOffset, TangentByteOffset);
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
