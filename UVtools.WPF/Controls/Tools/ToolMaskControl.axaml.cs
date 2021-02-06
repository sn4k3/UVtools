using System;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolMaskControl : ToolControl
    {
        private bool _isMaskInverted;
        private byte _genMinimumBrightness = 200;
        private byte _genMaximumBrightness = byte.MaxValue;
        private uint _genDiameter;
        private Bitmap _maskImage;
        public OperationMask Operation => BaseOperation as OperationMask;

        public bool IsMaskInverted
        {
            get => _isMaskInverted;
            set
            {
                if(!RaiseAndSetIfChanged(ref _isMaskInverted, value)) return;
                if (!Operation.HaveMask) return;
                Operation.InvertMask();
                MaskImage = Operation.Mask.ToBitmap();
            }
        }

        public byte GenMinimumBrightness
        {
            get => _genMinimumBrightness;
            set => RaiseAndSetIfChanged(ref _genMinimumBrightness, value);
        }

        public byte GenMaximumBrightness
        {
            get => _genMaximumBrightness;
            set => RaiseAndSetIfChanged(ref _genMaximumBrightness, value);
        }

        public uint GenDiameter
        {
            get => _genDiameter;
            set => RaiseAndSetIfChanged(ref _genDiameter, value);
        }

        public string InfoPrinterResolutionStr => $"Printer resolution: {App.SlicerFile.Resolution}";
        public string InfoMaskResolutionStr => $"Mask resolution: "+ (Operation.HaveMask ? Operation.Mask.Size.ToString() : "(Unloaded)");

        public Bitmap MaskImage
        {
            get => _maskImage;
            set
            {
                if(!RaiseAndSetIfChanged(ref _maskImage, value)) return;
                RaisePropertyChanged(nameof(InfoMaskResolutionStr));
                ParentWindow.ButtonOkEnabled = Operation.HaveMask;
            }
        }

        public ToolMaskControl()
        {
            InitializeComponent();
            BaseOperation = new OperationMask(SlicerFile);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void Callback(ToolWindow.Callbacks callback)
        {
            switch (callback)
            {
                case ToolWindow.Callbacks.Init:
                    ParentWindow.ButtonOkEnabled = false;
                    break;
                case ToolWindow.Callbacks.ClearROI:
                    Operation.Mask = null;
                    MaskImage = null;
                    break;
            }
        }

        public async void ImportImageMask()
        {
            var dialog = new OpenFileDialog
            {
                Filters = Helpers.ImagesFileFilter,
                AllowMultiple = false
            };

            var result = await dialog.ShowAsync(ParentWindow);
            if (result is null || result.Length == 0) return;

            try
            {
                Operation.Mask = CvInvoke.Imread(result[0], ImreadModes.Grayscale);
                var roi = App.MainWindow.ROI;
                if (roi.IsEmpty)
                {
                    if (Operation.Mask.Size != App.SlicerFile.Resolution)
                    {
                        CvInvoke.Resize(Operation.Mask, Operation.Mask, App.SlicerFile.Resolution);
                    }
                }
                else
                {
                    if (Operation.Mask.Size != roi.Size)
                    {
                        CvInvoke.Resize(Operation.Mask, Operation.Mask, roi.Size);
                    }
                }

                if (_isMaskInverted)
                {
                    Operation.InvertMask();
                }

                MaskImage = Operation.Mask.ToBitmap();
            }
            catch (Exception e)
            {
                await ParentWindow.MessageBoxError(e.ToString(), "Error while trying to read the image");
            }
        }

        public void GenerateMask()
        {
            var roi = App.MainWindow.ROI;
            Operation.Mask = roi.IsEmpty ? App.MainWindow.LayerCache.Image.CloneBlank() : new Mat(roi.Size, DepthType.Cv8U, 1);

            int radius = (int)_genDiameter;
            if (radius == 0)
            {
                radius = Math.Min(Operation.Mask.Width, Operation.Mask.Height) / 2;
            }
            else
            {
                radius = radius.Clamp(2, Math.Min(Operation.Mask.Width, Operation.Mask.Height)) / 2;
            }

            var maxScalar = new MCvScalar(_genMaximumBrightness);
            Operation.Mask.SetTo(maxScalar);

            var center = new Point(Operation.Mask.Width / 2, Operation.Mask.Height / 2);
            var colorDifference = _genMinimumBrightness - _genMaximumBrightness;
            //CvInvoke.Circle(Mask, center, radius, minScalar, -1);

            for (decimal i = 1; i < radius; i++)
            {
                int color = (int)(_genMinimumBrightness - i / radius * colorDifference); //or some another color calculation
                CvInvoke.Circle(Operation.Mask, center, (int)i, new MCvScalar(color), 2);
            }

            if (_isMaskInverted)
                Operation.InvertMask();
            MaskImage = Operation.Mask.ToBitmap();
        }
    }
}
