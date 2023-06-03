using System;
using System.Diagnostics;
using Silk.NET.OpenGL;

namespace RA2Render
{
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        private uint _handle;
        private GL _gl;

        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            _gl.CheckError();
            Bind();
            vbo.Bind();
            ebo.Bind();
        }

        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            _gl.VertexAttribPointer(index, count, type, /*normalized*/false, vertexSize * (uint)sizeof(TVertexType), (void*)(offSet * sizeof(TVertexType)));
            _gl.CheckError();
            _gl.EnableVertexAttribArray(index);
            _gl.CheckError();
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
            _gl.CheckError();
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
            _gl.CheckError();
        }
    }
}

