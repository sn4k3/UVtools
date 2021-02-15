using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Operations;
using Size = System.Drawing.Size;

namespace UVtools.Core.FileFormats
{
    public class ImageFile : FileFormat
    {
        public override FileFormatType FileType { get; } = FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } =
        {
            new ("jpg", "JPG"),
            new ("jpeg", "JPEG"),
            new ("png", "PNG"),
            new ("bmp", "BMP"),
            new ("gif", "GIF"),
            new ("tga", "TGA"),
        };
        public override PrintParameterModifier[] PrintParameterModifiers { get; } = null;
        public override byte ThumbnailsCount { get; } = 4;
        public override Size[] ThumbnailsOriginalSize { get; } = null;
        public override uint ResolutionX
        {
            get => (uint) ImageMat.Width;
            set => throw new NotImplementedException();
        }

        public override uint ResolutionY
        {
            get => (uint) ImageMat.Height;
            set => throw new NotImplementedException();
        }

        public override float DisplayWidth
        {
            get => ResolutionX;
            set
            {
                ResolutionX = (uint) value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => ResolutionY;
            set
            {
                ResolutionY = (uint) value;
                RaisePropertyChanged();
            }
        }

        public override bool MirrorDisplay
        {
            get => false;
            set { }
        }

        public override byte AntiAliasing
        {
            get => 1;
            set { }
        }

        public override float LayerHeight { get; set; } = 0.01f;
        /*public override float PrintTime { get; } = 0;
        public override float UsedMaterial { get; } = 0;
        public override float MaterialCost { get; } = 0;
        public override string MaterialName { get; } = null;
        public override string MachineName { get; } = null;*/
        public override object[] Configs { get; } = null;

        private Mat ImageMat { get; set; }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            throw new NotSupportedException();
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            ImageMat = CvInvoke.Imread(fileFullPath, ImreadModes.Grayscale);
            const byte startDivisor = 2;
            for (int i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = new Mat();
                var divisor = (i + 1) * startDivisor;
                CvInvoke.Resize(ImageMat, Thumbnails[i],
                    new Size(ImageMat.Width / divisor, ImageMat.Height / divisor));
            }

            /*if (ImageMat.NumberOfChannels > 1)
            {
                CvInvoke.CvtColor(ImageMat, ImageMat, ColorConversion.Bgr2Gray);
            }*/

            LayerManager = new LayerManager(1, this);
            this[0] = new Layer(0, ImageMat, LayerManager);
        }

        public override void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            this[0].LayerMat.Save(filePath ?? FileFullPath);
        }

        public override FileFormat Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            throw new NotSupportedException();
        }

        
    }
}
