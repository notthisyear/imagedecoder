namespace ImageDecoder.PngDecoding.Chunks
{
    internal interface IChunk
    {
        public static ChunkType ChunkType { get; }
    }
}
