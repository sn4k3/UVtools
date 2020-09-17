/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;
using Avalonia.Media;

namespace UVtools.WPF
{
    [Serializable]
    public sealed class UserSettings
    {
        #region Sub classes
        [Serializable]
        public sealed class GeneralUserSettings
        {
            public bool StartMaximized { get; set; } = true;
            public bool CheckForUpdatesOnStartup { get; set; } = true;
            public byte DefaultOpenFileExtensionIndex { get; set; }
            public string DefaultDirectoryOpenFile { get; set; }
            public string DefaultDirectorySaveFile { get; set; }
            public string DefaultDirectoryExtractFile { get; set; }
            public string DefaultDirectoryConvertFile { get; set; }
            public bool PromptOverwriteFileSave { get; set; } = true;
            public string FileSaveNamePrefix { get; set; }
            public string FileSaveNameSuffix { get; set; } = "_copy";
        }

        [Serializable]
        public sealed class LayerPreviewUserSettings
        {
            public Color TooltipOverlayBackgroundColor { get; set; } = new Color(210, 255, 255, 192);

            public bool TooltipOverlay { get; set; } = true;

            public Color VolumeBoundsOutlineColor { get; set; } = new Color(210, 0, 255, 0);
            public byte VolumeBoundsOutlineThickness { get; set; } = 3;
            public bool VolumeBoundsOutline { get; set; } = true;

            public Color LayerBoundsOutlineColor { get; set; } = new Color(210, 0, 255, 0);
            public byte LayerBoundsOutlineThickness { get; set; } = 3;
            public bool LayerBoundsOutline { get; set; } = true;

            public Color HollowOutlineColor { get; set; } = new Color(210, 255, 165, 0);
            public byte HollowOutlineLineThickness { get; set; } = 3;
            public bool HollowOutline { get; set; } = true;

            public Color PreviousLayerDifferenceColor { get; set; } = new Color(255, 255, 0, 255);
            public Color NextLayerDifferenceColor { get; set; } = new Color(255, 0, 255, 255);
            public Color BothLayerDifferenceColor { get; set; } = new Color(255, 255, 0, 0);
            public bool ShowLayerDifference { get; set; } = true;

            public Color IslandColor { get; set; } = new Color(255, 255,215, 0);
            public Color IslandHighlightColor { get; set; } = new Color(255, 255,255, 0);
            public Color ResinTrapColor { get; set; } = new Color(255, 244, 164, 96);
            public Color ResinTrapHighlightColor { get; set; } = new Color(255, 255, 165, 0);
            public Color TouchingBoundsColor { get; set; } = new Color(255, 255, 0, 0);
            public Color CrosshairColor { get; set; } = new Color(255, 255, 0, 0);

            public bool ZoomToFitPrintVolumeBounds { get; set; } = true;
            public byte ZoomLockLevelIndex { get; set; } = 7;
            public bool ZoomIssues { get; set; } = true;

            public bool CrosshairShowOnlyOnSelectedIssues { get; set; } = false;
            public byte CrosshairFadeLevelIndex { get; set; } = 5;
            public uint CrosshairLength { get; set; } = 20;
            public byte CrosshairMargin { get; set; } = 5;

            public bool AutoRotateLayerBestView { get; set; } = true;
            public bool LayerZoomToFitOnLoad { get; set; } = true;

        }

        [Serializable]
        public sealed class IssuesUserSettings
        {
            public bool ComputeIssuesOnLoad { get; set; } = false;
            public bool ComputeIssuesOnClickTab { get; set; } = true;
            public bool ComputeIslands { get; set; } = true;
            public bool ComputeResinTraps { get; set; } = true;
            public bool ComputeTouchingBounds { get; set; } = true;
            public bool ComputeEmptyLayers { get; set; } = true;

            public bool IslandAllowDiagonalBonds { get; set; } = false;
            public byte IslandBinaryThreshold { get; set; } = 0;
            public byte IslandRequiredAreaToProcessCheck { get; set; } = 1;
            public byte IslandRequiredPixelsToSupport { get; set; } = 10;
            public byte IslandRequiredPixelBrightnessToSupport { get; set; } = 150;


            public byte ResinTrapBinaryThreshold { get; set; } = 127;
            public byte ResinTrapRequiredAreaToProcessCheck { get; set; } = 17;
            public byte ResinTrapRequiredBlackPixelsToDrain { get; set; } = 10;
            public byte ResinTrapMaximumPixelBrightnessToDrain { get; set; } = 30;

        }

        [Serializable]
        public sealed class PixelEditorUserSettings
        {
            public Color AddPixelColor { get; set; } = new Color(255, 144, 238, 144);
            public Color AddPixelHighlightColor { get; set; } = new Color(255, 0, 255, 0);

            public Color RemovePixelColor { get; set; } = new Color(255, 219, 112, 147);
            public Color RemovePixelHighlightColor { get; set; } = new Color(255, 139, 0, 0);

            public Color SupportsColor { get; set; } = new Color(255, 0, 255, 255);
            public Color SupportsHighlightColor { get; set; } = new Color(255, 0, 139, 139);

            public Color DrainHolesColor { get; set; } = new Color(255, 142, 69, 133);
            public Color DrainHolesHighlightColor { get; set; } = new Color(255, 159, 0, 197);

            public bool PartialUpdateIslandsOnEditing { get; set; } = true;
            public bool CloseEditorOnApply { get; set; } = false;
        }

        [Serializable]
        public sealed class LayerRepairUserSettings
        {

            public byte ClosingIterations { get; set; } = 2;
            public byte OpeningIterations { get; set; } = 0;
            public byte RemoveIslandsBelowEqualPixels { get; set; } = 10;

            public bool RepairIslands { get; set; } = true;
            public bool RepairResinTraps { get; set; } = true;
            public bool RemoveEmptyLayers { get; set; } = true;
        }

        #endregion

            #region Singleton
            /// <summary>
            /// Default filepath for store <see cref="UserSettings"/>
            /// </summary>
            private const string FilePath = "Assets/usersettings.xml";


        private static UserSettings _instance;
        /// <summary>
        /// Instance of <see cref="UserSettings"/> (singleton)
        /// </summary>
        public static UserSettings Instance => _instance ??= new UserSettings();
        #endregion

        #region Properties

        public GeneralUserSettings General { get; set; } = new GeneralUserSettings();
        public LayerPreviewUserSettings LayerPreview { get; set; } = new LayerPreviewUserSettings();
        public IssuesUserSettings Issues { get; set; } = new IssuesUserSettings();
        public PixelEditorUserSettings PixelEditor { get; set; } = new PixelEditorUserSettings();
        public LayerRepairUserSettings LayerRepair { get; set; } = new LayerRepairUserSettings();

        /// <summary>
        /// Gets or sets the number of times this file has been saved
        /// </summary>
        public uint SavesCount { get; set; }

        /// <summary>
        /// Gets or sets the last time this file has been modified
        /// </summary>
        public DateTime ModifiedDateTime { get; set; }
        #endregion

        #region Constructor
        private UserSettings()
        { }
        #endregion

        #region Static Methods
        /// <summary>
        /// Reset settings to defaults
        /// </summary>
        /// <param name="save">True to save settings on file, otherwise false</param>
        public static void Reset(bool save = false)
        {
            _instance = new UserSettings();
            if(save) Save();
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(FilePath))
            {
                return;
            }

            var serializer = new XmlSerializer(typeof(UserSettings));
            using StreamReader myXmlReader = new StreamReader(FilePath);
            try
            {
                _instance = (UserSettings)serializer.Deserialize(myXmlReader);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }


        /// <summary>
        /// Save settings to file
        /// </summary>
        public static void Save()
        {
            Instance.SavesCount++;
            _instance.ModifiedDateTime = DateTime.Now;
            var serializer = new XmlSerializer(_instance.GetType());
            using StreamWriter myXmlWriter = new StreamWriter(FilePath);
            try
            {
                serializer.Serialize(myXmlWriter, _instance);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
        #endregion
    }
}
