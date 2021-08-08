/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Reflection;

namespace UVtools.Core
{
    public static class About
    {
        public const string Software = "UVtools";
        public static string SoftwareWithVersion => $"{Software} v{VersionStr}";
        public const string Author = "Tiago Conceição";
        public const string Company = "PTRTECH";
        public const string License = "GNU Affero General Public License v3.0 (AGPL)";
        public const string LicenseUrl = "https://github.com/sn4k3/UVtools/blob/master/LICENSE";
        public const string Website = "https://github.com/sn4k3/UVtools";
        public const string Donate = "https://paypal.me/SkillTournament";
        public const string Sponsor = "https://github.com/sponsors/sn4k3";

        public const string DemoFile = "UVtools_demo_file.sl1";

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static string VersionStr => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
        public static string Arch => Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
    }
}
