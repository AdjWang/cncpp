using System;

static class Program
{
    static void Main(string[] args)
    {
        // using (MainGame game = new MainGame())
        // {
        //     game.Run();
        // }

        // a simple demo as debug reference
        var glDemo = new RA2Render.OpenGLDemo();
        glDemo.Run();
    }
}

