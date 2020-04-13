/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Slicer file format representation interface
    /// </summary>
    public interface IFileFormat
    {
        /// <summary>
        /// Gets the input file path loaded into this <see cref="FileFormat"/>
        /// </summary>
        string FileFullPath { get; set; }

        /// <summary>
        /// Gets the valid file extensions for this <see cref="FileFormat"/>
        /// </summary>
        FileExtension[] ValidFiles { get; }

        uint ResolutionX { get; }
        uint ResolutionY { get; }

        /// <summary>
        /// Gets the thumbnails count present in this file format
        /// </summary>
        byte ThumbnailsCount { get; }

        /// <summary>
        /// Gets the thumbnails for this <see cref="FileFormat"/>
        /// </summary>
        Image<Rgba32>[] Thumbnails { get; set; }

        /// <summary>
        /// Number of layers present in this file
        /// </summary>
        uint LayerCount { get; }
        float InitialExposureTime { get; }
        float LayerExposureTime { get; }
        float PrintTime { get; }
        float UsedMaterial { get; }
        float MaterialCost { get; }
        string MaterialName { get; }
        string MachineName { get; }

        /// <summary>
        /// Layer Height in mm
        /// </summary>
        float LayerHeight { get; }

        float TotalHeight { get; }

        /// <summary>
        /// Get all configuration objects with properties and values
        /// </summary>
        object[] Configs { get; }



        /// <summary>
        /// If this file is valid to read
        /// </summary>
        bool IsValid { get; }

        /// <summary>
        /// Begin encode to an output file
        /// </summary>
        /// <param name="fileFullPath">Output file</param>
        void BeginEncode(string fileFullPath);

        /// <summary>
        /// Insert a layer image to be encoded
        /// </summary>
        /// <param name="image"></param>
        /// <param name="layerIndex"></param>
        void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex);

        /// <summary>
        /// Finish the encoding procedure
        /// </summary>
        void EndEncode();

        /// <summary>
        /// Decode a slicer file
        /// </summary>
        /// <param name="fileFullPath"></param>
        void Decode(string fileFullPath);

        /// <summary>
        /// Extract contents to a folder
        /// </summary>
        /// <param name="path">Path to folder where content will be extracted</param>
        /// <param name="emptyFirst">Empty target folder first</param>
        void Extract(string path, bool emptyFirst = true);

        /// <summary>
        /// Gets a image from layer
        /// </summary>
        /// <param name="layerIndex">The layer index</param>
        /// <returns>Returns a image</returns>
        Image<Gray8> GetLayerImage(uint layerIndex);

        /// <summary>
        /// Get height in mm from layer height
        /// </summary>
        /// <param name="layerNum">The layer height</param>
        /// <returns>The height in mm</returns>
        float GetHeightFromLayer(uint layerNum);

        /// <summary>
        /// Clears all definitions and properties, it also dispose valid candidates 
        /// </summary>
        void Clear();

        /// <summary>
        /// Converts this file type to another file type
        /// </summary>
        /// <param name="to">Target file format</param>
        /// <param name="fileFullPath">Output path file</param>
        /// <returns>True if convert succeed, otherwise false</returns>
        bool Convert(Type to, string fileFullPath);

        /// <summary>
        /// Converts this file type to another file type
        /// </summary>
        /// <param name="to">Target file format</param>
        /// <param name="fileFullPath">Output path file</param>
        /// <returns>True if convert succeed, otherwise false</returns>
        bool Convert(FileFormat to, string fileFullPath);
    }
}
