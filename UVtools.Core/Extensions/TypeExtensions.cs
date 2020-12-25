/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Extensions
{
    public static class TypeExtensions
    {
        /// <summary>
        /// Creates a new instance of this type
        /// </summary>
        /// <param name="type"></param>
        public static object CreateInstance(this Type type)
        {
            return Activator.CreateInstance(type);
        }

        /// <summary>
        /// Creates a new instance of this type
        /// </summary>
        /// <typeparam name="T">Target type</typeparam>
        /// <param name="type"></param>
        /// <returns>New instance of <see cref="T"/></returns>
        public static T CreateInstance<T>(this Type type)
        {
            return (T)Activator.CreateInstance(type);
        }

        public static byte ToByte(this bool value) => value ? 1 : 0;
    }
}
