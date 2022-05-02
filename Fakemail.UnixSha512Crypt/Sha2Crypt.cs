using System;
using System.Security.Cryptography;
using System.Text;

namespace Fakemail.Cryptography
{
    /// <summary>
    /// C# implementation of the unix crypt() algorithm from https://akkadia.org/drepper/SHA-crypt.txt
    ///  
    /// Implemented for SHA-512 and SHA-256
    /// 
    /// This allows a custom password database to be interoperable between C# and unix
    /// </summary>
    public static class Sha2Crypt
    {
        public static readonly int MIN_ROUNDS = 1000;
        public static readonly int DEFAULT_ROUNDS = 5000;
        public static readonly int MAX_ROUNDS = 999999999;
        public static readonly int MAX_SALT_LEN = 16;
        public static readonly char SALT_FIELD_SEPARATOR = '$';
        public static readonly string SHA256_IDENTIFIER = "5";
        public static readonly string SHA512_IDENTIFIER = "6";
        public static readonly string SHA256_PREFIX = "$5$";
        public static readonly string SHA512_PREFIX = "$6$";
        public static readonly int SHA256_BLOCKSIZE = 32;
        public static readonly int SHA512_BLOCKSIZE = 64;
        public static readonly string ROUNDS_PREFIX = "rounds=";

        /// <summary>
        /// Generate a Sha512Crypt hash value that can be decoded using the unix libc crypt() function
        /// </summary>
        public static string Sha512Crypt(string key, string salt = "")
        {
            return Crypt("SHA512", Encoding.UTF8.GetBytes(key), salt);
        }

        /// <summary>
        /// Generate a Sha256Crypt hash value that can be decoded using the unix libc crypt() function
        /// </summary>
        public static string Sha256Crypt(string key, string salt = "")
        {
            return Crypt("SHA256", Encoding.UTF8.GetBytes(key), salt);
        }

        /// <summary>
        /// Validate a password given a previously computed hash
        /// </summary>
        /// <param name="key">The password to validate</param>
        /// <param name="hash">The stored hash of the password</param>
        /// <returns></returns>
        public static bool Validate(string key, string hash)
        {
            var lastHashIndex = hash.LastIndexOf("$");
            
            if (lastHashIndex < 0)
                return false;

            var salt = hash.Substring(0, lastHashIndex);

            if (hash.StartsWith(SHA256_PREFIX))
                return hash == Sha256Crypt(key, salt);
            else if (hash.StartsWith(SHA512_PREFIX))
                return hash == Sha512Crypt(key, salt);

            throw new ArgumentException("No algorithm identifier in hash", nameof(hash));
        }

        /// <summary>
        /// Implement the Sha512Crypt algorithm
        /// This is mostly a translation of the Java version from Apache commmons codec (sha2crypt.java), 
        /// which is itself a translation of the original C version
        /// </summary>
        /// <param name="algorithm">SHA256 or SHA512</param>
        /// <param name="keyBytes">The password to be encrypted. Common practice seems to be UTF-8 encoding</param>
        /// <param name="salt">The salt to encrypt the password with. Will be automatically generated if missing or empty</param>
        /// <returns>A Sha512 crypt string that is interoperable with the unix crypt() function</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static string Crypt(string algorithm, byte[] keyBytes, string salt = "")
        {
            int rounds = DEFAULT_ROUNDS;
            string saltValue = "";
            bool roundsCustom = false;

            if (string.IsNullOrEmpty(salt))
            {
                // For some reason, the salt is limited to 16 base-64 symbols.
                // This limits the salt to 12 bytes.                
                saltValue = CryptBase64.GetRandomSymbols(16);
            }
            else
            {
                var parts = salt.Split(SALT_FIELD_SEPARATOR, StringSplitOptions.RemoveEmptyEntries);
                int i = 0;

                // the SHA version field (e.g. $6$) is optional.
                if (parts[i] == SHA512_IDENTIFIER && algorithm == "SHA512")
                {
                    i++;
                }
                else if (parts[i] == SHA256_IDENTIFIER && algorithm == "SHA256")
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

                        throw new ArgumentException("Salt must contain only symbols A-Z a-z . /", nameof(salt));
                    }
                }
            }

            return Crypt(keyBytes, rounds, saltValue, roundsCustom, algorithm);
        }

        private static string Crypt(byte[] keyBytes, int rounds, string saltString, bool roundsCustom, string algorithm)
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


            int blocksize;
            string saltPrefix;
            if (algorithm == "SHA512")
            {
                blocksize = SHA512_BLOCKSIZE;
                saltPrefix = SHA512_PREFIX;
            }
            else if (algorithm == "SHA256")
            {
                blocksize = SHA256_BLOCKSIZE;
                saltPrefix = SHA256_PREFIX;
            }
            else
            {
                throw new ArgumentException("Algorithm must be SHA512 or SHA256", algorithm);
            }

            var saltBytes = Encoding.UTF8.GetBytes(saltString);
            var saltLen = saltBytes.Length;
            var keyLen = keyBytes.Length;

            // 1. start digest A
            // Prepare for the real work.
            var ctx = HashAlgorithm.Create(algorithm);
            
            // 2. the password string is added to digest A
            /*
             * Add the key string.
             */
            ctx.Update(keyBytes);

            // 3. the salt string is added to digest A. This is just the salt string
            // itself without the enclosing '$', without the magic salt_prefix $5$ and
            // $6$ respectively and without the rounds=<N> specification.
            //
            // NB: the MD5 algorithm did add the $1$ salt_prefix. This is not deemed
            // necessary since it is a constant string and does not add security
            // and /possibly/ allows a plain text attack. Since the rounds=<N>
            // specification should never be added this would also create an
            // inconsistency.
            /*
             * The last part is the salt string. This must be at most 16 characters and it ends at the first `$' character
             * (for compatibility with existing implementations).
             */
            ctx.Update(saltBytes);

            // 4. start digest B
            /*
             * Compute alternate sha512 sum with input KEY, SALT, and KEY. The final result will be added to the first
             * context.
             */
            var altCtx = HashAlgorithm.Create(algorithm);

            // 5. add the password to digest B
            /*
             * Add key.
             */
            altCtx.Update(keyBytes);

            // 6. add the salt string to digest B
            /*
             * Add salt.
             */
            altCtx.Update(saltBytes);

            // 7. add the password again to digest B
            /*
             * Add key again.
             */
            altCtx.Update(keyBytes);

            // 8. finish digest B
            /*
             * Now get result of this (32 bytes) and add it to the other context.
             */
            var altResult = altCtx.Digest();

            // 9. For each block of 32 or 64 bytes in the password string (excluding
            // the terminating NUL in the C representation), add digest B to digest A
            /*
             * Add for any character in the key one byte of the alternate sum.
             */
            /*
             * (Remark: the C code comment seems wrong for key length > 32!)
             */
            int cnt = keyLen;
            while (cnt > blocksize)
            {
                ctx.Update(altResult!, 0, blocksize);
                cnt -= blocksize;
            }

            // 10. For the remaining N bytes of the password string add the first
            // N bytes of digest B to digest A
            ctx.Update(altResult!, 0, cnt);

            // 11. For each bit of the binary representation of the length of the
            // password string up to and including the highest 1-digit, starting
            // from to lowest bit position (numeric value 1):
            //
            // a) for a 1-digit add digest B to digest A
            //
            // b) for a 0-digit add the password string
            //
            // NB: this step differs significantly from the MD5 algorithm. It
            // adds more randomness.
            /*
             * Take the binary representation of the length of the key and for every 1 add the alternate sum, for every 0
             * the key.
             */
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

            // 12. finish digest A
            /*
             * Create intermediate result.
             */
            altResult = ctx.Digest();

            // 13. start digest DP
            /*
             * Start computation of P byte sequence.
             */
            altCtx = HashAlgorithm.Create(algorithm);

            // 14. for every byte in the password (excluding the terminating NUL byte
            // in the C representation of the string)
            //
            // add the password to digest DP
            /*
             * For every character in the password add the entire password.
             */
            for (int i = 1; i <= keyLen; i++)
            {
                altCtx.Update(keyBytes);
            }

            // 15. finish digest DP
            /*
             * Finish the digest.
             */
            byte[] tempResult = altCtx.Digest();

            // 16. produce byte sequence P of the same length as the password where
            //
            // a) for each block of 32 or 64 bytes of length of the password string
            // the entire digest DP is used
            //
            // b) for the remaining N (up to 31 or 63) bytes use the first N
            // bytes of digest DP
            /*
             * Create byte sequence P.
             */
            byte[] pBytes = new byte[keyLen];
            int cp = 0;
            while (cp < keyLen - blocksize)
            {
                Array.Copy(tempResult, 0, pBytes, cp, blocksize);
                cp += blocksize;
            }
            Array.Copy(tempResult, 0, pBytes, cp, keyLen - cp);

            // 17. start digest DS
            /*
             * Start computation of S byte sequence.
             */
            altCtx = HashAlgorithm.Create(algorithm);

            // 18. repeast the following 16+A[0] times, where A[0] represents the first
            // byte in digest A interpreted as an 8-bit unsigned value
            //
            // add the salt to digest DS
            /*
             * For every character in the password add the entire password.
             */
            for (int i = 1; i <= 16 + (altResult[0] & 0xff); i++)
            {
                altCtx.Update(saltBytes);
            }

            // 19. finish digest DS
            /*
             * Finish the digest.
             */
            tempResult = altCtx.Digest();

            // 20. produce byte sequence S of the same length as the salt string where
            //
            // a) for each block of 32 or 64 bytes of length of the salt string
            // the entire digest DS is used
            //
            // b) for the remaining N (up to 31 or 63) bytes use the first N
            // bytes of digest DS
            /*
             * Create byte sequence S.
             */
            // Remark: The salt is limited to 16 chars, how does this make sense?
            byte[] sBytes = new byte[saltLen];
            cp = 0;
            while (cp < saltLen - blocksize)
            {
                Array.Copy(tempResult, 0, sBytes, cp, blocksize);
                cp += blocksize;
            }
            Array.Copy(tempResult, 0, sBytes, cp, saltLen - cp);

            // 21. repeat a loop according to the number specified in the rounds=<N>
            // specification in the salt (or the default value if none is
            // present). Each round is numbered, starting with 0 and up to N-1.
            //
            // The loop uses a digest as input. In the first round it is the
            // digest produced in step 12. In the latter steps it is the digest
            // produced in step 21.h. The following text uses the notation
            // "digest A/C" to describe this behavior.
            /*
             * Repeatedly run the collected hash value through sha512 to burn CPU cycles.
             */
            for (int i = 0; i <= rounds - 1; i++)
            {
                // a) start digest C
                /*
                 * New context.
                 */
                ctx = HashAlgorithm.Create(algorithm);

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

            // e) the base-64 encoded final C digest. The encoding used is as
            // follows:
            // [...]
            //
            // Each group of three bytes from the digest produces four
            // characters as output:
            //
            // 1. character: the six low bits of the first byte
            // 2. character: the two high bits of the first byte and the
            // four low bytes from the second byte
            // 3. character: the four high bytes from the second byte and
            // the two low bits from the third byte
            // 4. character: the six high bits from the third byte
            //
            // The groups of three bytes are as follows (in this sequence).
            // These are the indices into the byte array containing the
            // digest, starting with index 0. For the last group there are
            // not enough bytes left in the digest and the value zero is used
            // in its place. This group also produces only three or two
            // characters as output for SHA-512 and SHA-512 respectively.

            // This was just a safeguard in the C implementation:
            // int buflen = salt_prefix.length() - 1 + ROUNDS_PREFIX.length() + 9 + 1 + salt_string.length() + 1 + 86 + 1;
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
    }
}