/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using System;

namespace UVtools.Core.Extensions;

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

    /// <summary>
    /// Calculates the age in years of the current System.DateTime object today.
    /// </summary>
    /// <param name="birthDate">The date of birth</param>
    /// <returns>Age in years today. 0 is returned for a future date of birth.</returns>
    public static int Age(this DateTime birthDate)
    {
        return Age(birthDate, DateTime.Today);
    }

    /// <summary>
    /// Calculates the age in years of the current System.DateTime object on a later date.
    /// </summary>
    /// <param name="birthDate">The date of birth</param>
    /// <param name="laterDate">The date on which to calculate the age.</param>
    /// <returns>Age in years on a later day. 0 is returned as minimum.</returns>
    public static int Age(this DateTime birthDate, DateTime laterDate)
    {
        var age = laterDate.Year - birthDate.Year;

        if (age > 0)
        {
            age -= Convert.ToInt32(laterDate.Date < birthDate.Date.AddYears(age));
        }
        else
        {
            age = 0;
        }

        return age;
    }

    /// <summary>
    /// Convert a <see cref="TimeSpan"/> to a string representation of the time
    /// </summary>
    /// <param name="timeSpan"></param>
    /// <returns></returns>
    public static string ToTimeString(this TimeSpan timeSpan)
    {
        var totalSeconds = timeSpan.TotalSeconds;
        return totalSeconds switch
        {
            < 60 => $"{totalSeconds}s",                               // Less than a minute
            < 3600 => timeSpan.ToString(@"mm\m\:ss\s"),        // Less than an hour
            < 86400 => timeSpan.ToString(@"hh\h\:mm\m\:ss\s"), // Less than a day
            _ => timeSpan.ToString(@"dd\ \d\a\y\(\s\)\ hh\h\:mm\m\:ss\s")
        };
    }
}