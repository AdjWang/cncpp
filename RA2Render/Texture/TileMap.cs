using System.Diagnostics;
using Silk.NET.OpenGL;

using RA2Lib;
using RA2Lib.AbstractHierarchy;
using RA2Lib.FileFormats.Text;
using Rectangle = RA2Lib.XnaUtils.Rectangle;
using Color = RA2Lib.XnaUtils.Color;
using Silk.NET.Maths;

namespace RA2Render.Texture
{
    public class TileMap: IDisposable
    {
        public TileMap(GL gl, string map)
        {
            _gl = gl;

            Map = new MapClass(map);

            (Rectangle previewSize, Color[] previewData) = Map.MapFile.GetPreviewTextureData();
            _previewTexture = new(_gl, (uint)previewSize.Width, (uint)previewSize.Height, previewData);

            Map.Initialize();

            // TODO: 800, 600
            Tactical = TacticalClass.Create(1920, 1080);
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

            UpdateTexture();
        }

        public void Draw()
        {
            _texture.Bind();
            _texture.Draw();
            // _previewTexture.Bind();
            // _previewTexture.Draw();
        }

        public void Dispose()
        {
            _texture.Dispose();
        }

        public void Move(Vector2D<int> amount)
        {
            int amountLeftRight = amount.X;
            int amountUpDown = amount.Y;
            bool mapMoved = false;

            if (amountLeftRight != 0)
            {
                TacticalClass.NudgeStatus moveStatus = Tactical.NudgeX(amountLeftRight);
                mapMoved |= moveStatus != TacticalClass.NudgeStatus.E_EDGE;
            }
            if (amountUpDown != 0)
            {
                TacticalClass.NudgeStatus moveStatus = Tactical.NudgeY(amountUpDown);
                mapMoved |= moveStatus != TacticalClass.NudgeStatus.E_EDGE;
            }

            if (mapMoved)
            {
                UpdateTexture();
            }
        }

        private readonly GL _gl;
        private Texture2D _texture;
        private Texture2D _previewTexture;
        private MapClass Map;
        private TacticalClass Tactical;

        private RA2Lib.Helpers.ZBufferedTexture? _mapTexture = null;

        private void UpdateTexture()
        {
            Map.GetTexture(ref _mapTexture);
            Debug.Assert(_mapTexture != null);
            if (_texture != null)
            {
                _texture.Dispose();
            }
            _texture = new(_gl, _mapTexture);
        }
    }
}
