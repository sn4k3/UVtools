/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Printer
{
    public class Machine : BindableBase
    {
        #region Members
        private float _totalPrintTime;
        private float _totalDisplayOnTime;
        #endregion

        #region Properties

        public PrinterBrand Brand { get; set; } = PrinterBrand.Generic;
        public string Name { get; set; } = FileFormats.FileFormat.DefaultMachineName;
        public string Model { get; set; } = FileFormats.FileFormat.DefaultMachineName;

        public ushort ResolutionX { get; set; }

        public ushort ResolutionY { get; set; }

        public float DisplayWidth { get; set; }

        public float DisplayHeight { get; set; }

        public float MachineZ { get; set; }

        public FlipDirection DisplayMirror { get; set; }
        
        public object? Tag { get; set; }

        public float TotalPrintTime
        {
            get => _totalPrintTime;
            set => RaiseAndSetIfChanged(ref _totalPrintTime, (float)Math.Max(0f, Math.Round(value, 2)));
        }

        public string TotalPrintTimeString => TimeSpan.FromSeconds(_totalPrintTime).ToTimeString();

        public float TotalDisplayOnTime
        {
            get => _totalDisplayOnTime;
            set => RaiseAndSetIfChanged(ref _totalDisplayOnTime, (float)Math.Max(0f, Math.Round(value, 2)));
        }

        public string DisplayTotalOnTimeString => TimeSpan.FromSeconds(_totalDisplayOnTime).ToTimeString();

        public float TotalDisplayOffTime => TotalPrintTime - TotalDisplayOnTime;

        public string DisplayTotalOffTimeString => TimeSpan.FromSeconds(TotalDisplayOffTime).ToTimeString();

        #endregion

        #region Constructor

        public Machine() { }

        public Machine(PrinterBrand brand, string name, string? model, ushort resolutionX, ushort resolutionY, float displayWidth, float displayHeight, float machineZ, FlipDirection displayMirror = default)
        {
            Brand = brand;
            Name = name;
            Model = model ?? name;

            /*if (model is null && name.Length > 0)
            {
                var whiteSpaceIndex = name.IndexOf(' ', StringComparison.Ordinal) + 1;
                if (whiteSpaceIndex > 0) Model = name[whiteSpaceIndex..];
            }*/

            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            DisplayWidth = displayWidth;
            DisplayHeight = displayHeight;
            MachineZ = machineZ;
            DisplayMirror = displayMirror;
        }

        public Machine(PrinterBrand brand, string name, string? model, Size resolution, SizeF display, float machineZ, FlipDirection displayMirror = default)
            : this(brand, name, model, (ushort)resolution.Width, (ushort)resolution.Height, display.Width, display.Height, machineZ, displayMirror) { }

        public Machine(PrinterBrand brand, string name, Size resolution, SizeF display, float machineZ, FlipDirection displayMirror = default)
            : this(brand, name, null, (ushort)resolution.Width, (ushort)resolution.Height, display.Width, display.Height, machineZ, displayMirror) { }

        public Machine(ushort resolutionX, ushort resolutionY, float displayWidth, float displayHeight, float machineZ, FlipDirection displayMirror = default, PrinterBrand brand = default, string name = FileFormats.FileFormat.DefaultMachineName, string? model = null)
            : this(brand, name, model, resolutionX, resolutionY, displayWidth, displayHeight, machineZ, displayMirror) {}

        #endregion

        #region Overrides

        protected bool Equals(Machine other)
        {
            return ResolutionX == other.ResolutionX && ResolutionY == other.ResolutionY && DisplayWidth.Equals(other.DisplayWidth) && DisplayHeight.Equals(other.DisplayHeight) && MachineZ.Equals(other.MachineZ) && Brand == other.Brand && Name == other.Name && Model == other.Model;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Machine)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ResolutionX, ResolutionY, DisplayWidth, DisplayHeight, MachineZ, (int)Brand, Name, Model);
        }

        public override string ToString()
        {
            return $"{Name} | Resolution={ResolutionX}x{ResolutionY}(px) Display={DisplayWidth}x{DisplayHeight}(mm) PixelSize={PixelWidthMicrons}x{PixelHeightMicrons}(µm) Z={MachineZ}mm";
        }
        #endregion

        #region Methods
        /// <summary>
        /// Gets or sets the pixels per mm on X direction
        /// </summary>
        public virtual float Xppmm => DisplayWidth > 0 ? ResolutionX / DisplayWidth : 0;

        /// <summary>
        /// Gets or sets the pixels per mm on Y direction
        /// </summary>
        public virtual float Yppmm => DisplayHeight > 0 ? ResolutionY / DisplayHeight : 0;

        /// <summary>
        /// Gets or sets the pixels per mm
        /// </summary>
        public SizeF Ppmm => new(Xppmm, Yppmm);

        /// <summary>
        /// Gets the maximum (Width or Height) pixels per mm 
        /// </summary>
        public float PpmmMax => Ppmm.Max();

        /// <summary>
        /// Gets the pixel width in millimeters
        /// </summary>
        public float PixelWidth => DisplayWidth > 0 && ResolutionX > 0 ? MathF.Round(DisplayWidth / ResolutionX, 3) : 0;

        /// <summary>
        /// Gets the pixel height in millimeters
        /// </summary>
        public float PixelHeight => DisplayHeight > 0 && ResolutionY > 0 ? MathF.Round(DisplayHeight / ResolutionY, 3) : 0;

        /// <summary>
        /// Gets the pixel size in millimeters
        /// </summary>
        public SizeF PixelSize => new(PixelWidth, PixelHeight);

        /// <summary>
        /// Gets the maximum pixel between width and height in millimeters
        /// </summary>
        public float PixelSizeMax => PixelSize.Max();

        /// <summary>
        /// Gets the pixel area in millimeters
        /// </summary>
        public float PixelArea => PixelSize.Area();

        /// <summary>
        /// Gets the pixel width in microns
        /// </summary>
        public float PixelWidthMicrons => DisplayWidth > 0 ? MathF.Round(DisplayWidth / ResolutionX * 1000, 3) : 0;

        /// <summary>
        /// Gets the pixel height in microns
        /// </summary>
        public float PixelHeightMicrons => DisplayHeight > 0 ? MathF.Round(DisplayHeight / ResolutionY * 1000, 3) : 0;

        /// <summary>
        /// Gets the pixel size in microns
        /// </summary>
        public SizeF PixelSizeMicrons => new(PixelWidthMicrons, PixelHeightMicrons);

        /// <summary>
        /// Gets the maximum pixel between width and height in microns
        /// </summary>
        public float PixelSizeMicronsMax => PixelSizeMicrons.Max();

        /// <summary>
        /// Gets the pixel area in millimeters
        /// </summary>
        public float PixelAreaMicrons => PixelSizeMicrons.Area();

        public Machine Clone()
        {
            return (Machine)MemberwiseClone();
        }
        #endregion

        #region Static methods

        /// <summary>
        /// Preset list of machines
        /// </summary>
        public static Machine[] Machines =>
            new Machine[]{
                // Creality
                /*new(PrinterBrand.Creality, "Halot One",      "CL-60",    1620, 2560, 81,      128,     160),
                new(PrinterBrand.Creality, "Halot One Pro",  "CL-70",    2560, 2400, 130.56f, 122.4f,  160),
                new(PrinterBrand.Creality, "Halot One Plus", "CL-79",    4320, 2560, 172.8f,  102.4f,  160),
                new(PrinterBrand.Creality, "Halot Sky",      "CL-89",    3840, 2400, 192,     120,     200),
                new(PrinterBrand.Creality, "Halot Sky Plus", "CL-92",    5760, 3600, 198.14f, 123.84f, 210),
                new(PrinterBrand.Creality, "Halot Lite",     "CL-89L",   3840, 2400, 192,     120,     200),
                new(PrinterBrand.Creality, "Halot Max",      "CL-133",   3840, 2160, 293.76f, 165.28f, 300),
                new(PrinterBrand.Creality, "CT133 Pro",      "CT133PRO", 3840, 2160, 293.76f, 165.24f, 300),
                new(PrinterBrand.Creality, "CT-005 Pro",     "CT-005",   3840, 2400, 192,     120,     250),*/

                new(PrinterBrand.Anet, "Anet N4", "N4", 1440, 2560, 68.04f, 120.96f, 135f, FlipDirection.Horizontally),
                new(PrinterBrand.Anet, "Anet N7", "N7", 2560, 1600, 192f, 120f, 300f, FlipDirection.Horizontally),

                new(PrinterBrand.AnyCubic, "AnyCubic Photon M3", "Photon M3", 4096, 2560, 163.84f, 102.40f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon M3", "Photon M3", 4096, 2560, 163.84f, 102.40f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon M3 Max", "Photon M3 Max", 6480, 3600, 298.08f, 165.60f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon M3 Plus", "Photon M3 Plus", 5760, 3600, 198.15f, 123.84f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon M3 Premium", "Photon M3 Premium", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M5", "Photon Mono M5", 11520, 5120, 218.88f, 122.88f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M5s", "Photon Mono M5s", 11520, 5120, 218.88f, 122.88f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M5s Pro", "Photon Mono M5s Pro", 13312, 5120, 223.6416f, 126.976f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M7", "Photon Mono M7", 13312, 5120, 223.6416f, 126.976f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M7 Pro", "Photon Mono M7 Pro", 13312, 5120, 223.6416f, 126.976f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono M7 Max", "Photon Mono M7 Max", 6480, 3600, 298.08f, 165.6f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono", "Photon Mono", 1620, 2560, 82.62f, 130.56f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 2", "Photon Mono 2", 4096, 2560, 143.36f, 89.60f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 4K", "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 4", "Photon Mono 4", 9024, 5120, 153.408f, 87.040f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 4K", "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 4 Ultra", "Photon Mono 4 Ultra", 9024, 5120, 153.408f, 87.040f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono 4K", "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono SE", "Photon Mono SE", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono SQ", "Photon Mono SQ", 2400, 2560, 120f, 128f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono X 6K", "Photon Mono X 6K", 5760, 3600, 198.15f, 123.84f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono X 6Ks", "Photon Mono X 6Ks", 5760, 3600, 195.84f, 122.40f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono X", "Photon Mono X", 3840, 2400, 192f, 120f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Mono X2", "Photon Mono X2", 4096, 2560, 196.61f, 122.88f, 260f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon", "Photon", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon S", "Photon S", 1440, 2560, 68.04f, 120.96f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Ultra", "Photon Ultra", 1280, 720, 102.4f, 57.6f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon D2", "Photon D2", 2560, 1440, 130.56f, 73.44f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon X", "Photon X", 2560, 1600, 192f, 120f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.AnyCubic, "AnyCubic Photon Zero", "Photon Zero", 480, 854, 55.4f, 98.63f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Concepts3D, "Concepts3D Athena 8K", "Athena 8K", 7680, 4320, 218.88f, 123.12f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Concepts3D, "Concepts3D Athena 12K", "Athena 12K", 11520, 5120, 218.88f, 122.88f, 245f, FlipDirection.Horizontally),

                new(PrinterBrand.Creality, "Creality CT-005 Pro", "CT-005", 3840, 2400, 192f, 120f, 250f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality CT-133 Pro", "CT133PRO", 3840, 2160, 293.76f, 165.24f, 300f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Lite CL-89L", "CL-89L", 3840, 2400, 192f, 120f, 200f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Max CL-133", "CL-133", 3840, 2160, 293.76f, 165.24f, 300f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot One CL-60", "CL-60", 1620, 2560, 81f, 128f, 160f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot One Plus CL-79", "CL-79", 4320, 2560, 172.8f, 102.4f, 160f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot One Pro CL-70", "CL-70", 2560, 2400, 130.56f, 122.4f, 160f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Sky CL-89", "CL-89", 3840, 2400, 192f, 120f, 200f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Sky Plus CL-92", "CL-92", 5760, 3600, 198.14f, 123.84f, 210f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Ray CL925", "CL925", 5760, 3600, 198.14f, 123.84f, 210f, FlipDirection.None),
                new(PrinterBrand.Creality, "Creality Halot Mage CL-103L", "CL-103L", 7680, 4320, 228.096f, 128.304f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "Creality Halot Mage Pro CL-103", "CL-103", 7680, 4320, 228.096f, 128.304f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "Creality LD-002H", "LD-002H", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "Creality LD-002R", "LD-002R", 1440, 2560, 68.04f, 120.96f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "Creality LD-006", "LD-006", 3840, 2400, 192f, 120f, 245f, FlipDirection.Horizontally),

                new(PrinterBrand.Elegoo, "Elegoo Jupiter", "Jupiter", 5448, 3064, 277.848f, 156.264f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars", "Mars", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 2 Pro", "Mars 2 Pro", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 2", "Mars 2", 1620, 2560, 82.62f, 130.56f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 3", "Mars 3", 4098, 2560, 143.43f, 89.6f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 3 Pro", "Mars 3 Pro", 4098, 2560, 143.43f, 89.6f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 4", "Mars 4", 8520, 4320, 153.36f, 77.76f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 4 DLP", "Mars 4 DLP", 4098, 2560, 132.8f, 74.7f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 4 Max", "Mars 4 Max", 5760, 3600, 195.84f, 122.4f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars 4 Ultra", "Mars 4 Ultra", 8520, 4320, 153.36f, 77.76f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Mars C", "Mars C", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn", "Saturn", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 2", "Saturn 2", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 3", "Saturn 3", 11520, 5120, 218.88f, 122.88f, 249.7f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 3 Ultra", "Saturn 3 Ultra", 11520, 5120, 218.88f, 122.88f, 260f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 4 Ultra 12K", "Saturn 4 Ultra 12K", 11520, 5120, 218.88f, 122.88f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 4 Ultra 16K", "Saturn 4 Ultra 16K", 15120, 6230, 211.68f, 118.37f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn 8K", "Saturn 8K", 7680, 4320, 218.88f, 123.12f, 210f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Elegoo Saturn S", "Saturn S", 4098, 2560, 196.704f, 122.88f, 210f, FlipDirection.Horizontally),

                new(PrinterBrand.EPAX, "EPAX DX1 PRO", "DX1 PRO", 4098, 2560, 143.43f, 89.6f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX DX10 Pro 5K", "DX10 Pro 5K", 4920, 2880, 221.4f, 129.6f, 120f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX DX10 Pro 8K", "DX10 Pro 8K", 7680, 4320, 218.88f, 123.12f, 120f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX E10 5K", "E10 5K", 4920, 2880, 221.4f, 129.6f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX E10 8K", "E10 8K", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX E10 Mono", "E10 Mono", 3840, 2400, 192f, 120f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX E6 Mono", "E6 Mono", 1620, 2560, 81f, 128f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X1 4KS", "X1 4KS", 4098, 2560, 143.43f, 89.6f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X1", "X1", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X10 4K Mono", "X10 4K Mono", 3840, 2400, 192f, 120f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X10 5K", "X10 5K", 4920, 2880, 221.4f, 129.6f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X10", "X10", 1600, 2560, 135.36f, 216.57f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X133 4K Mono", "X133 4K Mono", 3840, 2160, 293.76f, 165.24f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X133 6K", "X133 6K", 5760, 3240, 288f, 162f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X156 4K Color", "X156 4K Color", 3840, 2160, 345.6f, 194.4f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "EPAX X1K 2K Mono", "X1K 2K Mono", 1620, 2560, 82.62f, 130.56f, 155f, FlipDirection.Horizontally),

                new(PrinterBrand.FlashForge, "FlashForge Explorer MAX", "Explorer MAX", 2560, 1600, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Focus 13.3", "Focus 13.3", 3842, 2171, 292f, 165f, 400f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Focus 8.9", "Focus 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Foto 13.3", "Foto 13.3", 3842, 2171, 292f, 165f, 400f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Foto 6.0", "Foto 6.0", 2600, 1560, 130f, 78f, 155f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Foto 8.9", "Foto 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Foto 8.9S", "Foto 8.9S", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "FlashForge Hunter", "Hunter", 1920, 1080, 120f, 67.5f, 150f, FlipDirection.Vertically),

                new(PrinterBrand.Emake3D, "Emake3D Galaxy 1", "Galaxy 1", 8000, 4000, 400f, 200f, 400f, FlipDirection.Horizontally),

                new(PrinterBrand.Kelant, "Kelant S400", "S400", 2560, 1600, 192f, 120f, 200f, FlipDirection.Horizontally),

                new(PrinterBrand.Longer, "Longer Orange 10", "Orange 10", 480, 854, 55.44f, 98.64f, 140f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Longer Orange 120", "Orange 120", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Longer Orange 30", "Orange 30", 1440, 2560, 68.04f, 120.96f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Longer Orange 4K", "Orange 4K", 3840, 6480, 120.96f, 68.04f, 190f, FlipDirection.Horizontally),

                new(PrinterBrand.Nova3D, "Nova3D Bene4 Mono", "Bene4 Mono", 1566, 2549, 79.865f, 129.998f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Bene4", "Bene4", 1352, 2512, 70f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Bene5", "Bene5", 1566, 2549, 79.865f, 129.998f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Bene6", "Bene6", 4098, 2560, 143.43f, 89.6f, 180f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Elfin", "Elfin", 1410, 2531, 73f, 131f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Elfin2 Mono SE", "Elfin2 Mono SE", 1470, 2549, 75f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Elfin2", "Elfin2", 1352, 2512, 70f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Elfin3 Mini", "Elfin3 Mini", 1079, 1904, 68f, 120f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Whale", "Whale", 3840, 2400, 192f, 120f, 250f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Whale2", "Whale2", 3840, 2400, 192f, 120f, 250f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Nova3D Whale3 Pro", "Whale3 Pro", 7680, 4320, 228.096f, 128.304f, 260f, FlipDirection.Vertically),

                new(PrinterBrand.Peopoly, "Peopoly Phenom L", "Phenom L", 3840, 2160, 345.6f, 194.4f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Peopoly Phenom Noir", "Phenom Noir", 3840, 2160, 293.76f, 165.24f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Peopoly Phenom XXL", "Phenom XXL", 3840, 2160, 526.08f, 295.92f, 550f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Peopoly Phenom XXL v2", "Phenom XXL v2", 3840, 2160, 526.08f, 295.92f, 550f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Peopoly Phenom", "Phenom", 3840, 2160, 276.48f, 155.52f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Peopoly Phenom Forge", "Phenom Forge", 5760, 3240, 288f, 162f, 350f, FlipDirection.Horizontally),

                new(PrinterBrand.Phrozen, "Phrozen Shuffle 16", "Shuffle 16", 3840, 2160, 337.92f, 190.08f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Shuffle 4K", "Shuffle 4K", 2160, 3840, 68.04f, 120.96f, 170f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Phrozen Shuffle Lite", "Shuffle Lite", 1440, 2560, 68.04f, 120.96f, 170f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Phrozen Shuffle XL Lite", "Shuffle XL Lite", 2560, 1600, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Shuffle XL", "Shuffle XL", 2560, 1600, 192f, 120f, 200f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Phrozen Shuffle", "Shuffle", 1440, 2560, 67.68f, 120.32f, 200f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Phrozen Sonic 4K", "Sonic 4K", 3840, 2160, 134.4f, 75.6f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mega 8K", "Sonic Mega 8K", 7680, 4320, 330.24f, 185.76f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mighty 4K", "Sonic Mighty 4K", 3840, 2400, 199.68f, 124.8f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mighty 8K", "Sonic Mighty 8K", 7680, 4320, 218.88f, 123.12f, 235f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mini 4K", "Sonic Mini 4K", 3840, 2160, 134.4f, 75.6f, 130f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mini 8K", "Sonic Mini 8K", 7500, 3240, 165f, 71.28f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mini 8K S", "Sonic Mini 8K S", 7536, 3240, 165.792f, 71.28f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mini", "Sonic Mini", 1080, 1920, 68.04f, 120.96f, 130f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Phrozen Sonic", "Sonic", 1080, 1920, 68.04f, 120.96f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Sonic Mighty Revo", "Sonic Mighty Revo", 13320, 5120, 223.776f, 126.976f, 235f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Phrozen Transform", "Transform", 3840, 2160, 291.84f, 164.16f, 400f, FlipDirection.None),

                new(PrinterBrand.QIDI, "QIDI I-Box Mono", "I-Box Mono", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "QIDI S-Box", "S-Box", 1600, 2560, 135.36f, 216.576f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "QIDI Shadow5.5", "Shadow5.5", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "QIDI Shadow6.0 Pro", "Shadow6.0 Pro", 1440, 2560, 74.52f, 132.48f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Uniformation, "Uniformation GKone", "GKone", 4920, 2880, 221.4f, 129.6f, 245f, FlipDirection.Vertically),
                new(PrinterBrand.Uniformation, "Uniformation GKtwo", "GKtwo", 7680, 4320, 228.089f, 128.3f, 200f, FlipDirection.Vertically),

                new(PrinterBrand.Uniz, "Uniz IBEE", "IBEE", 3840, 2400, 192f, 120f, 220f, FlipDirection.Vertically),

                new(PrinterBrand.Prusa, "Prusa SL1", "SL1", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Prusa, "Prusa SL1S SPEED", "SL1S SPEED", 1620, 2560, 128f, 81f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Voxelab, "Voxelab Ceres 8.9", "Ceres 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Voxelab Polaris 5.5", "Polaris 5.5", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Voxelab Proxima 6", "Proxima 6", 1620, 2560, 82.62f, 130.56f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Voxelab Proxima 6 SVGX", "Proxima 6 SVGX", 2560, 1620, 130.56f, 82.62f, 155f, FlipDirection.Horizontally),

                new(PrinterBrand.Wanhao, "Wanhao CGR Mini Mono", "CGR Mini Mono", 1620, 2560, 82.62f, 130.56f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "Wanhao CGR Mono", "CGR Mono", 1620, 2560, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "Wanhao D7", "D7", 2560, 1440, 120.96f, 68.5f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "Wanhao D8", "D8", 2560, 1600, 192f, 120f, 180f, FlipDirection.Horizontally),

                new(PrinterBrand.Zortrax, "Zortrax Inkspire", "Inkspire", 1440, 2560, 74.67f, 132.88f, 175f, FlipDirection.Horizontally),
            };

        /// <summary>
        /// Gets all machines from PrusaSlicer profiles
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Machine> GetMachinesFromPrusaSlicer()
        {
            var psPrinterFiles = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Assets", "PrusaSlicer", "printer"));
            foreach (var file in psPrinterFiles)
            {
                var displayMirror = FlipDirection.None;
                var filenameNoExt = Path.GetFileNameWithoutExtension(file);
                var split = filenameNoExt.Split(' ', 2);
                if (!Enum.TryParse(split[0], true, out PrinterBrand brand))
                {
                    brand = filenameNoExt.StartsWith("UVtools Prusa") ? PrinterBrand.Prusa : PrinterBrand.Generic;
                }

                using var reader = new StreamReader(file);

                var machine = new Machine
                {
                    Name = filenameNoExt,
                    Model = split[1]
                };

                while (reader.ReadLine() is { } line)
                {
                    var keyValue = line.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if(keyValue.Length < 2) continue;

                    var key = keyValue[0];
                    var value = keyValue[1];
                    if (key.StartsWith("display_pixels_x"))
                    {
                        if (!ushort.TryParse(value, out var resolutionX)) continue;
                        machine.ResolutionX = resolutionX;
                    }

                    if (key.StartsWith("display_pixels_y"))
                    {
                        if (!ushort.TryParse(value, out var resolutionY)) continue;
                        machine.ResolutionY = resolutionY;
                    }

                    if (key.StartsWith("display_width"))
                    {
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var displayWidth)) continue;
                        machine.DisplayWidth = displayWidth;
                    }

                    if (key.StartsWith("display_height"))
                    {
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var displayHeight)) continue;
                        machine.DisplayHeight = displayHeight;
                    }

                    if (key.StartsWith("max_print_height"))
                    {
                        if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var machineZ)) continue;
                        machine.MachineZ = machineZ;
                    }

                    if (key.StartsWith("display_mirror_x"))
                    {
                        if(value.StartsWith("1")) displayMirror = displayMirror == FlipDirection.None ? FlipDirection.Horizontally : FlipDirection.Both;
                    }

                    if (key.StartsWith("display_mirror_y"))
                    {
                        if (value.StartsWith("1")) displayMirror = displayMirror == FlipDirection.None ? FlipDirection.Vertically : FlipDirection.Both;
                    }
                }

                if(machine.ResolutionX == 0 || machine.ResolutionY == 0) continue;
                machine.Brand = brand;
                machine.DisplayMirror = displayMirror;
                yield return machine;
            }
        }


        public static string GenerateMachinePresetsFromPrusaSlicer()
        {
            var machines = GetMachinesFromPrusaSlicer();
            var sb = new StringBuilder();

            PrinterBrand lastBrand = default;
            foreach (var machine in machines)
            {
                if (lastBrand != machine.Brand)
                {
                    if(sb.Length > 0) sb.AppendLine();
                    lastBrand = machine.Brand;
                }
                sb.AppendLine($"new(PrinterBrand.{machine.Brand}, \"{machine.Name}\", \"{machine.Model}\", {machine.ResolutionX}, {machine.ResolutionY}, {machine.DisplayWidth}f, {machine.DisplayHeight}f, {machine.MachineZ}f, FlipDirection.{machine.DisplayMirror}),");
            }
            
            return sb.ToString();
        }

        #endregion
    }
}
