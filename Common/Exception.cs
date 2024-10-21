using System;

namespace ImageDecoder.Common
{
    internal class PngDecodingException(string? message = default, Exception? innerException = default) : Exception(message, innerException)
    {
    }

    internal class PngEncodingException(string? message = default, Exception? innerException = default) : Exception(message, innerException)
    {
    }
}