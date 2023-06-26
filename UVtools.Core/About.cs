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

    public const string TermsOfUseTitle = $"Terms of Use for {Software}";

    public const string TermsOfUseHeader = $"These terms of use govern your use of the {Software} application.\n" +
                                           $"By using the {Software}, you agree to be bound by the following terms of use.\n" +
                                           $"If you do not agree with any part of these terms, you should not use the {Software}.";
    public const string TermsOfUse =
        $"1) License:\n" +
        $"The {Software} is an open-source application distributed under the GNU Affero General Public License v3.0 (AGPL). This license governs your use, modification, and distribution of the {Software}. Please refer to the AGPL license agreement accompanying the {Software} for more details.\n\n" +
        
        $"2) No Liability:\n" +
        $"The {Software} is provided \"as is\" and without any warranties or guarantees of any kind, whether express or implied. In no event shall we (the developers, contributors, or any associated parties) be liable for any damages or liabilities arising out of the use, misuse or inability to use UVtools, including but not limited to any indirect, incidental, or consequential damages, even if we have been advised of the possibility of such damages. You acknowledge that you use the {Software} at your own risk and it is your responsibility to ensure the suitability, accuracy, and reliability of {Software} for your intended purpose.\n\n" +
        
        $"3) Third-Party Components and Services:\n" +
        $"The {Software} may include or rely on third-party components, libraries, or services. These third-party components are subject to their respective licenses and terms of use. We do not assume any responsibility or liability for any third-party components or services used in {Software}.\n\n" +
        
        $"4) Intellectual Property:\n" +
        $"The {Software} may contain intellectual property owned by third parties, such as trademarks, logos, or copyrighted material. Your use of the {Software} does not grant you any rights or licenses to such intellectual property.\n\n" +

        $"5) No Support:\n" +
        $"We do not guarantee any support for the {Software}. While we may provide resources such as documentation, forums, or community platforms, we are under no obligation to respond to inquiries, address issues, or provide updates for the {Software}.\n\n" +
        
        $"6) Indemnification:\n" +
        $"You agree to indemnify, defend, and hold us harmless from any claims, damages, liabilities, or expenses arising out of or related to your use of the {Software} or any violation of these terms of use.\n\n" +
        
        $"7) Donations:\n" +
        $"We may accept voluntary donations to support the development and maintenance of {Software}. However, such donations do not grant any additional rights, privileges, or powers to the donor. They are purely voluntary and non-binding. Once a donation is made, it cannot be refunded or reversed.\n\n" +
        
        $"8) Modification of Terms:\n" +
        $"We reserve the right to modify or update these terms of use at any time without prior notice. It is your responsibility to review these terms periodically. Your continued use of the {Software} after any modifications constitutes your acceptance of the updated terms.\n\n" +
        
        $"9) Severability:\n" +
        $"If any provision of these terms of use is found to be invalid or unenforceable, the remaining provisions shall remain in full force and effect.\n\n" +
        
        $"10) Entire Agreement:\n" +
        $"These terms of use in addition with the license constitute the entire agreement between you and us regarding the use of the {Software}, superseding any prior agreements or understandings.\n\n" +
        
        //$"If you have any questions or concerns regarding these terms of use, please contact us via the GitHub page.\n" +
        $"By using the {Software}, you acknowledge that you have read, understood, and agreed to these terms of use."; 

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