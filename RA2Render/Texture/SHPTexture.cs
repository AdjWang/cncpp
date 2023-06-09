using System.Diagnostics;
using Silk.NET.OpenGL;

namespace RA2Render
{
    public class SHPTexture : IDisposable
    {
        public SHPTexture(GL gl, string shp)
        {
            _gl = gl;
            // LoadTexture(shp);
            var textures = RA2Lib.FileFormats.Binary.SHP.LoadFile(shp);
            Debug.Assert(textures != null);
            textures.ApplyPalette(RA2Lib.FileFormats.Binary.PAL.GrayscalePalette);
            //    if (MousePalette == null) {
            //        MousePalette = PAL.GrayscalePalette;
            //    }
            //    MouseTextures.ApplyPalette(MousePalette);

            _SHPTextures = new();
            for (uint frame = 0; frame < textures.FrameCount; frame++)
            {
                RA2Lib.Helpers.ZBufferedTexture texture = null!;
                textures.GetTexture(frame, ref texture, yflip: true);
                Debug.Assert(texture != null);
                _SHPTextures.Add(texture);
            }

            _texture = new Texture2D(_gl, _SHPTextures[0]);
        }

        private readonly GL _gl;
        private List<RA2Lib.Helpers.ZBufferedTexture> _SHPTextures;
        private Texture2D _texture;

        public void Draw()
        {
            _texture.Bind();
            _texture.Draw();
        }

        public void Dispose()
        {
            _texture.Dispose();
        }
    }
}
