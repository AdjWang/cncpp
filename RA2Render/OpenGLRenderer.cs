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

        private static BufferObject<float> Vbo;
        private static BufferObject<uint> Ebo;
        private static VertexArrayObject<float, uint> Vao;
        private Shader Shader;
        private Camera Camera;

        private const int Width = 800;
        private const int Height = 600;

        //Vertex shaders are run on each vertex.
        private readonly string VertexShaderSource = @"
        #version 330 core

        uniform mat4 view;
        uniform mat4 model;
        uniform mat4 projection;
        uniform mat4 transform; 

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec4 aColor;
        layout (location = 2) in vec3 aNormal;

        out vec3 Normal;
        out vec3 FragPos;
        out vec4 Color;

        void main(void)
        {
            Normal = aNormal;
            FragPos = vec3(view * model * vec4(aPosition, 1.0));       
            Color = aColor;	
            gl_Position = projection * view * model * transform * vec4(aPosition, 1.0);
        }
        ";

        //Fragment shaders are run on each fragment/pixel of the geometry.
        private readonly string FragmentShaderSource = @"
        #version 330 core

        struct Material {
            float shininess;
        }; 

        struct Light {
            //vec3 position;
            vec3 direction;
            vec3 color;
            vec3 ambient;
            vec3 diffuse;
            vec3 specular;
        };

        in vec3 Normal;
        in vec3 FragPos;
        in vec4 Color;

        uniform vec3 viewPos;
        uniform Material material;
        uniform Light light;

        out vec4 FragColor;

        void main()
        {
            // ambient
            vec3 ambient = light.ambient * light.color;
            
            // diffuse 
            vec3 norm = normalize(Normal);
            // vec3 lightDir = normalize(light.position - FragPos);
            vec3 lightDir = normalize(-light.direction);  
            float diff = max(dot(norm, lightDir), 0.0);
            vec3 diffuse = light.diffuse * diff * light.color;  
            
            // specular
            vec3 viewDir = normalize(viewPos - FragPos);
            vec3 reflectDir = reflect(-lightDir, norm);  
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), material.shininess);
            vec3 specular = light.specular * spec * light.color;  
                
            vec3 result = (ambient + diffuse + specular) * Color;
            FragColor = vec4(result, 1.0);
        }
        ";

        private void SetShaderUniforms()
        {
            // Creating Projection Matrix
            // var model = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(difference)) *
            //             Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(difference));
            var model = Matrix4x4.CreateRotationY(0.0f) *
                        Matrix4x4.CreateRotationX(0.0f);
            // var view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
            var view = Camera.GetViewMatrix();
            // var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), Width / Height, 0.1f, 100.0f);
            var projection = Camera.GetProjectionMatrix();

            Shader.SetUniform("model", model);
            Shader.SetUniform("view", view);
            Shader.SetUniform("projection", projection);

            Shader.SetUniform("viewPos", Camera.Position);
            Shader.SetUniform("material.shininess", 32.0f);
            Shader.SetUniform("light.direction", new Vector3(-0.2f, -1.0f, -0.3f));
            Shader.SetUniform("light.color", new Vector3(1.0f, 1.0f, 1.0f));
            Shader.SetUniform("light.ambient", new Vector3(0.2f, 0.2f, 0.2f));
            Shader.SetUniform("light.diffuse", new Vector3(0.5f, 0.5f, 0.5f));
            Shader.SetUniform("light.specular", new Vector3(1.0f, 1.0f, 1.0f));
        }

        //Vertex data, uploaded to the VBO.
        private readonly float[] Vertices =
        {
            // position, color, normal
             0.6f,  0.6f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
             0.6f, -0.6f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f, -0.6f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f,  0.6f, 0.0f,   1.0f, 0.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
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


            Ebo = new BufferObject<uint>(Gl, Indices, BufferTargetARB.ElementArrayBuffer);
            Vbo = new BufferObject<float>(Gl, Vertices, BufferTargetARB.ArrayBuffer);
            Vao = new VertexArrayObject<float, uint>(Gl, Vbo, Ebo);
            //Tell opengl how to give the data to the shaders.
            uint vertexLineSize = 10;
            uint position = 0;
            uint color = 1;
            uint normal = 2;
            Vao.VertexAttributePointer(position, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/0);
            Vao.VertexAttributePointer(color, /*count*/4, VertexAttribPointerType.Float, vertexLineSize, /*offset*/3);
            Vao.VertexAttributePointer(normal, /*count*/3, VertexAttribPointerType.Float, vertexLineSize, /*offset*/7);

            //Start a camera at position 3 on the Z axis, looking at position -1 on the Z axis
            Camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, Width / Height);

            Shader = new Shader(Gl, VertexShaderSource, FragmentShaderSource, inline: true);
        }

        private unsafe void OnRender(double obj) //Method needs to be unsafe due to draw elements.
        {
            Debug.Assert(Gl != null);
            Gl.Enable(EnableCap.DepthTest);
            //Clear the color channel.
            Gl.Clear((uint)ClearBufferMask.ColorBufferBit);

            //Bind the geometry and shader.
            Vao.Bind();

            Shader.Use();
            SetShaderUniforms();
            var difference = (float)(window.Time);
            var transform = Matrix4x4.CreateRotationY(difference) *
                            Matrix4x4.CreateRotationX(difference);
            Shader.SetUniform("transform", transform);

            //Draw the geometry.
            Gl.DrawElements(PrimitiveType.Triangles, (uint)Indices.Length, DrawElementsType.UnsignedInt, null);
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
