/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text;
using Emgu.CV;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
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
        /// Gets the available <see cref="FileFormat.PrintParameterModifier"/> per layer
        /// </summary>
        FileFormat.PrintParameterModifier[] PrintParameterPerLayerModifiers { get; }

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
        Mat[] Thumbnails { get; set; }

        /// <summary>
        /// Gets the cached layers into compressed bytes
        /// </summary>
        LayerManager LayerManager { get; set; }

        Size Resolution { get; }

        /// <summary>
        /// Gets the image width resolution
        /// </summary>
        uint ResolutionX { get; set; }

        /// <summary>
        /// Gets the image height resolution
        /// </summary>
        uint ResolutionY { get; set; }

        /// <summary>
        /// Gets the size of display in millimeters
        /// </summary>
        SizeF Display { get; set; }

        /// <summary>
        /// Gets or sets the display width in millimeters
        /// </summary>
        float DisplayWidth { get; set; }

        /// <summary>
        /// Gets or sets the display height in millimeters
        /// </summary>
        float DisplayHeight { get; set; }

        bool HaveAntiAliasing { get; }

        /// <summary>
        /// Gets the AntiAliasing level
        /// </summary>
        byte AntiAliasing { get; }

        /// <summary>
        /// Gets Layer Height in mm
        /// </summary>
        float LayerHeight { get; set; }

        /// <summary>
        /// Gets Total Height in mm
        /// </summary>
        float TotalHeight { get; }

        /// <summary>
        /// Gets the last layer index
        /// </summary>
        uint LastLayerIndex { get; }

        #region Universal Properties

        /// <summary>
        /// Gets if this format support per layer override settings
        /// </summary>
        bool SupportPerLayerSettings { get; }

        /// <summary>
        /// Gets the number of layers present in this file
        /// </summary>
        uint LayerCount { get; set; }

        /// <summary>
        /// Gets or sets the number of initial layer count
        /// </summary>
        ushort BottomLayerCount { get; set; }

        /// <summary>
        /// Gets the number of normal layer count
        /// </summary>
        uint NormalLayerCount { get; }

        /// <summary>
        /// Gets or sets the initial exposure time for <see cref="BottomLayerCount"/> in seconds
        /// </summary>
        float BottomExposureTime { get; set; }

        /// <summary>
        /// Gets or sets the normal layer exposure time in seconds
        /// </summary>
        float ExposureTime { get; set; }

        /// <summary>
        /// Gets or sets the bottom layer off time in seconds
        /// </summary>
        float BottomLayerOffTime { get; set; }

        /// <summary>
        /// Gets or sets the layer off time in seconds
        /// </summary>
        float LayerOffTime { get; set; }

        /// <summary>
        /// Gets or sets the bottom lift height in mm
        /// </summary>
        float BottomLiftHeight { get; set; }

        /// <summary>
        /// Gets or sets the lift height in mm
        /// </summary>
        float LiftHeight { get; set; }

        /// <summary>
        /// Gets or sets the bottom lift speed in mm/min
        /// </summary>
        float BottomLiftSpeed { get; set; }

        /// <summary>
        /// Gets or sets the speed in mm/min
        /// </summary>
        float LiftSpeed { get; set; }

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        float RetractSpeed { get; set; }

        /// <summary>
        /// Gets or sets the bottom pwm value from 0 to 255
        /// </summary>
        byte BottomLightPWM { get; set; }

        /// <summary>
        /// Gets or sets the pwm value from 0 to 255
        /// </summary>
        byte LightPWM { get; set; }
        #endregion

        /// <summary>
        /// Gets the estimate print time in seconds
        /// </summary>
        float PrintTime { get; }

        /// <summary>
        /// Gets the estimate print time in seconds, if print doesn't support it it will be calculated
        /// </summary>
        float PrintTimeOrComputed { get; }

        /// <summary>
        /// Gets the estimate print time in hours
        /// </summary>
        float PrintTimeHours { get; }

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
        string GCodeStr { get; }

        /// <summary>
        /// Gets if this file have available gcode
        /// </summary>
        bool HaveGCode { get; }

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
        Mat GetThumbnail(uint maxHeight = 400);

        /// <summary>
        /// Gets a thumbnail by the largest or smallest
        /// </summary>
        /// <param name="largest">True to get the largest, otherwise false</param>
        /// <returns></returns>
        Mat GetThumbnail(bool largest);

        /// <summary>
        /// Sets thumbnails from a list of thumbnails and clone them
        /// </summary>
        /// <param name="images"></param>
        void SetThumbnails(Mat[] images);

        /// <summary>
        /// Sets all thumbnails the same image
        /// </summary>
        /// <param name="images">Image to set</param>
        void SetThumbnails(Mat images);

        /// <summary>
        /// Sets a thumbnail from a disk file
        /// </summary>
        /// <param name="index">Thumbnail index</param>
        /// <param name="filePath"></param>
        void SetThumbnail(int index, string filePath);

        /// <summary>
        /// Encode to an output file
        /// </summary>
        /// <param name="fileFullPath">Output file</param>
        /// <param name="progress"></param>
        void Encode(string fileFullPath, OperationProgress progress = null);
        void AfterEncode();
        
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
        /// <param name="progress"></param>
        void Decode(string fileFullPath, OperationProgress progress = null);

        /// <summary>
        /// Extract contents to a folder
        /// </summary>
        /// <param name="path">Path to folder where content will be extracted</param>
        /// <param name="genericConfigExtract"></param>
        /// <param name="genericLayersExtract"></param>
        /// <param name="progress"></param>
        void Extract(string path, bool genericConfigExtract = true, bool genericLayersExtract = true,
            OperationProgress progress = null);

        /// <summary>
        /// Get height in mm from layer height
        /// </summary>
        /// <param name="layerIndex"></param>
        /// <param name="realHeight"></param>
        /// <returns>The height in mm</returns>
        float GetHeightFromLayer(uint layerIndex, bool realHeight = true);

        /// <summary>
        /// Gets the value for initial layer or normal layers based on layer index
        /// </summary>
        /// <typeparam name="T">Type of value</typeparam>
        /// <param name="layerIndex">Layer index</param>
        /// <param name="initialLayerValue">Initial value</param>
        /// <param name="normalLayerValue">Normal value</param>
        /// <returns></returns>
        T GetInitialLayerValueOrNormal<T>(uint layerIndex, T initialLayerValue, T normalLayerValue);

        void RefreshPrintParametersModifiersValues();
        void RefreshPrintParametersPerLayerModifiersValues(uint layerIndex);

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
        bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, decimal value);

        byte SetValuesFromPrintParametersModifiers();

        void EditPrintParameters(OperationEditParameters operation);

        /// <summary>
        /// Rebuilds GCode based on current settings
        /// </summary>
        void RebuildGCode();

        /// <summary>
        /// Saves current configuration on input file
        /// </summary>
        /// <param name="progress"></param>
        void Save(OperationProgress progress = null);

        /// <summary>
        /// Saves current configuration on a copy
        /// </summary>
        /// <param name="filePath">File path to save copy as, use null to overwrite active file (Same as <see cref="Save"/>)</param>
        /// <param name="progress"></param>
        void SaveAs(string filePath = null, OperationProgress progress = null);

        /// <summary>
        /// Converts this file type to another file type
        /// </summary>
        /// <param name="to">Target file format</param>
        /// <param name="fileFullPath">Output path file</param>
        /// <param name="progress"></param>
        /// <returns>True if convert succeed, otherwise false</returns>
        bool Convert(Type to, string fileFullPath, OperationProgress progress = null);

        /// <summary>
        /// Converts this file type to another file type
        /// </summary>
        /// <param name="to">Target file format</param>
        /// <param name="fileFullPath">Output path file</param>
        /// <param name="progress"></param>
        /// <returns>True if convert succeed, otherwise false</returns>
        bool Convert(FileFormat to, string fileFullPath, OperationProgress progress = null);

        /// <summary>
        /// Validate AntiAlias Level
        /// </summary>
        byte ValidateAntiAliasingLevel();

        #endregion
    }
}
