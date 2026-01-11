using Avalonia.Controls;
using SukiUI.MessageBox;

namespace UVtools.UI.Structures;

public static class SukiMessageBoxUtilities
{
    public static SukiMessageBoxOptions GetDefaultOptions()
    {
        return new SukiMessageBoxOptions
        {

            LogoContent = new Avalonia.Svg.Svg(App.SvgLogoUri)
            {
                Width = 20,
                Height = 20,
                Path = App.SvgLogoUri.AbsolutePath
            },
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
    }
}