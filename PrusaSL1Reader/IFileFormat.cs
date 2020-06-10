/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.IO;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// Slicer file format representation interface
    /// </summary>
    public interface IFileFormat
    {
        #region Properties
        /// <summary>
        /// Gets the file format type
        /// </summary>
        FileFormat.FileFormatType FileType { get; }

        /// <summary>
        /// Gets the valid file extensions for this <see cref="FileFormat"/>
        /// </summary>
        FileExtension[] FileExtensions { get; }

        /// <summary>
        /// Gets the implemented file formats able to convert to
        /// </summary>
        Type[] ConvertToFormats { get; }

        /// <summary>
        /// Gets the available <see cref="FileFormat.PrintParameterModifier"/>
        /// </summary>
        FileFormat.PrintParameterModifier[] PrintParameterModifiers { get; }

        /// <summary>
        /// Gets the file filter for open and save dialogs
        /// </summary>
        string FileFilter { get; }

        /// <summary>
        /// Gets all valid file extensions in "*.extension1;*.extension2" format
        /// </summary>

        string FileFilterExtensionsOnly { get; }

        /// <summary>
        /// Gets the input file path loaded into this <see cref="FileFormat"/>
        /// </summary>
        string FileFullPath { get; set; }

        /// <summary>
        /// Gets the thumbnails count present in this file format
        /// </summary>
        byte ThumbnailsCount { get; }

        /// <summary>
        /// Gets the number of created thumbnails
        /// </summary>
        byte CreatedThumbnailsCount { get; }

        /// <summary>
        /// Gets the original thumbnail sizes
        /// </summary>
        System.Drawing.Size[] ThumbnailsOriginalSize { get; }
        
        /// <summary>
        /// Gets the thumbnails for this <see cref="FileFormat"/>
        /// </summary>
        Image<Rgba32>[] Thumbnails { get; set; }

        /// <summary>
        /// Gets the cached layers into compressed bytes
        /// </summary>
        LayerManager LayerManager { get; set; }

        /// <summary>
        /// Gets the image width resolution
        /// </summary>
        uint ResolutionX { get; }

        /// <summary>
        /// Gets the image height resolution
        /// </summary>
        uint ResolutionY { get; }

        /// <summary>
        /// Gets Layer Height in mm
        /// </summary>
        float LayerHeight { get; }

        /// <summary>
        /// Gets Total Height in mm
        /// </summary>
        float TotalHeight { get; }

        /// <summary>
        /// Gets the number of layers present in this file
        /// </summary>
        uint LayerCount { get; }

        /// <summary>
        /// Gets the number of initial layer count
        /// </summary>
        /// </summary>
        ushort InitialLayerCount { get; }

        /// <summary>
        /// Gets the initial exposure time for <see cref="InitialLayerCount"/>
        /// </summary>
        float InitialExposureTime { get; }

        /// <summary>
        /// Gets the normal layer exposure time
        /// </summary>
        float LayerExposureTime { get; }

        /// <summary>
        /// Gets the speed in mm/min for the detracts
        /// </summary>
        float LiftSpeed { get; }

        /// <summary>
        /// Gets the height in mm to retract between layers
        /// </summary>
        float LiftHeight { get; }

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        float RetractSpeed { get; }

        /// <summary>
        /// Gets the estimate print time in seconds
        /// </summary>
        float PrintTime { get; }

        /// <summary>
        /// Gets the estimate used material in ml
        /// </summary>
        float UsedMaterial { get; }

        /// <summary>
        /// Gets the estimate material cost
        /// </summary>
        float MaterialCost { get; }

        /// <summary>
        /// Gets the material name
        /// </summary>
        string MaterialName { get; }

        /// <summary>
        /// Gets the machine name
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// Gets the GCode, returns null if not supported
        /// </summary>
        StringBuilder GCode { get; set; }

        /// <summary>
        /// Get all configuration objects with properties and values
        /// </summary>
        object[] Configs { get; }

        /// <summary>
        /// Gets if this file is valid to read
        /// </summary>
        bool IsValid { get; }

        #endregion

        #region Methods
        /// <summary>
        /// Clears all definitions and properties, it also dispose valid candidates 
        /// </summary>
        void Clear();

        /// <summary>
        /// Validate if a file is a valid <see cref="FileFormat"/>
        /// </summary>
        /// <param name="fileFullPath">Full file path</param>
        void FileValidation(string fileFullPath);

        /// <summary>
        /// Checks if a extension is valid under the <see cref="FileFormat"/>
        /// </summary>
        /// <param name="extension">Extension to check</param>
        /// <param name="isFilePath">True if <see cref="extension"/> is a full file path, otherwise false for extension only</param>
        /// <returns>True if valid, otherwise false</returns>
        bool IsExtensionValid(string extension, bool isFilePath = false);

        /// <summary>
        /// Gets all valid file extensions in a specified format
        /// </summary>

        string GetFileExtensions(string prepend = ".", string separator = ", ");

        /// <summary>
        /// Gets a thumbnail by it height or lower
        /// </summary>
        /// <param name="maxHeight">Max height allowed</param>
        /// <returns></returns>
        Image<Rgba32> GetThumbnail(uint maxHeight = 400);

        /// <summary>
        /// Sets thumbnails from a list of thumbnails and clone them
        /// </summary>
        /// <param name="images"></param>
        void SetThumbnails(Image<Rgba32>[] images);

        /// <summary>
        /// Sets all thumbnails the same image
        /// </summary>
        /// <param name="images">Image to set</param>
        void SetThumbnails(Image<Rgba32> images);

        /// <summary>
        /// Encode to an output file
        /// </summary>
        /// <param name="fileFullPath">Output file</param>
        void Encode(string fileFullPath);

        /*
        /// <summary>
        /// Begin encode to an output file
        /// </summary>
        /// <param name="fileFullPath">Output file</param>
        //void BeginEncode(string fileFullPath);

        /// <summary>
        /// Insert a layer image to be encoded
        /// </summary>
        /// <param name="image"></param>
        /// <param name="layerIndex"></param>
        //void InsertLayerImageEncode(Image<L8> image, uint layerIndex);

        /// <summary>
        /// Finish the encoding procedure
        /// </summary>
        //void EndEncode();*/

        /// <summary>
        /// Decode a slicer file
        /// </summary>
        /// <param name="fileFullPath"></param>
        void Decode(string fileFullPath);

        /// <summary>
        /// Extract contents to a folder
        /// </summary>
        /// <param name="path">Path to folder where content will be extracted</param>
        /// <param name="genericConfigExtract"></param>
        /// <param name="genericLayersExtract"></param>
        void Extract(string path, bool genericConfigExtract = true, bool genericLayersExtract = true);

        /// <summary>
        /// Get height in mm from layer height
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <returns>The height in mm</returns>
        float GetHeightFromLayer(uint layerIndex);

        /// <summary>
        /// Gets the value attributed to <see cref="FileFormat.PrintParameterModifier"/>
        /// </summary>
        /// <param name="modifier">Modifier to use</param>
        /// <returns>A value</returns>
        object GetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier);

        /// <summary>
        /// Sets a property value attributed to <see cref="modifier"/>
        /// </summary>
        /// <param name="modifier">Modifier to use</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if set, otherwise false = <see cref="modifier"/> not found</returns>
        bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, object value);

        /// <summary>
        /// Sets a property value attributed to <see cref="modifier"/>
        /// </summary>
        /// <param name="modifier">Modifier to use</param>
        /// <param name="value">Value to set</param>
        /// <returns>True if set, otherwise false = <see cref="modifier"/> not found</returns>
        bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, string value);

        /// <summary>
        /// Saves current configuration on input file
        /// </summary>
        void Save();

        /// <summary>
        /// Saves current configuration on a copy
        /// </summary>
        /// <param name="filePath">File path to save copy as, use null to overwrite active file (Same as <see cref="Save"/>)</param>
        void SaveAs(string filePath = null);

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

        /// <summary>
        /// Resizes layer images in x and y factor, starting at 1 = 100%
        /// </summary>
        /// <param name="startLayerIndex">Layer index to start</param>
        /// <param name="endLayerIndex">Layer index to end</param>
        /// <param name="x">X factor</param>
        /// <param name="y">Y factor</param>
        void Resize(uint startLayerIndex, uint endLayerIndex, float x, float y);

        #endregion
    }
}
