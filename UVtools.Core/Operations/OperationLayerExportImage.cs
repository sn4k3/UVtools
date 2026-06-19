/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationLayerExportImage : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums

    public enum LayerExportImageTypes : byte
    {
        [Description("PNG: Portable Network Graphics")]
        PNG,
        [Description("JPG: Joint Photographic Experts Group")]
        JPG,
        [Description("JPEG: Joint Photographic Experts Group")]
        JPEG,
        [Description("JP2: Joint Photographic Experts Group (JPEG 2000)")]
        JP2,
        [Description("TIF: Tag Image File Format")]
        TIF,
        [Description("TIFF: Tag Image File Format")]
        TIFF,
        [Description("BMP: Bitmap")]
        BMP,
        [Description("PBM: Portable Bitmap")]
        PBM,
        [Description("PGM: Portable Greymap")]
        PGM,
        //[Description("PPM: Portable Pixmap")]
        //PPM,
        [Description("SR: Sun raster")]
        SR,
        [Description("RAS: Sun raster")]
        RAS,
        [Description("SVG: Scalable Vector Graphics")]
        SVG
    }
    #endregion

    #region Members


    #endregion

    #region Overrides

    public override bool CanHaveProfiles => false;

    public override string IconClass => "FileImage";
    public override string Title => "Export layers to images";

    public override string Description =>
        "Export a layer range to images.";

    public override string ConfirmationText =>
        $"export {ImageType} images from layers {LayerIndexStart} through {LayerIndexEnd}?";

    public override string ProgressTitle =>
        $"Exporting {ImageType} images from layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => $"Exported {ImageType} images";

    /*public override string ValidateInternally()
    {
        var sb = new StringBuilder();

        if (LayerRangeCount < 2)
        {
            sb.AppendLine("To generate a heat map at least two layers are required.");
        }

        return sb.ToString();
    }*/

    public override string ToString()
    {
        var result = $"[Crop by ROI: {CropByROI}] [Pad index: {PadLayerIndex}]" +
                     LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial string OutputFolder { get; set; } = null!;

    [ObservableProperty]
    public partial string Filename { get; set; } = "layer";

    [ObservableProperty]
    public partial LayerExportImageTypes ImageType { get; set; } = LayerExportImageTypes.PNG;

    [ObservableProperty]
    public partial RotateDirection RotateDirection { get; set; } = RotateDirection.None;

    [ObservableProperty]
    public partial FlipDirection FlipDirection { get; set; } = FlipDirection.None;

    [ObservableProperty]
    public partial bool PadLayerIndex { get; set; } = true;

    [ObservableProperty]
    public partial bool CropByROI { get; set; } = true;

    #endregion

    #region Constructor

    public OperationLayerExportImage()
    { }

    public OperationLayerExportImage(FileFormat slicerFile) : base(slicerFile)
    {
        FlipDirection = SlicerFile.DisplayMirror;
    }

    public override void InitWithSlicerFile()
    {
        OutputFolder = Path.Combine(Path.GetDirectoryName(SlicerFile.FileFullPath) ?? string.Empty, FileFormat.GetFileNameStripExtensions(SlicerFile.FileFullPath) ?? string.Empty);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (!Directory.Exists(OutputFolder))
        {
            Directory.CreateDirectory(OutputFolder);
        }

        var slicedFileNameNoExt = SlicerFile.FilenameNoExt;

        Parallel.For(LayerIndexStart, LayerIndexEnd+1, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            var matRoi = mat;
            bool needDispose = false;
            if (CropByROI && HaveROI)
            {
                matRoi = GetRoiOrDefault(mat);
                needDispose = true;
            }

            if (FlipDirection != FlipDirection.None)
            {
                CvInvoke.Flip(matRoi, matRoi, (FlipType)FlipDirection);
            }

            if (RotateDirection != RotateDirection.None)
            {
                CvInvoke.Rotate(matRoi, matRoi, (RotateFlags)RotateDirection);
            }

            var filename = SlicerFile[layerIndex].FormatFileName(Filename, PadLayerIndex ? SlicerFile.LayerDigits : byte.MinValue, IndexStartNumber.Zero, string.Empty);
            var fileFullPath = Path.Combine(OutputFolder, $"{filename}.{ImageType.ToString().ToLower()}");

            if (ImageType != LayerExportImageTypes.SVG)
            {
                matRoi.Save(fileFullPath);
            }
            else
            {
                // SVG

                var paths = matRoi.GetSvgPath(ChainApproxMethod.ChainApproxTc89Kcos);

                using TextWriter tw = new StreamWriter(fileFullPath);
                tw.WriteLine("<!--");
                tw.WriteLine($"# Generated by {About.Software} v{About.VersionString} {About.SystemBits} @ {DateTime.UtcNow} #");
                tw.WriteLine($"File: {SlicerFile.Filename}");
                tw.WriteLine($"{SlicerFile[layerIndex].ToString().Replace(", ", "\n")}");
                tw.WriteLine("-->");
                tw.WriteLine("<svg " +
                             "xmlns=\"http://www.w3.org/2000/svg\" " +
                             //"xmlns:xlink=\"http://www.w3.org/1999/xlink\" " +
                             //"version=\"1.1\" " +
                             $"id=\"{slicedFileNameNoExt}_{filename}\" " +
                             $"data-name=\"{slicedFileNameNoExt}_{filename}\" " +
                             //"x=\"0\" " +
                             //"y=\"0\" " +
                             $"width=\"{matRoi.Width}\" " +
                             $"height=\"{matRoi.Height}\" " +
                             $"viewBox=\"0 0 {matRoi.Width} {matRoi.Height}\">");
                tw.WriteLine("\t<defs>");
                tw.WriteLine("\t\t<style>");
                //tw.WriteLine("\t\tsvg { background-color: #000000; }");
                tw.WriteLine("\t\t.background { fill: #000000; }");
                //tw.WriteLine("\t\t.black { fill: #000000; fill-rule: evenodd; }");
                //tw.WriteLine("\t\t.white { fill: #FFFFFF; fill-rule: evenodd; }");
                tw.WriteLine("\t\tpath { fill: #FFFFFF; fill-rule: evenodd; }");
                tw.WriteLine("\t\t</style>");
                tw.WriteLine("\t</defs>");
                tw.WriteLine($"\t<title>{slicedFileNameNoExt} #{layerIndex}</title>");

                tw.WriteLine($"\t<g id=\"layer{layerIndex}\">");
                tw.WriteLine($"\t<rect class=\"background\" width=\"{mat.Width}\" height=\"{mat.Height}\"/>");

                foreach (var path in paths)
                {
                    tw.WriteLine($"\t<path d=\"{path}\"/>");
                }

                /*bool firstTime = true;
                for (int i = 0; i < contours.Size; i++)
                {
                    if (hierarchy[i, EmguContour.HierarchyParent] == -1) // Top hierarchy
                    {
                        if (firstTime)
                        {
                            firstTime = false;
                        }
                        else
                        {
                            tw.WriteLine("\"/>");
                        }

                        tw.Write("\t<path d=\"");
                    }
                    else
                    {
                        tw.Write(" ");
                    }

                    tw.Write($"M {contours[i][0].X} {contours[i][0].Y} L");
                    for (int x = 1; x < contours[i].Size; x++)
                    {
                        tw.Write($" {contours[i][x].X} {contours[i][x].Y}");
                    }
                    tw.Write(" Z");
                }

                if(!firstTime) tw.WriteLine("\"/>");*/


                tw.WriteLine("\t</g>");
                tw.WriteLine("</svg>");

                if (needDispose) matRoi.Dispose();
            }

            progress.LockAndIncrement();
        });

        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    private bool Equals(OperationLayerExportImage other)
    {
        return OutputFolder == other.OutputFolder && Filename == other.Filename && ImageType == other.ImageType && RotateDirection == other.RotateDirection && FlipDirection == other.FlipDirection && PadLayerIndex == other.PadLayerIndex && CropByROI == other.CropByROI;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportImage other && Equals(other);
    }


    #endregion
}