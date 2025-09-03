using System;
using System.IO;

namespace ImageDecoder.Common
{
    internal static class Iso3309Crc32
    {
        private const uint CrcPolynomonialReverse = 0xedB88320;
        private static bool s_tableCreated = false;
        private static readonly uint[] s_crcTable = new uint[256];

        private static void CreateCrcTable()
        {
            for (var i = 0; i < s_crcTable.Length; i++)
            {
                uint c = (uint)i;
                for (var j = 0; j < 8; j++)
                {
                    if ((c & 0x01) == 0x01)
                        c = CrcPolynomonialReverse ^ c >> 1;
                    else
                        c >>= 1;
                }
                s_crcTable[i] = c;
            }
            s_tableCreated = true;
        }

        public static bool VerifyCrc(BinaryReader reader, int length, int expectedCrcLength)
        {
            var crc = CalculateCrc(reader.BaseStream, length);
            return crc == Utilities.ReadUInt32(reader.ReadBytes(expectedCrcLength));
        }

        public static uint CalculateCrc(ReadOnlySpan<byte> data)
        {
            uint crc;
            using (var ms = new MemoryStream())
            {
                ms.Write(data);
                crc = CalculateCrc(ms, data.Length);
            }
            return crc;
        }

        public static uint CalculateCrc(Stream reader, int length)
        {
            // Note: Based on description in https://www.w3.org/TR/png-3/#D-CRCAppendix
            if (!s_tableCreated)
                CreateCrcTable();

            uint crc = 0xffffffff;
            var i = 0;
            while (i < length)
            {
                crc = s_crcTable[(crc ^ reader.ReadByte()) & 0xff] ^ crc >> 8;
                i++;
            }

            return crc ^ 0xffffffff;
        }

    }
}
