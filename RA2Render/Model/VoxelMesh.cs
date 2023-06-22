// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Silk.NET.OpenGL;
using RA2Render.Common;

namespace RA2Render.Model
{
    public class VoxelMesh : IDisposable, IRenderable
    {
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }

        public VoxelMesh(GL gl, float[] vertices, uint[] indices)
        {
            GL = gl;
            Vertices = vertices;
            Indices = indices;

            EBO = new BufferObject<uint>(GL, indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(GL, vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(GL, VBO, EBO);

            uint vertexLineSize = 10;
            uint position = 0;
            uint color = 1;
            uint normal = 2;
            VAO.VertexAttributePointer(position, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/0);
            VAO.VertexAttributePointer(color, /*count*/4, VertexAttribPointerType.Float, vertexLineSize, /*offset*/3);
            VAO.VertexAttributePointer(normal, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/7);
        }

        private float[] Vertices { get; set; }
        private uint[] Indices { get; set; }

        private void Bind()
        {
            VAO.Bind();
        }

        public unsafe void Render()
        {
            Bind();
            GL.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
        }

        public void Dispose()
        {
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }
    }
}
