/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateExposureFinder : Operation
    {
        #region Constants

        const byte TextSpacing = 60;
        const byte TextLineBreak = 30;
        const FontFace FontFace = Emgu.CV.CvEnum.FontFace.HersheyDuplex;
        const byte TextStartX = 10;
        //const byte TextStartY = 50;
        const double TextScale = 0.8;
        const byte TextThickness = 2;

        #endregion

        #region Members
        private decimal _displayWidth;
        private decimal _displayHeight;
        private decimal _layerHeight = 0.05M;
        private ushort _bottomLayers = 3;
        private decimal _bottomExposure = 60;
        private decimal _normalExposure = 12;
        private decimal _topBottomMargin = 5;
        private decimal _leftRightMargin = 5;
        private byte _chamferLayers = 0;
        private byte _erodeBottomIterations = 0;
        private decimal _partMargin = 0;
        private bool _enableAntiAliasing = true;
        private bool _mirrorOutput;
        private decimal _baseHeight = 1;
        private decimal _holeHeight = 1;
        private decimal _holeMargin = 1.5m;
        private Measures _unitOfMeasure = Measures.Pixels;
        private string _holeDiametersMm = "0.1, 0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0";
        private string _holeDiametersPx = "2, 3, 4, 5, 6, 7, 8, 9, 10";
        private bool _multipleLayerHeight;
        private decimal _multipleLayerHeightMaximum = 0.1m;
        private decimal _multipleLayerHeightStep = 0.01m;
        private bool _multipleExposures;
        private ExposureGenTypes _exposureGenType = ExposureGenTypes.Linear;
        private bool _exposureGenIgnoreBaseExposure;
        private decimal _exposureGenBottomStep = 0.5m;
        private decimal _exposureGenNormalStep = 0.2m;
        private byte _exposureGenTests = 4;
        private decimal _exposureGenManualLayerHeight;
        private decimal _exposureGenManualBottom;
        private decimal _exposureGenManualNormal;
        private ObservableCollection<ExposureItem> _exposureTable = new();
        private CalibrateExposureFinderShapes _holeShape = CalibrateExposureFinderShapes.Square;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        //public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Exposure time finder";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the best exposure time for a given layer height.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the exposure time finder test?";

        public override string ProgressTitle =>
            $"Generating the exposure time finder test";

        public override string ProgressAction => "Generated layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (_displayWidth <= 0)
            {
                sb.AppendLine("Display width must be a positive value.");
            }

            if (_displayHeight <= 0)
            {
                sb.AppendLine("Display height must be a positive value.");
            }

            if (Holes.Length <= 0)
            {
                sb.AppendLine("No objects to output.");
            }

            if (_multipleExposures)
            {
                var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
                for (decimal layerHeight = _layerHeight;
                    layerHeight <= endLayerHeight;
                    layerHeight += _multipleLayerHeightStep)
                {
                    bool found = false;
                    foreach (var exposureItem in _exposureTable)
                    {
                        if (exposureItem.LayerHeight == layerHeight && exposureItem.IsValid)
                        {
                            found = true;
                            break;
                        }
                    }
                    if(!found)
                        sb.AppendLine($"[ME]: {layerHeight:F2}mm layer height have no exposure(s).");
                }
            }

            return new StringTag(sb.ToString());
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Bottom layers: {_bottomLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[TB:{_topBottomMargin} LR:{_leftRightMargin} PM:{_partMargin} CM:{_holeMargin}]  " +
                         $"[Chamfer: {_chamferLayers}] [Erode: {_erodeBottomIterations}] " +
                         $"[Obj height: {_holeHeight}] " +
                         $"[Holes: {Holes.Length}] " +
                         $"[AA: {_enableAntiAliasing}] [Mirror: {_mirrorOutput}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        public decimal DisplayWidth
        {
            get => _displayWidth;
            set
            {
                if(!RaiseAndSetIfChanged(ref _displayWidth, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(Xppmm));
            }
        }

        public decimal DisplayHeight
        {
            get => _displayHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _displayHeight, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(Yppmm));
            }
        }

        public decimal Xppmm => DisplayWidth > 0 ? Math.Round(SlicerFile.Resolution.Width / DisplayWidth, 2) : 0;
        public decimal Yppmm => DisplayHeight > 0 ? Math.Round(SlicerFile.Resolution.Height / DisplayHeight, 2) : 0;
        public decimal Ppmm => Math.Max(Xppmm, Yppmm);

        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(BottomLayersMM));
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public ushort Microns => (ushort)(LayerHeight * 1000);

        public ushort BottomLayers
        {
            get => _bottomLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomLayers, value)) return;
                RaisePropertyChanged(nameof(BottomLayersMM));
            }
        }

        public decimal BottomLayersMM => Math.Round(LayerHeight * BottomLayers, 2);

        public decimal BottomExposure
        {
            get => _bottomExposure;
            set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
        }

        public decimal NormalExposure
        {
            get => _normalExposure;
            set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
        }

        public decimal TopBottomMargin
        {
            get => _topBottomMargin;
            set => RaiseAndSetIfChanged(ref _topBottomMargin, Math.Round(value, 2));
        }

        public decimal LeftRightMargin
        {
            get => _leftRightMargin;
            set => RaiseAndSetIfChanged(ref _leftRightMargin, Math.Round(value, 2));
        }

        public byte ChamferLayers
        {
            get => _chamferLayers;
            set => RaiseAndSetIfChanged(ref _chamferLayers, value);
        }

        public byte ErodeBottomIterations
        {
            get => _erodeBottomIterations;
            set => RaiseAndSetIfChanged(ref _erodeBottomIterations, value);
        }

        public decimal PartMargin
        {
            get => _partMargin;
            set => RaiseAndSetIfChanged(ref _partMargin, Math.Round(value, 2));
        }

        public bool EnableAntiAliasing
        {
            get => _enableAntiAliasing;
            set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
        }

        public bool MirrorOutput
        {
            get => _mirrorOutput;
            set => RaiseAndSetIfChanged(ref _mirrorOutput, value);
        }

        public decimal BaseHeight
        {
            get => _baseHeight;
            set => RaiseAndSetIfChanged(ref _baseHeight, Math.Round(value, 2));
        }

        public decimal HoleHeight
        {
            get => _holeHeight;
            set => RaiseAndSetIfChanged(ref _holeHeight, Math.Round(value, 2));
        }

        public decimal HoleMargin
        {
            get => _holeMargin;
            set => RaiseAndSetIfChanged(ref _holeMargin, Math.Round(value, 2));
        }

        public decimal TotalHeight => BaseHeight + HoleHeight;

        public CalibrateExposureFinderShapes HoleShape
        {
            get => _holeShape;
            set => RaiseAndSetIfChanged(ref _holeShape, value);
        }

        public Measures UnitOfMeasure
        {
            get => _unitOfMeasure;
            set
            {
                if (!RaiseAndSetIfChanged(ref _unitOfMeasure, value)) return;
                RaisePropertyChanged(nameof(IsUnitOfMeasureMm));
            }
        }

        public bool IsUnitOfMeasureMm => _unitOfMeasure == Measures.Millimeters;

        public string HoleDiametersMm
        {
            get => _holeDiametersMm;
            set => RaiseAndSetIfChanged(ref _holeDiametersMm, value);
        }

        public string HoleDiametersPx
        {
            get => _holeDiametersPx;
            set => RaiseAndSetIfChanged(ref _holeDiametersPx, value);
        }

        /// <summary>
        /// Gets all holes in pixels and ordered
        /// </summary>
        public int[] Holes
        {
            get
            {
                List<int> holes = new();

                if (_unitOfMeasure == Measures.Millimeters)
                {
                    var split = _holeDiametersMm.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var mmStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(mmStr)) continue;
                        if (!decimal.TryParse(mmStr, out var mm)) continue;
                        if(mm <= 0) continue;
                        var mmPx = (int) Math.Floor(mm * Ppmm);
                        if(holes.Contains(mmPx) || mmPx > 500) continue;
                        holes.Add((int)Math.Floor(mm * Ppmm));
                    }
                }
                else
                {
                    var split = _holeDiametersPx.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var pxStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(pxStr)) continue;
                        if (!int.TryParse(pxStr, out var px)) continue;
                        if (px <= 0) continue;
                        if (holes.Contains(px) || px > 500) continue;
                        holes.Add(px);
                    }
                }

                return holes.OrderBy(pixels => pixels).ToArray();
            }
        }

        public int GetHolesLength(int[] holes)
        {
            return (int) (holes.Sum() + (holes.Length-1) * _holeMargin * Yppmm);
        }

        public bool MultipleLayerHeight
        {
            get => _multipleLayerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeight, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public decimal MultipleLayerHeightMaximum
        {
            get => _multipleLayerHeightMaximum;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeightMaximum, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public decimal MultipleLayerHeightStep
        {
            get => _multipleLayerHeightStep;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeightStep, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public bool MultipleExposures
        {
            get => _multipleExposures;
            set => RaiseAndSetIfChanged(ref _multipleExposures, value);
        }

        public ExposureGenTypes ExposureGenType
        {
            get => _exposureGenType;
            set => RaiseAndSetIfChanged(ref _exposureGenType, value);
        }

        public bool ExposureGenIgnoreBaseExposure
        {
            get => _exposureGenIgnoreBaseExposure;
            set => RaiseAndSetIfChanged(ref _exposureGenIgnoreBaseExposure, value);
        }

        public decimal ExposureGenBottomStep
        {
            get => _exposureGenBottomStep;
            set => RaiseAndSetIfChanged(ref _exposureGenBottomStep, Math.Round(value, 2));
        }

        public decimal ExposureGenNormalStep
        {
            get => _exposureGenNormalStep;
            set => RaiseAndSetIfChanged(ref _exposureGenNormalStep, Math.Round(value, 2));
        }

        public byte ExposureGenTests
        {
            get => _exposureGenTests;
            set => RaiseAndSetIfChanged(ref _exposureGenTests, value);
        }

        public decimal ExposureGenManualLayerHeight
        {
            get => _exposureGenManualLayerHeight;
            set => RaiseAndSetIfChanged(ref _exposureGenManualLayerHeight, value);
        }

        public decimal[] AvailableLayerHeights
        {
            get
            {
                List<decimal> layerHeights = new List<decimal>();
                var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
                List<ExposureItem> list = new();
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += _multipleLayerHeightStep)
                {
                    layerHeights.Add(Math.Round(layerHeight, 2));
                }

                return layerHeights.ToArray();
            }
        }

        public decimal ExposureGenManualBottom
        {
            get => _exposureGenManualBottom;
            set => RaiseAndSetIfChanged(ref _exposureGenManualBottom, value);
        }

        public decimal ExposureGenManualNormal
        {
            get => _exposureGenManualNormal;
            set => RaiseAndSetIfChanged(ref _exposureGenManualNormal, value);
        }

        public ExposureItem ExposureManualEntry => new (_exposureGenManualLayerHeight, _exposureGenManualBottom, _exposureGenManualNormal);


        public ObservableCollection<ExposureItem> ExposureTable
        {
            get => _exposureTable;
            set => RaiseAndSetIfChanged(ref _exposureTable, value);
        }

        #endregion

        #region Constructor

        public OperationCalibrateExposureFinder() { }

        public OperationCalibrateExposureFinder(FileFormat slicerFile) : base(slicerFile)
        {
            _layerHeight    = (decimal)slicerFile.LayerHeight;
            _bottomLayers   = slicerFile.BottomLayerCount;
            _bottomExposure = (decimal)slicerFile.BottomExposureTime;
            _normalExposure = (decimal)slicerFile.ExposureTime;
            _mirrorOutput   = slicerFile.MirrorDisplay;
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if (SlicerFile.DisplayWidth > 0)
                DisplayWidth = (decimal)SlicerFile.DisplayWidth;
            if (SlicerFile.DisplayHeight > 0)
                DisplayHeight = (decimal)SlicerFile.DisplayHeight;

            if (_exposureGenManualBottom == 0)
                _exposureGenManualBottom = (decimal) SlicerFile.BottomExposureTime;
            if (_exposureGenManualNormal == 0)
                _exposureGenManualNormal = (decimal)SlicerFile.ExposureTime;

            if (!SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.ExposureSeconds))
            {
                _multipleLayerHeight = false;
                _multipleExposures = false;
            }
        }

        #endregion

        #region Enums

        public enum CalibrateExposureFinderShapes : byte
        {
            Square,
            Circle
        }
        public static Array ShapesItems => Enum.GetValues(typeof(CalibrateExposureFinderShapes));

        public enum Measures : byte
        {
            Pixels,
            Millimeters,
        }

        public static Array MeasuresItems => Enum.GetValues(typeof(Measures));

        public enum ExposureGenTypes : byte
        {
            Linear,
            Multiplier
        }

        public static Array ExposureGenTypeItems => Enum.GetValues(typeof(ExposureGenTypes));
        #endregion

        #region Equality

        private bool Equals(OperationCalibrateExposureFinder other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && _chamferLayers == other._chamferLayers && _erodeBottomIterations == other._erodeBottomIterations && _partMargin == other._partMargin && _enableAntiAliasing == other._enableAntiAliasing && _mirrorOutput == other._mirrorOutput && _baseHeight == other._baseHeight && _holeHeight == other._holeHeight && _holeMargin == other._holeMargin && _holeShape == other._holeShape && _unitOfMeasure == other._unitOfMeasure && _holeDiametersMm == other._holeDiametersMm && _holeDiametersPx == other._holeDiametersPx && _multipleLayerHeight == other._multipleLayerHeight && _multipleLayerHeightMaximum == other._multipleLayerHeightMaximum && _multipleLayerHeightStep == other._multipleLayerHeightStep && _multipleExposures == other._multipleExposures && _exposureGenType == other._exposureGenType && _exposureGenIgnoreBaseExposure == other._exposureGenIgnoreBaseExposure && _exposureGenBottomStep == other._exposureGenBottomStep && _exposureGenNormalStep == other._exposureGenNormalStep && _exposureGenTests == other._exposureGenTests && Equals(_exposureTable, other._exposureTable);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateExposureFinder other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_topBottomMargin);
            hashCode.Add(_leftRightMargin);
            hashCode.Add(_chamferLayers);
            hashCode.Add(_erodeBottomIterations);
            hashCode.Add(_partMargin);
            hashCode.Add(_enableAntiAliasing);
            hashCode.Add(_mirrorOutput);
            hashCode.Add(_baseHeight);
            hashCode.Add(_holeHeight);
            hashCode.Add(_holeMargin);
            hashCode.Add((int) _holeShape);
            hashCode.Add((int) _unitOfMeasure);
            hashCode.Add(_holeDiametersMm);
            hashCode.Add(_holeDiametersPx);
            hashCode.Add(_multipleLayerHeight);
            hashCode.Add(_multipleLayerHeightMaximum);
            hashCode.Add(_multipleLayerHeightStep);
            hashCode.Add(_multipleExposures);
            hashCode.Add((int) _exposureGenType);
            hashCode.Add(_exposureGenIgnoreBaseExposure);
            hashCode.Add(_exposureGenBottomStep);
            hashCode.Add(_exposureGenNormalStep);
            hashCode.Add(_exposureGenTests);
            hashCode.Add(_exposureTable);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        public void SortExposureTable()
        {
            var list = _exposureTable.ToList();
            list.Sort();
            ExposureTable = new(list);
        }

        public void SanitizeExposureTable()
        {
            List<ExposureItem> list = _exposureTable.ToList().Distinct().ToList();
            list.Sort();
            ExposureTable = new(list);
        }

        public void GenerateExposure()
        {
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
            List<ExposureItem> list = new();
            for (decimal layerHeight = _layerHeight;
                layerHeight <= endLayerHeight;
                layerHeight += _multipleLayerHeightStep)
            {
                if(!_exposureGenIgnoreBaseExposure)
                    list.Add(new ExposureItem(layerHeight, (decimal) SlicerFile.BottomExposureTime, (decimal) SlicerFile.ExposureTime));
                for (ushort testN = 1; testN <= _exposureGenTests; testN++)
                {
                    decimal bottomExposureTime = 0;
                    decimal exposureTime = 0;

                    switch (_exposureGenType)
                    {
                        case ExposureGenTypes.Linear:
                            bottomExposureTime = (decimal) SlicerFile.BottomExposureTime + _exposureGenBottomStep * testN; 
                            exposureTime = (decimal) SlicerFile.ExposureTime + _exposureGenNormalStep * testN; 
                            break;
                        case ExposureGenTypes.Multiplier:
                            bottomExposureTime = (decimal)SlicerFile.BottomExposureTime + (decimal)SlicerFile.BottomExposureTime * layerHeight * _exposureGenBottomStep * testN;
                            exposureTime = (decimal)SlicerFile.ExposureTime + (decimal)SlicerFile.ExposureTime * layerHeight * _exposureGenNormalStep * testN;
                            break;
                    }

                    ExposureItem item = new(layerHeight, bottomExposureTime, exposureTime);
                    if(list.Contains(item)) continue; // Already on list, skip
                    list.Add(item);
                }
            }

            ExposureTable = new(list);
        }

        public Mat[] GetLayers()
        {
            var holes = Holes;
            int holeMarginX = (int)(Xppmm * _holeMargin);
            int holeMarginY = (int)(Yppmm * _holeMargin);
            Rectangle rect = new Rectangle(new Point(1, 1),
                new Size(holeMarginX * 4 + holes[^1] * 2,
                    holeMarginY * 3 + GetHolesLength(holes) + TextSpacing));
            var layers = new Mat[2];
            layers[0] = EmguExtensions.InitMat(rect.Size.Inflate(2));

            CvInvoke.Rectangle(layers[0], rect, EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            layers[1] = layers[0].Clone();
            CvInvoke.Rectangle(layers[1], new Rectangle(0, 0, rect.Size.Width / 2, layers[0].Height), EmguExtensions.BlackByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            //int holeXPos = (int) Math.Round(Xppmm * (CylinderMargin + maxCylinder / 2));
            // Print holes
            int holeXPos = 0;
            int holeYPos = 0;
            for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                var layer = layers[layerIndex];
                holeYPos = holeMarginY;
                for (int i = 0; i < holes.Length; i++)
                {
                    var diameter = holes[i];
                    var radius = diameter / 2;
                    holeXPos = rect.X + holeMarginX + holes[^1] - diameter;

                    CalibrateExposureFinderShapes effectiveShape = _holeShape == CalibrateExposureFinderShapes.Square || diameter < 6 ?
                        CalibrateExposureFinderShapes.Square : CalibrateExposureFinderShapes.Circle;

                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            holeXPos = rect.X + holeMarginX + holes[^1] - diameter;
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            holeXPos = rect.X + holeMarginX + holes[^1] - radius;
                            holeYPos += radius;
                            break;
                    }


                    // Left side
                    if (layerIndex == 1)
                    {
                        if (diameter == 1)
                        {
                            layer.SetByte(holeXPos, holeYPos, 255);
                        }
                        else
                        {
                            switch (effectiveShape)
                            {
                                case CalibrateExposureFinderShapes.Square:
                                    CvInvoke.Rectangle(layers[layerIndex],
                                        new Rectangle(new Point(holeXPos, holeYPos), new Size(diameter-1, diameter-1)),
                                        EmguExtensions.WhiteByte, -1,
                                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                    break;
                                case CalibrateExposureFinderShapes.Circle:
                                    CvInvoke.Circle(layers[layerIndex],
                                        new Point(holeXPos, holeYPos),
                                        radius, EmguExtensions.WhiteByte, -1,
                                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                    break;
                            }

                        }
                    }

                    //holeXPos = layers[0].Width - holeXPos;
                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            holeXPos = layers[0].Width - rect.X - holeMarginX - holes[^1];
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            holeXPos = layers[0].Width - rect.X - holeMarginX - holes[^1] + radius;
                            break;
                    }

                    // Right side
                    if (diameter == 1)
                    {
                        layer.SetByte(holeXPos, holeYPos, 0);
                    }
                    else
                    {
                        switch (effectiveShape)
                        {
                            case CalibrateExposureFinderShapes.Square:
                                CvInvoke.Rectangle(layers[layerIndex],
                                    new Rectangle(new Point(holeXPos, holeYPos), new Size(diameter-1, diameter-1)),
                                    EmguExtensions.BlackByte, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                            case CalibrateExposureFinderShapes.Circle:
                                CvInvoke.Circle(layers[layerIndex],
                                    new Point(holeXPos, holeYPos),
                                    radius, EmguExtensions.BlackByte, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                        }
                    }


                    holeYPos += holeMarginY;

                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            holeYPos += diameter;
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            holeYPos += radius;
                            break;
                    }
                }
            }


            /*if (_mirrorOutput)
            {
                Parallel.ForEach(layers, mat => CvInvoke.Flip(mat, mat, FlipType.Horizontal));
            }*/

            return layers;
        }

        public Mat GetThumbnail()
        {
            Mat thumbnail = EmguExtensions.InitMat(new Size(400, 200), 3);
            var fontFace = FontFace.HersheyDuplex;
            var fontScale = 1;
            var fontThickness = 2;
            const byte xSpacing = 45;
            const byte ySpacing = 45;
            CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
            CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.PutText(thumbnail, "Exposure Time Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);
            CvInvoke.PutText(thumbnail, $"Holes: {Holes.Length}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);

            return thumbnail;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.ItemCount = 0;
            SanitizeExposureTable();
            var layers = GetLayers();
            if (layers[0].Width > SlicerFile.ResolutionX || layers[0].Height > SlicerFile.ResolutionY)
            {
                return false;
            }

            List<Layer> newLayers = new();

            Dictionary<ExposureItem, Point> table = new(); 
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
            var totalHeight = TotalHeight;
            int sideMarginPx = (int)Math.Floor(_leftRightMargin * Xppmm);
            int topBottomMarginPx = (int)Math.Floor(_topBottomMargin * Yppmm);
            uint layerIndex = 0;
            int currentX = sideMarginPx;
            int currentY = topBottomMarginPx;
            int holeMarginY = (int)(Yppmm * _holeMargin);

            var anchor = new Point(-1, -1);
            using var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            void AddLayer(decimal currentHeight, decimal layerHeight, decimal bottomExposure, decimal normalExposure)
            {
                var layerDifference = currentHeight / layerHeight;

                if (!layerDifference.IsInteger()) return; // Not at right height to process with layer height
                                                            //Debug.WriteLine($"{currentHeight} / {layerHeight} = {layerDifference}, Floor={Math.Floor(layerDifference)}");

                Point position;
                ExposureItem key = new(layerHeight, bottomExposure, normalExposure);

                if (table.TryGetValue(key, out var pos))
                {
                    position = pos;
                }
                else
                {
                    if (currentX + layers[0].Width + sideMarginPx > SlicerFile.ResolutionX)
                    {
                        currentX = sideMarginPx;
                        currentY += layers[0].Height + (int)Math.Floor(_partMargin * Yppmm);
                    }

                    if (currentY + layers[0].Height + topBottomMarginPx > SlicerFile.ResolutionY)
                    {
                        return; // Reach the end
                    }

                    position = new Point(currentX, currentY);
                    table.Add(key, new Point(currentX, currentY));

                    currentX += layers[0].Width + (int)Math.Floor(_partMargin * Xppmm);
                }

                ushort microns = (ushort)Math.Floor(layerHeight * 1000);

                Mat mat = EmguExtensions.InitMat(SlicerFile.Resolution);
                Mat matRoi = new(mat, new Rectangle(position, layers[0].Size));

                int layerCountOnHeight = (int)Math.Floor(currentHeight / layerHeight);
                bool isBottomLayer = layerCountOnHeight <= _bottomLayers;
                bool isBaseLayer = currentHeight <= _baseHeight;
                layers[isBaseLayer ? 0 : 1].CopyTo(matRoi);

                if (isBottomLayer && _erodeBottomIterations > 0)
                {
                    CvInvoke.Erode(matRoi, matRoi, kernel, anchor, _erodeBottomIterations, BorderType.Reflect101, default);
                }

                if (layerCountOnHeight < _chamferLayers)
                {
                    CvInvoke.Erode(matRoi, matRoi, kernel, anchor, _chamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
                }

                var textHeightStart = matRoi.Height - holeMarginY - TextSpacing;
                CvInvoke.PutText(matRoi, $"{microns}u", new Point(TextStartX, textHeightStart), FontFace, TextScale, EmguExtensions.WhiteByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.PutText(matRoi, $"{bottomExposure}s", new Point(TextStartX, textHeightStart + TextLineBreak), FontFace, TextScale, EmguExtensions.WhiteByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.PutText(matRoi, $"{normalExposure}s", new Point(TextStartX, textHeightStart + TextLineBreak * 2), FontFace, TextScale, EmguExtensions.WhiteByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.PutText(matRoi, $"{microns}u", new Point(matRoi.Width / 2 + TextStartX, textHeightStart), FontFace, TextScale, EmguExtensions.BlackByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.PutText(matRoi, $"{bottomExposure}s", new Point(matRoi.Width / 2 + TextStartX, textHeightStart + TextLineBreak), FontFace, TextScale, EmguExtensions.BlackByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.PutText(matRoi, $"{normalExposure}s", new Point(matRoi.Width / 2 + TextStartX, textHeightStart + TextLineBreak * 2), FontFace, TextScale, EmguExtensions.BlackByte, TextThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                Layer layer = new(layerIndex++, mat, SlicerFile)
                {
                    PositionZ = (float)currentHeight,
                    ExposureTime = isBottomLayer ? (float)bottomExposure : (float)normalExposure,
                    LiftHeight = isBottomLayer ? SlicerFile.BottomLiftHeight : SlicerFile.LiftHeight,
                    LiftSpeed = isBottomLayer ? SlicerFile.BottomLiftSpeed : SlicerFile.LiftSpeed,
                    RetractSpeed = SlicerFile.RetractSpeed,
                    LightOffDelay = isBottomLayer ? SlicerFile.BottomLightOffDelay : SlicerFile.LightOffDelay,
                    LightPWM = isBottomLayer ? SlicerFile.BottomLightPWM : SlicerFile.LightPWM,
                    IsModified = true
                };
                newLayers.Add(layer);
                mat.Dispose();

                progress++;
            }
            
            for (decimal currentHeight = _layerHeight; currentHeight <= totalHeight; currentHeight += 0.01m)
            {
                currentHeight = Math.Round(currentHeight, 2);
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += _multipleLayerHeightStep)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    layerHeight = Math.Round(layerHeight, 2);
                    
                    if (_multipleExposures)
                    {
                        foreach (var exposureItem in _exposureTable)
                        {
                            if (exposureItem.IsValid && exposureItem.LayerHeight == layerHeight)
                            {
                                AddLayer(currentHeight, layerHeight, exposureItem.BottomExposure, exposureItem.Exposure);
                            }
                        }
                    }
                    else
                    {
                        AddLayer(currentHeight, layerHeight, _bottomExposure, _normalExposure);
                    }
                }
            }

            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            SlicerFile.SuppressRebuildProperties = true;
            SlicerFile.LayerHeight = (float)LayerHeight;
            SlicerFile.BottomExposureTime = (float)BottomExposure;
            SlicerFile.ExposureTime = (float)NormalExposure;
            SlicerFile.BottomLayerCount = BottomLayers;
            SlicerFile.LayerManager.Layers = newLayers.ToArray();
            SlicerFile.SuppressRebuildProperties = false;

            if (_mirrorOutput)
            {
                new OperationFlip(SlicerFile){FlipDirection = Enumerations.FlipDirection.Horizontally}.Execute(progress);
            }

            new OperationMove(SlicerFile).Execute(progress);

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
