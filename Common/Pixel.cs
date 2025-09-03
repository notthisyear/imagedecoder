using System;
using static ImageDecoder.PngDecoding.Chunks.IHDRChunk;

namespace ImageDecoder.Common
{
    internal sealed record Pixel
    {
        public byte Red { get; init; }
        public byte Green { get; init; }
        public byte Blue { get; init; }
        public byte Alpha { get; init; }

        public Pixel(ColorType colorType, ReadOnlySpan<byte> pixelData)
        {
            switch (colorType)
            {
                case ColorType.Truecolor:
                    Red = pixelData[0];
                    Green = pixelData[1];
                    Blue = pixelData[2];
                    break;
                case ColorType.TruecolorWithAlpha:
                    Red = pixelData[0];
                    Green = pixelData[1];
                    Blue = pixelData[2];
                    Alpha = pixelData[3];
                    break;
                case ColorType.Greyscale:
                    break;
                case ColorType.IndexedColor:
                    break;
                case ColorType.GreyscaleWithAlpha:
                    break;
            }
        }

        public byte GetComponent(int componentIndex)
            => componentIndex switch
            {
                0 => Red,
                1 => Green,
                2 => Blue,
                3 => Alpha,
                _ => throw new NotSupportedException("Pixel cannot have more than four components"),
            };
    }
}
