using System.IO;
using System.Security.Cryptography;

namespace UVtools.Core.Objects
{
    public static class StaticObjects
    {
        public static SHA256 Sha256 { get; } = SHA256.Create();

        // Compute the file's hash.
        public static byte[] GetHashSha256(string filename)
        {
            using (var stream = File.OpenRead(filename))
            {
                return Sha256.ComputeHash(stream);
            }
        }
    }
}
