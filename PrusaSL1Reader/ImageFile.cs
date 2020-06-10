using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;

namespace PrusaSL1Reader
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
        public override uint ResolutionX => (uint)ImageL8.Width;
        public override uint ResolutionY => (uint)ImageL8.Height;
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

        private Image<L8> ImageL8 { get; set; }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            throw new NotImplementedException();
        }

        public override void Decode(string fileFullPath)
        {
            base.Decode(fileFullPath);

            using (var ms = new MemoryStream())
            {
                using (var fs = File.Open(fileFullPath, FileMode.Open))
                {
                    fs.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);

                    var image = Image.Load(ms);
                    const byte startDivisor = 2;
                    for (int i = 0; i < ThumbnailsCount; i++)
                    {
                        Thumbnails[i] = image.CloneAs<Rgba32>();
                        var divisor = (i + startDivisor);
                        Thumbnails[i].Mutate(o => o.Resize(image.Width / divisor, image.Height / divisor));
                    }
                    image.Mutate(o => o.Grayscale());
                    ImageL8 = image.CloneAs<L8>();

                    LayerManager  = new LayerManager(1);
                    this[0] = new Layer(0, ImageL8, Path.GetFileName(fileFullPath));

                    fs.Close();
                }
            }
        }

        public override void SaveAs(string filePath = null)
        {
            this[0].Image.Save(filePath);
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            throw new NotImplementedException();
        }

        
    }
}
