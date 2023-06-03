// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Collections.Generic;
using Silk.NET.OpenGL;

namespace RA2Render
{
    public class VoxelMesh : IDisposable
    {
        public VoxelMesh(GL gl, float[] vertices, uint[] indices)
        {
            GL = gl;
            Vertices = vertices;
            Indices = indices;
            SetupMesh();
        }

        public float[] Vertices { get; private set; }
        public uint[] Indices { get; private set; }
        public VertexArrayObject<float, uint> VAO { get; set; }
        public BufferObject<float> VBO { get; set; }
        public BufferObject<uint> EBO { get; set; }
        public GL GL { get; }

        public unsafe void SetupMesh()
        {
            EBO = new BufferObject<uint>(GL, Indices, BufferTargetARB.ElementArrayBuffer);
            VBO = new BufferObject<float>(GL, Vertices, BufferTargetARB.ArrayBuffer);
            VAO = new VertexArrayObject<float, uint>(GL, VBO, EBO);

            uint vertexLineSize = 10;
            uint position = 0;
            uint color = 1;
            uint normal = 2;
            VAO.VertexAttributePointer(position, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/0);
            VAO.VertexAttributePointer(color, /*count*/4, VertexAttribPointerType.Float, vertexLineSize, /*offset*/3);
            VAO.VertexAttributePointer(normal, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/7);
        }

        public void Bind()
        {
            VAO.Bind();
        }

        public void Dispose()
        {
            VAO.Dispose();
            VBO.Dispose();
            EBO.Dispose();
        }
    }
}
