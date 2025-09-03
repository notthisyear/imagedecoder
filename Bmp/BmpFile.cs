using System.IO;

namespace ImageDecoder.Bmp
{
    internal static class BmpFile
    {
        public static void Write(string filename, BmpPixelArray pixelArray)
        {
            var totalFileSize = BitmapFileHeader.Size + BitmapInfoHeader.Size + pixelArray.Size;
            var fileHeader = new BitmapFileHeader(totalFileSize, BitmapFileHeader.Size + BitmapInfoHeader.Size);
            var bitmapInfoHeader = new BitmapInfoHeader(pixelArray.Width, pixelArray.Height, pixelArray.BitsPerPixel);

            using FileStream fs = new(filename, FileMode.Create, FileAccess.Write);
            fileHeader.Write(fs);
            bitmapInfoHeader.Write(fs);
            pixelArray.Write(fs);
            return;
        }
    }
}
