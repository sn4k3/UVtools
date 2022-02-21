/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    /// <summary>
    /// Slicer <see cref="FileFormat"/> representation
    /// </summary>
    public abstract class FileFormat : BindableBase, IDisposable, IEquatable<FileFormat>, IEnumerable<Layer>
    {
        #region Constants
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
            public static PrintParameterModifier TransitionLayerCount { get; } = new ("Transition layer count", "Number of transition layers", "layers",0, ushort.MaxValue, 1, 0);
            
            public static PrintParameterModifier BottomLightOffDelay { get; } = new("Bottom light-off seconds", "Total motor movement time + rest time to wait before cure a new bottom layer", "s");
            public static PrintParameterModifier LightOffDelay { get; } = new("Light-off seconds", "Total motor movement time + rest time to wait before cure a new layer", "s");

            public static PrintParameterModifier BottomWaitTimeBeforeCure { get; } = new ("Bottom wait before cure", "Time to wait/rest before cure a new bottom layer\nChitubox: Rest after retract\nLychee: Wait before print", "s");
            public static PrintParameterModifier WaitTimeBeforeCure { get; } = new ("Wait before cure", "Time to wait/rest before cure a new layer\nChitubox: Rest after retract\nLychee: Wait before print", "s");
            
            public static PrintParameterModifier BottomExposureTime { get; } = new ("Bottom exposure time", "Bottom layers cure time", "s", 0.1M);
            public static PrintParameterModifier ExposureTime { get; } = new ("Exposure time", "Layers cure time", "s", 0.1M);
           
            public static PrintParameterModifier BottomWaitTimeAfterCure { get; } = new("Bottom wait after cure", "Time to wait/rest after cure a new bottom layer\nChitubox: Rest before lift\nLychee: Wait after print", "s");
            public static PrintParameterModifier WaitTimeAfterCure { get; } = new("Wait after cure", "Time to wait/rest after cure a new bottom layer\nChitubox: Rest before lift\nLychee: Wait after print", "s");
            
            public static PrintParameterModifier BottomLiftHeight { get; } = new ("Bottom lift height", "Bottom lift/peel height between layers", "mm");
            public static PrintParameterModifier LiftHeight { get; } = new ("Lift height", @"Lift/peel height between layers", "mm");
            
            public static PrintParameterModifier BottomLiftSpeed { get; } = new ("Bottom lift speed", null, "mm/min", 10, 5000, 5);
            public static PrintParameterModifier LiftSpeed { get; } = new ("Lift speed", null, "mm/min", 10, 5000, 5);

            public static PrintParameterModifier BottomLiftHeight2 { get; } = new("2) Bottom lift height", "Bottom second lift/peel height between layers", "mm");
            public static PrintParameterModifier LiftHeight2 { get; } = new("2) Lift height", @"Second lift/peel height between layers", "mm");

            public static PrintParameterModifier BottomLiftSpeed2 { get; } = new("2) Bottom lift speed", null, "mm/min", 10, 5000, 5);
            public static PrintParameterModifier LiftSpeed2 { get; } = new("2) Lift speed", null, "mm/min", 10, 5000, 5);

            public static PrintParameterModifier BottomWaitTimeAfterLift { get; } = new("Bottom wait after lift", "Time to wait/rest after a lift/peel sequence at bottom layers\nChitubox: Rest after lift\nLychee: Wait after lift", "s");
            public static PrintParameterModifier WaitTimeAfterLift { get; } = new("Wait after lift", "Time to wait/rest after a lift/peel sequence at layers\nChitubox: Rest after lift\nLychee: Wait after lift", "s");
           
            public static PrintParameterModifier BottomRetractSpeed { get; } = new ("Bottom retract speed", "Bottom down speed from lift height to next layer cure position", "mm/min", 10, 5000, 5);
            public static PrintParameterModifier RetractSpeed { get; } = new ("Retract speed", "Down speed from lift height to next layer cure position", "mm/min", 10, 5000, 5);

            public static PrintParameterModifier BottomRetractHeight2 { get; } = new("2) Bottom retract height", null, "mm");
            public static PrintParameterModifier RetractHeight2 { get; } = new("2) Retract height", null, "mm");
            public static PrintParameterModifier BottomRetractSpeed2 { get; } = new("2) Bottom retract speed", null, "mm/min", 10, 5000, 5);
            public static PrintParameterModifier RetractSpeed2 { get; } = new("2) Retract speed", null, "mm/min", 10, 5000, 5);

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
            public PrintParameterModifier(string name, string description = null, string valueUnit = null, decimal minimum = 0, decimal maximum = 1000, double increment = 0.5, byte decimalPlates = 2)
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

            public override bool Equals(object obj)
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
            new FDGFile(), // fdg
            new PhotonWorkshopFile(),   // PSW
            new CWSFile(),   // CWS
            new OSLAFile(),  // OSLA
            new ZCodeFile(),   // zcode
            new ZCodexFile(),   // zcodex
            new MDLPFile(),   // MKS v1
            new GR1File(),   // GR1 Workshop
            //new CXDLPv1File(),   // Creality Box v1
            new CXDLPFile(),   // Creality Box
            new LGSFile(),   // LGS, LGS30
            new FlashForgeSVGXFile(), // SVGX
            new GenericZIPFile(),   // Generic zip files
            new VDAFile(),   // VDA
            new VDTFile(),   // VDT
            new UVJFile(),   // UVJ
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
        public static FileFormat FindByExtensionOrFilePath(string extensionOrFilePath, bool createNewInstance = false)
        {
            if (string.IsNullOrWhiteSpace(extensionOrFilePath)) return null;

            bool isFilePath = false;
            // Test for ext first
            var fileFormats = AvailableFormats.Where(fileFormat => fileFormat.IsExtensionValid(extensionOrFilePath)).ToArray();
            if (fileFormats.Length == 0) // Extension not found, can be filepath, try to find it
            {
                GetFileNameStripExtensions(extensionOrFilePath, out var extension);
                if (string.IsNullOrWhiteSpace(extension)) return null;

                fileFormats = AvailableFormats.Where(fileFormat => fileFormat.IsExtensionValid(extension)).ToArray();
                if (fileFormats.Length == 0) return null;
                isFilePath = true; // Was a file path
            }

            if (fileFormats.Length == 1 || !isFilePath) 
                return createNewInstance 
                    ? (FileFormat)Activator.CreateInstance(fileFormats[0].GetType()) 
                    : fileFormats[0];

            // Multiple instances using Check for valid candidate
            foreach (var fileFormat in fileFormats)
            {
                if (fileFormat.CanProcess(extensionOrFilePath))
                {
                    return createNewInstance
                        ? (FileFormat)Activator.CreateInstance(fileFormat.GetType())
                        : fileFormat;
                }
            }

            // Try this in a far and not probable attempt
            return createNewInstance
                ? (FileFormat)Activator.CreateInstance(fileFormats[0].GetType())
                : fileFormats[0];
        }

        public static FileExtension FindExtension(string extension)
        {
            return AvailableFormats.SelectMany(format => format.FileExtensions).FirstOrDefault(ext => ext.Equals(extension));
        }

        /// <summary>
        /// Find <see cref="FileFormat"/> by an type
        /// </summary>
        /// <param name="type">Type to find</param>
        /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
        /// <returns><see cref="FileFormat"/> object or null if not found</returns>
        public static FileFormat FindByType(Type type, bool createNewInstance = false)
        {
            var fileFormat = AvailableFormats.FirstOrDefault(format => format.GetType() == type);
            if (fileFormat is null) return null;
            return createNewInstance
                ? (FileFormat)Activator.CreateInstance(type)
                : fileFormat;
            //return (from t in AvailableFormats where type == t.GetType() select createNewInstance ? (FileFormat) Activator.CreateInstance(type) : t).FirstOrDefault();
        }

        public static string GetFileNameStripExtensions(string filepath)
        {
            //if (file.EndsWith(TemporaryFileAppend)) file = Path.GetFileNameWithoutExtension(file);
            return PathExtensions.GetFileNameStripExtensions(filepath, AllFileExtensionsString.OrderByDescending(s => s.Length).ToList(), out _);
        }

        public static string GetFileNameStripExtensions(string filepath, out string strippedExtension)
        {
            //if (file.EndsWith(TemporaryFileAppend)) file = Path.GetFileNameWithoutExtension(file);
            return PathExtensions.GetFileNameStripExtensions(filepath, AllFileExtensionsString.OrderByDescending(s => s.Length).ToList(), out strippedExtension);
        }

        public static FileFormat Open(string fileFullPath, FileDecodeType decodeType, OperationProgress progress = null)
        {
            var slicerFile = FindByExtensionOrFilePath(fileFullPath, true);
            if (slicerFile is null) return null;
            slicerFile.Decode(fileFullPath, decodeType, progress);
            return slicerFile;
        }

        public static FileFormat Open(string fileFullPath, OperationProgress progress = null) =>
            Open(fileFullPath, FileDecodeType.Full, progress);

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

                var fromValueStr = from.GetValueFromPrintParameterModifier(sourceModifier).ToString();
                var toValueStr = to.GetValueFromPrintParameterModifier(sourceModifier).ToString();

                if (decimal.TryParse(fromValueStr, out var fromValue) && decimal.TryParse(toValueStr, out var toValue) && fromValue != toValue)
                {
                    to.SetValueFromPrintParameterModifier(sourceModifier, fromValue);
                    count++;
                }
            }
            
            return count;
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
        #endregion

        #region Members
        public object Mutex = new();

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
        private string _materialName;
        private float _materialGrams;
        private float _materialCost;
        private bool _suppressRebuildGCode;

        private readonly Timer _queueTimerPrintTime = new(QueueTimerPrintTime){AutoReset = false};

        #endregion

        #region Properties

        /// <summary>
        /// Gets the file format type
        /// </summary>
        public abstract FileFormatType FileType { get; }

        /// <summary>
        /// Gets the valid file extensions for this <see cref="FileFormat"/>
        /// </summary>
        public abstract FileExtension[] FileExtensions { get; }

        public FileDecodeType DecodeType { get; private set; } = FileDecodeType.Full;

        /// <summary>
        /// Gets the available <see cref="PrintParameterModifier"/>
        /// </summary>
        public virtual PrintParameterModifier[] PrintParameterModifiers => null;

        /// <summary>
        /// Gets the available <see cref="PrintParameterModifier"/> per layer
        /// </summary>
        public virtual PrintParameterModifier[] PrintParameterPerLayerModifiers => null;

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
            SupportPerLayerSettings && PrintParameterPerLayerModifiers.Contains(modifier);

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
        /// Gets the input file path loaded into this <see cref="FileFormat"/>
        /// </summary>
        public string FileFullPath { get; set; }

        public string DirectoryPath => Path.GetDirectoryName(FileFullPath);
        public string Filename => Path.GetFileName(FileFullPath);
        public string FileExtension => Path.GetExtension(FileFullPath);
        public string FilenameNoExt => GetFileNameStripExtensions(FileFullPath);

        /// <summary>
        /// Gets the available versions to set in this file format
        /// </summary>
        public virtual uint[] AvailableVersions { get; }

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
        public virtual Size[] ThumbnailsOriginalSize => null;

        /// <summary>
        /// Gets the thumbnails for this <see cref="FileFormat"/>
        /// </summary>
        public Mat[] Thumbnails { get; set; }

        /// <summary>
        /// Gets the cached layers into compressed bytes
        /// </summary>
        public LayerManager LayerManager
        {
            get;
            /*set
            {
                var oldLayerManager = _layerManager;
                if (!RaiseAndSetIfChanged(ref _layerManager, value) || value is null) return;

                if(!ReferenceEquals(this, _layerManager.SlicerFile)) // Auto fix parent slicer file
                {
                    _layerManager.SlicerFile = this;
                }

                // Recalculate changes
                PrintHeight = PrintHeight;
                PrintTime = PrintTimeComputed;
                MaterialMilliliters = -1;

                if (oldLayerManager is null) return; // Init

                if (oldLayerManager.LayerCount != LayerCount)
                {
                    LayerCount = _layerManager.LayerCount;
                    if (SuppressRebuildProperties) return;
                    if (LayerCount == 0 || this[LastLayerIndex] is null) return; // Not initialized
                    LayerManager.RebuildLayersProperties();
                }
            }*/
        }

        public IssueManager IssueManager { get; }

        /// <summary>
        /// Gets the first layer
        /// </summary>
        public Layer FirstLayer => LayerManager.FirstLayer;

        /// <summary>
        /// Gets the last bottom layer
        /// </summary>
        public Layer LastBottomLayer => LayerManager.LastOrDefault(layer => layer.IsBottomLayer);

        /// <summary>
        /// Gets the first normal layer
        /// </summary>
        public Layer FirstNormalLayer => LayerManager.FirstOrDefault(layer => layer.IsNormalLayer);

        /// <summary>
        /// Gets the last layer
        /// </summary>
        public Layer LastLayer => LayerManager.LastLayer;

        /// <summary>
        /// Gets all bottom layers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Layer> BottomLayers => LayerManager.BottomLayers;

        /// <summary>
        /// Gets all normal layers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Layer> NormalLayers => LayerManager.NormalLayers;

        /// <summary>
        /// Gets all transition layers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Layer> TransitionLayers => LayerManager.TransitionLayers;

        /// <summary>
        /// Gets all layers that use TSMC values
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Layer> TsmcLayers => LayerManager.TsmcLayers;

        /// <summary>
        /// Gets the bounding rectangle of the object
        /// </summary>
        public Rectangle BoundingRectangle => LayerManager.BoundingRectangle;

        /// <summary>
        /// Gets the bounding rectangle of the object in millimeters
        /// </summary>
        public RectangleF BoundingRectangleMillimeters => LayerManager.BoundingRectangleMillimeters;

        /// <summary>
        /// Gets or sets if modifications require a full encode to save
        /// </summary>
        public bool RequireFullEncode
        {
            get => _haveModifiedLayers || LayerManager.IsModified;
            set => RaiseAndSetIfChanged(ref _haveModifiedLayers, value);
        } // => LayerManager.IsModified;

        /// <summary>
        /// Gets the image width resolution
        /// </summary>
        public Size Resolution
        {
            get => new((int)ResolutionX, (int)ResolutionY);
            set
            {
                ResolutionX = (uint) value.Width;
                ResolutionY = (uint) value.Height;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Xppmm));
                RaisePropertyChanged(nameof(Yppmm));
                RaisePropertyChanged(nameof(Ppmm));
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
        /// Gets the size of display in millimeters
        /// </summary>
        public SizeF Display
        {
            get => new(DisplayWidth, DisplayHeight);
            set
            {
                DisplayWidth = value.Width;
                DisplayHeight = value.Height;
                RaisePropertyChanged();
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
        public float DisplayDiagonalInches => (float)Math.Round(Math.Sqrt(Math.Pow(DisplayWidth, 2) + Math.Pow(DisplayHeight, 2)) * UnitExtensions.MillimeterInInch, 2);

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
        public virtual Enumerations.FlipDirection DisplayMirror { get; set; } = Enumerations.FlipDirection.None;

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
        /// First layer index, this is always 0
        /// </summary>
        public const uint FirstLayerIndex = 0;

        /// <summary>
        /// Gets the last layer index
        /// </summary>
        public uint LastLayerIndex => LayerManager.LastLayerIndex;

        /// <summary>
        /// Checks if this file format supports per layer settings
        /// </summary>
        public bool SupportPerLayerSettings => PrintParameterPerLayerModifiers is not null && PrintParameterPerLayerModifiers.Length > 0;

        /// <summary>
        /// Gets or sets the layer count
        /// </summary>
        public virtual uint LayerCount
        {
            get => LayerManager.LayerCount;
            set {
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NormalLayerCount));
            }
        }

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
            }
        }

        /// <summary>
        /// Gets the transition layer type
        /// </summary>
        public virtual TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Firmware;

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
                if (LayerManager.Layers is null) return uint.MaxValue;
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
        public bool CanUseLayerLightPWM => HaveLayerParameterModifier(PrintParameterModifier.LightPWM);

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
            return MoreLinq.MoreEnumerable.Batch(Enumerable.Range(0, (int)LayerCount), batchSize);
        }

        public IEnumerable<IEnumerable<Layer>> BatchLayers(int batchSize = 0)
        {
            if (batchSize <= 0) batchSize = Environment.ProcessorCount * 10;
            return MoreLinq.MoreEnumerable.Batch(this, batchSize);
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
                bool computeGeneral = LayerManager is null;
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
                return TimeSpan.FromSeconds(printTime >= float.PositiveInfinity ? 0 : printTime).ToString("hh\\hmm\\m");
            }
        }

        /// <summary>
        /// Gets the estimate used material in ml
        /// </summary>
        public virtual float MaterialMilliliters {
            get => _materialMilliliters;
            set
            {
                if (value <= 0)
                {
                    value = (float)Math.Round(this.Where(layer => layer is not null).Sum(layer => layer.MaterialMilliliters), 3);
                }
                else
                {
                    value = (float)Math.Round(value, 3);
                }

                RaiseAndSetIfChanged(ref _materialMilliliters, value);
            }
        }

        //public float MaterialMillilitersComputed =>


        /// <summary>
        /// Gets the estimate material in grams
        /// </summary>
        public virtual float MaterialGrams
        {
            get => _materialGrams;
            set => RaiseAndSetIfChanged(ref _materialGrams, value);
        }

        /// <summary>
        /// Gets the estimate material cost
        /// </summary>
        public virtual float MaterialCost
        {
            get => _materialCost;
            set => RaiseAndSetIfChanged(ref _materialCost, value);
        }

        /// <summary>
        /// Gets the material name
        /// </summary>
        public virtual string MaterialName
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
        public GCodeBuilder GCode { get; set; }

        /// <summary>
        /// Gets the GCode, returns null if not supported
        /// </summary>
        public string GCodeStr
        {
            get => GCode?.ToString();
            set
            {
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
        public bool HaveGCode => SupportsGCode && !GCode.IsEmpty;

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
        public abstract object[] Configs { get; }

        /// <summary>
        /// Gets if this file is valid to read
        /// </summary>
        public bool IsValid => FileFullPath is not null;
        #endregion

        #region Constructor
        protected FileFormat()
        {
            LayerManager = new(this);
            IssueManager = new(this);
            Thumbnails = new Mat[ThumbnailsCount];
            PropertyChanged += OnPropertyChanged;
            _queueTimerPrintTime.Elapsed += (sender, e) => UpdatePrintTime();
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
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
                LayerManager.RebuildLayersProperties(false, e.PropertyName);
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
        public Layer this[int index]
        {
            get => LayerManager[index];
            set => LayerManager[index] = value;
        }

        public Layer this[uint index]
        {
            get => LayerManager[index];
            set => LayerManager[index] = value;
        }

        public Layer this[long index]
        {
            get => LayerManager[index];
            set => LayerManager[index] = value;
        }

        public Layer[] this[Range range] => LayerManager[range];

        #endregion

        #region Numerators
        public IEnumerator<Layer> GetEnumerator()
        {
            return LayerManager.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            return Equals(obj as FileFormat);
        }

        public bool Equals(FileFormat other)
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
            LayerManager.Clear();
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
        public virtual bool CanProcess(string fileFullPath)
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
        public void FileValidation(string fileFullPath)
        {
            if (string.IsNullOrWhiteSpace(fileFullPath)) throw new ArgumentNullException(nameof(FileFullPath), "FileFullPath can't be null nor empty.");
            if (!File.Exists(fileFullPath)) throw new FileNotFoundException("The specified file does not exists.", fileFullPath);

            if (IsExtensionValid(fileFullPath, true))
            {
                return;
            }

            throw new FileLoadException($"The specified file is not valid.", fileFullPath);
        }

        /// <summary>
        /// Checks if a extension is valid under the <see cref="FileFormat"/>
        /// </summary>
        /// <param name="extension">Extension to check without the dot (.)</param>
        /// <param name="isFilePath">True if <see cref="extension"/> is a full file path, otherwise false for extension only</param>
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
            if (extension[0] != '.') extension = $".{extension}";
            return FileFullPath.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ||
                   FileFullPath.EndsWith($"{extension}{TemporaryFileAppend}", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets a thumbnail by it height or lower
        /// </summary>
        /// <param name="maxHeight">Max height allowed</param>
        /// <returns></returns>
        public Mat GetThumbnail(uint maxHeight = 400)
        {
            for (int i = 0; i < ThumbnailsCount; i++)
            {
                if(Thumbnails[i] is null) continue;
                if (Thumbnails[i].Height <= maxHeight) return Thumbnails[i];
            }

            return null;
        }

        /// <summary>
        /// Gets a thumbnail by the largest or smallest
        /// </summary>
        /// <param name="largest">True to get the largest, otherwise false</param>
        /// <returns></returns>
        public Mat GetThumbnail(bool largest)
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
                        return Thumbnails[0].Size.Area() >= Thumbnails[1].Size.Area() ? Thumbnails[0] : Thumbnails[1];
                    }
                    else
                    {
                        return Thumbnails[0].Size.Area() <= Thumbnails[1].Size.Area() ? Thumbnails[0] : Thumbnails[1];
                    }
            }
        }

        /// <summary>
        /// Sets thumbnails from a list of thumbnails and clone them
        /// </summary>
        /// <param name="images"></param>
        public void SetThumbnails(Mat[] images)
        {
            if (images is null || images.Length == 0) return;
            byte imageIndex = 0;
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
                if (Thumbnails[i].Size != ThumbnailsOriginalSize[i])
                {
                    CvInvoke.Resize(Thumbnails[i], Thumbnails[i], ThumbnailsOriginalSize[i]);
                }
            }

            RaisePropertyChanged(nameof(Thumbnails));
        }

        /// <summary>
        /// Sets all thumbnails the same image
        /// </summary>
        /// <param name="images">Image to set</param>
        public void SetThumbnails(Mat image)
        {
            if (image is null || image.IsEmpty) return;
            for (var i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = image.Clone();
                if (ThumbnailsOriginalSize is null || i >= ThumbnailsOriginalSize.Length) continue;
                if (Thumbnails[i].Size != ThumbnailsOriginalSize[i])
                {
                    CvInvoke.Resize(Thumbnails[i], Thumbnails[i], ThumbnailsOriginalSize[i]);
                }
            }
            RaisePropertyChanged(nameof(Thumbnails));
        }

        /// <summary>
        /// Sets a thumbnail from a disk file
        /// </summary>
        /// <param name="index">Thumbnail index</param>
        /// <param name="filePath"></param>
        public void SetThumbnail(int index, string filePath)
        {
            Thumbnails[index] = CvInvoke.Imread(filePath, ImreadModes.AnyColor);
            if (Thumbnails[index].Size != ThumbnailsOriginalSize[index])
            {
                CvInvoke.Resize(Thumbnails[index], Thumbnails[index], ThumbnailsOriginalSize[index]);
            }
            RaisePropertyChanged(nameof(Thumbnails));
        }

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
        public void Encode(string fileFullPath, OperationProgress progress = null)
        {
            if (DecodeType == FileDecodeType.Partial)
            {
                throw new InvalidOperationException("File was partial decoded, a full encode is not possible.");
            }

            progress ??= new OperationProgress();
            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);

#if !DEBUG
            if (this is CTBEncryptedFile file && file.Settings.LayerPointersOffset > 0)
            {
                throw new NotSupportedException(CTBEncryptedFile.Preamble);
            }
#endif

            LayerManager.Sanitize();

            if (File.Exists(fileFullPath)) File.Delete(fileFullPath);

            FileFullPath = fileFullPath;

            for (var i = 0; i < Thumbnails.Length; i++)
            {
                if (Thumbnails[i] is null || Thumbnails[i].IsEmpty) continue;
                if(Thumbnails[i].Size == ThumbnailsOriginalSize[i]) continue;
                CvInvoke.Resize(Thumbnails[i], Thumbnails[i], new Size(ThumbnailsOriginalSize[i].Width, ThumbnailsOriginalSize[i].Height));
            }

            EncodeInternally(progress);

            LayerManager.SetAllIsModified(false);
            RequireFullEncode = false;
        }

        /// <summary>
        /// Decode a slicer file
        /// </summary>
        /// <param name="progress"></param>
        protected abstract void DecodeInternally(OperationProgress progress);

        /// <summary>
        /// Decode a slicer file
        /// </summary>
        /// <param name="fileFullPath"></param>
        /// <param name="progress"></param>
        public void Decode(string fileFullPath, OperationProgress progress = null) => Decode(fileFullPath, FileDecodeType.Full, progress);

        /// <summary>
        /// Decode a slicer file
        /// </summary>
        /// <param name="fileFullPath"></param>
        /// <param name="fileDecodeType"></param>
        /// <param name="progress"></param>
        public void Decode(string fileFullPath, FileDecodeType fileDecodeType, OperationProgress progress = null)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
            DecodeType = fileDecodeType;
            progress ??= new OperationProgress();
            progress.Reset(OperationProgress.StatusGatherLayers, LayerCount);

            DecodeInternally(progress);

            progress.Token.ThrowIfCancellationRequested();

            var layerHeightDigits = LayerHeight.DecimalDigits();
            if (layerHeightDigits > Layer.HeightPrecision)
            {
                throw new FileLoadException($"The layer height ({LayerHeight}mm) have more decimal digits than the supported ({Layer.HeightPrecision}) digits.\n" +
                                                   "Lower and fix your layer height on slicer to avoid precision errors.", fileFullPath);
            }

            bool reSaveFile = LayerManager.Sanitize();
            if (reSaveFile)
            {
                Save(progress);
            }
        }

        /// <summary>
        /// Extract contents to a folder
        /// </summary>
        /// <param name="path">Path to folder where content will be extracted</param>
        /// <param name="genericConfigExtract"></param>
        /// <param name="genericLayersExtract"></param>
        /// <param name="progress"></param>
        public virtual void Extract(string path, bool genericConfigExtract = true, bool genericLayersExtract = true,
            OperationProgress progress = null)
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
                if (Configs is not null)
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
                
                progress.CanCancel = false;
                ZipArchiveExtensions.ImprovedExtractToDirectory(FileFullPath, path, ZipArchiveExtensions.Overwrite.Always);
                return;
            }

            progress.ItemCount = LayerCount;

            if (genericLayersExtract)
            {
                uint i = 0;
                if (Thumbnails is not null)
                {
                    foreach (var thumbnail in Thumbnails)
                    {
                        if (thumbnail is null)
                        {
                            continue;
                        }

                        thumbnail.Save(Path.Combine(path, $"Thumbnail{i}.png"));
                        i++;
                    }
                }

                if (LayerCount > 0 && DecodeType == FileDecodeType.Full)
                {
                    Parallel.ForEach(this, CoreSettings.ParallelOptions, layer =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        var byteArr = layer.CompressedBytes;
                        if (byteArr is null) return;
                        using var stream = new FileStream(Path.Combine(path, layer.Filename), FileMode.Create, FileAccess.Write);
                        stream.Write(byteArr, 0, byteArr.Length);
                        progress.LockAndIncrement();
                    });
                }
            }
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
                    if (layer.ExposureTime == ExposureTime) break; // First equal layer, transition ended

                    layer.ExposureTime = ExposureTime;
                }
            }

            if (transitionLayerCount == 0) return;

            float increment = Math.Max((BottomExposureTime - ExposureTime) / (transitionLayerCount + 1), 0f);
            if (increment <= 0) return;

            uint appliedLayers = 0;
            for (uint layerIndex = BottomLayerCount; appliedLayers < transitionLayerCount && layerIndex < LayerCount; layerIndex++)
            {
                appliedLayers++;
                this[layerIndex].ExposureTime = Math.Clamp(BottomExposureTime - (increment * appliedLayers), ExposureTime, BottomExposureTime);
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
        public object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
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
        /// Sets a property value attributed to <see cref="modifier"/>
        /// </summary>
        /// <param name="modifier">Modifier to use</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if set, otherwise false = <see cref="modifier"/> not found</returns>
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

        /// <summary>
        /// Attempt to set wait time before cure if supported, otherwise fallback to light-off delay
        /// </summary>
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
            GCode.RebuildGCode(this);
            RaisePropertyChanged(nameof(GCodeStr));
        }

        /// <summary>
        /// Saves current configuration on input file
        /// </summary>
        /// <param name="progress"></param>
        public void Save(OperationProgress progress = null)
        {
            SaveAs(null, progress);
        }

        /// <summary>
        /// Saves current configuration on a copy
        /// </summary>
        /// <param name="filePath">File path to save copy as, use null to overwrite active file (Same as <see cref="Save"/>)</param>
        /// <param name="progress"></param>
        public void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            if (RequireFullEncode)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileFullPath = filePath;
                }
                Encode(FileFullPath, progress);
                return;
            }

            if (!string.IsNullOrEmpty(filePath))
            {
                File.Copy(FileFullPath, filePath, true);
                FileFullPath = filePath;

            }

            progress ??= new OperationProgress();
            PartialSaveInternally(progress);
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
        public virtual FileFormat Convert(Type to, string fileFullPath, uint version = 0, OperationProgress progress = null)
        {
            if (!IsValid) return null;
            var found = AvailableFormats.Any(format => to == format.GetType());
            if (!found) return null;

            progress ??= new OperationProgress("Converting");
            
            var slicerFile = (FileFormat)Activator.CreateInstance(to);
            if (slicerFile is null) return null;
            slicerFile.FileFullPath = fileFullPath;

            if (!slicerFile.OnBeforeConvertFrom(this)) return null;
            if (!OnBeforeConvertTo(slicerFile)) return null;

            if (version > 0 && version != DefaultVersion)
            {
                slicerFile.Version = version;
            }

            slicerFile.SuppressRebuildPropertiesWork(() =>
            {
                slicerFile.LayerManager.Init(LayerManager.CloneLayers());
                slicerFile.AntiAliasing = ValidateAntiAliasingLevel();
                slicerFile.LayerCount = LayerManager.LayerCount;
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
        public FileFormat Convert(FileFormat to, string fileFullPath, uint version = 0, OperationProgress progress = null)
            => Convert(to.GetType(), fileFullPath, version, progress);

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
        public void SuppressRebuildPropertiesWork(Action action, bool callRebuildOnEnd = false, bool recalculateZPos = true, string property = null)
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
        public bool SuppressRebuildPropertiesWork(Func<bool> action, bool callRebuildOnEnd = false, bool recalculateZPos = true, string property = null)
        {
            bool result;
            try
            {
                SuppressRebuildProperties = true;
                result = action.Invoke();
                if (callRebuildOnEnd && result) LayerManager.RebuildLayersProperties(recalculateZPos, property);
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
        /// <param name="mm">Millimeters to convert</param>
        /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
        /// <returns>Pixels</returns>
        public uint MillimetersXToPixels(ushort mm, uint fallbackToPixels = 0)
        {
            var ppmm = Xppmm;
            if (ppmm <= 0) return fallbackToPixels;
            return (uint)(ppmm * mm);
        }

        /// <summary>
        /// Converts millimeters to pixels given the current resolution and display size
        /// </summary>
        /// <param name="mm">Millimeters to convert</param>
        /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
        /// <returns>Pixels</returns>
        public uint MillimetersYToPixels(ushort mm, uint fallbackToPixels = 0)
        {
            var ppmm = Yppmm;
            if (ppmm <= 0) return fallbackToPixels;
            return (uint)(ppmm * mm);
        }

        /// <summary>
        /// Converts millimeters to pixels given the current resolution and display size
        /// </summary>
        /// <param name="mm">Millimeters to convert</param>
        /// <param name="fallbackToPixels">Fallback to this value in pixels if no ratio is available to make the convertion</param>
        /// <returns>Pixels</returns>
        public uint MillimetersToPixels(ushort mm, uint fallbackToPixels = 0)
        {
            var ppmm = PpmmMax;
            if (ppmm <= 0) return fallbackToPixels;
            return (uint)(ppmm * mm);
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
    }
}
