using System.Security.Cryptography;

namespace Fakemail.Cryptography
{
    /// <summary>
    /// Methods to make it easier to translate the Java source code of Sha2Crypt from apache-commons
    /// </summary>
    public static class HashAlgorithmExtensions
    {
        public static void Update(this HashAlgorithm algorithm, byte[] buf) => algorithm.TransformBlock(buf, 0, buf.Length, null, 0);
        public static void Update(this HashAlgorithm algorithm, byte[] buf, int offset, int len) => algorithm.TransformBlock(buf, offset, len, null, 0);

        public static byte[] Digest(this HashAlgorithm algorithm)
        {
            algorithm.TransformFinalBlock(new byte[0], 0, 0);
            return algorithm.Hash ?? throw new Exception("Error calculating hash");
        }
    }
}