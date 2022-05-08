using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Fakemail.Core
{
    public class Utils
    {
        /// <summary>
        /// Create a base-62 string representing a number of up to 16 bytes.
        /// The returned string will have a length of up to 22 characters
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string CreateId(int size = 16)
        {
            const int radix = 62;
            const string symbols = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            var bytes = new byte[size + 1]; // append a zero byte to force the BigInteger to be unsigned
            RandomNumberGenerator.Fill(new Span<byte>(bytes, 0, size));

            void DivRem(BigInteger num, out BigInteger quot, out int rem)
            {
                quot = num / radix;
                rem = (int)(num - (radix * quot));
            }

            var i = new BigInteger(bytes); // uses little endian representation

            // log2(62) = 5.95419631039
            // log2(256) = 8
            // Therefore the output size of a byte array encoded in base-62 is a factor of 8/5.95419631039 larger, which is 1.34359023166
            int outputSize = (int)Math.Ceiling(size * 1.34359023166);
            var sb = new StringBuilder(outputSize);

            for (int j = 0; j < outputSize; j++)
                sb.Append('0');

            var pos = sb.Length;
            int rem;
            do
            {
                DivRem(i, out i, out rem);
                sb[--pos] = symbols[rem];
            } while (i > 0);

            pos = 0;
            return sb.ToString(pos, sb.Length - pos);
        }

        public static int Checksum(byte[] bytes)
        {
            var a = new Adler32();
            a.Update(bytes);
            return (int)a.Value;
        }

        public static int Checksum(string text)
        {
            return Checksum(Encoding.UTF8.GetBytes(text));
        }
    }
}
