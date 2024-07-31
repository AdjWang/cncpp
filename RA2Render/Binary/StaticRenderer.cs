using Serilog;
//using System.Drawing.Imaging;
using Silk.NET.OpenGL;

namespace RA2Render.Binary
{
    static class StaticRenderer
    {
        static void Main(string[] args)
        {
            // using (MainGame game = new MainGame())
            // {
            //     game.Run();
            // }

            // a simple demo as debug reference
            // var glDemo = new RA2Render.OpenGLDemo();
            // glDemo.Run();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Debug()
                .MinimumLevel.Debug()
                .CreateLogger();
            Log.Information("OpenGLRender initializing...");

            var renderer = new RA2Render.Renderer(dumpMode: true);
            renderer.Run();
        }
    }
}
