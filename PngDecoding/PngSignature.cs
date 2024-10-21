using System.IO;

namespace ImageDecoder.PngDecoding
{
    internal readonly ref struct PngSignature
    {    
        #region Private fields
        private static readonly byte[] s_expectedSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        #endregion
        
        public static bool ValidatePngSignature(BinaryReader reader)
        {
            var signature = reader.ReadBytes(s_expectedSignature.Length);
            var isValid = true;
            for (var i = 0; i < s_expectedSignature.Length; i++)
            {
                if (signature[i] != s_expectedSignature[i])
                {
                    isValid = false;
                    break;
                }
            }
            return isValid;
        }

        public static void WritePngSignature(FileStream fs)
        {
            fs.Write(s_expectedSignature);
        }
    }
}