using System;
using System.IO;
using System.IO.Compression;

namespace ImageDecoder.Common
{
    internal static class ZLibUtilities
    {
        public const int ZLibHeaderNumberOfBytes = 2;

        private const byte ExpectedZLibCompressionMethod = 0x08; // Indicates DEFLATE algorithm
        private const byte ZlibCompressionInformation = 0x07; // Denotes 32K windows size for LZ77

        // See https://www.rfc-editor.org/rfc/rfc1950 for zlib header specification
        public static void ValidateZLibHeader(BinaryReader reader)
        {
            var bytesLeftInStream = reader.BaseStream.Length - reader.BaseStream.Position;
            if (bytesLeftInStream < ZLibHeaderNumberOfBytes)
                throw new InvalidOperationException($"Expected {ZLibHeaderNumberOfBytes} bytes for zlib header, only {bytesLeftInStream} bytes left");
            ValidateZLibHeader(reader.ReadBytes(ZLibHeaderNumberOfBytes));
        }

        public static void ValidateZLibHeader(ReadOnlySpan<byte> header)
        {
            if (header.Length < ZLibHeaderNumberOfBytes)
                throw new InvalidOperationException($"Expected {ZLibHeaderNumberOfBytes} bytes for zlib header, got {header.Length}");

            byte cmf = header[0];
            byte flg = header[1];

            // Bits 0 to 3 - Compression method
            var compressionMethod = (byte)(cmf & 0x0f);
            if (compressionMethod != ExpectedZLibCompressionMethod)
                throw new NotSupportedException($"Compression method '{compressionMethod}' not supported");

            // Bits 4 to 7 - Compression info (denotes the log2 (minus eight) of the LZ77 window size)
            if (((cmf >> 4) & 0x0f) > 0x07)
                throw new NotSupportedException("LZ77 window sizes larger than 32K are not allowed in the specification");

            // Bits 0 to 4 - FCHECK (must be such that the below check works out)
            var checkBits = (ushort)((cmf << 8) | flg);
            if (checkBits % 31 != 0)
                throw new InvalidOperationException("The check bits should be a multiple of 31. Corrupt data?");

            // Bit 5 - FDICT (whether or not a dictionary was used)
            if (((flg >> 5) & 0x01) == 0x01)
                throw new NotSupportedException("Decompressing with dictionary is not supported");
        }

        public static byte[] GetZLibHeader(CompressionLevel compressionLevel)
        {
            var header = new byte[ZLibHeaderNumberOfBytes];
            byte flgByte = compressionLevel switch
            {
                CompressionLevel.Optimal => 0x9c,
                CompressionLevel.Fastest => 0x5e,
                CompressionLevel.NoCompression => 0x01,
                CompressionLevel.SmallestSize => 0xda,
                _ => throw new NotImplementedException(),
            };
            header[0] = (ZlibCompressionInformation << 4) + ExpectedZLibCompressionMethod;
            header[1] = flgByte;
            return header;
        }
        
    }
}
