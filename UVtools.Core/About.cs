/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core;

public static class About
{
    public const string Software = "UVtools";
    public static Version Version => CoreAssembly.GetName().Version!;
    public static string VersionStr => Version.ToString(3);
    public static string VersionArch => $"{VersionStr} {RuntimeInformation.ProcessArchitecture}";
    public static string SoftwareWithVersion => $"{Software} v{VersionStr}";
    public static string SoftwareWithVersionArch => $"{Software} v{VersionArch}";
    public const string Author = "Tiago Conceição";
    public const string License = "GNU Affero General Public License v3.0 (AGPL)";
    public const string LicenseUrl = "https://github.com/sn4k3/UVtools/blob/master/LICENSE";
    public const string Website = "https://github.com/sn4k3/UVtools";
    public const string Donate = "https://paypal.me/SkillTournament";
    public const string Sponsor = "https://github.com/sponsors/sn4k3";

    #region Assembly properties
    public static Assembly CoreAssembly => Assembly.GetExecutingAssembly();

    public static string AssemblyVersion => CoreAssembly.GetName().Version?.ToString()!;

    public static string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name!;

    public static string AssemblyTitle
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != string.Empty)
                {
                    return titleAttribute.Title;
                }
            }
            return Path.GetFileNameWithoutExtension(CoreAssembly.Location);
        }
    }

    public static string AssemblyDescription
    {
        get
        {
            var attributes = CoreAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return string.Empty;
            }

            var description = ((AssemblyDescriptionAttribute)attributes[0]).Description + $"{Environment.NewLine}{Environment.NewLine}Available File Formats:";

            return FileFormat.AvailableFormats.SelectMany(fileFormat => fileFormat.FileExtensions).Aggregate(description, (current, fileExtension) => current + $"{Environment.NewLine}- {fileExtension.Description} (.{fileExtension.Extension})");
        }
    }

    public static string AssemblyProduct
    {
        get
        {
            var attributes = CoreAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public static string AssemblyCopyright
    {
        get
        {
            var attributes = CoreAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    public static string AssemblyCompany
    {
        get
        {
            var attributes = CoreAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }
    #endregion

    public static string SystemBits => Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";

    /// <summary>
    /// Gets UVtools born date and time
    /// </summary>
    public static DateTime Born => new(2020, 4, 6, 20, 33, 14);

    /// <summary>
    /// Gets UVtools years
    /// </summary>
    public static int YearsOld => Born.Age();

    /// <summary>
    /// Return full age in a readable string
    /// </summary>
    public static string AgeStr
    {
        get
        {
            var sb = new StringBuilder($"{YearsOld} years");
            var born = Born;
            var now = DateTime.Now;

            var months = 12 + now.Month - born.Month + (now.Day >= born.Day ? 0 : -1);
            if (months >= 12) months -= 12;
            if (months > 0) sb.Append($", {months} month(s)");



            var days = 31 + now.Day - born.Day;
            if (days >= 31) days -= 31;
            if (days > 0) sb.Append($", {days} day(s)");

            var hours = 12 + now.Hour - born.Hour;
            if (hours >= 12) hours -= 12;
            if (hours > 0) sb.Append($", {hours} hour(s)");

            var minutes = 60 + now.Minute - born.Minute;
            if (minutes >= 60) minutes -= 60;
            if (minutes > 0) sb.Append($", {minutes} minutes(s)");

            var seconds = 60 + now.Second - born.Second;
            if (seconds >= 60) seconds -= 60;
            if (seconds > 0) sb.Append($", {seconds} seconds(s)");


            return sb.ToString();
        }
    }

    /// <summary>
    /// Checks if today is UVtools birthday
    /// </summary>
    public static bool IsBirthday
    {
        get
        {
            var born = Born;
            var now = DateTime.Now;
            return born.Month == now.Month && born.Day == now.Day;
        }
    }

    public const string DemoFile = "UVtools_demo_file.sl1";

}