/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PrusaSL1Reader.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Size = System.Drawing.Size;

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

        /// <summary>
        /// Enumeration of file thumbnail size types
        /// </summary>
        public enum FileThumbnailSize : byte
        {
            Small = 0,
            Large
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
            public static PrintParameterModifier LiftHeight { get; } = new PrintParameterModifier("Lift Height", @"Modify 'Lift Height' millimeters between layers", "mm");
            public static PrintParameterModifier LiftSpeed { get; } = new PrintParameterModifier("Lift Speed", @"Modify 'Lift Speed' mm/min between layers", "mm/min", 10, 5000);
            public static PrintParameterModifier RetractSpeed { get; } = new PrintParameterModifier("Retract Speed", @"Modify 'Retract Speed' mm/min between layers", "mm/min", 10, 5000);

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
            new ChituboxFile(), // cbddlp, cbt, photon
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

        public string FileFullPath { get; set; }

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

        public abstract Size[] ThumbnailsOriginalSize { get; }

        public Image<Rgba32>[] Thumbnails { get; set; }
        
        public byte[][] Layers { get; set; }

        public abstract uint ResolutionX { get; }

        public abstract uint ResolutionY { get; }

        public abstract float LayerHeight { get; }

        public float TotalHeight => (float)Math.Round(LayerCount * LayerHeight, 2);

        public abstract uint LayerCount { get; }
        
        public abstract ushort InitialLayerCount { get; }
        
        public abstract float InitialExposureTime { get; }

        public abstract float LayerExposureTime { get; }

        public abstract float LiftHeight { get; }

        public abstract float RetractSpeed { get; }

        public abstract float LiftSpeed { get; }

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
            Layers = null;
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

        public Image<Rgba32> GetThumbnail(uint maxHeight = 400)
        {
            for (int i = 0; i < ThumbnailsCount; i++)
            {
                if(ReferenceEquals(Thumbnails[i], null)) continue;
                if (Thumbnails[i].Height <= maxHeight) return Thumbnails[i];
            }

            return null;
        }

        public void SetThumbnails(Image<Rgba32>[] images)
        {
            for (var i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = images[Math.Min(i, images.Length - 1)].Clone();
            }
        }

        public void SetThumbnails(Image<Rgba32> image)
        {
            for (var i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = image.Clone();
            }
        }

        public byte[] CompressLayer(Stream input)
        {
            return CompressLayer(input.ToArray());
        }

        public byte[] CompressLayer(byte[] input)
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

        public byte[] DecompressLayer(byte[] input)
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

        public virtual void Encode(string fileFullPath)
        {
            if (File.Exists(fileFullPath))
            {
                File.Delete(fileFullPath);
            }

            for (var i = 0; i < Thumbnails.Length; i++)
            {
                if (ReferenceEquals(Thumbnails[i], null)) continue;

                Thumbnails[i].Mutate(x => x.Resize(ThumbnailsOriginalSize[i].Width, ThumbnailsOriginalSize[i].Height));
            }
        }

        /*public virtual void BeginEncode(string fileFullPath)
        {
        }
               

        public abstract void InsertLayerImageEncode(Image<L8> image, uint layerIndex);

        public abstract void EndEncode();*/

        public virtual void Decode(string fileFullPath)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
        }

        public virtual void Extract(string path, bool emptyFirst = true, bool genericConfigExtract = true, bool genericLayersExtract = true)
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

            if (FileType == FileFormatType.Archive)
            {
                ZipFile.ExtractToDirectory(FileFullPath, path);
                return;
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
                uint i = 0;
                foreach (var thumbnail in Thumbnails)
                {
                    if (ReferenceEquals(thumbnail, null))
                    {
                        continue;
                    }
                    thumbnail.Save(Path.Combine(path, $"Thumbnail{i}.png"), Helpers.PngEncoder);
                    i++;
                }

                Parallel.For(0, LayerCount, layerIndex => {
                        var byteArr = GetLayer((uint) layerIndex);
                        using (FileStream stream = File.Create(Path.Combine(path, $"Layer{layerIndex}.png"), byteArr.Length))
                        {
                            stream.Write(byteArr, 0, byteArr.Length);
                            stream.Close();
                        }
                    });
                /*for (i = 0; i < LayerCount; i++)
                {
                    var byteArr = GetLayer(i);
                    using (FileStream stream = File.Create(Path.Combine(path, $"Layer{i}.png"), byteArr.Length))
                    {
                        stream.Write(byteArr, 0, byteArr.Length);
                        stream.Close();
                    }
                }*/
            }
        }

        public virtual byte[] GetLayer(uint layerIndex)
        {
            return layerIndex >= LayerCount ? null : DecompressLayer(Layers[layerIndex]);
        }

        public virtual Image<L8> GetLayerImage(uint layerIndex)
        {
            return layerIndex >= LayerCount ? null : Image.Load<L8>(GetLayer(layerIndex));
        }
        
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

            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
                return LiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
                return LiftSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
                return RetractSpeed;
            


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
