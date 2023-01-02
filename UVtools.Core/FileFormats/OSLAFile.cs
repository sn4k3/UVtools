/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

// https://github.com/sn4k3/UVtools/blob/master/Documentation/osla.md

using BinarySerialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class OSLAFile : FileFormat
{
    #region Constants

    public const string MARKER = "OSLATiCo";
        
    #endregion

    #region Sub Classes
    #region Header
    public class FileDef
    {
        [FieldOrder(0)]
        [FieldLength(8)]
        public string Marker { get; set; } = MARKER;

        [FieldOrder(1)]
        public ushort Version { get; set; } = 0;

        [FieldOrder(2)]
        [FieldLength(20)]
        public string CreatedDateTime { get; set; } = DateTime.UtcNow.ToString("u");

        [FieldOrder(3)]
        [FieldLength(50)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string CreatedBy { get; set; } = About.SoftwareWithVersion;

        [FieldOrder(4)]
        [FieldLength(20)]
        public string ModifiedDateTime { get; set; } = DateTime.UtcNow.ToString("u");

        [FieldOrder(5)]
        [FieldLength(50)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string ModifiedBy { get; set; } = About.SoftwareWithVersion;

        public override string ToString()
        {
            return $"{nameof(Marker)}: {Marker}, {nameof(Version)}: {Version}, {nameof(CreatedDateTime)}: {CreatedDateTime}, {nameof(CreatedBy)}: {CreatedBy}, {nameof(ModifiedDateTime)}: {ModifiedDateTime}, {nameof(ModifiedBy)}: {ModifiedBy}";
        }

        public void Update()
        {
            ModifiedDateTime= DateTime.UtcNow.ToString("u");
            ModifiedBy = About.SoftwareWithVersion;;
        }

        public void Validate()
        {
            if (Marker != MARKER)
            {
                throw new FileLoadException($"Invalid marker: {Marker}, not a valid OSLA file.");
            }
        }
    }


    public class Header
    {
        [FieldOrder(0)] public uint TableSize { get; set; }
        [FieldOrder(1)] public uint ResolutionX { get; set; }
        [FieldOrder(2)] public uint ResolutionY { get; set; }
        [FieldOrder(3)] public float MachineZ { get; set; }
        [FieldOrder(4)] public float DisplayWidth { get; set; }
        [FieldOrder(5)] public float DisplayHeight { get; set; }
        [FieldOrder(6)] public byte DisplayMirror { get; set; } // 0 = No mirror | 1 = Horizontally | 2 = Vertically | 3 = Horizontally+Vertically | >3 = No mirror
        [FieldOrder(7)] [FieldLength(16)] [SerializeAs(SerializedType.TerminatedString)] public string PreviewDataType { get; set; } = "RGB565";
        [FieldOrder(8)] [FieldLength(16)] [SerializeAs(SerializedType.TerminatedString)] public string LayerDataType { get; set; } = "PNG";
        [FieldOrder(9)] public uint PreviewTableSize { get; set; } = 8;
        [FieldOrder(10)] public uint PreviewCount { get; set; }
        [FieldOrder(11)] public float LayerHeight { get; set; } = 0.05f;
        [FieldOrder(12)] public ushort BottomLayersCount { get; set; } = 4;
        [FieldOrder(13)] public uint LayerCount { get; set; }
        [FieldOrder(14)] public uint LayerTableSize { get; set; } = 69;
        [FieldOrder(15)] public uint LayerDefinitionsAddress { get; set; }
        [FieldOrder(16)] public uint GCodeAddress { get; set; }
        [FieldOrder(17)] public uint PrintTime { get; set; }
        [FieldOrder(18)] public float MaterialMilliliters { get; set; }
        [FieldOrder(19)] public float MaterialCost { get; set; }
        [FieldOrder(20)] [FieldLength(50)] [SerializeAs(SerializedType.TerminatedString)] public string? MaterialName { get; set; } = string.Empty;
        [FieldOrder(21)] [FieldLength(50)] [SerializeAs(SerializedType.TerminatedString)] public string MachineName { get; set; } = "Unknown";


        public override string ToString()
        {
            return $"{nameof(TableSize)}: {TableSize}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(MachineZ)}: {MachineZ}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(DisplayMirror)}: {DisplayMirror}, {nameof(PreviewDataType)}: {PreviewDataType}, {nameof(LayerDataType)}: {LayerDataType}, {nameof(PreviewTableSize)}: {PreviewTableSize}, {nameof(PreviewCount)}: {PreviewCount}, {nameof(LayerTableSize)}: {LayerTableSize}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(LayerCount)}: {LayerCount}, {nameof(LayerDefinitionsAddress)}: {LayerDefinitionsAddress}, {nameof(GCodeAddress)}: {GCodeAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(MaterialMilliliters)}: {MaterialMilliliters}, {nameof(MaterialCost)}: {MaterialCost}, {nameof(MaterialName)}: {MaterialName}, {nameof(MachineName)}: {MachineName}";
        }
    }
    #endregion

    #region Custom Table

    public class CustomTable
    {
        [FieldOrder(0)] public uint TableSize { get; set; }

        [FieldOrder(1)] [FieldCount(nameof(TableSize))] public byte[] Bytes { get; set; } = Array.Empty<byte>();

        public override string ToString()
        {
            return $"{nameof(TableSize)}: {TableSize}, {nameof(Bytes)}: {Bytes.Length}";
        }
    }

    #endregion

    #region Preview
    public class Preview
    {
        /// <summary>
        /// Gets the X dimension of the preview image, in pixels. 
        /// </summary>
        [FieldOrder(0)] public ushort ResolutionX { get; set; }

        /// <summary>
        /// Gets the Y dimension of the preview image, in pixels. 
        /// </summary>
        [FieldOrder(1)] public ushort ResolutionY { get; set; }

        /// <summary>
        /// Gets the image length in bytes.
        /// </summary>
        [FieldOrder(2)] public uint ImageLength { get; set; }
        //[FieldOrder(3)] [FieldCount(nameof(ImageLength))] public byte[] ImageData { get; set; }


        public override string ToString()
        {
            return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(ImageLength)}: {ImageLength}";
        }
    }

    #endregion

    #region Layer
    public class LayerDef
    {
        //[FieldOrder(0)] public uint DataAddress { get; set; }
            
        [FieldOrder(1)] public float PositionZ { get; set; }
        [FieldOrder(2)] public float LiftHeight { get; set; }
        [FieldOrder(3)] public float LiftSpeed { get; set; }
        [FieldOrder(4)] public float LiftHeight2 { get; set; }
        [FieldOrder(5)] public float LiftSpeed2 { get; set; }
        [FieldOrder(6)] public float WaitTimeAfterLift { get; set; }
        [FieldOrder(7)] public float RetractSpeed { get; set; }
        [FieldOrder(8)] public float RetractHeight2 { get; set; }
        [FieldOrder(9)] public float RetractSpeed2 { get; set; }
        [FieldOrder(10)] public float WaitTimeBeforeCure { get; set; }
        [FieldOrder(11)] public float ExposureTime { get; set; }
        [FieldOrder(12)] public float WaitTimeAfterCure { get; set; }
        [FieldOrder(13)] public byte LightPWM { get; set; }
        [FieldOrder(14)] public uint BoundingRectangleX { get; set; }
        [FieldOrder(15)] public uint BoundingRectangleY { get; set; }
        [FieldOrder(16)] public uint BoundingRectangleWidth { get; set; }
        [FieldOrder(17)] public uint BoundingRectangleHeight { get; set; }

        //[Ignore] public byte[] ImageData { get; set; }

        public LayerDef()
        {
        }

        public LayerDef(Layer layer)
        {
            PositionZ = layer.PositionZ;
            LiftHeight = layer.LiftHeight;
            LiftSpeed = layer.LiftSpeed;
            LiftHeight2 = layer.LiftHeight2;
            LiftSpeed2 = layer.LiftSpeed2;
            WaitTimeAfterLift = layer.WaitTimeAfterLift;
            RetractSpeed = layer.RetractSpeed;
            RetractHeight2 = layer.RetractHeight2;
            RetractSpeed2 = layer.RetractSpeed2;
            WaitTimeBeforeCure = layer.WaitTimeBeforeCure;
            ExposureTime = layer.ExposureTime;
            WaitTimeAfterCure = layer.WaitTimeAfterCure;
            LightPWM = layer.LightPWM;
            BoundingRectangleX = (uint)layer.BoundingRectangle.X;
            BoundingRectangleY = (uint)layer.BoundingRectangle.Y;
            BoundingRectangleWidth =  (uint)layer.BoundingRectangle.Width;
            BoundingRectangleHeight = (uint)layer.BoundingRectangle.Height;
        }

        public void CopyTo(Layer layer)
        {
            layer.PositionZ = PositionZ;
            layer.LiftHeight = LiftHeight;
            layer.LiftSpeed = LiftSpeed;
            layer.LiftHeight2 = LiftHeight2;
            layer.LiftSpeed2 = LiftSpeed2;
            layer.WaitTimeAfterLift = WaitTimeAfterLift;
            layer.RetractSpeed = RetractSpeed;
            layer.RetractHeight2 = RetractHeight2;
            layer.RetractSpeed2 = RetractSpeed2;
            layer.WaitTimeBeforeCure = WaitTimeBeforeCure;
            layer.ExposureTime = ExposureTime;
            layer.WaitTimeAfterCure = WaitTimeAfterCure;
            layer.LightPWM = LightPWM;
        }
    }
    #endregion

    #region GCode

    public class GCodeDef
    {
        [FieldOrder(0)]
        public uint GCodeSize { get; set; }

        [FieldOrder(1)] [FieldLength(nameof(GCodeSize))]
        public string? GCodeText { get; set; }
    }
    #endregion

    #endregion

    #region Properties

    public FileDef FileSettings { get; protected internal set; } = new();
    public Header HeaderSettings { get; protected internal set; } = new();
    public CustomTable CustomTableSettings { get; protected internal set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(OSLAFile), "osla", "Open SLA universal binary file (OSLA)"),
        //new ("omsla", "Open mSLA universal binary file"),
        //new ("odlp", "Open DLP universal binary file"),
    };

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } = {
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.TransitionLayerCount,

        PrintParameterModifier.BottomWaitTimeBeforeCure,
        PrintParameterModifier.WaitTimeBeforeCure,

        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.ExposureTime,

        PrintParameterModifier.BottomWaitTimeAfterCure,
        PrintParameterModifier.WaitTimeAfterCure,

        PrintParameterModifier.BottomLiftHeight,
        PrintParameterModifier.BottomLiftSpeed,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,

        PrintParameterModifier.BottomLiftHeight2,
        PrintParameterModifier.BottomLiftSpeed2,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,

        PrintParameterModifier.BottomWaitTimeAfterLift,
        PrintParameterModifier.WaitTimeAfterLift,

        PrintParameterModifier.BottomRetractSpeed,
        PrintParameterModifier.RetractSpeed,

        PrintParameterModifier.BottomRetractHeight2,
        PrintParameterModifier.BottomRetractSpeed2,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,

        PrintParameterModifier.BottomLightPWM,
        PrintParameterModifier.LightPWM,
    };

    public override PrintParameterModifier[]? PrintParameterPerLayerModifiers { get; } = {
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.WaitTimeBeforeCure,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.WaitTimeAfterCure,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,
        PrintParameterModifier.WaitTimeAfterLift,
        PrintParameterModifier.RetractSpeed,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,
        PrintParameterModifier.LightPWM,
    };

    public override Size[]? ThumbnailsOriginalSize { get; } =
    {
        new(400, 400), 
        new(200, 200)
    };

    public override uint ResolutionX
    {
        get => HeaderSettings.ResolutionX;
        set => base.ResolutionX = HeaderSettings.ResolutionX = value;
    }

    public override uint ResolutionY
    {
        get => HeaderSettings.ResolutionY;
        set => base.ResolutionY = HeaderSettings.ResolutionY = value;
    }

    public override float DisplayWidth
    {
        get => HeaderSettings.DisplayWidth;
        set => base.DisplayWidth = HeaderSettings.DisplayWidth = (float)Math.Round(value, 2);
    }


    public override float DisplayHeight
    {
        get => HeaderSettings.DisplayHeight;
        set => base.DisplayHeight = HeaderSettings.DisplayHeight = (float)Math.Round(value, 2);
    }

    public override float MachineZ
    {
        get => HeaderSettings.MachineZ > 0 ? HeaderSettings.MachineZ : base.MachineZ;
        set
        {
            HeaderSettings.MachineZ = (float)Math.Round(value, 2);
            RaisePropertyChanged();
        }
    }

    public override FlipDirection DisplayMirror
    {
        get => (FlipDirection)HeaderSettings.DisplayMirror;
        set
        {
            HeaderSettings.DisplayMirror = (byte)value;
            RaisePropertyChanged();
        }
    }

    public override float LayerHeight
    {
        get => HeaderSettings.LayerHeight;
        set
        {
            HeaderSettings.LayerHeight = Layer.RoundHeight(value);
            RaisePropertyChanged();
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => HeaderSettings.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => HeaderSettings.BottomLayersCount;
        set => base.BottomLayerCount = HeaderSettings.BottomLayersCount = value;
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            HeaderSettings.PrintTime = (uint)base.PrintTime;
        }
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            HeaderSettings.MaterialMilliliters = base.MaterialMilliliters;
        }
    }

    public override float MaterialCost
    {
        get => (float) Math.Round(HeaderSettings.MaterialCost, 3);
        set => base.MaterialCost = HeaderSettings.MaterialCost = (float)Math.Round(value, 3);
    }

    public override string? MaterialName
    {
        get => HeaderSettings.MaterialName;
        set => base.MaterialName = HeaderSettings.MaterialName = value;
    }

    public override string MachineName
    {
        get => HeaderSettings.MachineName;
        set => base.MachineName = HeaderSettings.MachineName = value;
    }

    public override object[] Configs => new object[] { FileSettings, HeaderSettings };

    #endregion

    #region Constructors
    public OSLAFile()
    {
        GCode = new GCodeBuilder
        {
            UseTailComma = true,
            UseComments = true,
            GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Absolute,
            GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
            GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
            GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.LayerIndex0Started,
            LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G0,
            EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G0,
            CommandShowImageM6054 = {Arguments = "{0}"},
        };
    }
    #endregion

    #region Methods
    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);
        FileSettings.Update();
        var fileDefSize = outputFile.WriteSerialize(FileSettings);
        HeaderSettings.TableSize = (uint)Helpers.Serializer.SizeOf(HeaderSettings);

        outputFile.Seek((int)HeaderSettings.TableSize, SeekOrigin.Current);

        outputFile.WriteSerialize(CustomTableSettings); // Custom table

        // Previews
        progress.Reset(OperationProgress.StatusEncodePreviews, ThumbnailsCount);
        HeaderSettings.PreviewCount = 0;
        uint sizeofPreview = 0;
        for (byte i = 0; i < ThumbnailsCount; i++)
        {
            var image = Thumbnails[i];
            if(image is null) continue;

            progress.ThrowIfCancellationRequested();

            var bytes = EncodeImage(HeaderSettings.PreviewDataType, image);
            if (bytes.Length == 0) continue;
            var preview = new Preview
            {
                ResolutionX = (ushort) image.Width,
                ResolutionY = (ushort) image.Height,
                ImageLength = (uint) bytes.Length,
            };

            if (sizeofPreview == 0)
            {
                sizeofPreview = (uint) Helpers.Serializer.SizeOf(preview);
            }

            HeaderSettings.PreviewCount++;

            outputFile.WriteSerialize(preview);
            // Need to fill what we don't know
            if (HeaderSettings.PreviewTableSize > sizeofPreview)
            {
                outputFile.Seek(HeaderSettings.LayerTableSize - sizeofPreview, SeekOrigin.Current);
            }
            outputFile.WriteBytes(bytes);
                
            progress++;
        }

        uint[] layerDataAddresses = new uint[LayerCount];
        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        HeaderSettings.LayerDefinitionsAddress = (uint) outputFile.Position;
            
        outputFile.Seek(HeaderSettings.LayerTableSize * LayerCount, SeekOrigin.Current); // Start of layer data

        var layersHash = new Dictionary<string, uint>();
            
        foreach (var batch in BatchLayersIndexes())
        {
            var layerBytes = new byte[LayerCount][];

            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                using (var mat = this[layerIndex].LayerMat)
                {
                    layerBytes[layerIndex] = EncodeImage(HeaderSettings.LayerDataType, mat);
                }

                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.ThrowIfCancellationRequested();

                // Try to reuse layers
                var hash = CryptExtensions.ComputeSHA1Hash(layerBytes[layerIndex]);
                if (layersHash.TryGetValue(hash, out var address))
                {
                    layerDataAddresses[layerIndex] = address;
                }
                else
                {
                    layerDataAddresses[layerIndex] = (uint)outputFile.Position;
                    outputFile.WriteUIntLittleEndian((uint)layerBytes[layerIndex].Length);
                    outputFile.WriteBytes(layerBytes[layerIndex]);
                    layersHash.Add(hash, layerDataAddresses[layerIndex]);
                }

                layerBytes[layerIndex] = null!; // Free this
            }
        }

        HeaderSettings.GCodeAddress = (uint)outputFile.Position;

        uint layerTableSize = 0;
        outputFile.Seek(HeaderSettings.LayerDefinitionsAddress, SeekOrigin.Begin);
        for (int layerIndex = 0; layerIndex < layerDataAddresses.Length; layerIndex++)
        {
            progress.ThrowIfCancellationRequested();

            var layer = this[layerIndex];
            var layerdef = new LayerDef(layer);
                
            outputFile.WriteUIntLittleEndian(layerDataAddresses[layerIndex]);
            outputFile.WriteSerialize(layerdef);
            if (layerTableSize == 0)
            {
                layerTableSize = 4 + (uint)Helpers.Serializer.SizeOf(layerdef);
            }
            if (HeaderSettings.LayerTableSize > layerTableSize)
            {
                outputFile.Seek(HeaderSettings.LayerTableSize - layerTableSize, SeekOrigin.Current);
            }
        }

        progress.Reset(OperationProgress.StatusEncodeGcode);
        outputFile.Seek(HeaderSettings.GCodeAddress, SeekOrigin.Begin);
        RebuildGCode();
        var gcodeSettings = new GCodeDef { GCodeText = GCodeStr };
        gcodeSettings.GCodeSize = (uint)(gcodeSettings.GCodeText?.Length ?? 0);
        outputFile.WriteSerialize(gcodeSettings);

        outputFile.Seek(fileDefSize, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(FileSettings);
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine("-End-");
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        FileSettings = Helpers.Deserialize<FileDef>(inputFile);
        Debug.Write("File -> ");
        Debug.WriteLine(FileSettings);
        FileSettings.Validate();

        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        Debug.Write("Header -> ");
        Debug.WriteLine(HeaderSettings);

        var headerSize = Helpers.Serializer.SizeOf(HeaderSettings);
        if (HeaderSettings.TableSize > headerSize) // By pass what we dont know
        {
            inputFile.Seek(HeaderSettings.TableSize - headerSize, SeekOrigin.Current);
        }

        CustomTableSettings = Helpers.Deserialize<CustomTable>(inputFile);
        Debug.Write("Custom table -> ");
        Debug.WriteLine(CustomTableSettings);

        progress.Reset(OperationProgress.StatusDecodePreviews, ThumbnailsCount);

        for (byte i = 0; i < HeaderSettings.PreviewCount; i++)
        {
            progress.ThrowIfCancellationRequested();
            var preview = Helpers.Deserialize<Preview>(inputFile);

            Debug.Write($"Preview {i} -> ");
            Debug.WriteLine(preview);

            // Need to fill what we don't know
            if (HeaderSettings.PreviewTableSize > 8)
            {
                inputFile.Seek(HeaderSettings.LayerTableSize - 8, SeekOrigin.Current);
            }

            var bytes = inputFile.ReadBytes((int)preview.ImageLength);

            Thumbnails[i] = DecodeImage(HeaderSettings.PreviewDataType, bytes, preview.ResolutionX, preview.ResolutionY);
            progress++;
        }

        inputFile.Seek(HeaderSettings.LayerDefinitionsAddress, SeekOrigin.Begin);

        Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);
        var layerDef = new LayerDef[LayerCount];


        progress.Reset(OperationProgress.StatusGatherLayers, HeaderSettings.LayerCount);
        uint[] layerDataAddresses = new uint[LayerCount];
        uint layerTableSize = 0;
        for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
        {
            progress.ThrowIfCancellationRequested();
            layerDataAddresses[layerIndex] = inputFile.ReadUIntLittleEndian();

            layerDef[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
            if (layerTableSize == 0)
            {
                layerTableSize = 4 + (uint)Helpers.Serializer.SizeOf(layerDef[layerIndex]);
            }
            if (HeaderSettings.LayerTableSize > layerTableSize)
            {
                inputFile.Seek(HeaderSettings.LayerTableSize - layerTableSize, SeekOrigin.Current);
            }

            progress++;
        }

            
        if (DecodeType == FileDecodeType.Full)
        {
            progress.Reset(OperationProgress.StatusDecodeLayers, HeaderSettings.LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                var layerBytes = new byte[LayerCount][];

                foreach (var layerIndex in batch)
                {
                    progress.ThrowIfCancellationRequested();

                    inputFile.Seek(layerDataAddresses[layerIndex], SeekOrigin.Begin);
                    layerBytes[layerIndex] = inputFile.ReadBytes(inputFile.ReadUIntLittleEndian());
                }

                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    using var mat = DecodeImage(HeaderSettings.LayerDataType, layerBytes[layerIndex], Resolution);
                    layerBytes[layerIndex] = null!; // Clean

                    _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);

                    progress.LockAndIncrement();
                });
            }
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            layerDef[layerIndex].CopyTo(this[layerIndex]);
        }

        progress.Reset(OperationProgress.StatusDecodeGcode);
        inputFile.Seek(HeaderSettings.GCodeAddress, SeekOrigin.Begin);
        var gcodeDef = Helpers.Deserialize<GCodeDef>(inputFile);
        GCodeStr = gcodeDef.GCodeText;
        //GCode.ParseLayersFromGCode(this);

        UpdateGlobalPropertiesFromLayers();
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(0, SeekOrigin.Begin);
        FileSettings.Update();
        outputFile.WriteSerialize(FileSettings);
        outputFile.WriteSerialize(HeaderSettings);

        outputFile.Seek(HeaderSettings.LayerDefinitionsAddress, SeekOrigin.Begin);
        foreach (var layer in this)
        {
            outputFile.Seek(4, SeekOrigin.Current); // skip address
            outputFile.WriteSerialize(new LayerDef(layer)); // Update layer values
        }

        if (HeaderSettings.GCodeAddress > 0)
        {
            outputFile.Seek(HeaderSettings.GCodeAddress, SeekOrigin.Begin);
            outputFile.SetLength(HeaderSettings.GCodeAddress);

            RebuildGCode();
            var gcodeSettings = new GCodeDef { GCodeText = GCodeStr };
            gcodeSettings.GCodeSize = (uint)(gcodeSettings.GCodeText?.Length ?? 0);
            outputFile.WriteSerialize(gcodeSettings);
        }
    }
    #endregion

    #region Static Methods

        


    #endregion
}