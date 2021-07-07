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
using System.IO;
using System.Linq;
using Org.BouncyCastle.Asn1.X509;
using QuantumConcepts.Formats.StereoLithography;

namespace UVtools.Core.Slicer
{
    public class Slicer
    {
        private float _lowX = float.NaN;
        private float _highX = float.NaN;
        private float _lowY = float.NaN;
        private float _highY = float.NaN;
        private float _lowZ = float.NaN;
        private float _highZ = float.NaN;
        private readonly STLDocument _stl;
        protected Dictionary<float, Slice> _slices = new ();

        private STLDocument Stl => _stl;

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

        public float LowX
        {
            get
            {
                if(_lowX == float.NaN)
                    _lowX = RangeUtils.CalculateLowX(_stl);

                return _lowX;
            }
        }

        public float HighX
        {
            get
            {
                if (_highX == float.NaN)
                    _highX = RangeUtils.CalculateHighX(_stl);

                return _highX;
            }
        }

        public float LowY
        {
            get
            {
                if (_lowY == float.NaN)
                    _lowY = RangeUtils.CalculateLowY(_stl);

                return _lowY;
            }
        }

        public float HighY
        {
            get
            {
                if (_highY == float.NaN)
                    _highY = RangeUtils.CalculateHighY(_stl);

                return _highY;
            }
        }

        public float LowZ
        {
            get
            {
                if (_lowZ == float.NaN)
                    _lowZ = RangeUtils.CalculateLowZ(_stl);

                return _lowZ;
            }
        }

        public float HighZ
        {
            get
            {
                if (_highZ == float.NaN)
                    _highZ = RangeUtils.CalculateHighZ(_stl);

                return _highZ;
            }
        }

        public Slicer(Size resolution, SizeF display)
        {
            Init(resolution, display);
        }

        public Slicer(Size resolution, SizeF display, STLDocument stl) : this(resolution, display)
        {
            _stl = stl;
        }

        public Slicer(Size resolution, SizeF display, string stlPath) : this(resolution, display)
        {
            _stl = STLDocument.Open(stlPath);

            if (_stl is null)
                throw new FileNotFoundException(null, stlPath);
        }

        public void Init(Size resolution, SizeF display)
        {
            Resolution = resolution;
            Display = display;
            
            Ppmm = new SizeF(resolution.Width / display.Width, resolution.Height / display.Height);
        }

        #region Slice Methods
        public Slice GetSliceAtZIndex(float z)
        {
            // cache this in the event you have to retrieve a single value more than once,
            // we don't want to have to do this math again
            if (!_slices.ContainsKey(z))
            {
                var facets = LinAlgUtils.FindFacetsIntersectingZIndex(_stl, z);
                _slices[z] = new Slice();
                _slices[z].AddRange(facets.Select(f => LinAlgUtils.CreateLineFromFacetAtZIndex(f, z)));
            }

            return _slices[z];
        }

        public Dictionary<float, Slice> SliceModel(float layerHeight)
        {
            /*float volume = 0;
            _stl.Facets.ForEach(facet =>
            {
                var v1 = facet.Vertices[0];
                var v2 = facet.Vertices[1];
                var v3 = facet.Vertices[2];

                volume += (
                     -(v3.X * v2.Y * v1.Z)
                    + (v2.X * v3.Y * v1.Z)
                    + (v3.X * v1.Y * v2.Z)
                    - (v1.X * v3.Y * v2.Z)
                    - (v2.X * v1.Y * v3.Z)
                    + (v1.X * v2.Y * v3.Z)
                ) / 6;
            });*/
            var newDict = new Dictionary<float, Slice>();
            
            for (var z = Layer.RoundHeight(LowZ); z <= HighZ; z = Layer.RoundHeight(z+layerHeight))
            {
                if (_slices.Keys.Contains(z))
                    newDict[z] = _slices[z];
                else
                {
                    newDict[z] = GetSliceAtZIndex(z);
                    _slices[z] = newDict[z];
                }
            }
            return newDict;
        }

        public void SliceModel2()
        {

        }
        #endregion

        public decimal MillimetersFromPixelsX(uint pixels) => (decimal) Math.Round(pixels / Ppmm.Width, 2);
        public decimal MillimetersFromPixelsY(uint pixels) => (decimal) Math.Round(pixels / Ppmm.Height, 2);
        public decimal MillimetersFromPixels (uint pixels) => (decimal) Math.Round(pixels / Math.Max(Ppmm.Width, Ppmm.Height), 2);

        public static decimal MillimetersFromPixelsX(Size resolution, SizeF display, uint pixels) => (decimal)Math.Round(pixels / (resolution.Width / display.Width), 2);
        public static decimal MillimetersFromPixelsY(Size resolution, SizeF display, uint pixels) => (decimal)Math.Round(pixels / (resolution.Height / display.Height), 2);



        public uint PixelsFromMillimetersX(decimal millimeters) => (uint)(millimeters * (decimal) Ppmm.Width);
        public uint PixelsFromMillimetersY(decimal millimeters) => (uint)(millimeters * (decimal) Ppmm.Height);
        public uint PixelsFromMillimeters (decimal millimeters) => (uint)(millimeters * (decimal) Math.Max(Ppmm.Width, Ppmm.Height));
        
        public static uint PixelsFromMillimetersX(Size resolution, SizeF display, decimal millimeters) => (uint)(resolution.Width / display.Width * (double) millimeters);
        public static uint PixelsFromMillimetersY(Size resolution, SizeF display, decimal millimeters) => (uint)(resolution.Height / display.Height * (double) millimeters);

        public static uint MillimetersToLayers(float millimeters, float layerHeight) => (uint)(millimeters / layerHeight);
        public static uint MillimetersToLayers(double millimeters, double layerHeight) => (uint)(millimeters / layerHeight);
        public static uint MillimetersToLayers(decimal millimeters, decimal layerHeight) => (uint)(millimeters / layerHeight);

        public static uint LayersToMillimeters(uint layers, float layerHeight) => (uint)(layers * layerHeight);
        public static uint LayersToMillimeters(uint layers, double layerHeight) => (uint)(layers * layerHeight);
        public static uint LayersToMillimeters(uint layers, decimal layerHeight) => (uint)(layers * layerHeight);
    }
}
