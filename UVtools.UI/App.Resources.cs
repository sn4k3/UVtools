using Avalonia.Platform;
using System;
using System.IO;
using Updatum;

namespace UVtools.UI;

public partial class App
{
    public static readonly Uri SvgLogoUri = new($"avares://{EntryApplication.AssemblyName}/Assets/Icons/UVtools.svg");

    public static string SvgLogo
    {
        get
        {
            if (field is null)
            {
                var logo = new Uri($"avares://{EntryApplication.AssemblyName}/Assets/Icons/UVtools.svg");
                using StreamReader streamReader = new(AssetLoader.Open(logo));
                field = streamReader.ReadToEnd();
            }

            return field;
        }
    }
}