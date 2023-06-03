// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Silk.NET.OpenGL;
using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace RA2Render
{
    public class VoxelModel : IDisposable
    {
        public VoxelModel(GL gl, string[] vxls, string[] hvas)
        {
            _gl = gl;

            Debug.Assert(vxls.Count() == hvas.Count());
            for (int i = 0; i < vxls.Count(); i++)
            {
                LoadModel(vxls[i], hvas[i]);
            }
        }

        private readonly GL _gl;

        public List<VoxelMesh> Meshes { get; protected set; } = new List<VoxelMesh>();

        private unsafe void LoadModel(string vxlPath, string hvaPath)
        {
            var vxl = RA2Lib.FileFormats.Binary.VoxLib.Create(vxlPath, hvaPath);
            // TODO: more frames?
            Meshes.Add(ProcessMesh(vxl));
        }

        private unsafe VoxelMesh ProcessMesh(RA2Lib.FileFormats.Binary.VoxLib voxel)
        {
            // data to fill
            var vertices = new List<RA2Lib.FileFormats.Binary.VXL.VertexPositionColorNormal>();
            var indices = new List<uint>();

            voxel.Voxel.GetVertices(/*FrameIdx*/0, /*out*/vertices, /*out*/indices);

            // return a mesh object created from the extracted mesh data
            var result = new VoxelMesh(_gl, BuildVertices(vertices), BuildIndices(indices));
            return result;
        }

        private float[] BuildVertices(List<RA2Lib.FileFormats.Binary.VXL.VertexPositionColorNormal> vertexCollection)
        {
            var vertices = new List<float>();

            foreach (var vertex in vertexCollection)
            {
                vertices.Add(vertex.Position.X);
                vertices.Add(vertex.Position.Y);
                vertices.Add(vertex.Position.Z);
                vertices.Add(vertex.Color.R);
                vertices.Add(vertex.Color.G);
                vertices.Add(vertex.Color.B);
                vertices.Add(vertex.Color.A);
                vertices.Add(vertex.Normal.X);
                vertices.Add(vertex.Normal.Y);
                vertices.Add(vertex.Normal.Z);
            }

            return vertices.ToArray();
        }

        private uint[] BuildIndices(List<uint> indices)
        {
            return indices.ToArray();
        }

        public void Dispose()
        {
            foreach (var mesh in Meshes)
            {
                mesh.Dispose();
            }
        }
    }
}
