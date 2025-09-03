using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImageDecoder.Bmp;
using ImageDecoder.Common;
using ImageDecoder.PngDecoding.Chunks;
using static ImageDecoder.Common.Scanline;
using static ImageDecoder.PngDecoding.Chunks.IHDRChunk;

namespace ImageDecoder.PngDecoding
{
    internal sealed class PngFile
    {
        public string FileName { get; init; } = string.Empty;

        public int Width { get; private set; }

        public int Height { get; private set; }

        private readonly List<(Chunk chunk, long dataOffset)> _pngChunks = [];

        private Scanline[]? _image;

        private bool _hasHeader = false;

        public void DumpImageDataAsBmp(string filename)
        {
            if (_image == default)
                throw new InvalidOperationException("Cannot dump PNG image data - no image");

            var dataChunk = _pngChunks.First(x => x.chunk.ChunkType == ChunkType.IDAT).chunk as IDATChunk;
            if (dataChunk == default)
                throw new InvalidOperationException("Cannot dump PNG image data - no data chunks");

            var header = TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR);
            if (header == default)
                throw new InvalidOperationException("Cannot dump PNG iamge data - no header");

            var bitsPerChannel = header.BitDepth;
            if (bitsPerChannel != 8)
                throw new NotImplementedException("Only one byte byte per channel is currently supported");

            if (header.Color != ColorType.Truecolor && header.Color != ColorType.TruecolorWithAlpha)
                throw new NotImplementedException($"Cannot yet dump data for other PNG types than '{ColorType.Truecolor}' and '{ColorType.TruecolorWithAlpha}'");

            var r = _image.SelectMany(x => x.Pixels).ToArray();
            BmpFile.Write(filename, new((int)header.Width, -((int)header.Height), new ReadOnlySpan<Pixel>(r)));
        }

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
                chunk!.DecodeChunk(reader);
            }
            else
            {
                throw new PngDecodingException("Multiple IDAT segments not yet supported");
            }

            var header = TryGetChunkOfType<IHDRChunk>(ChunkType.IHDR)
                ?? throw new PngDecodingException("Could not get IHDR chunk");

            _image = new Scanline[header.Height];
            for (var i = 0; i < header.Height; i++)
                _image[i] = new(i, chunk, header, i > 0 ? _image[i - 1] : default);
        }

        public void DumpScanlineFilters()
        {
            if (_image == default)
                return;

            Console.WriteLine("Scanline filters:");
            foreach (var type in Enum.GetValues<FilterType>())
            {
                var count = _image.Where(x => x.ScanlineFilter == type).Count();
                if (count > 0)
                    Console.WriteLine($"\t{type}: {count}");
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
            => [.. _pngChunks.Where(x => x.chunk.ChunkType == type).Select(x => (x.chunk as T, x.dataOffset))];
    }
}
