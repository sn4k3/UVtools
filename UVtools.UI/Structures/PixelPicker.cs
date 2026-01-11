using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UVtools.UI.Structures;

public partial class PixelPicker : ObservableObject
{
    [ObservableProperty]
    public partial bool IsSet { get; private set; }

    [ObservableProperty]
    public partial Point Location { get; set; }

    [ObservableProperty]
    public partial PointF LcdLocation { get; set; }

    [ObservableProperty]
    public partial byte Brightness { get; private set; }

    public void Set(Point location, byte brightness, PointF lcdLocation = default)
    {
        Location = location;
        LcdLocation = lcdLocation;
        Brightness = brightness;
        IsSet = true;
    }

    public void Reset()
    {
        Location = Point.Empty;
        LcdLocation = PointF.Empty;
        Brightness = 0;
        IsSet = false;
    }

    public override string ToString()
    {
        var text = $"{{X={Location.X}, Y={Location.Y}, B={Brightness}}}";
        if (!LcdLocation.IsEmpty)
        {
            text += $"\n{{X={LcdLocation.X}, Y={LcdLocation.Y} mm}}";
        }

        return text;
    }
}