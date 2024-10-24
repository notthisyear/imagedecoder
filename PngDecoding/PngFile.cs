using ImageDecoder.Common;
using ImageDecoder.PngDecoding.Chunks;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ImageDecoder.PngDecoding
{
    internal class PngFile
    {
        public string FileName { get; init; } = string.Empty;

        private readonly List<(Chunk chunk, long dataOffset)> _pngChunks = [];
        
        private bool _hasHeader = false;

        public void EncodeAndWrite(string filename)
        {
            if (_pngChunks.Count == 0)
                throw new PngEncodingException("Cannot encode PNG file - no chunks");

            if (!_hasHeader)
                throw new PngEncodingException("Cannot encode PNG file - no header");

            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);

            PngSignature.WritePngSignature(fs);
            var header = TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR)!;

            header.EncodeChunk(fs);
            foreach (var (chunk, _) in _pngChunks)
            {
                if (chunk.ChunkType == ChunkType.IHDR)
                    continue;

                chunk.EncodeChunk(fs);
            }            
        }

        public void DecodeData(BinaryReader reader)
        {
            var result = TryGetChunksOfTypeWithOffset<IDATChunk>(ChunkType.IDAT);
            if (result.Count == 0 || result.First().chunk == default)
                return;
            
            var (chunk, dataOffset) = result.First();
            reader.BaseStream.Seek(dataOffset, SeekOrigin.Begin);
            if (result.Count == 1)
            {
                chunk!.DecodeChunk(reader.ReadBytes((int)chunk.Length));
            }
            else
            {
                throw new PngDecodingException("Multiple IDAT segments not yet supported");
            }
        }

        public void AddChunk(Chunk chunk, long dataOffset)
        {
            if (chunk.ChunkType == ChunkType.IHDR)
            {
                if (_hasHeader)
                    throw new PngDecodingException("Got IHDR chunk again - is file corrupt?");
                _hasHeader = true;
            }
            _pngChunks.Add((chunk, dataOffset));
        }

        // TODO: This interface can be a little bit nicer
        public T? TryGetChunkOfType<T>(ChunkType type) where T : Chunk
            => _pngChunks.FirstOrDefault(x => x.chunk.ChunkType == type).chunk as T;
        
        public List<(T? chunk, long offset)> TryGetChunksOfTypeWithOffset<T>(ChunkType type) where T : Chunk
            => _pngChunks.Where(x => x.chunk.ChunkType == type).Select(x => (x.chunk as T, x.dataOffset)).ToList();
    }
}