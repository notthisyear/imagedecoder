using System.IO;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal sealed class IENDChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        : Chunk(length, chunkType, attributes, file, reader)
    {
        public override string ToString()
            => $"IEND chunk";
    }
}
