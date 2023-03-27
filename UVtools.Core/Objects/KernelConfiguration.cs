/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Managers;

namespace UVtools.Core.Objects;


public sealed class KernelConfiguration : BindableBase, IDisposable
{
    #region Members
    private bool _useDynamicKernel;
    private readonly KernelCacheManager _kernelCache = new();
    private ElementShape _kernelShape = ElementShape.Rectangle;
    private uint _matrixWidth = 3;
    private uint _matrixHeight = 3;
    private string _matrixText = "1 1 1\n1 1 1\n1 1 1";
    private int _anchorX = -1;
    private int _anchorY = -1;
    private Mat? _kernelMat;
    private readonly object _mutex = new();
    private ElementShape _dynamicKernelShape = ElementShape.Ellipse;

    #endregion

    #region Properties
    public bool UseDynamicKernel
    {
        get => _useDynamicKernel;
        set
        {
            if(!RaiseAndSetIfChanged(ref _useDynamicKernel, value)) return;
            _kernelCache.UseDynamicKernel = value;
        }
    }

    public ElementShape DynamicKernelShape
    {
        get => _dynamicKernelShape;
        set
        {
            if (!RaiseAndSetIfChanged(ref _dynamicKernelShape, value)) return;
            _kernelCache.DynamicKernelShape = value;
        }
    }

    public int AnchorX
    {
        get => _anchorX;
        set => RaiseAndSetIfChanged(ref _anchorX, value);
    }

    public int AnchorY
    {
        get => _anchorY;
        set => RaiseAndSetIfChanged(ref _anchorY, value);
    }

    public string MatrixText
    {
        get => _matrixText;
        set => RaiseAndSetIfChanged(ref _matrixText, value);
    }


    public uint MatrixWidth
    {
        get => _matrixWidth;
        set => RaiseAndSetIfChanged(ref _matrixWidth, value);
    }

    public uint MatrixHeight
    {
        get => _matrixHeight;
        set => RaiseAndSetIfChanged(ref _matrixHeight, value);
    }

    public Size MatrixSize => new((int)_matrixWidth, (int)_matrixHeight);

    [XmlIgnore]
    public Mat? KernelMat
    {
        get
        {
            lock (_mutex)
            {
                if (_kernelMat is null)
                {
                    
                    if (!string.IsNullOrWhiteSpace(_matrixText))
                    {
                        GenerateKernelFromText();
                    }
                    else
                    {
                        SetKernel(ElementShape.Rectangle);
                    }
                }
            }

            return _kernelMat;
        }
        set => _kernelMat = value;
    }

    public ElementShape KernelShape
    {
        get => _kernelShape;
        set => RaiseAndSetIfChanged(ref _kernelShape, value);
    }

    public IEnumerable<ElementShape> KernelShapes => ((ElementShape[])Enum.GetValues(typeof(ElementShape))).Where(element => element != ElementShape.Custom);

    public Point Anchor
    {
        get => new(_anchorX, _anchorY);
        set
        {
            _anchorX = value.X;
            _anchorY = value.Y;
        }
    }
    #endregion

    #region Constructor
    public KernelConfiguration() { }
    #endregion

    #region Methods
    public void ResetKernel()
    {
        KernelShape = ElementShape.Rectangle;
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
            var span = kernel.GetRowByteSpan(y);
            var line = string.Empty;
            for (int x = 0; x < span.Length; x++)
            {
                line += $" {span[x]}";
            }

            line = line.Remove(0, 1);
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
        if (string.IsNullOrEmpty(_matrixText))
        {
            throw new InvalidOperationException("Invalid kernel: Kernel can't be empty.");
        }
        Matrix<byte> matrix = null!;
        var lines = _matrixText.Split('\n');
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
        if (matrix.Cols <= _anchorX || matrix.Rows <= _anchorY)
        {
            matrix.Dispose();
            throw new InvalidOperationException("Invalid kernel: Anchor position X/Y can't be higher or equal than kernel columns/rows\nPlease fix the values.");
        }

        _kernelMat?.Dispose();
        KernelMat = matrix.Mat;
    }


    public void SetKernel(ElementShape shape, Size size, Point anchor)
    {
        KernelMat = CvInvoke.GetStructuringElement(shape, size, anchor);
    }

    public void SetKernel(ElementShape shape, Size size) => SetKernel(shape, size, EmguExtensions.AnchorCenter);
    public void SetKernel(ElementShape shape) => SetKernel(shape, new Size(3, 3), EmguExtensions.AnchorCenter);

    public Mat? GetKernel()
    {
        return KernelMat;
    }

    public Mat? GetKernel(ref int iterations)
    {
        if (!_useDynamicKernel) return KernelMat;
        return _kernelCache.Get(ref iterations);
    }

    public void Dispose()
    {
        _kernelMat?.Dispose();
        _kernelCache?.Dispose();
    }
    #endregion
}