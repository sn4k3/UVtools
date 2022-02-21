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
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Gets the Unix timestamp since Jan 1, 1970 UTC
        /// </summary>
        public static TimeSpan Timestamp => DateTime.UtcNow.Subtract(DateTime.UnixEpoch);

        /// <summary>
        /// Gets the Unix timestamp in seconds since Jan 1, 1970 UTC
        /// </summary>
        public static double TimestampSeconds => Timestamp.TotalSeconds;

        /// <summary>
        /// Gets the Unix minutes in seconds since Jan 1, 1970 UTC
        /// </summary>
        public static double TimestampMinutes => Timestamp.TotalMinutes;

        /// <summary>
        /// Gets the <see cref="DateTime"/> from a unix timestamp in seconds
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromTimestampSeconds(double seconds)
        {
            return DateTime.UnixEpoch.AddSeconds(seconds);
        }

        /// <summary>
        /// Gets the <see cref="DateTime"/> from a unix timestamp in minutes
        /// </summary>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static DateTime GetDateTimeFromTimestampMinutes(double minutes)
        {
            return DateTime.UnixEpoch.AddMinutes(minutes);
        }
    }
}
