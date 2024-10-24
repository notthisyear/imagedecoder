using ImageDecoder.Common;
using System;
using System.Text;
using static ImageDecoder.Common.Utilities;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal enum ChunkType
    {
        [ChunkType]
        Unknown,
        
        [ChunkType("IHDR")]
        IHDR,

        [ChunkType("sBIT")]
        sBIT,

        [ChunkType("IDAT")]
        IDAT,

        [ChunkType("IEND")]
        IEND,

    }

    internal record ChunkAttributes(uint ChunkId, bool IsCritical, bool IsPublic, bool IsSafeToCopy);

    
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    internal class ChunkTypeAttribute : Attribute
    {
        #region Public properties
        public string TypeName { get;}

        public uint ChunkId  { get; }
        #endregion

        #region Private fields
        private const int ChuckTypeNameLength = 4;
        #endregion
        
        public ChunkTypeAttribute()
        {
            TypeName = string.Empty;
        }

        public ChunkTypeAttribute(string typeName)
        {
            var chunkBytes = new ReadOnlySpan<byte>(Encoding.ASCII.GetBytes(typeName));
            if (!IsValidChunkName(chunkBytes))
                throw new PngDecodingException($"Chunk typename must be {ChuckTypeNameLength} ASCII letters");
            
            TypeName = typeName;
            ChunkId = chunkBytes.ReadUInt32(ByteOrder.LittleEndian);
        }
        
        public static bool IsValidChunkName(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != ChuckTypeNameLength)
                return false;

            foreach (var b in bytes)
            {
                if (!Ascii.IsValid(b))
                    return false;
            }
            return true;
        }

        public static ChunkAttributes GetChunkAttributes(ReadOnlySpan<byte> bytes)
            => new(bytes.ReadUInt32(ByteOrder.LittleEndian), !IsLower(bytes[0]), !IsLower(bytes[1]), IsLower(bytes[3]));
        
        private static bool IsLower(byte b)
            => (b & 0x20) == 0x20;     
    }
}