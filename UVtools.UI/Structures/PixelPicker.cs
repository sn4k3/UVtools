using System.Drawing;
using UVtools.Core.Objects;

namespace UVtools.UI.Structures;

public class PixelPicker : BindableBase
{
    private bool _isSet;
    private Point _location;
    private PointF _lcdLocation;
    private byte _brightness;

    public bool IsSet
    {
        get => _isSet;
        private set => RaiseAndSetIfChanged(ref _isSet, value);
    }

    public Point Location
    {
        get => _location;
        set => RaiseAndSetIfChanged(ref _location, value);
    }

    public PointF LcdLocation
    {
        get => _lcdLocation;
        set => RaiseAndSetIfChanged(ref _lcdLocation, value);
    }

    public byte Brightness
    {
        get => _brightness;
        private set => RaiseAndSetIfChanged(ref _brightness, value);
    }

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
        var text = $"{{X={_location.X}, Y={_location.Y}, B={_brightness}}}";
        if (!_lcdLocation.IsEmpty)
        {
            text += $"\n{{X={_lcdLocation.X}, Y={_lcdLocation.Y} mm}}";
        }

        return text;
    }
}