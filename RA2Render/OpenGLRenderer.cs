using System.Numerics;
using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;

using RA2Render.Model;

namespace RA2Render
{
    class Renderer : IDisposable
    {
        public Renderer()
        {
            var options = WindowOptions.Default;
            options.API = new(ContextAPI.OpenGL, new(3, 3));
            options.PreferredDepthBufferBits = 8;
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
        private GL Gl = null!;

        private Shader Shader = null!;
        private Camera Camera = null!;
        private VoxelModel Model = null!;
        private VoxelMesh DemoPlaneMesh = null!;
        private SHPTexture DemoSHP = null!;

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

            //Start a camera at position 3 on the Z axis, looking at position -1 on the Z axis
            Vector3 cameraPosition = new(0.0f, 0.0f, -6.0f);
            Vector3 cameraFront = new(0.0f, 0.0f, 1.0f);
            Vector3 cameraUp = new(0.0f, 1.0f, 0.0f);
            Camera = new Camera(cameraPosition, cameraFront, cameraUp, Width / Height);

            // Load RA2 files
            var ra2mix = RA2Lib.FileSystem.LoadMIX("D:\\Practice\\RA2\\ra2.mix");
            Debug.Assert(ra2mix != null);
            var ra2mdmix = RA2Lib.FileSystem.LoadMIX("D:\\Practice\\RA2\\ra2md.mix");
            Debug.Assert(ra2mdmix != null);
            RA2Lib.FileSystem.LoadMIX("conqmd.mix");
            RA2Lib.FileSystem.LoadMIX("local.mix");
            RA2Lib.FileSystem.LoadMIX("localmd.mix");
            var rules = RA2Lib.FileSystem.LoadFile("RULESMD.INI");
            Debug.Assert(rules != null);
            RA2Lib.FileFormats.Text.INI.Rules_INI = new RA2Lib.FileFormats.Text.INI(rules);

            DemoPlaneMesh = new VoxelMesh(Gl, DemoPlane.Vertices, DemoPlane.Indices);
            // load vxl model
            // string name = "bfrt";
            // string name = "bpln";
            // string name = "disktur";
            string name = "shad";
            // string name = "htnk";
            Model = new VoxelModel(Gl, new string[] { $"{name}.vxl" }, new string[] { $"{name}.hva" });

            string vertShaderPath = "D:\\Practice\\cncpp\\RA2Render\\Model\\common_shader_vert.txt";
            string fragShaderPath = "D:\\Practice\\cncpp\\RA2Render\\Model\\common_shader_frag.txt";
            Shader = new Shader(Gl, vertShaderPath, fragShaderPath, inline: false);

            DemoSHP = new SHPTexture(Gl, "brute.shp");

        }

        private unsafe void OnRender(double obj) //Method needs to be unsafe due to draw elements.
        {
            Debug.Assert(Gl != null);
            Gl.Enable(EnableCap.DepthTest);
            Gl.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);
            //Clear the color channel.
            // Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            Shader.Use();
            SetShaderUniforms();
            var difference = (float)(window.Time);
            var transform = Matrix4x4.CreateRotationZ(difference) *
                            Matrix4x4.CreateRotationY(0.0f) *
                            Matrix4x4.CreateRotationX(-15.0f);
            Shader.SetUniform("transform", transform);

            foreach (var mesh in Model.Meshes)
            {
                mesh.Bind();
                mesh.Draw();
            }

            // {
            //     DemoPlaneMesh.Bind();
            //     DemoPlaneMesh.Draw();
            // }

            {
                DemoSHP.Draw();
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
