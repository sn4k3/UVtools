/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;

namespace UVtools.Core
{
    public class Slicer
    {
        /// <summary>
        /// Gets the size of resolution
        /// </summary>
        public Size Resolution { get; private set; }

        /// <summary>
        /// Gets the size of display
        /// </summary>
        public SizeF Display { get; private set; }

        /// <summary>
        /// Gets the pixels per millimeters
        /// </summary>
        public SizeF Ppmm { get; private set; }

        public Slicer(Size resolution, SizeF display)
        {
            Init(resolution, display);
        }

        public void Init(Size resolution, SizeF display)
        {
            Resolution = resolution;
            Display = display;
            
            Ppmm = new SizeF(resolution.Width / display.Width, resolution.Height / display.Height);
        }

        public decimal MillimetersFromPixelsX(uint pixels) => (decimal) Math.Round(pixels / Ppmm.Width, 2);
        public decimal MillimetersFromPixelsY(uint pixels) => (decimal) Math.Round(pixels / Ppmm.Height, 2);
        public decimal MillimetersFromPixels (uint pixels) => (decimal) Math.Round(pixels / Math.Max(Ppmm.Width, Ppmm.Height), 2);

        public static decimal MillimetersFromPixelsX(Size resolution, SizeF display, uint pixels) => (decimal)Math.Round(pixels / (resolution.Width / display.Width), 2);
        public static decimal MillimetersFromPixelsY(Size resolution, SizeF display, uint pixels) => (decimal)Math.Round(pixels / (resolution.Height / display.Height), 2);



        public uint PixelsFromMillimetersX(decimal millimeters) => (uint) Math.Floor(millimeters * (decimal) Ppmm.Width);
        public uint PixelsFromMillimetersY(decimal millimeters) => (uint) Math.Floor(millimeters * (decimal) Ppmm.Height);
        public uint PixelsFromMillimeters (decimal millimeters) => (uint) Math.Floor(millimeters * (decimal) Math.Max(Ppmm.Width, Ppmm.Height));
        
        public static uint PixelsFromMillimetersX(Size resolution, SizeF display, decimal millimeters) => (uint)Math.Floor(resolution.Width / display.Width * (double) millimeters);
        public static uint PixelsFromMillimetersY(Size resolution, SizeF display, decimal millimeters) => (uint)Math.Floor(resolution.Height / display.Height * (double) millimeters);
    }
}
