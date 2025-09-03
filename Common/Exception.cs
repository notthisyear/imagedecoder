using System;

namespace ImageDecoder.Common
{
    internal sealed class PngDecodingException(string? message = default, Exception? innerException = default) : Exception(message, innerException)
    {
    }

    internal sealed class PngEncodingException(string? message = default, Exception? innerException = default) : Exception(message, innerException)
    {
    }
}
