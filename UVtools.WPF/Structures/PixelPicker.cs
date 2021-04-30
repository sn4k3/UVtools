using System.Drawing;
using UVtools.Core.Objects;

namespace UVtools.WPF.Structures
{
    public class PixelPicker : BindableBase
    {
        private bool _isSet;
        private Point _location = new Point(0,0);
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

        public byte Brightness
        {
            get => _brightness;
            private set => RaiseAndSetIfChanged(ref _brightness, value);
        }

        public void Set(Point location, byte brightness)
        {
            Location = location;
            Brightness = brightness;
            IsSet = true;
        }

        public void Reset()
        {
            Location = Point.Empty;
            Brightness = 0;
            IsSet = false;
        }

        public override string ToString()
        {
            return $"{{X={_location.X}, Y={_location.Y}, B={_brightness}}}";
        }
    }
}
