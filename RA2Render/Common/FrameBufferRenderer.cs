using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Silk.NET.OpenGL;

namespace RA2Render.Common
{
    public class FrameBufferRenderer : IDisposable, IRenderable
    {
        public FrameBufferRenderer(GL gl, IRenderable target)
        {
            _gl = gl;
            _target = target;

            InitBuffer();
            InitShader();
            InitTexture();
            InitFrameBuffer();
        }

        private unsafe void InitBuffer()
        {
            // Create the VAO.
            _vao = _gl.GenVertexArray();
            _gl.BindVertexArray(_vao);

            // The quad vertices data.
            // You may have noticed an addition - texture coordinates!
            // Texture coordinates are a value between 0-1 (see more later about this) which tell the GPU which part
            // of the texture to use for each vertex.
            float scale = 1.0f;
            float[] vertices =
            {
              // aPosition--------   aTexCoords
                 scale,  scale, 0.0f,  1.0f, 1.0f,
                 scale, -scale, 0.0f,  1.0f, 0.0f,
                -scale, -scale, 0.0f,  0.0f, 0.0f,
                -scale,  scale, 0.0f,  0.0f, 1.0f
            };

            // Create the VBO.
            _vbo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);

            // Upload the vertices data to the VBO.
            fixed (float* buf = vertices)
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), buf, BufferUsageARB.DynamicDraw);

            // The quad indices data.
            uint[] indices =
            {
                0u, 1u, 3u,
                1u, 2u, 3u
            };

            // Create the EBO.
            _ebo = _gl.GenBuffer();
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

            // Upload the indices data to the EBO.
            fixed (uint* buf = indices)
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), buf, BufferUsageARB.DynamicDraw);
        }

        private void InitShader()
        {
            // The vertex shader code.
            const string vertexCode = @"
            #version 330 core
            
            layout (location = 0) in vec3 aPosition;

            // On top of our aPosition attribute, we now create an aTexCoords attribute for our texture coordinates.
            layout (location = 1) in vec2 aTexCoords;

            // Likewise, we also assign an out attribute to go into the fragment shader.
            out vec2 frag_texCoords;
            
            void main()
            {
                gl_Position = vec4(aPosition, 1.0);

                // This basic vertex shader does no additional processing of texture coordinates, so we can pass them
                // straight to the fragment shader.
                frag_texCoords = aTexCoords;
            }";

            // The fragment shader code.
            const string fragmentCode = @"
            #version 330 core

            // This in attribute corresponds to the out attribute we defined in the vertex shader.
            in vec2 frag_texCoords;
            
            out vec4 out_color;

            // Now we define a uniform value!
            // A uniform in OpenGL is a value that can be changed outside of the shader by modifying its value.
            // A sampler2D contains both a texture and information on how to sample it.
            // Sampling a texture is basically calculating the color of a pixel on a texture at any given point.
            uniform sampler2D uTexture;
            
            void main()
            {
                // We use GLSL's texture function to sample from the texture at the given input texture coordinates.
                out_color = texture(uTexture, frag_texCoords);
            }";

            // Create our vertex shader, and give it our vertex shader source code.
            uint vertexShader = _gl.CreateShader(ShaderType.VertexShader);
            _gl.ShaderSource(vertexShader, vertexCode);

            // Attempt to compile the shader.
            _gl.CompileShader(vertexShader);

            // Check to make sure that the shader has successfully compiled.
            _gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vStatus);
            if (vStatus != (int)GLEnum.True)
                throw new Exception("Vertex shader failed to compile: " + _gl.GetShaderInfoLog(vertexShader));

            // Repeat this process for the fragment shader.
            uint fragmentShader = _gl.CreateShader(ShaderType.FragmentShader);
            _gl.ShaderSource(fragmentShader, fragmentCode);

            _gl.CompileShader(fragmentShader);

            _gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fStatus);
            if (fStatus != (int)GLEnum.True)
                throw new Exception("Fragment shader failed to compile: " + _gl.GetShaderInfoLog(fragmentShader));

            // Create our shader program, and attach the vertex & fragment shaders.
            _program = _gl.CreateProgram();

            _gl.AttachShader(_program, vertexShader);
            _gl.AttachShader(_program, fragmentShader);

            // Attempt to "link" the program together.
            _gl.LinkProgram(_program);

            // Similar to shader compilation, check to make sure that the shader program has linked properly.
            _gl.GetProgram(_program, ProgramPropertyARB.LinkStatus, out int lStatus);
            if (lStatus != (int)GLEnum.True)
                throw new Exception("Program failed to link: " + _gl.GetProgramInfoLog(_program));

            // Detach and delete our shaders. Once a program is linked, we no longer need the individual shader objects.
            _gl.DetachShader(_program, vertexShader);
            _gl.DetachShader(_program, fragmentShader);
            _gl.DeleteShader(vertexShader);
            _gl.DeleteShader(fragmentShader);
        }


        private void InitFrameBuffer()
        {
            _fbo = _gl.GenFramebuffer();
            _gl.CheckError();
            _gl.BindFramebuffer(GLEnum.Framebuffer, _fbo);
            _gl.CheckError();
            _gl.FramebufferTexture2D(GLEnum.Framebuffer, GLEnum.ColorAttachment0, GLEnum.Texture2D, _texColorBuffer, 0);
            _gl.CheckError();
            _gl.FramebufferRenderbuffer(GLEnum.Framebuffer, GLEnum.DepthStencilAttachment, GLEnum.Renderbuffer, _rboDepthStencil);
            _gl.CheckError();

            if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
            {
                throw new Exception("framebuffer init failed");
            }
            _gl.CheckError();
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            _gl.CheckError();
        }

        private unsafe void InitTexture()
        {
            _texColorBuffer = _gl.GenTexture();
            _gl.CheckError();
            _gl.BindTexture(TextureTarget.Texture2D, _texColorBuffer);
            _gl.CheckError();
            _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgb, 800, 600, 0, PixelFormat.Rgb, PixelType.UnsignedByte, null);
            _gl.CheckError();
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            _gl.CheckError();
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            _gl.CheckError();
            _gl.BindTexture(TextureTarget.Texture2D, 0);
            _gl.CheckError();

            _rboDepthStencil = _gl.GenRenderbuffer();
            _gl.CheckError();
            _gl.BindRenderbuffer(GLEnum.Renderbuffer, _rboDepthStencil);
            _gl.CheckError();
            _gl.RenderbufferStorage(GLEnum.Renderbuffer, GLEnum.Depth24Stencil8, 800, 600);
            _gl.CheckError();
        }

        private void Bind()
        {
            _gl.BindFramebuffer(GLEnum.Framebuffer, _fbo);
            _gl.CheckError();
            _gl.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            _gl.CheckError();
            _gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
            _gl.CheckError();
            _gl.Enable(EnableCap.DepthTest);
            _gl.CheckError();
        }

        private void Unbind()
        {
            _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
            _gl.CheckError();
        }

        public unsafe void Render()
        {
            Bind();
            _target.Render();
            Unbind();

            _gl.BindVertexArray(_vao);
            _gl.CheckError();
            _gl.UseProgram(_program);
            _gl.CheckError();
            _gl.BindTexture(TextureTarget.Texture2D, _texColorBuffer);
            _gl.CheckError();
            _gl.DrawElements(PrimitiveType.Triangles, /*count*/6, DrawElementsType.UnsignedInt, null);
            _gl.CheckError();
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.CheckError();
            _gl.DeleteVertexArray(_vbo);
            _gl.CheckError();
            _gl.DeleteVertexArray(_ebo);
            _gl.CheckError();
            _gl.DeleteProgram(_program);
            _gl.CheckError();
            _gl.DeleteFramebuffer(_fbo);
            _gl.CheckError();
            _gl.DeleteTexture(_texColorBuffer);
            _gl.CheckError();
            _gl.DeleteRenderbuffer(_rboDepthStencil);
            _gl.CheckError();
        }

        private readonly GL _gl;
        private IRenderable _target;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private uint _program;
        private uint _fbo;
        private uint _texColorBuffer;
        private uint _rboDepthStencil;
    }
}
