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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Operations;


public sealed partial class OperationLayerImport : Operation
{
    private const string ImageFileVirtualFormatPath = "#VIRTUAL";

    #region Enums
    public enum ImportTypes : byte
    {
        [Description("Insert: Inserts the new imported layers")]
        Insert,
        [Description("Replace: Replaces layers with the imported layers")]
        Replace,
        [Description("Stack: Stacks and combine imported layers in the current layers")]
        Stack,
        [Description("MergeSum: Merges current layers with the imported content by summing value of layer pixels")]
        MergeSum,
        [Description("MergeMax: Merges current layers with the imported content by using the maximum value of layer pixels")]
        MergeMax,
        [Description("Subtract: Subtracts current layers with the imported content")]
        Subtract,
        [Description("AbsDiff: Absolute difference between current layers and the imported content")]
        AbsDiff,
        [Description("BitwiseAnd: Perform a 'bitwise AND' operation over layers and the imported pixels")]
        BitwiseAnd,
        [Description("BitwiseOr: Perform a 'bitwise OR' operation over layers and the imported pixels")]
        BitwiseOr,
        [Description("BitwiseXOr: Perform a 'bitwise XOR' operation over layers and the imported pixels")]
        BitwiseXOr,
    }
    #endregion

    #region Members
    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override bool CanROI => false;

    public override string IconClass => "FileImagePlus";

    public override string Title => "Import layers";

    public override string Description =>
        "Import layers from local files into the model at a selected layer height.\n" +
        "NOTE: Imported images must be greyscale and have the same resolution as the model.";

    public override string ConfirmationText => $"{ImportType} import {Count} file{(Count>=1?"s":"")}?";

    public override string ProgressTitle =>
        $"{ImportType} importing {Count} file{(Count>=1 ? "s" : "")}";

    public override string ProgressAction => "Imported layers";

    public override bool CanCancel => true;

    public override uint LayerIndexEnd => StartLayerIndex + Count - 1;

    public override string? ValidateInternally()
    {
        /*var result = new ConcurrentBag<StringTag>();
        Parallel.ForEach(Files,  CoreSettings.ParallelOptions, file =>
        {
            using (Mat mat = CvInvoke.Imread(file.TagString, ImreadModes.AnyColor))
            {
                if (mat.Size != FileResolution)
                {
                    result.Add(file);
                }
            }
        });

        if (result.IsEmpty) return null;
        var message = new StringBuilder();
        message.AppendLine($"The following {result.Count} files mismatched the target resolution of {FileResolution.Width}x{FileResolution.Height}:");
        message.AppendLine();
        uint count = 0;
        foreach (var file in result)
        {
            count++;
            if (count == 20)
            {
                message.AppendLine("... Too many to show ...");
                break;
            }
            message.AppendLine(file.Content);
        }


        return new StringTag(message.ToString(), result);*/

        StringBuilder sb = new();

        if (Files.Count == 0)
        {
            sb.AppendLine("No files to import.");
        }
        else
        {
            foreach (var keyValue in Files)
            {
                if (!keyValue.Exists)
                {
                    sb.AppendLine($"The file '{keyValue.FilePath}' does not exists.");
                }
            }
        }

        return sb.ToString();
    }
    #endregion

    #region Properties

    public static string[] ValidImageExtensions =>
    [
        "png",
        "bmp",
        "jpeg",
        "jpg",
        "gif"
    ];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsImportStackType))]
    [NotifyPropertyChangedFor(nameof(IsExtendBeyondLayerCountVisible))]
    public partial ImportTypes ImportType { get; set; } = ImportTypes.Stack;


    public static Array ImportTypesItems => Enum.GetValues(typeof(ImportTypes));

    public bool IsImportStackType => ImportType == ImportTypes.Stack;

    [ObservableProperty]
    public partial uint StartLayerIndex { get; set; }

    [ObservableProperty]
    public partial bool ExtendBeyondLayerCount { get; set; } = true;

    public bool IsExtendBeyondLayerCountVisible => ImportType is ImportTypes.Replace or ImportTypes.Stack or ImportTypes.MergeSum or ImportTypes.MergeMax;

    [ObservableProperty]
    public partial bool DiscardUnmodifiedLayers { get; set; }

    [ObservableProperty]
    public partial ushort StackMargin { get; set; } = 50;

    [ObservableProperty]
    public partial RangeObservableCollection<GenericFileRepresentation> Files { get; set; } = [];

    public uint Count => (uint)Files.Count;
    #endregion

    #region Constructor

    public OperationLayerImport() { }

    public OperationLayerImport(FileFormat slicerFile) : base(slicerFile) { }


    #endregion

    #region Methods

    public void AddFile(string file)
    {
        Files.Add(new GenericFileRepresentation(file));
    }

    public void Sort()
    {
        Files.Sort();
    }


    /*public uint CalculateTotalLayers(uint totalLayers)
    {
        if (DiscardRemainingLayers)
        {
            return (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
        }
        if (ReplaceSubsequentLayers)
        {
            uint result = (uint) (1 + InsertAfterLayerIndex + Files.Count - (ReplaceStartLayer ? 1 : 0));
            return result <= totalLayers ? totalLayers : result;
        }

        return (uint)(totalLayers + Files.Count - (ReplaceStartLayer ? 1 : 0));
    }*/

    public override string ToString()
    {
        var result = $"[{ImportType}] [Start at: {StartLayerIndex}] [Files: {Count}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = 0;
        var result = SlicerFile.SuppressRebuildPropertiesWork(() => {
            var fileFormats = new List<FileFormat>();
            var keyImage = new List<KeyValuePair<uint, string>>();
            int lastProcessedLayerIndex = -1;

            // Order raw images
            for (int i = 0; i < Count; i++)
            {
                if(!ValidImageExtensions.AsValueEnumerable().Any(extension => Files[i].IsExtension(extension))) continue;
                keyImage.Add(new KeyValuePair<uint, string>((uint)keyImage.Count, Files[i].FilePath));
            }

            // Create virtual file format with images
            if (keyImage.Count > 0)
            {
                progress.Reset("Packing images", (uint)keyImage.Count);
                GenericZIPFile format = new();
                format.Init((uint)keyImage.Count);

                var resolution = Size.Empty;

                Parallel.ForEach(keyImage, CoreSettings.GetParallelOptions(progress), pair =>
                {
                    progress.PauseIfRequested();
                    using var mat = CvInvoke.Imread(pair.Value, ImreadModes.Grayscale);
                    format[pair.Key] = new Layer(pair.Key, mat, format);

                    lock (format.Mutex)
                    {
                        resolution = SizeExtensions.Max(resolution, mat.Size);
                    }

                    progress.LockAndIncrement();
                });

                format.Resolution = resolution;
                format.FileFullPath = "#VIRTUAL";
                fileFormats.Add(format);
            }

            // Order remaining possible file formats that are not images
            for (int i = 0; i < Count; i++)
            {
                if (ValidImageExtensions.AsValueEnumerable().Any(extension => Files[i].IsExtension(extension))) continue;
                var fileFormat = FileFormat.FindByExtensionOrFilePath(Files[i].FilePath, true);
                if (fileFormat is null) continue;
                fileFormat.FileFullPath = Files[i].FilePath;
                fileFormats.Add(fileFormat);
            }

            progress.PauseOrCancelIfRequested();

            if (fileFormats.Count == 0) return false;

            if (ImportType == ImportTypes.Stack)
            {
                new OperationMove(SlicerFile, Anchor.TopLeft).Execute(progress);
            }

            int importedFormats = 0;

            foreach (var fileFormat in fileFormats)
            {
                if (fileFormat.FileFullPath != ImageFileVirtualFormatPath)
                {
                    if (!fileFormat.CanDecode) continue;
                    fileFormat.Decode(fileFormat.FileFullPath, progress);
                }

                var boundingRectangle = SlicerFile.GetBoundingRectangle(progress);
                var fileFormatBoundingRectangle = fileFormat.GetBoundingRectangle(progress);
                var roiRectangle = Rectangle.Empty;

                // Check if is possible to process this file
                switch (ImportType)
                {
                    case ImportTypes.Insert:
                        if (SlicerFile.Resolution != fileFormat.Resolution &&
                            (SlicerFile.Resolution.Width < fileFormatBoundingRectangle.Width ||
                             SlicerFile.Resolution.Height < fileFormatBoundingRectangle.Height)) continue;
                        SlicerFile.ReallocateInsert(StartLayerIndex, fileFormat.LayerCount, fixPositionZ:true);
                        importedFormats++;
                        break;
                    case ImportTypes.Replace:
                    case ImportTypes.Stack:
                        if (SlicerFile.Resolution != fileFormat.Resolution &&
                            (SlicerFile.Resolution.Width < fileFormatBoundingRectangle.Width ||
                             SlicerFile.Resolution.Height < fileFormatBoundingRectangle.Height)) continue;

                        //if(fileFormatBoundingRectangle.Width >= SlicerFile.ResolutionX || fileFormatBoundingRectangle.Height >= SlicerFile.ResolutionY)
                        //    continue;

                        if (ImportType == ImportTypes.Stack)
                        {
                            int x = 0;
                            int y = 0;

                            if (SlicerFile.IsPixelInsideXBounds(boundingRectangle.Right + StackMargin + fileFormatBoundingRectangle.Width))
                            {
                                x = boundingRectangle.Right + StackMargin;
                            }
                            else
                            {
                                y = boundingRectangle.Bottom + StackMargin;
                            }

                            if (!SlicerFile.IsPixelInsideXBounds(x + fileFormatBoundingRectangle.Width))
                                continue;
                            if (!SlicerFile.IsPixelInsideYBounds(y + fileFormatBoundingRectangle.Height))
                                continue;

                            roiRectangle = fileFormatBoundingRectangle with {X = x, Y = y};
                        }

                        if (ExtendBeyondLayerCount)
                        {
                            int layerCountDifference = (int)(StartLayerIndex + fileFormat.LayerCount - SlicerFile.LayerCount);
                            if (layerCountDifference > 0)
                            {
                                SlicerFile.ReallocateEnd((uint)layerCountDifference);
                            }
                        }

                        importedFormats++;
                        break;
                    case ImportTypes.MergeSum:
                    case ImportTypes.MergeMax:
                        if (SlicerFile.Resolution != fileFormat.Resolution) continue;
                        if (ExtendBeyondLayerCount)
                        {
                            int layerCountDifference = (int)(StartLayerIndex + fileFormat.LayerCount - SlicerFile.LayerCount);
                            if (layerCountDifference > 0)
                            {
                                SlicerFile.ReallocateEnd((uint)layerCountDifference);
                            }
                        }
                        importedFormats++;
                        break;
                    case ImportTypes.Subtract:
                    case ImportTypes.AbsDiff:
                    case ImportTypes.BitwiseAnd:
                    case ImportTypes.BitwiseOr:
                    case ImportTypes.BitwiseXOr:
                        if (SlicerFile.Resolution != fileFormat.Resolution) continue;
                        importedFormats++;
                        break;
                }

                progress.Reset(ProgressAction, fileFormat.LayerCount);
                Parallel.For(0, fileFormat.LayerCount, CoreSettings.GetParallelOptions(progress), i =>
                {
                    progress.PauseIfRequested();
                    uint layerIndex = (uint)(StartLayerIndex + i);

                    switch (ImportType)
                    {
                        case ImportTypes.Insert:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            if (SlicerFile.Resolution == fileFormat.Resolution)
                            {
                                fileFormat[i].CopyImageTo(SlicerFile[layerIndex]);
                                break;
                            }

                            using var mat = fileFormat[i].LayerMat;
                            using var matRoi = mat.NewFromRoiToCenter(SlicerFile.Resolution, fileFormatBoundingRectangle);
                            SlicerFile[layerIndex].LayerMat = matRoi;

                            break;
                        }
                        case ImportTypes.Replace:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            if (SlicerFile.Resolution == fileFormat.Resolution)
                            {
                                fileFormat[i].CopyImageTo(SlicerFile[layerIndex]);
                                break;
                            }

                            using var mat = fileFormat[i].LayerMat;
                            using var matRoi = mat.NewFromRoiToCenter(SlicerFile.Resolution, fileFormatBoundingRectangle);
                            SlicerFile[layerIndex].LayerMat = matRoi;
                            break;
                        }
                        case ImportTypes.Stack:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var mat = SlicerFile[layerIndex].LayerMat;
                            using var importMat = fileFormat[i].LayerMat;
                            var matRoi = new Mat(mat, roiRectangle);
                            var importMatRoi = new Mat(importMat, fileFormatBoundingRectangle);
                            importMatRoi.CopyTo(matRoi);
                            SlicerFile[layerIndex].LayerMat = mat;

                            break;
                        }
                        case ImportTypes.MergeSum:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.Add(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.MergeMax:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.Max(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.Subtract:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.Subtract(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.AbsDiff:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.AbsDiff(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.BitwiseAnd:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.BitwiseAnd(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.BitwiseOr:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.BitwiseOr(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        case ImportTypes.BitwiseXOr:
                        {
                            if (layerIndex >= SlicerFile.LayerCount) return;
                            using var originalMat = SlicerFile[layerIndex].LayerMat;
                            using var newMat = fileFormat[i].LayerMat;
                            CvInvoke.BitwiseXor(originalMat, newMat, newMat);
                            SlicerFile[layerIndex].LayerMat = newMat;
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }


                    lock (progress.Mutex)
                    {
                        lastProcessedLayerIndex = Math.Max(lastProcessedLayerIndex, (int)layerIndex);
                        progress++;
                    }
                });

                fileFormat.Dispose();
            }


            if (importedFormats != fileFormats.Count)
            {
                AfterCompleteReport = $"Could not import and fit {fileFormats.Count - importedFormats} out of {fileFormats.Count} format(s) (Insufficient or unequal build space).";
            }

            if (lastProcessedLayerIndex < 0) return false;

            if (ImportType == ImportTypes.Stack)
            {
                new OperationMove(SlicerFile).Execute(progress);
            }



            if (lastProcessedLayerIndex + 1 < SlicerFile.LayerCount && DiscardUnmodifiedLayers)
            {
                SlicerFile.Reallocate((uint)lastProcessedLayerIndex + 1);
            }

            return true;
        });

        return !progress.Token.IsCancellationRequested && result;
    }

    #endregion
}