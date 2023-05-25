﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;

namespace RA2Lib.FileFormats.Binary {
    public class PAL : BinaryFileFormat {
        public static Dictionary<String, PAL> LoadedPalettes = new Dictionary<String, PAL>();

        private static PAL _GrayscalePalette;

        public static PAL GrayscalePalette {
            get {
                if (_GrayscalePalette == null) {
                    _GrayscalePalette = new PAL();
                    for (var i = 0; i < 256; ++i) {
                        _GrayscalePalette.Colors[i] = new Color(i, i, i, 255);
                    }
                }
                return _GrayscalePalette;
            }
        }

        private static Color _TranslucentColor;
        public static Color TranslucentColor {
            get {
                if (_TranslucentColor == null) {
                    _TranslucentColor = new Color(255, 255, 255, 255);
                }
                return _TranslucentColor;
            }
        }

        public Color[] Colors = new Color[256];

        //public static PAL Load(String filename) {
        //    if (!LoadedPalettes.ContainsKey(filename)) {
        //        var Palette = new PAL(filename);
        //        LoadedPalettes[filename] = Palette;
        //    }
        //    return LoadedPalettes[filename];
        //}

        public PAL(CCFileClass ccFile = null) : base(ccFile) {
        }

        private byte decompress_6_to_8(int v18) {
            return (byte)((v18 & 63) * 255 / 63);
        }

        protected override bool ReadFile(BinaryReader r) {
            if (r.BaseStream.Length != 768) {
                return false;
            }
            for (var i = 0; i < 256; ++i) {
                byte R = r.ReadByte();
                byte G = r.ReadByte();
                byte B = r.ReadByte();

                Colors[i] = new Color(decompress_6_to_8(R), decompress_6_to_8(G), decompress_6_to_8(B), (byte)255);
            }

            return true;
        }
    }
}
