namespace RA2Render.Model
{
    public static class DemoPlane
    {
        //Vertex data, uploaded to the VBO.
        public static readonly float[] Vertices =
        {
            // position, color, normal
             0.6f,  0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
             0.6f, -0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f,  0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
            -0.6f, -0.6f, 0.0f,   0.0f, 1.0f, 0.0f, 1.0f,   1.0f, 1.0f, 1.0f,
        };
        //Index data, uploaded to the EBO.
        public static readonly uint[] Indices =
        {
            0, 1, 3,
            1, 2, 3
        };
    }
}
