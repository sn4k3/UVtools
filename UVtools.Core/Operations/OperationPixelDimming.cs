/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationPixelDimming : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Subclasses
    class StringMatrix
    {
        public string? Text { get; }
        public Matrix<byte> Pattern { get; set; } = null!;

        public StringMatrix(string? text)
        {
            Text = text;
        }
    }
    #endregion

    #region Members
    private ushort _alternatePatternPerLayers = 1;

    #endregion

    #region Overrides
    public override string IconClass => "CircleOpacity";
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

    public override string? ValidateInternally()
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
        var result = $"[Border: {WallThicknessStart}px to {WallThicknessEnd}px] [Chamfer: {Chamfer}] [Only borders: {WallsOnly}] [Alternate every: {_alternatePatternPerLayers}] [B: {Brightness}]" + LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationPixelDimming()
    { }

    public OperationPixelDimming(FileFormat slicerFile) : base(slicerFile) { }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial bool LighteningPixels { get; set; }

    public uint WallThickness
    {
        get => WallThicknessStart;
        set
        {
            WallThicknessStart = value;
            WallThicknessEnd = value;
        }
    }

    [ObservableProperty]
    public partial uint WallThicknessStart { get; set; } = 10;

    [ObservableProperty]
    public partial uint WallThicknessEnd { get; set; } = 10;

    [ObservableProperty]
    public partial bool WallsOnly { get; set; }

    [ObservableProperty]
    public partial bool Chamfer { get; set; }

    /// <summary>
    /// Use the alternate pattern every <see cref="AlternatePatternPerLayers"/> layers
    /// </summary>
    public ushort AlternatePatternPerLayers
    {
        get => _alternatePatternPerLayers;
        set => SetProperty(ref _alternatePatternPerLayers, Math.Max((ushort)1, value));
    }

    [ObservableProperty]
    public partial string PatternText { get; set; } = null!;

    [ObservableProperty]
    public partial string? AlternatePatternText { get; set; }

    [XmlIgnore]
    [ObservableProperty]
    public partial Matrix<byte> Pattern { get; set; } = null!;

    [XmlIgnore]
    [ObservableProperty]
    public partial Matrix<byte> AlternatePattern { get; set; } = null!;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BrightnessPercent))]
    public partial byte Brightness { get; set; } = 127;

    public float BrightnessPercent => MathF.Round(Brightness * 100 / 255.0f, 2);


    [ObservableProperty]
    public partial ushort InfillGenThickness { get; set; } = 10;

    [ObservableProperty]
    public partial ushort InfillGenSpacing { get; set; } = 20;

    #endregion

    #region Equality

    protected bool Equals(OperationPixelDimming other)
    {
        return LighteningPixels == other.LighteningPixels && WallThicknessStart == other.WallThicknessStart && WallThicknessEnd == other.WallThicknessEnd && WallsOnly == other.WallsOnly && Chamfer == other.Chamfer && _alternatePatternPerLayers == other._alternatePatternPerLayers && PatternText == other.PatternText && AlternatePatternText == other.AlternatePatternText && Brightness == other.Brightness && InfillGenThickness == other.InfillGenThickness && InfillGenSpacing == other.InfillGenSpacing;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationPixelDimming)obj);
    }

    #endregion

    #region Methods
    public bool IsNormalPattern(uint layerIndex) => layerIndex / AlternatePatternPerLayers % 2 == 0;

    public bool IsAlternatePattern(uint layerIndex) => !IsNormalPattern(layerIndex);

    public unsafe void LoadPatternFromImage(Mat mat, bool isAlternatePattern = false)
    {
        var result = new string[mat.Height];
        var span = mat.BytePointer;
        Parallel.For(0, mat.Height, CoreSettings.ParallelOptions, y =>
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


    public void GeneratePixelDimming(object pattern) => GeneratePixelDimming(pattern.ToString()!);
    public void GeneratePixelDimming(string pattern)
    {
        if (pattern == "Chessboard")
        {
            PatternText = string.Format(
                "255 {0}{1}" +
                "{0} 255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "{0} 255{1}" +
                "255 {0}"
                , Brightness, "\n");

            return;
        }

        if (pattern == "Sparse")
        {
            PatternText = string.Format(
                "{0} 255 255 255{1}" +
                "255 255 {0} 255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 {0} 255{1}" +
                "{0} 255 255 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Crosses")
        {
            PatternText = string.Format(
                "{0} 255 {0} 255{1}" +
                "255 {0} 255 255{1}" +
                "{0} 255 {0} 255{1}" +
                "255 255 255 255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 255 255{1}" +
                "{0} 255 {0} 255{1}" +
                "255 {0} 255 255{1}" +
                "{0} 255 {0} 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Strips")
        {
            PatternText = string.Format(
                "{0}{1}" +
                "255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255{1}" +
                "{0}"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Pyramid")
        {
            PatternText = string.Format(
                "255 255 {0} 255 255 255{1}" +
                "255 {0} 255 {0} 255 255{1}" +
                "{0} 255 {0} 255 {0} 255{1}" +
                "255 255 255 255 255 255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 {0} 255 {0} 255 {0}{1}" +
                "255 255 {0} 255 {0} 255{1}" +
                "255 255 255 {0} 255 255{1}" +
                "255 255 255 255 255 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Rhombus")
        {
            PatternText = string.Format(
                "255 {0} 255 255{1}" +
                "{0} 255 {0} 255{1}" +
                "255 {0} 255 255{1}" +
                "255 255 255 255"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 255 255{1}" +
                "255 {0} 255 255{1}" +
                "{0} 255 {0} 255{1}" +
                "255 {0} 255 255"
                , Brightness, "\n");
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
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 255 255 255 255{1}" +
                "255 255 {0} 255 {0} 255{1}" +
                "255 {0} 255 {0} 255 {0}{1}" +
                "255 {0} 255 255 255 {0}{1}" +
                "255 255 {0} 255 {0} 255{1}" +
                "255 255 255 {0} 255 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Slashes")
        {
            PatternText = string.Format(
                "{0} 255 255{1}" +
                "255 {0} 255{1}" +
                "255 255 {0}"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 {0}{1}" +
                "255 {0} 255{1}" +
                "{0} 255 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Waves")
        {
            PatternText = string.Format(
                "{0} 255 255{1}" +
                "255 255 {0}"
                , Brightness, "\n");

            AlternatePatternText = string.Format(
                "255 255 {0}{1}" +
                "{0} 255 255"
                , Brightness, "\n");
            return;
        }

        if (pattern == "Solid")
        {
            PatternText = Brightness.ToString();
            AlternatePatternText = null;
            return;
        }
    }
    public void GenerateInfill(object pattern) => GenerateInfill(pattern.ToString()!);
    public void GenerateInfill(string pattern)
    {
        if (pattern == "Rectilinear")
        {
            PatternText = ($"0\n".Repeat(InfillGenSpacing) + $"255\n".Repeat(InfillGenSpacing)).Trim('\n', '\r');
            AlternatePatternText = null;
            return;
        }

        if (pattern == "Square grid")
        {
            var p1 = "0 ".Repeat(InfillGenSpacing) + "255 ".Repeat(InfillGenThickness);
            p1 = p1.Trim() + "\n";
            p1 += p1.Repeat(InfillGenThickness);


            var p2 = "255 ".Repeat(InfillGenSpacing) + "255 ".Repeat(InfillGenThickness);
            p2 = p2.Trim() + '\n';
            p2 += p2.Repeat(InfillGenThickness);

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
                while (pos >= 0 && pos <= InfillGenSpacing)
                {
                    p1 += "0 ".Repeat(pos);
                    p1 += "255 ".Repeat(InfillGenThickness);
                    p1 += "0 ".Repeat(InfillGenSpacing - pos);
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

            var zeros = Math.Max(0, InfillGenSpacing - InfillGenThickness * 2);

            // Pillar
            for (int i = 0; i < InfillGenThickness; i++)
            {
                p1 += "255 ".Repeat(InfillGenThickness);
                p1 += "0 ".Repeat(zeros);
                p1 += "255 ".Repeat(InfillGenThickness);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p1 += "0 ".Repeat(InfillGenSpacing);
                p1 = p1.Trim() + '\n';
            }

            for (int i = 0; i < InfillGenThickness; i++)
            {
                p1 += "255 ".Repeat(InfillGenThickness);
                p1 += "0 ".Repeat(zeros);
                p1 += "255 ".Repeat(InfillGenThickness);
                p1 = p1.Trim() + '\n';
            }

            // Square
            for (int i = 0; i < InfillGenThickness; i++)
            {
                p2 += "255 ".Repeat(InfillGenSpacing);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < zeros; i++)
            {
                p2 += "255 ".Repeat(InfillGenThickness);
                p2 += "0 ".Repeat(zeros);
                p2 += "255 ".Repeat(InfillGenThickness);
                p2 = p2.Trim() + '\n';
            }

            for (int i = 0; i < InfillGenThickness; i++)
            {
                p2 += "255 ".Repeat(InfillGenSpacing);
                p2 = p2.Trim() + '\n';
            }



            PatternText = p1.Trim('\n', '\r');
            AlternatePatternText = p2.Trim('\n', '\r');
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

        using var blankMat = EmguCvExtensions.InitMat(SlicerFile.Resolution);
        using var matPattern = blankMat.NewZeros();
        using var matAlternatePattern = blankMat.NewZeros();
        using var target = GetRoiOrDefault(blankMat);

        CvInvoke.Repeat(Pattern, target.Rows / Pattern.Rows + 1, target.Cols / Pattern.Cols + 1, matPattern);
        CvInvoke.Repeat(AlternatePattern, target.Rows / AlternatePattern.Rows + 1, target.Cols / AlternatePattern.Cols + 1, matAlternatePattern);

        using var patternMask = new Mat(matPattern, new Rectangle(0, 0, target.Width, target.Height));
        using var alternatePatternMask = new Mat(matAlternatePattern, new Rectangle(0, 0, target.Width, target.Height));
        /*if (WallsOnly)
        {
            CvInvoke.BitwiseNot(patternMask, patternMask);
            CvInvoke.BitwiseNot(alternatePatternMask, alternatePatternMask);
        }*/

        CvInvoke.BitwiseNot(patternMask, patternMask);
        CvInvoke.BitwiseNot(alternatePatternMask, alternatePatternMask);

        Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            Execute(mat, layerIndex, patternMask, alternatePatternMask);
            SlicerFile[layerIndex].LayerMat = mat;

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    public override bool Execute(Mat mat, params object[]? arguments)
    {
        if (arguments is null || arguments.Length < 2) return false;

        var kernel = EmguCvExtensions.Kernel3X3Rectangle;

        uint layerIndex = Convert.ToUInt32(arguments[0]);
        Mat patternMask = (Mat)arguments[1];
        Mat alternatePatternMask = arguments.Length >= 3 && arguments[2] is not null ? (Mat)arguments[2] : patternMask;

        int wallThickness = FileFormat.MutateGetIterationChamfer(
            layerIndex,
            LayerIndexStart,
            LayerIndexEnd,
            (int)WallThicknessStart,
            (int)WallThicknessEnd,
            Chamfer
        );


        using Mat erode = new();
        //using Mat diff = new();
        var original = mat.Clone();
        using var originalRoi = GetRoiOrDefault(original);
        using var target = GetRoiOrDefault(mat);
        using var mask = GetMask(mat);


        CvInvoke.Erode(target, erode, kernel, EmguCvExtensions.AnchorCenter, wallThickness, BorderType.Reflect101, default);

        if (LighteningPixels)
        {
            CvInvoke.Add(target, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target, WallsOnly ? target : erode);
        }
        else
        {
            CvInvoke.Subtract(target, IsNormalPattern(layerIndex) ? patternMask : alternatePatternMask, target, WallsOnly ? target : erode);
        }

        if (WallsOnly)
        {
            originalRoi.CopyTo(target, erode);
        }

        ApplyMask(originalRoi, target, mask);

        return true;
    }

    #endregion
}