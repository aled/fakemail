using System;
using System.Security.Cryptography;
using System.Text;

namespace Fakemail.Cryptography
{
    struct Sha2CryptSalt    
    {
        public string algorithmIdentifier;
        public int rounds;
        public string randomSymbols;

        public Sha2CryptSalt Create()
        {
            throw new NotImplementedException();
        }

        public Sha2CryptSalt Parse(string encodedSalt)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Minimal version of base-64 encoding that uses a different set of symbols from the default .NET implementation.
    /// Padding is not implemented - the output length of the crypt is always a multiple of 3 bytes, and the salt uses
    /// the encoded characters directly.
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

    // C# implementation of the unix crypt function. Only SHA-512 is implemented.
    // This is a translation of the original source code, similar to the java version implemented by Apache commons codec.
    public static class UnixSha2Crypt
    {
        // these constants taken from the specification document at https://akkadia.org/drepper/SHA-crypt.txt
        public static readonly int MIN_ROUNDS = 1000;
        public static readonly int DEFAULT_ROUNDS = 5000;
        public static readonly int MAX_ROUNDS = 999999999;
        public static readonly int MAX_SALT_LEN = 16;
        public static readonly char SALT_FIELD_SEPARATOR = '$';
        public static readonly string SHA256_PREFIX = "5";
        public static readonly string SHA512_PREFIX = "6";
        public static readonly string ROUNDS_PREFIX = "rounds=";

        /// <summary>
        /// Generate a hash value that can be decoded using the unix libc crypt() function
        /// </summary>
        public static string Sha512Crypt(string key, string salt = "")
        {
            return Sha512Crypt(Encoding.UTF8.GetBytes(key), salt);
        }

        public static bool Sha512Validate(string key, string hash)
        {
            var salt = hash.Substring(0, hash.LastIndexOf("$"));

            return hash == Sha512Crypt(key, salt);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="keyBytes"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static string Sha512Crypt(byte[] keyBytes, string salt = "")
        {
            int rounds = DEFAULT_ROUNDS;
            string saltValue = "";
            bool roundsCustom = false;

            if (string.IsNullOrEmpty(salt))
            {
                // For some reason, the salt is limited to 16 base-64 symbols.
                // This limits the salt to 12 bytes.                
                saltValue = CryptBase64.GetRandomSymbols(16); // because there is no padding in the base64 implementation, needs to be a multiple of 4
            }
            else
            {
                var parts = salt.Split(SALT_FIELD_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;

                // the SHA version field (e.g. $6$) is optional
                if (parts[0] == SHA512_PREFIX)
                {
                    i++;
                }

                // the rounds field (e.g. rounds=1000) is optional
                if (parts.Length > i && parts[i].StartsWith(ROUNDS_PREFIX))
                {
                    if (!int.TryParse(parts[i].Substring(ROUNDS_PREFIX.Length), out rounds))
                    {
                        throw new ArgumentException("Invalid value for Rounds", nameof(salt));
                    }
                    roundsCustom = true;
                    i++;
                }

                // the actual salt value is required
                if (parts.Length > i)
                {
                    saltValue = parts[i];
                    if (saltValue.Length > MAX_SALT_LEN)
                    {
                        saltValue = saltValue.Substring(0, MAX_SALT_LEN);
                    }
                    foreach (char c in parts[i])
                    {
                        if ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '.' || c == '/')
                            continue;

                        throw new ArgumentException("Value must be base-64 encoded using symbols A-Z a-z . /", nameof(salt));
                    }
                }
            }

            return Sha512Crypt(keyBytes, rounds, saltValue, roundsCustom);
        }

        private static string Sha512Crypt(byte[] keyBytes, int rounds, string saltString, bool roundsCustom)
        {
            if (rounds < MIN_ROUNDS) 
                rounds = MIN_ROUNDS;
            
            if (rounds > MAX_ROUNDS) throw new ArgumentOutOfRangeException(nameof(rounds));

            if (saltString == null) throw new ArgumentNullException(nameof(saltString));
            if (saltString.Length > MAX_SALT_LEN)
            {
                saltString = saltString.Substring(0, MAX_SALT_LEN);
            }

            if (keyBytes == null) throw new ArgumentNullException(nameof(keyBytes));

            var saltPrefix = SALT_FIELD_SEPARATOR + SHA512_PREFIX + SALT_FIELD_SEPARATOR;
            var saltBytes = Encoding.UTF8.GetBytes(saltString);
            var saltLen = saltBytes.Length;
            var keyLen = keyBytes.Length;

            var ctx = SHA512.Create();
            ctx.Update(keyBytes);
            ctx.Update(saltBytes);

            var altCtx = SHA512.Create();
            altCtx.Update(keyBytes);
            altCtx.Update(saltBytes);
            altCtx.Update(keyBytes);
            var altResult = altCtx.Digest();

            int cnt = keyLen;
            var blocksize = 64; // would be 32 for SHA-256
            while (cnt > blocksize)
            {
                ctx.Update(altResult!, 0, blocksize);
                cnt -= blocksize;
            }

            ctx.Update(altResult!, 0, cnt);

            cnt = keyLen;
            while (cnt > 0)
            {
                if ((cnt & 1) != 0)
                {
                    ctx.Update(altResult!, 0, blocksize);
                }
                else
                {
                    ctx.Update(keyBytes, 0, keyBytes.Length);
                }
                cnt >>= 1;
            }

            altResult = ctx.Digest();

            altCtx = SHA512.Create();
            for (int i = 1; i <= keyLen; i++)
            {
                altCtx.Update(keyBytes);
            }

            byte[] tempResult = altCtx.Digest();

            byte[] pBytes = new byte[keyLen];
            int cp = 0;
            while (cp < keyLen - blocksize)
            {
                Array.Copy(tempResult, 0, pBytes, cp, blocksize);
                cp += blocksize;
            }
            Array.Copy(tempResult, 0, pBytes, cp, keyLen - cp);

            altCtx = SHA512.Create();

            for (int i = 1; i <= 16 + (altResult[0] & 0xff); i++)
            {
                altCtx.Update(saltBytes);
            }

            tempResult = altCtx.Digest();

            byte[] sBytes = new byte[saltLen];
            cp = 0;
            while (cp < saltLen - blocksize)
            {
                Array.Copy(tempResult, 0, sBytes, cp, blocksize);
                cp += blocksize;
            }
            Array.Copy(tempResult, 0, sBytes, cp, saltLen - cp);

            for (int i = 0; i <= rounds - 1; i++)
            {
                // a) start digest C
                /*
                 * New context.
                 */
                ctx = SHA512.Create();

                // b) for odd round numbers add the byte sequense P to digest C
                // c) for even round numbers add digest A/C
                /*
                 * Add key or last result.
                 */
                if ((i & 1) != 0)
                {
                    ctx.Update(pBytes, 0, keyLen);
                }
                else
                {
                    ctx.Update(altResult, 0, blocksize);
                }

                // d) for all round numbers not divisible by 3 add the byte sequence S
                /*
                 * Add salt for numbers not divisible by 3.
                 */
                if (i % 3 != 0)
                {
                    ctx.Update(sBytes, 0, saltLen);
                }

                // e) for all round numbers not divisible by 7 add the byte sequence P
                /*
                 * Add key for numbers not divisible by 7.
                 */
                if (i % 7 != 0)
                {
                    ctx.Update(pBytes, 0, keyLen);
                }

                // f) for odd round numbers add digest A/C
                // g) for even round numbers add the byte sequence P
                /*
                 * Add key or last result.
                 */
                if ((i & 1) != 0)
                {
                    ctx.Update(altResult, 0, blocksize);
                }
                else
                {
                    ctx.Update(pBytes, 0, keyLen);
                }

                // h) finish digest C.
                /*
                 * Create intermediate result.
                 */
                altResult = ctx.Digest();
            }

            // 22. Produce the output string. This is an ASCII string of the maximum
            // size specified above, consisting of multiple pieces:
            //
            // a) the salt salt_prefix, $5$ or $6$ respectively
            //
            // b) the rounds=<N> specification, if one was present in the input
            // salt string. A trailing '$' is added in this case to separate
            // the rounds specification from the following text.
            //
            // c) the salt string truncated to 16 characters
            //
            // d) a '$' character
            /*
             * Now we can construct the result string. It consists of three parts.
             */
            var buffer = new StringBuilder(saltPrefix);
            if (roundsCustom)
            {
                buffer.Append(ROUNDS_PREFIX);
                buffer.Append(rounds);
                buffer.Append("$");
            }
            buffer.Append(saltString);
            buffer.Append("$");

            if (blocksize == 32)
            {
                CryptBase64.b64from24bit(altResult[0], altResult[10], altResult[20], 4, buffer);
                CryptBase64.b64from24bit(altResult[21], altResult[1], altResult[11], 4, buffer);
                CryptBase64.b64from24bit(altResult[12], altResult[22], altResult[2], 4, buffer);
                CryptBase64.b64from24bit(altResult[3], altResult[13], altResult[23], 4, buffer);
                CryptBase64.b64from24bit(altResult[24], altResult[4], altResult[14], 4, buffer);
                CryptBase64.b64from24bit(altResult[15], altResult[25], altResult[5], 4, buffer);
                CryptBase64.b64from24bit(altResult[6], altResult[16], altResult[26], 4, buffer);
                CryptBase64.b64from24bit(altResult[27], altResult[7], altResult[17], 4, buffer);
                CryptBase64.b64from24bit(altResult[18], altResult[28], altResult[8], 4, buffer);
                CryptBase64.b64from24bit(altResult[9], altResult[19], altResult[29], 4, buffer);
                CryptBase64.b64from24bit((byte)0, altResult[31], altResult[30], 3, buffer);
            }
            else
            {
                CryptBase64.b64from24bit(altResult[0], altResult[21], altResult[42], 4, buffer);
                CryptBase64.b64from24bit(altResult[22], altResult[43], altResult[1], 4, buffer);
                CryptBase64.b64from24bit(altResult[44], altResult[2], altResult[23], 4, buffer);
                CryptBase64.b64from24bit(altResult[3], altResult[24], altResult[45], 4, buffer);
                CryptBase64.b64from24bit(altResult[25], altResult[46], altResult[4], 4, buffer);
                CryptBase64.b64from24bit(altResult[47], altResult[5], altResult[26], 4, buffer);
                CryptBase64.b64from24bit(altResult[6], altResult[27], altResult[48], 4, buffer);
                CryptBase64.b64from24bit(altResult[28], altResult[49], altResult[7], 4, buffer);
                CryptBase64.b64from24bit(altResult[50], altResult[8], altResult[29], 4, buffer);
                CryptBase64.b64from24bit(altResult[9], altResult[30], altResult[51], 4, buffer);
                CryptBase64.b64from24bit(altResult[31], altResult[52], altResult[10], 4, buffer);
                CryptBase64.b64from24bit(altResult[53], altResult[11], altResult[32], 4, buffer);
                CryptBase64.b64from24bit(altResult[12], altResult[33], altResult[54], 4, buffer);
                CryptBase64.b64from24bit(altResult[34], altResult[55], altResult[13], 4, buffer);
                CryptBase64.b64from24bit(altResult[56], altResult[14], altResult[35], 4, buffer);
                CryptBase64.b64from24bit(altResult[15], altResult[36], altResult[57], 4, buffer);
                CryptBase64.b64from24bit(altResult[37], altResult[58], altResult[16], 4, buffer);
                CryptBase64.b64from24bit(altResult[59], altResult[17], altResult[38], 4, buffer);
                CryptBase64.b64from24bit(altResult[18], altResult[39], altResult[60], 4, buffer);
                CryptBase64.b64from24bit(altResult[40], altResult[61], altResult[19], 4, buffer);
                CryptBase64.b64from24bit(altResult[62], altResult[20], altResult[41], 4, buffer);
                CryptBase64.b64from24bit((byte)0, (byte)0, altResult[63], 2, buffer);
            }

            // Clear the buffer for the intermediate result so that people attaching to processes or reading core dumps
            // cannot get any information.
            Array.Fill(tempResult, (byte)0);
            Array.Fill(pBytes, (byte)0);
            Array.Fill(sBytes, (byte)0);
            ctx.Clear();
            altCtx.Clear();
            Array.Fill(keyBytes, (byte)0);
            Array.Fill(saltBytes, (byte)0);

            return buffer.ToString();
        }

        public static bool Verify(string input, string hash)
        {
            throw new NotImplementedException();
        }
    }
}