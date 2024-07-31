using System.Diagnostics;
using System.IO;
using Serilog;
using Silk.NET.OpenGL;

using RA2Render.Common;
using Color = RA2Lib.XnaUtils.Color;

namespace RA2Render
{
    public class SHPTexture : IDisposable, IRenderable
    {
        public SHPTexture(GL gl, string shp, string pal)
        {
            _gl = gl;

            var p = RA2Lib.FileSystem.LoadFile(pal);
            var animPal = new RA2Lib.FileFormats.Binary.PAL(p);

            // var textures = RA2Lib.FileFormats.Binary.SHP.LoadFile(shp);
            // Debug.Assert(textures != null);
            // textures.ApplyPalette(animPal);
            // //    if (MousePalette == null) {
            // //        MousePalette = PAL.GrayscalePalette;
            // //    }
            // //    MouseTextures.ApplyPalette(MousePalette);

            // for (uint frame = 0; frame < textures.FrameCount; frame++)
            // {
            //     RA2Lib.Helpers.ZBufferedTexture texture = null!;
            //     textures.GetTexture(frame, ref texture, yflip: true);
            //     Debug.Assert(texture != null);
            //     _textures.Add(new Texture2D(_gl, texture));
            // }

            // RA2Lib.Helpers.ZBufferedTexture texture = null!;
            // int frameCount = textures.GetFullTexture(ref texture, yflip: true);
            // Debug.Assert(texture != null);
            // Log.Debug($"{Path.GetFileName(shp)} got {frameCount} frames");
            // _texture = new Texture2D(_gl, texture);


            bool found = false;
            MemoryStream fileContent = new();
            foreach (var M in RA2Lib.FileFormats.Binary.MIX.LoadedMIXes)
            {
                if (M.ContainsFile(shp))
                {
                    found = true;
                    fileContent = M.GetFileContents(shp);
                    break;
                }
            }
            Debug.Assert(found);

            OpenRA.Mods.Common.SpriteLoaders.ShpTSLoader loader = new();
            bool ok = loader.TryParseSprite(fileContent, "", out var frames);
            Debug.Assert(ok);
            for (int i = 0; i < frames.Length; i++)
            {
                var frame = frames[i];
                if (frame.Size.Width * frame.Size.Height == 0)
                {
                    continue;
                }
                Debug.Assert(frame.Size.Width * frame.Size.Height == frame.Data.Length);
                List<Color> data = new();
                List<byte> pixelData = new();
                foreach (var ix in frame.Data)
                {
                    Color c;
                    if (ix == 0)
                    {
                        c = RA2Lib.FileFormats.Binary.PAL.TranslucentColor;
                    }
                    else
                    {
                        c = animPal.Colors[ix];
                    }
                    data.Add(c);
                    pixelData.Add(c.R);
                    pixelData.Add(c.G);
                    pixelData.Add(c.B);
                    pixelData.Add(c.A);
                }
                _textures.Add(new Texture2D(_gl, (uint)frame.Size.Width, (uint)frame.Size.Height, data.ToArray()));
                //DEBUG
                int width = frame.Size.Width;
                int height = frame.Size.Height;
                DumpBmp(width, height, pixelData.ToArray());
                break;
            }
        }

        private unsafe void DumpBmp(int width, int height, byte[] data)
        {
            var snapShotBmp = new Bitmap(width, height);
            System.Drawing.Imaging.BitmapData bmpData = snapShotBmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                                                      System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                                      System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            System.Runtime.InteropServices.Marshal.Copy(data, 0, bmpData.Scan0, width * height * 4);
            snapShotBmp.UnlockBits(bmpData);
            snapShotBmp.Save(@"C:\Users\c5\Desktop\RA2Res\temp.bmp");
        }

        private readonly GL _gl;
        private List<Texture2D> _textures = new();
        // private Texture2D _currentTexture;

        private int _frame = 0;

        public void SetFrame(int frame)
        {
            _frame = frame;
        }

        public void Render()
        {
            _textures[_frame].Render();
        }

        public void Dispose()
        {
            foreach (var texture in _textures)
            {
                texture.Dispose();
            }
        }
    }
}
