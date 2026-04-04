using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace MorphSDF
{
    public static class MeshExtension
    {
        public static int GetAttributeOffset(this Mesh mesh, VertexAttribute attribute, int stream = 0)
        {
            var attributes = mesh.GetVertexAttributes();
            int offset = 0;
            foreach (var attr in attributes)
            {
                if (attr.stream != stream) continue;
                if (attr.attribute == attribute) return offset;

                offset += GetFormatSize(attr.format) * attr.dimension;
            }
            return 0;
        }

        private static int GetFormatSize(VertexAttributeFormat format)
        {
            return format switch
            {
                VertexAttributeFormat.Float32 => 4,
                VertexAttributeFormat.Float16 => 2,
                VertexAttributeFormat.UNorm8 => 1,
                VertexAttributeFormat.SNorm8 => 1,
                VertexAttributeFormat.UNorm16 => 2,
                VertexAttributeFormat.SNorm16 => 2,
                VertexAttributeFormat.UInt8 => 1,
                VertexAttributeFormat.SInt8 => 1,
                VertexAttributeFormat.UInt16 => 2,
                VertexAttributeFormat.SInt16 => 2,
                VertexAttributeFormat.UInt32 => 4,
                VertexAttributeFormat.SInt32 => 4,
                _ => throw new InvalidOperationException()
            };
        }
    }
}