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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    /// <summary>
    /// Slicer <see cref="FileFormat"/> representation
    /// </summary>
    public abstract class FileFormat : IFileFormat, IDisposable, IEquatable<FileFormat>, IEnumerable<Layer>
    {
        public const string TemporaryFileAppend = ".tmp";
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
            public static PrintParameterModifier BottomLayerCount { get; } = new PrintParameterModifier("Bottom layer count", null, "layers",0, ushort.MaxValue, 0);
            public static PrintParameterModifier BottomExposureSeconds { get; } = new PrintParameterModifier("Bottom exposure time", null, "s", 0.1M, 1000, 2);
            public static PrintParameterModifier ExposureSeconds { get; } = new PrintParameterModifier("Exposure time", null, "s", 0.1M, 1000, 2);
            
            public static PrintParameterModifier BottomLayerOffTime { get; } = new PrintParameterModifier("Bottom layer off seconds", null, "s");
            public static PrintParameterModifier LayerOffTime { get; } = new PrintParameterModifier("Layer off seconds", null, "s");
            public static PrintParameterModifier BottomLiftHeight { get; } = new PrintParameterModifier("Bottom lift height", @"Modify 'Bottom lift height' millimeters between bottom layers", "mm", 1);
            public static PrintParameterModifier LiftHeight { get; } = new PrintParameterModifier("Lift height", @"Modify 'Lift height' millimeters between layers", "mm", 1);
            public static PrintParameterModifier BottomLiftSpeed { get; } = new PrintParameterModifier("Bottom lift Speed", @"Modify 'Bottom lift Speed' mm/min between bottom layers", "mm/min", 10);
            public static PrintParameterModifier LiftSpeed { get; } = new PrintParameterModifier("Lift speed", @"Modify 'Lift speed' mm/min between layers", "mm/min", 10, 5000, 2);
            public static PrintParameterModifier RetractSpeed { get; } = new PrintParameterModifier("Retract speed", @"Modify 'Retract speed' mm/min between layer", "mm/min", 10, 5000, 2);

            public static PrintParameterModifier BottomLightPWM { get; } = new PrintParameterModifier("Bottom light PWM", @"Modify 'Bottom light PWM' value", null, 1, byte.MaxValue, 0);
            public static PrintParameterModifier LightPWM { get; } = new PrintParameterModifier("Light PWM", @"Modify 'Light PWM' value", null, 1, byte.MaxValue, 0);

            public static PrintParameterModifier[] Parameters = {
                BottomLayerCount,
                BottomExposureSeconds,
                ExposureSeconds,

                BottomLayerOffTime,
                LayerOffTime,
                BottomLiftHeight,
                BottomLiftSpeed,
                LiftHeight,
                LiftSpeed,
                RetractSpeed,

                BottomLightPWM,
                LightPWM
            };
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
            public PrintParameterModifier(string name, string description = null, string valueUnit = null, decimal minimum = 0, decimal maximum = 1000, byte decimalPlates = 2)
            {
                Name = name;
                Description = description ?? $"Modify '{name}'";
                ValueUnit = valueUnit ?? string.Empty;
                Minimum = minimum;
                Maximum = maximum;
                DecimalPlates = decimalPlates;
            }
            #endregion

            #region Overrides
            public override string ToString()
            {
                return $"{nameof(Name)}: {Name}, {nameof(Description)}: {Description}, {nameof(ValueUnit)}: {ValueUnit}, {nameof(Minimum)}: {Minimum}, {nameof(Maximum)}: {Maximum}, {nameof(DecimalPlates)}: {DecimalPlates}, {nameof(OldValue)}: {OldValue}, {nameof(NewValue)}: {NewValue}, {nameof(HasChanged)}: {HasChanged}";
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
            new ChituboxZipFile(),      // Zip
            new ChituboxFile(), // cbddlp, cbt, photon
            new PHZFile(), // phz
            new PWSFile(),   // PSW
            new ZCodexFile(),   // zcodex
            new CWSFile(),   // CWS
            //new MakerbaseFile(),   // MKS
            new LGSFile(),   // LGS, LGS30
            new UVJFile(),   // UVJ
            new ImageFile(),   // images
        };

        public static string AllSlicerFiles => AvaliableFormats.Aggregate("All slicer files|",
            (current, fileFormat) => current.EndsWith("|")
                ? $"{current}{fileFormat.FileFilterExtensionsOnly}"
                : $"{current}; {fileFormat.FileFilterExtensionsOnly}");

        /// <summary>
        /// Gets all filters for open and save file dialogs
        /// </summary>
        public static string AllFileFilters =>
            AllSlicerFiles
            +
            AvaliableFormats.Aggregate(string.Empty,
                (current, fileFormat) => $"{current}|" + fileFormat.FileFilter);

        public static List<KeyValuePair<string, List<string>>> AllFileFiltersAvalonia
        {
            get
            {
                var result = new List<KeyValuePair<string, List<string>>>
                {
                    new KeyValuePair<string, List<string>>("All slicer files", new List<string>())
                };
                
                for (int i = 0; i < AvaliableFormats.Length; i++)
                {
                    foreach (var fileExtension in AvaliableFormats[i].FileExtensions)
                    {
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

        /// <summary>
        /// Find <see cref="FileFormat"/> by an type
        /// </summary>
        /// <param name="type">Type to find</param>
        /// <param name="createNewInstance">True to create a new instance of found file format, otherwise will return a pre created one which should be used for read-only purpose</param>
        /// <returns><see cref="FileFormat"/> object or null if not found</returns>
        public static FileFormat FindByType(Type type, bool createNewInstance = false)
        {
            return (from t in AvaliableFormats where type == t.GetType() select createNewInstance ? (FileFormat) Activator.CreateInstance(type) : t).FirstOrDefault();
        }
        #endregion

        #region Properties

        public abstract FileFormatType FileType { get; }

        public abstract FileExtension[] FileExtensions { get; }
        public abstract Type[] ConvertToFormats { get; }

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

        public List<KeyValuePair<string, List<string>>> FileFilterAvalonia 
            => FileExtensions.Select(fileExt => new KeyValuePair<string, List<string>>(fileExt.Description, new List<string> {fileExt.Extension})).ToList();

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

        public string FileFullPath { get; set; }

        public abstract byte ThumbnailsCount { get; }

        public byte CreatedThumbnailsCount {
            get
            {
                if (Thumbnails is null) return 0;
                byte count = 0;

                foreach (var thumbnail in Thumbnails)
                {
                    if (thumbnail is null) continue;
                    count++;
                }

                return count;
            }
        }

        public abstract Size[] ThumbnailsOriginalSize { get; }

        public Mat[] Thumbnails { get; set; }
        public LayerManager LayerManager { get; set; }

        private bool _haveModifiedLayers;

        /// <summary>
        /// Gets or sets if modifications require a full encode to save
        /// </summary>
        public bool RequireFullEncode
        {
            get => _haveModifiedLayers || LayerManager.IsModified;
            set => _haveModifiedLayers = value;
        } // => LayerManager.IsModified;

        public Size Resolution => new Size((int) ResolutionX, (int) ResolutionY);

        public abstract uint ResolutionX { get; set; }

        public abstract uint ResolutionY { get; set; }
        public bool HaveAntiAliasing => AntiAliasing > 1;
        public abstract byte AntiAliasing { get; }

        public abstract float LayerHeight { get; set; }

        public float TotalHeight => LayerCount == 0 ? 0 : this[LayerCount - 1].PositionZ; //(float)Math.Round(LayerCount * LayerHeight, 2);

        public uint LastLayerIndex => LayerCount - 1;

        public virtual uint LayerCount
        {
            get => LayerManager?.Count ?? 0;
            set => throw new NotImplementedException();
        }

        public virtual ushort BottomLayerCount { get; set; }
        public virtual float BottomExposureTime { get; set; }
        public virtual float ExposureTime { get; set; }
        public virtual float BottomLayerOffTime { get; set; }
        public virtual float LayerOffTime { get; set; }
        public virtual float BottomLiftHeight { get; set; }
        public virtual float LiftHeight { get; set; }
        public virtual float BottomLiftSpeed { get; set; }
        public virtual float LiftSpeed { get; set; }
        public virtual float RetractSpeed { get; set; }
        public virtual byte BottomLightPWM { get; set; } = 255;
        public virtual byte LightPWM { get; set; } = 255;


        public abstract float PrintTime { get; }

        public float PrintTimeHours => (float) Math.Round(PrintTime / 3600, 2);

        public abstract float UsedMaterial { get; }

        public abstract float MaterialCost { get; }

        public abstract string MaterialName { get; }
        
        public abstract string MachineName { get; }

        public StringBuilder GCode { get; set; }

        public string GCodeStr => GCode?.ToString();

        public bool HaveGCode => !(GCode is null);
        public abstract object[] Configs { get; }

        public bool IsValid => !ReferenceEquals(FileFullPath, null);
        #endregion

        #region Constructor
        protected FileFormat()
        {
            Thumbnails = new Mat[ThumbnailsCount];
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
        #endregion

        #region Numerators
        public IEnumerator<Layer> GetEnumerator()
        {
            return ((IEnumerable<Layer>)LayerManager.Layers).GetEnumerator();
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
            LayerManager = null;
            GCode = null;

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
            return FileExtensions.Any(fileExtension => fileExtension.Extension.Equals(extension, StringComparison.InvariantCultureIgnoreCase));
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

        public Mat GetThumbnail(uint maxHeight = 400)
        {
            for (int i = 0; i < ThumbnailsCount; i++)
            {
                if(ReferenceEquals(Thumbnails[i], null)) continue;
                if (Thumbnails[i].Height <= maxHeight) return Thumbnails[i];
            }

            return null;
        }

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
                        return Thumbnails[0].Size.GetArea() >= Thumbnails[1].Size.GetArea() ? Thumbnails[0] : Thumbnails[1];
                    }
                    else
                    {
                        return Thumbnails[0].Size.GetArea() <= Thumbnails[1].Size.GetArea() ? Thumbnails[0] : Thumbnails[1];
                    }
            }
        }

        public void SetThumbnails(Mat[] images)
        {
            for (var i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = images[Math.Min(i, images.Length - 1)].Clone();
            }
        }

        public void SetThumbnails(Mat image)
        {
            for (var i = 0; i < ThumbnailsCount; i++)
            {
                Thumbnails[i] = image.Clone();
            }
        }

        public void SetThumbnail(int index, string filePath)
        {
            Thumbnails[index] = CvInvoke.Imread(filePath, ImreadModes.AnyColor);
            if (Thumbnails[index].Size != ThumbnailsOriginalSize[index])
            {
                CvInvoke.Resize(Thumbnails[index], Thumbnails[index], ThumbnailsOriginalSize[index]);
            }
        }

        public virtual void Encode(string fileFullPath, OperationProgress progress = null)
        {
            FileFullPath = fileFullPath;

            if (File.Exists(fileFullPath))
            {
                File.Delete(fileFullPath);
            }

            for (var i = 0; i < Thumbnails.Length; i++)
            {
                if (ReferenceEquals(Thumbnails[i], null)) continue;
                if(Thumbnails[i].Size == ThumbnailsOriginalSize[i]) continue;
                CvInvoke.Resize(Thumbnails[i], Thumbnails[i], new Size(ThumbnailsOriginalSize[i].Width, ThumbnailsOriginalSize[i].Height));
            }

            if(ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        }

        public void AfterEncode()
        {
            LayerManager.Desmodify();
            RequireFullEncode = false;
        }

        public virtual void Decode(string fileFullPath, OperationProgress progress = null)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
        }

        public virtual void Extract(string path, bool genericConfigExtract = true, bool genericLayersExtract = true,
            OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
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
            

            if (FileType == FileFormatType.Archive)
            {
                
                progress.CanCancel = false;
                //ZipFile.ExtractToDirectory(FileFullPath, path);
                ZipArchiveExtensions.ImprovedExtractToDirectory(FileFullPath, path, ZipArchiveExtensions.Overwrite.Always);
                return;
            }

            progress.ItemCount = LayerCount;

            if (genericConfigExtract)
            {
                if (!ReferenceEquals(Configs, null))
                {
                    using (TextWriter tw = new StreamWriter(Path.Combine(path, $"{ExtractConfigFileName}.{ExtractConfigFileExtension}"), false))
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
            }

            if (genericLayersExtract)
            {
                uint i = 0;
                if (!ReferenceEquals(Thumbnails, null))
                {
                    foreach (var thumbnail in Thumbnails)
                    {
                        if (ReferenceEquals(thumbnail, null))
                        {
                            continue;
                        }

                        thumbnail.Save(Path.Combine(path, $"Thumbnail{i}.png"));
                        i++;
                    }
                }

                if (LayerCount > 0)
                {
                    Parallel.ForEach(this, (layer) =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        var byteArr = layer.CompressedBytes;
                        using (FileStream stream = File.Create(Path.Combine(path, $"Layer{layer.Index.ToString().PadLeft(LayerCount.ToString().Length, '0')}.png"),
                            byteArr.Length))
                        {
                            stream.Write(byteArr, 0, byteArr.Length);
                            stream.Close();
                            lock (progress.Mutex)
                            {
                                progress++;
                            }
                        }
                    });
                }

                /* Parallel.For(0, LayerCount, layerIndex => {
                         var byteArr = this[layerIndex].RawData;
                         using (FileStream stream = File.Create(Path.Combine(path, $"Layer{layerIndex}.png"), byteArr.Length))
                         {
                             stream.Write(byteArr, 0, byteArr.Length);
                             stream.Close();
                         }
                     });*/
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

        public float GetHeightFromLayer(uint layerIndex, bool realHeight = true)
        {
            return (float)Math.Round((layerIndex+(realHeight ? 1 : 0)) * LayerHeight, 2);
        }

        public T GetInitialLayerValueOrNormal<T>(uint layerIndex, T initialLayerValue, T normalLayerValue)
        {
            return layerIndex < BottomLayerCount ? initialLayerValue : normalLayerValue;
        }

        public void RefreshPrintParametersModifiersValues()
        {
            if (PrintParameterModifiers is null) return;
            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLayerCount))
            {
                PrintParameterModifier.BottomLayerCount.OldValue = BottomLayerCount;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomExposureSeconds))
            {
                PrintParameterModifier.BottomExposureSeconds.OldValue = (decimal) BottomExposureTime;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.ExposureSeconds))
            {
                PrintParameterModifier.ExposureSeconds.OldValue = (decimal)ExposureTime;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLayerOffTime))
            {
                PrintParameterModifier.BottomLayerOffTime.OldValue = (decimal)BottomLayerOffTime;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.LayerOffTime))
            {
                PrintParameterModifier.LayerOffTime.OldValue = (decimal)LayerOffTime;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftHeight))
            {
                PrintParameterModifier.BottomLiftHeight.OldValue = (decimal)BottomLiftHeight;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftHeight))
            {
                PrintParameterModifier.LiftHeight.OldValue = (decimal)LiftHeight;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLiftSpeed))
            {
                PrintParameterModifier.BottomLiftSpeed.OldValue = (decimal)BottomLiftSpeed;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.LiftSpeed))
            {
                PrintParameterModifier.LiftSpeed.OldValue = (decimal)LiftSpeed;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.RetractSpeed))
            {
                PrintParameterModifier.RetractSpeed.OldValue = (decimal)RetractSpeed;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.BottomLightPWM))
            {
                PrintParameterModifier.BottomLightPWM.OldValue = BottomLightPWM;
            }

            if (PrintParameterModifiers.Contains(PrintParameterModifier.LightPWM))
            {
                PrintParameterModifier.LightPWM.OldValue = LightPWM;
            }
        }

        public object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerCount))
                return BottomLayerCount;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomExposureSeconds))
                return BottomExposureTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
                return ExposureTime;

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime))
                return BottomLayerOffTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime))
                return LayerOffTime;

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
                return BottomLiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
                return LiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
                return BottomLiftSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
                return LiftSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
                return RetractSpeed;

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
                return BottomLightPWM;
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
                return LightPWM;

            return null;
        }

        public bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, decimal value)
        {
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerCount))
            {
                BottomLayerCount = (ushort)value;
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomExposureSeconds))
            {
                BottomExposureTime = (float) value;
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
            {
                ExposureTime = (float) value;
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime))
            {
                BottomLayerOffTime = (float) value;
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime))
            {
                LayerOffTime = (float) value;
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
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            {
                RetractSpeed = (float) value;
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

        public virtual byte SetValuesFromPrintParametersModifiers()
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

            if (changed == 0) return changed;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                this[layerIndex].ExposureTime = GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime);
            }

            RebuildGCode();

            return changed;
        }

        public virtual void RebuildGCode()
        { }

        public void Save(OperationProgress progress = null)
        {
            SaveAs(null, progress);
        }

        public abstract void SaveAs(string filePath = null, OperationProgress progress = null);

        public abstract bool Convert(Type to, string fileFullPath, OperationProgress progress = null);
        public bool Convert(FileFormat to, string fileFullPath, OperationProgress progress = null)
        {
            return Convert(to.GetType(), fileFullPath, progress);
        }

        public byte ValidateAntiAliasingLevel()
        {
            if (AntiAliasing < 2) return 1;
            if(AntiAliasing % 2 != 0) throw new ArgumentException("AntiAliasing must be multiples of 2, otherwise use 0 or 1 to disable it", nameof(AntiAliasing));
            return AntiAliasing;
        }

        #endregion
    }
}
