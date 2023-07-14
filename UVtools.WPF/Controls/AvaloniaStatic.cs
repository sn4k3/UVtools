/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Linq;

namespace UVtools.WPF.Controls;

public static class AvaloniaStatic
{
    public static readonly List<FilePickerFileType> ImagesFileFilter = new()
    {
        new("All images")
        {
            Patterns = new []
            {
                "*.png",
                "*.bmp",
                "*.jpeg",
                "*.jpg",
                "*.jp2",
                "*.tif",
                "*.tiff",
                "*.sr",
                "*.ras",
            },
            AppleUniformTypeIdentifiers = new []
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
            },
            MimeTypes = new []
            {
                "image/png",
                "image/bmp",
                "image/jpeg",
                "image/tiff",
                "image/x-cmu-raster",
            }
        },
    };

    public static readonly List<FilePickerFileType> ImagesFullFileFilter = new()
    {
        FilePickerFileTypes.ImagePng,
        FilePickerFileTypes.ImageJpg,
        new("BMP image")
        {
            Patterns = new[] { "*.bmp" },
            AppleUniformTypeIdentifiers = new[] { "com.microsoft.bmp" },
            MimeTypes = new[] { "image/bmp" }
        },
        /*new("TIFF image")
        {
            Patterns = new[] { "*.tif", "*.tiff" },
            AppleUniformTypeIdentifiers = new[] { "public.tiff" },
            MimeTypes = new[] { "image/tiff" }
        }*/
    };

    public static readonly List<FilePickerFileType> PngFileFilter = new()
    {
        FilePickerFileTypes.ImagePng
    };

    public static readonly List<FilePickerFileType> GifFileFilter = new()
    {
        new("GIF image")
        {
            Patterns = new[] { "*.gif" },
            AppleUniformTypeIdentifiers = new[] { "com.compuserve.gif" },
            MimeTypes = new[] { "image/gif" }
        },
        /*new("TIFF image")
        {
            Patterns = new[] { "*.tif", "*.tiff" },
            AppleUniformTypeIdentifiers = new[] { "public.tiff" },
            MimeTypes = new[] { "image/tiff" }
        }*/
    };

    public static readonly List<FilePickerFileType> TxtFileFilter = new()
    {
        FilePickerFileTypes.TextPlain
    };

    public static readonly List<FilePickerFileType> IniFileFilter = new()
    {
        new("Ini files")
        {
            Patterns = new []
            {
                "*.ini",
            },
            AppleUniformTypeIdentifiers = new []
            {
                "ini",
            },
            MimeTypes = new []
            {
                "text/plain"
            }
        }
    };

    public static readonly List<FilePickerFileType> IssuesFileFilter = new()
    {
        new("UVtools issues")
        {
            Patterns = new []
            {
                "*.uvtissues"
            },
            AppleUniformTypeIdentifiers = new []
            {
                "uvtissues"
            },
            MimeTypes = new []
            {
                "application/xml"
            }
        }
    };

    public static readonly List<FilePickerFileType> OperationSettingFileFilter = new()
    {
        new("UVtools operations")
        {
            Patterns = new []
            {
                "*.uvtop"
            },
            AppleUniformTypeIdentifiers = new []
            {
                "uvtop"
            },
            MimeTypes = new []
            {
                "application/xml"
            }
        }
    };

    public static readonly List<FilePickerFileType> SuggestionSettingFileFilter = new()
    {
        new("UVtools suggestions")
        {
            Patterns = new []
            {
                "*.uvtsu"
            },
            AppleUniformTypeIdentifiers = new []
            {
                "uvtsu"
            },
            MimeTypes = new []
            {
                "application/xml"
            }
        }
    };

    public static readonly List<FilePickerFileType> ScriptsFileFilter = new()
    {
        new("C# Scripts")
        {
            Patterns = new []
            {
                "*.cs",
                "*.csx",
            },
            AppleUniformTypeIdentifiers = new []
            {
                "cs",
                "csx",
            },
            MimeTypes = new []
            {
                "text/plain"
            }
        }
    };

    public static readonly List<FilePickerFileType> ZipFileFilter = new()
    {
        new("Zip archives")
        {
            Patterns = new []
            {
                "*.zip"
            },
            AppleUniformTypeIdentifiers = new []
            {
                "public.zip-archive"
            },
            MimeTypes = new []
            {
                "application/zip"
            }
        }
    };

    public static readonly List<FilePickerFileType> HtmlFileFilter = new()
    {
        new("HTML files")
        {
            Patterns = new []
            {
                "*.html"
            },
            AppleUniformTypeIdentifiers = new []
            {
                "public.html"
            },
            MimeTypes = new []
            {
                "text/html"
            }
        }
    };

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
        return new List<FilePickerFileType> { CreateFilePickerFileType(name, extensions) };
    }

    public static List<FilePickerFileType> ToAvaloniaFileFilter(List<KeyValuePair<string, List<string>>> data)
    {
        var result = new List<FilePickerFileType>(data.Capacity);
        result.AddRange(data.Select(kv => CreateFilePickerFileType(kv.Key, kv.Value.ToArray())));
        return result;
    }
}