using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Avalonia;
using ReactiveUI;

namespace UVtools.WPF.Structures
{
    public class PixelPicker : ReactiveObject
    {
        private bool _isSet;
        private Point _location = new Point(0,0);
        private byte _brightness;

        public bool IsSet
        {
            get => _isSet;
            private set => this.RaiseAndSetIfChanged(ref _isSet, value);
        }

        public Point Location
        {
            get => _location;
            private set => this.RaiseAndSetIfChanged(ref _location, value);
        }

        public byte Brightness
        {
            get => _brightness;
            private set => this.RaiseAndSetIfChanged(ref _brightness, value);
        }

        public void Set(Point location, byte brightness)
        {
            Location = location;
            Brightness = brightness;
            IsSet = true;
        }

        public override string ToString()
        {
            return $"{{X={_location.X}, Y={_location.Y}, B={_brightness}}}";
        }
    }
}
