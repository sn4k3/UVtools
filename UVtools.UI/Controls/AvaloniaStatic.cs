/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;

namespace UVtools.UI.Controls;

public static class AvaloniaStatic
{
    public static readonly List<FilePickerFileType> ImagesFileFilter =
    [
        new("All images")
        {
            Patterns =
            [
                "*.png",
                "*.bmp",
                "*.jpeg",
                "*.jpg",
                "*.jp2",
                "*.tif",
                "*.tiff",
                "*.sr",
                "*.ras"
            ],
            AppleUniformTypeIdentifiers =
            [
                "png",
                "bmp",
                "jpeg",
                "jpg",
                "jp2",
                "tif",
                "tiff",
                "sr",
                "ras"
            ],
            MimeTypes =
            [
                "image/png",
                "image/bmp",
                "image/jpeg",
                "image/tiff",
                "image/x-cmu-raster"
            ]
        }

    ];

    public static readonly List<FilePickerFileType> ImagesFullFileFilter =
    [
        FilePickerFileTypes.ImagePng,
        FilePickerFileTypes.ImageJpg,
        new("BMP image")
        {
            Patterns = ["*.bmp"],
            AppleUniformTypeIdentifiers = ["com.microsoft.bmp"],
            MimeTypes = ["image/bmp"]
        }
        /*new("TIFF image")
        {
            Patterns = new[] { "*.tif", "*.tiff" },
            AppleUniformTypeIdentifiers = new[] { "public.tiff" },
            MimeTypes = new[] { "image/tiff" }
        }*/

    ];

    public static readonly List<FilePickerFileType> PngFileFilter = [FilePickerFileTypes.ImagePng];

    public static readonly List<FilePickerFileType> GifFileFilter =
    [
        new("GIF image")
        {
            Patterns = ["*.gif"],
            AppleUniformTypeIdentifiers = ["com.compuserve.gif"],
            MimeTypes = ["image/gif"]
        }
        /*new("TIFF image")
        {
            Patterns = new[] { "*.tif", "*.tiff" },
            AppleUniformTypeIdentifiers = new[] { "public.tiff" },
            MimeTypes = new[] { "image/tiff" }
        }*/

    ];

    public static readonly List<FilePickerFileType> TxtFileFilter = [FilePickerFileTypes.TextPlain];

    public static readonly List<FilePickerFileType> IniFileFilter =
    [
        new("Ini files")
        {
            Patterns =
            [
                "*.ini"
            ],
            AppleUniformTypeIdentifiers =
            [
                "ini"
            ],
            MimeTypes =
            [
                "text/plain"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> IssuesFileFilter =
    [
        new("UVtools issues")
        {
            Patterns =
            [
                "*.uvtissues"
            ],
            AppleUniformTypeIdentifiers =
            [
                "uvtissues"
            ],
            MimeTypes =
            [
                "application/xml"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> OperationSettingFileFilter =
    [
        new("UVtools operations")
        {
            Patterns =
            [
                "*.uvtop"
            ],
            AppleUniformTypeIdentifiers =
            [
                "uvtop"
            ],
            MimeTypes =
            [
                "application/xml"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> SuggestionSettingFileFilter =
    [
        new("UVtools suggestions")
        {
            Patterns =
            [
                "*.uvtsu"
            ],
            AppleUniformTypeIdentifiers =
            [
                "uvtsu"
            ],
            MimeTypes =
            [
                "application/xml"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> ScriptsFileFilter =
    [
        new("C# Scripts")
        {
            Patterns =
            [
                "*.cs",
                "*.csx"
            ],
            AppleUniformTypeIdentifiers =
            [
                "cs",
                "csx"
            ],
            MimeTypes =
            [
                "text/plain"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> ZipFileFilter =
    [
        new("Zip archives")
        {
            Patterns =
            [
                "*.zip"
            ],
            AppleUniformTypeIdentifiers =
            [
                "public.zip-archive"
            ],
            MimeTypes =
            [
                "application/zip"
            ]
        }
    ];

    public static readonly List<FilePickerFileType> HtmlFileFilter =
    [
        new("HTML files")
        {
            Patterns =
            [
                "*.html"
            ],
            AppleUniformTypeIdentifiers =
            [
                "public.html"
            ],
            MimeTypes =
            [
                "text/html"
            ]
        }
    ];

    public static FilePickerFileType CreateFilePickerFileType(string name, params string[] extensions)
    {
        var pattern = new List<string>(extensions.Length);
        var apple = new List<string>(extensions.Length);
        foreach (var extension in extensions)
        {
            pattern.Add($"*.{extension}");
            apple.Add(extension);
        }
        return new FilePickerFileType(name)
        {
            Patterns = pattern,
            AppleUniformTypeIdentifiers = apple,
            MimeTypes = new List<string>
            {
                "application/octet-stream"
            }
        };
    }

    public static List<FilePickerFileType> CreateFilePickerFileTypes(string name, params string[] extensions)
    {
        return [CreateFilePickerFileType(name, extensions)];
    }

    public static List<FilePickerFileType> ToAvaloniaFileFilter(List<KeyValuePair<string, List<string>>> data)
    {
        var result = new List<FilePickerFileType>(data.Capacity);
        result.AddRange(data.Select(kv => CreateFilePickerFileType(kv.Key, kv.Value.ToArray())));
        return result;
    }
}