/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;

namespace UVtools.WPF.Controls
{
    public static class Helpers
    {
        public static readonly List<FileDialogFilter> ImagesFileFilter = new List<FileDialogFilter>
        {
            new FileDialogFilter
            {
                Name = "Image Files",
                Extensions = new List<string>
                {
                    "png",
                    "bmp",
                    "jpeg",
                    "jpg",
                    "gif"
                }
            }
        };

        public static readonly List<FileDialogFilter> PngFileFilter = new List<FileDialogFilter>
        {
            new FileDialogFilter
            {
                Name = "Image Files",
                Extensions = new List<string>
                {
                    "png",
                }
            }
        };

        public static readonly List<FileDialogFilter> TxtFileFilter = new List<FileDialogFilter>
        {
            new FileDialogFilter
            {
                Name = "Text Files",
                Extensions = new List<string>
                {
                    "txt",
                }
            }
        };

        public static readonly List<FileDialogFilter> IniFileFilter = new List<FileDialogFilter>
        {
            new FileDialogFilter
            {
                Name = "Ini Files",
                Extensions = new List<string>
                {
                    "ini",
                }
            }
        };

        public static List<FileDialogFilter> ToAvaloniaFileFilter(List<KeyValuePair<string, List<string>>> data)
        {
            var result = new List<FileDialogFilter>(data.Capacity);
            result.AddRange(data.Select(kv => new FileDialogFilter {Name = kv.Key, Extensions = kv.Value}));
            return result;
        }
    }
}
