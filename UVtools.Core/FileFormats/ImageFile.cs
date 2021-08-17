using System;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Operations;
using Size = System.Drawing.Size;

namespace UVtools.Core.FileFormats
{
    public class ImageFile : FileFormat
    {
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } =
        {
            new (typeof(ImageFile), "png", "PNG: Portable Network Graphics"),
            new (typeof(ImageFile), "jpg", "JPG: Joint Photographic Experts Group"),
            new (typeof(ImageFile), "jpeg", "JPEG: Joint Photographic Experts Group"),
            new (typeof(ImageFile), "jp2", "JP2: Joint Photographic Experts Group (JPEG 2000)"),
            //new (typeof(ImageFile), "tga", "TGA: Truevision"),
            new (typeof(ImageFile), "tif", "TIF: Tag Image File Format"),
            new (typeof(ImageFile), "tiff", "TIFF: Tag Image File Format"),
            new (typeof(ImageFile), "bmp", "BMP: Bitmap"),
            new (typeof(ImageFile), "pbm", "PBM: Portable Bitmap"),
            new (typeof(ImageFile), "pgm", "PGM: Portable Greymap"),
            //new (typeof(ImageFile), "gif", "GIF"),
            new (typeof(ImageFile), "sr", "SR: Sun raster"),
            new (typeof(ImageFile), "RAS", "RAS: Sun raster"),
        };
        public override PrintParameterModifier[] PrintParameterModifiers => null;

        public override Size[] ThumbnailsOriginalSize { get; } = {
            Size.Empty,
            Size.Empty,
            Size.Empty,
            Size.Empty
        };
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

        public override bool DisplayMirror
        {
            get => false;
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
            LayerManager.Init(1);
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
