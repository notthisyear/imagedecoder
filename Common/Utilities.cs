using System;
using System.IO;
using System.Reflection;

namespace ImageDecoder.Common
{
    internal static class Utilities
    {
        public enum ByteOrder{
            LittleEndian,
            BigEndian,
        }

        public static (T? attr, Exception? e) GetCustomAttributeFromEnum<T>(this Enum? value)
        {
            if (value == null)
                return (default, new ArgumentNullException(nameof(value)));

            var t = value.GetType();
            var name = Enum.GetName(t, value);
            if (string.IsNullOrEmpty(name))
                return (default, new InvalidOperationException($"Could not get name of enum {t}"));

            if (t.GetField(name) is not FieldInfo field)
                return (default, new InvalidOperationException($"Could not get field info for enum {name}"));

            if (Attribute.GetCustomAttribute(field, typeof(T)) is T attr)
                return (attr, default);
            else
                return (default, new ArgumentException($"{value} does not have attribute '{typeof(T)}'"));
        }

        public static uint ReadUInt32(this ReadOnlySpan<byte> bytes, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            if (bytes.Length != 4)
                throw new ArgumentException($"Unexpected length of byte span - expected 4 got {bytes.Length}");

            byte shift = byteOrder == ByteOrder.BigEndian ? (byte)24 : (byte)0;
            int change = byteOrder == ByteOrder.BigEndian ? -8 : 8;
            
            uint result = 0;
            foreach (var b in bytes)
            {
                result += (uint)(b << shift);
                shift = (byte)(shift + change);
            }
            return result;
        }

        public static void WriteUInt32(this uint value, FileStream fs, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            byte shift = byteOrder == ByteOrder.BigEndian ? (byte)24 : (byte)0;
            int change = byteOrder == ByteOrder.BigEndian ? -8 : 8;
            
            for (var i = 0; i < 4; i++)
            {
                var v = (byte)((value & ((uint)0xff << shift)) >> shift);
                fs.WriteByte(v);
                shift = (byte)(shift + change);
            }
        }

        public static Span<byte> AsSpan(this uint value, ByteOrder byteOrder = ByteOrder.BigEndian)
        {
            byte shift = byteOrder == ByteOrder.BigEndian ? (byte)24 : (byte)0;
            int change = byteOrder == ByteOrder.BigEndian ? -8 : 8;

            Span<byte> span = new(new byte[4]);
            for (var i = 0; i < 4; i++)
            {
                span[i] = (byte)((value & ((uint)0xff << shift)) >> shift);
                shift = (byte)(shift + change);
            }
            return span;

        }

        public static bool TryGetAsEnum<TIn, TOut>(TIn v, out TOut? enumValue) where TOut : Enum
        {
            enumValue = default;
            if (typeof(TIn) != Enum.GetUnderlyingType(typeof(TOut)))
                return false;
            
            if (v == null)
                return false;

            if (Enum.IsDefined(typeof(TOut), v))
            {
                enumValue = (TOut)(object)v;
                return true;
            }
            return false;
        }   
    }
}