/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Managers;

namespace UVtools.Core.Objects;


public sealed partial class KernelConfiguration : ObservableObject, IDisposable
{
    #region Members
    private readonly KernelCacheManager _kernelCache = new();
    private Mat? _kernelMat;
    private readonly Lock _mutex = new();

    #endregion

    #region Properties
    [ObservableProperty]
    public partial bool UseDynamicKernel { get; set; }

    partial void OnUseDynamicKernelChanged(bool value) => _kernelCache.UseDynamicKernel = value;

    [ObservableProperty]
    public partial MorphShapes DynamicKernelShape { get; set; } = MorphShapes.Ellipse;

    partial void OnDynamicKernelShapeChanged(MorphShapes value) => _kernelCache.DynamicKernelShape = value;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Anchor))]
    public partial int AnchorX { get; set; } = -1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Anchor))]
    public partial int AnchorY { get; set; } = -1;

    [ObservableProperty]
    public partial string MatrixText { get; set; } = "1 1 1\n1 1 1\n1 1 1";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MatrixSize))]
    public partial uint MatrixWidth { get; set; } = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MatrixSize))]
    public partial uint MatrixHeight { get; set; } = 3;

    public Size MatrixSize => new((int)MatrixWidth, (int)MatrixHeight);

    [XmlIgnore]
    public Mat? KernelMat
    {
        get
        {
            lock (_mutex)
            {
                if (_kernelMat is null)
                {

                    if (!string.IsNullOrWhiteSpace(MatrixText))
                    {
                        GenerateKernelFromText();
                    }
                    else
                    {
                        SetKernel(MorphShapes.Rectangle);
                    }
                }
            }

            return _kernelMat;
        }
        set => _kernelMat = value;
    }

    [ObservableProperty]
    public partial MorphShapes KernelShape { get; set; } = MorphShapes.Rectangle;

    public IEnumerable<MorphShapes> KernelShapes => ((MorphShapes[])Enum.GetValues(typeof(MorphShapes))).Where(element => element != MorphShapes.Custom);

    public Point Anchor
    {
        get => new(AnchorX, AnchorY);
        set
        {
            AnchorX = value.X;
            AnchorY = value.Y;
        }
    }
    #endregion

    #region Constructor
    public KernelConfiguration() { }
    #endregion

    #region Methods
    public void ResetKernel()
    {
        KernelShape = MorphShapes.Rectangle;
        MatrixWidth = 3;
        MatrixHeight = 3;
        AnchorX = -1;
        AnchorY = -1;
        _kernelMat?.Dispose();
        _kernelMat = null;
        MatrixText = "1 1 1\n1 1 1\n1 1 1";
    }

    public void GenerateKernelText()
    {
        using var kernel = CvInvoke.GetStructuringElement(KernelShape, MatrixSize, Anchor);
        string text = string.Empty;
        for (int y = 0; y < kernel.Height; y++)
        {
            var span = kernel.GetReadOnlyRowSpanOfBytes(y);
            var line = string.Empty;
            for (int x = 0; x < span.Length; x++)
            {
                line += $" {span[x]}";
            }

            line = line[1..];
            text += line;
            if (y < kernel.Height - 1)
            {
                text += '\n';
            }
        }

        MatrixText = text;
    }

    public void GenerateKernelFromText()
    {
        if (string.IsNullOrEmpty(MatrixText))
        {
            throw new InvalidOperationException("Invalid kernel: Kernel can't be empty.");
        }
        Matrix<byte> matrix = null!;
        var lines = MatrixText.Split('\n');
        for (var row = 0; row < lines.Length; row++)
        {
            var bytes = lines[row].Trim().Split(' ');
            if (row == 0)
            {
                matrix = new Matrix<byte>(lines.Length, bytes.Length);
            }
            else
            {
                if (matrix.Cols != bytes.Length)
                {
                    matrix.Dispose();
                    throw new InvalidOperationException($"Invalid kernel: Row {row + 1} have invalid number of columns, the matrix must have equal columns count per line, per defined on line 1");
                }
            }

            for (int col = 0; col < bytes.Length; col++)
            {
                if (byte.TryParse(bytes[col], out var value))
                {
                    matrix[row, col] = value;
                }
                else
                {
                    matrix.Dispose();
                    throw new InvalidOperationException($"Invalid kernel: {bytes[col]} is a invalid number, use values from 0 to 255");
                }
            }
        }
        if (matrix.Cols <= AnchorX || matrix.Rows <= AnchorY)
        {
            matrix.Dispose();
            throw new InvalidOperationException("Invalid kernel: Anchor position X/Y can't be higher or equal than kernel columns/rows\nPlease fix the values.");
        }

        _kernelMat?.Dispose();
        KernelMat = matrix.Mat;
    }


    public void SetKernel(MorphShapes shape, Size size, Point anchor)
    {
        KernelMat = CvInvoke.GetStructuringElement(shape, size, anchor);
    }

    public void SetKernel(MorphShapes shape, Size size) => SetKernel(shape, size, EmguCvExtensions.AnchorCenter);
    public void SetKernel(MorphShapes shape) => SetKernel(shape, new Size(3, 3), EmguCvExtensions.AnchorCenter);

    public Mat? GetKernel()
    {
        return KernelMat;
    }

    public Mat? GetKernel(ref int iterations)
    {
        if (!UseDynamicKernel) return KernelMat;
        return _kernelCache.Get(ref iterations);
    }

    public void Dispose()
    {
        _kernelMat?.Dispose();
        _kernelCache?.Dispose();
    }
    #endregion
}
