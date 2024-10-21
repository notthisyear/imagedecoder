using System;
using System.IO;
using System.Text;
using ImageDecoder.Common;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal class Chunk
    {
        #region Public properties
        public uint Length { get; }
        
        public ChunkType ChunkType { get; }

        public ChunkAttributes Attributes { get; }
        
        public bool IsValid { get; }        
        #endregion

        public const int CrcNumberOfBytes = 4;
        
        protected PngFile File { get; }

        public Chunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        {
            var data = new ReadOnlySpan<byte>(length == 0 ? [] : reader.ReadBytes((int)length));
            IsValid = CheckCrc(reader.ReadBytes(CrcNumberOfBytes));
            if (!IsValid)
                throw new PngDecodingException("CRC check failed - chunk is corrupt");
            
            Length = length;
            ChunkType = chunkType;
            Attributes = attributes;
            File = file;

            if (ChunkType != ChunkType.IDAT)
                DecodeChunk(data);
        }

        public static Chunk GetChunk(BinaryReader reader, PngFile file, bool warnOnUnknownChunk = false)
        {
            var length = new ReadOnlySpan<byte>(reader.ReadBytes(4)).ReadUInt32();
            var chunkTypeBytes = reader.ReadBytes(4);
            
            var (validChunkName, knownChunk) = PngDecoder.TryGetPngChunkType(chunkTypeBytes, out var chunkType);
            if (!validChunkName)
                throw new PngDecodingException($"Encountered invalid chunk type name '{Encoding.ASCII.GetString(chunkTypeBytes)}'");
            
            if (!knownChunk && warnOnUnknownChunk)
                Console.WriteLine($"Encountered unknown chunk type name '{Encoding.ASCII.GetString(chunkTypeBytes)}'");

            var attributes = ChunkTypeAttribute.GetChunkAttributes(chunkTypeBytes);
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
            return $"{ChunkType} chunk ({Length} bytes, CRC {(IsValid ? "OK" : "Not OK")}) [Critical: {Attributes.IsCritical}, IsPublic: {Attributes.IsPublic}, SafeToCopy: {Attributes.IsSafeToCopy}]";
        }

        public virtual void DecodeChunk(ReadOnlySpan<byte> data) { return; }

        public void EncodeChunk(FileStream fs)
        {
            // Write header
            Length.WriteUInt32(fs);
            var (attr, _) = ChunkType.GetCustomAttributeFromEnum<ChunkTypeAttribute>();
            attr!.ChunkId.WriteUInt32(fs);

            // Write content
            EncodeChunkData(fs);

            // Write CRC
            ((uint)0).WriteUInt32(fs);
        }

        protected virtual void EncodeChunkData(FileStream fs) { return; }
        
        protected static T GetValueFromByte<T>(byte b) where T : Enum
        {
            if (!Utilities.TryGetAsEnum(b, out T? t))
                throw new PngDecodingException($"Unexpected {typeof(T).Name} value '{b}'");
            return t!;
        }

        private bool CheckCrc(ReadOnlySpan<byte> crc)
        {
            // TODO: Figure out
            return true;
        }
    }
}