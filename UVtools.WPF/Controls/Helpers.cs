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
        public static readonly List<FileDialogFilter> ImagesFileFilter = new()
        {
            new()
            {
                Name = "Image Files",
                Extensions = new List<string>
                {
                    "png",
                    "bmp",
                    "jpeg",
                    "jpg",
                    "jp2",
                    "tif",
                    "tiff",
                    "sr",
                    "ras",
                }
            },
        };

        public static readonly List<FileDialogFilter> ImagesFullFileFilter = new()
        {
            new()
            {
                Name = "PNG Files",
                Extensions = new List<string>
                {
                    "png"
                }
            },
            new()
            {
                Name = "JPG Files",
                Extensions = new List<string>
                {
                    "jpg",
                    "jpeg"
                }
            },
            new()
            {
                Name = "BMP Files",
                Extensions = new List<string>
                {
                    "bmp",
                }
            },
            new()
            {
                Name = "TIF Files",
                Extensions = new List<string>
                {
                    "tif",
                    "tiff",
                }
            },
        };

        public static readonly List<FileDialogFilter> PngFileFilter = new()
        {
            new()
            {
                Name = "Image Files",
                Extensions = new List<string>
                {
                    "png",
                }
            }
        };

        public static readonly List<FileDialogFilter> TxtFileFilter = new()
        {
            new()
            {
                Name = "Text Files",
                Extensions = new List<string>
                {
                    "txt",
                }
            }
        };

        public static readonly List<FileDialogFilter> IniFileFilter = new()
        {
            new()
            {
                Name = "Ini Files",
                Extensions = new List<string>
                {
                    "ini",
                }
            }
        };

        public static readonly List<FileDialogFilter> OperationSettingFileFilter = new()
        {
            new()
            {
                Name = "UVtools operation settings",
                Extensions = new List<string>
                {
                    "uvtop",
                }
            }
        };

        public static readonly List<FileDialogFilter> ScriptsFileFilter = new()
        {
            new()
            {
                Name = "Script Files",
                Extensions = new List<string>
                {
                    "csx",
                    "cs",
                }
            }
        };

        public static List<FileDialogFilter> ToAvaloniaFileFilter(List<KeyValuePair<string, List<string>>> data)
        {
            var result = new List<FileDialogFilter>(data.Capacity);
            result.AddRange(data.Select(kv => new FileDialogFilter {Name = kv.Key, Extensions = kv.Value}));
            return result;
        }

        public static List<FileDialogFilter> ToAvaloniaFilter(string name, string extension)
        {
            return new(1)
            {
                new() {Name = name, Extensions = new List<string>(1) {extension}}
            };
        }
    }
}
