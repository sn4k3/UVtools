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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Slicer <see cref="FileFormat"/> representation
    /// </summary>
    public abstract class FileFormat : IFileFormat, IDisposable
    {
        /// <summary>
        /// Gets the input file path loaded into this <see cref="FileFormat"/>
        /// </summary>
        public abstract string FileFullPath { get; protected set; }

        /// <summary>
        /// Gets the valid file extensions for this <see cref="FileFormat"/>
        /// </summary>
        public abstract FileExtension[] ValidFiles { get; }

        /// <summary>
        /// Gets the thumbnails count present in this file format
        /// </summary>
        public abstract byte ThumbnailsCount { get; }

        /// <summary>
        /// Gets the thumbnails for this <see cref="FileFormat"/>
        /// </summary>
        public abstract Image<Rgba32>[] Thumbnails { get; protected internal set; }

        protected FileFormat()
        {
            Thumbnails = new Image<Rgba32>[ThumbnailsCount];
        }

        public bool IsValid()
        {
            return !ReferenceEquals(FileFullPath, null);
        }

        public abstract void BeginEncode(string fileFullPath);

        public abstract void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex);

        public abstract void EndEncode();

        public abstract void Decode(string fileFullPath);
        public abstract Image<Gray8> GetLayerImage(uint layerIndex);

        protected void DecodeInternal(string fileFullPath)
        {
            Clear();
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;
        }
        public abstract float GetHeightFromLayer(uint layerNum);

        public abstract void Clear();

        protected void ClearInternal()
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
            string result = String.Empty;

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
