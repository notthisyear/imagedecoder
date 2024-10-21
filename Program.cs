

using System;
using ImageDecoder.PngDecoding;

namespace ImageDecoder
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
                args = ["image.png"];

            var imageToRead = args[0];
            Console.WriteLine($"Decoding file '{imageToRead}'...");
            var file = PngDecoder.Decode(imageToRead, true);
            file.EncodeAndWrite("image_out.png");
        }
    }
}