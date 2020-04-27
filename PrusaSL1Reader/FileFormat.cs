/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Dynamic;
using System.IO;
using System.Linq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Slicer <see cref="FileFormat"/> representation
    /// </summary>
    public abstract class FileFormat : IFileFormat, IDisposable, IEquatable<FileFormat>
    {
        #region Enums

        /// <summary>
        /// Enumeration of file format types
        /// </summary>
        public enum FileFormatType : byte
        {
            Archive,
            Binary
        }
        #endregion

        #region Sub Classes
        /// <summary>
        /// Available Print Parameters to modify
        /// </summary>
        public class PrintParameterModifier
        {
            
            #region Instances
            public static PrintParameterModifier InitialLayerCount { get; } = new PrintParameterModifier("Initial Layer Count", @"Modify 'Initial Layer Count' value", null,0, ushort.MaxValue);
            public static PrintParameterModifier InitialExposureSeconds { get; } = new PrintParameterModifier("Initial Exposure Time", @"Modify 'Initial Exposure Time' seconds", "s", 0.1M, byte.MaxValue);
            public static PrintParameterModifier ExposureSeconds { get; } = new PrintParameterModifier("Exposure Time", @"Modify 'Exposure Time' seconds", "s", 0.1M, byte.MaxValue);
            
            public static PrintParameterModifier BottomLayerOffTime { get; } = new PrintParameterModifier("Bottom Layer Off Time", @"Modify 'Bottom Layer Off Time' seconds", "s");
            public static PrintParameterModifier LayerOffTime { get; } = new PrintParameterModifier("Layer Off Time", @"Modify 'Layer Off Time' seconds", "s");
            public static PrintParameterModifier BottomLiftHeight { get; } = new PrintParameterModifier("Bottom Lift Height", @"Modify 'Bottom Lift Height' millimeters between bottom layers", "mm");
            public static PrintParameterModifier BottomLiftSpeed { get; } = new PrintParameterModifier("Bottom Lift Speed", @"Modify 'Bottom Lift Speed' mm/min between bottom layers", "mm/min");
            public static PrintParameterModifier ZRetractHeight { get; } = new PrintParameterModifier("Z Retract Height", @"Modify 'Z Retract Height' millimeters between layers", "mm");
            public static PrintParameterModifier ZRetractSpeed { get; } = new PrintParameterModifier("Z Retract Speed", @"Modify 'Z Retract Speed' mm/min between layers", "mm/min", 10, 5000);
            public static PrintParameterModifier ZDetractSpeed { get; } = new PrintParameterModifier("Z Detract Speed", @"Modify 'Z Detract Speed' mm/min between layers", "mm/min", 10, 5000);

            public static PrintParameterModifier BottomLightPWM { get; } = new PrintParameterModifier("Bottom Light PWM", @"Modify 'Bottom Light PWM' value", null, 50, byte.MaxValue);
            public static PrintParameterModifier LightPWM { get; } = new PrintParameterModifier("Light PWM", @"Modify 'Light PWM' value", null, 50, byte.MaxValue);
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
            #endregion

            #region Constructor
            public PrintParameterModifier(string name, string description, string valueUnit = null, decimal minimum = 0, decimal maximum = 1000)
            {
                Name = name;
                Description = description;
                ValueUnit = valueUnit ?? string.Empty;
                Minimum = minimum;
                Maximum = maximum;
            }
            #endregion

            #region Overrides
            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(Description)}: {Description}, {nameof(ValueUnit)}: {ValueUnit}, {nameof(Minimum)}: {Minimum}, {nameof(Maximum)}: {Maximum}";
            }
            #endregion
        }
        #endregion

        #region Constants
        private const string ExtractConfigFileName = "Configuration";
        private const string ExtractConfigFileExtension = "ini";
        #endregion

        #region Static Methods
        /// <summary>
        /// Gets the available formats to process
        /// </summary>
        public static FileFormat[] AvaliableFormats { get; } =
        {
            new SL1File(),      // Prusa SL1
            new CbddlpFile(),   // cbddlp, photon
            new ZCodexFile(),   // zcodex
        };

        /// <summary>
        /// Gets all filters for open and save file dialogs
        /// </summary>
        public static string AllFileFilters =>
            AvaliableFormats.Aggregate(string.Empty,
                (current, fileFormat) => string.IsNullOrEmpty(current)
                    ? fileFormat.FileFilter
                    : $"{current}|" + fileFormat.FileFilter)
            +
            AvaliableFormats.Aggregate("|All slicer files|",
                (current, fileFormat) => current.EndsWith("|")
                    ? $"{current}{fileFormat.FileFilterExtensionsOnly}"
                    : $"{current};{fileFormat.FileFilterExtensionsOnly}");

        /// <summary>
        /// Gets the count of available file extensions
        /// </summary>
        public static byte FileExtensionsCount
        {
            get
            {
                return AvaliableFormats.Aggregate<FileFormat, byte>(0, (current, fileFormat) => (byte) (current + fileFormat.FileExtensions.Length));
            }
        }

        /// <summary>
        /// Find <see cref="FileFormat"/> by an extension
        /// </summary>
        /// <param name="extension">Extension name to find</param>
        /// <param name="isFilePath">True if <see cref="extension"/> is a file path rather than only a extension name</param>
        /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
        /// <returns><see cref="FileFormat"/> object or null if not found</returns>
        public static FileFormat FindByExtension(string extension, bool isFilePath = false, bool createNewInstance = false)
        {
            return (from fileFormat in AvaliableFormats where fileFormat.IsExtensionValid(extension, isFilePath) select createNewInstance ? (FileFormat) Activator.CreateInstance(fileFormat.GetType()) : fileFormat).FirstOrDefault();
        }
        #endregion

        #region Properties

        public abstract FileFormatType FileType { get; }

        public abstract FileExtension[] FileExtensions { get; }

        public abstract PrintParameterModifier[] PrintParameterModifiers { get; }

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

        public string FileFilterExtensionsOnly
        {
            get
            {
                var result = string.Empty;

                foreach (var fileExt in FileExtensions)
                {
                    if (!ReferenceEquals(result, string.Empty))
                    {
                        result += ';';
                    }
                    result += $"*.{fileExt.Extension}";
                }

                return result;
            }
        }

        public abstract string FileFullPath { get; set; }

        public abstract byte ThumbnailsCount { get; }

        public byte CreatedThumbnailsCount {
            get
            {
                if (ReferenceEquals(Thumbnails, null)) return 0;
                byte count = 0;

                foreach (var thumbnail in Thumbnails)
                {
                    if (ReferenceEquals(thumbnail, null)) continue;
                    count++;
                }

                return count;
            }
        }

        public abstract Image<Rgba32>[] Thumbnails { get; set; }

        public abstract uint ResolutionX { get; }

        public abstract uint ResolutionY { get; }

        public abstract float LayerHeight { get; }

        public float TotalHeight => (float)Math.Round(LayerCount * LayerHeight, 2);

        public abstract uint LayerCount { get; }
        
        public abstract ushort InitialLayerCount { get; }
        
        public abstract float InitialExposureTime { get; }

        public abstract float LayerExposureTime { get; }

        public abstract float ZRetractHeight { get; }

        public abstract float ZRetractSpeed { get; }

        public abstract float ZDetractSpeed { get; }

        public abstract float PrintTime { get; }
        
        public abstract float UsedMaterial { get; }

        public abstract float MaterialCost { get; }

        public abstract string MaterialName { get; }
        
        public abstract string MachineName { get; }

        public virtual string GCode { get; set; }

        public abstract object[] Configs { get; }

        public bool IsValid => !ReferenceEquals(FileFullPath, null);
        #endregion

        #region Constructor
        protected FileFormat()
        {
            Thumbnails = new Image<Rgba32>[ThumbnailsCount];
        }
        #endregion

        #region Overrides
        public override bool Equals(object obj)
        {
            return Equals(obj as FileFormat);
        }

        public bool Equals(FileFormat other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FileFullPath.Equals(other.FileFullPath);
        }

        public override int GetHashCode()
        {
            return (FileFullPath != null ? FileFullPath.GetHashCode() : 0);
        }

        public void Dispose()
        {
            Clear();
        }

        #endregion

        #region Methods
        public virtual void Clear()
        {
            FileFullPath = null;
            if (!ReferenceEquals(Thumbnails, null))
            {
                for (int i = 0; i < ThumbnailsCount; i++)
                {
                    Thumbnails[i]?.Dispose();
                }
            }
        }

        public void FileValidation(string fileFullPath)
        {
            if (ReferenceEquals(fileFullPath, null)) throw new ArgumentNullException(nameof(FileFullPath), "fullFilePath can't be null.");
            if (!File.Exists(fileFullPath)) throw new FileNotFoundException("The specified file does not exists.", fileFullPath);

            if (IsExtensionValid(fileFullPath, true))
            {
                return;
            }

            throw new FileLoadException($"The specified file is not valid.", fileFullPath);
        }

        public bool IsExtensionValid(string extension, bool isFilePath = false)
        {
            extension = isFilePath ? Path.GetExtension(extension)?.Remove(0, 1) : extension;
            return FileExtensions.Any(fileExtension => fileExtension.Extension.Equals(extension));
        }

        public virtual void BeginEncode(string fileFullPath)
        {
            if (File.Exists(fileFullPath))
            {
                File.Delete(fileFullPath);
            }
        }

        public abstract void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex);

        public abstract void EndEncode();

        public virtual void Decode(string fileFullPath)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
        }

        public virtual void Extract(string path, bool emptyFirst = true, bool genericConfigExtract = false, bool genericLayersExtract = false)
        {
            if (emptyFirst)
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
            }

            if (genericConfigExtract)
            {
                using (TextWriter tw = new StreamWriter(Path.Combine(path, $"{ExtractConfigFileName}.{ExtractConfigFileExtension}")))
                {
                    foreach (var config in Configs)
                    {
                        var type = config.GetType();
                        tw.WriteLine($"[{type.Name}]");
                        foreach (var property in type.GetProperties())
                        {
                            tw.WriteLine($"{property.Name} = {property.GetValue(config)}");
                        }
                        tw.WriteLine();
                    }
                    tw.Close();
                }
            }

            if (genericLayersExtract)
            {
                var encoder = new PngEncoder();
                uint i = 0;
                foreach (var thumbnail in Thumbnails)
                {
                    if (ReferenceEquals(thumbnail, null))
                    {
                        continue;
                    }
                    thumbnail.Save(Path.Combine(path, $"Thumbnail{i}.png"), encoder);
                    i++;
                }
                for (i = 0; i < LayerCount; i++)
                {
                    
                    GetLayerImage(i).Save(Path.Combine(path, $"Layer{i}.png"), encoder);
                }
            }
        }

        public abstract Image<Gray8> GetLayerImage(uint layerIndex);

        public virtual float GetHeightFromLayer(uint layerNum)
        {
            return (float)Math.Round(layerNum * LayerHeight, 2);
        }

        public virtual object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialLayerCount))
                return InitialLayerCount;
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialExposureSeconds))
                return InitialExposureTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
                return LayerExposureTime;

            if (ReferenceEquals(modifier, PrintParameterModifier.ZRetractHeight))
                return ZRetractHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.ZRetractSpeed))
                return ZRetractSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.ZDetractSpeed))
                return ZDetractSpeed;


            return null;
        }

        public virtual bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, object value)
        {
            return SetValueFromPrintParameterModifier(modifier, value.ToString());
        }

        public abstract bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value);

        public void Save()
        {
            SaveAs();
        }

        public abstract void SaveAs(string filePath = null);

        public abstract bool Convert(Type to, string fileFullPath);
        public bool Convert(FileFormat to, string fileFullPath)
        {
            return Convert(to.GetType(), fileFullPath);
        }
        #endregion
    }
}
