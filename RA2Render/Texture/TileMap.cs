using System.Diagnostics;
using Silk.NET.OpenGL;

using RA2Lib;
using RA2Lib.AbstractHierarchy;
using RA2Lib.FileFormats.Text;

namespace RA2Render.Texture
{
    public class TileMap: IDisposable
    {
        public TileMap(GL gl, string map)
        {
            _gl = gl;

            MapClass Map = new MapClass(map);

            //if (Map.Preview != null) {
            //    MapPreview = Map.GetPreviewTexture(GraphicsDevice);
            //} else {
            //    MapPreview = null;
            //}

            Map.Initialize();

            TacticalClass Tactical;
            // TODO: 800, 600
            Tactical = TacticalClass.Create(800, 600);
            Tactical.SetMap(Map);

            INI.Rules_Combined = new INI();
            INI.Rules_Combined.CombineWithFile(INI.Rules_INI);
            INI.Rules_Combined.CombineWithFile(Map.MapFile);

            TiberiumClass.LoadListFromINI(INI.Rules_Combined);

            OverlayTypeClass.LoadListFromINI(INI.Rules_Combined);

            IsoTileTypeClass.LoadListFromINI(Map.TheaterData, true);
            IsoTileTypeClass.PrepaintTiles();

            TiberiumClass.All.ReadAllFromINI(INI.Rules_Combined);

            CCFactory<OverlayTypeClass, OverlayClass>.Get().ReadAllFromINI(INI.Rules_Combined);

            Map.SetupOverlays();

            Map.GetTexture(_texture);
            Debug.Assert(_texture != null);
        }

        private readonly GL _gl;
        private Texture _texture;

        public void Draw()
        {
            _texture.Bind();
            _texture.Draw();
        }

        public void Dispose()
        {
            _texture.Dispose();
        }

        private void LoadMap(string mapfile)
        {
            // // String file = "D:\\practice\\RA2Resources\\2peaks.map";
            // String file = "D:\\practice\\RA2Resources\\deathll.yrm";
        }
    }
}
