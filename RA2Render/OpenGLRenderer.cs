using System.Numerics;
using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace RA2Render
{
    class Renderer : IDisposable
    {
        public Renderer()
        {
            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(Width, Height);
            options.Title = "RA2OpenGLRenderer";
            window = Window.Create(options);
            Debug.Assert(window != null);

            window.Load += OnLoad;
            window.Render += OnRender;
            window.Update += OnUpdate;
            window.Closing += OnClose;
        }

        public void Run()
        {
            window.Run();
        }

        public void Dispose()
        {
            window.Dispose();
        }


        private IWindow window;
        private GL? Gl;

        private Shader Shader;
        private Camera Camera;
        private VoxelModel Model;
        private VoxelMesh TempMesh;

        private const int Width = 800;
        private const int Height = 600;

        private void SetShaderUniforms()
        {
            // Creating Projection Matrix
            var model = Matrix4x4.CreateRotationY(0.0f) *
                        Matrix4x4.CreateRotationX(0.0f);
            var view = Camera.GetViewMatrix();
            var projection = Camera.GetProjectionMatrix();
            Shader.SetUniform("model", model);
            Shader.SetUniform("view", view);
            Shader.SetUniform("projection", projection);

            Shader.SetUniform("viewPos", Camera.Position);
            Shader.SetUniform("material.shininess", 32.0f);
            Shader.SetUniform("light.direction", new Vector3(-0.2f, -1.0f, -0.3f));

            Shader.SetUniform("light.color", new Vector3(1.0f, 1.0f, 1.0f));
            Shader.SetUniform("light.ambient", 1.0f);
            Shader.SetUniform("light.diffuse", 0.5f);
            Shader.SetUniform("light.specular", 1.0f);
        }

        //Vertex data, uploaded to the VBO.
        private readonly float[] Vertices =
        {
            // position, color, normal
             0.6f,  0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
             0.6f, -0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f, -0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f,  0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
        };
        //Index data, uploaded to the EBO.
        private readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };

        private unsafe void OnLoad()
        {
            IInputContext input = window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
            }

            //Getting the opengl api for drawing to the screen.
            Gl = GL.GetApi(window);
            Debug.Assert(Gl != null);

            TempMesh = new VoxelMesh(Gl, Vertices, Indices);
            // load vxl model
            // string name = "bfrt";
            // // string name = "bpln";
            // // string name = "disktur";
            // string vxlPath = $"D:\\practice\\RA2Resources\\{name}.vxl";
            // string hvaPath = $"D:\\practice\\RA2Resources\\{name}.hva";
            string vxlPath = "F:\\Practice\\RA2\\test\\bfrt.vxl";
            string hvaPath = "F:\\Practice\\RA2\\test\\bfrt.hva";
            Model = new VoxelModel(Gl, new string[] { vxlPath }, new string[] { hvaPath });

            //Start a camera at position 3 on the Z axis, looking at position -1 on the Z axis
            Camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, Width / Height);

            string vertShaderPath = "F:\\Practice\\cncpp\\RA2Render\\Model\\common_shader_vert.txt";
            string fragShaderPath = "F:\\Practice\\cncpp\\RA2Render\\Model\\common_shader_frag.txt";
            Shader = new Shader(Gl, vertShaderPath, fragShaderPath, inline: false);
        }

        private unsafe void OnRender(double obj) //Method needs to be unsafe due to draw elements.
        {
            Debug.Assert(Gl != null);
            // Gl.Enable(EnableCap.DepthTest);
            //Clear the color channel.
            Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            foreach (var mesh in Model.Meshes)
            {
                mesh.Bind();

                Shader.Use();
                SetShaderUniforms();
                var difference = (float)(window.Time);
                var transform = Matrix4x4.CreateRotationY(difference) *
                                Matrix4x4.CreateRotationX(difference);
                Shader.SetUniform("transform", transform);

                Gl.DrawElements(PrimitiveType.Triangles, (uint)mesh.Indices.Length, DrawElementsType.UnsignedInt, null);
            }

            {
                TempMesh.Bind();

                Shader.Use();
                SetShaderUniforms();
                var difference = (float)(window.Time);
                var transform = Matrix4x4.CreateRotationY(difference) *
                                Matrix4x4.CreateRotationX(difference);
                Shader.SetUniform("transform", transform);

                Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
            }
        }

        private void OnUpdate(double obj)
        { }

        private void OnClose()
        { }

        private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                window.Close();
            }
        }
    }
}
