/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Represents a file extension for slicer file formats
    /// </summary>
    public sealed class FileExtension
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
        public FileExtension(string extension, string description)
        {
            Extension = extension;
            Description = description;
        }
        #endregion

        #region Overrides

        public override string ToString()
        {
            return $"{nameof(Extension)}: {Extension}, {nameof(Description)}: {Description}";
        }

        private bool Equals(FileExtension other)
        {
            return Extension == other.Extension;
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
    }
}
