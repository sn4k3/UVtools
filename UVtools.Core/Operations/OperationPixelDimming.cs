/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationPixelDimming : Operation
    {
        #region Subclasses
        class StringMatrix
        {
            public string Text { get; }
            public Matrix<byte> Pattern { get; set; }

            public StringMatrix(string text)
            {
                Text = text;
            }
        }
        #endregion

        #region Members
        private uint _wallThicknessStart = 10;
        private uint _wallThicknessEnd = 10;
        private bool _wallsOnly;
        private bool _chamfer;
        private Matrix<byte> _pattern;
        private Matrix<byte> _alternatePattern;
        private ushort _alternatePatternPerLayers = 1;
        private string _patternText;
        private string _alternatePatternText;
        private byte _brightness = 127;
        private ushort _infillGenThickness = 10;
        private ushort _infillGenSpacing = 20;
        #endregion

        #region Overrides
        public override string Title => "Pixel dimming";
        public override string Description =>
            "Dim white pixels in a chosen pattern applied over the print area.\n\n" +
            "The selected pattern will tiled over the image.  Benefits are:\n" +
            "1) Reduced layer expansion for large layer objects\n" +
            "2) Reduced cross layer exposure\n" +
            "3) Extended pixel life of the LCD\n\n" +
            "NOTE: Run this tool only after repairs and all other transformations.\n" +
            "To create your own patterns: www.piskelapp.com";

        public override string ConfirmationText =>
            $"dim pixels from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Dimming from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Dimmed layers";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();
            /*if (WallThicknessStart == 0 && WallsOnly)
            {
                sb.AppendLine("Border size must be positive in order to use \"Dim only borders\" function.");
            }*/

            var stringMatrix = new[]
            {
                new StringMatrix(PatternText),
                new StringMatrix(AlternatePatternText),
            };

            foreach (var item in stringMatrix)
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;
                var lines = item.Text.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                for (var row = 0; row < lines.Length; row++)
                {
                    var bytes = lines[row].Split(' ');
                    if (row == 0)
                    {
                        item.Pattern = new Matrix<byte>(lines.Length, bytes.Length);
                    }
                    else
                    {
                        if (item.Pattern.Cols != bytes.Length)
                        {
                            sb.AppendLine($"Row {row + 1} have invalid number of pixels, the pattern must have equal pixel count per line, per defined on line 1");
                            return sb.ToString();
                        }
                    }

                    for (int col = 0; col < bytes.Length; col++)
                    {
                        if (byte.TryParse(bytes[col], out var value))
                        {
                            item.Pattern[row, col] = value;
                        }
                        else
                        {
                            sb.AppendLine($"{bytes[col]} is a invalid number, use values from 0 to 255");
                            return sb.ToString();
                        }
                    }
                }
            }

            Pattern = stringMatrix[0].Pattern;
            AlternatePattern = stringMatrix[1].Pattern;

            if (Pattern is null && AlternatePattern is null)
            {
                sb.AppendLine("Either even or odd pattern must contain a valid matrix.");
                return sb.ToString();
            }

            return sb.ToString();
        }
        
        public override string ToString()
        {
            var result = $"[Border: {_wallThicknessStart}px to {_wallThicknessEnd}px] [Chamfer: {_chamfer}] [Only borders: {_wallsOnly}] [Alternate every: {_alternatePatternPerLayers}] [B: {_brightness}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Constructor

        public OperationPixelDimming() { }

        public OperationPixelDimming(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Properties

        public uint WallThickness
        {
            get => _wallThicknessStart;
            set
            {
                WallThicknessStart = value;
                WallThicknessEnd = value;
            }
        }

        public uint WallThicknessStart
        {
            get => _wallThicknessStart;
            set => RaiseAndSetIfChanged(ref _wallThicknessStart, value);
        }

        public uint WallThicknessEnd
        {
            get => _wallThicknessEnd;
            set => RaiseAndSetIfChanged(ref _wallThicknessEnd, value);
        }

        public bool WallsOnly
        {
            get => _wallsOnly;
            set => RaiseAndSetIfChanged(ref _wallsOnly, value);
        }

        public bool Chamfer
        {
            get => _chamfer;
            set => RaiseAndSetIfChanged(ref _chamfer, value);
        }

        /// <summary>
        /// Use the alternate pattern every <see cref="AlternatePatternPerLayers"/> layers
        /// </summary>
        public ushort AlternatePatternPerLayers
        {
            get => _alternatePatternPerLayers;
            set => RaiseAndSetIfChanged(ref _alternatePatternPerLayers, Math.Max((ushort)1, value));
        }

        public string PatternText
        {
            get => _patternText;
            set => RaiseAndSetIfChanged(ref _patternText, value);
        }

        public string AlternatePatternText
        {
            get => _alternatePatternText;
            set => RaiseAndSetIfChanged(ref _alternatePatternText, value);
        }

        [XmlIgnore]
        public Matrix<byte> Pattern
        {
            get => _pattern;
            set => RaiseAndSetIfChanged(ref _pattern, value);
        }

        [XmlIgnore]
        public Matrix<byte> AlternatePattern
        {
            get => _alternatePattern;
            set => RaiseAndSetIfChanged(ref _alternatePattern, value);
        }

        public byte Brightness
        {
            get => _brightness;
            set
            {
                if(!RaiseAndSetIfChanged(ref _brightness, value)) return;
                RaisePropertyChanged(nameof(BrightnessPercent));
            }
        }
        
        public decimal BrightnessPercent => Math.Round(_brightness * 100 / 255M, 2);


        public ushort InfillGenThickness
        {
            get => _infillGenThickness;
            set => RaiseAndSetIfChanged(ref _infillGenThickness, value);
        }

        public ushort InfillGenSpacing
        {
            get => _infillGenSpacing;
            set => RaiseAndSetIfChanged(ref _infillGenSpacing, value);
        }
        
        #endregion

        #region Equality

        protected bool Equals(OperationPixelDimming other)
        {
            return _wallThicknessStart == other._wallThicknessStart && _wallThicknessEnd == other._wallThicknessEnd && _wallsOnly == other._wallsOnly && _chamfer == other._chamfer && _alternatePatternPerLayers == other._alternatePatternPerLayers && _patternText == other._patternText && _alternatePatternText == other._alternatePatternText && _brightness == other._brightness && _infillGenThickness == other._infillGenThickness && _infillGenSpacing == other._infillGenSpacing;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationPixelDimming) obj);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_wallThicknessStart);
            hashCode.Add(_wallThicknessEnd);
            hashCode.Add(_wallsOnly);
            hashCode.Add(_chamfer);
            hashCode.Add(_alternatePatternPerLayers);
            hashCode.Add(_patternText);
            hashCode.Add(_alternatePatternText);
            hashCode.Add(_brightness);
            hashCode.Add(_infillGenThickness);
            hashCode.Add(_infillGenSpacing);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods
        public bool IsNormalPattern(uint layerIndex) => layerIndex / AlternatePatternPerLayers % 2 == 0;

        public bool IsAlternatePattern(uint layerIndex) => !IsNormalPattern(layerIndex);

        public unsafe void LoadPatternFromImage(Mat mat, bool isAlternatePattern = false)
        {
            var result = new string[mat.Height];
            var span = mat.GetBytePointer();
            Parallel.For(0, mat.Height, y =>
            {
                result[y] = string.Empty;
                for (int x = 0; x < mat.Width; x++)
                {
                    result[y] += $"{span[mat.GetPixelPos(x, y)]} ";
                }

                result[y] = result[y].Trim();
            });

            StringBuilder sb = new();
            foreach (var s in result)
            {
                sb.AppendLine(s);
            }

            if (isAlternatePattern)
            {
                AlternatePatternText = sb.ToString();
            }
            else
            {
                PatternText = sb.ToString();
            }
        }

        public void LoadPatternFromImage(string filepath, bool isAlternatePattern = false)
        {
            try
            {
                using var mat = CvInvoke.Imread(filepath, ImreadModes.Grayscale);
                LoadPatternFromImage(mat, isAlternatePattern);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }


        public void GeneratePixelDimming(string pattern)
        {
            if (pattern == "Chessboard")
            {
                PatternText = string.Format(
                    "255 {0}{1}" +
                    "{0} 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "{0} 255{1}" +
                    "255 {0}"
                    , _brightness, "\n");

                return;
            }

            if (pattern == "Sparse")
            {
                PatternText = string.Format(
                    "{0} 255 255 255{1}" +
                    "255 255 {0} 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0} 255{1}" +
                    "{0} 255 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Crosses")
            {
                PatternText = string.Format(
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 255 255 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Strips")
            {
                PatternText = string.Format(
                    "{0}{1}" +
                    "255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255{1}" +
                    "{0}"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Pyramid")
            {
                PatternText = string.Format(
                    "255 255 {0} 255 255 255{1}" +
                    "255 {0} 255 {0} 255 255{1}" +
                    "{0} 255 {0} 255 {0} 255{1}" +
                    "255 255 255 255 255 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255{1}" +
                    "255 255 255 255 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Rhombus")
            {
                PatternText = string.Format(
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "255 255 255 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Hearts")
            {
                PatternText = string.Format(
                    "255 {0} 255 {0} 255 255{1}" +
                    "{0} 255 {0} 255 {0} 255{1}" +
                    "{0} 255 255 255 {0} 255{1}" +
                    "255 {0} 255 {0} 255 255{1}" +
                    "255 255 {0} 255 255 255{1}" +
                    "255 255 255 255 255 255"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255 255 255{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 {0} 255 255 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Slashes")
            {
                PatternText = string.Format(
                    "{0} 255 255{1}" +
                    "255 {0} 255{1}" +
                    "255 255 {0}"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0}{1}" +
                    "255 {0} 255{1}" +
                    "{0} 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Waves")
            {
                PatternText = string.Format(
                    "{0} 255 255{1}" +
                    "255 255 {0}"
                    , _brightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0}{1}" +
                    "{0} 255 255"
                    , _brightness, "\n");
                return;
            }

            if (pattern == "Solid")
            {
                PatternText = _brightness.ToString();
                AlternatePatternText = null;
                return;
            }
        }

        public void GenerateInfill(string pattern)
        {
            if (pattern == "Rectilinear")
            {
                PatternText = ($"0\n".Repeat(_infillGenSpacing) + $"255\n".Repeat(_infillGenSpacing)).Trim('\n', '\r');
                AlternatePatternText = null;
                return;
            }

            if (pattern == "Square grid")
            {
                var p1 = "0 ".Repeat(_infillGenSpacing) + "255 ".Repeat(_infillGenThickness);
                p1 = p1.Trim() + "\n";
                p1 += p1.Repeat(_infillGenThickness);


                var p2 = "255 ".Repeat(_infillGenSpacing) + "255 ".Repeat(_infillGenThickness);
                p2 = p2.Trim() + '\n';
                p2 += p2.Repeat(_infillGenThickness);

                p2 = p2.Trim('\n', '\r');

                PatternText = p1 + p2;
                AlternatePatternText = null;
                return;
            }

            if (pattern == "Waves")
            {
                var p1 = string.Empty;
                var pos = 0;
                for (sbyte dir = 1; dir >= -1; dir -= 2)
                {
                    while (pos >= 0 && pos <= _infillGenSpacing)
                    {
                        p1 += "0 ".Repeat(pos);
                        p1 += "255 ".Repeat(_infillGenThickness);
                        p1 += "0 ".Repeat(_infillGenSpacing - pos);
                        p1 = p1.Trim() + '\n';

                        pos += dir;
                    }

                    pos--;
                }

                PatternText = p1.Trim('\n', '\r');
                AlternatePatternText = null;
                return;
            }

            if (pattern == "Lattice")
            {
                var p1 = string.Empty;
                var p2 = string.Empty;

                var zeros = Math.Max(0, _infillGenSpacing - _infillGenThickness * 2);

                // Pillar
                for (int i = 0; i < _infillGenThickness; i++)
                {
                    p1 += "255 ".Repeat(_infillGenThickness);
                    p1 += "0 ".Repeat(zeros);
                    p1 += "255 ".Repeat(_infillGenThickness);
                    p1 = p1.Trim() + '\n';
                }

                for (int i = 0; i < zeros; i++)
                {
                    p1 += "0 ".Repeat(_infillGenSpacing);
                    p1 = p1.Trim() + '\n';
                }

                for (int i = 0; i < _infillGenThickness; i++)
                {
                    p1 += "255 ".Repeat(_infillGenThickness);
                    p1 += "0 ".Repeat(zeros);
                    p1 += "255 ".Repeat(_infillGenThickness);
                    p1 = p1.Trim() + '\n';
                }

                // Square
                for (int i = 0; i < _infillGenThickness; i++)
                {
                    p2 += "255 ".Repeat(_infillGenSpacing);
                    p2 = p2.Trim() + '\n';
                }

                for (int i = 0; i < zeros; i++)
                {
                    p2 += "255 ".Repeat(_infillGenThickness);
                    p2 += "0 ".Repeat(zeros);
                    p2 += "255 ".Repeat(_infillGenThickness);
                    p2 = p2.Trim() + '\n';
                }

                for (int i = 0; i < _infillGenThickness; i++)
                {
                    p2 += "255 ".Repeat(_infillGenSpacing);
                    p2 = p2.Trim() + '\n';
                }



                PatternText = p1.Trim('\n', '\r');
                AlternatePatternText = p2.Trim('\n', '\r'); ;
                return;
            }
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            if (Pattern is null)
            {
                Pattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127,
                    [0, 1] = 255,
                    [1, 0] = 255,
                    [1, 1] = 127,
                };

                AlternatePattern ??= new Matrix<byte>(2, 2)
                {
                    [0, 0] = 255,
                    [0, 1] = 127,
                    [1, 0] = 127,
                    [1, 1] = 255,
                };
            }

            AlternatePattern ??= Pattern;

            using var blankMat = EmguExtensions.InitMat(SlicerFile.Resolution);
            using var matPattern = blankMat.CloneBlank();
            using var matAlternatePattern = blankMat.CloneBlank();
            var target = GetRoiOrDefault(blankMat);

            CvInvoke.Repeat(Pattern, target.Rows / Pattern.Rows + 1, target.Cols / Pattern.Cols + 1, matPattern);
            CvInvoke.Repeat(AlternatePattern, target.Rows / AlternatePattern.Rows + 1, target.Cols / AlternatePattern.Cols + 1, matAlternatePattern);

            using var patternMask = new Mat(matPattern, new Rectangle(0, 0, target.Width, target.Height));
            using var alternatePatternMask = new Mat(matAlternatePattern, new Rectangle(0, 0, target.Width, target.Height));
            /*if (_wallsOnly)
            {
                CvInvoke.BitwiseNot(patternMask, patternMask);
                CvInvoke.BitwiseNot(alternatePatternMask, alternatePatternMask);
            }*/

            CvInvoke.BitwiseNot(patternMask, patternMask);
            CvInvoke.BitwiseNot(alternatePatternMask, alternatePatternMask);

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat, layerIndex, patternMask, alternatePatternMask);
                SlicerFile[layerIndex].LayerMat = mat;

                progress.LockAndIncrement();
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            if (arguments is null || arguments.Length < 2) return false;
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            uint layerIndex = Convert.ToUInt32(arguments[0]);
            Mat patternMask = (Mat)arguments[1];
            Mat alternatePatternMask = arguments.Length >= 3 && arguments[2] is not null ? (Mat)arguments[2] : patternMask;

            int wallThickness = LayerManager.MutateGetIterationChamfer(
                layerIndex,
                LayerIndexStart,
                LayerIndexEnd,
                (int)WallThicknessStart,
                (int)WallThicknessEnd,
                Chamfer
            );


            using Mat erode = new();
            using Mat diff = new();
            var original = mat.Clone();
            var target = GetRoiOrDefault(mat);
            using var mask = GetMask(mat);


            CvInvoke.Erode(target, erode, kernel, anchor, wallThickness, BorderType.Reflect101, default);

            if (_wallsOnly)
            {
                CvInvoke.Subtract(target, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target);
                //CvInvoke.BitwiseAnd(diff, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target, mask);
                CvInvoke.Add(erode, target, target);
            }
            else
            {
                //CvInvoke.Subtract(target, erode, diff);
                //CvInvoke.BitwiseAnd(erode, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target, mask);
                //CvInvoke.Add(target, diff, target, mask);
                CvInvoke.Subtract(target, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target, erode);
            }

            ApplyMask(original, target, mask);

            return true;
        }

        #endregion
    }
}
