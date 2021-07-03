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
        public static string Software = "UVtools";
        public static string Author = "Tiago Conceição";
        public static string Company = "PTRTECH";
        public static string Website = "https://github.com/sn4k3/UVtools";
        public static string Donate = "https://paypal.me/SkillTournament";

        public static string DemoFile = "UVtools_demo_file.sl1";

        public static Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public static string VersionStr => Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
    }
}
