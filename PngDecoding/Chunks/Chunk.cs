using ImageDecoder.Common;
using System;
using System.IO;
using System.Text;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal class Chunk
    {
        #region Public properties
        public uint Length { get; }

        public ChunkType ChunkType { get; }

        public ChunkAttributes Attributes { get; }
        #endregion

        public const int CrcNumberOfBytes = 4;

        private const int LengthTypeNumberOfBytes = 4;
        private const int ChunkTypeNumberOfBytes = 4;

        protected PngFile File { get; }

        protected Chunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        {
            Length = length;
            ChunkType = chunkType;
            Attributes = attributes;
            File = file;

            if (ChunkType != ChunkType.IDAT)
            {
                var position = reader.BaseStream.Position;
                reader.BaseStream.Seek(position - Length - CrcNumberOfBytes, SeekOrigin.Begin);
                DecodeChunk(reader);
                _ = reader.BaseStream.Seek(CrcNumberOfBytes, SeekOrigin.Current);
            }
        }

        public static Chunk GetChunk(BinaryReader reader, PngFile file, bool warnOnUnknownChunk = false)
        {
            var length = new ReadOnlySpan<byte>(reader.ReadBytes(LengthTypeNumberOfBytes)).ReadUInt32();

            var chunkTypeStartPosition = reader.BaseStream.Position;
            var chunkTypeBytes = reader.ReadBytes(ChunkTypeNumberOfBytes);

            // Reset the reader back to where the chunk type starts, as we need that in the CRC calculation
            reader.BaseStream.Seek(chunkTypeStartPosition, SeekOrigin.Begin);

            var (validChunkName, knownChunk) = PngDecoder.TryGetPngChunkType(chunkTypeBytes, out var chunkType);
            var attributes = ChunkTypeAttribute.GetChunkAttributes(chunkTypeBytes);

            if (!validChunkName)
                throw new PngDecodingException($"Encountered invalid chunk type name '{Encoding.ASCII.GetString(chunkTypeBytes)}'");

            if (!knownChunk)
            {
                if (attributes.IsCritical)
                    throw new PngDecodingException($"Encountered unknown critical chunk '{Encoding.ASCII.GetString(chunkTypeBytes)}'");
                else if (warnOnUnknownChunk)
                    Console.WriteLine($"Encountered unknown ancillary chunk '{Encoding.ASCII.GetString(chunkTypeBytes)}'");
            }

            var bytesRemaining = reader.BaseStream.Length - reader.BaseStream.Position;
            if ((ChunkTypeNumberOfBytes + length + CrcNumberOfBytes) > bytesRemaining)
                throw new PngDecodingException($"File is too short - expected to read {length} bytes, only {bytesRemaining} bytes left");

            if (!Iso3309Crc32.VerifyCrc(reader, ChunkTypeNumberOfBytes + (int)length, CrcNumberOfBytes))
                throw new PngDecodingException("CRC check failed - chunk is corrupt");

            var chunk = chunkType switch
            {
                ChunkType.IHDR => new IHDRChunk(length, chunkType, attributes, file, reader),
                ChunkType.sBIT => new sBITChunk(length, chunkType, attributes, file, reader),
                ChunkType.IDAT => new IDATChunk(length, chunkType, attributes, file, reader),
                ChunkType.IEND => new IENDChunk(length, chunkType, attributes, file, reader),
                _ => new Chunk(length, chunkType, attributes, file, reader)
            };

            return chunk;
        }

        public override string ToString()
        {
            return $"{ChunkType} chunk ({Length} bytes) [Critical: {Attributes.IsCritical}, IsPublic: {Attributes.IsPublic}, SafeToCopy: {Attributes.IsSafeToCopy}]";
        }

        public virtual void DecodeChunk(BinaryReader reader) { _ = reader.ReadBytes((int)Length); }

        public void EncodeChunk(FileStream fs)
        {
            // Write header
            if (ChunkType != ChunkType.IDAT)
                Length.WriteUInt32(fs);

            // Write content
            var crc = EncodeChunkTypeAndData(fs);

            // Write CRC
            crc.WriteUInt32(fs);
        }

        protected virtual uint EncodeChunkTypeAndData(FileStream fs)
        {
            Attributes.ChunkId.WriteUInt32(fs, Utilities.ByteOrder.LittleEndian);
            return Iso3309Crc32.CalculateCrc(Attributes.ChunkId.AsSpan(Utilities.ByteOrder.LittleEndian));
        }

        protected static T GetValueFromByte<T>(byte b) where T : Enum
        {
            if (!Utilities.TryGetAsEnum(b, out T? t))
                throw new PngDecodingException($"Unexpected {typeof(T).Name} value '{b}'");
            return t!;
        }
    }
}
