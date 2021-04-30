/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.IO;

namespace UVtools.Core.Extensions
{
    public static class PathExtensions
    {
        public static string GetFileNameStripAllExtensions(string path)
        {
            path = Path.GetFileName(path);
            if(string.IsNullOrEmpty(path)) return string.Empty;
            var splitPath = path.Split('.', 2, StringSplitOptions.TrimEntries);
            return splitPath.Length == 0 ? string.Empty : splitPath[0];
        }

        public static string GetFileNameStripExtensions(string path, List<string> extensions, out string strippedExtension)
        {
            strippedExtension = string.Empty;
            path = Path.GetFileName(path);
            if (string.IsNullOrEmpty(path)) return string.Empty;
            foreach (var extension in extensions)
            {
                var dotExtension = $".{extension}";
                if (!path.EndsWith(dotExtension)) continue;
                strippedExtension = extension;
                return path.Remove(path.Length - dotExtension.Length);
            }

            return path;
        }
    }
}
