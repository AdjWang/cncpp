using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;

namespace System.Diagnostics
{
    public static class OpenGLExtensions
    {
        public static void CheckError(this GL gl)
        {
            GLEnum lastErr = gl.GetError();
            Debug.Assert(lastErr == GLEnum.NoError, $"OpenGL error: {lastErr}");
        }
    }
}
