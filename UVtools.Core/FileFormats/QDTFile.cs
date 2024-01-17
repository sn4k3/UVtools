/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Diagnostics;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.IO;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class QDTFile : FileFormat
{
    #region Constants

    /// <summary>
    /// The file header<br/>
    /// {0} = layer thickness in microns<br/>
    /// {1} = ResolutionX<br/>
    /// {2} = ResolutionY<br/>
    /// </summary>
    public const string FileHeader = "JieHe,{0},{2},{1},2,019,0,FA";

    #endregion

    #region Enums

    public enum QDTFileLineExpect
    {
        /// <summary>
        /// JieHe,50,4000,8000,2,019,0,FA
        /// </summary>
        Header,
        /// <summary>
        /// Layer number 1 started or End of layer definitions
        /// </summary>
        LayerNumberOrFD,
        /// <summary>
        /// Layer lines start
        /// </summary>
        FB,
        /// <summary>
        /// Layer lines end
        /// </summary>
        FC,
        /// <summary>
        /// End of layer definitions
        /// </summary>
        FD,
        /// <summary>
        /// Number of layers in the file
        /// </summary>
        LayerCount,
        /// <summary>
        /// End of the file completed
        /// </summary>
        End
    }

    #endregion

    #region Properties

    public override FileFormatType FileType => FileFormatType.Text;

    public override PrinterManufacturingProcess ManufacturingProcess => PrinterManufacturingProcess.SLA;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(QDTFile), "qdt", "Emake3D Galaxy 1 (QDT)"),
    };

    /*public override Size[] ThumbnailsOriginalSize { get; } =
    {
        new(800, 400), 
    };*/

    public override FlipDirection DisplayMirror
    {
        get => FlipDirection.Horizontally;
        set {}
    }

    public override bool SupportAntiAliasing => false;

    #endregion

    #region Constructors
    public QDTFile()
    {
        ResolutionX = 8000;
        ResolutionY = 4000;
        DisplayWidth = 400;
        DisplayHeight = 200;
        MachineZ = 400;
        MachineName = "Emake3D Galaxy 1";
    }
    #endregion

    #region Methods
    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new StreamWriter(TemporaryOutputFileFullPath);

        // Header
        outputFile.WriteLine(FileHeader, LayerHeightUm, ResolutionX, ResolutionY);

        var layersLines = new List<GreyLine>[LayerCount, 2];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();

                var layer = this[layerIndex];

                if (layer.IsEmpty)
                {
                    layersLines[layerIndex, 0] = new List<GreyLine>();
                    layersLines[layerIndex, 1] = layersLines[layerIndex, 0];
                }
                else
                {
                    var mirror = layerIndex % 2 != 0;

                    using var matRoiModel = layer.LayerMatModelBoundingRectangle;
                    using var matResize = new Mat();
                    CvInvoke.Resize(matRoiModel.SourceMat, matResize, default, 0.1, 0.1);

                    using var matRoiLayer = matRoiModel.SourceMat.Roi(layer.BoundingRectangle);
                    CvInvoke.Threshold(matRoiLayer, matRoiLayer, 127, 255, ThresholdType.Binary);

                    if (mirror)
                    {
                        CvInvoke.Flip(matRoiModel.SourceMat, matRoiModel.SourceMat, FlipType.Horizontal);
                        layersLines[layerIndex, 0] = matRoiModel.SourceMat.ScanLines(true, 127, new Point(2, 0));
                    }
                    else
                    {
                        layersLines[layerIndex, 0] = matRoiModel.RoiMat.ScanLines(true, 127, matRoiModel.RoiLocation);
                    }

                    layersLines[layerIndex, 1] = matResize.ScanLines(true, 127);
                }


                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();
                
                outputFile.WriteLine(layerIndex + 1);

                for (int j = 1; j >= 0; j--)
                {
                    if (j == 0)
                    {
                        outputFile.WriteLine("FB");
                    }

                    var layerLines = layersLines[layerIndex, j];

                    if (layerLines.Count == 0) continue;

                    var currentLine = layerLines[0];

                    // First line
                    outputFile.WriteLine($"{currentLine.StartX},{currentLine.StartY},0");
                    outputFile.WriteLine($"{currentLine.StartX},{currentLine.EndY - currentLine.StartY},1");

                    for (int i = 1; i < layerLines.Count; i++)
                    {
                        var previousLine = layerLines[i - 1];
                        currentLine = layerLines[i];
                        if (currentLine.StartX == previousLine.StartX)
                        {
                            // Partial position increment
                            outputFile.WriteLine($"{currentLine.StartX},{currentLine.StartY - previousLine.EndY},0");
                        }
                        else
                        {
                            // Absolute position
                            outputFile.WriteLine($"{previousLine.StartX},{previousLine.StartY},0");
                            outputFile.WriteLine($"{currentLine.StartX},{currentLine.StartY},0");
                        }

                        outputFile.WriteLine($"{currentLine.StartX},{currentLine.EndY - currentLine.StartY},1");
                    }

                    // End lines
                    outputFile.WriteLine($"{currentLine.StartX},{currentLine.EndY},0");
                }

                // End layer
                outputFile.WriteLine("FC");

                layersLines[layerIndex, 0] = null!; // Free this
                layersLines[layerIndex, 1] = null!; // Free this
            }
        }

        // End file
        outputFile.WriteLine("FD");
        outputFile.WriteLine(LayerCount);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new StreamReader(FileFullPath!);

        var line = inputFile.ReadLine()?.Trim();

        if (line is null)
        {
            throw new FileLoadException("Can not decode the the file: The file it's empty");
        }

        var splitLine = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Header
        if (splitLine.Length < 8) throw new FileLoadException($"Can not decode the the file: Invalid header, got: {line}");

        if (splitLine[0] != "JieHe") throw new FileLoadException($"Can not decode the the file: Expecting <JieHe>, got <{splitLine[0]}>");

        if (ushort.TryParse(splitLine[1], out var layerHeightUm)) LayerHeightUm = layerHeightUm;
        else throw new FileLoadException($"Can not decode the the file: Expecting LayerHeightUm on header, but got: {splitLine[1]}");

        if (ushort.TryParse(splitLine[2], out var resolutionY)) ResolutionY = resolutionY;
        else throw new FileLoadException($"Can not decode the the file: Expecting ResolutionX on header, but got: {splitLine[3]}");

        if (ushort.TryParse(splitLine[3], out var resolutionX)) ResolutionX = resolutionX;
        else throw new FileLoadException($"Can not decode the the file: Expecting ResolutionX on header, but got: {splitLine[2]}");

        if (!ushort.TryParse(splitLine[4], out var unknown1)) throw new FileLoadException($"Can not decode the the file: Expecting a number(2) on header, got <{splitLine[4]}>");
        if (!ushort.TryParse(splitLine[5], out var unknown2)) throw new FileLoadException($"Can not decode the the file: Expecting a number(019) on header, got <{splitLine[5]}>");
        if (!ushort.TryParse(splitLine[6], out var unknown3)) throw new FileLoadException($"Can not decode the the file: Expecting a number(0) on header, got <{splitLine[6]}>");

        if (splitLine[7] != "FA") throw new FileLoadException($"Can not decode the the file: Expecting \"FA\" on header, got <{splitLine[7]}>");

        var reverseFile = new ReverseLineReader(FileFullPath!);

        int reverseIndex = 0;
        uint layerCount = 0;
        foreach (var reverseLine in reverseFile)
        {
            var reverseLineTrim = reverseLine.Trim();
            if (reverseLineTrim == "FD") break; // Need to find FD
            reverseIndex++;

            uint.TryParse(reverseLineTrim, out layerCount);

            if (reverseIndex > 10) break;
        }

        if (layerCount == 0)
        {
            throw new FileLoadException("Can not decode the the file: Unable to fetch the layer count of file end");
        }

        Init(layerCount, true);
        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

        if (DecodeType == FileDecodeType.Partial) return;

        var layerCoords = new List<ushort[]>[layerCount];
        var expecting = QDTFileLineExpect.LayerNumberOrFD;
        int lastProcessedLayerIndex = -1;
        uint currentLayerIndex = 0;
        var processBatchCount = DefaultParallelBatchCount;
        var currentBatch = 0;

        List<ushort[]> currentLayer = null!;

        void ProcessBatch()
        {
            lastProcessedLayerIndex++;

            if (lastProcessedLayerIndex > currentLayerIndex) return;

            Parallel.For(lastProcessedLayerIndex, currentLayerIndex + 1, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();

                using var mat = CreateMat();

                if (layerCoords[layerIndex].Count > 0)
                {
                    Guard.IsEqualTo((int)layerCoords[layerIndex][0][2], 0);
                    int zerosInRow = 1;

                    var coords = layerCoords[layerIndex][0];
                    
                    var startPoint = new Point(coords[0], coords[1]);
                    var endPoint = startPoint;

                    var mirror = layerIndex % 2 != 0;

                    for (var i = 1; i < layerCoords[layerIndex].Count; i++)
                    {
                        coords = layerCoords[layerIndex][i];
                        if (coords[2] == 0)
                        {
                            zerosInRow++;
                            continue;
                        }

                        Guard.IsBetweenOrEqualTo(zerosInRow, 1, 2);

                        var lastCoords = layerCoords[layerIndex][i - 1];

                        if (zerosInRow == 1)
                        {
                            if (i > 1) startPoint = new Point(coords[0], endPoint.Y + lastCoords[1]);
                        }
                        else
                        {
                            startPoint = new Point(coords[0], lastCoords[1]);
                        }

                        endPoint = new Point(coords[0], startPoint.Y + coords[1]);

                        if (mirror)
                        {
                            CvInvoke.Line(mat, 
                                startPoint with { X = (int)(ResolutionX - startPoint.X + 1) },
                                endPoint with { X = (int)(ResolutionX - endPoint.X + 1) },
                                EmguExtensions.WhiteColor);
                        }
                        else
                        {
                            CvInvoke.Line(mat, startPoint, endPoint, EmguExtensions.WhiteColor);
                        }

                        zerosInRow = 0;
                    }
                }

                layerCoords[layerIndex] = null!; // Clean
                _layers[layerIndex].LayerMat = mat;

                progress.LockAndIncrement();
            });

            currentBatch = 0;
            lastProcessedLayerIndex = (int)currentLayerIndex;
        }

        while ((line = inputFile.ReadLine()) is not null)
        {
            progress.PauseOrCancelIfRequested();

            line = line.Trim();

            splitLine = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (splitLine.Length == 1) // Layer Number, FB, FC, FD
            {
                if (splitLine[0] == "FB")
                {
                    if (expecting != QDTFileLineExpect.FB) throw new FileLoadException($"Error while decoding the line: Was expecting <{expecting}>, got <{splitLine[0]}>.");
                    expecting = QDTFileLineExpect.FC;
                    continue;
                }

                if (splitLine[0] == "FC")
                {
                    if (expecting != QDTFileLineExpect.FC) throw new FileLoadException($"Error while decoding the line: Was expecting <{expecting}>, got <{splitLine[0]}>.");
                    expecting = QDTFileLineExpect.LayerNumberOrFD;
                    currentBatch++;
                    if (currentBatch >= processBatchCount)
                    {
                        ProcessBatch();
                    }
                    continue;
                }

                if (splitLine[0] == "FD")
                {
                    if (expecting != QDTFileLineExpect.LayerNumberOrFD) throw new FileLoadException($"Error while decoding the line: Was expecting <{expecting}>, got <{splitLine[0]}>.");
                    expecting = QDTFileLineExpect.LayerCount;
                    if (layerCount != currentLayerIndex + 1) throw new FileLoadException($"Error while decoding the file: It was expected <{layerCount}> layers, got <{currentLayerIndex + 1}>.");
                    ProcessBatch();
                    continue;
                }

                if (!uint.TryParse(splitLine[0], out currentLayerIndex)) throw new FileLoadException($"Error while decoding the line: Was expecting an layer number, got <{splitLine[0]}>.");

                if (expecting == QDTFileLineExpect.LayerNumberOrFD)
                {
                    expecting = QDTFileLineExpect.FB;
                    if (currentLayerIndex == 0) throw new FileLoadException($"Error while decoding the line: Was expecting an positive layer number, got <{splitLine[0]}>.");
                    currentLayerIndex--;
                    currentLayer = new List<ushort[]>();

                    if (layerCoords[currentLayerIndex] is not null) throw new FileLoadException($"Error while creating the layer {currentLayerIndex}: It already exists on the collection.");
                    layerCoords[currentLayerIndex] = currentLayer;

                    continue;
                }

                if (expecting == QDTFileLineExpect.LayerCount)
                {
                    if (layerCount != currentLayerIndex) throw new FileLoadException($"Error while decoding the file: The prior fetch of the layer count of <{layerCount}>, mismatch the current fetch of <{currentLayer}>.");
                    expecting = QDTFileLineExpect.End;
                    break;
                }

                continue;
            }

            if (splitLine.Length == 3)
            {
                if (expecting == QDTFileLineExpect.FB) continue; // Thumbnail area
                if (expecting != QDTFileLineExpect.FC) throw new FileLoadException($"Error while decoding the line <{line}>: Was expecting <FC>, got <{expecting}>.");

                if (!ushort.TryParse(splitLine[0], out var x)) throw new FileLoadException($"Error while decoding the line: Was expecting X coordinate, got <{splitLine[0]}>.");
                if (!ushort.TryParse(splitLine[1], out var y)) throw new FileLoadException($"Error while decoding the line: Was expecting Y coordinate, got <{splitLine[1]}>.");
                if (!byte.TryParse(splitLine[2], out var onFlag) || onFlag > 1) throw new FileLoadException($"Error while decoding the line: Was expecting <0/1> flag, got <{splitLine[2]}>.");

                currentLayer.Add(new[] { x, y, onFlag });

                continue;
            }
            
        }

        if (expecting != QDTFileLineExpect.End) throw new FileLoadException($"Error while decoding the file: The file end was expected but got: <{expecting}>");
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        Encode(progress);
    }

    #endregion
}