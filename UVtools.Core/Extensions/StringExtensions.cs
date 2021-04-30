/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace UVtools.Core.Extensions
{
    public static class StringExtensions
    {
        public static string RemoveFromEnd(this string input, int count)
        {
            return input.Remove(input.Length - count);
        }

        /// <summary>
        /// Upper the first character in a string
        /// </summary>
        /// <param name="input">Input string</param>
        /// <returns>Modified string with fist character upper</returns>
        public static string FirstCharToUpper(this string input)
        {
            switch (input)
            {
                case null: throw new ArgumentNullException(nameof(input));
                case "": throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input));
                default: return input.First().ToString().ToUpper() + input[1..];
            }
        }

        public static string Repeat(this string s, int n)
            => n <= 0 ? string.Empty : new StringBuilder(s.Length * n).Insert(0, s, n).ToString();

        /// <summary>
        /// Converts a string into a target type
        /// </summary>
        /// <typeparam name="T">Target type to convert into</typeparam>
        /// <param name="input">Value</param>
        /// <returns>Converted value into target type</returns>
        public static T Convert<T>(this string input)
        {
            var converter = TypeDescriptor.GetConverter(typeof(T));
            if (converter is not null)
            {
                //Cast ConvertFromString(string text) : object to (T)
                return (T)converter.ConvertFromString(input);
            }
            return default(T);
        }
    }
}
