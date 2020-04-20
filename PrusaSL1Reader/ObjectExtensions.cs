/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PrusaSL1Reader
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Converts a string into a target type
        /// </summary>
        /// <typeparam name="T">Target type to convert into</typeparam>
        /// <param name="input">Value</param>
        /// <returns>Converted value into target type</returns>
        public static T Convert<T>(this object input)
        {
            return input.ToString().Convert<T>();
        }

        public static object DeserializeFromBytes(byte[] bytes)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bytes))
            {
                return formatter.Deserialize(stream);
            }
        }
    }
}
