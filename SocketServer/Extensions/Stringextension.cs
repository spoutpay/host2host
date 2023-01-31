using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketServer.Extensions
{
    internal static class Stringextension
    {
        private static string HEXES = "0123456789ABCDEF";
        public static string ToHex(this string data)
        {
            if (data == null) throw new ArgumentNullException("data");

            var dataBytes = Encoding.UTF8.GetBytes(data);

            var result = "";
            foreach (byte b in dataBytes)
            {
                result += Convert.ToString(b, 16);
            }
            return result.ToUpper();
        }


        public static string ToUtf8(this string data)
        {
            if (data == null) throw new ArgumentNullException("data");
            if (data.Length % 2 != 0) throw new ArgumentException("data must be even length");

            var sb = new StringBuilder();
            for (var i = 0; i < data.Length; i += 2)
            {
                var hexChar = data.Substring(i, 2);
                sb.Append((char)Convert.ToByte(hexChar, 16));
            }
            return sb.ToString();
        }

        public static string IsoRemoveLength(this string data, bool isHex = false)
        {
            if (isHex)
            {
                return data.Substring(4);
            }
            return data.Substring(2);
        }

        public static string IsoGetMTI(this string data, bool isHex = false)
        {
            if (isHex)
            {
                return data.Substring(0, 8).ToUtf8();
            }

            return data.Substring(0, 4);
        }

        public static string IsoGetDataElementsPart(this string data, string bitmap, bool isHex = false, bool withMTI = true)
        {
            var MTI = withMTI ? data.IsoGetMTI(isHex) : "";
            if (isHex)
            {
                return data.Substring((MTI.Length * 2) + bitmap.Length / 4);
            }

            return data.Substring((MTI.Length + (bitmap.Length / 4)));
        }

        public static string BinaryToUtf8(this string data)
        {
            Encoding iso = Encoding.GetEncoding("ISO-8859-1");
            Encoding utf8 = Encoding.UTF8;
            byte[] isoBytes = iso.GetBytes(data);
            byte[] utfBytes = Encoding.Convert(iso, utf8, isoBytes);
            string result = utf8.GetString(isoBytes);
            return result;
        }

        public static string BytesToUtf8(this byte[] data)
        {
            Encoding utf8 = Encoding.UTF8;
            string result = utf8.GetString(data);
            return result;
        }

        public static byte[] HexToByteArray(this string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static byte[] Utf8ToByteArray(this string utf8)
        {
            return Encoding.UTF8.GetBytes(utf8);
        }

        public static string BytesToHexString(this byte[] value, int? size = null)
        {
            if (value == null)
                throw new ArgumentException("Value to convert cannot be null");
            StringBuilder hex = new StringBuilder(2 * (size ?? value.Length));
            for (int i = 0; i < size; i++)
            {
                byte b = value[i];
                hex.Append(HEXES[((b & 0xF0) >> 4)])
                        .Append(HEXES[((b & 0x0F))]);
            }
            return hex.ToString();
        }

        public static string ToBinHex(this int data)
        {
            StringBuilder hex = new StringBuilder();
            var c = data >> 8;
            hex.Append(HEXES[((c & 0xF0) >> 4)])
                        .Append(HEXES[((c & 0x0F))]);

            var d = data & 0xFF;
            hex.Append(HEXES[((d & 0xF0) >> 4)])
                        .Append(HEXES[((d & 0x0F))]);

            return hex.ToString();
        }

        public static byte[] ToSendData(this string data)
        {
            return ((data.Length.ToBinHex()) + (data.ToHex())).HexToByteArray();
        }

    }
}
