using ImageDecoder.Common;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using static ImageDecoder.Common.Iso3309Crc32;
using static ImageDecoder.Common.Utilities;

namespace ImageDecoder.PngDecoding.Chunks
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Name is from PNG spec")]
    internal sealed class sBITChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        : Chunk(length, chunkType, attributes, file, reader)
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

            var expectedLength = GetExpectedLengthForColorType(headerChunk.Color);
            switch (headerChunk.Color)
            {
                case IHDRChunk.ColorType.Greyscale:
                    AssertDataLength(expectedLength);
                    SignificantGreyscaleBits = data[0];
                    break;

                case IHDRChunk.ColorType.Truecolor:
                case IHDRChunk.ColorType.IndexedColor:
                    AssertDataLength(expectedLength);
                    SignificantRedBits = data[0];
                    SignificantGreenBits = data[1];
                    SignificantBlueBits = data[2];
                    break;

                case IHDRChunk.ColorType.GreyscaleWithAlpha:
                    AssertDataLength(expectedLength);
                    SignificantGreyscaleBits = data[0];
                    SignificantAlphaBits = data[1];
                    break;

                case IHDRChunk.ColorType.TruecolorWithAlpha:
                    AssertDataLength(expectedLength);
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

        protected override uint EncodeChunkTypeAndData(FileStream fs)
        {
            var headerChunk = File.TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR)!;
            var dataLength = GetExpectedLengthForColorType(headerChunk.Color);
            
            Span<byte> chunkTypeAndData = new(new byte[4 + dataLength]);
            attributes.ChunkId.AsSpan(ByteOrder.LittleEndian).CopyTo(chunkTypeAndData);

            var byteIdx = 4;
            switch (headerChunk.Color)
            {
                case IHDRChunk.ColorType.Greyscale:
                    chunkTypeAndData[byteIdx] = SignificantRedBits;
                    break;

                case IHDRChunk.ColorType.Truecolor:
                case IHDRChunk.ColorType.IndexedColor:
                    chunkTypeAndData[byteIdx++] = SignificantRedBits;
                    chunkTypeAndData[byteIdx++] = SignificantGreenBits;
                    chunkTypeAndData[byteIdx] = SignificantBlueBits;
                    break;

                case IHDRChunk.ColorType.GreyscaleWithAlpha:

                    chunkTypeAndData[byteIdx++] = SignificantGreyscaleBits;
                    chunkTypeAndData[byteIdx] = SignificantAlphaBits;
                    break;

                case IHDRChunk.ColorType.TruecolorWithAlpha:

                    chunkTypeAndData[byteIdx++] = SignificantRedBits;
                    chunkTypeAndData[byteIdx++] = SignificantGreenBits;
                    chunkTypeAndData[byteIdx++] = SignificantBlueBits;
                    chunkTypeAndData[byteIdx] = SignificantAlphaBits;
                    break;

                default:
                    throw new NotSupportedException($"Color type '{headerChunk.Color}' is unknown");
            }

            fs.Write(chunkTypeAndData);
            return CalculateCrc(chunkTypeAndData);
        }

        private void AssertDataLength(int actualLength)
        {
            if (Length != actualLength)
                throw new PngDecodingException($"Unexpected data length in sBIT chunk - expected {actualLength}, got {Length}"); 
        }

        private static int GetExpectedLengthForColorType(IHDRChunk.ColorType type)
            => type switch
            {
                IHDRChunk.ColorType.Greyscale => 1,
                IHDRChunk.ColorType.Truecolor or IHDRChunk.ColorType.IndexedColor => 3,
                IHDRChunk.ColorType.GreyscaleWithAlpha => 2,
                IHDRChunk.ColorType.TruecolorWithAlpha => 4,
                _ => throw new NotImplementedException(),
            };

        private static string ValueOrX(byte b)
            => b == 0 ? "x" : b.ToString(); 
    }
}
