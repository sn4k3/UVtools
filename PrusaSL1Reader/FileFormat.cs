/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Slicer <see cref="FileFormat"/> representation
    /// </summary>
    public abstract class FileFormat : IFileFormat, IDisposable
    {
        public static FileFormat[] AvaliableFormats { get; } =
        {
            new SL1File(),
            new CbddlpFile(),
        };

        public static string AllFileFilters => AvaliableFormats.Aggregate(string.Empty, (current, fileFormat) => string.IsNullOrEmpty(current) ? fileFormat.GetFileFilter() : $"{current}|" + fileFormat.GetFileFilter());

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
        /// Gets the input file path loaded into this <see cref="FileFormat"/>
        /// </summary>
        public abstract string FileFullPath { get; set; }

        /// <summary>
        /// Gets the valid file extensions for this <see cref="FileFormat"/>
        /// </summary>
        public abstract FileExtension[] ValidFiles { get; }

        public abstract uint ResolutionX { get; }
        public abstract uint ResolutionY { get; }

        /// <summary>
        /// Gets the thumbnails count present in this file format
        /// </summary>
        public abstract byte ThumbnailsCount { get; }

        /// <summary>
        /// Gets the number of created thumbnails
        /// </summary>
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

        /// <summary>
        /// Gets the thumbnails for this <see cref="FileFormat"/>
        /// </summary>
        public abstract Image<Rgba32>[] Thumbnails { get; set; }

        public abstract uint LayerCount { get; }

        public abstract float InitialExposureTime { get; }

        public abstract float LayerExposureTime { get; }
        public abstract float PrintTime { get; }
        public abstract float UsedMaterial { get; }

        public abstract float MaterialCost { get; }

        public abstract string MaterialName { get; }
        public abstract string MachineName { get; }
        public abstract float LayerHeight { get; }
        public float TotalHeight => (float)Math.Round(LayerCount * LayerHeight, 2);

        protected FileFormat()
        {
            Thumbnails = new Image<Rgba32>[ThumbnailsCount];
        }

        public abstract object[] Configs { get; }
        public bool IsValid => !ReferenceEquals(FileFullPath, null);


        public abstract void BeginEncode(string fileFullPath);

        public abstract void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex);

        public abstract void EndEncode();

        public virtual void Decode(string fileFullPath)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
        }

        public virtual void Extract(string path, bool emptyFirst = true)
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
        }

        public abstract Image<Gray8> GetLayerImage(uint layerIndex);

        public abstract float GetHeightFromLayer(uint layerNum);

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

        public abstract bool Convert(Type to, string fileFullPath);
        public bool Convert(FileFormat to, string fileFullPath)
        {
            return Convert(to.GetType(), fileFullPath);
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
            return ValidFiles.Any(fileExtension => fileExtension.Extension.Equals(extension));
        }

        public string GetFileFilter()
        {
            string result = string.Empty;

            foreach (var fileExt in ValidFiles)
            {
                if (!ReferenceEquals(result, string.Empty))
                {
                    result += '|';
                }
                result += fileExt.Filter;
            }

            return result;
        }

        public void Dispose()
        {
            Clear();
        }
    }
}
