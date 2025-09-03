using System;
using System.Collections.Generic;
using System.Linq;
using ImageDecoder.PngDecoding.Chunks;
using static ImageDecoder.PngDecoding.Chunks.IHDRChunk;

namespace ImageDecoder.Common
{
    internal sealed record Scanline
    {
        public enum FilterType : byte
        {
            None = 0x00,
            Sub = 0x01,
            Up = 0x02,
            Average = 0x03,
            Paeth = 0x04,
        }

        public FilterType ScanlineFilter { get; }

        private const int FilterMethodNumberOfBytes = 1;
        private delegate byte Filter(byte raw, byte a, byte b, byte c);
        private readonly static Dictionary<FilterType, Filter> s_filterMap = new()
        {
            { FilterType.None, (raw, a, _, _) => raw },
            { FilterType.Sub, (raw, a, _, _) => (byte)((raw + a) % 256) },
            { FilterType.Up, (raw, _, b, _) => (byte)((raw + b) % 256) },
            { FilterType.Average, (raw, a, b, _) =>
            {
                var average = (((int)a + (int)b) / 2);
                return (byte)((raw + average) % 256);
            }},
            {
                FilterType.Paeth, (raw, a, b, c) =>
                {
                    var baseValue = (int)a + (int)b - (int)c;
                    var diffs = new int[] { a, b, c }.Select(x => Math.Abs(baseValue - x)).ToArray();

                    if (diffs[0] <= diffs[1] && diffs[0] <= diffs[2])
                        return (byte)((raw + a) % 256);
                    else if (diffs[1] <= diffs[2])
                        return (byte)((raw + b) % 256);
                    return (byte)((raw + c) % 256);
                }
            }
};

        public Pixel[] Pixels { get; }

        public Scanline(int index, IDATChunk data, IHDRChunk header, Scanline? previousScanline = default)
        {
            if (header.Filter != FilterMethod.AdaptiveFiltering)
                throw new NotSupportedException($"Cannot decode PNG file with filter method '{header.Filter}'");

            if (header.Color != ColorType.Truecolor && header.Color != ColorType.TruecolorWithAlpha)
                throw new NotImplementedException($"Only '{ColorType.Truecolor}' and '{ColorType.TruecolorWithAlpha}' are currently supported");

            var bitsPerPixel = header.BitDepth * header.ComponentsPerPixel;
            var bitsPerScanline = (8 * FilterMethodNumberOfBytes) + (bitsPerPixel * header.Width);
            var bytesPerPixel = bitsPerPixel >> 3;
            var bytesPerScanline = bitsPerScanline >> 3;

            var d = data.GetData();
            var filterByteIndex = index * (int)bytesPerScanline;
            var currentFilterByte = d[filterByteIndex];

            if (!Enum.IsDefined(typeof(FilterType), currentFilterByte))
                throw new NotSupportedException($"Scanline filter method {currentFilterByte} is not known");

            Pixels = new Pixel[header.Width];
            ScanlineFilter = (FilterType)currentFilterByte;

            var components = new byte[header.ComponentsPerPixel];
            var componentIdx = 0;
            var pixelIdx = 0;

            // Note: Skip over the filter byte
            for (var i = filterByteIndex + 1; i < filterByteIndex + bytesPerScanline; i++)
            {
                var (a, b, c) = GetFilterBytes(pixelIdx, componentIdx, Pixels, previousScanline);
                components[componentIdx++] = s_filterMap[ScanlineFilter](d[i], a, b, c);

                if (componentIdx == header.ComponentsPerPixel)
                {
                    Pixels[pixelIdx++] = new Pixel(header.Color, components);
                    componentIdx = 0;
                }
            }
        }

        private static (byte a, byte b, byte c) GetFilterBytes(int pixelIdx, int componentIndex, Pixel[] pixels, Scanline? previousScanline)
        {
            var a = (pixelIdx > 0) ? pixels[pixelIdx - 1].GetComponent(componentIndex) : (byte)0x00;
            var b = (previousScanline != default) ? previousScanline.Pixels[pixelIdx].GetComponent(componentIndex) : (byte)0x00;
            var c = (previousScanline != default && pixelIdx > 0) ? previousScanline.Pixels[pixelIdx - 1].GetComponent(componentIndex) : (byte)0x00;
            return (a, b, c);
        }
    }
}
