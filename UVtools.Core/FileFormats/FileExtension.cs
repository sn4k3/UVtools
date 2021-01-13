/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;

namespace UVtools.Core.FileFormats
{
    /// <summary>
    /// Represents a file extension for slicer file formats
    /// </summary>
    public sealed class FileExtension : IEquatable<FileExtension>, IEquatable<string>
    {
        #region Properties
        /// <summary>
        /// Gets the extension name without the dot (.)
        /// </summary>
        public string Extension { get; }
        
        /// <summary>
        /// Gets the extension description
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Gets a tag object
        /// </summary>
        public object Tag { get; }

        /// <summary>
        /// Gets the file filter for open and save dialogs
        /// </summary>
        public string Filter => $@"{Description} (*.{Extension})|*.{Extension}";
        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="extension">The extension name without the dot (.)</param>
        /// <param name="description">The extension description</param>
        /// <param name="tag">Tag object</param>
        public FileExtension(string extension, string description, object tag = null)
        {
            Extension = extension;
            Description = description;
            Tag = tag;
        }
        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{Description} ({Extension})";
        }

        public bool Equals(FileExtension other)
        {
            return Extension.Equals(other.Extension, StringComparison.InvariantCultureIgnoreCase);
        }

        public bool Equals(string other)
        {
            return Extension.Equals(other, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is FileExtension other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Extension != null ? Extension.GetHashCode() : 0);
        }

        private sealed class ExtensionEqualityComparer : IEqualityComparer<FileExtension>
        {
            public bool Equals(FileExtension x, FileExtension y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Extension == y.Extension;
            }

            public int GetHashCode(FileExtension obj)
            {
                return (obj.Extension != null ? obj.Extension.GetHashCode() : 0);
            }
        }

        public static IEqualityComparer<FileExtension> ExtensionComparer { get; } = new ExtensionEqualityComparer();
        #endregion

        #region Methods

        public FileFormat GetFileFormat() =>
            FileFormat.FindByExtension(Extension);
    
        public static FileExtension Find(string extension)=>
            FileFormat.FindExtension(extension);
        #endregion
    }
}
