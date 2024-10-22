using System;
using System.IO;
using System.IO.Compression;
using ImageDecoder.Common;
using static ImageDecoder.Common.Utilities;
using static ImageDecoder.Common.Iso3309Crc32;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal sealed class IDATChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        : Chunk(length, chunkType, attributes, file, reader)
    {
        private byte[] _data = [];

        public override void DecodeChunk(ReadOnlySpan<byte> data)
        {
            var headerChunk = File.TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR);
            if (headerChunk == default)
                throw new PngDecodingException("Could not get IHDR section");

            if (headerChunk.Compression == IHDRChunk.CompressionMethod.Deflate)
            {
                using var decompressedStream = new MemoryStream();
                // Note: The data is gzip compressed, so we skip the first
                //       two bytes to get the underlying DEFLATE stream
                using var decompressor =
                    new DeflateStream(
                        new MemoryStream(data[2..].ToArray()),
                        CompressionMode.Decompress);
                decompressor.CopyTo(decompressedStream);
                _data = decompressedStream.ToArray();
            }
        }

        protected override uint EncodeChunkTypeAndData(FileStream fs)
        {
            Span<byte> chunkTypeAndData = new(new byte[4 + _data.Length]);
            attributes.ChunkId.AsSpan(ByteOrder.LittleEndian).CopyTo(chunkTypeAndData);
            _data.CopyTo(chunkTypeAndData[4..]);
            fs.Write(chunkTypeAndData);
            return CalculateCrc(chunkTypeAndData);
        }

        public override string ToString()
            => $"IDAT chunk ({Length} bytes)";
    }
}
