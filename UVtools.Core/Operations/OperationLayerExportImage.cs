/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerExportImage : Operation
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

        private string _outputFolder;
        private string _filename = "layer";
        private LayerExportImageTypes _imageType = LayerExportImageTypes.PNG;
        private Enumerations.RotateDirection _rotateDirection = Enumerations.RotateDirection.None;
        private Enumerations.FlipDirection _flipDirection = Enumerations.FlipDirection.None;
        private bool _padLayerIndex = true;
        private bool _cropByRoi = true;

        #endregion

        #region Overrides

        public override bool CanHaveProfiles => false;
        public override string Title => "Export layers to images";

        public override string Description =>
            "Export a layer range to images.";

        public override string ConfirmationText =>
            $"export {_imageType} images from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Exporting {_imageType} images from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => $"Exported {_imageType} images";

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
            var result = $"[Crop by ROI: {_cropByRoi}] [Pad index: {_padLayerIndex}]" +
                         LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        public string OutputFolder
        {
            get => _outputFolder;
            set => RaiseAndSetIfChanged(ref _outputFolder, value);
        }

        public string Filename
        {
            get => _filename;
            set => RaiseAndSetIfChanged(ref _filename, value);
        }

        public LayerExportImageTypes ImageType
        {
            get => _imageType;
            set => RaiseAndSetIfChanged(ref _imageType, value);
        }

        public Enumerations.RotateDirection RotateDirection
        {
            get => _rotateDirection;
            set => RaiseAndSetIfChanged(ref _rotateDirection, value);
        }

        public Enumerations.FlipDirection FlipDirection
        {
            get => _flipDirection;
            set => RaiseAndSetIfChanged(ref _flipDirection, value);
        }

        public bool PadLayerIndex
        {
            get => _padLayerIndex;
            set => RaiseAndSetIfChanged(ref _padLayerIndex, value);
        }

        public bool CropByROI
        {
            get => _cropByRoi;
            set => RaiseAndSetIfChanged(ref _cropByRoi, value);
        }

        #endregion

        #region Constructor

        public OperationLayerExportImage()
        { }

        public OperationLayerExportImage(FileFormat slicerFile) : base(slicerFile)
        {
            _outputFolder = Path.Combine(Path.GetDirectoryName(SlicerFile.FileFullPath) ?? string.Empty, FileFormat.GetFileNameStripExtensions(SlicerFile.FileFullPath));
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            if (!Directory.Exists(_outputFolder))
            {
                Directory.CreateDirectory(_outputFolder);
            }

            var slicedFileNameNoExt = SlicerFile.FilenameNoExt;

            Parallel.For(LayerIndexStart, LayerIndexEnd+1, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                using var mat = SlicerFile[layerIndex].LayerMat;
                var matRoi = mat;
                if (_cropByRoi && HaveROI)
                {
                    matRoi = GetRoiOrDefault(mat);
                }

                if (_flipDirection != Enumerations.FlipDirection.None)
                {
                    CvInvoke.Flip(matRoi, matRoi, Enumerations.ToOpenCVFlipType(_flipDirection));
                }

                if (_rotateDirection != Enumerations.RotateDirection.None)
                {
                    CvInvoke.Rotate(matRoi, matRoi, Enumerations.ToOpenCVRotateFlags(_rotateDirection));
                }

                var filename = SlicerFile[layerIndex].FormatFileName(_filename, _padLayerIndex ? SlicerFile.LayerManager.LayerDigits : byte.MinValue, true, string.Empty);
                var fileFullPath = Path.Combine(_outputFolder, $"{filename}.{_imageType.ToString().ToLower()}");

                if (_imageType != LayerExportImageTypes.SVG)
                {
                    matRoi.Save(fileFullPath);
                }
                else
                {
                    // SVG
                    
                    CvInvoke.Threshold(matRoi, matRoi, 127, byte.MaxValue, ThresholdType.Binary); // Remove AA

                    using var contours = new VectorOfVectorOfPoint();
                    using var hierarchy = new Mat();
                    CvInvoke.FindContours(matRoi, contours, hierarchy, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

                    var hierarchyJagged = hierarchy.GetData();
                    
                    using TextWriter tw = new StreamWriter(fileFullPath);
                    tw.WriteLine("<!--");
                    tw.WriteLine($"# Generated by {About.Software} v{About.VersionStr} {About.Arch} @ {DateTime.UtcNow} #");
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
                    
                    //
                    //hierarchy[i][0]: the index of the next contour of the same level
                    //hierarchy[i][1]: the index of the previous contour of the same level
                    //hierarchy[i][2]: the index of the first child
                    //hierarchy[i][3]: the index of the parent
                    //


                    bool firstTime = true;
                    for (int i = 0; i < contours.Size; i++)
                    {
                        if (contours[i].Size == 0) continue;
                        if ((int)hierarchyJagged.GetValue(0, i, 3) == -1) // Top hierarchy
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

                    if(!firstTime) tw.WriteLine("\"/>");

                    // Old method!
                    /*for (int i = 0; i < contours.Size; i++)
                    {
                        if (contours[i].Size == 0) continue;

                        var style = "white";

                        int parentIndex = i;
                        int count = 0;
                        while ((parentIndex = (int)hierarchyJagged.GetValue(0, parentIndex, 3)) != -1)
                        {
                            count++;
                        } 

                        if (count % 2 != 0)
                            style = "black";

                        tw.Write($"\t<path class=\"{style}\" d=\"M{contours[i][0].X} {contours[i][0].Y}");
                        for (int x = 1; x < contours[i].Size; x++)
                        {
                            tw.Write($",L{contours[i][x].X} {contours[i][x].Y}");
                        }
                        tw.WriteLine("Z\"/>");
                    }*/

                    tw.WriteLine("\t</g>");
                    tw.WriteLine("</svg>");
                }
                
                progress.LockAndIncrement();
            });

            return !progress.Token.IsCancellationRequested;
        }

        #endregion

        #region Equality

        private bool Equals(OperationLayerExportImage other)
        {
            return _outputFolder == other._outputFolder && _filename == other._filename && _imageType == other._imageType && _rotateDirection == other._rotateDirection && _flipDirection == other._flipDirection && _padLayerIndex == other._padLayerIndex && _cropByRoi == other._cropByRoi;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationLayerExportImage other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_outputFolder, _filename, (int) _imageType, (int) _rotateDirection, (int) _flipDirection, _padLayerIndex, _cropByRoi);
        }

        #endregion
    }
}
