using System;
using System.Drawing;
using System.Net.Http;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Utilities;

namespace Fakemail.Core
{
    public class PwnedPasswordApi(HttpClient httpClient) : IPwnedPasswordApi
    {
        public Task<string> RangeAsync(string url)
        {
            return httpClient.GetStringAsync(url);
        }
    }

    public interface IPwnedPasswordApi
    {
        Task<string> RangeAsync(string prefix);

        async Task<bool> IsPwnedPasswordAsync(string password)
        {
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
            var pwnedPasswordHashes = await RangeAsync($"https://api.pwnedpasswords.com/range/{hash.AsSpan(0, 5)}");
            return MemoryExtensions.Contains(pwnedPasswordHashes, hash.AsSpan(5), StringComparison.Ordinal);
        }
    }

    public static class Utils
    {        
        /// <summary>
        /// Create a base-62 string representing 16 bytes.
        /// The returned string will be 22 base-62 characters
        /// </summary>
        /// <returns></returns>
        public static string CreateId()
        {
            const int size = 16;
            const int outputSize = 22;

            Span<byte> bytes = stackalloc byte[size];
            RandomNumberGenerator.Fill(bytes);

            var i = new UInt128(BitConverter.ToUInt64(bytes), BitConverter.ToUInt64(bytes[8..]));

            return ToBase62(i, outputSize);
        }

        /// <summary>
        /// Create a base-62 string representing an arbritrary number of bytes.
        /// The returned string will be longer than the number of bytes; 
        /// e.g. 16 bytes will become 22 base-62 characters
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public static string CreateId(int size)
        {
            if (size < 1 || size > 16)
            {
                throw new ArgumentException("Id length must be between 1 and 16 bytes", nameof(size));
            }

            // log2(62) = 5.95419631039
            // log2(256) = 8
            // Therefore the output size of a byte array encoded in base-62 is a factor of 8/5.95419631039 larger, which is 1.34359023166
            var outputSize = (int)(1 + (size * 1.34359023166));

            Span<byte> bytes = stackalloc byte[size];
            RandomNumberGenerator.Fill(bytes);

            UInt128 i = bytes[0];
            for (int j = 1; j < size; j++)
            {
                i = i << 8 | bytes[j];
            }

            return ToBase62(i, outputSize);
        }

        private static string ToBase62(UInt128 i, int outputSize)
        {
            return string.Create(outputSize, i, (chars, i) =>
            {
                const int radix = 62;
                const string symbols = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

                var pos = outputSize;
                int rem;
                UInt128 quot;
                do
                {
                    quot = i / radix;
                    rem = (int)(i - (radix * quot));
                    chars[--pos] = symbols[rem];
                    i = quot;
                } while (i > 0);

                while (pos > 0)
                {
                    chars[--pos] = '0';
                }
            });
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
