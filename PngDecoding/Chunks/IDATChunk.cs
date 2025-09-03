using ImageDecoder.Common;
using System;
using System.IO;
using System.IO.Compression;
using static ImageDecoder.Common.Utilities;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal sealed class IDATChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        : Chunk(length, chunkType, attributes, file, reader)
    {
        private byte[] _data = [];

        public override void DecodeChunk(BinaryReader reader)
        {
            ZLibUtilities.ValidateZLibHeader(reader);
            using var decompressedStream = new MemoryStream();
            using var decompressor =
                new DeflateStream(
                    new MemoryStream(reader.ReadBytes((int)Length)),
                    CompressionMode.Decompress);
            decompressor.CopyTo(decompressedStream);
            _data = decompressedStream.ToArray();
        }

        public ReadOnlySpan<byte> GetData()
            => _data;

        protected override uint EncodeChunkTypeAndData(FileStream fs)
        {
            byte[] compressedData;
            using var stream = new MemoryStream();
            {
                using (DeflateStream compressionStream = new(stream, CompressionLevel.Optimal))
                    compressionStream.Write(_data, 0, _data.Length);
                compressedData = stream.ToArray();
            }

            var zlibHeader = ZLibUtilities.GetZLibHeader(CompressionLevel.Optimal);
            ((uint)(compressedData.Length + ZLibUtilities.ZLibHeaderNumberOfBytes)).WriteUInt32(fs);
            fs.Write(Attributes.ChunkId.AsSpan(ByteOrder.LittleEndian));
            fs.Write(zlibHeader);
            fs.Write(compressedData);

            Span<byte> chunkTypeAndData = new(new byte[4 + ZLibUtilities.ZLibHeaderNumberOfBytes + compressedData.Length]);
            Attributes.ChunkId.AsSpan(ByteOrder.LittleEndian).CopyTo(chunkTypeAndData);
            zlibHeader.CopyTo(chunkTypeAndData[4..]);
            compressedData.CopyTo(chunkTypeAndData[(4 + ZLibUtilities.ZLibHeaderNumberOfBytes)..]);
            return Iso3309Crc32.CalculateCrc(chunkTypeAndData);
        }

        public override string ToString()
            => $"IDAT chunk ({Length} bytes)";
    }
}
