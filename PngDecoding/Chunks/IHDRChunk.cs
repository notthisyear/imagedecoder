using ImageDecoder.Common;
using System;
using System.Collections.Generic;
using System.IO;
using static ImageDecoder.Common.Utilities;

namespace ImageDecoder.PngDecoding.Chunks
{
    internal sealed class IHDRChunk(uint length, ChunkType chunkType, ChunkAttributes attributes, PngFile file, BinaryReader reader)
        : Chunk(length, chunkType, attributes, file, reader)
    {
        #region Enums
        [AttributeUsage(AttributeTargets.Field)]
        private class ColorHeaderFieldAttribute : Attribute
        {
            private readonly List<byte> _allowedBitDepths;
            
            public ColorHeaderFieldAttribute(params byte[] allowedBitDepths)
            {
                _allowedBitDepths = [];
                for (var i = 0; i < allowedBitDepths.Length; i++)
                    _allowedBitDepths.Add(allowedBitDepths[i]);   
            }

            public bool BitDepthValid(byte depth)
                => _allowedBitDepths.Contains(depth); 
        }
        
        public enum ColorType : byte
        {
            [ColorHeaderField(1, 2, 4, 8, 16)]
            Greyscale = 0x00,
            
            [ColorHeaderField(8, 16)]
            Truecolor = 0x02,
            
            [ColorHeaderField(1, 2, 4, 8)]
            IndexedColor = 0x03,

            [ColorHeaderField(8, 16)]
            GreyscaleWithAlpha = 0x04,
            
            [ColorHeaderField(8, 16)]
            TruecolorWithAlpha = 0x06
        };

        public enum CompressionMethod : byte
        {
            Deflate = 0x00
        };

        public enum FilterMethod : byte
        {
            AdaptiveFiltering = 0x00
        };

        public enum InterlaceMethod : byte
        {
            NoInterlace = 0x00,
            Adam7 = 0x01
        };

        public uint Width { get; private set; }
        #endregion

        #region Public properties
        public uint Height { get; private set; }

        public byte BitDepth { get; private set; }
         
        public ColorType Color { get; private set; }

        public CompressionMethod Compression { get; private set; }

        public FilterMethod Filter { get; private set; }

        public InterlaceMethod Interlace { get; private set; }
        #endregion

        private const int ChunkNumberOfBytes = 13;

        public override void DecodeChunk(BinaryReader reader)
        {
            if (Length < ChunkNumberOfBytes)
                throw new PngDecodingException($"IHDR chunk has incorrect length - expected {ChunkNumberOfBytes} bytes, got {Length}");

            ReadOnlySpan<byte> data = reader.ReadBytes((int)Length);
            Width = data[0..4].ReadUInt32();
            Height = data[4..8].ReadUInt32();
            BitDepth = data[8];
           
            Color = GetValueFromByte<ColorType>(data[9]);
            Compression = GetValueFromByte<CompressionMethod>(data[10]);
            Filter = GetValueFromByte<FilterMethod>(data[11]);
            Interlace = GetValueFromByte<InterlaceMethod>(data[12]);

            var (attr, _) = Color.GetCustomAttributeFromEnum<ColorHeaderFieldAttribute>();
            if (!attr!.BitDepthValid(BitDepth))
                throw new PngDecodingException($"Invalid bit depth ({BitDepth}) specified for color type {Color})");
        }

        public override string ToString()
            => $"IHDR chunk ({Width} x {Height}, {BitDepth} bps/bppi, {Color}, {Compression}, {Filter}, {Interlace})";

        protected override uint EncodeChunkTypeAndData(FileStream fs)
        {
            Span<byte> chunkTypeAndData = new(new byte[4 + ChunkNumberOfBytes]);
            Attributes.ChunkId.AsSpan(ByteOrder.LittleEndian).CopyTo(chunkTypeAndData);

            var byteIdx = 4;
            Width.AsSpan().CopyTo(chunkTypeAndData[byteIdx..]);
            byteIdx += 4;

            Height.AsSpan().CopyTo(chunkTypeAndData[byteIdx..]);
            byteIdx += 4;

            chunkTypeAndData[byteIdx++] = BitDepth;
            chunkTypeAndData[byteIdx++] = (byte)Color;
            chunkTypeAndData[byteIdx++] = (byte)Compression;
            chunkTypeAndData[byteIdx++] = (byte)Filter;
            chunkTypeAndData[byteIdx] = (byte)Interlace;

            fs.Write(chunkTypeAndData);
            return Iso3309Crc32.CalculateCrc(chunkTypeAndData);
        }
    }
}
