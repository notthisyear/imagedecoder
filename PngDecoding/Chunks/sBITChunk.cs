using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using ImageDecoder.Common;

namespace ImageDecoder.PngDecoding.Chunks
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name is from PNG spec")]
    internal sealed class sBITChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader) : Chunk(length, chunkType, attributes, file, reader)
    {
        #region Public properties
        public byte SignificantGreyscaleBits { get; private set; }

        public byte SignificantRedBits { get; private set; }

        public byte SignificantGreenBits { get; private set; }

        public byte SignificantBlueBits { get; private set; }

        public byte SignificantAlphaBits { get; private set; }
        #endregion
        
        public override void DecodeChunk(ReadOnlySpan<byte> data)
        {
            var headerChunk = File.TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR);
            if (headerChunk == default)
                throw new PngDecodingException("No IHDR chunk found before finding sBIT chunk");
            
            switch (headerChunk.Color)
            {
                case IHDRChunk.ColorType.Greyscale:
                    AssertDataLength(1);
                    SignificantGreyscaleBits = data[0];
                    break;

                case IHDRChunk.ColorType.Truecolor:
                case IHDRChunk.ColorType.IndexedColor:
                    AssertDataLength(3);
                    SignificantRedBits = data[0];
                    SignificantGreenBits = data[1];
                    SignificantBlueBits = data[2];
                    break;

                case IHDRChunk.ColorType.GreyscaleWithAlpha:
                    AssertDataLength(2);
                    SignificantGreyscaleBits = data[0];
                    SignificantAlphaBits = data[1];
                    break;

                case IHDRChunk.ColorType.TruecolorWithAlpha:
                    AssertDataLength(4);
                    SignificantRedBits = data[0];
                    SignificantGreenBits = data[1];
                    SignificantBlueBits = data[2];
                    SignificantAlphaBits = data[3];
                    break;

                default:
                    throw new NotSupportedException($"Color type '{headerChunk.Color}' is unknown");
            }
        }

        public override string ToString()
            => $"sBIT chunk {ValueOrX(SignificantGreyscaleBits)}" +
            $"{ValueOrX(SignificantRedBits)}" +
            $"{ValueOrX(SignificantGreenBits)}" +
            $"{ValueOrX(SignificantBlueBits)}" +
            $"{ValueOrX(SignificantAlphaBits)} (GrRGBA)";

        private void AssertDataLength(int actualLength)
        {
            if (Length != actualLength)
                throw new PngDecodingException($"Unexpected data length in sBIT chunk - expected {actualLength}, got {Length}"); 
        }

        private static string ValueOrX(byte b)
            => b == 0 ? "x" : b.ToString(); 
    }
}
