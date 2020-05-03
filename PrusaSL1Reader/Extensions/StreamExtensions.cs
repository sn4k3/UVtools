/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PrusaSL1Reader.Extensions
{
    public static class StreamExtensions
    {
        /// <summary>
        /// Converts stream into byte array
        /// </summary>
        /// <param name="stream">Input</param>
        /// <returns>Byte array data</returns>
        public static byte[] ToArray(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
