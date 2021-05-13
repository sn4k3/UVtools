using System;
using System.Collections.Generic;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.WPF.Extensions;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;

namespace UVtools.WPF.Controls
{
    public class KernelControl : UserControlEx
    {

        private ElementShape _selectedKernelShape = ElementShape.Rectangle;
        private uint _matrixWidth = 3;
        private uint _matrixHeight = 3;
        private int _anchorX = -1;
        private int _anchorY = -1;
        private string _matrixText;
        public List<ElementShape> KernelShapes { get; } = new();

        public string MatrixText
        {
            get => _matrixText;
            set => RaiseAndSetIfChanged(ref _matrixText, value);
        }

        public ElementShape SelectedKernelShape
        {
            get => _selectedKernelShape;
            set => RaiseAndSetIfChanged(ref _selectedKernelShape, value);
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

        
        public Point Anchor => new(_anchorX, _anchorY);


        public KernelControl()
        {
            InitializeComponent();
            foreach (var element in (ElementShape[])Enum.GetValues(
                typeof(ElementShape)))
            {
                if (element == ElementShape.Custom) continue;
                KernelShapes.Add(element);
            }
            ResetKernel();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void GenerateKernel()
        {
            if (MatrixWidth <= AnchorX || MatrixHeight <= AnchorY)
            {
                App.MainWindow.MessageBoxError("Anchor position X/Y can't be higher or equal than size X/Y\nPlease fix the values.", "Invalid anchor position");
                return;
            }

            using var kernel = CvInvoke.GetStructuringElement(SelectedKernelShape, MatrixSize, Anchor);
            string text = string.Empty;
            for (int y = 0; y < kernel.Height; y++)
            {
                var span = kernel.GetPixelRowSpan<byte>(y);
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

        public void ResetKernel()
        {
            SelectedKernelShape = ElementShape.Rectangle;
            MatrixWidth = 3;
            MatrixHeight = 3;
            AnchorX = -1;
            AnchorY = -1;
            GenerateKernel();
        }

        public Matrix<byte> GetMatrix()
        {
            if (string.IsNullOrEmpty(MatrixText))
            {
                App.MainWindow.MessageBoxError("Kernel can't be empty.", "Empty Kernel");
                return null;
            }
            Matrix<byte> matrix = null;
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
                        App.MainWindow.MessageBoxError($"Row {row + 1} have invalid number of columns, the matrix must have equal columns count per line, per defined on line 1", "Invalid kernel");
                        matrix.Dispose();
                        return null;
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
                        App.MainWindow.MessageBoxError($"{bytes[col]} is a invalid number, use values from 0 to 255");
                        matrix.Dispose();
                        return null;
                    }
                }
            }
            if (matrix.Cols <= AnchorX || matrix.Rows <= AnchorY)
            {
                App.MainWindow.MessageBoxError("Anchor position X/Y can't be higher or equal than kernel columns/rows\nPlease fix the values.", "Invalid anchor position");
                matrix.Dispose();
                return null;
            }
            return matrix;
        }

        public Kernel GetKernel()
        {
            var matrix = GetMatrix();
            return matrix is null ? null : new Kernel(matrix, Anchor);
        }
    }
}
