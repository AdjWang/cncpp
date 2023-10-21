using System.Diagnostics;
using Silk.NET.OpenGL;
using Silk.NET.Maths;

using RA2Lib;
using RA2Lib.AbstractHierarchy;
using RA2Lib.FileFormats.Text;
using RA2Render.Common;
using Rectangle = RA2Lib.XnaUtils.Rectangle;
using Color = RA2Lib.XnaUtils.Color;

namespace RA2Render.Texture
{
    public class TileMap: IDisposable, IRenderable
    {
        public TileMap(GL gl, string map, int w, int h)
        {
            _gl = gl;

            Map = new MapClass(map);

            (Rectangle previewSize, Color[] previewData) = Map.MapFile.GetPreviewTextureData();
            // TODO: add back
            // _previewTexture = new(_gl, (uint)previewSize.Width, (uint)previewSize.Height, previewData);

            Map.Initialize();

            Tactical = TacticalClass.Create(w, h);
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

            Map.GetTexture(ref _mapTexture);
            Debug.Assert(_mapTexture != null);
            _texture = new(_gl, _mapTexture);
        }

        public void Render()
        {
            if (_mapMoved)
            {
                _mapMoved = false;
                UpdateTexture();
            }
            _texture.Render();
            // _previewTexture.Render();
        }

        public void Dispose()
        {
            _texture.Dispose();
        }

        public void Move(Vector2D<int> amount)
        {
            int amountLeftRight = amount.X;
            int amountUpDown = amount.Y;

            if (amountLeftRight != 0)
            {
                TacticalClass.NudgeStatus moveStatus = Tactical.NudgeX(amountLeftRight);
                _mapMoved |= moveStatus != TacticalClass.NudgeStatus.E_EDGE;
            }
            if (amountUpDown != 0)
            {
                TacticalClass.NudgeStatus moveStatus = Tactical.NudgeY(amountUpDown);
                _mapMoved |= moveStatus != TacticalClass.NudgeStatus.E_EDGE;
            }
        }

        public void Resize(Vector2D<int> size)
        {
            // TODO: resize is essential to draw correctly in different window size
            // texture would be messed up if not handling the window size
            throw new NotImplementedException();
        }

        private readonly GL _gl;
        private Texture2D _texture;
        // private Texture2D _previewTexture;
        private MapClass Map;
        private TacticalClass Tactical;
        private bool _mapMoved = false;

        private RA2Lib.Helpers.ZBufferedTexture? _mapTexture = null;

        private void UpdateTexture()
        {
            Map.GetTexture(ref _mapTexture);
            Debug.Assert(_mapTexture != null);
            _texture.UpdateTexture(0, 0, (uint)_mapTexture.Width, (uint)_mapTexture.Height, _mapTexture.GetPixelData());
        }
    }
}
