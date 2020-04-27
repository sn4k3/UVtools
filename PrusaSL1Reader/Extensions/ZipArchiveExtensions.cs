/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;
using System.IO.Compression;

namespace PrusaSL1Reader.Extensions
{
    public static class ZipArchiveExtensions
    {
        /// <summary>
        /// Create a file into archive and write content to it
        /// </summary>
        /// <param name="input"><see cref="ZipArchive"/></param>
        /// <param name="filename">Filename to create</param>
        /// <param name="content">Content to write</param>
        /// <returns>Created <see cref="ZipArchiveEntry"/></returns>
        public static ZipArchiveEntry PutFileContent(this ZipArchive input, string filename, string content, bool deleteFirst = true)
        {
            if(deleteFirst) input.GetEntry(filename)?.Delete();

            var entry = input.CreateEntry(filename);
            if (ReferenceEquals(content, null)) return entry;
            using (TextWriter tw = new StreamWriter(entry.Open()))
            {
                tw.Write(content);
                tw.Close();
            }

            return entry;
        }

        public static ZipArchiveEntry PutFileContent(this ZipArchive input, string filename, Stream content, bool deleteFirst = true)
        {
            if (deleteFirst) input.GetEntry(filename)?.Delete();
            var entry = input.CreateEntry(filename);
            if (ReferenceEquals(content, null)) return entry;
            using (StreamWriter tw = new StreamWriter(entry.Open()))
            {
                tw.Write(content);
                tw.Close();
            }

            return entry;
        }


    }
}
