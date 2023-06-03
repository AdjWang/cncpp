using System;
using System.Diagnostics;
using Silk.NET.OpenGL;

namespace RA2Render
{
    public class BufferObject<TDataType> : IDisposable
        where TDataType : unmanaged
    {
        private uint _handle;
        private BufferTargetARB _bufferType;
        private GL _gl;

        public unsafe BufferObject(GL gl, Span<TDataType> data, BufferTargetARB bufferType)
        {
            _gl = gl;
            _bufferType = bufferType;

            _handle = _gl.GenBuffer();
            _gl.CheckError();
            Bind();
            fixed (void* d = data)
            {
                _gl.BufferData(bufferType, (nuint) (data.Length * sizeof(TDataType)), d, BufferUsageARB.StaticDraw);
                _gl.CheckError();
            }
        }

        public void Bind()
        {
            _gl.BindBuffer(_bufferType, _handle);
            _gl.CheckError();
        }

        public void Dispose()
        {
            _gl.DeleteBuffer(_handle);
            _gl.CheckError();
        }
    }
}

