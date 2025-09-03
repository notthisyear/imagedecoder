using System;
using System.IO;
using ImageDecoder.Common;

namespace ImageDecoder.Bmp
{
    internal readonly ref struct BmpPixelArray
    {
        #region Public methods
        public int Width { get; }

        public int Height { get; }

        public ushort BitsPerPixel { get; }

        public uint Size => (uint)(_rowSize * _absoluteHeight);
        #endregion

        #region Private fields
        private readonly int _absoluteHeight;
        private readonly uint _rowSize;
        private readonly ReadOnlySpan<Pixel> _pixels;
        #endregion

        public BmpPixelArray(int width, int height, ReadOnlySpan<Pixel> pixels)
        {
            Width = width;
            Height = height;
            BitsPerPixel = 24;

            _absoluteHeight = Math.Abs(Height);
            _pixels = pixels;
            _rowSize = (uint)(Math.Ceiling(BitsPerPixel * Width / 32.0) * 4);
        }

        public void Write(FileStream fs)
        {
            for (var row = 0; row < _absoluteHeight; row++)
            {
                var rowPixelsStart = row * Width;
                var rowPixelsEnd = rowPixelsStart + Width;

                var byteIdx = 0;
                for (var pixelIdx = rowPixelsStart; pixelIdx < rowPixelsEnd; pixelIdx++)
                {
                    var pixel = _pixels[pixelIdx];
                    fs.WriteByte(pixel.Blue);
                    fs.WriteByte(pixel.Green);
                    fs.WriteByte(pixel.Red);
                    byteIdx += 3;
                }

                while (byteIdx < _rowSize)
                {
                    fs.WriteByte(0x00);
                    byteIdx++;
                }
            }
        }
    }
}
