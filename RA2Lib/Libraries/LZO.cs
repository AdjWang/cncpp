using System;
using System.IO.Compression;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using lzo.net;

namespace RA2Lib.Libraries {
    public class LZO {
        public static byte[] Slurp(byte[] packed) {
            using var unpacked = new MemoryStream();
            var offs = 0;
            while (offs < packed.Length)
            {
                int InputSize = BitConverter.ToInt16(packed, offs);
                int OutputSize = BitConverter.ToInt16(packed, offs + 2);
                offs += 4;
                if (offs + InputSize <= packed.Length)
                {
                    var Input = new byte[InputSize];
                    Buffer.BlockCopy(packed, offs, Input, 0, InputSize);

                    using var stream = new MemoryStream(Input);
                    using var decompressed = new LzoStream(stream, CompressionMode.Decompress);
                    decompressed.CopyTo(unpacked);
                }
                else
                {
                    Debug.WriteLine("LZO Chunking problem: offs {0} + inputsize {1} > packed Length {2}", offs, InputSize, packed.Length);
                }
                offs += InputSize;
            }
            return unpacked.ToArray();
        }
    }
}
