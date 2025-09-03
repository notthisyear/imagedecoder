using ImageDecoder.Common;
using System.IO;
using static ImageDecoder.Common.Utilities;

namespace ImageDecoder.Bmp
{
    internal readonly ref struct BitmapFileHeader(uint size, uint dataOffset)
    {
        public const uint Size = 14U;

        private readonly uint _size = size;
        private readonly uint _dataOffset = dataOffset;

        public void Write(FileStream fs)
        {
            fs.Write([0x42, 0x4d]);
            fs.Write(_size.EncodeUInt32(ByteOrder.LittleEndian));
            fs.Write(((uint)0x00).EncodeUInt32(ByteOrder.LittleEndian));
            fs.Write(_dataOffset.EncodeUInt32(ByteOrder.LittleEndian));
        }
    }

    internal readonly ref struct BitmapInfoHeader(int width,
                                                  int height,
                                                  ushort bitsPerPixel)
    {
        public const uint Size = 40U;

        #region Private fields
        private readonly int _width = width;
        private readonly int _height = height;
        private readonly ushort _bitsPerPixel = bitsPerPixel;
        #endregion

        public void Write(FileStream fs)
        {

            // Size of header
            fs.Write(((uint)40).EncodeUInt32(ByteOrder.LittleEndian));

            // Width
            fs.Write(_width.EncodeInt32(ByteOrder.LittleEndian));

            // Height
            fs.Write(_height.EncodeInt32(ByteOrder.LittleEndian));

            // Number of color planes (must be 1)
            fs.Write(((ushort)1).EncodeUInt16(ByteOrder.LittleEndian));

            // Bits per pixel (1, 4, 8 or 24)
            fs.Write(_bitsPerPixel.EncodeUInt16(ByteOrder.LittleEndian));

            // Compression method
            fs.Write(((uint)0).EncodeUInt32(ByteOrder.LittleEndian));

            // Image size 
            fs.Write(((uint)0).EncodeUInt32(ByteOrder.LittleEndian));

            // Pixels per meter (x)
            fs.Write(((int)0).EncodeInt32(ByteOrder.LittleEndian));

            // Pixels per meter (y)
            fs.Write(((int)0).EncodeInt32(ByteOrder.LittleEndian));

            // Number of used colors
            fs.Write(((uint)0).EncodeUInt32(ByteOrder.LittleEndian));

            // Number of important colors
            fs.Write(((uint)0).EncodeUInt32(ByteOrder.LittleEndian));
        }
    }
}
