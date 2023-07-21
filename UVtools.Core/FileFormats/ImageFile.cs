using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using Size = System.Drawing.Size;

namespace UVtools.Core.FileFormats;

public sealed class ImageFile : FileFormat
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
        new (typeof(ImageFile), "ras", "RAS: Sun raster"),
    };
    
    public override float DisplayWidth
    {
        get => ResolutionX;
        set => base.DisplayWidth = value;
    }

    public override float DisplayHeight
    {
        get => ResolutionY;
        set => base.DisplayHeight = value;
    }

    public override float LayerHeight { get; set; } = 0.01f;

    protected override void EncodeInternally(OperationProgress progress)
    {
        FirstLayer?.LayerMat.Save(TemporaryOutputFileFullPath);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var mat = CvInvoke.Imread(FileFullPath, ImreadModes.Grayscale);
        
        const byte startDivisor = 2;
        for (int i = 0; i < 4; i++)
        {
            var thumbnail = new Mat();
            var divisor = (i + 1) * startDivisor;
            CvInvoke.Resize(mat, thumbnail, new Size(mat.Width / divisor, mat.Height / divisor));
            Thumbnails.Add(thumbnail);
        }
        
        /*if (ImageMat.NumberOfChannels > 1)
        {
            CvInvoke.CvtColor(ImageMat, ImageMat, ColorConversion.Bgr2Gray);
        }*/

        Init(1);
        this[0] = new Layer(0, mat, this);
        Resolution = mat.Size;
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        EncodeInternally(progress);
    }

    public override FileFormat Convert(Type to, string fileFullPath, uint version = 0, OperationProgress? progress = null)
    {
        throw new NotSupportedException();
    }

        
}