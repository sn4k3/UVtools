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

        /// <summary>
        /// Gets or sets the printer brand
        /// </summary>
        public PrinterBrand Brand { get; set; } = PrinterBrand.Generic;

        /// <summary>
        /// Gets or sets the printer model
        /// </summary>
        public string Model { get; set; } = FileFormats.FileFormat.DefaultMachineName;

        /// <summary>
        /// Gets the printer name, which is a combination of <see cref="Brand"/> and <see cref="Model"/>>
        /// </summary>
        public string Name => $"{Brand} {Model}";

        /// <summary>
        /// Gets or sets the printer resolution in X direction
        /// </summary>
        public ushort ResolutionX { get; set; }

        /// <summary>
        /// Gets or sets the printer resolution in Y direction
        /// </summary>
        public ushort ResolutionY { get; set; }

        /// <summary>
        /// Gets or sets the display width in millimeters
        /// </summary>
        public float DisplayWidth { get; set; }

        /// <summary>
        /// Gets or sets the display height in millimeters
        /// </summary>
        public float DisplayHeight { get; set; }

        /// <summary>
        /// Gets or sets the machine Z height in millimeters
        /// </summary>
        public float MachineZ { get; set; }

        /// <summary>
        /// Gets or sets the display mirror direction
        /// </summary>
        public FlipDirection DisplayMirror { get; set; }

        public object? Tag { get; set; }

        /// <summary>
        /// Gets or sets the total print time in seconds
        /// </summary>
        public float TotalPrintTime
        {
            get => _totalPrintTime;
            set => RaiseAndSetIfChanged(ref _totalPrintTime, (float)Math.Max(0f, Math.Round(value, 2)));
        }

        /// <summary>
        /// Gets the total print time as a formatted string
        /// </summary>
        public string TotalPrintTimeString => TimeSpan.FromSeconds(_totalPrintTime).ToTimeString();

        /// <summary>
        /// Gets or sets the total display on time in seconds
        /// </summary>
        public float TotalDisplayOnTime
        {
            get => _totalDisplayOnTime;
            set => RaiseAndSetIfChanged(ref _totalDisplayOnTime, (float)Math.Max(0f, Math.Round(value, 2)));
        }

        /// <summary>
        /// Gets the total display on time as a formatted string
        /// </summary>
        public string DisplayTotalOnTimeString => TimeSpan.FromSeconds(_totalDisplayOnTime).ToTimeString();

        /// <summary>
        /// Gets the total display off time in seconds, which is the difference between total print time and total display on time
        /// </summary>
        public float TotalDisplayOffTime => TotalPrintTime - TotalDisplayOnTime;

        /// <summary>
        /// Gets the total display off time as a formatted string
        /// </summary>
        public string DisplayTotalOffTimeString => TimeSpan.FromSeconds(TotalDisplayOffTime).ToTimeString();

        #endregion

        #region Constructor

        public Machine() { }

        public Machine(PrinterBrand brand, string model, ushort resolutionX, ushort resolutionY, float displayWidth, float displayHeight, float machineZ, FlipDirection displayMirror = default)
        {
            Brand = brand;
            Model = model;

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


        public Machine(PrinterBrand brand, string model, Size resolution, SizeF display, float machineZ, FlipDirection displayMirror = default)
            : this(brand, model, (ushort)resolution.Width, (ushort)resolution.Height, display.Width, display.Height, machineZ, displayMirror) { }


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
        [
                new(PrinterBrand.Anet, "N4", 1440, 2560, 68.04f, 120.96f, 135f, FlipDirection.Horizontally),
                new(PrinterBrand.Anet, "N7", 2560, 1600, 192f, 120f, 300f, FlipDirection.Horizontally),

                new(PrinterBrand.Anycubic, "Photon M3", 4096, 2560, 163.84f, 102.40f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon M3", 4096, 2560, 163.84f, 102.40f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon M3 Max", 6480, 3600, 298.08f, 165.60f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon M3 Plus", 5760, 3600, 198.15f, 123.84f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon M3 Premium", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M5", 11520, 5120, 218.88f, 122.88f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M5s", 11520, 5120, 218.88f, 122.88f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M5s Pro", 13312, 5120, 223.6416f, 126.976f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M7", 13312, 5120, 223.6416f, 126.976f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M7 Pro", 13312, 5120, 223.6416f, 126.976f, 230f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono M7 Max", 6480, 3600, 298.08f, 165.6f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono", 1620, 2560, 82.62f, 130.56f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 2", 4096, 2560, 143.36f, 89.60f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 4", 9024, 5120, 153.408f, 87.040f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 4 Ultra", 9024, 5120, 153.408f, 87.040f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono 4K", 3840, 2400, 134.40f, 84f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono SE", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono SQ", 2400, 2560, 120f, 128f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono X 6K", 5760, 3600, 198.15f, 123.84f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono X 6Ks", 5760, 3600, 195.84f, 122.40f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono X", 3840, 2400, 192f, 120f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Mono X2", 4096, 2560, 196.61f, 122.88f, 260f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon S", 1440, 2560, 68.04f, 120.96f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Ultra", 1280, 720, 102.4f, 57.6f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon D2", 2560, 1440, 130.56f, 73.44f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon X", 2560, 1600, 192f, 120f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Anycubic, "Photon Zero", 480, 854, 55.4f, 98.63f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Concepts3D, "Athena 8K", 7680, 4320, 218.88f, 123.12f, 245f, FlipDirection.Horizontally),
                new(PrinterBrand.Concepts3D, "Athena 12K", 11520, 5120, 218.88f, 122.88f, 245f, FlipDirection.Horizontally),

                new(PrinterBrand.Creality, "CT-005", 3840, 2400, 192f, 120f, 250f, FlipDirection.None),
                new(PrinterBrand.Creality, "CT133PRO", 3840, 2160, 293.76f, 165.24f, 300f, FlipDirection.None),
                new(PrinterBrand.Creality, "CL-89L", 3840, 2400, 192f, 120f, 200f, FlipDirection.None), // Halot Lite
                new(PrinterBrand.Creality, "CL-133", 3840, 2160, 293.76f, 165.24f, 300f, FlipDirection.None), // Halot Max
                new(PrinterBrand.Creality, "CL-60", 1620, 2560, 81f, 128f, 160f, FlipDirection.None), // Halot One
                new(PrinterBrand.Creality, "CL-79", 4320, 2560, 172.8f, 102.4f, 160f, FlipDirection.None), // Halot One Plus
                new(PrinterBrand.Creality, "CL-70", 2560, 2400, 130.56f, 122.4f, 160f, FlipDirection.None), // Halot One Pro
                new(PrinterBrand.Creality, "CL-89", 3840, 2400, 192f, 120f, 200f, FlipDirection.None), // Halot Sky
                new(PrinterBrand.Creality, "CL-92", 5760, 3600, 198.14f, 123.84f, 210f, FlipDirection.None), // Halot Sky Plus
                new(PrinterBrand.Creality, "CL925", 5760, 3600, 198.14f, 123.84f, 210f, FlipDirection.None), // Halot Ray
                new(PrinterBrand.Creality, "CL-103L", 7680, 4320, 228.096f, 128.304f, 230f, FlipDirection.Horizontally), //  Halot Mage
                new(PrinterBrand.Creality, "CL-103", 7680, 4320, 228.096f, 128.304f, 230f, FlipDirection.Horizontally), // Halot Mage Pro
                new(PrinterBrand.Creality, "LD-002H", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "LD-002R", 1440, 2560, 68.04f, 120.96f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Creality, "LD-006", 3840, 2400, 192f, 120f, 245f, FlipDirection.Horizontally),

                new(PrinterBrand.Elegoo, "Jupiter", 5448, 3064, 277.848f, 156.264f, 300f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 2 Pro", 1620, 2560, 82.62f, 130.56f, 160f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 2", 1620, 2560, 82.62f, 130.56f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 3", 4098, 2560, 143.43f, 89.6f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 3 Pro", 4098, 2560, 143.43f, 89.6f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 4", 8520, 4320, 153.36f, 77.76f, 175f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 4 DLP", 4098, 2560, 132.8f, 74.7f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 4 Max", 5760, 3600, 195.84f, 122.4f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 4 Ultra", 8520, 4320, 153.36f, 77.76f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 5", 4098, 2560, 143.43f, 89.6f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars 5 Ultra", 8520, 4320, 153.36f, 77.76f, 165f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Mars C", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 2", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 3", 11520, 5120, 218.88f, 122.88f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 3 Ultra", 11520, 5120, 218.88f, 122.88f, 260f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 4", 11520, 5120, 218.88f, 122.88f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 4 Ultra 12K", 11520, 5120, 218.88f, 122.88f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 4 Ultra 16K", 15120, 6230, 211.68f, 118.37f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn 8K", 7680, 4320, 218.88f, 123.12f, 210f, FlipDirection.Horizontally),
                new(PrinterBrand.Elegoo, "Saturn S", 4098, 2560, 196.704f, 122.88f, 210f, FlipDirection.Horizontally),

                new(PrinterBrand.EPAX, "DX1 PRO", 4098, 2560, 143.43f, 89.6f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "DX10 Pro 5K", 4920, 2880, 221.4f, 129.6f, 120f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "DX10 Pro 8K", 7680, 4320, 218.88f, 123.12f, 120f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "E10 5K", 4920, 2880, 221.4f, 129.6f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "E10 8K", 7680, 4320, 218.88f, 123.12f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "E10 Mono", 3840, 2400, 192f, 120f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "E6 Mono", 1620, 2560, 81f, 128f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X1 4KS", 4098, 2560, 143.43f, 89.6f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X1", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X10 4K Mono", 3840, 2400, 192f, 120f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X10 5K", 4920, 2880, 221.4f, 129.6f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X10", 1600, 2560, 135.36f, 216.57f, 250f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X133 4K Mono", 3840, 2160, 293.76f, 165.24f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X133 6K", 5760, 3240, 288f, 162f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X156 4K Color", 3840, 2160, 345.6f, 194.4f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.EPAX, "X1K 2K Mono", 1620, 2560, 82.62f, 130.56f, 155f, FlipDirection.Horizontally),

                new(PrinterBrand.FlashForge, "Explorer MAX", 2560, 1600, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Focus 13.3", 3842, 2171, 292f, 165f, 400f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Focus 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Foto 13.3", 3842, 2171, 292f, 165f, 400f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Foto 6.0", 2600, 1560, 130f, 78f, 155f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Foto 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Foto 8.9S", 3840, 2400, 192f, 120f, 200f, FlipDirection.Vertically),
                new(PrinterBrand.FlashForge, "Hunter", 1920, 1080, 120f, 67.5f, 150f, FlipDirection.Vertically),

                new(PrinterBrand.Emake3D, "Galaxy 1", 8000, 4000, 400f, 200f, 400f, FlipDirection.Horizontally),

                new(PrinterBrand.Kelant, "S400", 2560, 1600, 192f, 120f, 200f, FlipDirection.Horizontally),

                new(PrinterBrand.Longer, "Orange 10", 480, 854, 55.44f, 98.64f, 140f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Orange 120", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Orange 30", 1440, 2560, 68.04f, 120.96f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Longer, "Orange 4K", 3840, 6480, 120.96f, 68.04f, 190f, FlipDirection.Horizontally),

                new(PrinterBrand.Nova3D, "Bene4 Mono", 1566, 2549, 79.865f, 129.998f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Bene4", 1352, 2512, 70f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Bene5", 1566, 2549, 79.865f, 129.998f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Bene6", 4098, 2560, 143.43f, 89.6f, 180f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Elfin", 1410, 2531, 73f, 131f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Elfin2 Mono SE", 1470, 2549, 75f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Elfin2", 1352, 2512, 70f, 130f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Elfin3 Mini", 1079, 1904, 68f, 120f, 150f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Whale", 3840, 2400, 192f, 120f, 250f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Whale2", 3840, 2400, 192f, 120f, 250f, FlipDirection.Vertically),
                new(PrinterBrand.Nova3D, "Whale3 Pro", 7680, 4320, 228.096f, 128.304f, 260f, FlipDirection.Vertically),

                new(PrinterBrand.Peopoly, "Phenom L", 3840, 2160, 345.6f, 194.4f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Phenom Noir", 3840, 2160, 293.76f, 165.24f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Phenom XXL", 3840, 2160, 526.08f, 295.92f, 550f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Phenom XXL v2", 3840, 2160, 526.08f, 295.92f, 550f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Phenom", 3840, 2160, 276.48f, 155.52f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Peopoly, "Phenom Forge", 5760, 3240, 288f, 162f, 350f, FlipDirection.Horizontally),

                new(PrinterBrand.Phrozen, "Shuffle 16", 3840, 2160, 337.92f, 190.08f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Shuffle 4K", 2160, 3840, 68.04f, 120.96f, 170f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Shuffle Lite", 1440, 2560, 68.04f, 120.96f, 170f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Shuffle XL Lite", 2560, 1600, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Shuffle XL", 2560, 1600, 192f, 120f, 200f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Shuffle", 1440, 2560, 67.68f, 120.32f, 200f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Sonic 4K", 3840, 2160, 134.4f, 75.6f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mega 8K", 7680, 4320, 330.24f, 185.76f, 400f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mighty 4K", 3840, 2400, 199.68f, 124.8f, 220f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mighty 8K", 7680, 4320, 218.88f, 123.12f, 235f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mini 4K", 3840, 2160, 134.4f, 75.6f, 130f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mini 8K", 7500, 3240, 165f, 71.28f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mini 8K S", 7536, 3240, 165.792f, 71.28f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mini", 1080, 1920, 68.04f, 120.96f, 130f, FlipDirection.None),
                new(PrinterBrand.Phrozen, "Sonic", 1080, 1920, 68.04f, 120.96f, 170f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Sonic Mighty Revo", 13320, 5120, 223.776f, 126.976f, 235f, FlipDirection.Horizontally),
                new(PrinterBrand.Phrozen, "Transform", 3840, 2160, 291.84f, 164.16f, 400f, FlipDirection.None),

                new(PrinterBrand.QIDI, "I-Box Mono", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "S-Box", 1600, 2560, 135.36f, 216.576f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "Shadow5.5", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.QIDI, "Shadow6.0 Pro", 1440, 2560, 74.52f, 132.48f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Uniformation, "GKone", 4920, 2880, 221.4f, 129.6f, 245f, FlipDirection.Vertically),
                new(PrinterBrand.Uniformation, "GKtwo", 7680, 4320, 228.089f, 128.3f, 200f, FlipDirection.Vertically),

                new(PrinterBrand.Uniz, "IBEE", 3840, 2400, 192f, 120f, 220f, FlipDirection.Vertically),

                new(PrinterBrand.Prusa, "SL1", 1440, 2560, 68.04f, 120.96f, 150f, FlipDirection.Horizontally),
                new(PrinterBrand.Prusa, "SL1S SPEED", 1620, 2560, 128f, 81f, 150f, FlipDirection.Horizontally),

                new(PrinterBrand.Voxelab, "Ceres 8.9", 3840, 2400, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Polaris 5.5", 1440, 2560, 68.04f, 120.96f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Proxima 6", 1620, 2560, 82.62f, 130.56f, 155f, FlipDirection.Horizontally),
                new(PrinterBrand.Voxelab, "Proxima 6 SVGX", 2560, 1620, 130.56f, 82.62f, 155f, FlipDirection.Horizontally),

                new(PrinterBrand.Wanhao, "CGR Mini Mono", 1620, 2560, 82.62f, 130.56f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "CGR Mono", 1620, 2560, 192f, 120f, 200f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "D7", 2560, 1440, 120.96f, 68.5f, 180f, FlipDirection.Horizontally),
                new(PrinterBrand.Wanhao, "D8", 2560, 1600, 192f, 120f, 180f, FlipDirection.Horizontally),

                new(PrinterBrand.Zortrax, "Inkspire", 1440, 2560, 74.67f, 132.88f, 175f, FlipDirection.Horizontally)
        ];

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
                    //Name = filenameNoExt,
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
                sb.AppendLine($"new(PrinterBrand.{machine.Brand}, \"{machine.Model}\", {machine.ResolutionX}, {machine.ResolutionY}, {machine.DisplayWidth}f, {machine.DisplayHeight}f, {machine.MachineZ}f, FlipDirection.{machine.DisplayMirror}),");
            }

            return sb.ToString();
        }

        #endregion
    }
}
