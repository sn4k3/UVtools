using System;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolPixelDimmingControl : ToolControl
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

        private string _patternText;
        private string _alternatePatternText;
        private byte _dimGenBrightness = 127;
        private ushort _infillGenThickness = 10;
        private ushort _infillGenSpacing = 20;
        public OperationPixelDimming Operation { get; }

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

        public byte DimGenBrightness
        {
            get => _dimGenBrightness;
            set => RaiseAndSetIfChanged(ref _dimGenBrightness, value);
        }

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

        public ToolPixelDimmingControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationPixelDimming();
            GeneratePixelDimming("Chessboard");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override bool UpdateOperation()
        {
            var stringMatrix = new[]
            {
                new StringMatrix(PatternText),
                new StringMatrix(AlternatePatternText),
            };

            foreach (var item in stringMatrix)
            {
                if (string.IsNullOrWhiteSpace(item.Text)) continue;
                var lines = item.Text.Split('\n');
                for (var row = 0; row < lines.Length; row++)
                {

                    var bytes = lines[row].Trim().Split(' ');
                    if (row == 0)
                    {
                        item.Pattern = new Matrix<byte>(lines.Length, bytes.Length);
                    }
                    else
                    {
                        if (item.Pattern.Cols != bytes.Length)
                        {
                            ParentWindow.MessageBoxError($"Row {row + 1} have invalid number of pixels, the pattern must have equal pixel count per line, per defined on line 1");
                            return false;
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
                            ParentWindow.MessageBoxError($"{bytes[col]} is a invalid number, use values from 0 to 255");
                            return false;
                        }
                    }
                }
            }

            Operation.EvenPattern = stringMatrix[0].Pattern;
            Operation.OddPattern = stringMatrix[1].Pattern;

            return true;
        }

        public void GeneratePixelDimming(string pattern)
        {
            if (pattern == "Chessboard")
            {
                PatternText = string.Format(
                    "255 {0}{1}" +
                    "{0} 255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "{0} 255{1}" +
                    "255 {0}"
                    , _dimGenBrightness, "\n");

                return;
            }

            if (pattern == "Sparse")
            {
                PatternText = string.Format(
                    "{0} 255 255 255{1}" +
                    "255 255 {0} 255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0} 255{1}" +
                    "{0} 255 255 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Crosses")
            {
                PatternText = string.Format(
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 255 255 255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Strips")
            {
                PatternText = string.Format(
                    "{0}{1}" +
                    "255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255{1}" +
                    "{0}"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Pyramid")
            {
                PatternText = string.Format(
                    "255 255 {0} 255 255 255{1}" +
                    "255 {0} 255 {0} 255 255{1}" +
                    "{0} 255 {0} 255 {0} 255{1}" +
                    "255 255 255 255 255 255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255{1}" +
                    "255 255 255 255 255 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Rhombus")
            {
                PatternText = string.Format(
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255{1}" +
                    "255 255 255 255"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255{1}" +
                    "255 {0} 255 255{1}" +
                    "{0} 255 {0} 255{1}" +
                    "255 {0} 255 255"
                    , _dimGenBrightness, "\n");
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
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 255 255 255 255{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 {0} 255 {0} 255 {0}{1}" +
                    "255 {0} 255 255 255 {0}{1}" +
                    "255 255 {0} 255 {0} 255{1}" +
                    "255 255 255 {0} 255 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Slashes")
            {
                PatternText = string.Format(
                    "{0} 255 255{1}" +
                    "255 {0} 255{1}" +
                    "255 255 {0}"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0}{1}" +
                    "255 {0} 255{1}" +
                    "{0} 255 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Waves")
            {
                PatternText = string.Format(
                    "{0} 255 255{1}" +
                    "255 255 {0}"
                    , _dimGenBrightness, "\n");

                AlternatePatternText = string.Format(
                    "255 255 {0}{1}" +
                    "{0} 255 255"
                    , _dimGenBrightness, "\n");
                return;
            }

            if (pattern == "Solid")
            {
                PatternText = _dimGenBrightness.ToString();
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
        }

    }
}
