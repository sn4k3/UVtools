using System;
using System.IO;
using System.Net.Mime;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using Size = System.Drawing.Size;

namespace UVtools.Core
{
    public class ImageFile : FileFormat
    {
        public override FileFormatType FileType { get; } = FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } =
        {
            new FileExtension("jpg", "JPG"),
            new FileExtension("jpeg", "JPEG"),
            new FileExtension("png", "PNG"),
            new FileExtension("bmp", "BMP"),
            new FileExtension("gif", "GIF"),
            new FileExtension("tga", "TGA"),
        };

        public override Type[] ConvertToFormats { get; } = null;
        public override PrintParameterModifier[] PrintParameterModifiers { get; } = null;
        public override byte ThumbnailsCount { get; } = 4;
        public override Size[] ThumbnailsOriginalSize { get; } = null;
        public override uint ResolutionX => (uint)ImageMat.Width;
        public override uint ResolutionY => (uint)ImageMat.Height;
        public override byte AntiAliasing { get; } = 1;
        public override float LayerHeight { get; } = 0;
        public override ushort InitialLayerCount { get; } = 1;
        public override float InitialExposureTime { get; } = 0;
        public override float LayerExposureTime { get; } = 0;
        public override float LiftHeight { get; } = 0;
        public override float RetractSpeed { get; } = 0;
        public override float LiftSpeed { get; } = 0;
        public override float PrintTime { get; } = 0;
        public override float UsedMaterial { get; } = 0;
        public override float MaterialCost { get; } = 0;
        public override string MaterialName { get; } = null;
        public override string MachineName { get; } = null;
        public override object[] Configs { get; } = null;

        private Mat ImageMat { get; set; }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            throw new NotImplementedException();
        }

        public override void Decode(string fileFullPath)
        {
            base.Decode(fileFullPath);
            var grayMat = new Mat();

            ImageMat = CvInvoke.Imread(fileFullPath, ImreadModes.AnyColor);
            const byte startDivisor = 2;
            for (int i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = new Mat();
                var divisor = (i + 1) * startDivisor;
                CvInvoke.Resize(ImageMat, Thumbnails[i],
                    new Size(ImageMat.Width / divisor, ImageMat.Height / divisor));
            }

            CvInvoke.CvtColor(ImageMat, ImageMat, ColorConversion.Bgr2Gray);

            LayerManager = new LayerManager(1);
            this[0] = new Layer(0, ImageMat, Path.GetFileName(fileFullPath));
        }

        public override void SaveAs(string filePath = null)
        {
            this[0].LayerMat.Save(filePath);
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            throw new NotImplementedException();
        }

        
    }
}
