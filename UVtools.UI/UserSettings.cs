/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Avalonia.Themes.Fluent;
using CommunityToolkit.Mvvm.ComponentModel;
using SukiUI.Enums;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Network;
using UVtools.Core.Objects;
using ZLinq;
using Color = UVtools.UI.Structures.Color;

namespace UVtools.UI;


public partial class UserSettings : ObservableObject
{
    #region Constants
    public const ushort SETTINGS_VERSION = 8;
    #endregion

    #region Sub classes

    #region General

    public partial class GeneralUserSettings : ObservableObject
    {
        public const byte LockedFilesMaxOpenCounter = 10;

        [ObservableProperty]
        public partial App.ApplicationTheme Theme { get; set; } = App.ApplicationTheme.FluentSystem;

        [ObservableProperty]
        public partial DensityStyle ThemeDensity { get; set; } = DensityStyle.Normal;

        [ObservableProperty]
        public partial string ThemeColor { get; set; } = "UVtools";

        [ObservableProperty]
        public partial bool BackgroundAnimations { get; set; }

        [ObservableProperty]
        public partial bool BackgroundTransitions { get; set; } = true;

        [ObservableProperty]
        public partial SukiBackgroundStyle BackgroundStyle { get; set; } = SukiBackgroundStyle.GradientSoft;

        [ObservableProperty]
        public partial Rectangle LastWindowBounds { get; set; } = new(40, 40, 1024, 600);

        [ObservableProperty]
        public partial bool StartMaximized { get; set; } = true;

        [ObservableProperty]
        public partial bool RestoreWindowLastPosition { get; set; }

        [ObservableProperty]
        public partial bool RestoreWindowLastSize { get; set; }

        [ObservableProperty]
        public partial bool CheckForUpdatesOnStartup { get; set; } = true;

        [ObservableProperty]
        public partial bool LoadDemoFileOnStartup { get; set; } = true;

        [ObservableProperty]
        public partial bool LoadLastRecentFileOnStartup { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of available RAM in GB to be able to run, otherwise will pause/cancel or exit.
        /// </summary>
        public decimal AvailableRamLimit
        {
            get;
            set => SetProperty(ref field, Math.Max(0, value));
        }

        [ObservableProperty]
        public partial RamLimitAction AvailableRamOnHitLimitAction { get; set; }

        [ObservableProperty]
        public partial bool AvailableRamOnHitLimitKillIfUnableToAction { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent tasks enabled by a ParallelOptions instance.
        /// </summary>
        public int MaxDegreeOfParallelism
        {
            get;
            set => SetProperty(ref field, Math.Min(value, Environment.ProcessorCount));
        } = -2;

        [ObservableProperty]
        public partial LayerCompressionCodec LayerCompressionCodec { get; set; } = CoreSettings.DefaultLayerCompressionCodec;

        [ObservableProperty]
        public partial LayerCompressionLevel LayerCompressionLevel { get; set; } = CoreSettings.DefaultLayerCompressionLevel;

        [ObservableProperty]
        public partial float AverageResin1000MlBottleCost { get; set; } = CoreSettings.AverageResin1000MlBottleCost;

        [ObservableProperty]
        public partial bool WindowsCanResize { get; set; }

        [ObservableProperty]
        public partial bool WindowsTakeIntoAccountScreenScaling { get; set; } = true;

        [ObservableProperty]
        public partial float WindowsMaxWidthScreenRatio { get; set; } = 0.9f;

        [ObservableProperty]
        public partial float WindowsMaxHeightScreenRatio { get; set; } = 0.9f;

        [ObservableProperty]
        public partial byte DefaultOpenFileExtensionIndex { get; set; }

        [ObservableProperty]
        public partial string? DefaultDirectoryOpenFile { get; set; }

        [ObservableProperty]
        public partial string? DefaultDirectorySaveFile { get; set; }

        [ObservableProperty]
        public partial string? DefaultDirectoryExtractFile { get; set; }

        [ObservableProperty]
        public partial string? DefaultDirectoryConvertFile { get; set; }

        [ObservableProperty]
        public partial string? DefaultDirectoryScripts { get; set; }

        [ObservableProperty]
        public partial bool FileSavePromptOverwrite { get; set; } = true;

        [ObservableProperty]
        public partial bool FileSaveUpdateNameWithNewInformation { get; set; } = true;

        [ObservableProperty]
        public partial string? FileSaveAsDefaultName { get; set; } = "{0}_{PrintTimeString}_{MaterialMillilitersInteger}ml_copy";

        [ObservableProperty]
        public partial string? FileSaveAsDefaultNameCleanUpRegex { get; set; } = @"_?[0-9]+h[0-9]+m([0-9]+s)?|_?(([0-9]*[.])?[0-9]+)ml|_copy([0-9]*)?";

        [ObservableProperty]
        public partial bool NotificationBeep { get; set; } = true;

        [ObservableProperty]
        public partial byte NotificationBeepCount { get; set; } = 1;

        [ObservableProperty]
        public partial ushort NotificationBeepActivateAboveTime { get; set; } = 20;

        [ObservableProperty]
        public partial ushort NotificationBeepFrequency { get; set; } = 600;

        [ObservableProperty]
        public partial ushort NotificationBeepDuration { get; set; } = 300;

        [ObservableProperty]
        public partial int NotificationBeepRepeatFrequencyOffset { get; set; } = 50;

        [ObservableProperty]
        public partial ushort NotificationBeepRepeatDelay { get; set; }

        [ObservableProperty]
        public partial bool SendToPromptForRemovableDeviceEject { get; set; } = true;

        [ObservableProperty]
        public partial RangeObservableCollection<MappedDevice> SendToCustomLocations { get; set; } = [];

        [ObservableProperty]
        public partial RangeObservableCollection<MappedProcess> SendToProcess { get; set; } = [];

        [ObservableProperty]
        public partial ushort LockedFilesOpenCounter { get; set; }

        public GeneralUserSettings() { }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(Theme))
            {
                App.ChangeBaseTheme(Theme);
            }
            else if (e.PropertyName == nameof(ThemeColor))
            {
                App.ChangeColorTheme(ThemeColor);
            }
        }

        public GeneralUserSettings Clone()
        {
            return this.CloneByXmlSerialization();
            /*var clone = MemberwiseClone() as GeneralUserSettings;
            _sendToCustomLocations ??= new();
            clone.SendToCustomLocations = new RangeObservableCollection<MappedDevice>();
            foreach (var location in _sendToCustomLocations)
            {
                clone.SendToCustomLocations.Add(location.Clone());
            }
            return clone;*/
        }
    }
    #endregion

    #region Layer Preview

    public sealed partial class LayerPreviewUserSettings : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TooltipOverlayBackgroundBrush))]
        public partial Color TooltipOverlayBackgroundColor { get; set; } = new(210, 226, 223, 215);

        [XmlIgnore]
        public Avalonia.Media.Color TooltipOverlayBackgroundBrush
        {
            get => TooltipOverlayBackgroundColor.ToAvalonia();
            set => TooltipOverlayBackgroundColor = new Color(value);
        }

        [ObservableProperty]
        public partial bool TooltipOverlay { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(VolumeBoundsOutlineBrush))]
        public partial Color VolumeBoundsOutlineColor { get; set; } = new(255, 0, 255, 0);

        [XmlIgnore]
        public Avalonia.Media.Color VolumeBoundsOutlineBrush
        {
            get => VolumeBoundsOutlineColor.ToAvalonia();
            set => VolumeBoundsOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte VolumeBoundsOutlineThickness { get; set; } = 3;

        [ObservableProperty]
        public partial bool VolumeBoundsOutline { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LayerBoundsOutlineBrush))]
        public partial Color LayerBoundsOutlineColor { get; set; } = new(255, 45, 150, 45);

        [XmlIgnore]
        public Avalonia.Media.Color LayerBoundsOutlineBrush
        {
            get => LayerBoundsOutlineColor.ToAvalonia();
            set => LayerBoundsOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte LayerBoundsOutlineThickness { get; set; } = 3;

        [ObservableProperty]
        public partial bool LayerBoundsOutline { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ContourBoundsOutlineBrush))]
        public partial Color ContourBoundsOutlineColor { get; set; } = new(255, 50, 100, 50);

        [XmlIgnore]
        public Avalonia.Media.Color ContourBoundsOutlineBrush
        {
            get => ContourBoundsOutlineColor.ToAvalonia();
            set => ContourBoundsOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte ContourBoundsOutlineThickness { get; set; } = 2;

        [ObservableProperty]
        public partial bool ContourBoundsOutline { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(EnclosingCirclesOutlineBrush))]
        public partial Color EnclosingCirclesOutlineColor { get; set; } = new(255, 127, 0, 0);

        [XmlIgnore]
        public Avalonia.Media.Color EnclosingCirclesOutlineBrush
        {
            get => EnclosingCirclesOutlineColor.ToAvalonia();
            set => EnclosingCirclesOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte EnclosingCirclesOutlineThickness { get; set; } = 2;

        [ObservableProperty]
        public partial bool EnclosingCirclesOutline { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HollowOutlineBrush))]
        public partial Color HollowOutlineColor { get; set; } = new(255, 255, 165, 0);

        [XmlIgnore]
        public Avalonia.Media.Color HollowOutlineBrush
        {
            get => HollowOutlineColor.ToAvalonia();
            set => HollowOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial sbyte HollowOutlineLineThickness { get; set; } = 5;

        [ObservableProperty]
        public partial bool HollowOutline { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CentroidOutlineBrush))]
        public partial Color CentroidOutlineColor { get; set; } = new(255, 255, 0, 0);

        [XmlIgnore]
        public Avalonia.Media.Color CentroidOutlineBrush
        {
            get => CentroidOutlineColor.ToAvalonia();
            set => CentroidOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte CentroidOutlineDiameter { get; set; } = 8;

        [ObservableProperty]
        public partial bool CentroidOutlineHollow { get; set; }

        [ObservableProperty]
        public partial bool CentroidOutline { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TriangulateOutlineBrush))]
        public partial Color TriangulateOutlineColor { get; set; } = new(255, 0, 0, 255);

        [XmlIgnore]
        public Avalonia.Media.Color TriangulateOutlineBrush
        {
            get => TriangulateOutlineColor.ToAvalonia();
            set => TriangulateOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial byte TriangulateOutlineLineThickness { get; set; } = 2;

        [ObservableProperty]
        public partial bool TriangulateOutlineShowCount { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(MaskOutlineBrush))]
        public partial Color MaskOutlineColor { get; set; } = new(255, 42, 157, 244);

        [XmlIgnore]
        public Avalonia.Media.Color MaskOutlineBrush
        {
            get => MaskOutlineColor.ToAvalonia();
            set => MaskOutlineColor = new Color(value);
        }

        [ObservableProperty]
        public partial sbyte MaskOutlineLineThickness { get; set; } = 10;

        [ObservableProperty]
        public partial bool MaskClearROIAfterSet { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PreviousLayerDifferenceBrush))]
        public partial Color PreviousLayerDifferenceColor { get; set; } = new(255, 81, 131, 82);

        [XmlIgnore]
        public Avalonia.Media.Color PreviousLayerDifferenceBrush
        {
            get => PreviousLayerDifferenceColor.ToAvalonia();
            set => PreviousLayerDifferenceColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(NextLayerDifferenceBrush))]
        public partial Color NextLayerDifferenceColor { get; set; } = new(255, 81, 249, 252);

        [XmlIgnore]
        public Avalonia.Media.Color NextLayerDifferenceBrush
        {
            get => NextLayerDifferenceColor.ToAvalonia();
            set => NextLayerDifferenceColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BothLayerDifferenceBrush))]
        public partial Color BothLayerDifferenceColor { get; set; } = new(255, 246, 240, 216);

        [XmlIgnore]
        public Avalonia.Media.Color BothLayerDifferenceBrush
        {
            get => BothLayerDifferenceColor.ToAvalonia();
            set => BothLayerDifferenceColor = new Color(value);
        }

        [ObservableProperty]
        public partial bool ShowLayerDifference { get; set; }

        [ObservableProperty]
        public partial bool LayerDifferenceHighlightSimilarityInstead { get; set; }

        [ObservableProperty]
        public partial bool UseIssueColorOnTracker { get; set; } = true;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IslandBrush))]
        public partial Color IslandColor { get; set; } = new(255, 255, 255, 0);

        [XmlIgnore]
        public Avalonia.Media.Color IslandBrush
        {
            get => IslandColor.ToAvalonia();
            set => IslandColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IslandHighlightBrush))]
        public partial Color IslandHighlightColor { get; set; } = new(255, 255, 215, 0);

        [XmlIgnore]
        public Avalonia.Media.Color IslandHighlightBrush
        {
            get => IslandHighlightColor.ToAvalonia();
            set => IslandHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OverhangBrush))]
        public partial Color OverhangColor { get; set; } = new(255, 255, 105, 180);

        [XmlIgnore]
        public Avalonia.Media.Color OverhangBrush
        {
            get => OverhangColor.ToAvalonia();
            set => OverhangColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(OverhangHighlightBrush))]
        public partial Color OverhangHighlightColor { get; set; } = new(255, 255, 20, 147);

        [XmlIgnore]
        public Avalonia.Media.Color OverhangHighlightBrush
        {
            get => OverhangHighlightColor.ToAvalonia();
            set => OverhangHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResinTrapBrush))]
        public partial Color ResinTrapColor { get; set; } = new(255, 255, 165, 0);

        [XmlIgnore]
        public Avalonia.Media.Color ResinTrapBrush
        {
            get => ResinTrapColor.ToAvalonia();
            set => ResinTrapColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ResinTrapHighlightBrush))]
        public partial Color ResinTrapHighlightColor { get; set; } = new(255, 255, 127, 0);

        [XmlIgnore]
        public Avalonia.Media.Color ResinTrapHighlightBrush
        {
            get => ResinTrapHighlightColor.ToAvalonia();
            set => ResinTrapHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SuctionCupBrush))]
        public partial Color SuctionCupColor { get; set; } = new(255, 180, 235, 255);

        [XmlIgnore]
        public Avalonia.Media.Color SuctionCupBrush
        {
            get => SuctionCupColor.ToAvalonia();
            set => SuctionCupColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SuctionCupHighlightBrush))]
        public partial Color SuctionCupHighlightColor { get; set; } = new(255, 77, 207, 255);

        [XmlIgnore]
        public Avalonia.Media.Color SuctionCupHighlightBrush
        {
            get => SuctionCupHighlightColor.ToAvalonia();
            set => SuctionCupHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TouchingBoundsBrush))]
        public partial Color TouchingBoundsColor { get; set; } = new(255, 255, 0, 0);

        [XmlIgnore]
        public Avalonia.Media.Color TouchingBoundsBrush
        {
            get => TouchingBoundsColor.ToAvalonia();
            set => TouchingBoundsColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CrosshairBrush))]
        public partial Color CrosshairColor { get; set; } = new(255, 255, 0, 0);

        [XmlIgnore]
        public Avalonia.Media.Color CrosshairBrush
        {
            get => CrosshairColor.ToAvalonia();
            set => CrosshairColor = new Color(value);
        }

        [ObservableProperty]
        public partial bool ZoomPreferNative { get; set; }

        [ObservableProperty]
        public partial ushort ZoomDebounceMilliseconds { get; set; } = 20;

        [ObservableProperty]
        public partial bool ZoomToFitPrintVolumeBounds { get; set; } = true;

        [ObservableProperty]
        public partial byte ZoomLockLevelIndex { get; set; } = 7;

        [ObservableProperty]
        public partial bool ZoomIssues { get; set; } = true;

        [ObservableProperty]
        public partial bool CrosshairShowOnlyOnSelectedIssues { get; set; }

        [ObservableProperty]
        public partial byte CrosshairFadeLevelIndex { get; set; } = 5;

        [ObservableProperty]
        public partial uint CrosshairLength { get; set; } = 20;

        [ObservableProperty]
        public partial byte CrosshairMargin { get; set; } = 5;

        [ObservableProperty]
        public partial bool AutoRotateLayerBestView { get; set; } = true;

        [ObservableProperty]
        public partial bool AutoFlipLayerIfMirrored { get; set; } = true;

        [ObservableProperty]
        public partial bool LayerZoomToFitOnLoad { get; set; } = true;

        [ObservableProperty]
        public partial bool ShowBackgroundGrid { get; set; }

        [ObservableProperty]
        public partial ushort LayerSliderDebounce { get; set; }

        public LayerPreviewUserSettings Clone()
        {
            return (MemberwiseClone() as LayerPreviewUserSettings)!;
        }

    }
    #endregion

    #region Issues

    public sealed partial class IssuesUserSettings : ObservableObject
    {
        public enum ComputeIssuesOnFileLoadType : byte
        {
            [Description("Do not compute issues")]
            None,
            [Description("Compute time inexpensive issues (Empty layers and print height)")]
            TimeInexpensiveIssues,
            [Description("Compute the enabled issues")]
            EnabledIssues,
        }

        [ObservableProperty]
        public partial ComputeIssuesOnFileLoadType ComputeIssuesOnFileLoad { get; set; } = ComputeIssuesOnFileLoadType.TimeInexpensiveIssues;

        [ObservableProperty]
        public partial bool AutoRepairIssuesOnLoad { get; set; }

        [ObservableProperty]
        public partial bool ComputeIssuesOnClickTab { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeIslands { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeOverhangs { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeResinTraps { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeSuctionCups { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeTouchingBounds { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputePrintHeight { get; set; } = true;

        [ObservableProperty]
        public partial bool ComputeEmptyLayers { get; set; } = true;

        [ObservableProperty]
        public partial IssuesOrderBy DataGridOrderBy { get; set; } = IssuesOrderBy.TypeAscLayerAscAreaDesc;

        [ObservableProperty]
        public partial bool DataGridGroupByType { get; set; } = true;

        [ObservableProperty]
        public partial bool DataGridGroupByLayerIndex { get; set; }

        [ObservableProperty]
        public partial bool IslandEnhancedDetection { get; set; } = true;

        [ObservableProperty]
        public partial bool IslandAllowDiagonalBonds { get; set; }

        [ObservableProperty]
        public partial byte IslandBinaryThreshold { get; set; }

        [ObservableProperty]
        public partial byte IslandRequiredAreaToProcessCheck { get; set; } = 1;

        [ObservableProperty]
        public partial decimal IslandRequiredPixelsToSupportMultiplier { get; set; } = 0.25m;

        [ObservableProperty]
        public partial byte IslandRequiredPixelsToSupport { get; set; } = 10;

        [ObservableProperty]
        public partial byte IslandRequiredPixelBrightnessToProcessCheck { get; set; } = 1;

        [ObservableProperty]
        public partial byte IslandRequiredPixelBrightnessToSupport { get; set; } = 150;

        [ObservableProperty]
        public partial bool OverhangIndependentFromIslands { get; set; } = true;

        [ObservableProperty]
        public partial byte OverhangErodeIterations { get; set; } = 49;

        [ObservableProperty]
        public partial byte ResinTrapBinaryThreshold { get; set; } = 127;

        [ObservableProperty]
        public partial byte ResinTrapRequiredAreaToProcessCheck { get; set; } = 17;

        [ObservableProperty]
        public partial byte ResinTrapRequiredBlackPixelsToDrain { get; set; } = 10;

        [ObservableProperty]
        public partial byte ResinTrapMaximumPixelBrightnessToDrain { get; set; } = 30;

        [ObservableProperty]
        public partial uint SuctionCupRequiredAreaToConsider { get; set; } = 10000;

        [ObservableProperty]
        public partial decimal SuctionCupRequiredHeightToConsider { get; set; } = 0.5m;

        [ObservableProperty]
        public partial byte TouchingBoundMinimumPixelBrightness { get; set; } = 127;

        public byte TouchingBoundMarginLeft
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                if (TouchingBoundSyncMargins)
                {
                    TouchingBoundMarginRight = value;
                }
            }
        } = 5;

        public byte TouchingBoundMarginTop
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                if (TouchingBoundSyncMargins)
                {
                    TouchingBoundMarginBottom = value;
                }
            }
        } = 5;

        public byte TouchingBoundMarginRight
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                if (TouchingBoundSyncMargins)
                {
                    TouchingBoundMarginLeft = value;
                }
            }
        } = 5;

        public byte TouchingBoundMarginBottom
        {
            get;
            set
            {
                if (!SetProperty(ref field, value)) return;
                if (TouchingBoundSyncMargins)
                {
                    TouchingBoundMarginTop = value;
                }
            }
        } = 5;

        [ObservableProperty]
        public partial bool TouchingBoundSyncMargins { get; set; } = true;

        [ObservableProperty]
        public partial decimal PrintHeightOffset { get; set; }

        public IssuesUserSettings Clone()
        {
            return (MemberwiseClone() as IssuesUserSettings)!;
        }

    }
    #endregion

    #region Pixel Editor

    public sealed partial class PixelEditorUserSettings : ObservableObject
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddPixelBrush))]
        public partial Color AddPixelColor { get; set; } = new(255, 144, 238, 144);

        [XmlIgnore]
        public Avalonia.Media.Color AddPixelBrush
        {
            get => AddPixelColor.ToAvalonia();
            set => AddPixelColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddPixelHighlightBrush))]
        public partial Color AddPixelHighlightColor { get; set; } = new(255, 0, 255, 0);

        [XmlIgnore]
        public Avalonia.Media.Color AddPixelHighlightBrush
        {
            get => AddPixelHighlightColor.ToAvalonia();
            set => AddPixelHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RemovePixelBrush))]
        public partial Color RemovePixelColor { get; set; } = new(255, 219, 112, 147);

        [XmlIgnore]
        public Avalonia.Media.Color RemovePixelBrush
        {
            get => RemovePixelColor.ToAvalonia();
            set => RemovePixelColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(RemovePixelHighlightBrush))]
        public partial Color RemovePixelHighlightColor { get; set; } = new(255, 139, 0, 0);

        [XmlIgnore]
        public Avalonia.Media.Color RemovePixelHighlightBrush
        {
            get => RemovePixelHighlightColor.ToAvalonia();
            set => RemovePixelHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SupportsBrush))]
        public partial Color SupportsColor { get; set; } = new(255, 0, 255, 255);

        [XmlIgnore]
        public Avalonia.Media.Color SupportsBrush
        {
            get => SupportsColor.ToAvalonia();
            set => SupportsColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SupportsHighlightBrush))]
        public partial Color SupportsHighlightColor { get; set; } = new(255, 0, 139, 139);

        [XmlIgnore]
        public Avalonia.Media.Color SupportsHighlightBrush
        {
            get => SupportsHighlightColor.ToAvalonia();
            set => SupportsHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DrainHoleBrush))]
        public partial Color DrainHoleColor { get; set; } = new(255, 142, 69, 133);

        [XmlIgnore]
        public Avalonia.Media.Color DrainHoleBrush
        {
            get => DrainHoleColor.ToAvalonia();
            set => DrainHoleColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DrainHoleHighlightBrush))]
        public partial Color DrainHoleHighlightColor { get; set; } = new(255, 159, 0, 197);

        [XmlIgnore]
        public Avalonia.Media.Color DrainHoleHighlightBrush
        {
            get => DrainHoleHighlightColor.ToAvalonia();
            set => DrainHoleHighlightColor = new Color(value);
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CursorBrush))]
        public partial Color CursorColor { get; set; } = new(150, 52, 152, 219);

        [XmlIgnore]
        public Avalonia.Media.Color CursorBrush
        {
            get => CursorColor.ToAvalonia();
            set => CursorColor = new Color(value);
        }

        [ObservableProperty]
        public partial bool PartialUpdateIslandsOnEditing { get; set; } = true;

        [ObservableProperty]
        public partial bool CloseEditorOnApply { get; set; }

        public PixelEditorUserSettings Clone()
        {
            return (MemberwiseClone() as PixelEditorUserSettings)!;
        }
    }
    #endregion

    #region Layer Repair

    public sealed partial class LayerRepairUserSettings : ObservableObject
    {
        [ObservableProperty]
        public partial bool RepairIslands { get; set; } = true;

        [ObservableProperty]
        public partial bool RepairResinTraps { get; set; } = true;

        [ObservableProperty]
        public partial bool RepairSuctionCups { get; set; }

        [ObservableProperty]
        public partial bool RemoveEmptyLayers { get; set; } = true;

        [ObservableProperty]
        public partial ushort RemoveIslandsBelowEqualPixels { get; set; } = 5;

        [ObservableProperty]
        public partial ushort RemoveIslandsRecursiveIterations { get; set; } = 4;

        [ObservableProperty]
        public partial ushort AttachIslandsBelowLayers { get; set; } = 2;

        [ObservableProperty]
        public partial byte ResinTrapsOverlapBy { get; set; }

        [ObservableProperty]
        public partial byte SuctionCupsVentHole { get; set; } = 16;

        [ObservableProperty]
        public partial byte ClosingIterations { get; set; } = 2;

        [ObservableProperty]
        public partial byte OpeningIterations { get; set; }

        public LayerRepairUserSettings Clone()
        {
            return (MemberwiseClone() as LayerRepairUserSettings)!;
        }
    }
    #endregion

    #region Tools


    public sealed partial class ToolsUserSettings : ObservableObject
    {
        [ObservableProperty]
        public partial bool ExpandDescriptions { get; set; } = true;

        [ObservableProperty]
        public partial bool PromptForConfirmation { get; set; } = true;

        [ObservableProperty]
        public partial bool RestoreLastUsedSettings { get; set; }

        [ObservableProperty]
        public partial bool LastUsedSettingsKeepOnCloseFile { get; set; } = true;

        [ObservableProperty]
        public partial bool LastUsedSettingsPriorityOverDefaultProfile { get; set; } = true;
    }

    #endregion

    #region File Formats

    public sealed partial class FileFormatsUserSettings : ObservableObject
    {
        [ObservableProperty]
        public partial PerLayerSettingsModes PerLayerSettingsMode { get; set; } = CoreSettings.PerLayerSettingsMode;

        public FileFormatsUserSettings Clone()
        {
            return (MemberwiseClone() as FileFormatsUserSettings)!;
        }
    }

    #endregion

    #region Automations

    public sealed partial class AutomationsUserSettings : ObservableObject
    {
        [ObservableProperty]
        public partial bool SaveFileAfterModifications { get; set; } = true;

        [ObservableProperty]
        public partial bool FileNameOnlyAsciiCharacters { get; set; }

        [ObservableProperty]
        public partial bool AutoConvertFiles { get; set; } = true;

        [ObservableProperty]
        public partial RemoveSourceFileAction RemoveSourceFileAfterAutoConversion { get; set; } = RemoveSourceFileAction.No;

        [ObservableProperty]
        public partial RemoveSourceFileAction RemoveSourceFileAfterManualConversion { get; set; } = RemoveSourceFileAction.No;

        [ObservableProperty]
        public partial string? EventAfterFileLoadScriptFile { get; set; }

        [ObservableProperty]
        public partial string? EventBeforeFileSaveScriptFile { get; set; }

        [ObservableProperty]
        public partial string? EventAfterFileSaveScriptFile { get; set; }

        public AutomationsUserSettings Clone()
        {
            return (MemberwiseClone() as AutomationsUserSettings)!;
        }
    }

    #endregion

    #region Network


    public sealed partial class NetworkUserSettings : ObservableObject
    {
        [ObservableProperty]
        public partial RangeObservableCollection<RemotePrinter> RemotePrinters { get; set; } = [];

        public NetworkUserSettings Clone()
        {
            return this.CloneByXmlSerialization();
        }
    }

    #endregion

    #endregion

    #region Singleton

    public static string SettingsFolder => CoreSettings.DefaultSettingsFolderAndEnsureCreation;

    /// <summary>
    /// Default filepath for store <see cref="UserSettings"/>
    /// </summary>
    private static string FilePath => Path.Combine(SettingsFolder, "usersettings.xml");


    private static UserSettings? _instance;
    /// <summary>
    /// Instance of <see cref="UserSettings"/> (singleton)
    /// </summary>
    public static UserSettings Instance
    {
        get => _instance ??= new UserSettings();
        internal set => _instance = value;
    }
    #endregion

    #region Properties

    private GeneralUserSettings? _general;
    private LayerPreviewUserSettings? _layerPreview;
    private IssuesUserSettings? _issues;
    private PixelEditorUserSettings? _pixelEditor;
    private LayerRepairUserSettings? _layerRepair;
    private ToolsUserSettings? _tools;
    private FileFormatsUserSettings? _fileFormats;
    private AutomationsUserSettings? _automations;
    private NetworkUserSettings? _network;

    private string? _appVersion;


    public GeneralUserSettings General
    {
        get => _general ??= new GeneralUserSettings();
        set => _general = value;
    }

    public LayerPreviewUserSettings LayerPreview
    {
        get => _layerPreview ??= new LayerPreviewUserSettings();
        set => _layerPreview = value;
    }


    public IssuesUserSettings Issues
    {
        get => _issues ??= new IssuesUserSettings();
        set => _issues = value;
    }


    public PixelEditorUserSettings PixelEditor
    {
        get => _pixelEditor ??= new PixelEditorUserSettings();
        set => _pixelEditor = value;
    }



    public LayerRepairUserSettings LayerRepair
    {
        get => _layerRepair ??= new LayerRepairUserSettings();
        set => _layerRepair = value;
    }

    public ToolsUserSettings Tools
    {
        get => _tools ??= new ToolsUserSettings();
        set => _tools = value;
    }

    public FileFormatsUserSettings FileFormats
    {
        get => _fileFormats ??= new FileFormatsUserSettings();
        set => _fileFormats = value;
    }

    public AutomationsUserSettings Automations
    {
        get => _automations ??= new AutomationsUserSettings();
        set => _automations = value;
    }

    public NetworkUserSettings Network
    {
        get => _network ??= new NetworkUserSettings();
        set => _network = value;
    }

    /*
    /// <summary>
    /// Gets or sets the number of times this file has been reset to defaults
    /// </summary>
    public uint ResetCount { get; set; }
    */

    [ObservableProperty]
    public partial ushort SettingsVersion { get; set; } = SETTINGS_VERSION;

    /// <summary>
    /// Gets or sets the last running version of UVtools with these settings
    /// </summary>
    public string AppVersion
    {
        get => _appVersion ??= About.VersionString;
        set => SetProperty(ref _appVersion, value);
    }

    [ObservableProperty]
    public partial int LastBirthdayYearsOld { get; set; }

    /// <summary>
    /// Gets or sets the number of times this file has been saved
    /// </summary>
    [ObservableProperty]
    public partial uint SavesCount { get; set; }

    /// <summary>
    /// Gets or sets the last time this file has been modified
    /// </summary>
    [ObservableProperty]
    public partial DateTime ModifiedDateTime { get; set; }

    #endregion

    #region Constructor

    private UserSettings()
    {
        if (OperatingSystem.IsMacOS()) // Fix macOS scaling information
        {
            var monjave = new Version(10, 14, 6);
            if (Environment.OSVersion.Version.CompareTo(monjave) >= 0)
            {
                General.WindowsTakeIntoAccountScreenScaling = false;
            }
        }
    }
    #endregion

    #region Static Methods
    /// <summary>
    /// Reset settings to defaults
    /// </summary>
    /// <param name="save">True to save settings on file, otherwise false</param>
    public static void Reset(bool save = false)
    {
        _instance = new UserSettings();
        if (save) Save();
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

        try
        {
            _instance = XmlExtensions.DeserializeFromFile<UserSettings>(FilePath);

            if (_instance.SettingsVersion < SETTINGS_VERSION)
            {
                // Upgrade
                _instance.SettingsVersion = SETTINGS_VERSION;
            }

            CoreSettings.MaxDegreeOfParallelism = _instance.General.MaxDegreeOfParallelism;
            CoreSettings.DefaultLayerCompressionCodec = _instance.General.LayerCompressionCodec;
            CoreSettings.DefaultLayerCompressionLevel = _instance.General.LayerCompressionLevel;
            CoreSettings.AverageResin1000MlBottleCost = _instance.General.AverageResin1000MlBottleCost;
            CoreSettings.PerLayerSettingsMode = _instance.FileFormats.PerLayerSettingsMode;

            if (_instance.Network.RemotePrinters.Count == 0)
            {
                _instance.Network.RemotePrinters.AddRange(
                [
                    new RemotePrinter("0.0.0.0", 8081, "Nova3D")
                        {
                            CompatibleExtensions = "cws",
                            RequestUploadFile  = new (RemotePrinterRequest.RequestType.UploadFile,  RemotePrinterRequest.RequestMethod.POST, "file/upload/{0}"),
                            RequestPrintFile   = new (RemotePrinterRequest.RequestType.PrintFile,   RemotePrinterRequest.RequestMethod.GET, "file/print/{0}"),
                            RequestDeleteFile  = new (RemotePrinterRequest.RequestType.DeleteFile,  RemotePrinterRequest.RequestMethod.GET, "file/delete/{0}"),
                            RequestPausePrint  = new (RemotePrinterRequest.RequestType.PausePrint,  RemotePrinterRequest.RequestMethod.GET, "job/toggle/{0}"),
                            RequestResumePrint = new (RemotePrinterRequest.RequestType.ResumePrint, RemotePrinterRequest.RequestMethod.GET, "job/toggle/{0}"),
                            RequestStopPrint   = new (RemotePrinterRequest.RequestType.StopPrint,   RemotePrinterRequest.RequestMethod.GET, "job/stop/{0}"),
                            RequestGetFiles    = new (RemotePrinterRequest.RequestType.GetFiles,    RemotePrinterRequest.RequestMethod.GET, "file/list"),
                            RequestPrintStatus = new (RemotePrinterRequest.RequestType.PrintStatus, RemotePrinterRequest.RequestMethod.GET, "job/list"),
                            RequestPrinterInfo = new (RemotePrinterRequest.RequestType.PrinterInfo, RemotePrinterRequest.RequestMethod.GET, "setting/printerInfo"),
                        },
                        new RemotePrinter("0.0.0.0", 6000, "Anycubic")
                        {
                            // https://github.com/rudetrooper/Octoprint-Chituboard/issues/4#issuecomment-961264287
                            // https://github.com/adamoutler/anycubic-python
                            // https://github.com/adamoutler/Pi-Zero-W-Smart-USB-Flash-Drive/tree/main/src/home/pi/usb_share/scripts
                            CompatibleExtensions = "pws;pw0;pwx;dlp;dl2p;pwmo;pwma;pwms;pwmx;pmx2;pwmb;pwsq;pm3;pm3m;pm3r;pwc",
                            RequestUploadFile  = new (RemotePrinterRequest.RequestType.UploadFile,  RemotePrinterRequest.RequestMethod.TCP),
                            RequestPrintFile   = new (RemotePrinterRequest.RequestType.PrintFile,   RemotePrinterRequest.RequestMethod.TCP, @"<$getfile>{0}\/([0-9]+[.][0-9a-zA-Z]+),>goprint,{#1}$>"),
                            RequestDeleteFile  = new (RemotePrinterRequest.RequestType.DeleteFile,  RemotePrinterRequest.RequestMethod.TCP, @"<$getfile>{0}\/([0-9]+[.][0-9a-zA-Z]+),>delfile,{#1}$>"),
                            RequestPausePrint  = new (RemotePrinterRequest.RequestType.PausePrint,  RemotePrinterRequest.RequestMethod.TCP, "gopause"),
                            RequestResumePrint = new (RemotePrinterRequest.RequestType.ResumePrint, RemotePrinterRequest.RequestMethod.TCP, "goresume"),
                            RequestStopPrint   = new (RemotePrinterRequest.RequestType.StopPrint,   RemotePrinterRequest.RequestMethod.TCP, "gostop"),
                            RequestGetFiles    = new (RemotePrinterRequest.RequestType.GetFiles,    RemotePrinterRequest.RequestMethod.TCP, "getfile"),
                            RequestPrintStatus = new (RemotePrinterRequest.RequestType.PrintStatus, RemotePrinterRequest.RequestMethod.TCP, "getstatus"),
                            RequestPrinterInfo = new (RemotePrinterRequest.RequestType.PrinterInfo, RemotePrinterRequest.RequestMethod.TCP, "sysinfo"),
                            // getmode
                            // getwifi - displays the current wifi network name.
                            // gethistory - gets the history and print settings of previous prints.
                            // delhistory - deletes printing history.
                            // getPreview1 - returns a list of dimensions used for the print.
                            // getPreview2 - returns a binary preview image of the print.
                        }
                    /*new RemotePrinter("0.0.0.0", 40454, "Creality Halot")
                        {
                            CompatibleExtensions = "cxdlp",
                            RequestUploadFile  = new (RemotePrinterRequest.RequestType.UploadFile,  RemotePrinterRequest.RequestMethod.POST, "{0}"),
                            //RequestPrintFile   = new (RemotePrinterRequest.RequestType.PrintFile,   RemotePrinterRequest.RequestMethod.GET, "file/print/{0}"),
                            //RequestDeleteFile  = new (RemotePrinterRequest.RequestType.DeleteFile,  RemotePrinterRequest.RequestMethod.GET, "file/delete/{0}"),
                            //RequestPausePrint  = new (RemotePrinterRequest.RequestType.PausePrint,  RemotePrinterRequest.RequestMethod.GET, "job/toggle/{0}"),
                            //RequestResumePrint = new (RemotePrinterRequest.RequestType.ResumePrint, RemotePrinterRequest.RequestMethod.GET, "job/toggle/{0}"),
                            //RequestStopPrint   = new (RemotePrinterRequest.RequestType.StopPrint,   RemotePrinterRequest.RequestMethod.GET, "job/stop/{0}"),
                            //RequestGetFiles    = new (RemotePrinterRequest.RequestType.GetFiles,    RemotePrinterRequest.RequestMethod.GET, "file/list"),
                            //RequestPrintStatus = new (RemotePrinterRequest.RequestType.PrintStatus, RemotePrinterRequest.RequestMethod.GET, "job/list"),
                            //RequestPrinterInfo = new (RemotePrinterRequest.RequestType.PrinterInfo, RemotePrinterRequest.RequestMethod.GET, "setting/printerInfo"),
                        }*/
                ]);
            }

            if (_instance.General.SendToProcess.Count == 0)
            {
                if (OperatingSystem.IsWindows())
                {
                    var findDirectories = new List<string>
                    {
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)
                    };
                    if (Environment.Is64BitOperatingSystem)
                    {
                        findDirectories.Add(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86));
                    }

                    foreach (var path in findDirectories)
                    {
                        var directories = Directory.GetDirectories(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
                        foreach (var directory in directories)
                        {
                            /*if (directory.StartsWith($"{path}\\Prusa3D", StringComparison.InvariantCultureIgnoreCase))
                            {
                                var executable = $"{directory}\\PrusaSlicer\\prusa-slicer.exe";
                                if (File.Exists(executable))
                                {
                                    var application = new MappedApplication(executable, "Slicer: PrusaSlicer");
                                    foreach (var slicerFile in FileFormat.AvailableFormats)
                                    {
                                        if (slicerFile is not SL1File) continue;
                                        application.CompatibleExtensions += slicerFile.GetFileExtensions(string.Empty, ";");
                                    }

                                    _instance.General.SendToApplications.Add(application);
                                }

                                continue;
                            }*/

                            if (directory.StartsWith($"{path}\\Chitubox", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\CHITUBOX.exe";
                                //var executablePro = $"{directory}\\CHITUBOXPro.exe";
                                if (File.Exists(executable))
                                {
                                    _instance.General.SendToProcess.Add(new MappedProcess(executable, "Slicer: Chitubox")
                                    {
                                        CompatibleExtensions = "cbddlp;ctb;phz;photon;photons;fdg"
                                    });
                                }
                                /*else if (File.Exists(executablePro))
                                {
                                    _instance.General.SendToApplications.Add(new MappedApplication(executablePro, "Slicer: Chitubox Pro")
                                    {
                                        CompatibleExtensions = "cbddlp;ctb;phz;photon;photons;fdg"
                                    });
                                }*/
                                continue;
                            }

                            if (directory.StartsWith($"{path}\\Photon_WorkShop", StringComparison.OrdinalIgnoreCase))
                            {
                                var directoryName = Path.GetFileName(directory);
                                var executable = $"{directory}\\{directoryName}.exe";
                                if (File.Exists(executable))
                                {
                                    var application = new MappedProcess(executable, $"Slicer: {directoryName.Replace('_', ' ')}")
                                    {
                                        CompatibleExtensions = "photon;photons;"
                                    };
                                    foreach (var slicerFile in FileFormat.AvailableFormats)
                                    {
                                        if (slicerFile is not AnycubicFile) continue;
                                        application.CompatibleExtensions += slicerFile.GetFileExtensions(string.Empty, ";", true);
                                    }


                                    _instance.General.SendToProcess.Add(application);
                                }
                                continue;
                            }

                            if (directory.StartsWith($"{path}\\UNIZ", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\UnizMaker\\UnizMaker.exe";
                                if (File.Exists(executable))
                                {
                                    _instance.General.SendToProcess.Add(new MappedProcess(executable, "Slicer: UnizMaker")
                                    {
                                        CompatibleExtensions = "zcode"
                                    });
                                }
                                continue;
                            }

                            if (directory.StartsWith($"{path}\\Zortrax", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\Z-Suite\\Z-SUITE.exe";
                                if (File.Exists(executable))
                                {
                                    _instance.General.SendToProcess.Add(new MappedProcess(executable, "Slicer: Z-SUITE")
                                    {
                                        CompatibleExtensions = "zcodex"
                                    });
                                }
                                continue;
                            }

                            if (directory.StartsWith($"{path}\\WinRAR", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\WinRAR.exe";
                                if (File.Exists(executable))
                                {

                                    var application = new MappedProcess(executable, "Open archive: WinRAR");
                                    var extensions = new List<string>();
                                    foreach (var slicerFile in FileFormat.AvailableFormats)
                                    {
                                        if (slicerFile.FileType != FileFormat.FileFormatType.Archive) continue;
                                        using var pooledArray = slicerFile.FileExtensions
                                            .AsValueEnumerable()
                                            .Where(extension => !extension.IsVirtual)
                                            .Select(extension => extension.Extension)
                                            .ToArrayPool();
                                        extensions.AddRange(pooledArray.Span);
                                    }

                                    application.CompatibleExtensions += extensions
                                        .AsValueEnumerable()
                                        .Distinct()
                                        .JoinToString(';');
                                    _instance.General.SendToProcess.Add(application);
                                }

                                continue;
                            }

                            if (directory.StartsWith($"{path}\\7-Zip", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\7zFM.exe";
                                if (File.Exists(executable))
                                {
                                    var application = new MappedProcess(executable, "Open archive: 7-Zip");
                                    var extensions = new List<string>();
                                    foreach (var slicerFile in FileFormat.AvailableFormats)
                                    {
                                        if (slicerFile.FileType != FileFormat.FileFormatType.Archive) continue;
                                        using var pooledArray = slicerFile.FileExtensions
                                            .AsValueEnumerable()
                                            .Where(extension => !extension.IsVirtual)
                                            .Select(extension => extension.Extension)
                                            .ToArrayPool();
                                        extensions.AddRange(pooledArray.Span);
                                    }

                                    application.CompatibleExtensions += extensions
                                        .AsValueEnumerable()
                                        .Distinct()
                                        .JoinToString(';');
                                    _instance.General.SendToProcess.Add(application);
                                }

                                continue;
                            }


                            if (directory.StartsWith($"{path}\\010 Editor", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\010Editor.exe";
                                if (File.Exists(executable))
                                {
                                    var application = new MappedProcess(true, executable, "Hex editor: 010");
                                    _instance.General.SendToProcess.Add(application);
                                }

                                continue;
                            }

                            if (directory.StartsWith($"{path}\\HxD", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\HxD.exe";
                                if (File.Exists(executable))
                                {
                                    var application = new MappedProcess(false, executable, "Hex editor: HxD");
                                    _instance.General.SendToProcess.Add(application);
                                }

                                continue;
                            }

                            if (directory.StartsWith($"{path}\\Notepad++", StringComparison.OrdinalIgnoreCase))
                            {
                                var executable = $"{directory}\\notepad++.exe";
                                if (File.Exists(executable))
                                {
                                    _instance.General.SendToProcess.Add(new MappedProcess(executable, "Notepad++")
                                    {
                                        CompatibleExtensions = "qdt"
                                    });
                                }
                                continue;
                            }
                        }
                    }

                    _instance.General.SendToProcess.Add(new MappedProcess("notepad.exe", "Notepad")
                    {
                        CompatibleExtensions = "qdt"
                    });

                    if (_instance.General.SendToProcess.Count > 0)
                    {
                        _instance.General.SendToProcess.Sort((process, mappedProcess) => string.Compare(process.Name, mappedProcess.Name, StringComparison.Ordinal));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            Console.WriteLine(e.Message);
            //File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "uvtools.txt"), e.Message);
            Reset();
        }
    }


    /// <summary>
    /// Save settings to file
    /// </summary>
    public static void Save()
    {
        Instance.SavesCount++;
        _instance!.ModifiedDateTime = DateTime.Now;
        CoreSettings.MaxDegreeOfParallelism = _instance.General.MaxDegreeOfParallelism;
        CoreSettings.DefaultLayerCompressionCodec = _instance.General.LayerCompressionCodec;
        CoreSettings.DefaultLayerCompressionLevel = _instance.General.LayerCompressionLevel;
        CoreSettings.AverageResin1000MlBottleCost = _instance.General.AverageResin1000MlBottleCost;
        CoreSettings.PerLayerSettingsMode = _instance.FileFormats.PerLayerSettingsMode;
        try
        {
            XmlExtensions.SerializeToFile(_instance, FilePath, XmlExtensions.SettingsIndent);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }
    }

    public static void SetVersion()
    {
        Instance.AppVersion = About.VersionString;
    }

    public static object[] PackObjects =>
    [
        Instance.General,
        Instance.LayerPreview,
        Instance.Issues,
        Instance.PixelEditor,
        Instance.LayerRepair,
        Instance.FileFormats,
        Instance.Automations,
        Instance.Network
    ];
    #endregion

    #region Methods

    public UserSettings Clone()
    {
        /*var clone = MemberwiseClone() as UserSettings;
        clone.General = General.Clone();
        clone.LayerPreview = LayerPreview.Clone();
        clone.Issues = Issues.Clone();
        clone.PixelEditor = PixelEditor.Clone();
        clone.LayerRepair = LayerRepair.Clone();
        clone.Automations = Automations.Clone();
        clone.Network = Network.Clone();
        return clone;*/

        return this.CloneByXmlSerialization();
    }

    #endregion
}