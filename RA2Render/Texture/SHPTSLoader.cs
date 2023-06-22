#region Copyright & License Information
/*
 * Copyright (c) The OpenRA Developers and Contributors
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using Silk.NET.Maths;
using System.IO;

namespace OpenRA.Mods.Common.SpriteLoaders
{
	public static class StreamExts
	{
		public static byte[] ReadBytes(this Stream s, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
			var buffer = new byte[count];
			s.ReadBytes(buffer, 0, count);
			return buffer;
		}

		public static void ReadBytes(this Stream s, byte[] buffer, int offset, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), "Non-negative number required.");
			while (count > 0)
			{
				int bytesRead;
				if ((bytesRead = s.Read(buffer, offset, count)) == 0)
					throw new EndOfStreamException();
				offset += bytesRead;
				count -= bytesRead;
			}
		}

		public static byte ReadUInt8(this Stream s)
		{
			var b = s.ReadByte();
			if (b == -1)
				throw new EndOfStreamException();
			return (byte)b;
		}

		public static ushort ReadUInt16(this Stream s)
		{
			return (ushort)(s.ReadUInt8() | s.ReadUInt8() << 8);
		}

		public static short ReadInt16(this Stream s)
		{
			return (short)(s.ReadUInt8() | s.ReadUInt8() << 8);
		}

		public static uint ReadUInt32(this Stream s)
		{
			return (uint)(s.ReadUInt8() | s.ReadUInt8() << 8 | s.ReadUInt8() << 16 | s.ReadUInt8() << 24);
		}

		public static int ReadInt32(this Stream s)
		{
			return s.ReadUInt8() | s.ReadUInt8() << 8 | s.ReadUInt8() << 16 | s.ReadUInt8() << 24;
		}

		public static void Write(this Stream s, int value)
		{
			s.WriteArray(BitConverter.GetBytes(value));
		}

		public static void Write(this Stream s, float value)
		{
			s.WriteArray(BitConverter.GetBytes(value));
		}

		public static float ReadFloat(this Stream s)
		{
			return BitConverter.ToSingle(s.ReadBytes(4), 0);
		}

		public static double ReadDouble(this Stream s)
		{
			return BitConverter.ToDouble(s.ReadBytes(8), 0);
		}

		// Note: renamed from Write() to avoid being aliased by
		// System.IO.Stream.Write(System.ReadOnlySpan) (which is not implemented in Mono)
		public static void WriteArray(this Stream s, byte[] data)
		{
			s.Write(data, 0, data.Length);
		}

		public static IEnumerable<string> ReadAllLines(this Stream s)
		{
			string line;
			using (var sr = new StreamReader(s))
				while ((line = sr.ReadLine()) != null)
					yield return line;
		}
	}

	/// <summary>
	/// Describes the format of the pixel data in a ISpriteFrame.
	/// Note that the channel order is defined for little-endian bytes, so BGRA corresponds
	/// to a 32bit ARGB value, such as that returned by Color.ToArgb().
	/// </summary>
	public enum SpriteFrameType
	{
		// 8 bit index into an external palette
		Indexed8,

		// 32 bit color such as returned by Color.ToArgb() or the bmp file format
		// (remember that little-endian systems place the little bits in the first byte!)
		Bgra32,

		// Like BGRA, but without an alpha channel
		Bgr24,

		// 32 bit color in big-endian format, like png
		Rgba32,

		// Like RGBA, but without an alpha channel
		Rgb24
	}

	public interface ISpriteLoader
	{
		bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames);
	}

	public interface ISpriteFrame
	{
		SpriteFrameType Type { get; }

		/// <summary>
		/// Size of the frame's `Data`.
		/// </summary>
		Size Size { get; }

		/// <summary>
		/// Size of the entire frame including the frame's `Size`.
		/// Think of this like a picture frame.
		/// </summary>
		Size FrameSize { get; }

		Vector2D<float> Offset { get; }
		byte[] Data { get; }
		bool DisableExportPadding { get; }
	}

	public class FastByteReader
	{
		readonly byte[] src;
		int offset;

		public FastByteReader(byte[] src, int offset = 0)
		{
			this.src = src;
			this.offset = offset;
		}

		public bool Done() { return offset >= src.Length; }
		public byte ReadByte() { return src[offset++]; }
		public int ReadWord()
		{
			var x = ReadByte();
			return x | (ReadByte() << 8);
		}

		public void CopyTo(byte[] dest, int offset, int count)
		{
			Array.Copy(src, this.offset, dest, offset, count);
			this.offset += count;
		}

		public int Remaining() { return src.Length - offset; }
	}

	// Run length encoded sequences of zeros (aka Format2)
	public static class RLEZerosCompression
	{
		public static void DecodeInto(byte[] src, byte[] dest, int destIndex)
		{
			var r = new FastByteReader(src);

			while (!r.Done())
			{
				var cmd = r.ReadByte();
				if (cmd == 0)
				{
					var count = r.ReadByte();
					while (count-- > 0)
						dest[destIndex++] = 0;
				}
				else
					dest[destIndex++] = cmd;
			}
		}
	}

	public class ShpTSLoader : ISpriteLoader
	{
		sealed class ShpTSFrame : ISpriteFrame
		{
			public SpriteFrameType Type => SpriteFrameType.Indexed8;
			public Size Size { get; }
			public Size FrameSize { get; }
			public Vector2D<float> Offset { get; }
			public byte[] Data { get; set; }
			public bool DisableExportPadding => false;

			public readonly uint FileOffset;
			public readonly byte Format;

			public ShpTSFrame(Stream s, Size frameSize, bool yFlip = false)
			{
				var x = s.ReadUInt16();
				var y = s.ReadUInt16();
				var width = s.ReadUInt16();
				var height = s.ReadUInt16();

				// Pad the dimensions to an even number to avoid issues with half-integer offsets
				var dataWidth = width;
				var dataHeight = height;
				if (dataWidth % 2 == 1)
					dataWidth += 1;

				if (dataHeight % 2 == 1)
					dataHeight += 1;

				Offset = new Vector2D<float>(x + (dataWidth - frameSize.Width) / 2, y + (dataHeight - frameSize.Height) / 2);
				Size = new Size(dataWidth, dataHeight);
				FrameSize = frameSize;

				Format = s.ReadUInt8();
				s.Position += 11;
				FileOffset = s.ReadUInt32();

				if (FileOffset == 0)
					return;

				// Parse the frame data as we go (but remember to jump back to the header before returning!)
				var start = s.Position;
				s.Position = FileOffset;

				Data = new byte[dataWidth * dataHeight];

				if (Format == 3)
				{
					// Format 3 provides RLE-zero compressed scanlines
					for (var j = 0; j < height; j++)
					{
						var length = s.ReadUInt16() - 2;
						RLEZerosCompression.DecodeInto(s.ReadBytes(length), Data, dataWidth * j);
					}
				}
				else
				{
					// Format 2 provides uncompressed length-prefixed scanlines
					// Formats 1 and 0 provide an uncompressed full-width row
					var length = Format == 2 ? s.ReadUInt16() - 2 : width;
					for (var j = 0; j < height; j++)
					{
						s.ReadBytes(Data, dataWidth * j, length);
					}
				}

				if (yFlip)
				{
					var flipData = new byte[dataWidth * dataHeight];
					for (var j = 0; j < height; j++)
					{
						Array.Copy(Data, dataWidth * j, flipData, dataWidth * (height - 1 - j), dataWidth);
					}
					Data = flipData;
				}

				s.Position = start;
			}
		}

		static bool IsShpTS(Stream s)
		{
			var start = s.Position;

			// First word is zero
			if (s.ReadUInt16() != 0)
			{
				s.Position = start;
				return false;
			}

			// Sanity Check the image count
			s.Position += 4;
			var imageCount = s.ReadUInt16();
			if (s.Position + 24 * imageCount > s.Length)
			{
				s.Position = start;
				return false;
			}

			// Check the image size and compression type format flag
			// Some files define bogus frames, so loop until we find a valid one
			s.Position += 4;
			ushort w, h, f = 0;
			byte type;
			do
			{
				w = s.ReadUInt16();
				h = s.ReadUInt16();
				type = s.ReadUInt8();

				// Zero sized frames always define a non-zero type
				if ((w == 0 || h == 0) && type == 0)
					return false;

				s.Position += 19;
			}
			while (w == 0 && h == 0 && ++f < imageCount);

			s.Position = start;
			return f == imageCount || type < 4;
		}

		static ShpTSFrame[] ParseFrames(Stream s)
		{
			var start = s.Position;

			s.ReadUInt16();
			var width = s.ReadUInt16();
			var height = s.ReadUInt16();
			var size = new Size(width, height);
			var frameCount = s.ReadUInt16();

			var frames = new ShpTSFrame[frameCount];
			for (var i = 0; i < frames.Length; i++)
			{
				frames[i] = new ShpTSFrame(s, size, yFlip: true);
			}

			s.Position = start;
			return frames;
		}

		public bool TryParseSprite(Stream s, string filename, out ISpriteFrame[] frames)
		{
			if (!IsShpTS(s))
			{
				frames = null;
				return false;
			}

			frames = ParseFrames(s);
			return true;
		}
	}
}

