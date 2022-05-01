using System.Security.Cryptography;

namespace Fakemail.Cryptography
{
    public static class Sha2Extensions
    {
        public static void Update(this SHA512 sha512, byte[] buf) => sha512.TransformBlock(buf, 0, buf.Length, null, 0);
        public static void Update(this SHA512 sha512, byte[] buf, int offset, int len) => sha512.TransformBlock(buf, offset, len, null, 0);

        public static byte[] Digest(this SHA512 sha512)
        {
            sha512.TransformFinalBlock(new byte[0], 0, 0);
            return sha512.Hash;
        }
    }
}