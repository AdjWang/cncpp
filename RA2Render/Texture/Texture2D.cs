// https://github.com/dotnet/Silk.NET/blob/main/examples/CSharp/OpenGL%20Tutorials/Tutorial%201.3%20-%20Textures/Program.cs
using System.Diagnostics;
using RA2Render.Common;
using Silk.NET.OpenGL;

namespace RA2Render
{
    public class Texture2D : IDisposable, IRenderable
    {
        public Texture2D(GL gl, uint Width, uint Height, byte[] Data)
        {
            _gl = gl;
            InitBuffer();
            _gl.CheckError();
            InitShader();
            _gl.CheckError();
            InitAttributes();
            _gl.CheckError();
            LoadTexture(Width, Height, Data);
            _gl.CheckError();
        }

        public Texture2D(GL gl, uint Width, uint Height, RA2Lib.XnaUtils.Color[] pixels)
        {
            _gl = gl;
            InitBuffer();
            _gl.CheckError();
            InitShader();
            _gl.CheckError();
            InitAttributes();
            _gl.CheckError();
            LoadTexture(Width, Height, RA2Lib.Helpers.ZBufferedTexture.GetPixelData(pixels));
            _gl.CheckError();
        }
        public Texture2D(GL gl, RA2Lib.Helpers.ZBufferedTexture texture)
        {
            _gl = gl;
            InitBuffer();
            _gl.CheckError();
            InitShader();
            _gl.CheckError();
            InitAttributes();
            _gl.CheckError();
            LoadTexture(texture);
            _gl.CheckError();
        }

        private readonly GL _gl;
        private uint _vao;
        private uint _vbo;
        private uint _ebo;
        private uint _texture;
        private uint _program;

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

        private unsafe void InitAttributes()
        {
            // Set up our vertex attributes! These tell the vertex array (VAO) how to process the vertex data we defined
            // earlier. Each vertex array contains attributes. 

            // Our stride constant. The stride must be in bytes, so we take the first attribute (a vec3), multiply it
            // by the size in bytes of a float, and then take our second attribute (a vec2), and do the same.
            const uint stride = (3 * sizeof(float)) + (2 * sizeof(float));

            // Enable the "aPosition" attribute in our vertex array, providing its size and stride too.
            const uint positionLoc = 0;
            _gl.EnableVertexAttribArray(positionLoc);
            _gl.VertexAttribPointer(positionLoc, 3, VertexAttribPointerType.Float, false, stride, (void*)0);

            // Now we need to enable our texture coordinates! We've defined that as location 1 so that's what we'll use
            // here. The code is very similar to above, but you must make sure you set its offset to the **size in bytes**
            // of the attribute before.
            const uint textureLoc = 1;
            _gl.EnableVertexAttribArray(textureLoc);
            _gl.VertexAttribPointer(textureLoc, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

            // Unbind everything as we don't need it.
            _gl.BindVertexArray(0);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, 0);
        }

        private unsafe void LoadTexture(RA2Lib.Helpers.ZBufferedTexture texture)
        {
            LoadTexture((uint)texture.Width, (uint)texture.Height, texture.GetPixelData());
        }

        // private static bool done = false;
        private unsafe void LoadTexture(uint Width, uint Height, byte[] Data)
        {
            _texture = _gl.GenTexture();

            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, _texture);

            fixed (byte* ptr = Data)
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)Width,
                    (uint)Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            _gl.GenerateMipmap(TextureTarget.Texture2D);

            _gl.BindTexture(TextureTarget.Texture2D, 0);

            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        public unsafe void UpdateTexture(int x, int y, uint Width, uint Height, byte[] Data)
        {
            _gl.BindTexture(TextureTarget.Texture2D, _texture);
            fixed(byte* ptr = Data)
            {
                _gl.TexSubImage2D(TextureTarget.Texture2D, 0, x, y, Width, Height, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            }
            _gl.BindTexture(TextureTarget.Texture2D, 0);
        }

        private void Bind()
        {
            // Bind our VAO, then the program.
            _gl.BindVertexArray(_vao);
            _gl.CheckError();
            _gl.UseProgram(_program);
            _gl.CheckError();

            // Much like our texture creation earlier, we must first set our active texture unit, and then bind the
            // texture to use it during draw!
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.CheckError();
            _gl.BindTexture(TextureTarget.Texture2D, _texture);
            _gl.CheckError();
        }

        public unsafe void Render()
        {
            Bind();
            _gl.DrawElements(PrimitiveType.Triangles, /*count*/6, DrawElementsType.UnsignedInt, null);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_vao);
            _gl.CheckError();
            _gl.DeleteVertexArray(_vbo);
            _gl.CheckError();
            _gl.DeleteVertexArray(_ebo);
            _gl.CheckError();
            _gl.DeleteTexture(_texture);
            _gl.CheckError();
            _gl.DeleteProgram(_program);
            _gl.CheckError();
        }
    }
}

