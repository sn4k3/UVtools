using System;
using System.IO;
using System.Security.Cryptography;

namespace UVtools.Core.Objects
{
    public static class StaticObjects
    {
        public static readonly SHA256 Sha256 = SHA256.Create();

        public static readonly string[] LineBreakCharacters = {"\r\n", "\r", "\n"};

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
