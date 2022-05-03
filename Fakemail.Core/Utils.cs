using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Fakemail.Core
{
    public class Utils
    {
        public static string CreateId()
        {
            var bytes = new byte[17]; // 16 bytes of data plus a zero byte (to force the BigInteger to be unsigned)

            RandomNumberGenerator.Fill(new Span<byte>(bytes, 0, 16));

            const int radix = 62;
            const string symbols = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

            static void DivRem(BigInteger num, out BigInteger quot, out int rem)
            {
                quot = num / radix;
                rem = (int)(num - (radix * quot));
            }

            var i = new BigInteger(bytes); // uses little endian representation
            var sb = new StringBuilder("0000000000000000000000");
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
    }
}
