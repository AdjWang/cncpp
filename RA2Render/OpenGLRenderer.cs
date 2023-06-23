using System.IO;
using System.Numerics;
using System.Diagnostics;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;

using RA2Render.Model;
using RA2Render.Texture;
using RA2Render.Common;

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
            window.Resize += OnResize;
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
        private FrameBufferRenderer _renderWrapper;
        private VoxelModel Model = null!;
        private VoxelMesh DemoPlaneMesh = null!;
        private SHPTexture DemoSHP = null!;
        private TileMap Map = null!;
        private Vector2D<int> _mapMoveAmount;
        private int _mapMoveSpeed = 300;

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
            // string GameDir = "D:\\Games\\RA2\\RA2";
            // string ProjectDir = "D:\\temp\\RA2Render\\cncpp";
            // string ResourceDir = "D:\\temp\\RA2Render\\RA2Resources";
            string GameDir = "D:\\Practice\\RA2";
            string ProjectDir = "D:\\Practice\\cncpp";
            string ResourceDir = "D:\\Practice\\RA2Resources";

            IInputContext input = window.CreateInput();
            for (int i = 0; i < input.Keyboards.Count; i++)
            {
                input.Keyboards[i].KeyDown += KeyDown;
                input.Keyboards[i].KeyUp += KeyUp;
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
            var ra2mix = RA2Lib.FileSystem.LoadMIX(Path.Combine(GameDir, "ra2.mix"));
            Debug.Assert(ra2mix != null);
            var ra2mdmix = RA2Lib.FileSystem.LoadMIX(Path.Combine(GameDir, "ra2md.mix"));
            Debug.Assert(ra2mdmix != null);
            RA2Lib.FileSystem.LoadMIX("local.mix");
            RA2Lib.FileSystem.LoadMIX("cache.mix");
            RA2Lib.FileSystem.LoadMIX("conquer.mix");
            RA2Lib.FileSystem.LoadMIX("temperat.mix");
            RA2Lib.FileSystem.LoadMIX("localmd.mix");
            RA2Lib.FileSystem.LoadMIX("cachemd.mix");
            RA2Lib.FileSystem.LoadMIX("conqmd.mix");

            // RA2Lib.FileSystem.LoadMIX("CONQMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("GENERMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("GENERIC.MIX");
            // RA2Lib.FileSystem.LoadMIX("ISOGENMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("ISOGEN.MIX");
            // RA2Lib.FileSystem.LoadMIX("CONQUER.MIX");
            // RA2Lib.FileSystem.LoadMIX("CAMEOMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("CAMEO.MIX");
            // RA2Lib.FileSystem.LoadMIX("MAPSMD03.MIX");
            // RA2Lib.FileSystem.LoadMIX("MULTIMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("THEMEMD.MIX");
            // RA2Lib.FileSystem.LoadMIX("MOVMD03.MIX");

            var rules = RA2Lib.FileSystem.LoadFile("RULESMD.INI");
            Debug.Assert(rules != null);
            RA2Lib.FileFormats.Text.INI.Rules_INI = new RA2Lib.FileFormats.Text.INI(rules);

            var art = RA2Lib.FileSystem.LoadFile("ARTMD.INI");
            Debug.Assert(art != null);
            RA2Lib.FileFormats.Text.INI.Art_INI = new RA2Lib.FileFormats.Text.INI(art);

            DemoPlaneMesh = new VoxelMesh(Gl, DemoPlane.Vertices, DemoPlane.Indices);
            // load vxl model
             string name = "bfrt";
            // string name = "bpln";
            // string name = "disktur";
            // string name = "shad";
            // string name = "htnk";
            Model = new VoxelModel(Gl, new string[] { $"{name}.vxl" }, new string[] { $"{name}.hva" });
            _renderWrapper = new(Gl, Model);

            string vertShaderPath = Path.Combine(ProjectDir, "RA2Render\\Model\\common_shader_vert.txt");
            string fragShaderPath = Path.Combine(ProjectDir, "RA2Render\\Model\\common_shader_frag.txt");
            Shader = new Shader(Gl, vertShaderPath, fragShaderPath, inline: false);

            // DemoSHP = new SHPTexture(Gl, "brute.shp");

            // string mapfile = Path.Combine(ResourceDir, "2peaks.map");
            // Map = new TileMap(Gl, mapfile, 1920, 1080);
            // _mapMoveAmount = new(0, 0);
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

            {
                // Model.Render();
                _renderWrapper.Render();
            }

            // {
            //     DemoPlaneMesh.Render();
            // }

            // {
            //     DemoSHP.Render();
            // }

            // {
            //     Map.Render();
            // }
        }

        private void OnUpdate(double obj)
        {
            // Map.Move(_mapMoveAmount);
        }

        private void OnClose()
        { }

        private void OnResize(Vector2D<int> size)
        {
            Gl.Viewport(0, 0, (uint)size.X, (uint)size.Y);
        }

        private void KeyDown(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.Escape)
            {
                window.Close();
            }
            else if (arg2 == Key.W)
            {
                _mapMoveAmount.Y += _mapMoveSpeed;
            }
            else if (arg2 == Key.A)
            {
                _mapMoveAmount.X += -_mapMoveSpeed;
            }
            else if (arg2 == Key.S)
            {
                _mapMoveAmount.Y += -_mapMoveSpeed;
            }
            else if (arg2 == Key.D)
            {
                _mapMoveAmount.X += _mapMoveSpeed;
            }
        }

        private void KeyUp(IKeyboard arg1, Key arg2, int arg3)
        {
            if (arg2 == Key.W)
            {
                _mapMoveAmount.Y += -_mapMoveSpeed;
            }
            else if (arg2 == Key.A)
            {
                _mapMoveAmount.X += _mapMoveSpeed;
            }
            else if (arg2 == Key.S)
            {
                _mapMoveAmount.Y += _mapMoveSpeed;
            }
            else if (arg2 == Key.D)
            {
                _mapMoveAmount.X += -_mapMoveSpeed;
            }
        }
    }
}
