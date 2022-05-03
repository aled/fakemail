using System.Security.Cryptography;
using System.Text;

namespace Fakemail.Cryptography
{
    /// <summary>
    /// Minimal version of base-64 encoding that uses a different set of symbols from the default .NET implementation.
    /// Padding is not implemented differently from 'normal' base 64; the salt uses the symbols directly and the hash
    /// uses it's own padding mechanism.
    /// </summary>
    class CryptBase64
    {
        static readonly char[] BASE64_SYMBOLS = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

        public static void b64from24bit(byte b2, byte b1, byte b0, int outLen, StringBuilder builder)
        {
            int w = (b2 << 16) | (b1 << 8) | b0;
            
            int n = outLen;
            while (n-- > 0)
            {
                builder.Append(BASE64_SYMBOLS[w & 0x3f]);
                w >>= 6;
            }
        }

        public static string GetRandomSymbols(int count)
        {
            var sb = new StringBuilder(count);

            for (int i = 0; i < count; i++)
                sb.Append(BASE64_SYMBOLS[RandomNumberGenerator.GetInt32(0, 64)]);

            return sb.ToString();
        }
    }
}