/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;
using Range = System.Range;

namespace UVtools.Core.FileFormats;

/// <summary>
/// Slicer <see cref="FileFormat"/> representation
/// </summary>
public abstract class FileFormat : BindableBase, IDisposable, IEquatable<FileFormat>, IList<Layer>
{
    #region Constants

    public const SpeedUnit CoreSpeedUnit = SpeedUnit.MillimetersPerMinute;
    public const string TemporaryFileAppend = ".tmp";
    public const ushort ExtraPrintTime = 300;

    private const string ExtractConfigFileName = "Configuration";
    private const string ExtractConfigFileExtension = "ini";


    public const float DefaultLayerHeight = 0.05f;
    public const ushort DefaultBottomLayerCount = 4;
    public const ushort DefaultTransitionLayerCount = 0;

    public const float DefaultBottomExposureTime = 30;
    public const float DefaultExposureTime = 3;

    public const float DefaultBottomLiftHeight = 5;
    public const float DefaultLiftHeight = 5;
    public const float DefaultBottomLiftSpeed = 100;
    public const float DefaultLiftSpeed = 100;

    public const float DefaultBottomLiftHeight2 = 0;
    public const float DefaultLiftHeight2 = 0;
    public const float DefaultBottomLiftSpeed2 = 300;
    public const float DefaultLiftSpeed2 = 300;


    public const float DefaultBottomRetractSpeed = 100;
    public const float DefaultRetractSpeed = 100;
    public const float DefaultBottomRetractHeight2 = 0;
    public const float DefaultRetractHeight2 = 0;
    public const float DefaultBottomRetractSpeed2 = 80;
    public const float DefaultRetractSpeed2 = 80;

    public const byte DefaultBottomLightPWM = 255;
    public const byte DefaultLightPWM = 255;

    public const string DefaultMachineName = "Unknown";

    public const byte MaximumAntiAliasing = 16;

    public const float MinimumLayerHeight = 0.01f;
    public const float MaximumLayerHeight = 0.20f;

    private const ushort QueueTimerPrintTime = 250; // ms

    public const string DATATYPE_PNG = "PNG";
    public const string DATATYPE_JPG = "JPG";
    public const string DATATYPE_JPEG = "JPEG";
    public const string DATATYPE_JP2 = "JP2";
    public const string DATATYPE_BMP = "BMP";
    public const string DATATYPE_TIF = "TIF";
    public const string DATATYPE_TIFF = "TIFF";
    public const string DATATYPE_PPM = "PPM";
    public const string DATATYPE_PMG = "PMG";
    public const string DATATYPE_SR = "SR";
    public const string DATATYPE_RAS = "RAS";

    public const string DATATYPE_RGB555 = "RGB555";
    public const string DATATYPE_RGB565 = "RGB565";
    public const string DATATYPE_RGB555_BE = "RGB555-BE";
    public const string DATATYPE_RGB565_BE = "RGB565-BE";
    public const string DATATYPE_RGB888 = "RGB888";


    public const string DATATYPE_BGR555 = "BGR555";
    public const string DATATYPE_BGR565 = "BGR565";
    public const string DATATYPE_BGR555_BE = "BGR555-BE";
    public const string DATATYPE_BGR565_BE = "BGR565-BE";
    public const string DATATYPE_BGR888 = "BGR888";
    #endregion 

    #region Enums

    /// <summary>
    /// Enumeration of file format types
    /// </summary>
    public enum FileFormatType : byte
    {
        Archive,
        Binary
    }

    /// <summary>
    /// Enumeration of file thumbnail size types
    /// </summary>
    public enum FileThumbnailSize : byte
    {
        Small = 0,
        Large
    }

    public enum TransitionLayerTypes : byte
    {
        /// <summary>
        /// Firmware transition layers are handled by printer firmware
        /// </summary>
        Firmware,

        /// <summary>
        /// Software transition layers are handled by software and written on layer data
        /// </summary>
        Software
    }

    /// <summary>
    /// File decode type
    /// </summary>
    public enum FileDecodeType : byte
    {
        /// <summary>
        /// Decodes all the file information and caches layer images
        /// </summary>
        Full,

        /// <summary>
        /// Decodes only the information in the file and thumbnails, no layer image is read nor cached, fast
        /// </summary>
        Partial,
    }

    /// <summary>
    /// Image data type
    /// </summary>
    public enum FileImageType : byte
    {
        Custom,
        Png8,
        Png24,
        Png32,
        /// <summary>
        /// eg: Nova Bene4
        /// </summary>
        Png24BgrAA,
        /// <summary>
        /// eg: Uniformation GKone
        /// </summary>
        Png24RgbAA,

    }

    #endregion

    #region Sub Classes
    /// <summary>
    /// Available Print Parameters to modify
    /// </summary>
    public class PrintParameterModifier
    {
            
        #region Instances
        public static PrintParameterModifier PositionZ { get; } = new ("Position Z", "Absolute Z position", "mm", 0, 100000, 0.01, Layer.HeightPrecision);
        public static PrintParameterModifier BottomLayerCount { get; } = new ("Bottom layer count", "Number of bottom/burn-in layers", "layers", 0, ushort.MaxValue, 1, 0);
        public static PrintParameterModifier TransitionLayerCount { get; } = new ("Transition layer count", "Number of fade/transition layers", "layers",0, ushort.MaxValue, 1, 0);
            
        public static PrintParameterModifier BottomLightOffDelay { get; } = new("Bottom light-off seconds", "Total motor movement time + rest time to wait before cure a new bottom layer", "s");
        public static PrintParameterModifier LightOffDelay { get; } = new("Light-off seconds", "Total motor movement time + rest time to wait before cure a new layer", "s");

        public static PrintParameterModifier BottomWaitTimeBeforeCure { get; } = new ("Bottom wait before cure", "Time to wait/rest before cure a new bottom layer\nChitubox: Rest after retract\nLychee: Wait before print", "s");
        public static PrintParameterModifier WaitTimeBeforeCure { get; } = new ("Wait before cure", "Time to wait/rest before cure a new layer\nChitubox: Rest after retract\nLychee: Wait before print", "s");
            
        public static PrintParameterModifier BottomExposureTime { get; } = new ("Bottom exposure time", "Bottom layers exposure time", "s", 0.1M);
        public static PrintParameterModifier ExposureTime { get; } = new ("Exposure time", "Normal layers exposure time", "s", 0.1M);
           
        public static PrintParameterModifier BottomWaitTimeAfterCure { get; } = new("Bottom wait after cure", "Time to wait/rest after cure a new bottom layer\nChitubox: Rest before lift\nLychee: Wait after print", "s");
        public static PrintParameterModifier WaitTimeAfterCure { get; } = new("Wait after cure", "Time to wait/rest after cure a new bottom layer\nChitubox: Rest before lift\nLychee: Wait after print", "s");
            
        public static PrintParameterModifier BottomLiftHeight { get; } = new ("Bottom lift height", "Bottom lift/peel height between layers", "mm");
        public static PrintParameterModifier LiftHeight { get; } = new ("Lift height", @"Lift/peel height between layers", "mm");
            
        public static PrintParameterModifier BottomLiftSpeed { get; } = new ("Bottom lift speed", "Lift speed of bottom layers", "mm/min", 10, 5000, 5);
        public static PrintParameterModifier LiftSpeed { get; } = new ("Lift speed", "Lift speed of normal layers", "mm/min", 10, 5000, 5);

        public static PrintParameterModifier BottomLiftHeight2 { get; } = new("2) Bottom lift height", "Bottom second lift/peel height between layers", "mm");
        public static PrintParameterModifier LiftHeight2 { get; } = new("2) Lift height", @"Second lift/peel height between layers", "mm");

        public static PrintParameterModifier BottomLiftSpeed2 { get; } = new("2) Bottom lift speed", "Lift speed of bottom layers for the second lift sequence (TSMC)", "mm/min", 0, 5000, 5);
        public static PrintParameterModifier LiftSpeed2 { get; } = new("2) Lift speed", "Lift speed of normal layers for the second lift sequence (TSMC)", "mm/min", 0, 5000, 5);

        public static PrintParameterModifier BottomWaitTimeAfterLift { get; } = new("Bottom wait after lift", "Time to wait/rest after a lift/peel sequence at bottom layers\nChitubox: Rest after lift\nLychee: Wait after lift", "s");
        public static PrintParameterModifier WaitTimeAfterLift { get; } = new("Wait after lift", "Time to wait/rest after a lift/peel sequence at layers\nChitubox: Rest after lift\nLychee: Wait after lift", "s");
           
        public static PrintParameterModifier BottomRetractSpeed { get; } = new ("Bottom retract speed", "Bottom down speed from lift height to next layer cure position", "mm/min", 10, 5000, 5);
        public static PrintParameterModifier RetractSpeed { get; } = new ("Retract speed", "Down speed from lift height to next layer cure position", "mm/min", 10, 5000, 5);

        public static PrintParameterModifier BottomRetractHeight2 { get; } = new("2) Bottom retract height", "Slow retract height of bottom layers (TSMC)", "mm");
        public static PrintParameterModifier RetractHeight2 { get; } = new("2) Retract height", "Slow retract height of normal layers (TSMC)", "mm");
        public static PrintParameterModifier BottomRetractSpeed2 { get; } = new("2) Bottom retract speed", "Slow retract speed of bottom layers (TSMC)", "mm/min", 0, 5000, 5);
        public static PrintParameterModifier RetractSpeed2 { get; } = new("2) Retract speed", "Slow retract speed of normal layers (TSMC)", "mm/min", 0, 5000, 5);

        public static PrintParameterModifier BottomLightPWM { get; } = new ("Bottom light PWM", "UV LED power for bottom layers", "☀", 1, byte.MaxValue, 5, 0);
        public static PrintParameterModifier LightPWM { get; } = new ("Light PWM", "UV LED power for layers", "☀", 1, byte.MaxValue, 5, 0);

        /*public static PrintParameterModifier[] Parameters = {
            BottomLayerCount,

            BottomWaitTimeBeforeCure,
            WaitTimeBeforeCure,
            BottomExposureTime,
            ExposureTime,
            BottomWaitTimeAfterCure,
            WaitTimeAfterCure,

            BottomLightOffDelay,
            LightOffDelay,
            BottomLiftHeight,
            BottomLiftSpeed,
            LiftHeight,
            LiftSpeed,
            BottomWaitTimeAfterLift,
            WaitTimeAfterLift,
            RetractSpeed,

            BottomLightPWM,
            LightPWM
        };*/
        #endregion

        #region Properties

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets the value unit
        /// </summary>
        public string ValueUnit { get; }

        /// <summary>
        /// Gets the minimum value
        /// </summary>
        public decimal Minimum { get; }

        /// <summary>
        /// Gets the maximum value
        /// </summary>
        public decimal Maximum { get; }

        /// <summary>
        /// Gets the incrementing value for the dropdown
        /// </summary>
        public double Increment { get; set; } = 1;

        /// <summary>
        /// Gets the number of decimal plates
        /// </summary>
        public byte DecimalPlates { get; }

        /// <summary>
        /// Gets or sets the current / old value
        /// </summary>
        public decimal OldValue { get; set; }

        /// <summary>
        /// Gets or sets the new value
        /// </summary>
        public decimal NewValue { get; set; }

        public decimal Value
        {
            get => NewValue;
            set => OldValue = NewValue = value;
        }

        /// <summary>
        /// Gets if the value has changed
        /// </summary>
        public bool HasChanged => OldValue != NewValue;
        #endregion

        #region Constructor
        public PrintParameterModifier(string name, string? description = null, string? valueUnit = null, decimal minimum = 0, decimal maximum = 1000, double increment = 0.5, byte decimalPlates = 2)
        {
            Name = name;
            Description = description ?? $"Modify '{name}'";
            ValueUnit = valueUnit ?? string.Empty;
            Minimum = minimum;
            Maximum = maximum;
            Increment = decimalPlates == 0 ? Math.Max(1, increment) : increment;
            DecimalPlates = decimalPlates;
        }
        #endregion

        #region Overrides

        protected bool Equals(PrintParameterModifier other)
        {
            return Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((PrintParameterModifier) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(Description)}: {Description}, {nameof(ValueUnit)}: {ValueUnit}, {nameof(Minimum)}: {Minimum}, {nameof(Maximum)}: {Maximum}, {nameof(DecimalPlates)}: {DecimalPlates}, {nameof(OldValue)}: {OldValue}, {nameof(NewValue)}: {NewValue}, {nameof(HasChanged)}: {HasChanged}";
        }
        #endregion
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Gets the available formats to process
    /// </summary>
    public static FileFormat[] AvailableFormats { get; } =
    {
        new SL1File(),      // Prusa SL1
        new ChituboxZipFile(), // Zip
        new ChituboxFile(), // cbddlp, cbt, photon
        new CTBEncryptedFile(), // encrypted ctb
        new PhotonSFile(), // photons
        new PHZFile(), // phz
        new PhotonWorkshopFile(),   // PSW
        new CWSFile(),   // CWS
        new LGSFile(),   // LGS, LGS30
        new VDAFile(),   // VDA
        new VDTFile(),   // VDT
        new AnetN4File(), // N4
        //new CXDLPv1File(),   // Creality Box v1
        new CXDLPFile(),   // Creality Box
        new FDGFile(), // fdg
        new ZCodeFile(),   // zcode
        new JXSFile(),      // jxs
        new ZCodexFile(),   // zcodex
        new MDLPFile(),   // MKS v1
        new GR1File(),   // GR1 Workshop
        new FlashForgeSVGXFile(), // SVGX
        new OSLAFile(),  // OSLA
        new OSFFile(),   // OSF
        new UVJFile(),   // UVJ
        new GenericZIPFile(),   // Generic zip files
        new ImageFile(),   // images
    };

    public static string AllSlicerFiles => AvailableFormats.Aggregate("All slicer files|",
        (current, fileFormat) => current.EndsWith("|")
            ? $"{current}{fileFormat.FileFilterExtensionsOnly}"
            : $"{current}; {fileFormat.FileFilterExtensionsOnly}");

    /// <summary>
    /// Gets all filters for open and save file dialogs
    /// </summary>
    public static string AllFileFilters =>
        AllSlicerFiles
        +
        AvailableFormats.Aggregate(string.Empty,
            (current, fileFormat) => $"{current}|" + fileFormat.FileFilter);

    public static List<KeyValuePair<string, List<string>>> AllFileFiltersAvalonia
    {
        get
        {
            var result = new List<KeyValuePair<string, List<string>>>
            {
                new("All slicer files", new List<string>())
            };
                
            for (int i = 0; i < AvailableFormats.Length; i++)
            {
                foreach (var fileExtension in AvailableFormats[i].FileExtensions)
                {
                    if(!fileExtension.IsVisibleOnFileFilters) continue;
                    result[0].Value.Add(fileExtension.Extension);
                    result.Add(new KeyValuePair<string, List<string>>(fileExtension.Description, new List<string>
                    {
                        fileExtension.Extension
                    }));
                }
            }

            return result;
        }
            
    }

    public static List<FileExtension> AllFileExtensions
    {
        get
        {
            List<FileExtension> extensions = new();
            foreach (var slicerFile in AvailableFormats)
            {
                extensions.AddRange(slicerFile.FileExtensions);
            }
            return extensions;
        }
    }

    public static List<string> AllFileExtensionsString => (from slicerFile in AvailableFormats from extension in slicerFile.FileExtensions select extension.Extension).ToList();


    /// <summary>
    /// Gets the count of available file extensions
    /// </summary>
    public static byte FileExtensionsCount => AvailableFormats.Aggregate<FileFormat, byte>(0, (current, fileFormat) => (byte) (current + fileFormat.FileExtensions.Length));

    /// <summary>
    /// Find <see cref="FileFormat"/> by an extension
    /// </summary>
    /// <param name="extensionOrFilePath"> name to find</param>
    /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
    /// <returns><see cref="FileFormat"/> object or null if not found</returns>
    public static FileFormat? FindByExtensionOrFilePath(string extensionOrFilePath, bool createNewInstance = false) =>
        FindByExtensionOrFilePath(extensionOrFilePath, out _, createNewInstance);

    /// <summary>
    /// Find <see cref="FileFormat"/> by an extension
    /// </summary>
    /// <param name="extensionOrFilePath"> name to find</param>
    /// <param name="fileFormatsSharingExt">Number of file formats sharing the input extension</param>
    /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
    /// <returns><see cref="FileFormat"/> object or null if not found</returns>
    public static FileFormat? FindByExtensionOrFilePath(string extensionOrFilePath, out byte fileFormatsSharingExt, bool createNewInstance = false)
    {
        fileFormatsSharingExt = 0;
        if (string.IsNullOrWhiteSpace(extensionOrFilePath)) return null;

        bool isFilePath = false;
        // Test for ext first
        var fileFormats = AvailableFormats.Where(fileFormat => fileFormat.IsExtensionValid(extensionOrFilePath)).ToArray();
        fileFormatsSharingExt = (byte)fileFormats.Length;

        if (fileFormats.Length == 0) // Extension not found, can be filepath, try to find it
        {
            GetFileNameStripExtensions(extensionOrFilePath, out var extension);
            if (string.IsNullOrWhiteSpace(extension)) return null;

            fileFormats = AvailableFormats.Where(fileFormat => fileFormat.IsExtensionValid(extension)).ToArray();
            if (fileFormats.Length == 0) return null;
            isFilePath = true; // Was a file path
        }

        if (fileFormats.Length == 1 || !isFilePath)
        {
            return createNewInstance
                ? Activator.CreateInstance(fileFormats[0].GetType()) as FileFormat
                : fileFormats[0];
        }


        // Multiple instances using Check for valid candidate
        foreach (var fileFormat in fileFormats)
        {
            if (fileFormat.CanProcess(extensionOrFilePath))
            {
                return createNewInstance
                    ? Activator.CreateInstance(fileFormat.GetType()) as FileFormat
                    : fileFormat;
            }
        }

        // Try this in a far and not probable attempt
        return createNewInstance
            ? Activator.CreateInstance(fileFormats[0].GetType()) as FileFormat
            : fileFormats[0];
    }

    /// <summary>
    /// Find <see cref="FileFormat"/> by an type name
    /// </summary>
    /// <param name="type">Type name to find</param>
    /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
    /// <returns><see cref="FileFormat"/> object or null if not found</returns>
    public static FileFormat? FindByType(string type, bool createNewInstance = false)
    {
        if (!type.EndsWith("File"))
        {
            type += "File";
        }

        var fileFormat = AvailableFormats.FirstOrDefault(format => string.Equals(format.GetType().Name, type, StringComparison.OrdinalIgnoreCase));
        if (fileFormat is null) return null;
        return createNewInstance
            ? Activator.CreateInstance(fileFormat.GetType()) as FileFormat
            : fileFormat;
        //return (from t in AvailableFormats where type == t.GetType() select createNewInstance ? (FileFormat) Activator.CreateInstance(type) : t).FirstOrDefault();
    }

    /// <summary>
    /// Find <see cref="FileFormat"/> by any means (type name, extension, filepath)
    /// </summary>
    /// <param name="name">Name to find</param>
    /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
    /// <returns><see cref="FileFormat"/> object or null if not found</returns>
    public static FileFormat? FindByAnyMeans(string name, bool createNewInstance = false)
    {
        return FindByType(name, true)
               ?? FindByExtensionOrFilePath(name, true);
    }

    /// <summary>
    /// Find <see cref="FileFormat"/> by an type
    /// </summary>
    /// <param name="type">Type to find</param>
    /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
    /// <returns><see cref="FileFormat"/> object or null if not found</returns>
    public static FileFormat? FindByType(Type type, bool createNewInstance = false)
    {
        var fileFormat = AvailableFormats.FirstOrDefault(format => format.GetType() == type);
        if (fileFormat is null) return null;
        return createNewInstance
            ? Activator.CreateInstance(type) as FileFormat
            : fileFormat;
        //return (from t in AvailableFormats where type == t.GetType() select createNewInstance ? (FileFormat) Activator.CreateInstance(type) : t).FirstOrDefault();
    }

    public static FileExtension? FindExtension(string extension)
    {
        return AvailableFormats.SelectMany(format => format.FileExtensions).FirstOrDefault(ext => ext.Equals(extension));
    }

    public static IEnumerable<FileExtension> FindExtensions(string extension)
    {
        return AvailableFormats.SelectMany(format => format.FileExtensions).Where(ext => ext.Equals(extension));
    }

    public static string? GetFileNameStripExtensions(string? filepath)
    {
        if (filepath is null) return null;
        //if (file.EndsWith(TemporaryFileAppend)) file = Path.GetFileNameWithoutExtension(file);
        return PathExtensions.GetFileNameStripExtensions(filepath, AllFileExtensionsString.OrderByDescending(s => s.Length).ToList(), out _);
    }

    public static string GetFileNameStripExtensions(string filepath, out string strippedExtension)
    {
        //if (file.EndsWith(TemporaryFileAppend)) file = Path.GetFileNameWithoutExtension(file);
        return PathExtensions.GetFileNameStripExtensions(filepath, AllFileExtensionsString.OrderByDescending(s => s.Length).ToList(), out strippedExtension);
    }

    public static FileFormat? Open(string fileFullPath, FileDecodeType decodeType, OperationProgress? progress = null)
    {
        var slicerFile = FindByExtensionOrFilePath(fileFullPath, true);
        if (slicerFile is null) return null;
        slicerFile.Decode(fileFullPath, decodeType, progress);
        return slicerFile;
    }

    public static FileFormat? Open(string fileFullPath, OperationProgress? progress = null) =>
        Open(fileFullPath, FileDecodeType.Full, progress);

    public static Task<FileFormat?> OpenAsync(string fileFullPath, FileDecodeType decodeType, OperationProgress? progress = null) 
        => Task.Run(() => Open(fileFullPath, decodeType, progress), progress?.Token ?? default);

    public static Task<FileFormat?> OpenAsync(string fileFullPath, OperationProgress? progress = null) => OpenAsync(fileFullPath, FileDecodeType.Full, progress);

    /// <summary>
    /// Copy parameters from one file to another
    /// </summary>
    /// <param name="from">From source file</param>
    /// <param name="to">To target file</param>
    /// <returns>Number of affected parameters</returns>
    public static uint CopyParameters(FileFormat from, FileFormat to)
    {
        if (ReferenceEquals(from, to)) return 0;
        if (from.PrintParameterModifiers is null || to.PrintParameterModifiers is null) return 0;

        uint count = 0;

        to.RefreshPrintParametersModifiersValues();
        var targetPrintModifier = to.PrintParameterModifiers.ToArray();
        from.RefreshPrintParametersModifiersValues();
        foreach (var sourceModifier in from.PrintParameterModifiers)
        {
            if (!targetPrintModifier.Contains(sourceModifier)) continue;

            var fromValueObj = from.GetValueFromPrintParameterModifier(sourceModifier);
            var toValueObj = to.GetValueFromPrintParameterModifier(sourceModifier);

            if (fromValueObj is null || toValueObj is null) continue;
            var fromValue = System.Convert.ToDecimal(fromValueObj);
            var toValue = System.Convert.ToDecimal(toValueObj);
            if (fromValue != toValue)
            {
                to.SetValueFromPrintParameterModifier(sourceModifier, fromValue);
                count++;
            }
        }
            
        return count;
    }

    /// <summary>
    /// Compress a layer from a <see cref="Stream"/>
    /// </summary>
    /// <param name="input"><see cref="Stream"/> to compress</param>
    /// <returns>Compressed byte array</returns>
    public static byte[] CompressLayer(Stream input)
    {
        return CompressLayer(input.ToArray());
    }

    /// <summary>
    /// Compress a layer from a byte array
    /// </summary>
    /// <param name="input">byte array to compress</param>
    /// <returns>Compressed byte array</returns>
    public static byte[] CompressLayer(byte[] input)
    {
        return input;
        /*using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(input, 0, input.Length);
            }
            return output.ToArray();
        }*/
    }

    /// <summary>
    /// Decompress a layer from a byte array
    /// </summary>
    /// <param name="input">byte array to decompress</param>
    /// <returns>Decompressed byte array</returns>
    public static byte[] DecompressLayer(byte[] input)
    {
        return input;
        /*using (MemoryStream ms = new MemoryStream(input))
        {
            using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(ms, CompressionMode.Decompress))
                {
                    dstream.CopyTo(output);
                }
                return output.ToArray();
            }
        }*/
    }

    public static byte[] EncodeImage(string dataType, Mat mat)
    {
        dataType = dataType.ToUpperInvariant();
        if (dataType
            is DATATYPE_PNG
            or DATATYPE_JPG
            or DATATYPE_JPEG
            or DATATYPE_JP2
            or DATATYPE_BMP
            or DATATYPE_TIF
            or DATATYPE_TIFF
            or DATATYPE_PPM
            or DATATYPE_PMG
            or DATATYPE_SR
            or DATATYPE_RAS
           )
        {
            return CvInvoke.Imencode($".{dataType.ToLowerInvariant()}", mat);
        }

        if (dataType
            is DATATYPE_RGB555
            or DATATYPE_RGB565
            or DATATYPE_RGB555_BE
            or DATATYPE_RGB565_BE
            or DATATYPE_RGB888

            or DATATYPE_BGR555
            or DATATYPE_BGR565
            or DATATYPE_BGR555_BE
            or DATATYPE_BGR565_BE
            or DATATYPE_BGR888
           )
        {
            var bytesPerPixel = dataType is "RGB888" or "BGR888" ? 3 : 2;
            var bytes = new byte[mat.Width * mat.Height * bytesPerPixel];
            uint index = 0;
            var span = mat.GetDataByteSpan();
            for (int i = 0; i < span.Length;)
            {
                byte b = span[i++];
                byte g;
                byte r;

                if (mat.NumberOfChannels == 1) // 8 bit safe-guard
                {
                    r = g = b;
                }
                else
                {
                    g = span[i++];
                    r = span[i++];
                }

                if (mat.NumberOfChannels == 4) i++; // skip alpha

                switch (dataType)
                {
                    case DATATYPE_RGB555:
                        var rgb555 = (ushort)(((r & 0b11111000) << 7) | ((g & 0b11111000) << 2) | (b >> 3));
                        BitExtensions.ToBytesLittleEndian(rgb555, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_RGB565:
                        var rgb565 = (ushort)(((r & 0b11111000) << 8) | ((g & 0b11111100) << 3) | (b >> 3));
                        BitExtensions.ToBytesLittleEndian(rgb565, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_RGB555_BE:
                        var rgb555Be = (ushort)(((r & 0b11111000) << 7) | ((g & 0b11111000) << 2) | (b >> 3));
                        BitExtensions.ToBytesBigEndian(rgb555Be, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_RGB565_BE:
                        var rgb565Be = (ushort)(((r & 0b11111000) << 8) | ((g & 0b11111100) << 3) | (b >> 3));
                        BitExtensions.ToBytesBigEndian(rgb565Be, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_RGB888:
                        bytes[index++] = r;
                        bytes[index++] = g;
                        bytes[index++] = b;
                        break;
                    case DATATYPE_BGR555:
                        var bgr555 = (ushort)(((b & 0b11111000) << 7) | ((g & 0b11111000) << 2) | (r >> 3));
                        BitExtensions.ToBytesLittleEndian(bgr555, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_BGR565:
                        var bgr565 = (ushort)(((b & 0b11111000) << 8) | ((g & 0b11111100) << 3) | (r >> 3));
                        BitExtensions.ToBytesLittleEndian(bgr565, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_BGR555_BE:
                        var bgr555Be = (ushort)(((b & 0b11111000) << 7) | ((g & 0b11111000) << 2) | (r >> 3));
                        BitExtensions.ToBytesBigEndian(bgr555Be, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_BGR565_BE:
                        var bgr565Be = (ushort)(((b & 0b11111000) << 8) | ((g & 0b11111100) << 3) | (r >> 3));
                        BitExtensions.ToBytesBigEndian(bgr565Be, bytes, index);
                        index += 2;
                        break;
                    case DATATYPE_BGR888:
                        bytes[index++] = b;
                        bytes[index++] = g;
                        bytes[index++] = r;
                        break;
                }
            }

            return bytes;
        }

        throw new NotSupportedException($"The encode type: {dataType} is not supported.");
    }

    public static Mat DecodeImage(string dataType, byte[] bytes, Size resolution)
    {
        if (dataType
            is DATATYPE_PNG
            or DATATYPE_JPG
            or DATATYPE_JPEG
            or DATATYPE_JP2
            or DATATYPE_BMP
            or DATATYPE_TIF
            or DATATYPE_TIFF
            or DATATYPE_PPM
            or DATATYPE_PMG
            or DATATYPE_SR
            or DATATYPE_RAS
           )
        {
            var mat = new Mat();
            CvInvoke.Imdecode(bytes, ImreadModes.AnyColor, mat);
            return mat;
        }

        if (dataType
            is DATATYPE_RGB555
            or DATATYPE_RGB565
            or DATATYPE_RGB555_BE
            or DATATYPE_RGB565_BE
            or DATATYPE_RGB888

            or DATATYPE_BGR555
            or DATATYPE_BGR565
            or DATATYPE_BGR555_BE
            or DATATYPE_BGR565_BE
            or DATATYPE_BGR888
           )
        {
            var mat = new Mat(resolution, DepthType.Cv8U, 3);
            var span = mat.GetDataByteSpan();
            var pixel = 0;
            int i = 0;
            while (i < bytes.Length && pixel < span.Length)
            {
                switch (dataType)
                {
                    case DATATYPE_RGB555:
                        ushort rgb555 = BitExtensions.ToUShortLittleEndian(bytes, i);
                        // 0b0rrrrrgggggbbbbb
                        span[pixel++] = (byte)((rgb555 & 0b00000000_00011111) << 3); // b
                        span[pixel++] = (byte)((rgb555 & 0b00000011_11100000) >> 2); // g
                        span[pixel++] = (byte)((rgb555 & 0b01111100_00000000) >> 7); // r
                        /*span[pixel++] = (byte)((rgb555 << 3) & 0b11111000); // b
                        span[pixel++] = (byte)((rgb555 >> 2) & 0b11111000); // g
                        span[pixel++] = (byte)((rgb555 >> 7) & 0b11111000); // r*/
                        i += 2;
                        break;
                    case DATATYPE_RGB565:
                        // 0brrrrrggggggbbbbb
                        ushort rgb565 = BitExtensions.ToUShortLittleEndian(bytes, i);
                        span[pixel++] = (byte)((rgb565 & 0b00000000_00011111) << 3); // b
                        span[pixel++] = (byte)((rgb565 & 0b00000111_11100000) >> 3); // g
                        span[pixel++] = (byte)((rgb565 & 0b11111000_00000000) >> 8); // r
                        i += 2;
                        break;
                    case DATATYPE_RGB555_BE:
                        ushort rgb555Be = BitExtensions.ToUShortBigEndian(bytes, i);
                        span[pixel++] = (byte)((rgb555Be & 0b00000000_00011111) << 3); // b
                        span[pixel++] = (byte)((rgb555Be & 0b00000011_11100000) >> 2); // g
                        span[pixel++] = (byte)((rgb555Be & 0b01111100_00000000) >> 7); // r
                        i += 2;
                        break;
                    case DATATYPE_RGB565_BE:
                        ushort rgb565Be = BitExtensions.ToUShortBigEndian(bytes, i);
                        span[pixel++] = (byte)((rgb565Be & 0b00000000_00011111) << 3); // b
                        span[pixel++] = (byte)((rgb565Be & 0b00000111_11100000) >> 3); // g
                        span[pixel++] = (byte)((rgb565Be & 0b11111000_00000000) >> 8); // r
                        i += 2;
                        break;
                    case DATATYPE_RGB888:
                        span[pixel++] = bytes[i + 2]; // b
                        span[pixel++] = bytes[i + 1]; // g
                        span[pixel++] = bytes[i];     // r
                        i += 3;
                        break;
                    case DATATYPE_BGR555:
                        ushort bgr555 = BitExtensions.ToUShortLittleEndian(bytes, i);
                        span[pixel++] = (byte)((bgr555 & 0b01111100_00000000) >> 7); // b
                        span[pixel++] = (byte)((bgr555 & 0b00000011_11100000) >> 2); // g
                        span[pixel++] = (byte)((bgr555 & 0b00000000_00011111) << 3); // r
                        i += 2;
                        break;
                    case DATATYPE_BGR565:
                        ushort bgr565 = BitExtensions.ToUShortLittleEndian(bytes, i);
                        span[pixel++] = (byte)((bgr565 & 0b11111000_00000000) >> 8); // b
                        span[pixel++] = (byte)((bgr565 & 0b00000111_11100000) >> 3); // g
                        span[pixel++] = (byte)((bgr565 & 0b00000000_00011111) << 3); // r
                        i += 2;
                        break;
                    case DATATYPE_BGR555_BE:
                        ushort bgr555Be = BitExtensions.ToUShortBigEndian(bytes, i);
                        span[pixel++] = (byte)((bgr555Be & 0b01111100_00000000) >> 7); // b
                        span[pixel++] = (byte)((bgr555Be & 0b00000011_11100000) >> 2); // g
                        span[pixel++] = (byte)((bgr555Be & 0b00000000_00011111) << 3); // r
                        i += 2;
                        break;
                    case DATATYPE_BGR565_BE:
                        ushort bgr565Be = BitExtensions.ToUShortBigEndian(bytes, i);
                        span[pixel++] = (byte)((bgr565Be & 0b11111000_00000000) >> 8); // b
                        span[pixel++] = (byte)((bgr565Be & 0b00000111_11100000) >> 3); // g
                        span[pixel++] = (byte)((bgr565Be & 0b00000000_00011111) << 3); // r
                        i += 2;
                        break;
                    case DATATYPE_BGR888:
                        span[pixel++] = bytes[i]; // b
                        span[pixel++] = bytes[i + 1]; // g
                        span[pixel++] = bytes[i + 2]; // r
                        i += 3;
                        break;
                }
            }

            for (; pixel < span.Length; pixel++) // Fill leftovers
            {
                span[pixel] = 0; 
            }

            return mat;
        }

        throw new NotSupportedException($"The decode type: {dataType} is not supported.");
    }

    public static Mat DecodeImage(string dataType, byte[] bytes, uint resolutionX = 0, uint resolutionY = 0)
        => DecodeImage(dataType, bytes, new Size((int)resolutionX, (int)resolutionY));

    public static void MutateGetVarsIterationChamfer(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out float iterationSteps, out int maxIteration)
    {
        iterationSteps = 0;
        maxIteration = 0;
        isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
        if (!isFade) return;
        iterationSteps = Math.Abs((iterationsStart - (float)iterationsEnd) / ((float)endLayerIndex - startLayerIndex));
        maxIteration = Math.Max(iterationsStart, iterationsEnd);
    }

    public static int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, float iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
    {
        if (!isFade) return iterationsStart;
        // calculate iterations based on range
        int iterations = (int)(iterationsStart < iterationsEnd
            ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
            : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

        // constrain
        return Math.Min(Math.Max(0, iterations), maxIteration);
    }

    public static int MutateGetIterationChamfer(uint layerIndex, uint startLayerIndex, uint endLayerIndex, int iterationsStart,
        int iterationsEnd, bool isFade)
    {
        MutateGetVarsIterationChamfer(startLayerIndex, endLayerIndex, iterationsStart, iterationsEnd, ref isFade,
            out float iterationSteps, out int maxIteration);
        return MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, layerIndex);
    }
    #endregion

    #region Members
    public object Mutex = new();

    protected Layer[] _layers = Array.Empty<Layer>();

    private bool _haveModifiedLayers;
    private uint _version;

    private byte _antiAliasing = 1;

    private ushort _bottomLayerCount = DefaultBottomLayerCount;
    private ushort _transitionLayerCount = DefaultTransitionLayerCount;


    private float _bottomLightOffDelay;
    private float _lightOffDelay;

    private float _bottomWaitTimeBeforeCure;
    private float _waitTimeBeforeCure;

    private float _bottomExposureTime = DefaultBottomExposureTime;
    private float _exposureTime = DefaultExposureTime;

    private float _bottomWaitTimeAfterCure;
    private float _waitTimeAfterCure;
        
    private float _bottomLiftHeight = DefaultBottomLiftHeight;
    private float _liftHeight = DefaultLiftHeight;
    private float _bottomLiftSpeed = DefaultBottomLiftSpeed;
    private float _liftSpeed = DefaultLiftSpeed;

    private float _bottomLiftHeight2 = DefaultBottomLiftHeight2;
    private float _liftHeight2 = DefaultLiftHeight2;
    private float _bottomLiftSpeed2 = DefaultBottomLiftSpeed2;
    private float _liftSpeed2 = DefaultLiftSpeed2;

    private float _bottomWaitTimeAfterLift;
    private float _waitTimeAfterLift;

    private float _bottomRetractHeight2 = DefaultBottomRetractHeight2;
    private float _retractHeight2 = DefaultRetractHeight2;
    private float _bottomRetractSpeed2 = DefaultBottomRetractSpeed2;
    private float _retractSpeed2 = DefaultRetractSpeed2;
    private float _bottomRetractSpeed = DefaultBottomRetractSpeed;
    private float _retractSpeed = DefaultRetractSpeed;


    private byte _bottomLightPwm = DefaultBottomLightPWM;
    private byte _lightPwm = DefaultLightPWM;

    private float _printTime;
    private float _materialMilliliters;
    private float _machineZ;
    private string _machineName = "Unknown";
    private string? _materialName;
    private float _materialGrams;
    private float _materialCost;
    private bool _suppressRebuildGCode;

    private Rectangle _boundingRectangle = Rectangle.Empty;

    private readonly Timer _queueTimerPrintTime = new(QueueTimerPrintTime){AutoReset = false};

    #endregion

    #region Properties

    public FileDecodeType DecodeType { get; private set; } = FileDecodeType.Full;

    /// <summary>
    /// Gets the file format type
    /// </summary>
    public abstract FileFormatType FileType { get; }

    /// <summary>
    /// Gets the layer image data type used on this file format
    /// </summary>
    public virtual FileImageType LayerImageType => FileType == FileFormatType.Archive ? FileImageType.Png8 : FileImageType.Custom;

    /// <summary>
    /// Gets the group name under convert menu to group all extensions, set to null to not group items
    /// </summary>
    public virtual string? ConvertMenuGroup => null;

    /// <summary>
    /// Gets the valid file extensions for this <see cref="FileFormat"/>
    /// </summary>
    public abstract FileExtension[] FileExtensions { get; }

    /// <summary>
    /// The speed unit used by this file format in his internal data
    /// </summary>
    public virtual SpeedUnit FormatSpeedUnit => CoreSpeedUnit;
    
    /// <summary>
    /// Gets the available <see cref="PrintParameterModifier"/>
    /// </summary>
    public virtual PrintParameterModifier[]? PrintParameterModifiers => null;

    /// <summary>
    /// Gets the available <see cref="PrintParameterModifier"/> per layer
    /// </summary>
    public virtual PrintParameterModifier[]? PrintParameterPerLayerModifiers => null;

    /// <summary>
    /// Checks if a <see cref="PrintParameterModifier"/> exists on print parameters
    /// </summary>
    /// <param name="modifier"></param>
    /// <returns>True if exists, otherwise false</returns>
    public bool HavePrintParameterModifier(PrintParameterModifier modifier) =>
        PrintParameterModifiers is not null && PrintParameterModifiers.Contains(modifier);

    /// <summary>
    /// Checks if a <see cref="PrintParameterModifier"/> exists on layer parameters
    /// </summary>
    /// <param name="modifier"></param>
    /// <returns>True if exists, otherwise false</returns>
    public bool HaveLayerParameterModifier(PrintParameterModifier modifier) =>
        SupportPerLayerSettings && PrintParameterPerLayerModifiers!.Contains(modifier);

    /// <summary>
    /// Gets the file filter for open and save dialogs
    /// </summary>
    public string FileFilter {
        get
        {
            var result = string.Empty;

            foreach (var fileExt in FileExtensions)
            {
                if (!ReferenceEquals(result, string.Empty))
                {
                    result += '|';
                }
                result += fileExt.Filter;
            }

            return result;
        }
    }

    /// <summary>
    /// Gets all valid file extensions for Avalonia file dialog
    /// </summary>
    public List<KeyValuePair<string, List<string>>> FileFilterAvalonia 
        => FileExtensions.Select(fileExt => new KeyValuePair<string, List<string>>(fileExt.Description, new List<string> {fileExt.Extension})).ToList();

    /// <summary>
    /// Gets all valid file extensions in "*.extension1;*.extension2" format
    /// </summary>
    public string FileFilterExtensionsOnly
    {
        get
        {
            var result = string.Empty;

            foreach (var fileExt in FileExtensions)
            {
                if (!ReferenceEquals(result, string.Empty))
                {
                    result += "; ";
                }
                result += $"*.{fileExt.Extension}";
            }

            return result;
        }
    }

    /// <summary>
    /// Gets or sets if change a global property should rebuild every layer data based on them
    /// </summary>
    public bool SuppressRebuildProperties { get; set; }

    /// <summary>
    /// Gets the temporary output file path to use on save and encode
    /// </summary>
    public string TemporaryOutputFileFullPath => $"{FileFullPath}{TemporaryFileAppend}";

    /// <summary>
    /// Gets the input file path loaded into this <see cref="FileFormat"/>
    /// </summary>
    public string? FileFullPath { get; set; }

    public string? DirectoryPath => Path.GetDirectoryName(FileFullPath);
    public string? Filename => Path.GetFileName(FileFullPath);
    public string? FileExtension => Path.GetExtension(FileFullPath);
    public string? FilenameNoExt => GetFileNameStripExtensions(FileFullPath);

    /// <summary>
    /// Gets the available versions to set in this file format
    /// </summary>
    public virtual uint[]? AvailableVersions => null;

    /// <summary>
    /// Gets the amount of available versions in this file format
    /// </summary>
    public virtual byte AvailableVersionsCount => (byte)(AvailableVersions?.Length ?? 0);

    /// <summary>
    /// Gets the default version to use in this file when not setting the version
    /// </summary>
    public virtual uint DefaultVersion => 0;

    /// <summary>
    /// Gets or sets the version of this file format
    /// </summary>
    public virtual uint Version
    {
        get => _version;
        set
        {
            if (AvailableVersions is not null && !AvailableVersions.Contains(value))
            {
                throw new VersionNotFoundException($"Version {value} not known for this file format");
            }

            RequireFullEncode = true;
            RaiseAndSetIfChanged(ref _version, value);
        }
    }

    /// <summary>
    /// Gets the thumbnails count present in this file format
    /// </summary>
    public byte ThumbnailsCount => (byte)(ThumbnailsOriginalSize?.Length ?? 0);

    /// <summary>
    /// Gets the number of created thumbnails
    /// </summary>
    public byte CreatedThumbnailsCount {
        get
        {
            if (Thumbnails is null) return 0;
            byte count = 0;

            foreach (var thumbnail in Thumbnails)
            {
                if (thumbnail is null || thumbnail.IsEmpty) continue;
                count++;
            }

            return count;
        }
    }

    /// <summary>
    /// Gets the original thumbnail sizes
    /// </summary>
    public virtual Size[]? ThumbnailsOriginalSize => null;

    /// <summary>
    /// Gets the thumbnails for this <see cref="FileFormat"/>
    /// </summary>
    public Mat?[] Thumbnails { get; init; }

    public IssueManager IssueManager { get; }

    /// <summary>
    /// Layers List
    /// </summary>
    public Layer[] Layers
    {
        get => _layers;
        set
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            //if (ReferenceEquals(_layers, value)) return;

            var rebuildProperties = false;
            var oldLayerCount = LayerCount;
            var oldLayers = _layers;
            _layers = value;
            BoundingRectangle = Rectangle.Empty;

            if (LayerCount != oldLayerCount)
            {
                LayerCount = LayerCount;
            }

            RequireFullEncode = true;
            PrintHeight = PrintHeight;
            UpdatePrintTime();

            if (LayerCount > 0)
            {
                //SetAllIsModified(true);

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++) // Forced sanitize
                {
                    if (_layers[layerIndex] is null) continue;
                    _layers[layerIndex].Index = layerIndex;
                    _layers[layerIndex].SlicerFile = this;

                    if (layerIndex >= oldLayerCount || layerIndex < oldLayerCount && !_layers[layerIndex].Equals(oldLayers[layerIndex]))
                    {
                        // Marks as modified only if layer image changed on this index
                        _layers[layerIndex].IsModified = true;
                    }
                }

                if (LayerCount != oldLayerCount && !SuppressRebuildProperties && LastLayer is not null)
                {
                    RebuildLayersProperties();
                    rebuildProperties = true;
                }
            }

            if (!rebuildProperties)
            {
                MaterialMilliliters = -1;
                RebuildGCode();
            }

            RaisePropertyChanged();
        }
    }

    /// <summary>
    /// First layer index, this is always 0
    /// </summary>
    public const uint FirstLayerIndex = 0;

    /// <summary>
    /// Gets the last layer index
    /// </summary>
    public uint LastLayerIndex => LayerCount > 0 ? LayerCount - 1 : 0;

    /// <summary>
    /// Gets the first layer
    /// </summary>
    public Layer? FirstLayer => LayerCount > 0 ? this[0] : null;

    /// <summary>
    /// Gets the last bottom layer
    /// </summary>
    public Layer? LastBottomLayer => this.LastOrDefault(layer => layer.IsBottomLayer);

    /// <summary>
    /// Gets the first normal layer
    /// </summary>
    public Layer? FirstNormalLayer => this.FirstOrDefault(layer => layer.IsNormalLayer);

    /// <summary>
    /// Gets the last layer
    /// </summary>
    public Layer? LastLayer => LayerCount > 0 ? this[^1] : null;

    /// <summary>
    /// Gets the smallest bottom layer using the pixel count
    /// </summary>
    public Layer? SmallestBottomLayer => this.Where(layer => layer.IsBottomLayer && !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount);

    /// <summary>
    /// Gets the largest bottom layer using the pixel count
    /// </summary>
    public Layer? LargestBottomLayer => this.Where(layer => layer.IsBottomLayer && !layer.IsEmpty).MaxBy(layer => layer.NonZeroPixelCount);

    /// <summary>
    /// Gets the smallest normal layer using the pixel count
    /// </summary>
    public Layer? SmallestNormalLayer => this.Where(layer => layer.IsNormalLayer && !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount);

    /// <summary>
    /// Gets the largest layer using the pixel count
    /// </summary>
    public Layer? LargestNormalLayer => this.Where(layer => layer.IsNormalLayer && !layer.IsEmpty).MaxBy(layer => layer.NonZeroPixelCount);

    /// <summary>
    /// Gets the smallest normal layer using the pixel count
    /// </summary>
    public Layer? SmallestLayer => this.Where(layer => !layer.IsEmpty).MinBy(layer => layer.NonZeroPixelCount);

    /// <summary>
    /// Gets the largest layer using the pixel count
    /// </summary>
    public Layer? LargestLayer => this.MaxBy(layer => layer.NonZeroPixelCount);

    public Layer? GetSmallestLayerBetween(uint layerStartIndex, uint layerEndIndex)
    {
        return this.Where((layer, index) => !layer.IsEmpty && index >= layerStartIndex && index <= layerEndIndex).MinBy(layer => layer.NonZeroPixelCount);
    }

    public Layer? GetLargestLayerBetween(uint layerStartIndex, uint layerEndIndex)
    {
        return this.Where((layer, index) => !layer.IsEmpty && index >= layerStartIndex && index <= layerEndIndex).MaxBy(layer => layer.NonZeroPixelCount);
    }

    /// <summary>
    /// Gets all bottom layers
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Layer> BottomLayers => this.Where(layer => layer.IsBottomLayer);

    /// <summary>
    /// Gets all normal layers
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Layer> NormalLayers => this.Where(layer => layer.IsNormalLayer);

    /// <summary>
    /// Gets all transition layers
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Layer> TransitionLayers => this.Where(layer => layer.IsTransitionLayer);

    /// <summary>
    /// Gets all layers that use TSMC values
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Layer> TsmcLayers => this.Where(layer => layer.IsUsingTSMC);

    /// <summary>
    /// Gets all layers on same position but exclude the first layer on that position
    /// </summary>
    public IEnumerable<Layer> SamePositionedLayers
    {
        get
        {
            for (int layerIndex = 1; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                if (this[layerIndex - 1].PositionZ != layer.PositionZ) continue;
                yield return layer;
            }
        }
    }

    public IEnumerable<Layer> GetDistinctLayersByPositionZ(uint layerIndexStart = 0) =>
        GetDistinctLayersByPositionZ(layerIndexStart, LastLayerIndex);

    public IEnumerable<Layer> GetDistinctLayersByPositionZ(uint layerIndexStart, uint layerIndexEnd)
    {
        return layerIndexEnd - layerIndexStart >= LastLayerIndex
            ? this.DistinctBy(layer => layer.PositionZ)
            : this.Where((_, layerIndex) => layerIndex >= layerIndexStart && layerIndex <= layerIndexEnd).DistinctBy(layer => layer.PositionZ);
    }

    public IEnumerable<Layer> GetLayersFromHeightRange(float startPositionZ, float endPositionZ)
    {
        return this.Where(layer => layer.PositionZ >= startPositionZ && layer.PositionZ <= endPositionZ);
    }

    public IEnumerable<Layer> GetLayersFromHeightRange(float endPositionZ)
    {
        return this.Where(layer => layer.PositionZ <= endPositionZ);
    }

    /// <summary>
    /// True if all layers are using same value parameters as global settings, otherwise false
    /// </summary>
    public bool AllLayersAreUsingGlobalParameters => this.All(layer => layer.IsUsingGlobalParameters);

    /// <summary>
    /// True if any layer is using TSMC, otherwise false when none of layers is using TSMC
    /// </summary>
    public bool AnyLayerIsUsingTSMC => this.Any(layer => layer.IsUsingTSMC);

    /// <summary>
    /// True if the file global property is using TSMC, otherwise false when not using
    /// </summary>
    public bool IsUsingTSMC => (CanUseAnyLiftHeight2 || CanUseAnyRetractHeight2) && (BottomLiftHeight2 > 0 || BottomRetractHeight2 > 0 || LiftHeight2 > 0 || RetractHeight2 > 0);


    /// <summary>
    /// Gets if any layer got modified, otherwise false
    /// Sets all layers `IsModified` flag
    /// </summary>
    public bool IsModified
    {
        get
        {
            for (uint i = 0; i < LayerCount; i++)
            {
                if (this[i].IsModified) return true;
            }
            return false;
        }
        set
        {
            for (uint i = 0; i < LayerCount; i++)
            {
                if (this[i] is null) continue;
                this[i].IsModified = value;
            }
        }
    }

    /// <summary>
    /// Gets the bounding rectangle of the model
    /// </summary>
    public Rectangle BoundingRectangle
    {
        get => GetBoundingRectangle();
        set
        {
            RaiseAndSetIfChanged(ref _boundingRectangle, value);
            RaisePropertyChanged(nameof(BoundingRectangleMillimeters));
        }
    }

    /// <summary>
    /// Gets the bounding rectangle of the object in millimeters
    /// </summary>
    public RectangleF BoundingRectangleMillimeters
    {
        get
        {
            var pixelSize = PixelSize;
            return new RectangleF(
                (float)Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
        }
    }

    /// <summary>
    /// Gets or sets if modifications require a full encode to save
    /// </summary>
    public bool RequireFullEncode
    {
        get => _haveModifiedLayers || IsModified;
        set => RaiseAndSetIfChanged(ref _haveModifiedLayers, value);
    }

    /// <summary>
    /// Gets the image width and height resolution
    /// </summary>
    public Size Resolution
    {
        get => new((int)ResolutionX, (int)ResolutionY);
        set
        {
            ResolutionX = (uint) value.Width;
            ResolutionY = (uint) value.Height;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(ResolutionRectangle));
            RaisePropertyChanged(nameof(DisplayAspectRatio));
            RaisePropertyChanged(nameof(DisplayAspectRatioStr));
            RaisePropertyChanged(nameof(Xppmm));
            RaisePropertyChanged(nameof(Yppmm));
            RaisePropertyChanged(nameof(Ppmm));
            RaisePropertyChanged(nameof(PpmmMax));
            RaisePropertyChanged(nameof(PixelSizeMicrons));
            RaisePropertyChanged(nameof(PixelArea));
            RaisePropertyChanged(nameof(PixelAreaMicrons));
            RaisePropertyChanged(nameof(PixelHeight));
            RaisePropertyChanged(nameof(PixelHeightMicrons));
            RaisePropertyChanged(nameof(PixelSize));
            RaisePropertyChanged(nameof(PixelSizeMax));
            RaisePropertyChanged(nameof(PixelWidth));
            RaisePropertyChanged(nameof(PixelWidthMicrons));
        }
    }

    /// <summary>
    /// Gets the image width resolution
    /// </summary>
    public abstract uint ResolutionX { get; set; }

    /// <summary>
    /// Gets the image height resolution
    /// </summary>
    public abstract uint ResolutionY { get; set; }

    /// <summary>
    /// Gets an rectangle that starts at 0,0 and goes up to <see cref="Resolution"/>
    /// </summary>
    public Rectangle ResolutionRectangle => new(Point.Empty, Resolution);

    /// <summary>
    /// Gets the display total number of pixels (<see cref="ResolutionX"/> * <see cref="ResolutionY"/>)
    /// </summary>
    public uint DisplayPixelCount => ResolutionX * ResolutionY;

    /// <summary>
    /// Gets the size of display in millimeters
    /// </summary>
    public SizeF Display
    {
        get => new(DisplayWidth, DisplayHeight);
        set
        {
            DisplayWidth = (float)Math.Round(value.Width, 4);
            DisplayHeight = (float)Math.Round(value.Height, 4);
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(DisplayAspectRatio));
            RaisePropertyChanged(nameof(DisplayAspectRatioStr));
            RaisePropertyChanged(nameof(DisplayDiagonal));
            RaisePropertyChanged(nameof(DisplayDiagonalInches));
            RaisePropertyChanged(nameof(Xppmm));
            RaisePropertyChanged(nameof(Yppmm));
            RaisePropertyChanged(nameof(Ppmm));
            RaisePropertyChanged(nameof(PpmmMax));
            RaisePropertyChanged(nameof(PixelSizeMicrons));
            RaisePropertyChanged(nameof(PixelArea));
            RaisePropertyChanged(nameof(PixelAreaMicrons));
            RaisePropertyChanged(nameof(PixelHeight));
            RaisePropertyChanged(nameof(PixelHeightMicrons));
            RaisePropertyChanged(nameof(PixelSize));
            RaisePropertyChanged(nameof(PixelSizeMax));
            RaisePropertyChanged(nameof(PixelWidth));
        }
    }

    /// <summary>
    /// Gets or sets the display width in millimeters
    /// </summary>
    public virtual float DisplayWidth { get; set; }

    /// <summary>
    /// Gets or sets the display height in millimeters
    /// </summary>
    public virtual float DisplayHeight { get; set; }

    /// <summary>
    /// Gets the display diagonal in millimeters
    /// </summary>
    public float DisplayDiagonal => (float)Math.Round(Math.Sqrt(Math.Pow(DisplayWidth, 2) + Math.Pow(DisplayHeight, 2)), 2);

    /// <summary>
    /// Gets the display diagonal in inch's
    /// </summary>
    public float DisplayDiagonalInches => (float)Math.Round(Math.Sqrt(Math.Pow(DisplayWidth, 2) + Math.Pow(DisplayHeight, 2)) * UnitExtensions.MillimeterToInch, 2);

    /// <summary>
    /// Gets the display ratio
    /// </summary>
    public Size DisplayAspectRatio
    {
        get
        {
            var gcd = MathExtensions.GCD(ResolutionX, ResolutionY);
            return new((int)(ResolutionX / gcd), (int)(ResolutionY / gcd));
        }
    }

    public string DisplayAspectRatioStr
    {
        get
        {
            var aspect = DisplayAspectRatio;
            return $"{aspect.Width}:{aspect.Height}";
        }
    }

    /// <summary>
    /// Gets or sets if images need to be mirrored on lcd to print on the correct orientation
    /// </summary>
    public virtual FlipDirection DisplayMirror { get; set; } = FlipDirection.None;

    /// <summary>
    /// Gets if the display is in portrait mode
    /// </summary>
    public bool IsDisplayPortrait => ResolutionY > ResolutionX;

    /// <summary>
    /// Gets if the display is in landscape mode
    /// </summary>
    public bool IsDisplayLandscape => !IsDisplayPortrait;

    /// <summary>
    /// Gets or sets the maximum printer build Z volume
    /// </summary>
    public virtual float MachineZ
    {
        get => _machineZ > 0 ? _machineZ : PrintHeight;
        set => RaiseAndSetIfChanged(ref _machineZ, value);
    }

    /// <summary>
    /// Gets or sets the pixels per mm on X direction
    /// </summary>
    public virtual float Xppmm
    {
        get => DisplayWidth > 0 ? ResolutionX / DisplayWidth : 0;
        set
        {
            RaisePropertyChanged(nameof(Xppmm));
            RaisePropertyChanged(nameof(Ppmm));
            RaisePropertyChanged(nameof(PpmmMax));
        }
    }

    /// <summary>
    /// Gets or sets the pixels per mm on Y direction
    /// </summary>
    public virtual float Yppmm
    {
        get => DisplayHeight > 0 ? ResolutionY / DisplayHeight : 0;
        set
        {
            RaisePropertyChanged(nameof(Yppmm));
            RaisePropertyChanged(nameof(Ppmm));
            RaisePropertyChanged(nameof(PpmmMax));
        }
    }

    /// <summary>
    /// Gets or sets the pixels per mm
    /// </summary>
    public SizeF Ppmm
    {
        get => new(Xppmm, Yppmm);
        set
        {
            Xppmm = value.Width;
            Yppmm = value.Height;
        }
    }

    /// <summary>
    /// Gets the maximum (Width or Height) pixels per mm 
    /// </summary>
    public float PpmmMax => Ppmm.Max();

    /// <summary>
    /// Gets the pixel width in millimeters
    /// </summary>
    public float PixelWidth => DisplayWidth > 0 && ResolutionX > 0 ? (float) Math.Round(DisplayWidth / ResolutionX, 3) : 0;

    /// <summary>
    /// Gets the pixel height in millimeters
    /// </summary>
    public float PixelHeight => DisplayHeight > 0 && ResolutionY > 0 ? (float) Math.Round(DisplayHeight / ResolutionY, 3) : 0;

    /// <summary>
    /// Gets the pixel size in millimeters
    /// </summary>
    public SizeF PixelSize => new(PixelWidth, PixelHeight);

    /// <summary>
    /// Gets the maximum pixel between width and height in millimeters
    /// </summary>
    public float PixelSizeMax => PixelSize.Max();

    /// <summary>
    /// Gets the pixel area in millimeters
    /// </summary>
    public float PixelArea => PixelSize.Area();

    /// <summary>
    /// Gets the pixel width in microns
    /// </summary>
    public float PixelWidthMicrons => DisplayWidth > 0 ? (float)Math.Round(DisplayWidth / ResolutionX * 1000, 3) : 0;

    /// <summary>
    /// Gets the pixel height in microns
    /// </summary>
    public float PixelHeightMicrons => DisplayHeight > 0 ? (float)Math.Round(DisplayHeight / ResolutionY * 1000, 3) : 0;

    /// <summary>
    /// Gets the pixel size in microns
    /// </summary>
    public SizeF PixelSizeMicrons => new(PixelWidthMicrons, PixelHeightMicrons);

    /// <summary>
    /// Gets the maximum pixel between width and height in microns
    /// </summary>
    public float PixelSizeMicronsMax => PixelSizeMicrons.Max();

    /// <summary>
    /// Gets the pixel area in millimeters
    /// </summary>
    public float PixelAreaMicrons => PixelSizeMicrons.Area();

    /// <summary>
    /// Gets the file volume (XYZ) in mm^3
    /// </summary>
    public float Volume => (float)Math.Round(this.Sum(layer => layer.GetVolume()), 3);

    /// <summary>
    /// Checks if this file have AntiAliasing
    /// </summary>
    public bool HaveAntiAliasing => AntiAliasing > 1;

    /// <summary>
    /// Gets if the AntiAliasing is emulated/fake with fractions of the time or if is real grey levels
    /// </summary>
    public virtual bool IsAntiAliasingEmulated => false;

    /// <summary>
    /// Gets or sets the AntiAliasing level
    /// </summary>
    public virtual byte AntiAliasing
    {
        get => _antiAliasing;
        set => RaiseAndSet(ref _antiAliasing, value);
    }

    /// <summary>
    /// Gets Layer Height in mm
    /// </summary>
    public abstract float LayerHeight { get; set; }

    /// <summary>
    /// Gets Layer Height in um
    /// </summary>
    public ushort LayerHeightUm => (ushort) (LayerHeight * 1000);


    /// <summary>
    /// Gets or sets the print height in mm
    /// </summary>
    public virtual float PrintHeight
    {
        get => LayerCount == 0 ? 0 : LastLayer?.PositionZ ?? 0;
        set => RaisePropertyChanged();
    }

    /// <summary>
    /// Checks if this file format supports per layer settings
    /// </summary>
    public bool SupportPerLayerSettings => PrintParameterPerLayerModifiers is not null && PrintParameterPerLayerModifiers.Length > 0;

    public bool IsReadOnly => false;

    public int Count => _layers?.Length ?? 0;

    /// <summary>
    /// Gets or sets the layer count
    /// </summary>
    public virtual uint LayerCount
    {
        get => (uint)Count;
        set {
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(NormalLayerCount));
            RaisePropertyChanged(nameof(TransitionLayersRepresentation));
        }
    }

    /// <summary>
    /// Return the number of digits on the layer count number, eg: 123 layers = 3 digits
    /// </summary>
    public byte LayerDigits => (byte)LayerCount.DigitCount();

    /// <summary>
    /// Gets or sets the total height for the bottom layers in millimeters
    /// </summary>
    public float BottomLayersHeight
    {
        get => LastBottomLayer?.PositionZ ?? 0;
        set => BottomLayerCount = (ushort)Math.Ceiling(value / LayerHeight);
    } 

    #region Universal Properties

    /// <summary>
    /// Gets or sets the number of initial layer count
    /// </summary>
    public virtual ushort BottomLayerCount
    {
        get => _bottomLayerCount;
        set
        {
            RaiseAndSet(ref _bottomLayerCount, value);
            RaisePropertyChanged(nameof(NormalLayerCount));
            RaisePropertyChanged(nameof(TransitionLayersRepresentation));
        }
    }

    /// <summary>
    /// Gets the transition layer type
    /// </summary>
    public virtual TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Software;

    /// <summary>
    /// Gets or sets the number of transition layers
    /// </summary>
    public virtual ushort TransitionLayerCount
    {
        get => _transitionLayerCount;
        set
        {
            RaiseAndSet(ref _transitionLayerCount, (ushort)Math.Min(value, MaximumPossibleTransitionLayerCount));
            RaisePropertyChanged(nameof(HaveTransitionLayers));
            RaisePropertyChanged(nameof(TransitionLayersRepresentation));
        }
    }

    /// <summary>
    /// Gets if have transition layers
    /// </summary>
    public bool HaveTransitionLayers => _transitionLayerCount > 0;

    /// <summary>
    /// Gets the maximum transition layers this layer collection supports
    /// </summary>
    public uint MaximumPossibleTransitionLayerCount
    {
        get
        {
            if (BottomLayerCount == 0) return 0;
            if (_layers is null) return uint.MaxValue;
            int layerCount = (int)LayerCount - BottomLayerCount - 1;
            if (layerCount <= 0) return 0;
            return (uint)layerCount;
        }
    }

    /// <summary>
    /// Gets the number of normal layer count
    /// </summary>
    public uint NormalLayerCount => LayerCount - BottomLayerCount;

    /// <summary>
    /// Gets or sets the bottom layer off time in seconds
    /// </summary>
    public virtual float BottomLightOffDelay
    {
        get => _bottomLightOffDelay;
        set
        {
            RaiseAndSet(ref _bottomLightOffDelay, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LightOffDelayRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the layer off time in seconds
    /// </summary>
    public virtual float LightOffDelay
    {
        get => _lightOffDelay;
        set
        {
            RaiseAndSet(ref _lightOffDelay, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LightOffDelayRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the bottom time in seconds to wait before cure the layer
    /// </summary>
    public virtual float BottomWaitTimeBeforeCure
    {
        get => _bottomWaitTimeBeforeCure;
        set
        {
            RaiseAndSet(ref _bottomWaitTimeBeforeCure, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }


    /// <summary>
    /// Gets or sets the time in seconds to wait after cure the layer
    /// </summary>
    public virtual float WaitTimeBeforeCure
    {
        get => _waitTimeBeforeCure;
        set
        {
            RaiseAndSet(ref _waitTimeBeforeCure, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the initial exposure time for <see cref="BottomLayerCount"/> in seconds
    /// </summary>
    public virtual float BottomExposureTime
    {
        get => _bottomExposureTime;
        set
        {
            RaiseAndSet(ref _bottomExposureTime, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(ExposureRepresentation));
            RaisePropertyChanged(nameof(TransitionLayersRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the normal layer exposure time in seconds
    /// </summary>
    public virtual float ExposureTime
    {
        get => _exposureTime;
        set
        {
            RaiseAndSet(ref _exposureTime, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(ExposureRepresentation));
            RaisePropertyChanged(nameof(TransitionLayersRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the bottom time in seconds to wait after cure the layer
    /// </summary>
    public virtual float BottomWaitTimeAfterCure
    {
        get => _bottomWaitTimeAfterCure;
        set
        {
            RaiseAndSet(ref _bottomWaitTimeAfterCure, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the time in seconds to wait after cure the layer
    /// </summary>
    public virtual float WaitTimeAfterCure
    {
        get => _waitTimeAfterCure;
        set
        {
            RaiseAndSet(ref _waitTimeAfterCure, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }

    /// <summary>
    /// Gets: Total bottom lift height (lift1 + lift2)
    /// Sets: Bottom lift1 with value and lift2 with 0
    /// </summary>
    public float BottomLiftHeightTotal
    {
        get => (float)Math.Round(BottomLiftHeight + BottomLiftHeight2, 2);
        set
        {
            BottomLiftHeight = (float)Math.Round(value, 2);
            BottomLiftHeight2 = 0;
        }
    }

    /// <summary>
    /// Gets: Total lift height (lift1 + lift2)
    /// Sets: Lift1 with value and lift2 with 0
    /// </summary>
    public float LiftHeightTotal
    {
        get => (float)Math.Round(LiftHeight + LiftHeight2, 2);
        set
        {
            LiftHeight = (float)Math.Round(value, 2);
            LiftHeight2 = 0;
        }
    }

    /// <summary>
    /// Gets or sets the bottom lift height in mm
    /// </summary>
    public virtual float BottomLiftHeight
    {
        get => _bottomLiftHeight;
        set
        {
            RaiseAndSet(ref _bottomLiftHeight, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(BottomLiftHeightTotal));
            RaisePropertyChanged(nameof(LiftRepresentation));
            BottomRetractHeight2 = BottomRetractHeight2; // Sanitize
        }
    }

    /// <summary>
    /// Gets or sets the bottom lift speed in mm/min
    /// </summary>
    public virtual float BottomLiftSpeed
    {
        get => _bottomLiftSpeed;
        set
        {
            RaiseAndSet(ref _bottomLiftSpeed, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the lift height in mm
    /// </summary>
    public virtual float LiftHeight
    {
        get => _liftHeight;
        set
        {
            RaiseAndSet(ref _liftHeight, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftHeightTotal));
            RaisePropertyChanged(nameof(LiftRepresentation));
            RetractHeight2 = RetractHeight2; // Sanitize
        }
    }

        

    /// <summary>
    /// Gets or sets the speed in mm/min
    /// </summary>
    public virtual float LiftSpeed
    {
        get => _liftSpeed;
        set
        {
            RaiseAndSet(ref _liftSpeed, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the second bottom lift height in mm
    /// </summary>
    public virtual float BottomLiftHeight2
    {
        get => _bottomLiftHeight2;
        set
        {
            RaiseAndSet(ref _bottomLiftHeight2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(BottomLiftHeightTotal));
            RaisePropertyChanged(nameof(LiftRepresentation));
            BottomRetractHeight2 = BottomRetractHeight2; // Sanitize
        }
    }

    /// <summary>
    /// Gets or sets the second bottom lift speed in mm/min
    /// </summary>
    public virtual float BottomLiftSpeed2
    {
        get => _bottomLiftSpeed2;
        set
        {
            RaiseAndSet(ref _bottomLiftSpeed2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the second lift height in mm (This is the closer to fep retract)
    /// </summary>
    public virtual float LiftHeight2
    {
        get => _liftHeight2;
        set
        {
            RaiseAndSet(ref _liftHeight2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftHeightTotal));
            RaisePropertyChanged(nameof(LiftRepresentation));
            RetractHeight2 = RetractHeight2; // Sanitize
        }
    }

        
    /// <summary>
    /// Gets or sets the second speed in mm/min (This is the closer to fep retract)
    /// </summary>
    public virtual float LiftSpeed2
    {
        get => _liftSpeed2;
        set
        {
            RaiseAndSet(ref _liftSpeed2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(LiftRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the bottom time in seconds to wait after lift / before retract
    /// </summary>
    public virtual float BottomWaitTimeAfterLift
    {
        get => _bottomWaitTimeAfterLift;
        set
        {
            RaiseAndSet(ref _bottomWaitTimeAfterLift, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the time in seconds to wait after lift / before retract
    /// </summary>
    public virtual float WaitTimeAfterLift
    {
        get => _waitTimeAfterLift;
        set
        {
            RaiseAndSet(ref _waitTimeAfterLift, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(WaitTimeRepresentation));
        }
    }

    /// <summary>
    /// Gets: Total bottom retract height (retract1 + retract2)  alias of <see cref="BottomLiftHeightTotal"/>
    /// </summary>
    public float BottomRetractHeightTotal => BottomLiftHeightTotal;

    /// <summary>
    /// Gets: Total retract height (retract1 + retract2) alias of <see cref="LiftHeightTotal"/>
    /// </summary>
    public float RetractHeightTotal => LiftHeightTotal;

    /// <summary>
    /// Gets the bottom retract height in mm
    /// </summary>
    public float BottomRetractHeight => (float)Math.Round(BottomLiftHeightTotal - BottomRetractHeight2, 2);

    /// <summary>
    /// Gets the speed in mm/min for the bottom retracts
    /// </summary>
    public virtual float BottomRetractSpeed
    {
        get => _bottomRetractSpeed;
        set
        {
            RaiseAndSet(ref _bottomRetractSpeed, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets the retract height in mm
    /// </summary>
    public float RetractHeight => (float)Math.Round(LiftHeightTotal - RetractHeight2, 2);
        
    /// <summary>
    /// Gets the speed in mm/min for the retracts
    /// </summary>
    public virtual float RetractSpeed
    {
        get => _retractSpeed;
        set
        {
            RaiseAndSet(ref _retractSpeed, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the second bottom retract height in mm
    /// </summary>
    public virtual float BottomRetractHeight2
    {
        get => _bottomRetractHeight2;
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, BottomRetractHeightTotal);
            RaiseAndSet(ref _bottomRetractHeight2, value);
            RaisePropertyChanged(nameof(BottomRetractHeight));
            RaisePropertyChanged(nameof(BottomRetractHeightTotal));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets the speed in mm/min for the retracts
    /// </summary>
    public virtual float BottomRetractSpeed2
    {
        get => _bottomRetractSpeed2;
        set
        {
            RaiseAndSet(ref _bottomRetractSpeed2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the second retract height in mm
    /// </summary>
    public virtual float RetractHeight2
    {
        get => _retractHeight2;
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
            RaiseAndSet(ref _retractHeight2, value);
            RaisePropertyChanged(nameof(RetractHeight));
            RaisePropertyChanged(nameof(RetractHeightTotal));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets the speed in mm/min for the retracts
    /// </summary>
    public virtual float RetractSpeed2
    {
        get => _retractSpeed2;
        set
        {
            RaiseAndSet(ref _retractSpeed2, (float)Math.Round(value, 2));
            RaisePropertyChanged(nameof(RetractRepresentation));
        }
    }

    /// <summary>
    /// Gets or sets the bottom pwm value from 0 to 255
    /// </summary>
    public virtual byte BottomLightPWM
    {
        get => _bottomLightPwm;
        set => RaiseAndSet(ref _bottomLightPwm, value);
    }

    /// <summary>
    /// Gets or sets the pwm value from 0 to 255
    /// </summary>
    public virtual byte LightPWM
    {
        get => _lightPwm;
        set => RaiseAndSet(ref _lightPwm, value);
    }

    /// <summary>
    /// Gets the minimum used speed for bottom layers in mm/min
    /// </summary>
    public float MinimumBottomSpeed
    {
        get
        {
            float speed = float.MaxValue;
            if (BottomLiftSpeed > 0) speed = Math.Min(speed, BottomLiftSpeed);
            if (CanUseBottomLiftSpeed2 && BottomLiftSpeed2 > 0) speed = Math.Min(speed, BottomLiftSpeed2);
            if (CanUseBottomRetractSpeed && BottomRetractSpeed > 0) speed = Math.Min(speed, BottomRetractSpeed);
            if (CanUseBottomRetractSpeed2 && BottomRetractSpeed2 > 0) speed = Math.Min(speed, BottomRetractSpeed2);
            if (Math.Abs(speed - float.MaxValue) < 0.01) return 0;

            return speed;
        }
    }

    /// <summary>
    /// Gets the minimum used speed for normal bottom layers in mm/min
    /// </summary>
    public float MinimumNormalSpeed
    {
        get
        {
            float speed = float.MaxValue;
            if (LiftSpeed > 0) speed = Math.Min(speed, LiftSpeed);
            if (CanUseLiftSpeed2 && LiftSpeed2 > 0) speed = Math.Min(speed, LiftSpeed2);
            if (CanUseRetractSpeed && RetractSpeed > 0) speed = Math.Min(speed, RetractSpeed);
            if (CanUseRetractSpeed2 && RetractSpeed2 > 0) speed = Math.Min(speed, RetractSpeed2);
            if (Math.Abs(speed - float.MaxValue) < 0.01) return 0;

            return speed;
        }
    }

    /// <summary>
    /// Gets the minimum used speed in mm/min
    /// </summary>
    public float MinimumSpeed
    {
        get
        {
            var bottomSpeed = MinimumBottomSpeed;
            var normalSpeed = MinimumNormalSpeed;
            if (bottomSpeed <= 0) return normalSpeed;
            if (normalSpeed <= 0) return bottomSpeed;

            return Math.Min(bottomSpeed, normalSpeed);
        }
    }

    /// <summary>
    /// Gets the maximum used speed for bottom layers in mm/min
    /// </summary>
    public float MaximumBottomSpeed
    {
        get
        {
            float speed = BottomLiftSpeed;
            if (CanUseBottomLiftSpeed2) speed = Math.Max(speed, BottomLiftSpeed2);
            if (CanUseBottomRetractSpeed) speed = Math.Max(speed, BottomRetractSpeed);
            if (CanUseBottomRetractSpeed2) speed = Math.Max(speed, BottomRetractSpeed2);

            return speed;
        }
    }

    /// <summary>
    /// Gets the maximum used speed for normal bottom layers in mm/min
    /// </summary>
    public float MaximumNormalSpeed
    {
        get
        {
            float speed = LiftSpeed;
            if (CanUseLiftSpeed2) speed = Math.Max(speed, LiftSpeed2);
            if (CanUseRetractSpeed) speed = Math.Max(speed, RetractSpeed);
            if (CanUseRetractSpeed2) speed = Math.Max(speed, RetractSpeed2);

            return speed;
        }
    }

    /// <summary>
    /// Gets the maximum used speed in mm/min
    /// </summary>
    public float MaximumSpeed => Math.Max(MaximumBottomSpeed, MaximumNormalSpeed);

    public bool CanUseBottomLayerCount => HavePrintParameterModifier(PrintParameterModifier.BottomLayerCount);
    public bool CanUseTransitionLayerCount => HavePrintParameterModifier(PrintParameterModifier.TransitionLayerCount);

    public bool CanUseBottomLightOffDelay => HavePrintParameterModifier(PrintParameterModifier.BottomLightOffDelay);
    public bool CanUseLightOffDelay => HavePrintParameterModifier(PrintParameterModifier.LightOffDelay);
    public bool CanUseAnyLightOffDelay => CanUseBottomLightOffDelay || CanUseLightOffDelay;

    public bool CanUseBottomWaitTimeBeforeCure => HavePrintParameterModifier(PrintParameterModifier.BottomWaitTimeBeforeCure);
    public bool CanUseWaitTimeBeforeCure => HavePrintParameterModifier(PrintParameterModifier.WaitTimeBeforeCure);
    public bool CanUseAnyWaitTimeBeforeCure => CanUseBottomWaitTimeBeforeCure || CanUseWaitTimeBeforeCure;

    public bool CanUseBottomExposureTime => HavePrintParameterModifier(PrintParameterModifier.BottomExposureTime);
    public bool CanUseExposureTime => HavePrintParameterModifier(PrintParameterModifier.ExposureTime);
    public bool CanUseAnyExposureTime => CanUseBottomExposureTime || CanUseExposureTime;

    public bool CanUseBottomWaitTimeAfterCure => HavePrintParameterModifier(PrintParameterModifier.BottomWaitTimeAfterCure);
    public bool CanUseWaitTimeAfterCure => HavePrintParameterModifier(PrintParameterModifier.WaitTimeAfterCure);
    public bool CanUseAnyWaitTimeAfterCure => CanUseBottomWaitTimeAfterCure || CanUseWaitTimeAfterCure;

    public bool CanUseBottomLiftHeight => HavePrintParameterModifier(PrintParameterModifier.BottomLiftHeight);
    public bool CanUseLiftHeight => HavePrintParameterModifier(PrintParameterModifier.LiftHeight);
    public bool CanUseAnyLiftHeight => CanUseBottomLiftHeight || CanUseLiftHeight;

    public bool CanUseBottomLiftSpeed => HavePrintParameterModifier(PrintParameterModifier.BottomLiftSpeed);
    public bool CanUseLiftSpeed => HavePrintParameterModifier(PrintParameterModifier.LiftSpeed);
    public bool CanUseAnyLiftSpeed => CanUseBottomLiftSpeed || CanUseLiftSpeed;

    public bool CanUseBottomLiftHeight2 => HavePrintParameterModifier(PrintParameterModifier.BottomLiftHeight2);
    public bool CanUseLiftHeight2 => HavePrintParameterModifier(PrintParameterModifier.LiftHeight2);
    public bool CanUseAnyLiftHeight2 => CanUseBottomLiftHeight2 || CanUseLiftHeight2;

    public bool CanUseBottomLiftSpeed2 => HavePrintParameterModifier(PrintParameterModifier.BottomLiftSpeed2);
    public bool CanUseLiftSpeed2 => HavePrintParameterModifier(PrintParameterModifier.LiftSpeed2);
    public bool CanUseAnyLiftSpeed2 => CanUseBottomLiftSpeed2 || CanUseLiftSpeed2;

    public bool CanUseBottomWaitTimeAfterLift => HavePrintParameterModifier(PrintParameterModifier.BottomWaitTimeAfterLift);
    public bool CanUseWaitTimeAfterLift => HavePrintParameterModifier(PrintParameterModifier.WaitTimeAfterLift);
    public bool CanUseAnyWaitTimeAfterLift => CanUseBottomWaitTimeAfterLift || CanUseWaitTimeAfterLift;

    public bool CanUseBottomRetractSpeed => HavePrintParameterModifier(PrintParameterModifier.BottomRetractSpeed);
    public bool CanUseRetractSpeed => HavePrintParameterModifier(PrintParameterModifier.RetractSpeed);
    public bool CanUseAnyRetractSpeed => CanUseBottomRetractSpeed || CanUseRetractSpeed;

    public bool CanUseBottomRetractHeight2 => HavePrintParameterModifier(PrintParameterModifier.BottomRetractHeight2);
    public bool CanUseRetractHeight2 => HavePrintParameterModifier(PrintParameterModifier.RetractHeight2);
    public bool CanUseAnyRetractHeight2 => CanUseBottomRetractHeight2 || CanUseRetractHeight2;
    public bool CanUseBottomRetractSpeed2 => HavePrintParameterModifier(PrintParameterModifier.BottomRetractSpeed2);
    public bool CanUseRetractSpeed2 => HavePrintParameterModifier(PrintParameterModifier.RetractSpeed2);
    public bool CanUseAnyRetractSpeed2 => CanUseBottomRetractSpeed2 || CanUseRetractSpeed2;

    public bool CanUseAnyWaitTime => CanUseBottomWaitTimeBeforeCure || CanUseBottomWaitTimeAfterCure || CanUseBottomWaitTimeAfterLift ||
                                     CanUseWaitTimeBeforeCure || CanUseWaitTimeAfterCure || CanUseWaitTimeAfterLift;

    public bool CanUseBottomLightPWM => HavePrintParameterModifier(PrintParameterModifier.BottomLightPWM);
    public bool CanUseLightPWM => HavePrintParameterModifier(PrintParameterModifier.LightPWM);
    public bool CanUseAnyLightPWM => CanUseBottomLightPWM || CanUseLightPWM;

    public bool CanUseLayerPositionZ => HaveLayerParameterModifier(PrintParameterModifier.PositionZ);
    public bool CanUseLayerWaitTimeBeforeCure => HaveLayerParameterModifier(PrintParameterModifier.WaitTimeBeforeCure);
    public bool CanUseLayerExposureTime => HaveLayerParameterModifier(PrintParameterModifier.ExposureTime);
    public bool CanUseLayerWaitTimeAfterCure => HaveLayerParameterModifier(PrintParameterModifier.WaitTimeAfterCure);
    public bool CanUseLayerLiftHeight => HaveLayerParameterModifier(PrintParameterModifier.LiftHeight);
    public bool CanUseLayerLiftSpeed => HaveLayerParameterModifier(PrintParameterModifier.LiftSpeed);
    public bool CanUseLayerLiftHeight2 => HaveLayerParameterModifier(PrintParameterModifier.LiftHeight2);
    public bool CanUseLayerLiftSpeed2 => HaveLayerParameterModifier(PrintParameterModifier.LiftSpeed2);
    public bool CanUseLayerWaitTimeAfterLift => HaveLayerParameterModifier(PrintParameterModifier.WaitTimeAfterLift);
    public bool CanUseLayerRetractSpeed => HaveLayerParameterModifier(PrintParameterModifier.RetractSpeed);
    public bool CanUseLayerRetractHeight2 => HaveLayerParameterModifier(PrintParameterModifier.RetractHeight2);
    public bool CanUseLayerRetractSpeed2 => HaveLayerParameterModifier(PrintParameterModifier.RetractSpeed2);
    public bool CanUseLayerLightOffDelay => HaveLayerParameterModifier(PrintParameterModifier.LightOffDelay);
    public bool CanUseLayerAnyWaitTimeBeforeCure => CanUseLayerWaitTimeBeforeCure || CanUseLayerLightOffDelay;
    public bool CanUseLayerLightPWM => HaveLayerParameterModifier(PrintParameterModifier.LightPWM);

    public string TransitionLayersRepresentation
    {
        get
        {
            var str = TransitionLayerCount.ToString(CultureInfo.InvariantCulture);

            if (!CanUseTransitionLayerCount)
            {
                return str;
            }

            if (TransitionLayerCount > 0)
            {
                var decrement = ParseTransitionStepTimeFromLayers();
                if (decrement != 0)
                {
                    str += $"/{-decrement}s";
                }
            }

            return str;
        }
    }

    public string ExposureRepresentation
    {
        get
        {
            var str = string.Empty;

            if (CanUseBottomExposureTime)
            {
                str += BottomExposureTime.ToString(CultureInfo.InvariantCulture);
            }
            if (CanUseExposureTime)
            {
                if (!string.IsNullOrEmpty(str)) str += '/';
                str += ExposureTime.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(str)) str += 's';

            return str;
        }
    }

    public string LiftRepresentation
    {
        get
        {
            var str = string.Empty;

            var haveBottomLiftHeight = CanUseBottomLiftHeight;
            var haveLiftHeight = CanUseLiftHeight;
            var haveBottomLiftHeight2 = CanUseBottomLiftHeight2;
            var haveLiftHeight2 = CanUseLiftHeight2;

            var haveBottomLiftSpeed2 = CanUseBottomLiftSpeed2;
            var haveLiftSpeed2 = CanUseLiftSpeed2;

            if (!haveBottomLiftHeight && !haveLiftHeight && !haveBottomLiftHeight2 && !haveLiftHeight2) return str;

            // Sequence 1
            if (haveBottomLiftHeight)
            {
                str += BottomLiftHeight.ToString(CultureInfo.InvariantCulture);
                if (haveBottomLiftHeight2 && BottomLiftHeight2 > 0)
                {
                    str += $"+{BottomLiftHeight2.ToString(CultureInfo.InvariantCulture)}";
                }
            }
               
            if (haveLiftHeight)
            {
                if (!string.IsNullOrEmpty(str)) str += '/';
                str += LiftHeight.ToString(CultureInfo.InvariantCulture);

                if (haveLiftHeight2 && LiftHeight2 > 0)
                {
                    str += $"+{LiftHeight2.ToString(CultureInfo.InvariantCulture)}";
                }
            }
               
            if (string.IsNullOrEmpty(str)) return str;

            str += "mm @ ";

            var haveBottomLiftSpeed = CanUseBottomLiftSpeed;
            var haveLiftSpeed = CanUseLiftSpeed;
            if (haveBottomLiftSpeed)
            {
                str += BottomLiftSpeed.ToString(CultureInfo.InvariantCulture);
                if (haveBottomLiftSpeed2 && haveBottomLiftHeight2 && BottomLiftHeight2 > 0)
                {
                    str += $"+{BottomLiftSpeed2.ToString(CultureInfo.InvariantCulture)}";
                }
            }
            if (haveLiftSpeed)
            {
                if (haveBottomLiftSpeed) str += '/';
                str += LiftSpeed.ToString(CultureInfo.InvariantCulture);
                if (haveLiftSpeed2 && haveLiftHeight2 && LiftHeight2 > 0)
                {
                    str += $"+{LiftSpeed2.ToString(CultureInfo.InvariantCulture)}";
                }
            }

            str += "mm/min";

            /*// Sequence 2
            if (haveBottomLiftHeight2)
            {
                str += $"\n2th: {BottomLiftHeight2.ToString(CultureInfo.InvariantCulture)}";
            }
            if (haveLiftHeight2)
            {
                str += str.EndsWith("mm/min") ? "\n2th: " : '/';
                str += LiftHeight2.ToString(CultureInfo.InvariantCulture);
            }

            if (str.EndsWith("mm/min")) return str;

            str += "mm @ ";

            var haveBottomLiftSpeed2 = CanUseBottomLiftSpeed2;
            var haveLiftSpeed2 = CanUseLiftSpeed2;
            if (haveBottomLiftSpeed2)
            {
                str += BottomLiftSpeed2.ToString(CultureInfo.InvariantCulture);
            }
            if (haveLiftSpeed2)
            {
                if (haveBottomLiftSpeed2) str += '/';
                str += LiftSpeed2.ToString(CultureInfo.InvariantCulture);
            }

            str += "mm/min";*/

            return str;
        }
    }

    public string RetractRepresentation
    {
        get
        {
            var str = string.Empty;

            var haveBottomRetractHeight = CanUseLiftHeight;
            var haveRetractHeight = CanUseBottomLiftHeight;
            var haveBottomRetractSpeed = CanUseBottomRetractSpeed;
            var haveRetractSpeed = CanUseRetractSpeed;
            var haveBottomRetractHeight2 = CanUseBottomRetractHeight2;
            var haveRetractHeight2 = CanUseRetractHeight2;
            var haveBottomRetractSpeed2 = CanUseBottomRetractSpeed2;
            var haveRetractSpeed2 = CanUseRetractSpeed2;

            if (!haveBottomRetractSpeed && !haveRetractSpeed && !haveBottomRetractHeight2 && !haveRetractHeight2) return str;

            // Sequence 1
            if (haveBottomRetractHeight)
            {
                str += BottomRetractHeight.ToString(CultureInfo.InvariantCulture);
                if (haveBottomRetractHeight2 && BottomRetractHeight2 > 0)
                {
                    str += $"+{BottomRetractHeight2.ToString(CultureInfo.InvariantCulture)}";
                }
            }
            if (haveRetractHeight)
            {
                if (!string.IsNullOrEmpty(str)) str += '/';
                str += RetractHeight.ToString(CultureInfo.InvariantCulture);
                if (haveRetractHeight2 && RetractHeight2 > 0)
                {
                    str += $"+{RetractHeight2.ToString(CultureInfo.InvariantCulture)}";
                }
            }

            if (string.IsNullOrEmpty(str)) return str;

            str += "mm @ ";

                
            if (haveBottomRetractSpeed)
            {
                str += BottomRetractSpeed.ToString(CultureInfo.InvariantCulture);
                if (haveBottomRetractSpeed2 && haveBottomRetractHeight2 && BottomRetractHeight2 > 0)
                {
                    str += $"+{BottomRetractSpeed2.ToString(CultureInfo.InvariantCulture)}";
                }
            }
            if (haveRetractSpeed)
            {
                if (haveBottomRetractSpeed) str += '/';
                str += RetractSpeed.ToString(CultureInfo.InvariantCulture);
                if (haveRetractSpeed2 && haveRetractHeight2 && RetractHeight2 > 0)
                {
                    str += $"+{RetractSpeed2.ToString(CultureInfo.InvariantCulture)}";
                }
            }

            str += "mm/min";

            // Sequence 2
            /*if (haveBottomRetractHeight2)
            {
                str += $"\n2th: {BottomRetractHeight2.ToString(CultureInfo.InvariantCulture)}";
            }
            if (haveRetractHeight2)
            {
                str += str.EndsWith("mm/min") ? "\n2th: " : '/';
                str += RetractHeight2.ToString(CultureInfo.InvariantCulture);
            }

            if (str.EndsWith("mm/min")) return str;

            str += "mm @ ";

            if (haveBottomRetractSpeed2)
            {
                str += BottomRetractSpeed2.ToString(CultureInfo.InvariantCulture);
            }
            if (haveRetractSpeed2)
            {
                if (haveBottomRetractSpeed2) str += '/';
                str += RetractSpeed2.ToString(CultureInfo.InvariantCulture);
            }

            str += "mm/min";*/

            return str;
        }
    }

    public string LightOffDelayRepresentation
    {
        get
        {
            var str = string.Empty;

            if (CanUseBottomLightOffDelay)
            {
                str += BottomLightOffDelay.ToString(CultureInfo.InvariantCulture);
            }
            if (CanUseLightOffDelay)
            {
                if (!string.IsNullOrEmpty(str)) str += '/';
                str += LightOffDelay.ToString(CultureInfo.InvariantCulture);
            }

            if (!string.IsNullOrEmpty(str)) str += 's';

            return str;
        }
    }

    public string WaitTimeRepresentation
    {
        get
        {
            var str = string.Empty;

            if (CanUseBottomWaitTimeBeforeCure || CanUseBottomWaitTimeAfterCure || CanUseBottomWaitTimeAfterLift)
            {
                str += $"{BottomWaitTimeBeforeCure}/{BottomWaitTimeAfterCure}/{BottomWaitTimeAfterLift}s";
            }
            if (!string.IsNullOrEmpty(str)) str += "|";
            if (CanUseWaitTimeBeforeCure || CanUseWaitTimeAfterCure || CanUseWaitTimeAfterLift)
            {
                str += $"{WaitTimeBeforeCure}/{WaitTimeAfterCure}/{WaitTimeAfterLift}s";
            }

            return str;
        }
    }

    public IEnumerable<IEnumerable<int>> BatchLayersIndexes(int batchSize = 0)
    {
        if (batchSize <= 0) batchSize = Environment.ProcessorCount * 10;
        return Enumerable.Range(0, (int) LayerCount).Chunk(batchSize);
    }

    public IEnumerable<IEnumerable<Layer>> BatchLayers(int batchSize = 0)
    {
        if (batchSize <= 0) batchSize = Environment.ProcessorCount * 10;
        return this.Chunk(batchSize);
    }

    #endregion

    /// <summary>
    /// Gets the estimate print time in seconds
    /// </summary>
    public virtual float PrintTime
    {
        get
        {
            if (_printTime <= 0)
            {
                _printTime = PrintTimeComputed;
            }
            return _printTime;
        } 
        set
        {
            if (value <= 0)
            {
                value = PrintTimeComputed;
            }
            if(!RaiseAndSetIfChanged(ref _printTime, value)) return;
            RaisePropertyChanged(nameof(PrintTimeHours));
            RaisePropertyChanged(nameof(PrintTimeString));
            RaisePropertyChanged(nameof(DisplayTotalOnTime));
            RaisePropertyChanged(nameof(DisplayTotalOnTimeString));
            RaisePropertyChanged(nameof(DisplayTotalOffTime));
            RaisePropertyChanged(nameof(DisplayTotalOffTimeString));
        }
    }

    /// <summary>
    /// Gets the calculated estimate print time in seconds
    /// </summary>
    public float PrintTimeComputed
    {
        get
        {
            if (LayerCount == 0) return 0;
            float time = ExtraPrintTime;
            bool computeGeneral = _layers is null;
            if (!computeGeneral)
            {
                foreach (var layer in this)
                {
                    if (layer is null)
                    {
                        computeGeneral = true;
                        break;
                    }

                    var motorTime = layer.CalculateMotorMovementTime();
                    time += layer.WaitTimeBeforeCure + layer.ExposureTime + layer.WaitTimeAfterCure + layer.WaitTimeAfterLift;
                    if (SupportsGCode)
                    {
                        time += motorTime;
                        if (layer.WaitTimeBeforeCure <= 0)
                        {
                            time += layer.LightOffDelay;
                        }
                    }
                    else
                    {
                        time += motorTime > layer.LightOffDelay ? motorTime : layer.LightOffDelay;
                    }
                    /*if (lightOffDelay >= layer.LightOffDelay)
                        time += lightOffDelay;
                    else
                        time += layer.LightOffDelay;*/
                }
            }

            if (computeGeneral)
            {
                var bottomMotorTime = CalculateMotorMovementTime(true);
                var motorTime = CalculateMotorMovementTime(false);
                time = ExtraPrintTime +
                       BottomLightOffDelay * BottomLayerCount +
                       LightOffDelay * NormalLayerCount +
                       BottomWaitTimeBeforeCure * BottomLayerCount +
                       WaitTimeBeforeCure * NormalLayerCount +
                       BottomExposureTime * BottomLayerCount +
                       ExposureTime * NormalLayerCount +
                       BottomWaitTimeAfterCure * BottomLayerCount +
                       WaitTimeAfterCure * NormalLayerCount +
                       BottomWaitTimeAfterLift * BottomLayerCount +
                       WaitTimeAfterLift * NormalLayerCount;

                if (SupportsGCode)
                {
                    time += bottomMotorTime * BottomLayerCount + motorTime * NormalLayerCount;

                    if (BottomWaitTimeBeforeCure <= 0)
                    {
                        time += BottomLightOffDelay * BottomLayerCount;
                    }
                    if (WaitTimeBeforeCure <= 0)
                    {
                        time += LightOffDelay * NormalLayerCount;
                    }
                }
                else
                {
                    time += motorTime > BottomLightOffDelay ? bottomMotorTime * BottomLayerCount : BottomLightOffDelay * BottomLayerCount;
                    time += motorTime > LightOffDelay ? motorTime * NormalLayerCount : LightOffDelay * NormalLayerCount;
                }
            }

            return (float) Math.Round(time, 2);
        }
    }

    /// <summary>
    /// Gets the estimate print time in hours
    /// </summary>
    public float PrintTimeHours => (float) Math.Round(PrintTime / 3600, 2);

    /// <summary>
    /// Gets the estimate print time in hours and minutes formatted
    /// </summary>
    public string PrintTimeString
    {
        get
        {
            var printTime = PrintTime;
            return TimeSpan.FromSeconds(float.IsPositiveInfinity(printTime) || float.IsNaN(printTime) ? 0 : printTime).ToString("hh\\hmm\\m");
        }
    }

    /// <summary>
    /// Gets the total time in seconds the display will remain on exposing the layers during the print
    /// </summary>
    public float DisplayTotalOnTime => (float)Math.Round(this.Where(layer => layer is not null).Sum(layer => layer.ExposureTime), 2);

    /// <summary>
    /// Gets the total time formatted in hours, minutes and seconds the display will remain on exposing the layers during the print
    /// </summary>
    public string DisplayTotalOnTimeString => TimeSpan.FromSeconds(DisplayTotalOnTime).ToString("hh\\hmm\\mss\\s");

    /// <summary>
    /// Gets the total time in seconds the display will remain off during the print.
    /// This is the difference between <see cref="PrintTime"/> and <see cref="DisplayTotalOnTime"/>
    /// </summary>
    public float DisplayTotalOffTime
    {
        get
        {
            var printTime = PrintTime;
            if (float.IsPositiveInfinity(printTime) || float.IsNaN(printTime)) return float.NaN;
            var value = (float) Math.Round(PrintTime - DisplayTotalOnTime, 2);
            return value <= 0 ? float.NaN : value;
        }
    }

    /// <summary>
    /// Gets the total time formatted in hours, minutes and seconds the display will remain off during the print.
    /// This is the difference between <see cref="PrintTime"/> and <see cref="DisplayTotalOnTime"/>
    /// </summary>
    public string DisplayTotalOffTimeString
    {
        get
        {
            var time = DisplayTotalOffTime;
            return TimeSpan.FromSeconds(float.IsPositiveInfinity(time) || float.IsNaN(time) ? 0 : time).ToString("hh\\hmm\\mss\\s");
        }
    }

    /// <summary>
    /// Gets the starting material milliliters when the file was loaded
    /// </summary>
    public float StartingMaterialMilliliters { get; private set; }

    /// <summary>
    /// Gets the estimate used material in ml
    /// </summary>
    public virtual float MaterialMilliliters {
        get => _materialMilliliters;
        set
        {
            if (value <= 0) // Recalculate
            {
                value = (float)Math.Round(this.Where(layer => layer is not null).Sum(layer => layer.MaterialMilliliters), 3);
            }
            else // Set from value
            {
                value = (float)Math.Round(value, 3);
            }

            if(!RaiseAndSetIfChanged(ref _materialMilliliters, value)) return;

            if (StartingMaterialMilliliters > 0 && StartingMaterialCost > 0)
            {
                MaterialCost = GetMaterialCostPer(_materialMilliliters);
            }
            //RaisePropertyChanged(nameof(MaterialCost));
        }
    }

    //public float MaterialMillilitersComputed =>


    /// <summary>
    /// Gets the estimate material in grams
    /// </summary>
    public virtual float MaterialGrams
    {
        get => _materialGrams;
        set => RaiseAndSetIfChanged(ref _materialGrams, (float)Math.Round(value, 3));
    }

    /// <summary>
    /// Gets the starting material cost when the file was loaded
    /// </summary>
    public float StartingMaterialCost { get; private set; }

    /// <summary>
    /// Gets the estimate material cost
    /// </summary>
    public virtual float MaterialCost
    {
        get => _materialCost;
        set => RaiseAndSetIfChanged(ref _materialCost, (float)Math.Round(value, 3));
    }

    /// <summary>
    /// Gets the material cost per one milliliter
    /// </summary>
    public float MaterialMilliliterCost => StartingMaterialMilliliters > 0 ? StartingMaterialCost / StartingMaterialMilliliters : 0;

    public float GetMaterialCostPer(float milliliters, byte roundDigits = 3) => (float)Math.Round(MaterialMilliliterCost * milliliters, roundDigits);

    /// <summary>
    /// Gets the material name
    /// </summary>
    public virtual string? MaterialName
    {
        get => _materialName;
        set => RaiseAndSetIfChanged(ref _materialName, value);
    }

    /// <summary>
    /// Gets the machine name
    /// </summary>
    public virtual string MachineName
    {
        get => _machineName;
        set
        {
            if(!RaiseAndSetIfChanged(ref _machineName, value)) return;
            if(FileType == FileFormatType.Binary) RequireFullEncode = true;
        }

    }

    /// <summary>
    /// Gets the GCode, returns null if not supported
    /// </summary>
    public GCodeBuilder? GCode { get; set; }

    /// <summary>
    /// Gets the GCode, returns null if not supported
    /// </summary>
    public string? GCodeStr
    {
        get => GCode?.ToString();
        set
        {
            if (GCode is null) return;
            GCode.Clear();
            if (!string.IsNullOrWhiteSpace(value))
            {
                GCode.Append(value);
            }
            RaisePropertyChanged();
                
        }
    }

    /// <summary>
    /// Gets if this file format supports gcode
    /// </summary>
    public virtual bool SupportsGCode => GCode is not null;

    /// <summary>
    /// Gets if this file have available gcode to read
    /// </summary>
    public bool HaveGCode => SupportsGCode && !GCode!.IsEmpty;

    /// <summary>
    /// Disable or enable the gcode auto rebuild when needed, set this to false to manually write your own gcode
    /// </summary>
    public bool SuppressRebuildGCode
    {
        get => _suppressRebuildGCode;
        set => RaiseAndSetIfChanged(ref _suppressRebuildGCode, value);
    }

    /// <summary>
    /// Get all configuration objects with properties and values
    /// </summary>
    public virtual object[] Configs => Array.Empty<object>();

    /// <summary>
    /// Gets if this file is valid to read
    /// </summary>
    public bool IsValid => FileFullPath is not null;
    #endregion

    #region Constructor
    protected FileFormat()
    {
        IssueManager = new(this);
        Thumbnails = new Mat[ThumbnailsCount];
        PropertyChanged += OnPropertyChanged;
        _queueTimerPrintTime.Elapsed += (sender, e) => UpdatePrintTime();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (SuppressRebuildProperties) return;
        if (
            e.PropertyName 
            is nameof(BottomLayerCount)
            or nameof(BottomLightOffDelay)
            or nameof(LightOffDelay)
            or nameof(BottomWaitTimeBeforeCure)
            or nameof(WaitTimeBeforeCure)
            or nameof(BottomExposureTime) 
            or nameof(ExposureTime)
            or nameof(BottomWaitTimeAfterCure)
            or nameof(WaitTimeAfterCure)
            or nameof(BottomLiftHeight) 
            or nameof(BottomLiftSpeed)
            or nameof(LiftHeight) 
            or nameof(LiftSpeed)
            or nameof(BottomLiftHeight2)
            or nameof(BottomLiftSpeed2)
            or nameof(LiftHeight2)
            or nameof(LiftSpeed2)
            or nameof(BottomWaitTimeAfterLift)
            or nameof(WaitTimeAfterLift)
            or nameof(BottomRetractSpeed) 
            or nameof(RetractSpeed)
            or nameof(BottomRetractHeight2)
            or nameof(BottomRetractSpeed2)
            or nameof(RetractHeight2)
            or nameof(RetractSpeed2) 
            or nameof(BottomLightPWM) 
            or nameof(LightPWM)
        )
        {
            RebuildLayersProperties(false, e.PropertyName);
            if(e.PropertyName 
                   is nameof(BottomLayerCount) 
                   or nameof(BottomExposureTime)
                   or nameof(ExposureTime)
               && TransitionLayerType == TransitionLayerTypes.Software
              ) ResetCurrentTransitionLayers(false);
                
            if(e.PropertyName 
               is not nameof(BottomLightPWM) 
               and not nameof(LightPWM)
              ) UpdatePrintTimeQueued();

            return;
        }

        // Fix transition layers times in software mode
        if (e.PropertyName is nameof(TransitionLayerCount) && TransitionLayerType == TransitionLayerTypes.Software)
        {
            ResetCurrentTransitionLayers();
            return;
        }
    }

    #endregion

    #region Indexers
    public Layer this[uint index]
    {
        get => _layers[index];
        set => SetLayer(index, value);
    }

    public Layer this[int index]
    {
        get => _layers[index];
        set => SetLayer((uint)index, value);
    }

    public Layer this[long index]
    {
        get => _layers[index];
        set => SetLayer((uint)index, value);
    }

    public Layer[] this[Range range] => _layers[range];

    /// <summary>
    /// Sets a layer
    /// </summary>
    /// <param name="index">Layer index</param>
    /// <param name="layer">Layer to add</param>
    /// <param name="makeClone">True to add a clone of the layer</param>
    public void SetLayer(uint index, Layer layer, bool makeClone = false)
    {
        if (index >= LayerCount) return;
        layer.IsModified = true;
        _layers[index] = makeClone ? layer.Clone() : layer;
        layer.Index = index;
        layer.SlicerFile = this;
    }

    /// <summary>
    /// Add a list of layers
    /// </summary>
    /// <param name="layers">Layers to add</param>
    /// <param name="makeClone">True to add a clone of layers</param>
    public void SetLayers(IEnumerable<Layer> layers, bool makeClone = false)
    {
        foreach (var layer in layers)
        {
            SetLayer(layer.Index, layer, makeClone);
        }
    }

    /// <summary>
    /// Get layer given index
    /// </summary>
    /// <param name="index">Layer index</param>
    /// <returns></returns>
    public Layer GetLayer(uint index)
    {
        return _layers[index];
    }

    #endregion

    #region Numerators
    public IEnumerator<Layer> GetEnumerator()
    {
        return ((IEnumerable<Layer>)Layers).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
    #endregion

    #region Overrides
    public override bool Equals(object? obj)
    {
        return Equals(obj as FileFormat);
    }

    public bool Equals(FileFormat? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return FileFullPath == other.FileFullPath;
    }

    public override int GetHashCode()
    {
        return (FileFullPath != null ? FileFullPath.GetHashCode() : 0);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _queueTimerPrintTime.Dispose();
        Clear();
    }

    #endregion

    #region Methods
    /// <summary>
    /// Clears all definitions and properties, it also dispose valid candidates 
    /// </summary>
    public virtual void Clear()
    {
        FileFullPath = null;
        _layers = Array.Empty<Layer>();
        GCode?.Clear();

        if (Thumbnails is not null)
        {
            for (int i = 0; i < Thumbnails.Length; i++)
            {
                Thumbnails[i]?.Dispose();
            }
        }
    }

    /// <summary>
    /// Check if a file is valid and can be processed before read it against the <see cref="FileFormat"/> decode scheme
    /// </summary>
    /// <param name="fileFullPath"></param>
    /// <returns></returns>
    public virtual bool CanProcess(string? fileFullPath)
    {
        if (fileFullPath is null) return false;
        if (!File.Exists(fileFullPath)) return false;
        //if (!IsExtensionValid(fileFullPath, true)) return false;
        return true;
    }


    /// <summary>
    /// Validate if a file is a valid <see cref="FileFormat"/>
    /// </summary>
    /// <param name="fileFullPath">Full file path</param>
    public void FileValidation(string? fileFullPath)
    {
        if (string.IsNullOrWhiteSpace(fileFullPath)) throw new ArgumentNullException(nameof(FileFullPath), "FileFullPath can't be null nor empty.");
        if (!File.Exists(fileFullPath)) throw new FileNotFoundException("The specified file does not exists.", fileFullPath);

        if (!IsExtensionValid(fileFullPath, true)) throw new FileLoadException("The specified file is not valid.", fileFullPath);
    }

    /// <summary>
    /// Checks if a extension is valid under the <see cref="FileFormat"/>
    /// </summary>
    /// <param name="extension">Extension to check without the dot (.)</param>
    /// <param name="isFilePath">True if <paramref name="extension"/> is a full file path, otherwise false for extension only</param>
    /// <returns>True if valid, otherwise false</returns>
    public bool IsExtensionValid(string extension, bool isFilePath = false)
    {
        if (isFilePath)
        {
            GetFileNameStripExtensions(extension, out extension);
        }
        return !string.IsNullOrWhiteSpace(extension) && FileExtensions.Any(fileExtension => fileExtension.Equals(extension));
    }

    /// <summary>
    /// Gets all valid file extensions in a specified format
    /// </summary>
    public string GetFileExtensions(string prepend = ".", string separator = ", ")
    {
        var result = string.Empty;

        foreach (var fileExt in FileExtensions)
        {
            if (!ReferenceEquals(result, string.Empty))
            {
                result += separator;
            }
            result += $"{prepend}{fileExt.Extension}";
        }

        return result;
    }

    public bool FileEndsWith(string extension)
    {
        if (FileFullPath is null) return false;
        if (extension[0] != '.') extension = $".{extension}";
        return FileFullPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
               FileFullPath.EndsWith($"{extension}{TemporaryFileAppend}", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets a thumbnail by it height or lower
    /// </summary>
    /// <param name="maxHeight">Max height allowed</param>
    /// <returns></returns>
    public Mat? GetThumbnail(uint maxHeight = 400)
    {
        for (int i = 0; i < ThumbnailsCount; i++)
        {
            if(Thumbnails[i] is null) continue;
            if (Thumbnails[i]!.Height <= maxHeight) return Thumbnails[i];
        }

        return null;
    }

    /// <summary>
    /// Gets a thumbnail by the largest or smallest
    /// </summary>
    /// <param name="largest">True to get the largest, otherwise false</param>
    /// <returns></returns>
    public Mat? GetThumbnail(bool largest)
    {
        switch (CreatedThumbnailsCount)
        {
            case 0:
                return null;
            case 1:
                return Thumbnails[0];
            default:
                if (largest)
                {
                    return Thumbnails[0]!.Size.Area() >= Thumbnails[1]!.Size.Area() ? Thumbnails[0] : Thumbnails[1];
                }
                else
                {
                    return Thumbnails[0]!.Size.Area() <= Thumbnails[1]!.Size.Area() ? Thumbnails[0] : Thumbnails[1];
                }
        }
    }

    /// <summary>
    /// Sets thumbnails from a list of thumbnails and clone them
    /// </summary>
    /// <param name="images"></param>
    public byte SetThumbnails(Mat?[] images)
    {
        if (images.Length == 0) return 0;
        byte imageIndex = 0;
        byte changed = 0;
        for (int i = 0; i < ThumbnailsCount; i++)
        {
            var image = images[Math.Min(imageIndex++, images.Length - 1)];
            if (image is null || image.IsEmpty)
            {
                if (imageIndex >= images.Length) break;
                i--;
                continue;
            }

            Thumbnails[i] = image.Clone();
            if (Thumbnails[i]!.Size != ThumbnailsOriginalSize![i])
            {
                CvInvoke.Resize(Thumbnails[i], Thumbnails[i], ThumbnailsOriginalSize[i]);
            }

            changed++;
        }

        if (changed <= 0) return 0;

        RaisePropertyChanged(nameof(Thumbnails));
        RequireFullEncode = true;
        return changed;

    }

    /// <summary>
    /// Sets all thumbnails the same image
    /// </summary>
    /// <param name="image">Image to set</param>
    public byte SetThumbnails(Mat image)
    {
        return SetThumbnails(new[] {image});
    }

    /// <summary>
    /// Sets all thumbnails from a disk file
    /// </summary>
    /// <param name="filePath"></param>
    public byte SetThumbnails(string filePath)
    {
        if (!File.Exists(filePath)) return 0;
        using var image = CvInvoke.Imread(filePath, ImreadModes.Color);
        return SetThumbnails(image);
    }

    /// <summary>
    /// Sets a thumbnail from mat
    /// </summary>
    /// <param name="index">Thumbnail index</param>
    /// <param name="image"></param>
    public bool SetThumbnail(int index, Mat image)
    {
        if (index >= Thumbnails.Length) return false;
        Thumbnails[index] = image.Clone();
        if (Thumbnails[index]!.Size != ThumbnailsOriginalSize![index])
        {
            CvInvoke.Resize(Thumbnails[index], Thumbnails[index], ThumbnailsOriginalSize[index]);
        }
        RaisePropertyChanged(nameof(Thumbnails));
        RequireFullEncode = true;
        return true;
    }
    

    /// <summary>
    /// Sets a thumbnail from a disk file
    /// </summary>
    /// <param name="index">Thumbnail index</param>
    /// <param name="filePath"></param>
    public bool SetThumbnail(int index, string filePath)
    {
        if (!File.Exists(filePath)) return false;
        if (index >= Thumbnails.Length) return false;
        Thumbnails[index] = CvInvoke.Imread(filePath, ImreadModes.Color);
        if (Thumbnails[index]!.Size != ThumbnailsOriginalSize![index])
        {
            CvInvoke.Resize(Thumbnails[index], Thumbnails[index], ThumbnailsOriginalSize[index]);
        }
        RaisePropertyChanged(nameof(Thumbnails));
        RequireFullEncode = true;
        return true;
    }

    /// <summary>
    /// Triggers before attempt to save/encode the file
    /// </summary>
    protected virtual void OnBeforeEncode(bool isPartialEncode){}

    /// <summary>
    /// Triggers after save/encode the file
    /// </summary>
    protected virtual void OnAfterEncode(bool isPartialEncode) { }

    /// <summary>
    /// Encode to an output file
    /// </summary>
    /// <param name="progress"></param>
    protected abstract void EncodeInternally(OperationProgress progress);

    /// <summary>
    /// Encode to an output file
    /// </summary>
    /// <param name="fileFullPath">Output file</param>
    /// <param name="progress"></param>
    public void Encode(string? fileFullPath, OperationProgress? progress = null)
    {
        fileFullPath ??= FileFullPath ?? throw new ArgumentNullException(nameof(fileFullPath));

        if (DecodeType == FileDecodeType.Partial) throw new InvalidOperationException("File was partial decoded, a full encode is not possible.");

        progress ??= new OperationProgress();
        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);

        Sanitize();
        OnBeforeEncode(false);

        for (var i = 0; i < Thumbnails.Length; i++)
        {
            if (Thumbnails[i] is null || Thumbnails[i]!.IsEmpty) continue;
            if(Thumbnails[i]!.Size == ThumbnailsOriginalSize![i]) continue;
            CvInvoke.Resize(Thumbnails[i], Thumbnails[i], new Size(ThumbnailsOriginalSize[i].Width, ThumbnailsOriginalSize[i].Height));
        }

        // Backup old file name and prepare the temporary file to be written next
        var oldFilePath = FileFullPath;
        FileFullPath = fileFullPath;
        var tempFile = TemporaryOutputFileFullPath;
        if (File.Exists(tempFile)) File.Delete(tempFile);

        try
        {
            EncodeInternally(progress);

            // Move temporary output file in place
            File.Move(tempFile, fileFullPath, true);

            IsModified = false;
            RequireFullEncode = false;

            OnAfterEncode(false);
        }
        catch (Exception)
        {
            // Restore backup file path and delete the temporary
            FileFullPath = oldFilePath;
            if (File.Exists(tempFile)) File.Delete(tempFile);
            throw;
        }
    }

    public void Encode(OperationProgress progress) => Encode(null, progress);

    public Task EncodeAsync(string? fileFullPath, OperationProgress? progress = null) =>
        Task.Run(() => Encode(fileFullPath, progress), progress?.Token ?? default);

    public Task EncodeAsync(OperationProgress progress) => EncodeAsync(null, progress);

    /// <summary>
    /// Decode a slicer file
    /// </summary>
    /// <param name="progress"></param>
    protected abstract void DecodeInternally(OperationProgress progress);

    /// <summary>
    /// Decode a slicer file
    /// </summary>
    /// <param name="fileFullPath">file path to load, use null to reload file</param>
    /// <param name="progress"></param>
    public void Decode(string? fileFullPath = null, OperationProgress? progress = null) => Decode(fileFullPath, FileDecodeType.Full, progress);

    /// <summary>
    /// Decode a slicer file
    /// </summary>
    /// <param name="fileFullPath">file path to load, use null to reload file</param>
    /// <param name="fileDecodeType"></param>
    /// <param name="progress"></param>
    public void Decode(string? fileFullPath, FileDecodeType fileDecodeType, OperationProgress? progress = null)
    {
        Clear();
        if(!string.IsNullOrWhiteSpace(fileFullPath)) FileFullPath = fileFullPath;
        FileValidation(FileFullPath);
        
        DecodeType = fileDecodeType;
        progress ??= new OperationProgress();
        progress.Reset(OperationProgress.StatusGatherLayers, LayerCount);

        DecodeInternally(progress);
        progress.ThrowIfCancellationRequested();

        var layerHeightDigits = LayerHeight.DecimalDigits();
        if (layerHeightDigits > Layer.HeightPrecision)
        {
            throw new FileLoadException($"The layer height ({LayerHeight}mm) have more decimal digits than the supported ({Layer.HeightPrecision}) digits.\n" +
                                        "Lower and fix your layer height on slicer to avoid precision errors.", fileFullPath);
        }

        IsModified = false;
        StartingMaterialMilliliters = MaterialMilliliters;
        StartingMaterialCost = MaterialCost;
        if (StartingMaterialCost <= 0)
        {
            StartingMaterialCost = StartingMaterialMilliliters * CoreSettings.AverageResin1000MlBottleCost / 1000f;
            MaterialCost = StartingMaterialCost;
        }

        if (CanUseTransitionLayerCount && TransitionLayerType == TransitionLayerTypes.Software)
        {
            SuppressRebuildPropertiesWork(() => TransitionLayerCount = ParseTransitionLayerCountFromLayers());
        }

        bool reSaveFile = Sanitize();
        if (reSaveFile)
        {
            Save(progress);
        }

        GetBoundingRectangle(progress);
    }

    public Task DecodeAsync(string? fileFullPath, FileDecodeType fileDecodeType, OperationProgress? progress = null) =>
        Task.Run(() => Decode(fileFullPath, fileDecodeType, progress), progress?.Token ?? default);

    public Task DecodeAsync(string? fileFullPath = null, OperationProgress? progress = null) 
        => DecodeAsync(fileFullPath, FileDecodeType.Full, progress);

    
    /// <summary>
    /// Reloads the file
    /// </summary>
    /// <param name="fileDecodeType"></param>
    /// <param name="progress"></param>
    public void Reload(FileDecodeType fileDecodeType, OperationProgress? progress = null) => Decode(null, fileDecodeType, progress);

    /// <summary>
    /// Reloads the file
    /// </summary>
    /// <param name="progress"></param>
    public void Reload(OperationProgress? progress = null) => Reload(FileDecodeType.Full, progress);

    /// <summary>
    /// Reloads the file
    /// </summary>
    /// <param name="fileDecodeType"></param>
    /// <param name="progress"></param>
    public Task ReloadAsync(FileDecodeType fileDecodeType, OperationProgress? progress = null) => DecodeAsync(null, fileDecodeType, progress);

    /// <summary>
    /// Reloads the file
    /// </summary>
    /// <param name="progress"></param>
    public Task ReloadAsync(OperationProgress? progress = null) => ReloadAsync(FileDecodeType.Full, progress);

    public void EncodeLayersInZip(ZipArchive zipArchive, string prepend, byte padDigits, IndexStartNumber layerIndexStartNumber = default, 
        OperationProgress? progress = null, string path = "", Func<uint, Mat, Mat>? matGenFunc = null)
    {
        if (DecodeType != FileDecodeType.Full || LayerCount == 0) return;
        progress ??= new OperationProgress();
        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var batches = BatchLayersIndexes();
        var pngLayerBytes = new byte[LayerCount][];

        var layerImageType = LayerImageType;

        foreach (var batch in batches)
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                if (matGenFunc is null)
                {
                    switch (layerImageType)
                    {
                        case FileImageType.Png24:
                        {
                            using var mat = this[layerIndex].LayerMat;
                            CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
                            pngLayerBytes[layerIndex] = mat.GetPngByes();

                            break;
                        }
                        case FileImageType.Png32:
                        {
                            using var mat = this[layerIndex].LayerMat;
                            CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgra);
                            pngLayerBytes[layerIndex] = mat.GetPngByes();

                            break;
                        }
                        case FileImageType.Png24BgrAA:
                        {
                            using var mat = this[layerIndex].LayerMat;
                            using var outputMat = mat.Reshape(3);
                            pngLayerBytes[layerIndex] = outputMat.GetPngByes();

                            break;
                        }
                        case FileImageType.Png24RgbAA:
                        {
                            using var mat = this[layerIndex].LayerMat;
                            using var outputMat = mat.Reshape(3);
                            CvInvoke.CvtColor(outputMat, outputMat, ColorConversion.Bgr2Rgb);
                            pngLayerBytes[layerIndex] = outputMat.GetPngByes();

                            break;
                        }
                        default:
                            pngLayerBytes[layerIndex] = this[layerIndex].CompressedPngBytes!;
                            break;
                    }
                }
                else
                {
                    using var mat = this[layerIndex].LayerMat;
                    using var newMat = matGenFunc.Invoke((uint) layerIndex, mat);
                    pngLayerBytes[layerIndex] = newMat.GetPngByes();
                }

                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                zipArchive.PutFileContent(Path.Combine(path, this[layerIndex].FormatFileName(prepend, padDigits, layerIndexStartNumber)), pngLayerBytes[layerIndex], ZipArchiveMode.Create);
                pngLayerBytes[layerIndex] = null!;
            }
        }
    }

    public void EncodeLayersInZip(ZipArchive zipArchive, byte padDigits, IndexStartNumber layerIndexStartNumber = default, OperationProgress? progress = null, string path = "", Func<uint, Mat, Mat>? matGenFunc = null)
        => EncodeLayersInZip(zipArchive, string.Empty, padDigits, layerIndexStartNumber, progress, path, matGenFunc);

    public void EncodeLayersInZip(ZipArchive zipArchive, string prepend, IndexStartNumber layerIndexStartNumber = default, OperationProgress? progress = null, string path = "", Func<uint, Mat, Mat>? matGenFunc = null)
        => EncodeLayersInZip(zipArchive, prepend, 0, layerIndexStartNumber, progress, path, matGenFunc);

    public void EncodeLayersInZip(ZipArchive zipArchive, IndexStartNumber layerIndexStartNumber, OperationProgress? progress = null, string path = "", Func<uint, Mat, Mat>? matGenFunc = null)
        => EncodeLayersInZip(zipArchive, string.Empty, 0, layerIndexStartNumber, progress, path, matGenFunc);

    public void EncodeLayersInZip(ZipArchive zipArchive, OperationProgress progress, string path = "", Func<uint, Mat, Mat>? matGenFunc = null)
        => EncodeLayersInZip(zipArchive, string.Empty, 0, IndexStartNumber.Zero, progress, path, matGenFunc);


    public void DecodeLayersFromZip(ZipArchiveEntry[] layerEntries, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
    {
        if (DecodeType != FileDecodeType.Full || LayerCount == 0) return;
        progress ??= new OperationProgress();
        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

        var layerImageType = LayerImageType;

        Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
        {
            byte[] pngBytes;
            lock (Mutex)
            {
                using var stream = layerEntries[layerIndex].Open();
                pngBytes = stream.ToArray();
            }

            if (matGenFunc is null)
            {
                switch (layerImageType)
                {
                    case FileImageType.Png24BgrAA:
                    {
                        using var bgrMat = new Mat();
                        CvInvoke.Imdecode(pngBytes, ImreadModes.Color, bgrMat);
                        using var greyMat = bgrMat.Reshape(1);

                        _layers[layerIndex] = new Layer((uint) layerIndex, greyMat, this);

                        break;
                    }
                    case FileImageType.Png24RgbAA:
                    {
                        using Mat rgbMat = new();
                        CvInvoke.Imdecode(pngBytes, ImreadModes.Color, rgbMat);
                        CvInvoke.CvtColor(rgbMat, rgbMat, ColorConversion.Bgr2Rgb);
                        using var greyMat = rgbMat.Reshape(1);

                        _layers[layerIndex] = new Layer((uint)layerIndex, greyMat, this);

                        break;
                    }
                    default:
                        _layers[layerIndex] = new Layer((uint)layerIndex, pngBytes, this);
                        break;
                }
            }
            else
            {
                using var mat = matGenFunc.Invoke((uint) layerIndex, pngBytes);
                _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
            }

            progress.LockAndIncrement();
        });
    }

    public void DecodeLayersFromZipRegex(ZipArchive zipArchive, string regex, IndexStartNumber layerIndexStartNumber = IndexStartNumber.Zero, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
    {
        var layerEntries = new ZipArchiveEntry?[LayerCount];

        foreach (var entry in zipArchive.Entries)
        {
            var match = Regex.Match(entry.Name, regex);
            if (!match.Success || match.Groups.Count < 2 || match.Groups[1].Value.Length == 0 || !uint.TryParse(match.Groups[1].Value, out var layerIndex)) continue;
            

            if (layerIndexStartNumber == IndexStartNumber.One && layerIndex > 0) layerIndex--;
            if (layerIndex >= LayerCount) continue;

            layerEntries[layerIndex] = entry;
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            if (layerEntries[layerIndex] is not null) continue;
            Clear();
            throw new FileLoadException($"Layer {layerIndex} not found", FileFullPath);
        }


        DecodeLayersFromZip(layerEntries!, progress, matGenFunc);
    }

    public void DecodeLayersFromZip(ZipArchive zipArchive, byte padDigits, IndexStartNumber layerIndexStartNumber = IndexStartNumber.Zero, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
        => DecodeLayersFromZipRegex(zipArchive, $@"(\d{{{padDigits}}}).png$", layerIndexStartNumber, progress, matGenFunc);

    public void DecodeLayersFromZip(ZipArchive zipArchive, string prepend, IndexStartNumber layerIndexStartNumber = IndexStartNumber.Zero, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
        => DecodeLayersFromZipRegex(zipArchive, $@"^{Regex.Escape(prepend)}(\d+).png$", layerIndexStartNumber, progress, matGenFunc);

    public void DecodeLayersFromZip(ZipArchive zipArchive, IndexStartNumber layerIndexStartNumber = IndexStartNumber.Zero, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
        => DecodeLayersFromZipRegex(zipArchive, @"^(\d+).png$", layerIndexStartNumber, progress, matGenFunc);

    public void DecodeLayersFromZip(ZipArchive zipArchive, OperationProgress progress, Func<uint, byte[], Mat>? matGenFunc = null)
        => DecodeLayersFromZipRegex(zipArchive, @"^(\d+).png$", IndexStartNumber.Zero, progress, matGenFunc);

    public void DecodeLayersFromZipIgnoreFilename(ZipArchive zipArchive, IndexStartNumber layerIndexStartNumber = IndexStartNumber.Zero, OperationProgress? progress = null, Func<uint, byte[], Mat>? matGenFunc = null)
    {
        DecodeLayersFromZipRegex(zipArchive, @$"({@"\d?".Repeat(LayerDigits - 1)}\d).png$", layerIndexStartNumber, progress, matGenFunc);
    }

    public void DecodeLayersFromZipIgnoreFilename(ZipArchive zipArchive, OperationProgress progress, Func<uint, byte[], Mat>? matGenFunc = null)
        => DecodeLayersFromZipIgnoreFilename(zipArchive, IndexStartNumber.Zero, progress, matGenFunc);

    /// <summary>
    /// Extract contents to a folder
    /// </summary>
    /// <param name="path">Path to folder where content will be extracted</param>
    /// <param name="genericConfigExtract"></param>
    /// <param name="genericLayersExtract"></param>
    /// <param name="progress"></param>
    public virtual void Extract(string path, bool genericConfigExtract = true, bool genericLayersExtract = true,
        OperationProgress? progress = null)
    {
        progress ??= new OperationProgress();
        progress.ItemName = OperationProgress.StatusExtracting;
        /*if (emptyFirst)
        {
            if (Directory.Exists(path))
            {
                DirectoryInfo di = new DirectoryInfo(path);

                foreach (FileInfo file in di.GetFiles())
                {
                    file.Delete();
                }
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    dir.Delete(true);
                }
            }
        }*/

        //if (!Directory.Exists(path))
        //{
        Directory.CreateDirectory(path);
        //}

        if (genericConfigExtract)
        {
            if (Configs.Length > 0)
            {
                using TextWriter tw = new StreamWriter(Path.Combine(path, $"{ExtractConfigFileName}.{ExtractConfigFileExtension}"), false);
                foreach (var config in Configs)
                {
                    var type = config.GetType();
                    tw.WriteLine($"[{type.Name}]");
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (property.Name.Equals("Item")) continue;
                        tw.WriteLine($"{property.Name} = {property.GetValue(config)}");
                    }

                    tw.WriteLine();
                }

                tw.Close();
            }
        }

        if (genericLayersExtract)
        {
            if (LayerCount > 0)
            {
                using TextWriter tw = new StreamWriter(Path.Combine(path, "Layers.ini"));
                for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    var layer = this[layerIndex];
                    tw.WriteLine($"[{layerIndex}]");
                    tw.WriteLine($"{nameof(layer.NonZeroPixelCount)}: {layer.NonZeroPixelCount}");
                    tw.WriteLine($"{nameof(layer.BoundingRectangle)}: {layer.BoundingRectangle}");
                    tw.WriteLine($"{nameof(layer.IsBottomLayer)}: {layer.IsBottomLayer}");
                    tw.WriteLine($"{nameof(layer.LayerHeight)}: {layer.LayerHeight}");
                    tw.WriteLine($"{nameof(layer.PositionZ)}: {layer.PositionZ}");

                    if (CanUseLayerLightOffDelay)
                        tw.WriteLine($"{nameof(layer.LightOffDelay)}: {layer.LightOffDelay}");
                    if (CanUseLayerWaitTimeBeforeCure)
                        tw.WriteLine($"{nameof(layer.WaitTimeBeforeCure)}: {layer.WaitTimeBeforeCure}");
                    tw.WriteLine($"{nameof(layer.ExposureTime)}: {layer.ExposureTime}");
                    if (CanUseLayerWaitTimeAfterCure)
                        tw.WriteLine($"{nameof(layer.WaitTimeAfterCure)}: {layer.WaitTimeAfterCure}");


                    if (CanUseLayerLiftHeight)
                        tw.WriteLine($"{nameof(layer.LiftHeight)}: {layer.LiftHeight}");
                    if (CanUseLayerLiftSpeed)
                        tw.WriteLine($"{nameof(layer.LiftSpeed)}: {layer.LiftSpeed}");
                    if (CanUseLayerLiftHeight2)
                        tw.WriteLine($"{nameof(layer.LiftHeight2)}: {layer.LiftHeight2}");
                    if (CanUseLayerLiftSpeed2)
                        tw.WriteLine($"{nameof(layer.LiftSpeed2)}: {layer.LiftSpeed2}");
                    if (CanUseLayerWaitTimeAfterLift)
                        tw.WriteLine($"{nameof(layer.WaitTimeAfterLift)}: {layer.WaitTimeAfterLift}");
                    if (CanUseLayerRetractSpeed)
                    {
                        tw.WriteLine($"{nameof(layer.RetractHeight)}: {layer.RetractHeight}");
                        tw.WriteLine($"{nameof(layer.RetractSpeed)}: {layer.RetractSpeed}");
                    }
                    if (CanUseLayerRetractHeight2)
                        tw.WriteLine($"{nameof(layer.RetractHeight2)}: {layer.RetractHeight2}");
                    if (CanUseLayerRetractSpeed2)
                        tw.WriteLine($"{nameof(layer.RetractSpeed2)}: {layer.RetractSpeed2}");

                    if (CanUseLayerLightPWM)
                        tw.WriteLine($"{nameof(layer.LightPWM)}: {layer.LightPWM}");

                    var materialMillilitersPercent = layer.MaterialMillilitersPercent;
                    if (!float.IsNaN(materialMillilitersPercent))
                    {
                        tw.WriteLine($"{nameof(layer.MaterialMilliliters)}: {layer.MaterialMilliliters}ml ({materialMillilitersPercent:F2}%)");
                    }

                    tw.WriteLine();
                }
                tw.Close();
            }
        }


        if (FileType == FileFormatType.Archive)
        {
            if (FileFullPath is not null)
            {
                progress.CanCancel = false;
                ZipArchiveExtensions.ImprovedExtractToDirectory(FileFullPath, path, ZipArchiveExtensions.Overwrite.Always);
                return;
            }
        }

        progress.ItemCount = LayerCount;

        if (genericLayersExtract)
        {
            uint i = 0;
            foreach (var thumbnail in Thumbnails)
            {
                if (thumbnail is null)
                {
                    continue;
                }

                thumbnail.Save(Path.Combine(path, $"Thumbnail{i}.png"));
                i++;
            }

            if (LayerCount > 0 && DecodeType == FileDecodeType.Full)
            {
                Parallel.ForEach(this, CoreSettings.GetParallelOptions(progress), layer =>
                {
                    var byteArr = layer.CompressedPngBytes;
                    if (byteArr is null) return;
                    using var stream = new FileStream(Path.Combine(path, layer.Filename), FileMode.Create, FileAccess.Write);
                    stream.Write(byteArr, 0, byteArr.Length);
                    progress.LockAndIncrement();
                });
            }
        }
    }

    /// <summary>
    /// Gets the transition layer count calculated from layer exposure time configuration
    /// </summary>
    /// <returns>Transition layer count</returns>
    public ushort ParseTransitionLayerCountFromLayers()
    {
        ushort count = 0;
        for (uint layerIndex = BottomLayerCount + 1u; layerIndex < LayerCount; layerIndex++)
        {
            if (Math.Abs(this[layerIndex - 1].ExposureTime - this[layerIndex].ExposureTime) < 0.009f) break; // First equal layer, transition ended
            count++;
        }

        return count;
    }

    /// <summary>
    /// Parse the transition step time from layers, value is returned as positive from normal perspective and logic (Longer - shorter)
    /// </summary>
    /// <returns>Seconds</returns>
    public float ParseTransitionStepTimeFromLayers()
    {
        var transitionLayerCount = ParseTransitionLayerCountFromLayers();
        return transitionLayerCount == 0
            ? 0
            : (float)Math.Round(this[BottomLayerCount].ExposureTime - this[BottomLayerCount + 1].ExposureTime, 2);
    }

    /// <summary>
    /// Gets the transition step time from a long and short exposure time, value is returned as positive from normal perspective and logic (Longer - shorter)
    /// </summary>
    /// <param name="longExposureTime">The long exposure time</param>
    /// <param name="shortExposureTime">The small exposure time</param>
    /// <param name="transitionLayerCount">Number of transition layers</param>
    /// <returns>Seconds</returns>
    public static float GetTransitionStepTime(float longExposureTime, float shortExposureTime, ushort transitionLayerCount)
    {
        return transitionLayerCount == 0 ? 0 : (float)Math.Round((longExposureTime - shortExposureTime) / (transitionLayerCount + 1), 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Gets the transition step time from <see cref="BottomExposureTime"/> and <see cref="ExposureTime"/>, value is returned as positive from normal perspective and logic (Longer - shorter)
    /// </summary>
    /// <param name="transitionLayerCount">Number of transition layers</param>
    /// <returns>Seconds</returns>
    public float GetTransitionStepTime(ushort transitionLayerCount)
    {
        return GetTransitionStepTime(BottomExposureTime, ExposureTime, transitionLayerCount);
    }

    /// <summary>
    /// Gets the transition step time from <see cref="BottomExposureTime"/> and <see cref="ExposureTime"/>, value is returned as positive from normal perspective and logic (Longer - shorter)
    /// </summary>
    /// <returns>Seconds</returns>
    public float GetTransitionStepTime() => GetTransitionStepTime(TransitionLayerCount);

    /// <summary>
    /// Gets the transition layer count based on long and short exposure time
    /// </summary>
    /// <param name="longExposureTime">The long exposure time</param>
    /// <param name="shortExposureTime">The small exposure time</param>
    /// <param name="decrementTime">Decrement time</param>
    /// <param name="rounding">Midpoint rounding method</param>
    /// <returns></returns>
    public static ushort GetTransitionLayerCount(float longExposureTime, float shortExposureTime, float decrementTime, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        return decrementTime == 0 ? (ushort)0 : (ushort)Math.Round((longExposureTime - shortExposureTime) / decrementTime - 1, rounding);
    }

    /// <summary>
    /// Gets the transition layer count based on <see cref="BottomExposureTime"/> and <see cref="ExposureTime"/>
    /// </summary>
    /// <param name="stepDecrementTime">Step decrement time in seconds</param>
    /// <param name="constrainToLayerCount">True if transition layer count can't be higher than supported by the file, otherwise set to false to not look at possible file layers</param>
    /// <param name="rounding">Midpoint rounding method</param>
    /// <returns>Transition layer count</returns>
    public ushort GetTransitionLayerCount(float stepDecrementTime, bool constrainToLayerCount = true, MidpointRounding rounding = MidpointRounding.AwayFromZero)
    {
        var count = GetTransitionLayerCount(BottomExposureTime, ExposureTime, stepDecrementTime, rounding);
        if (constrainToLayerCount) count = (ushort)Math.Min(count, MaximumPossibleTransitionLayerCount);
        return count;
    }


    /// <summary>
    /// Re-set exposure time to the transition layers
    /// </summary>
    /// <param name="resetExposureTimes">True to default all the previous transition layers exposure time, otherwise false</param>
    public void ResetCurrentTransitionLayers(bool resetExposureTimes = true)
    {
        if (TransitionLayerType != TransitionLayerTypes.Software) return;
        SetTransitionLayers(TransitionLayerCount, resetExposureTimes);
    }

    /// <summary>
    /// Set transition layers and exposure times, but do not set that count to file property <see cref="TransitionLayerCount"/>
    /// </summary>
    /// <param name="transitionLayerCount">Number of transition layers to set</param>
    /// <param name="resetExposureTimes">True to default all the previous transition layers exposure time, otherwise false</param>
    public void SetTransitionLayers(ushort transitionLayerCount, bool resetExposureTimes = true)
    {
        if (resetExposureTimes)
        {
            for (uint layerIndex = BottomLayerCount; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                if (Math.Abs(layer.ExposureTime - ExposureTime) < 0.009) break; // First equal layer, transition ended

                layer.ExposureTime = ExposureTime;
            }
        }

        if (transitionLayerCount == 0) return;

        float decrement = GetTransitionStepTime(transitionLayerCount);
        if (decrement <= 0) return;

        uint appliedLayers = 0;
        for (uint layerIndex = BottomLayerCount; appliedLayers < transitionLayerCount && layerIndex < LayerCount; layerIndex++)
        {
            appliedLayers++;
            this[layerIndex].ExposureTime = Math.Clamp(BottomExposureTime - (decrement * appliedLayers), ExposureTime, BottomExposureTime);
        }
    }

    /// <summary>
    /// Get height in mm from layer height
    /// </summary>
    /// <param name="layerIndex"></param>
    /// <param name="realHeight"></param>
    /// <returns>The height in mm</returns>
    public float GetHeightFromLayer(uint layerIndex, bool realHeight = true)
    {
        return Layer.RoundHeight((layerIndex + (realHeight ? 1 : 0)) * LayerHeight);
    }

    /// <summary>
    /// Gets the global value for bottom or normal layers based on layer index
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="layerIndex">Layer index</param>
    /// <param name="bottomValue">Initial value</param>
    /// <param name="normalValue">Normal value</param>
    /// <returns></returns>
    public T GetBottomOrNormalValue<T>(uint layerIndex, T bottomValue, T normalValue)
    {
        return layerIndex < BottomLayerCount ? bottomValue : normalValue;
    }

    /// <summary>
    /// Gets the global value for bottom or normal layers based on layer
    /// </summary>
    /// <typeparam name="T">Type of value</typeparam>
    /// <param name="layer">Layer</param>
    /// <param name="bottomValue">Initial value</param>
    /// <param name="normalValue">Normal value</param>
    /// <returns></returns>
    public T GetBottomOrNormalValue<T>(Layer layer, T bottomValue, T normalValue)
    {
        return layer.IsBottomLayer ? bottomValue : normalValue;
    }

    /// <summary>
    /// Refresh print parameters globals with this file settings
    /// </summary>
    public void RefreshPrintParametersModifiersValues()
    {
        if (PrintParameterModifiers is null) return;
        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLayerCount))
        {
            PrintParameterModifier.BottomLayerCount.Value = BottomLayerCount;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.TransitionLayerCount))
        {
            PrintParameterModifier.TransitionLayerCount.Value = TransitionLayerCount;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLightOffDelay))
        {
            PrintParameterModifier.BottomLightOffDelay.Value = (decimal)BottomLightOffDelay;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LightOffDelay))
        {
            PrintParameterModifier.LightOffDelay.Value = (decimal)LightOffDelay;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomWaitTimeBeforeCure))
        {
            PrintParameterModifier.BottomWaitTimeBeforeCure.Value = (decimal)BottomWaitTimeBeforeCure;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.WaitTimeBeforeCure))
        {
            PrintParameterModifier.WaitTimeBeforeCure.Value = (decimal)WaitTimeBeforeCure;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomExposureTime))
        {
            PrintParameterModifier.BottomExposureTime.Value = (decimal) BottomExposureTime;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.ExposureTime))
        {
            PrintParameterModifier.ExposureTime.Value = (decimal)ExposureTime;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomWaitTimeAfterCure))
        {
            PrintParameterModifier.BottomWaitTimeAfterCure.Value = (decimal)BottomWaitTimeAfterCure;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.WaitTimeAfterCure))
        {
            PrintParameterModifier.WaitTimeAfterCure.Value = (decimal)WaitTimeAfterCure;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftHeight))
        {
            PrintParameterModifier.BottomLiftHeight.Value = (decimal)BottomLiftHeight;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftSpeed))
        {
            PrintParameterModifier.BottomLiftSpeed.Value = (decimal)BottomLiftSpeed;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftHeight))
        {
            PrintParameterModifier.LiftHeight.Value = (decimal)LiftHeight;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftSpeed))
        {
            PrintParameterModifier.LiftSpeed.Value = (decimal)LiftSpeed;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftHeight2))
        {
            PrintParameterModifier.BottomLiftHeight2.Value = (decimal)BottomLiftHeight2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftSpeed2))
        {
            PrintParameterModifier.BottomLiftSpeed2.Value = (decimal)BottomLiftSpeed2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftHeight2))
        {
            PrintParameterModifier.LiftHeight2.Value = (decimal)LiftHeight2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftSpeed2))
        {
            PrintParameterModifier.LiftSpeed2.Value = (decimal)LiftSpeed2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomWaitTimeAfterLift))
        {
            PrintParameterModifier.BottomWaitTimeAfterLift.Value = (decimal)BottomWaitTimeAfterLift;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.WaitTimeAfterLift))
        {
            PrintParameterModifier.WaitTimeAfterLift.Value = (decimal)WaitTimeAfterLift;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomRetractSpeed))
        {
            PrintParameterModifier.BottomRetractSpeed.Value = (decimal)BottomRetractSpeed;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.RetractSpeed))
        {
            PrintParameterModifier.RetractSpeed.Value = (decimal)RetractSpeed;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomRetractHeight2))
        {
            PrintParameterModifier.BottomRetractHeight2.Value = (decimal)BottomRetractHeight2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomRetractSpeed2))
        {
            PrintParameterModifier.BottomRetractSpeed2.Value = (decimal)BottomRetractSpeed2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.RetractHeight2))
        {
            PrintParameterModifier.RetractHeight2.Value = (decimal)RetractHeight2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.RetractSpeed2))
        {
            PrintParameterModifier.RetractSpeed2.Value = (decimal)RetractSpeed2;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLightPWM))
        {
            PrintParameterModifier.BottomLightPWM.Value = BottomLightPWM;
        }

        if (PrintParameterModifiers.Contains(PrintParameterModifier.LightPWM))
        {
            PrintParameterModifier.LightPWM.Value = LightPWM;
        }
    }

    /// <summary>
    /// Refresh print parameters per layer globals with this file settings
    /// </summary>
    public void RefreshPrintParametersPerLayerModifiersValues(uint layerIndex)
    {
        if (PrintParameterPerLayerModifiers is null) return;
        var layer = this[layerIndex];

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.PositionZ))
        {
            PrintParameterModifier.PositionZ.Value = (decimal)layer.PositionZ;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LightOffDelay))
        {
            PrintParameterModifier.LightOffDelay.Value = (decimal)layer.LightOffDelay;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.WaitTimeBeforeCure))
        {
            PrintParameterModifier.WaitTimeBeforeCure.Value = (decimal)layer.WaitTimeBeforeCure;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.ExposureTime))
        {
            PrintParameterModifier.ExposureTime.Value = (decimal)layer.ExposureTime;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.WaitTimeAfterCure))
        {
            PrintParameterModifier.WaitTimeAfterCure.Value = (decimal)layer.WaitTimeAfterCure;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LiftHeight))
        {
            PrintParameterModifier.LiftHeight.Value = (decimal)layer.LiftHeight;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LiftSpeed))
        {
            PrintParameterModifier.LiftSpeed.Value = (decimal)layer.LiftSpeed;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LiftHeight2))
        {
            PrintParameterModifier.LiftHeight2.Value = (decimal)layer.LiftHeight2;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LiftSpeed2))
        {
            PrintParameterModifier.LiftSpeed2.Value = (decimal)layer.LiftSpeed2;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.WaitTimeAfterLift))
        {
            PrintParameterModifier.WaitTimeAfterLift.Value = (decimal)layer.WaitTimeAfterLift;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.RetractSpeed))
        {
            PrintParameterModifier.RetractSpeed.Value = (decimal)layer.RetractSpeed;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.RetractHeight2))
        {
            PrintParameterModifier.RetractHeight2.Value = (decimal)layer.RetractHeight2;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.RetractSpeed2))
        {
            PrintParameterModifier.RetractSpeed2.Value = (decimal)layer.RetractSpeed2;
        }

        if (PrintParameterPerLayerModifiers.Contains(PrintParameterModifier.LightPWM))
        {
            PrintParameterModifier.LightPWM.Value = layer.LightPWM;
        }
    }

    /// <summary>
    /// Gets the value attributed to <see cref="FileFormat.PrintParameterModifier"/>
    /// </summary>
    /// <param name="modifier">Modifier to use</param>
    /// <returns>A value</returns>
    public object? GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
    {
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerCount))
            return BottomLayerCount;

        if (ReferenceEquals(modifier, PrintParameterModifier.TransitionLayerCount))
            return TransitionLayerCount;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightOffDelay))
            return BottomLightOffDelay;
        if (ReferenceEquals(modifier, PrintParameterModifier.LightOffDelay))
            return LightOffDelay;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeBeforeCure))
            return BottomWaitTimeBeforeCure;
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeBeforeCure))
            return WaitTimeBeforeCure;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomExposureTime))
            return BottomExposureTime;
        if (ReferenceEquals(modifier, PrintParameterModifier.ExposureTime))
            return ExposureTime;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeAfterCure))
            return BottomWaitTimeAfterCure;
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeAfterCure))
            return WaitTimeAfterCure;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
            return BottomLiftHeight;
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
            return LiftHeight;
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
            return BottomLiftSpeed;
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
            return LiftSpeed;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight2))
            return BottomLiftHeight2;
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight2))
            return LiftHeight2;
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed2))
            return BottomLiftSpeed2;
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed2))
            return LiftSpeed2;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeAfterLift))
            return BottomWaitTimeAfterLift;
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeAfterLift))
            return WaitTimeAfterLift;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractSpeed))
            return BottomRetractSpeed;
        if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            return RetractSpeed;

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractHeight2))
            return BottomRetractHeight2;
        if (ReferenceEquals(modifier, PrintParameterModifier.RetractHeight2))
            return RetractHeight2;
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractSpeed2))
            return BottomRetractSpeed2;
        if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed2))
            return RetractSpeed2;



        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
            return BottomLightPWM;
        if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
            return LightPWM;

        return null;
    }

    /// <summary>
    /// Sets a property value attributed to <paramref name="modifier"/>
    /// </summary>
    /// <param name="modifier">Modifier to use</param>
    /// <param name="value">Value to set</param>
    /// <returns>True if set, otherwise false <paramref name="modifier"/> not found</returns>
    public bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, decimal value)
    {
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerCount))
        {
            BottomLayerCount = (ushort)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.TransitionLayerCount))
        {
            TransitionLayerCount = (ushort)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightOffDelay))
        {
            BottomLightOffDelay = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LightOffDelay))
        {
            LightOffDelay = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeBeforeCure))
        {
            BottomWaitTimeBeforeCure = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeBeforeCure))
        {
            WaitTimeBeforeCure = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomExposureTime))
        {
            BottomExposureTime = (float) value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.ExposureTime))
        {
            ExposureTime = (float) value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeAfterCure))
        {
            BottomWaitTimeAfterCure = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeAfterCure))
        {
            WaitTimeAfterCure = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
        {
            BottomLiftHeight = (float) value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
        {
            LiftHeight = (float) value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
        {
            BottomLiftSpeed = (float) value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
        {
            LiftSpeed = (float) value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight2))
        {
            BottomLiftHeight2 = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight2))
        {
            LiftHeight2 = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed2))
        {
            BottomLiftSpeed2 = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed2))
        {
            LiftSpeed2 = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomWaitTimeAfterLift))
        {
            BottomWaitTimeAfterLift = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.WaitTimeAfterLift))
        {
            WaitTimeAfterLift = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractSpeed))
        {
            BottomRetractSpeed = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
        {
            RetractSpeed = (float) value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractHeight2))
        {
            BottomRetractHeight2 = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.RetractHeight2))
        {
            RetractHeight2 = (float)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.BottomRetractSpeed2))
        {
            BottomRetractSpeed2 = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed2))
        {
            RetractSpeed2 = (float)value;
            return true;
        }

        if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
        {
            BottomLightPWM = (byte)value;
            return true;
        }
        if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
        {
            LightPWM = (byte)value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets properties from print parameters
    /// </summary>
    /// <returns>Number of affected parameters</returns>
    public byte SetValuesFromPrintParametersModifiers()
    {
        if (PrintParameterModifiers is null) return 0;
        byte changed = 0;
        foreach (var modifier in PrintParameterModifiers)
        {
            if(!modifier.HasChanged) continue;
            modifier.OldValue = modifier.NewValue;
            SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
            changed++;
        }

        return changed;
    }

    public void SetNoDelays()
    {
        BottomLightOffDelay = 0;
        LightOffDelay = 0;
        BottomWaitTimeBeforeCure = 0;
        WaitTimeBeforeCure = 0;
        BottomWaitTimeAfterCure = 0;
        WaitTimeAfterCure = 0;
        BottomWaitTimeAfterLift = 0;
        WaitTimeAfterLift = 0;
    }

    public float CalculateBottomLightOffDelay(float extraTime = 0) => CalculateLightOffDelay(true, extraTime);

    public bool SetBottomLightOffDelay(float extraTime = 0) => SetLightOffDelay(true, extraTime);

    public float CalculateNormalLightOffDelay(float extraTime = 0) => CalculateLightOffDelay(false, extraTime);

    public bool SetNormalLightOffDelay(float extraTime = 0) => SetLightOffDelay(false, extraTime);

    public float CalculateMotorMovementTime(bool isBottomLayer, float extraTime = 0)
    {
        return isBottomLayer
            ? OperationCalculator.LightOffDelayC.CalculateSeconds(BottomLiftHeight, BottomLiftSpeed, BottomRetractSpeed, extraTime, BottomLiftHeight2, BottomLiftSpeed2, BottomRetractHeight2, BottomRetractSpeed2)
            : OperationCalculator.LightOffDelayC.CalculateSeconds(LiftHeight, LiftSpeed, RetractSpeed, extraTime, LiftHeight2, LiftSpeed2, RetractHeight2, RetractSpeed2);
    }

    public float CalculateLightOffDelay(bool isBottomLayer, float extraTime = 0)
    {
        extraTime = (float)Math.Round(extraTime, 2);
        if (SupportsGCode) return extraTime;
        return CalculateMotorMovementTime(isBottomLayer, extraTime);
    }

    public bool SetLightOffDelay(bool isBottomLayer, float extraTime = 0)
    {
        float lightOff = CalculateLightOffDelay(isBottomLayer, extraTime);
        if (isBottomLayer)
        {
            if (BottomLightOffDelay != lightOff)
            {
                BottomLightOffDelay = lightOff;
                return true;
            }

            return false;
        }
            
        if (LightOffDelay != lightOff)
        {
            LightOffDelay = lightOff;
            return true;
        }

        return false;
    }

    public float GetWaitTimeBeforeCure(bool isBottomLayer)
    {
        return isBottomLayer ? GetBottomWaitTimeBeforeCure() : GetNormalWaitTimeBeforeCure();
    }

    /// <summary>
    /// Gets the bottom wait time before cure, if not available calculate it from light off delay
    /// </summary>
    /// <returns></returns>
    public float GetBottomWaitTimeBeforeCure()
    {
        if (CanUseBottomWaitTimeBeforeCure)
        {
            return BottomWaitTimeBeforeCure;
        }

        if (CanUseWaitTimeBeforeCure)
        {
            return WaitTimeBeforeCure;
        }

        if (CanUseBottomLightOffDelay)
        {
            return (float)Math.Max(0, Math.Round(BottomLightOffDelay - CalculateBottomLightOffDelay(), 2));
        }

        if (CanUseLightOffDelay)
        {
            return (float)Math.Max(0, Math.Round(LightOffDelay - CalculateNormalLightOffDelay(), 2));
        }

        return 0;
    }

    /// <summary>
    /// Gets the wait time before cure, if not available calculate it from light off delay
    /// </summary>
    /// <returns></returns>
    public float GetNormalWaitTimeBeforeCure()
    {
        if (CanUseWaitTimeBeforeCure)
        {
            return WaitTimeBeforeCure;
        }

        if (CanUseLightOffDelay)
        {
            return (float)Math.Max(0, Math.Round(LightOffDelay - CalculateNormalLightOffDelay(), 2));
        }

        return 0;
    }

    /// <summary>
    /// Attempt to set wait time before cure if supported, otherwise fall-back to light-off delay
    /// </summary>
    /// <param name="isBottomLayer">True to set to bottom properties, otherwise false</param>
    /// <param name="time">The time to set</param>
    /// <param name="zeroLightOffDelayCalculateBase">When true and time is zero, it will calculate light-off delay without extra time, otherwise false to set light-off delay to 0 when time is 0</param>
    public void SetWaitTimeBeforeCureOrLightOffDelay(bool isBottomLayer, float time = 0, bool zeroLightOffDelayCalculateBase = false)
    {
        if (isBottomLayer)
        {
            SetBottomWaitTimeBeforeCureOrLightOffDelay(time, zeroLightOffDelayCalculateBase);
        }
        else
        {
            SetNormalWaitTimeBeforeCureOrLightOffDelay(time, zeroLightOffDelayCalculateBase);
        }
    }

    public void SetBottomWaitTimeBeforeCureOrLightOffDelay(float time = 0, bool zeroLightOffDelayCalculateBase = false)
    {
        if (CanUseBottomWaitTimeBeforeCure)
        {
            BottomLightOffDelay = 0;
            BottomWaitTimeBeforeCure = time;
        }
        else if (CanUseBottomLightOffDelay)
        {
            if (time == 0 && !zeroLightOffDelayCalculateBase)
            {
                BottomLightOffDelay = 0;
                return;
            }

            SetBottomLightOffDelay(time);
        }
    }

    public void SetNormalWaitTimeBeforeCureOrLightOffDelay(float time = 0, bool zeroLightOffDelayCalculateBase = false)
    {
        if (CanUseWaitTimeBeforeCure)
        {
            LightOffDelay = 0;
            WaitTimeBeforeCure = time;
        }
        else if (CanUseLightOffDelay)
        {
            if (time == 0 && !zeroLightOffDelayCalculateBase)
            {
                LightOffDelay = 0;
                return;
            }

            SetNormalLightOffDelay(time);
        }
    }

    /// <summary>
    /// Rebuilds GCode based on current settings
    /// </summary>
    public virtual void RebuildGCode()
    {
        if (!SupportsGCode || _suppressRebuildGCode) return;
        GCode!.RebuildGCode(this);
        RaisePropertyChanged(nameof(GCodeStr));
    }

    /// <summary>
    /// Saves current configuration on input file
    /// </summary>
    /// <param name="progress"></param>
    public void Save(OperationProgress? progress = null)
    {
        SaveAs(null, progress);
    }

    /// <summary>
    /// Saves current configuration on a copy
    /// </summary>
    /// <param name="filePath">File path to save copy as, use null to overwrite active file (Same as <see cref="Save"/>)</param>
    /// <param name="progress"></param>
    /// <exception cref="ArgumentNullException"><see cref="FileFullPath"/></exception>
    public void SaveAs(string? filePath = null, OperationProgress? progress = null)
    {
        if (RequireFullEncode)
        {
            if (!string.IsNullOrWhiteSpace(filePath))
            {
                FileFullPath = filePath;
            }
            Encode(FileFullPath, progress);
            return;
        }

        if (string.IsNullOrWhiteSpace(FileFullPath))
        {
            if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentNullException(nameof(FileFullPath), "Not encoded yet and both source and output files are null");
            Encode(filePath, progress);
            return;
        }

        OnBeforeEncode(true);


        // Backup old file name and prepare the temporary file to be written next
        var oldFilePath = FileFullPath!;
        if(!string.IsNullOrWhiteSpace(filePath)) FileFullPath = filePath;
        var tempFile = TemporaryOutputFileFullPath;
        
        try
        {
            File.Copy(oldFilePath, tempFile, true);

            progress ??= new OperationProgress();
            PartialSaveInternally(progress);

            // Move temporary output file in place
            File.Move(tempFile, FileFullPath, true);
            OnAfterEncode(true);
        }
        catch (Exception)
        {
            // Restore backup file path and delete the temporary
            FileFullPath = oldFilePath;
            if (File.Exists(tempFile)) File.Delete(tempFile);
            throw;
        }
        
    }

    /// <summary>
    /// Partial save of the file, this is the file information only.
    /// When this function is called it's already ready to save to file
    /// </summary>
    /// <param name="progress"></param>
    protected abstract void PartialSaveInternally(OperationProgress progress);

    /// <summary>
    /// Triggers when a conversion is valid and before start converting values
    /// </summary>
    /// <param name="source">Source file format</param>
    /// <returns>True to continue the conversion, otherwise false to stop</returns>
    protected virtual bool OnBeforeConvertFrom(FileFormat source) => true;

    /// <summary>
    /// Triggers when a conversion is valid and before start converting values
    /// </summary>
    /// <param name="output">Target file format</param>
    /// <returns>True to continue the conversion, otherwise false to stop</returns>
    protected virtual bool OnBeforeConvertTo(FileFormat output) => true;

    /// <summary>
    /// Triggers when the conversion is made but before encoding
    /// </summary>
    /// <param name="source">Source file format</param>
    /// <returns>True to continue the conversion, otherwise false to stop</returns>
    protected virtual bool OnAfterConvertFrom(FileFormat source) => true;

    /// <summary>
    /// Triggers when the conversion is made but before encoding
    /// </summary>
    /// <param name="output">Output file format</param>
    /// <returns>True to continue the conversion, otherwise false to stop</returns>
    protected virtual bool OnAfterConvertTo(FileFormat output) => true;

    /// <summary>
    /// Converts this file type to another file type
    /// </summary>
    /// <param name="to">Target file format</param>
    /// <param name="fileFullPath">Output path file</param>
    /// <param name="version">File version to use</param>
    /// <param name="progress"></param>
    /// <returns>The converted file if successful, otherwise null</returns>
    public virtual FileFormat? Convert(Type to, string fileFullPath, uint version = 0, OperationProgress? progress = null)
    {
        if (!IsValid) return null;
        var found = AvailableFormats.Any(format => to == format.GetType());
        if (!found) return null;

        progress ??= new OperationProgress("Converting");

        if (Activator.CreateInstance(to) is not FileFormat slicerFile) return null;
        slicerFile.FileFullPath = fileFullPath;

        if (!slicerFile.OnBeforeConvertFrom(this)) return null;
        if (!OnBeforeConvertTo(slicerFile)) return null;

        if (version > 0 && version != DefaultVersion)
        {
            slicerFile.Version = version;
        }

        slicerFile.SuppressRebuildPropertiesWork(() =>
        {
            slicerFile.Init(CloneLayers());
            slicerFile.AntiAliasing = ValidateAntiAliasingLevel();
            slicerFile.LayerCount = LayerCount;
            slicerFile.BottomLayerCount = BottomLayerCount;
            slicerFile.TransitionLayerCount = TransitionLayerCount;
            slicerFile.LayerHeight = LayerHeight;
            slicerFile.ResolutionX = ResolutionX;
            slicerFile.ResolutionY = ResolutionY;
            slicerFile.DisplayWidth = DisplayWidth;
            slicerFile.DisplayHeight = DisplayHeight;
            slicerFile.MachineZ = MachineZ;
            slicerFile.DisplayMirror = DisplayMirror;

            // Exposure
            slicerFile.BottomExposureTime = BottomExposureTime;
            slicerFile.ExposureTime = ExposureTime;

            // Lifts
            slicerFile.BottomLiftHeight = BottomLiftHeight;
            slicerFile.BottomLiftSpeed = BottomLiftSpeed;
                
            slicerFile.LiftHeight = LiftHeight;
            slicerFile.LiftSpeed = LiftSpeed;

            slicerFile.BottomLiftSpeed2 = BottomLiftSpeed2;
            slicerFile.LiftSpeed2 = LiftSpeed2;

            slicerFile.BottomRetractSpeed = BottomRetractSpeed;
            slicerFile.RetractSpeed = RetractSpeed;

            slicerFile.BottomRetractSpeed2 = BottomRetractSpeed2;
            slicerFile.RetractSpeed2 = RetractSpeed2;


            if (slicerFile.CanUseAnyLiftHeight2 && (CanUseAnyLiftHeight2 || GetType() == typeof(SL1File))) // Both are TSMC compatible
            {
                slicerFile.BottomLiftHeight2 = BottomLiftHeight2;
                slicerFile.LiftHeight2 = LiftHeight2;

                slicerFile.BottomRetractHeight2 = BottomRetractHeight2;
                slicerFile.RetractHeight2 = RetractHeight2;
            }
            /*else if (slicerFile.CanUseAnyLiftHeight2) // Output format is compatible with TSMC, but input isn't
            {
                slicerFile.BottomLiftHeight = BottomLiftHeight;
                slicerFile.LiftHeight = LiftHeight;
            }*/
            else if (CanUseAnyLiftHeight2) // Output format isn't compatible with TSMC, but input is
            {
                slicerFile.BottomLiftHeight = BottomLiftHeightTotal;
                slicerFile.LiftHeight = LiftHeightTotal;

                // Set to the slowest retract speed
                if (BottomRetractSpeed2 > 0 && BottomRetractSpeed > BottomRetractSpeed2)
                {
                    slicerFile.BottomRetractSpeed = BottomRetractSpeed2;
                }

                // Set to the slowest retract speed
                if (RetractSpeed2 > 0 && RetractSpeed > RetractSpeed2)
                {
                    slicerFile.RetractSpeed = RetractSpeed2;
                }
            }

            // Wait times
            slicerFile.BottomLightOffDelay = BottomLightOffDelay;
            slicerFile.LightOffDelay = LightOffDelay;

            slicerFile.BottomWaitTimeBeforeCure = BottomWaitTimeBeforeCure;
            slicerFile.WaitTimeBeforeCure = WaitTimeBeforeCure;

            slicerFile.BottomWaitTimeAfterCure = BottomWaitTimeAfterCure;
            slicerFile.WaitTimeAfterCure = WaitTimeAfterCure;

            slicerFile.BottomWaitTimeAfterLift = BottomWaitTimeAfterLift;
            slicerFile.WaitTimeAfterLift = WaitTimeAfterLift;

            slicerFile.BottomLightPWM = BottomLightPWM;
            slicerFile.LightPWM = LightPWM;


            slicerFile.MachineName = MachineName;
            slicerFile.MaterialName = MaterialName;
            slicerFile.MaterialMilliliters = MaterialMilliliters;
            slicerFile.MaterialGrams = MaterialGrams;
            slicerFile.MaterialCost = MaterialCost;
            slicerFile.Xppmm = Xppmm;
            slicerFile.Yppmm = Yppmm;
            slicerFile.PrintTime = PrintTime;
            slicerFile.PrintHeight = PrintHeight;

            slicerFile.SetThumbnails(Thumbnails);
        });

        if (!slicerFile.OnAfterConvertFrom(this)) return null;
        if (!OnAfterConvertTo(slicerFile)) return null;

        slicerFile.Encode(fileFullPath, progress);

        return slicerFile;
    }

    /// <summary>
    /// Converts this file type to another file type
    /// </summary>
    /// <param name="to">Target file format</param>
    /// <param name="fileFullPath">Output path file</param>
    /// <param name="version">File version</param>
    /// <param name="progress"></param>
    /// <returns>TThe converted file if successful, otherwise null</returns>
    public FileFormat? Convert(FileFormat to, string fileFullPath, uint version = 0, OperationProgress? progress = null)
        => Convert(to.GetType(), fileFullPath, version, progress);

    /// <summary>
    /// Changes the compression method of all layers to a new method
    /// </summary>
    /// <param name="newCodec">The new method to change to</param>
    /// <param name="progress"></param>
    public void ChangeLayersCompressionMethod(LayerCompressionCodec newCodec, OperationProgress? progress = null)
    {
        progress ??= new OperationProgress($"Changing layers compression codec to {newCodec}");
        progress.Reset("Layers", LayerCount);

        Parallel.ForEach(this, CoreSettings.GetParallelOptions(progress), layer =>
        {
            layer.CompressionCodec = newCodec;
            progress.LockAndIncrement();
        });
    }

    /// <summary>
    /// Validate AntiAlias Level
    /// </summary>
    public byte ValidateAntiAliasingLevel()
    {
        if (AntiAliasing <= 1) return 1;
        //if(AntiAliasing % 2 != 0) throw new ArgumentException("AntiAliasing must be multiples of 2, otherwise use 0 or 1 to disable it", nameof(AntiAliasing));
        return AntiAliasing;
    }

    /// <summary>
    /// SuppressRebuildProperties = true, call the invoker and reset SuppressRebuildProperties = false
    /// </summary>
    /// <param name="action">Action work</param>
    /// <param name="callRebuildOnEnd">True to force rebuild the layer properties after the work and before reset to false</param>
    /// <param name="recalculateZPos">True to recalculate z position of each layer (requires <paramref name="callRebuildOnEnd"/> = true), otherwise false</param>
    /// <param name="property">Property name to change for each layer, use null to update all properties (requires <paramref name="callRebuildOnEnd"/> = true)</param>
    public void SuppressRebuildPropertiesWork(Action action, bool callRebuildOnEnd = false, bool recalculateZPos = true, string? property = null)
    {
        /*SuppressRebuildProperties = true;
        action.Invoke();
        if(callRebuildOnEnd) LayerManager.RebuildLayersProperties(recalculateZPos, property);
        SuppressRebuildProperties = false;*/
        SuppressRebuildPropertiesWork(() =>
        {
            action.Invoke();
            return true;
        }, callRebuildOnEnd, recalculateZPos, property);
    }

    /// <summary>
    /// SuppressRebuildProperties = true, call the invoker and reset SuppressRebuildProperties = false
    /// </summary>
    /// <param name="action">Action work</param>
    /// <param name="callRebuildOnEnd">True to force rebuild the layer properties after the work and before reset to false</param>
    /// <param name="recalculateZPos">True to recalculate z position of each layer (requires <paramref name="callRebuildOnEnd"/> = true), otherwise false</param>
    /// <param name="property">Property name to change for each layer, use null to update all properties (requires <paramref name="callRebuildOnEnd"/> = true)</param>
    public bool SuppressRebuildPropertiesWork(Func<bool> action, bool callRebuildOnEnd = false, bool recalculateZPos = true, string? property = null)
    {
        bool result;
        try
        {
            SuppressRebuildProperties = true;
            result = action.Invoke();
            if (callRebuildOnEnd && result) RebuildLayersProperties(recalculateZPos, property);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            throw;
        }
        finally
        {
            SuppressRebuildProperties = false;
        }
            
        return result;
    }

    public void UpdateGlobalPropertiesFromLayers()
    {
        if (LayerCount == 0) return;

        SuppressRebuildPropertiesWork(() =>
        {
            var bottomLayer = FirstLayer;
            if (bottomLayer is not null)
            {
                if (bottomLayer.LightOffDelay > 0) BottomLightOffDelay = bottomLayer.LightOffDelay;
                if (bottomLayer.WaitTimeBeforeCure > 0) BottomWaitTimeBeforeCure = bottomLayer.WaitTimeBeforeCure;
                if (bottomLayer.ExposureTime > 0) BottomExposureTime = bottomLayer.ExposureTime;
                if (bottomLayer.WaitTimeAfterCure > 0) BottomWaitTimeAfterCure = bottomLayer.WaitTimeAfterCure;
                if (bottomLayer.LiftHeight > 0) BottomLiftHeight = bottomLayer.LiftHeight;
                if (bottomLayer.LiftSpeed > 0) BottomLiftSpeed = bottomLayer.LiftSpeed;
                if (bottomLayer.LiftHeight2 > 0) BottomLiftHeight2 = bottomLayer.LiftHeight2;
                if (bottomLayer.LiftSpeed2 > 0) BottomLiftSpeed2 = bottomLayer.LiftSpeed2;
                if (bottomLayer.WaitTimeAfterLift > 0) BottomWaitTimeAfterLift = bottomLayer.WaitTimeAfterLift;
                if (bottomLayer.RetractSpeed > 0) BottomRetractSpeed = bottomLayer.RetractSpeed;
                if (bottomLayer.RetractHeight2 > 0) BottomRetractHeight2 = bottomLayer.RetractHeight2;
                if (bottomLayer.RetractSpeed2 > 0) BottomRetractSpeed2 = bottomLayer.RetractSpeed2;
                if (bottomLayer.LightPWM > 0) BottomLightPWM = bottomLayer.LightPWM;
            }

            var normalLayer = LastLayer;
            if (normalLayer is not null)
            {
                if (normalLayer.LightOffDelay > 0) LightOffDelay = normalLayer.LightOffDelay;
                if (normalLayer.WaitTimeBeforeCure > 0) WaitTimeBeforeCure = normalLayer.WaitTimeBeforeCure;
                if (normalLayer.ExposureTime > 0) ExposureTime = normalLayer.ExposureTime;
                if (normalLayer.WaitTimeAfterCure > 0) WaitTimeAfterCure = normalLayer.WaitTimeAfterCure;
                if (normalLayer.LiftHeight > 0) LiftHeight = normalLayer.LiftHeight;
                if (normalLayer.LiftSpeed > 0) LiftSpeed = normalLayer.LiftSpeed;
                if (normalLayer.LiftHeight2 > 0) LiftHeight2 = normalLayer.LiftHeight2;
                if (normalLayer.LiftSpeed2 > 0) LiftSpeed2 = normalLayer.LiftSpeed2;
                if (normalLayer.WaitTimeAfterLift > 0) WaitTimeAfterLift = normalLayer.WaitTimeAfterLift;
                if (normalLayer.RetractSpeed > 0) RetractSpeed = normalLayer.RetractSpeed;
                if (normalLayer.RetractHeight2 > 0) RetractHeight2 = normalLayer.RetractHeight2;
                if (normalLayer.RetractSpeed2 > 0) RetractSpeed2 = normalLayer.RetractSpeed2;
                if (normalLayer.LightPWM > 0) LightPWM = normalLayer.LightPWM;
            }
        });
    }

    public void UpdatePrintTime()
    {
        PrintTime = PrintTimeComputed;
        //Debug.WriteLine($"Time updated: {_printTime}s");
    }

    public void UpdatePrintTimeQueued()
    {
        lock (Mutex)
        {
            _queueTimerPrintTime.Stop();
            _queueTimerPrintTime.Start();
        }
    }

    /// <summary>
    /// Converts millimeters to pixels given the current resolution and display size
    /// </summary>
    /// <param name="millimeters">Millimeters to convert</param>
    /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
    /// <returns>Pixels</returns>
    public uint MillimetersXToPixels(float millimeters, uint fallbackToPixels = 0)
    {
        var ppmm = Xppmm;
        if (ppmm <= 0) return fallbackToPixels;
        return (uint)(ppmm * millimeters);
    }

    /// <summary>
    /// Converts millimeters to pixels given the current resolution and display size
    /// </summary>
    /// <param name="millimeters">Millimeters to convert</param>
    /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
    /// <returns>Pixels</returns>
    public uint MillimetersYToPixels(float millimeters, uint fallbackToPixels = 0)
    {
        var ppmm = Yppmm;
        if (ppmm <= 0) return fallbackToPixels;
        return (uint)(ppmm * millimeters);
    }

    /// <summary>
    /// Converts millimeters to pixels given the current resolution and display size
    /// </summary>
    /// <param name="millimeters">Millimeters to convert</param>
    /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
    /// <returns>Pixels</returns>
    public uint MillimetersToPixels(float millimeters, uint fallbackToPixels = 0)
    {
        var ppmm = PpmmMax;
        if (ppmm <= 0) return fallbackToPixels;
        return (uint)(ppmm * millimeters);
    }

    /// <summary>
    /// Converts millimeters to pixels given the current resolution and display size
    /// </summary>
    /// <param name="millimeters">Millimeters to convert</param>
    /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
    /// <returns>Pixels</returns>
    public float MillimetersToPixelsF(float millimeters, uint fallbackToPixels = 0)
    {
        var ppmm = PpmmMax;
        if (ppmm <= 0) return fallbackToPixels;
        return ppmm * millimeters;
    }

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="x">X position in pixels</param>
    /// <param name="precision">Decimal precision</param>
    /// <returns>Display position in millimeters</returns>
    public float PixelToDisplayPositionX(int x, byte precision = 3) => (float)Math.Round(PixelWidth * x, precision);

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="y">Y position in pixels</param>
    /// <param name="precision">Decimal precision</param>
    /// <returns>Display position in millimeters</returns>
    public float PixelToDisplayPositionY(int y, byte precision = 3) => (float)Math.Round(PixelHeight * y, precision);

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="x">X position in pixels</param>
    /// <param name="y">Y position in pixels</param>
    /// <param name="precision">Decimal precision</param>
    /// <returns>Resolution position in pixels</returns>
    public PointF PixelToDisplayPosition(int x, int y, byte precision = 3) =>new(PixelToDisplayPositionX(x, precision), PixelToDisplayPositionY(y, precision));
    public PointF PixelToDisplayPosition(Point point, byte precision = 3) => new(PixelToDisplayPositionX(point.X, precision), PixelToDisplayPositionY(point.Y, precision));

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="x">X position in millimeters</param>
    /// <returns>Resolution position in pixels</returns>
    public int DisplayToPixelPositionX(float x) => (int)(x * Xppmm);

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="y">Y position in millimeters</param>
    /// <returns>Resolution position in pixels</returns>
    public int DisplayToPixelPositionY(float y) => (int)(y * Yppmm);

    /// <summary>
    /// From a pixel position get the equivalent position on the display
    /// </summary>
    /// <param name="x">X position in millimeters</param>
    /// <param name="y">Y position in millimeters</param>
    /// <returns>Resolution position in pixels</returns>
    public Point DisplayToPixelPosition(float x, float y) => new(DisplayToPixelPositionX(x), DisplayToPixelPositionY(y));
    public Point DisplayToPixelPosition(PointF point) => new(DisplayToPixelPositionX(point.X), DisplayToPixelPositionY(point.Y));

    public bool SanitizeBoundingRectangle(ref Rectangle rectangle)
    {
        var oldRectangle = rectangle;
        rectangle = Rectangle.Intersect(rectangle, ResolutionRectangle);
        return oldRectangle != rectangle;
    }

    public Rectangle GetBoundingRectangle(OperationProgress? progress = null)
    {
        var firstLayer = FirstLayer;
        if (!_boundingRectangle.IsEmpty || LayerCount == 0 || firstLayer is null || !firstLayer.HaveImage) return _boundingRectangle;
        progress ??= new OperationProgress(OperationProgress.StatusOptimizingBounds, LayerCount - 1);
        _boundingRectangle = Rectangle.Empty;
        uint firstValidLayerBounds = 0;

        void FindFirstBoundingRectangle()
        {
            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                firstValidLayerBounds = layerIndex;
                if (this[layerIndex] is null || this[layerIndex].BoundingRectangle == Rectangle.Empty) continue;
                _boundingRectangle = this[layerIndex].BoundingRectangle;
                break;
            }
        }

        FindFirstBoundingRectangle();
        //_boundingRectangle = firstLayer.BoundingRectangle;

        if (_boundingRectangle.IsEmpty) // Safe checking, all layers haven't a bounding rectangle
        {
            progress.Reset(OperationProgress.StatusOptimizingBounds, LayerCount - 1);
            Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                this[layerIndex].GetBoundingRectangle();
                progress.LockAndIncrement();
            });

            FindFirstBoundingRectangle();
        }

        if (firstValidLayerBounds + 1 < LayerCount)
        {
            progress.Reset(OperationProgress.StatusCalculatingBounds, LayerCount - firstValidLayerBounds - 1);
            for (var i = firstValidLayerBounds + 1; i < LayerCount; i++)
            {
                if (this[i] is null || this[i].BoundingRectangle.IsEmpty) continue;
                _boundingRectangle = Rectangle.Union(_boundingRectangle, this[i].BoundingRectangle);
                progress++;
            }
        }

        RaisePropertyChanged(nameof(BoundingRectangle));
        return _boundingRectangle;
    }

    public Rectangle GetBoundingRectangle(int marginX, int marginY, OperationProgress? progress = null)
    {
        var rect = GetBoundingRectangle(progress);
        if (marginX == 0 && marginY == 0) return rect;
        rect.Inflate(marginX / 2, marginY / 2);
        SanitizeBoundingRectangle(ref rect);
        return rect;
    }

    public Rectangle GetBoundingRectangle(int margin, OperationProgress? progress = null) => GetBoundingRectangle(margin, margin, progress);
    public Rectangle GetBoundingRectangle(Size margin, OperationProgress? progress = null) => GetBoundingRectangle(margin.Width, margin.Height, progress);


    /// <summary>
    /// Creates a empty mat of file <see cref="Resolution"/> size and create a dummy pixel to prevent a empty layer detection
    /// </summary>
    /// <param name="dummyPixelLocation">Location to set the dummy pixel, use a negative value (-1,-1) to set to the bounding center</param>
    /// <param name="dummyPixelBrightness">Dummy pixel brightness</param>
    /// <returns></returns>
    public Mat CreateMatWithDummyPixel(Point dummyPixelLocation, byte dummyPixelBrightness)
    {
        var newMat = EmguExtensions.InitMat(Resolution);
        if (dummyPixelBrightness > 0)
        {
            if (dummyPixelLocation.IsAnyNegative()) dummyPixelLocation = BoundingRectangle.Center();
            newMat.SetByte(newMat.GetPixelPos(dummyPixelLocation), dummyPixelBrightness);
        }

        return newMat;
    }

    /// <summary>
    /// Creates a empty mat of file <see cref="Resolution"/> size and create a dummy pixel to prevent a empty layer detection
    /// </summary>
    /// <param name="dummyPixelLocation">Location to set the dummy pixel, use a negative value (-1,-1) to set to the bounding center</param>
    /// <returns></returns>
    public Mat CreateMatWithDummyPixel(Point dummyPixelLocation) => CreateMatWithDummyPixel(dummyPixelLocation, SupportsGCode ? (byte) 1 : (byte) 128);

    /// <summary>
    /// Creates a empty mat of file <see cref="Resolution"/> size
    /// </summary>
    /// <param name="dummyPixelBrightness">Dummy pixel brightness</param>
    /// <returns></returns>
    public Mat CreateMatWithDummyPixel(byte dummyPixelBrightness) => CreateMatWithDummyPixel(BoundingRectangle.Center(), dummyPixelBrightness);

    /// <summary>
    /// Creates a empty mat of file <see cref="Resolution"/> size
    /// </summary>
    /// <returns></returns>
    public Mat CreateMatWithDummyPixel() => CreateMatWithDummyPixel(SupportsGCode ? (byte)1 : (byte)128);
      
    /// <summary>
    /// Creates a empty mat of file <see cref="Resolution"/> size
    /// </summary>
    /// <param name="initMat">True to black out the mat</param>
    /// <returns></returns>
    public Mat CreateMat(bool initMat = true)
    {
        return initMat
            ? EmguExtensions.InitMat(Resolution)
            : new Mat(Resolution, DepthType.Cv8U, 1);
    }

    #endregion

    #region Layer collection methods
    public void Init(Layer[] layers)
    {
        var oldLayerCount = LayerCount;
        _layers = layers;
        if (LayerCount != oldLayerCount)
        {
            LayerCount = LayerCount;
        }

        SanitizeLayers();
    }

    public void Init(uint layerCount, bool initializeLayers = false)
    {
        var oldLayerCount = LayerCount;
        _layers = new Layer[layerCount];
        if (initializeLayers)
        {
            for (uint layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                _layers[layerIndex] = new Layer(layerIndex, this);
            }
        }

        if (LayerCount != oldLayerCount)
        {
            LayerCount = LayerCount;
        }
    }

    public void Add(Layer layer)
    {
        Layers = _layers.Append(layer).ToArray();
    }

    public void Add(IEnumerable<Layer> layers)
    {
        var list = _layers.ToList();
        list.AddRange(layers);
        Layers = list.ToArray();
    }

    public bool Contains(Layer layer)
    {
        return _layers.Contains(layer);
    }

    public void CopyTo(Layer[] array, int arrayIndex)
    {
        _layers.CopyTo(array, arrayIndex);
    }

    public bool Remove(Layer layer)
    {
        var list = _layers.ToList();
        var result = list.Remove(layer);
        if (result)
        {
            Layers = list.ToArray();
        }

        return result;
    }

    public int IndexOf(Layer layer)
    {
        for (int layerIndex = 0; layerIndex < Count; layerIndex++)
        {
            if (_layers[layerIndex].Equals(layer)) return layerIndex;
        }

        return -1;
    }

    public void Prepend(Layer layer) => Insert(0, layer);
    public void Prepend(IEnumerable<Layer> layers) => InsertRange(0, layers);
    public void Append(Layer layer) => Add(layer);
    public void AppendRange(IEnumerable<Layer> layers) => Add(layers);

    public void Insert(int index, Layer layer)
    {
        if (index < 0) return;
        if (index > Count)
        {
            Add(layer); // Append
            return;
        }

        var list = _layers.ToList();
        list.Insert(index, layer);
        Layers = list.ToArray();
    }

    public void InsertRange(int index, IEnumerable<Layer> layers)
    {
        if (index < 0) return;

        if (index > Count)
        {
            Add(layers);
            return;
        }

        var list = _layers.ToList();
        list.InsertRange(index, layers);
        Layers = list.ToArray();
    }

    public void RemoveAt(int index)
    {
        if (index >= LastLayerIndex) return;
        var list = _layers.ToList();
        list.RemoveAt(index);
        Layers = list.ToArray();
    }

    public void RemoveRange(int index, int count)
    {
        if (count <= 0 || index >= LastLayerIndex) return;
        var list = _layers.ToList();
        list.RemoveRange(index, count);
        Layers = list.ToArray();
    }

    /// <summary>
    /// Clone layers
    /// </summary>
    /// <returns></returns>
    public Layer[] CloneLayers()
    {
        return Layer.CloneLayers(_layers);
    }

    /*
    /// <summary>
    /// Removes all null layers in the collection
    /// </summary>
    public void RemoveNullLayers()
    {
        var oldCount = LayerCount;
        var layers = this.Where(layer => layer is not null).ToArray();
        if (layers.Length == oldCount) return;
        Layers = layers;
    }*/

    /// <summary>
    /// Reallocate with new size
    /// </summary>
    /// <returns></returns>
    public Layer[] ReallocateNew(uint newLayerCount, bool makeClone = false)
    {
        var layers = new Layer[newLayerCount];
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            if (layerIndex >= newLayerCount) break;
            var layer = this[layerIndex];
            layers[layerIndex] = makeClone ? layer.Clone() : layer;
        }

        return layers;
    }

    /// <summary>
    /// Reallocate layer count with a new size
    /// </summary>
    /// <param name="newLayerCount">New layer count</param>
    /// <param name="initBlack"></param>
    public void Reallocate(uint newLayerCount, bool initBlack = false)
    {
        var oldLayerCount = LayerCount;
        int differenceLayerCount = (int)newLayerCount - Count;
        if (differenceLayerCount == 0) return;
        var newLayers = new Layer[newLayerCount];

        Array.Copy(_layers, 0, newLayers, 0, Math.Min(newLayerCount, newLayers.Length));

        if (differenceLayerCount > 0 && initBlack)
        {
            using var blackMat = CreateMat(false);
            var pngBytes = blackMat.GetPngByes();
            for (var layerIndex = oldLayerCount; layerIndex < newLayerCount; layerIndex++)
            {
                newLayers[layerIndex] = new Layer(layerIndex, pngBytes.ToArray(), this);
            }
        }

        SuppressRebuildPropertiesWork(() =>
        {
            Layers = newLayers;
        });
    }

    /// <summary>
    /// Reallocate at given index
    /// </summary>
    /// <returns></returns>
    public void ReallocateInsert(uint insertAtLayerIndex, uint layerCount, bool initBlack = false)
    {
        if (layerCount == 0) return;
        insertAtLayerIndex = Math.Min(insertAtLayerIndex, LayerCount);
        var newLayers = new Layer[LayerCount + layerCount];

        // Copy from start to insert index
        if (insertAtLayerIndex > 0)
            Array.Copy(_layers, 0, newLayers, 0, insertAtLayerIndex);

        // Rearrange from last insert to end
        if (insertAtLayerIndex < LayerCount)
            Array.Copy(
                _layers, insertAtLayerIndex,
                newLayers, insertAtLayerIndex + layerCount,
                LayerCount - insertAtLayerIndex);
        /*for (uint layerIndex = insertAtLayerIndex; layerIndex < _layers.Length; layerIndex++)
        {
            newLayers[layerCount + layerIndex] = _layers[layerIndex];
            newLayers[layerCount + layerIndex].Index = layerCount + layerIndex;
        }*/

        // Allocate new layers in between
        if (initBlack)
        {
            using var blackMat = EmguExtensions.InitMat(Resolution);
            var pngBytes = blackMat.GetPngByes();
            for (var layerIndex = insertAtLayerIndex; layerIndex < insertAtLayerIndex + layerCount; layerIndex++)
            {
                newLayers[layerIndex] = new Layer(layerIndex, pngBytes.ToArray(), this);
            }
        }

        SuppressRebuildPropertiesWork(() =>
        {
            Layers = newLayers;
        });
    }

    /// <summary>
    /// Reallocate at a kept range
    /// </summary>
    /// <param name="startLayerIndex"></param>
    /// <param name="endLayerIndex"></param>
    public void ReallocateKeepRange(uint startLayerIndex, uint endLayerIndex)
    {
        if ((int)(endLayerIndex - startLayerIndex) < 0) return;
        var newLayers = new Layer[1 + endLayerIndex - startLayerIndex];

        Array.Copy(_layers, startLayerIndex, newLayers, 0, newLayers.Length);
        /*uint currentLayerIndex = 0;
        for (uint layerIndex = startLayerIndex; layerIndex <= endLayerIndex; layerIndex++)
        {
            newLayers[currentLayerIndex++] = _layers[layerIndex];
        }*/

        SuppressRebuildPropertiesWork(() =>
        {
            Layers = newLayers;
        });
    }

    /// <summary>
    /// Reallocate at start
    /// </summary>
    /// <returns></returns>
    public void ReallocateStart(uint layerCount, bool initBlack = false) => ReallocateInsert(0, layerCount, initBlack);

    /// <summary>
    /// Reallocate at end
    /// </summary>
    /// <returns></returns>
    public void ReallocateEnd(uint layerCount, bool initBlack = false) => ReallocateInsert(LayerCount, layerCount, initBlack);

    /// <summary>
    /// Allocate layers from a Mat array
    /// </summary>
    /// <param name="mats"></param>
    /// <param name="progress"></param>
    /// <returns>The new Layer array</returns>
    public Layer[] AllocateFromMat(Mat[] mats, OperationProgress? progress = null)
    {
        progress ??= new OperationProgress();
        var layers = new Layer[mats.Length];
        Parallel.For(0, mats.Length, CoreSettings.GetParallelOptions(progress), i =>
        {
            layers[i] = new Layer((uint)i, mats[i], this);
        });

        return layers;
    }

    /// <summary>
    /// Allocate layers from a Mat array and set them to the current file
    /// </summary>
    /// <param name="mats"></param>
    /// <param name="progress"></param>
    /// /// <returns>The new Layer array</returns>
    public Layer[] AllocateAndSetFromMat(Mat[] mats, OperationProgress? progress = null)
    {
        var layers = AllocateFromMat(mats, progress);
        Layers = layers;
        return layers;
    }

    /// <summary>
    /// Checks if a layer index exists in the collection
    /// </summary>
    /// <param name="layerIndex">Layer index to check</param>
    /// <returns></returns>
    public bool LayerExists(int layerIndex)
    {
        return layerIndex >= 0 && layerIndex < LayerCount;
    }

    /// <summary>
    /// Checks if a layer index exists in the collection
    /// </summary>
    /// <param name="layerIndex">Layer index to check</param>
    /// <returns></returns>
    public bool LayerExists(uint layerIndex)
    {
        return layerIndex < LayerCount;
    }
    #endregion

    #region Layer methods

    /// <summary>
    /// Try to parse starting and ending layer index from a string
    /// </summary>
    /// <param name="value">String value to parse, in start:end format</param>
    /// <param name="layerIndexStart">Parsed starting layer index</param>
    /// <param name="layerIndexEnd">Parsed ending layer index</param>
    /// <returns></returns>
    public bool TryParseLayerIndexRange(string value, out uint layerIndexStart, out uint layerIndexEnd)
    {
        layerIndexStart = 0;
        layerIndexEnd = LastLayerIndex;

        if (string.IsNullOrWhiteSpace(value)) return false;

        var split = value.Split(new[]{':', '|', '-'}, StringSplitOptions.TrimEntries);

        if (split[0] != string.Empty)
        {
            if(split[0].Equals("FIRST", StringComparison.OrdinalIgnoreCase)) layerIndexStart = 0;
            else if(split[0].Equals("LB", StringComparison.OrdinalIgnoreCase)) layerIndexStart = LastBottomLayer?.Index ?? 0;
            else if(split[0].Equals("FN", StringComparison.OrdinalIgnoreCase)) layerIndexStart = FirstNormalLayer?.Index ?? 0;
            else if(split[0].Equals("LAST", StringComparison.OrdinalIgnoreCase)) layerIndexStart = LastLayerIndex;
            else if(!uint.TryParse(split[0], out layerIndexStart)) return false;
            SanitizeLayerIndex(ref layerIndexStart);
        }

        if (split.Length == 1)
        {
            layerIndexEnd = layerIndexStart;
            return true;
        }

        if (split[1] != string.Empty)
        {
            if (split[1].Equals("FIRST", StringComparison.OrdinalIgnoreCase)) layerIndexEnd = 0;
            else if (split[1].Equals("LB", StringComparison.OrdinalIgnoreCase)) layerIndexEnd = LastBottomLayer?.Index ?? 0;
            else if (split[1].Equals("FN", StringComparison.OrdinalIgnoreCase)) layerIndexEnd = FirstNormalLayer?.Index ?? 0;
            else if (split[1].Equals("LAST", StringComparison.OrdinalIgnoreCase)) layerIndexEnd = LastLayerIndex;
            else if (!uint.TryParse(split[1], out layerIndexEnd)) return false;
            SanitizeLayerIndex(ref layerIndexEnd);
        }

        return layerIndexStart <= layerIndexEnd;
    }

    /// <summary>
    /// Constrains a layer index to be inside the range between 0 and <see cref="LastLayerIndex"/>
    /// </summary>
    /// <param name="layerIndex">Layer index to sanitize</param>
    /// <returns>True if sanitized, otherwise false</returns>
    public bool SanitizeLayerIndex(ref int layerIndex)
    {
        var originalValue = layerIndex;
        layerIndex = Math.Clamp(layerIndex, 0, (int)LastLayerIndex);
        return originalValue != layerIndex;
    }

    /// <summary>
    /// Constrains a layer index to be inside the range between 0 and <see cref="LastLayerIndex"/>
    /// </summary>
    /// <param name="layerIndex">Layer index to sanitize</param>
    /// <returns>True if sanitized, otherwise false</returns>
    public bool SanitizeLayerIndex(ref uint layerIndex)
    {
        var originalValue = layerIndex;
        layerIndex = Math.Min(layerIndex, LastLayerIndex);
        return originalValue != layerIndex;
    }

    /// <summary>
    /// Constrains a layer index to be inside the range between 0 and <see cref="LastLayerIndex"/>
    /// </summary>
    /// <param name="layerIndex">Layer index to sanitize</param>
    public uint SanitizeLayerIndex(int layerIndex)
    {
        return (uint)Math.Clamp(layerIndex, 0, LastLayerIndex);
    }

    /// <summary>
    /// Constrains a layer index to be inside the range between 0 and <see cref="LastLayerIndex"/>
    /// </summary>
    /// <param name="layerIndex">Layer index to sanitize</param>
    public uint SanitizeLayerIndex(uint layerIndex)
    {
        return Math.Min(layerIndex, LastLayerIndex);
    }


    /// <summary>
    /// Re-assign layer indexes and parent <see cref="FileFormat"/>
    /// </summary>
    public void SanitizeLayers()
    {
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            if(this[layerIndex] is null) continue;
            this[layerIndex].Index = layerIndex;
            this[layerIndex].SlicerFile = this;
        }
    }

    /// <summary>
    /// Sanitize file and thrown exception if a severe problem is found
    /// </summary>
    /// <returns>True if one or more corrections has been applied, otherwise false</returns>
    public bool Sanitize()
    {
        bool appliedCorrections = false;

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            // Check for null layers
            if (this[layerIndex] is null) throw new InvalidDataException($"Layer {layerIndex} was defined but doesn't contain a valid image.");
            if (layerIndex <= 0) continue;
            // Check for bigger position z than it successor
            if (this[layerIndex - 1].PositionZ > this[layerIndex].PositionZ && this[layerIndex - 1].NonZeroPixelCount > 1)
                throw new InvalidDataException($"Layer {layerIndex - 1} ({this[layerIndex - 1].PositionZ}mm) have a higher Z position than the successor layer {layerIndex} ({this[layerIndex].PositionZ}mm).\n");
        }

        if ((ResolutionX == 0 || ResolutionY == 0) && DecodeType == FileDecodeType.Full)
        {
            var layer = FirstLayer;
            if (layer is not null)
            {
                using var mat = layer.LayerMat;

                if (mat.Size.HaveZero())
                {
                    throw new FileLoadException($"File resolution ({Resolution}) is invalid and can't be auto fixed due invalid layers with same problem ({mat.Size}).", FileFullPath);
                }

                Resolution = mat.Size;
                appliedCorrections = true;
            }
        }

        // Fix 0mm positions at layer 0
        if (this[0].PositionZ == 0)
        {
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                this[layerIndex].PositionZ = Layer.RoundHeight(this[layerIndex].PositionZ + LayerHeight);
            }

            appliedCorrections = true;
        }

        // Fix LightPWM of 0
        if (LightPWM == 0)
        {
            LightPWM = DefaultLightPWM;
            appliedCorrections = true;
        }
        if (BottomLightPWM == 0)
        {
            BottomLightPWM = DefaultBottomLightPWM;
            appliedCorrections = true;
        }

        return appliedCorrections;
    }

    /// <summary>
    /// Rebuild layer properties based on slice settings
    /// </summary>
    public void RebuildLayersProperties(bool recalculateZPos = true, string? property = null)
    {
        //var layerHeight = SlicerFile.LayerHeight;
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var layer = this[layerIndex];
            layer.Index = layerIndex;
            layer.SlicerFile = this;

            if (recalculateZPos)
            {
                layer.PositionZ = GetHeightFromLayer(layerIndex);
            }

            if (property != string.Empty)
            {
                if (property is null or nameof(BottomLayerCount))
                {
                    layer.LightOffDelay = GetBottomOrNormalValue(layer, BottomLightOffDelay, LightOffDelay);
                    layer.WaitTimeBeforeCure = GetBottomOrNormalValue(layer, BottomWaitTimeBeforeCure, WaitTimeBeforeCure);
                    layer.ExposureTime = GetBottomOrNormalValue(layer, BottomExposureTime, ExposureTime);
                    layer.WaitTimeAfterCure = GetBottomOrNormalValue(layer, BottomWaitTimeAfterCure, WaitTimeAfterCure);
                    layer.LiftHeight = GetBottomOrNormalValue(layer, BottomLiftHeight, LiftHeight);
                    layer.LiftSpeed = GetBottomOrNormalValue(layer, BottomLiftSpeed, LiftSpeed);
                    layer.LiftHeight2 = GetBottomOrNormalValue(layer, BottomLiftHeight2, LiftHeight2);
                    layer.LiftSpeed2 = GetBottomOrNormalValue(layer, BottomLiftSpeed2, LiftSpeed2);
                    layer.WaitTimeAfterLift = GetBottomOrNormalValue(layer, BottomWaitTimeAfterLift, WaitTimeAfterLift);
                    layer.RetractSpeed = GetBottomOrNormalValue(layer, BottomRetractSpeed, RetractSpeed);
                    layer.RetractHeight2 = GetBottomOrNormalValue(layer, BottomRetractHeight2, RetractHeight2);
                    layer.RetractSpeed2 = GetBottomOrNormalValue(layer, BottomRetractSpeed2, RetractSpeed2);
                    layer.LightPWM = GetBottomOrNormalValue(layer, BottomLightPWM, LightPWM);
                }
                else
                {
                    if (layer.IsBottomLayer)
                    {
                        if (property == nameof(BottomLightOffDelay)) layer.LightOffDelay = BottomLightOffDelay;
                        else if (property == nameof(BottomWaitTimeBeforeCure)) layer.WaitTimeBeforeCure = BottomWaitTimeBeforeCure;
                        else if (property == nameof(BottomExposureTime)) layer.ExposureTime = BottomExposureTime;
                        else if (property == nameof(BottomWaitTimeAfterCure)) layer.WaitTimeAfterCure = BottomWaitTimeAfterCure;
                        else if (property == nameof(BottomLiftHeight)) layer.LiftHeight = BottomLiftHeight;
                        else if (property == nameof(BottomLiftSpeed)) layer.LiftSpeed = BottomLiftSpeed;
                        else if (property == nameof(BottomLiftHeight2)) layer.LiftHeight2 = BottomLiftHeight2;
                        else if (property == nameof(BottomLiftSpeed2)) layer.LiftSpeed2 = BottomLiftSpeed2;
                        else if (property == nameof(BottomWaitTimeAfterLift)) layer.WaitTimeAfterLift = BottomWaitTimeAfterLift;
                        else if (property == nameof(BottomRetractSpeed)) layer.RetractSpeed = BottomRetractSpeed;
                        else if (property == nameof(BottomRetractHeight2)) layer.RetractHeight2 = BottomRetractHeight2;
                        else if (property == nameof(BottomRetractSpeed2)) layer.RetractSpeed2 = BottomRetractSpeed2;
                        else if (property == nameof(BottomLightPWM)) layer.LightPWM = BottomLightPWM;

                        // Propagate value to layer when bottom property does not exists
                        else if (property == nameof(LightOffDelay) && !CanUseBottomLightOffDelay) layer.LightOffDelay = LightOffDelay;
                        else if (property == nameof(WaitTimeBeforeCure) && !CanUseBottomWaitTimeBeforeCure) layer.WaitTimeBeforeCure = WaitTimeBeforeCure;
                        else if (property == nameof(ExposureTime) && !CanUseBottomExposureTime) layer.ExposureTime = ExposureTime;
                        else if (property == nameof(WaitTimeAfterCure) && !CanUseBottomWaitTimeAfterCure) layer.WaitTimeAfterCure = WaitTimeAfterCure;
                        else if (property == nameof(LiftHeight) && !CanUseBottomLiftHeight) layer.LiftHeight = LiftHeight;
                        else if (property == nameof(LiftSpeed) && !CanUseBottomLiftSpeed) layer.LiftSpeed = LiftSpeed;
                        else if (property == nameof(LiftHeight2) && !CanUseBottomLiftHeight2) layer.LiftHeight2 = LiftHeight2;
                        else if (property == nameof(LiftSpeed2) && !CanUseBottomLiftSpeed2) layer.LiftSpeed2 = LiftSpeed2;
                        else if (property == nameof(WaitTimeAfterLift) && !CanUseBottomWaitTimeAfterLift) layer.WaitTimeAfterLift = WaitTimeAfterLift;
                        else if (property == nameof(RetractSpeed) && !CanUseBottomRetractSpeed) layer.RetractSpeed = RetractSpeed;
                        else if (property == nameof(RetractHeight2) && !CanUseBottomRetractHeight2) layer.RetractHeight2 = RetractHeight2;
                        else if (property == nameof(RetractSpeed2) && !CanUseRetractSpeed2) layer.RetractSpeed2 = RetractSpeed2;
                        else if (property == nameof(LightPWM) && !CanUseBottomLightPWM) layer.LightPWM = LightPWM;
                    }
                    else // Normal layers
                    {
                        if (property == nameof(LightOffDelay)) layer.LightOffDelay = LightOffDelay;
                        else if (property == nameof(WaitTimeBeforeCure)) layer.WaitTimeBeforeCure = WaitTimeBeforeCure;
                        else if (property == nameof(ExposureTime)) layer.ExposureTime = ExposureTime;
                        else if (property == nameof(WaitTimeAfterCure)) layer.WaitTimeAfterCure = WaitTimeAfterCure;
                        else if (property == nameof(LiftHeight)) layer.LiftHeight = LiftHeight;
                        else if (property == nameof(LiftSpeed)) layer.LiftSpeed = LiftSpeed;
                        else if (property == nameof(LiftHeight2)) layer.LiftHeight2 = LiftHeight2;
                        else if (property == nameof(LiftSpeed2)) layer.LiftSpeed2 = LiftSpeed2;
                        else if (property == nameof(WaitTimeAfterLift)) layer.WaitTimeAfterLift = WaitTimeAfterLift;
                        else if (property == nameof(RetractSpeed)) layer.RetractSpeed = RetractSpeed;
                        else if (property == nameof(RetractHeight2)) layer.RetractHeight2 = RetractHeight2;
                        else if (property == nameof(RetractSpeed2)) layer.RetractSpeed2 = RetractSpeed2;
                        else if (property == nameof(LightPWM)) layer.LightPWM = LightPWM;
                    }
                }
            }

            layer.MaterialMilliliters = -1; // Recalculate this value to be sure
        }

        RebuildGCode();
    }

    /// <summary>
    /// Set LiftHeight to 0 if previous and current have same PositionZ
    /// <param name="zeroLightOffDelay">If true also set light off to 0, otherwise current value will be kept.</param>
    /// </summary>
    public void SetNoLiftForSamePositionedLayers(bool zeroLightOffDelay = false)
        => SetLiftForSamePositionedLayers(0, zeroLightOffDelay);

    public void SetLiftForSamePositionedLayers(float liftHeight = 0, bool zeroLightOffDelay = false)
    {
        for (int layerIndex = 1; layerIndex < LayerCount; layerIndex++)
        {
            var layer = this[layerIndex];
            if (this[layerIndex - 1].PositionZ != layer.PositionZ) continue;
            layer.LiftHeightTotal = liftHeight;
            layer.WaitTimeAfterLift = 0;
            if (zeroLightOffDelay)
            {
                layer.LightOffDelay = 0;
                layer.WaitTimeBeforeCure = 0;
                layer.WaitTimeAfterCure = 0;
            }
        }
        RebuildGCode();
    }

    public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, MatCacheManager cacheManager, out uint lastLayerIndex)
    {
        var startLayerPositionZ = this[layerIndex].PositionZ;
        lastLayerIndex = layerIndex;
        var layerMat = cacheManager.Get1(layerIndex).Clone();

        for (var curIndex = layerIndex + 1; curIndex < LayerCount && this[curIndex].PositionZ == startLayerPositionZ; curIndex++)
        {
            CvInvoke.Max(layerMat, cacheManager.Get1(curIndex), layerMat);
            lastLayerIndex = curIndex;
        }

        return layerMat;
    }

    public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, MatCacheManager cacheManager)
        => GetMergedMatForSequentialPositionedLayers(layerIndex, cacheManager, out _);

    public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex, out uint lastLayerIndex)
    {
        var startLayer = this[layerIndex];
        lastLayerIndex = layerIndex;
        var layerMat = startLayer.LayerMat;

        for (var curIndex = layerIndex + 1; curIndex < LayerCount && this[curIndex].PositionZ == startLayer.PositionZ; curIndex++)
        {
            using var nextLayer = this[curIndex].LayerMat;
            CvInvoke.Max(nextLayer, layerMat, layerMat);
            lastLayerIndex = curIndex;
        }

        return layerMat;
    }

    public Mat GetMergedMatForSequentialPositionedLayers(uint layerIndex)
        => GetMergedMatForSequentialPositionedLayers(layerIndex, out _);
    #endregion

    #region Draw Modifications
    public void DrawModifications(IList<PixelOperation> drawings, OperationProgress? progress = null)
    {
        progress ??= new OperationProgress();
        progress.Reset("Drawings", (uint)drawings.Count);

        var group1 = drawings
            .Where(operation => operation.OperationType
                is PixelOperation.PixelOperationType.Drawing
                or PixelOperation.PixelOperationType.Text
                or PixelOperation.PixelOperationType.Eraser)
            .GroupBy(operation => operation.LayerIndex);

        Parallel.ForEach(group1, CoreSettings.GetParallelOptions(progress), layerOperationGroup =>
        {
            var layer = this[layerOperationGroup.Key];
            using var mat = layer.LayerMat;

            foreach (var operation in layerOperationGroup)
            {
                if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                {
                    var operationDrawing = (PixelDrawing)operation;

                    if (operationDrawing.BrushSize == 1)
                    {
                        mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Brightness);
                        continue;
                    }

                    mat.DrawPolygon((byte)operationDrawing.BrushShape, operationDrawing.BrushSize / 2, operationDrawing.Location,
                        new MCvScalar(operationDrawing.Brightness), operationDrawing.RotationAngle, operationDrawing.Thickness, operationDrawing.LineType);
                    /*switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Square:
                            CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                                new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }*/
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                {
                    var operationText = (PixelText)operation;
                    mat.PutTextRotated(operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(operationText.Brightness), operationText.Thickness, operationText.LineType, operationText.Mirror, operationText.LineAlignment, operationText.Angle);
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                {
                    using var layerContours = mat.FindContours(out var hierarchy, RetrType.Tree);

                    if (mat.GetByte(operation.Location) >= 10)
                    {
                        using var vec = EmguContours.GetContoursInside(layerContours, hierarchy, operation.Location);

                        if (vec.Size > 0)
                        {
                            CvInvoke.DrawContours(mat, vec, -1, new MCvScalar(operation.PixelBrightness), -1);
                        }
                    }
                }
            }

            layer.LayerMat = mat;
            progress.LockAndIncrement();
        });

        var group2 = drawings
            .Where(operation => operation.OperationType
                is PixelOperation.PixelOperationType.Supports
                or PixelOperation.PixelOperationType.DrainHole)
            .GroupBy(operation => operation.LayerIndex)
            .OrderByDescending(group => group.Key);

        if (group2.Any())
        {
            using var matCache = new MatCacheManager(this, 0, group2.First().Key)
            {
                AutoDispose = true,
                Direction = false
            };
            foreach (var layerOperationGroup in group2)
            {
                var toProcess = layerOperationGroup.ToList();
                var drawnSupportLayers = 0;
                var drawnDrainHoleLayers = 0;
                for (int operationLayer = (int)layerOperationGroup.Key - 1; operationLayer >= 0 && toProcess.Count > 0; operationLayer--)
                {
                    progress.ThrowIfCancellationRequested();
                    var layer = this[operationLayer];
                    var mat = matCache.Get1((uint) operationLayer);
                    var isMatModified = false;

                    for (var i = toProcess.Count-1; i >= 0; i--)
                    {
                        var operation = toProcess[i];
                        if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                        {
                            var operationSupport = (PixelSupport) operation;

                            int radius = (operationLayer > 10
                                ? Math.Min(operationSupport.TipDiameter + drawnSupportLayers, operationSupport.PillarDiameter)
                                : operationSupport.BaseDiameter) / 2;
                            uint whitePixels;

                            int yStart = Math.Max(0, operation.Location.Y - operationSupport.TipDiameter / 2);
                            int xStart = Math.Max(0, operation.Location.X - operationSupport.TipDiameter / 2);

                            using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationSupport.TipDiameter, operationSupport.TipDiameter)))
                            {
                                using var matCircleMask = matCircleRoi.NewBlank();
                                CvInvoke.Circle(matCircleMask,
                                    new Point(operationSupport.TipDiameter / 2, operationSupport.TipDiameter / 2),
                                    operationSupport.TipDiameter / 2, new MCvScalar(operation.PixelBrightness), -1);
                                CvInvoke.BitwiseAnd(matCircleRoi, matCircleMask, matCircleMask);
                                whitePixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                            }

                            if (whitePixels >= Math.Pow(operationSupport.TipDiameter, 2) / 3)
                            {
                                //CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                                if (drawnSupportLayers == 0) continue; // Supports nonexistent, keep digging
                                toProcess.RemoveAt(i);
                                continue; // White area end supporting
                            }

                            CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(operation.PixelBrightness), -1, operationSupport.LineType);
                            isMatModified = true;
                            drawnSupportLayers++;
                        }
                        else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                        {
                            var operationDrainHole = (PixelDrainHole) operation;

                            int radius = operationDrainHole.Diameter / 2;
                            uint blackPixels;

                            int yStart = Math.Max(0, operation.Location.Y - radius);
                            int xStart = Math.Max(0, operation.Location.X - radius);

                            using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationDrainHole.Diameter, operationDrainHole.Diameter)))
                            {
                                using var matCircleRoiInv = new Mat();
                                CvInvoke.Threshold(matCircleRoi, matCircleRoiInv, 100, 255, ThresholdType.BinaryInv);
                                using var matCircleMask = matCircleRoi.NewBlank();
                                CvInvoke.Circle(matCircleMask, new Point(radius, radius), radius, EmguExtensions.WhiteColor, -1);
                                CvInvoke.BitwiseAnd(matCircleRoiInv, matCircleMask, matCircleMask);
                                blackPixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                            }

                            if (blackPixels >= Math.Pow(operationDrainHole.Diameter, 2) / 3) // Enough area to drain?
                            {
                                if (drawnDrainHoleLayers == 0) continue; // Drill not found a target yet, keep digging
                                toProcess.RemoveAt(i);
                                continue; // Stop drill drain found!
                            }

                            CvInvoke.Circle(mat, operation.Location, radius, EmguExtensions.BlackColor, -1, operationDrainHole.LineType);
                            isMatModified = true;
                            drawnDrainHoleLayers++;
                        }
                    }

                    if (isMatModified)
                    {
                        layer.LayerMat = mat;
                    }
                }

                progress += (uint)layerOperationGroup.Count();
            }
        }

        /*
         // Old and memory hunger code
        ConcurrentDictionary<uint, Mat> modifiedLayers = new();
        for (var i = 0; i < drawings.Count; i++)
        {
            var operation = drawings[i];

            if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
            {
                var operationDrawing = (PixelDrawing)operation;
                var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                if (operationDrawing.BrushSize == 1)
                {
                    mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Brightness);
                    continue;
                }

                mat.DrawPolygon((byte)operationDrawing.BrushShape, operationDrawing.BrushSize / 2, operationDrawing.Location,
                    new MCvScalar(operationDrawing.Brightness), operationDrawing.RotationAngle, operationDrawing.Thickness, operationDrawing.LineType);
                //switch (operationDrawing.BrushShape)
                //{
                //    case PixelDrawing.BrushShapeType.Square:
                //        CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                //        break;
                //    case PixelDrawing.BrushShapeType.Circle:
                //        CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                //            new MCvScalar(operationDrawing.Brightness), operationDrawing.Thickness, operationDrawing.LineType);
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException();
                //}
            }
            else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
            {
                var operationText = (PixelText)operation;
                var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                mat.PutTextRotated(operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(operationText.Brightness), operationText.Thickness, operationText.LineType, operationText.Mirror, operationText.LineAlignment, operationText.Angle);
            }
            else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
            {
                var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                using var layerContours = mat.FindContours(out var hierarchy, RetrType.Tree);

                if (mat.GetByte(operation.Location) >= 10)
                {
                    using var vec = EmguContours.GetContoursInside(layerContours, hierarchy, operation.Location);

                    if (vec.Size > 0)
                    {
                        CvInvoke.DrawContours(mat, vec, -1, new MCvScalar(operation.PixelBrightness), -1);
                    }
                }
            }
            else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
            {
                var operationSupport = (PixelSupport)operation;
                int drawnLayers = 0;
                for (int operationLayer = (int)operation.LayerIndex - 1; operationLayer >= 0; operationLayer--)
                {
                    var mat = modifiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
                    int radius = (operationLayer > 10 ? Math.Min(operationSupport.TipDiameter + drawnLayers, operationSupport.PillarDiameter) : operationSupport.BaseDiameter) / 2;
                    uint whitePixels;

                    int yStart = Math.Max(0, operation.Location.Y - operationSupport.TipDiameter / 2);
                    int xStart = Math.Max(0, operation.Location.X - operationSupport.TipDiameter / 2);

                    using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationSupport.TipDiameter, operationSupport.TipDiameter)))
                    {
                        using var matCircleMask = matCircleRoi.NewBlank();
                        CvInvoke.Circle(matCircleMask, new Point(operationSupport.TipDiameter / 2, operationSupport.TipDiameter / 2),
                            operationSupport.TipDiameter / 2, new MCvScalar(operation.PixelBrightness), -1);
                        CvInvoke.BitwiseAnd(matCircleRoi, matCircleMask, matCircleMask);
                        whitePixels = (uint)CvInvoke.CountNonZero(matCircleMask);
                    }

                    if (whitePixels >= Math.Pow(operationSupport.TipDiameter, 2) / 3)
                    {
                        //CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                        if (drawnLayers == 0) continue; // Supports nonexistent, keep digging
                        break; // White area end supporting
                    }

                    CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(operation.PixelBrightness), -1, operationSupport.LineType);
                    drawnLayers++;
                }
            }
            else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
            {
                uint drawnLayers = 0;
                var operationDrainHole = (PixelDrainHole)operation;
                for (int operationLayer = (int)operation.LayerIndex; operationLayer >= 0; operationLayer--)
                {
                    var mat = modifiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
                    int radius = operationDrainHole.Diameter / 2;
                    uint blackPixels;

                    int yStart = Math.Max(0, operation.Location.Y - radius);
                    int xStart = Math.Max(0, operation.Location.X - radius);

                    using (var matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationDrainHole.Diameter, operationDrainHole.Diameter)))
                    {
                        using var matCircleRoiInv = new Mat();
                        CvInvoke.Threshold(matCircleRoi, matCircleRoiInv, 100, 255, ThresholdType.BinaryInv);
                        using var matCircleMask = matCircleRoi.NewBlank();
                        CvInvoke.Circle(matCircleMask, new Point(radius, radius), radius, EmguExtensions.WhiteColor, -1);
                        CvInvoke.BitwiseAnd(matCircleRoiInv, matCircleMask, matCircleMask);
                        blackPixels = (uint)CvInvoke.CountNonZero(matCircleMask);
                    }

                    if (blackPixels >= Math.Pow(operationDrainHole.Diameter, 2) / 3) // Enough area to drain?
                    {
                        if (drawnLayers == 0) continue; // Drill not found a target yet, keep digging
                        break; // Stop drill drain found!
                    }

                    CvInvoke.Circle(mat, operation.Location, radius, EmguExtensions.BlackColor, -1, operationDrainHole.LineType);
                    drawnLayers++;
                }
            }

            progress++;
        }

        progress.Reset("Saving", (uint)modifiedLayers.Count);
        Parallel.ForEach(modifiedLayers, CoreSettings.GetParallelOptions(progress), modifiedLayer =>
        {
            this[modifiedLayer.Key].LayerMat = modifiedLayer.Value;
            modifiedLayer.Value.Dispose();

            progress.LockAndIncrement();
        });
        */
    }
    #endregion

    #region Generators methods

    /// <summary>
    /// Generates a heatmap based on a stack of layers
    /// </summary>
    /// <param name="layerIndexStart">Layer index to start from</param>
    /// <param name="layerIndexEnd">Layer index to end on</param>
    /// <param name="roi">Region of interest</param>
    /// <param name="progress"></param>
    /// <returns>Heatmap grayscale Mat</returns>
    public Mat GenerateHeatmap(uint layerIndexStart = 0, uint layerIndexEnd = uint.MaxValue, Rectangle roi = default, OperationProgress? progress = null)
    {
        SanitizeLayerIndex(ref layerIndexEnd);

        progress ??= new OperationProgress();
        progress.Title = $"Generating a heatmap from layers {layerIndexStart} through {layerIndexEnd}";
        progress.ItemName = "layers";

        if (roi.IsEmpty) roi = ResolutionRectangle;
        
        var resultMat = EmguExtensions.InitMat(roi.Size, 1, DepthType.Cv32S);
        var layerRange = GetDistinctLayersByPositionZ(layerIndexStart, layerIndexEnd).ToArray();

        progress.ItemCount = (uint)layerRange.Length;
        
        Parallel.ForEach(layerRange, CoreSettings.GetParallelOptions(progress), layer =>
        {
            using var mat = GetMergedMatForSequentialPositionedLayers(layer.Index);
            using var mat32Roi = mat.Roi(roi);

            mat32Roi.ConvertTo(mat32Roi, DepthType.Cv32S);
            
            lock (progress.Mutex)
            {
                CvInvoke.Add(resultMat, mat32Roi, resultMat);
                progress++;
            }
        });


        resultMat.ConvertTo(resultMat, DepthType.Cv8U, 1.0 / layerRange.Length);

        return resultMat;
    }

    /// <summary>
    /// Generates a heatmap based on a stack of layers
    /// </summary>
    /// <param name="roi">Region of interest</param>
    /// <param name="progress"></param>
    /// <returns>Heatmap grayscale Mat</returns>
    public Mat GenerateHeatmap(Rectangle roi, OperationProgress? progress = null) => GenerateHeatmap(0, uint.MaxValue, roi, progress);

    public Task<Mat> GenerateHeatmapAsync(uint layerIndexStart = 0, uint layerIndexEnd = uint.MaxValue, Rectangle roi = default, OperationProgress? progress = null) 
        => Task.Run(() => GenerateHeatmap(layerIndexStart, layerIndexEnd, roi, progress));

    public Task<Mat> GenerateHeatmapAsync(Rectangle roi, OperationProgress? progress = null) 
        => Task.Run(() => GenerateHeatmap(0, uint.MaxValue, roi, progress));

    #endregion
}