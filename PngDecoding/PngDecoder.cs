using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ImageDecoder.Common;
using ImageDecoder.PngDecoding.Chunks;

namespace ImageDecoder.PngDecoding
{
    // Based on the spec at https://www.w3.org/TR/png-3/
    internal static class PngDecoder
    {
        #region Private fields
        private readonly static Dictionary<uint, ChunkType> s_chunkTypeLookup = [];
        #endregion

        public static PngFile Decode(string path, bool verbose = false)
        {
            if (!File.Exists(path))
                throw new PngDecodingException($"Could not find file '{path}'");
            
            using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
            using BinaryReader reader = new(stream);

            if (!PngSignature.ValidatePngSignature(reader))
                throw new PngDecodingException("PNG signature invalid");

            var i = 0;
            PngFile file = new() { FileName = path };
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                var chunk = Chunk.GetChunk(reader, file, verbose);
                if (verbose)
                    Console.WriteLine($"Chunk {i++}: {chunk}");
                file.AddChunk(chunk, reader.BaseStream.Position - chunk.Length - Chunk.CrcNumberOfBytes);
            }

            reader.BaseStream.Seek(0, SeekOrigin.Begin);
            file.DecodeData(reader);
            return file;
        }

        public static (bool validChunkName, bool knownChunk) TryGetPngChunkType(ReadOnlySpan<byte> bytes, out ChunkType type)
        {
            if (s_chunkTypeLookup.Count == 0)
                PopulateChunkAttributeCache();

            var chunkId = MemoryMarshal.Read<uint>(bytes);
            if (s_chunkTypeLookup.TryGetValue(chunkId, out var t))
            {
                type = t;
                return (true, true);
            }

            type = ChunkType.Unknown;
            if (!ChunkTypeAttribute.IsValidChunkName(bytes))
                return (false, false);
            s_chunkTypeLookup.Add(chunkId, type);
            return (true, false);
        }

        private static void PopulateChunkAttributeCache()
        {
            foreach (ChunkType t in Enum.GetValues(typeof(ChunkType)))
            {
                if (t == ChunkType.Unknown)
                    continue;

                var (attr, e) = t.GetCustomAttributeFromEnum<ChunkTypeAttribute>();
                if (e != null)
                    throw e;

                s_chunkTypeLookup.Add(attr!.ChunkId, t);
            }
        }
    }
}