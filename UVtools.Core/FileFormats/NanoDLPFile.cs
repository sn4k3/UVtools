/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.FileFormats;

public sealed class NanoDLPFile : FileFormat
{
    #region Constants
    private const string MetaManifestFileName = "meta.json";
    private const string SlicerManifestFileName = "slicer.json";
    private const string PlateManifestFileName = "plate.json";
    private const string ProfileManifestFileName = "profile.json";
    private const string OverrideManifestFileName = "override.json";
    private const string InfoManifestFileName = "info.json";
    private const string StlFileName = "plate.stl";
    private const string ThumbnailFileName = "3d.png";
    private const string ThumbnailMetaFileName = "3d.png.meta";
    #endregion

    #region Sub Classes

    public sealed class NanoDLPMetaManifest
    {
        /// <summary>
        /// Current version of the export file
        /// </summary>
        public int FormatVersion { get; set; } = 2;

        /// <summary>
        /// Manufacturer or target printer
        /// </summary>
        public string Distro { get; set; } = "generic";

        /// <summary>
        /// Program used to prepare file
        /// </summary>
        public string Program { get; set; } = About.Software;

        /// <summary>
        /// Program version
        /// </summary>
        public string Version { get; set; } = About.VersionString;

        /// <summary>
        /// OS used to export it
        /// </summary>
        public string OS { get; set; } = RuntimeInformation.RuntimeIdentifier;

        /// <summary>
        /// Architecture used
        /// </summary>
        public string Arch { get; set; } = RuntimeInformation.OSArchitecture.ToString();

        /// <summary>
        /// Is profile file should be used
        /// </summary>
        public bool Profile { get; set; }

        public override string ToString()
        {
            return $"{nameof(FormatVersion)}: {FormatVersion}, {nameof(Distro)}: {Distro}, {nameof(Program)}: {Program}, {nameof(Version)}: {Version}, {nameof(OS)}: {OS}, {nameof(Arch)}: {Arch}, {nameof(Profile)}: {Profile}";
        }
    }

    public sealed class NanoDLPBoundary
    {
        public float XMin { get; set; }
        public float XMax { get; set; }
        public float YMin { get; set; }
        public float YMax { get; set; }
        public float ZMin { get; set; }
        public float ZMax { get; set; }

        public override string ToString()
        {
            return $"{nameof(XMin)}: {XMin}, {nameof(XMax)}: {XMax}, {nameof(YMin)}: {YMin}, {nameof(YMax)}: {YMax}, {nameof(ZMin)}: {ZMin}, {nameof(ZMax)}: {ZMax}";
        }
    }

    public sealed class NanoDLPColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
        public byte A { get; set; } = byte.MaxValue;

        public NanoDLPColor()
        {
        }

        public NanoDLPColor(byte r, byte g, byte b, byte a = byte.MaxValue)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public override string ToString()
        {
            return $"{nameof(R)}: {R}, {nameof(G)}: {G}, {nameof(B)}: {B}, {nameof(A)}: {A}";
        }
    }

    public sealed class NanoDLPMCItem
    {
        public float StartX { get; set; }
        public float StartY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float MultiCureGap { get; set; }
        public uint Count { get; set; }

        public override string ToString()
        {
            return $"{nameof(StartX)}: {StartX}, {nameof(StartY)}: {StartY}, {nameof(Width)}: {Width}, {nameof(Height)}: {Height}, {nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(MultiCureGap)}: {MultiCureGap}, {nameof(Count)}: {Count}";
        }
    }

    public sealed class NanoDLPSlicerManifest
    {
        public string Type { get; set; } = "stl";
        public string URL { get; set; } = string.Empty;
        public uint PWidth { get; set; }
        public uint PHeight { get; set; }
        public float ScaleFactor { get; set; }
        public uint StartLayer { get; set; }

        public float SupportDepth { get; set; } = DefaultLayerHeight;
        public float SupportLayerNumber { get; set; } = DefaultBottomLayerCount;
        public float Thickness { get; set; } = DefaultLayerHeight;
        public float XOffset { get; set; }
        public float YOffset { get; set; }
        public float ZOffset { get; set; }
        public float XPixelSize { get; set; }
        public float YPixelSize { get; set; }
        public string? Mask { get; set; }
        public bool SliceFromZero { get; set; }
        public bool DisableValidator { get; set; }
        public byte AutoCenter { get; set; }
        public bool PreviewGenerate { get; set; }
        public bool Running { get; set; }
        public bool Debug { get; set; }
        public bool IsFaulty { get; set; }
        public bool Corrupted { get; set; }
        public bool MultiMaterial { get; set; }
        public string AdaptExport { get; set; } = string.Empty;
        public string PreviewColor { get; set; } = string.Empty;
        public int[]? FaultyLayers { get; set; } = [];
        public int[]? OverhangLayers { get; set; } = [];
        public int[]? LayerStatus { get; set; } = [];
        public NanoDLPBoundary Boundary { get; set; } = new();
        public NanoDLPMCItem MC { get; set; } = new();
        public string? MultiThickness { get; set; } = string.Empty;
        public string ExportPath { get; set; } = string.Empty;
        public string NetworkSave { get; set; } = string.Empty;
        public string File { get; set; } = $"/{StlFileName}";
        public uint FileSize { get; set; }
        public byte AdaptSlicing { get; set; }
        public float AdaptSlicingMin { get; set; }
        public float AdaptSlicingMax { get; set; }
        public float SupportOffset { get; set; }
        public float Offset { get; set; }
        public string FillColor { get; set; } = "#ffffff";
        public string BlankColor { get; set; } = "#000000";
        public float DimAmount { get; set; }
        public int DimWall { get; set; }
        public uint DimSkip { get; set; }
        public byte PixelDiming { get; set; }
        public byte HatchingType { get; set; }
        public byte ElephantMidExposure { get; set; } = 1;
        public byte ElephantType { get; set; }
        public float ElephantAmount { get; set; }
        public uint ElephantWall { get; set; }
        public uint ElephantLayers { get; set; }
        public uint HatchingWall { get; set; } = 20;
        public uint HatchingGap { get; set; } = 20;
        public uint HatchingOuterWall { get; set; } = 20;
        public uint HatchingTopCap { get; set; } = 10;
        public uint HatchingBottomCap { get; set; } = 10;
        public float MultiCureGap { get; set; }
        public byte AntiAlias { get; set; }
        public byte AntiAlias3D { get; set; }
        public float AntiAlias3DDistance { get; set; }
        public byte AntiAlias3DMin { get; set; }
        public float AntiAliasThreshold { get; set; }
        public byte ImageRotate { get; set; }
        public byte IgnoreMask { get; set; } = 1;
        public float XYRes { get; set; }
        public float XRes { get; set; }
        public float YRes { get; set; }
        public float ZResPerc { get; set; }
        public uint PreviewWidth { get; set; }
        public uint PreviewHeight { get; set; }
        public float BarrelFactor { get; set; }
        public float BarrelX { get; set; }
        public float BarrelY { get; set; }
        public byte ImageMirror { get; set; }
        public byte DisplayController { get; set; } = 1;
        public string LightOutputFormula { get; set; } = string.Empty;
        public uint PlateID { get; set; }
        public uint LayerID { get; set; }
        public uint LayerCount { get; set; }
        public string UUID { get; set; } = string.Empty;
        public float[]? DynamicThickness { get; set; } = [];

        public NanoDLPColor FillColorRGB { get; set; } = new(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
        public NanoDLPColor BlankColorRGB { get; set; } = new();

        public byte ExportType { get; set; } = 1;
        public string OutputPath { get; set; } = string.Empty;
        public string Suffix { get; set; } = string.Empty;
        public byte SkipEmpty { get; set; }

        public override string ToString()
        {
            return $"{nameof(Type)}: {Type}, {nameof(PWidth)}: {PWidth}, {nameof(PHeight)}: {PHeight}, {nameof(ScaleFactor)}: {ScaleFactor}, {nameof(StartLayer)}: {StartLayer}, {nameof(SupportDepth)}: {SupportDepth}, {nameof(SupportLayerNumber)}: {SupportLayerNumber}, {nameof(Thickness)}: {Thickness}, {nameof(XOffset)}: {XOffset}, {nameof(YOffset)}: {YOffset}, {nameof(ZOffset)}: {ZOffset}, {nameof(XPixelSize)}: {XPixelSize}, {nameof(YPixelSize)}: {YPixelSize}, {nameof(Mask)}: {Mask}, {nameof(SliceFromZero)}: {SliceFromZero}, {nameof(DisableValidator)}: {DisableValidator}, {nameof(AutoCenter)}: {AutoCenter}, {nameof(PreviewGenerate)}: {PreviewGenerate}, {nameof(Running)}: {Running}, {nameof(Debug)}: {Debug}, {nameof(IsFaulty)}: {IsFaulty}, {nameof(Corrupted)}: {Corrupted}, {nameof(MultiMaterial)}: {MultiMaterial}, {nameof(AdaptExport)}: {AdaptExport}, {nameof(PreviewColor)}: {PreviewColor}, {nameof(FaultyLayers)}: {FaultyLayers}, {nameof(OverhangLayers)}: {OverhangLayers}, {nameof(LayerStatus)}: {LayerStatus}, {nameof(Boundary)}: {Boundary}, {nameof(MC)}: {MC}, {nameof(MultiThickness)}: {MultiThickness}, {nameof(ExportPath)}: {ExportPath}, {nameof(NetworkSave)}: {NetworkSave}, {nameof(File)}: {File}, {nameof(FileSize)}: {FileSize}, {nameof(AdaptSlicing)}: {AdaptSlicing}, {nameof(AdaptSlicingMin)}: {AdaptSlicingMin}, {nameof(AdaptSlicingMax)}: {AdaptSlicingMax}, {nameof(SupportOffset)}: {SupportOffset}, {nameof(Offset)}: {Offset}, {nameof(FillColor)}: {FillColor}, {nameof(BlankColor)}: {BlankColor}, {nameof(DimAmount)}: {DimAmount}, {nameof(DimWall)}: {DimWall}, {nameof(DimSkip)}: {DimSkip}, {nameof(PixelDiming)}: {PixelDiming}, {nameof(HatchingType)}: {HatchingType}, {nameof(ElephantMidExposure)}: {ElephantMidExposure}, {nameof(ElephantType)}: {ElephantType}, {nameof(ElephantAmount)}: {ElephantAmount}, {nameof(ElephantWall)}: {ElephantWall}, {nameof(ElephantLayers)}: {ElephantLayers}, {nameof(HatchingWall)}: {HatchingWall}, {nameof(HatchingGap)}: {HatchingGap}, {nameof(HatchingOuterWall)}: {HatchingOuterWall}, {nameof(HatchingTopCap)}: {HatchingTopCap}, {nameof(HatchingBottomCap)}: {HatchingBottomCap}, {nameof(MultiCureGap)}: {MultiCureGap}, {nameof(AntiAlias)}: {AntiAlias}, {nameof(AntiAlias3D)}: {AntiAlias3D}, {nameof(AntiAlias3DDistance)}: {AntiAlias3DDistance}, {nameof(AntiAlias3DMin)}: {AntiAlias3DMin}, {nameof(AntiAliasThreshold)}: {AntiAliasThreshold}, {nameof(ImageRotate)}: {ImageRotate}, {nameof(IgnoreMask)}: {IgnoreMask}, {nameof(XYRes)}: {XYRes}, {nameof(XRes)}: {XRes}, {nameof(YRes)}: {YRes}, {nameof(ZResPerc)}: {ZResPerc}, {nameof(PreviewWidth)}: {PreviewWidth}, {nameof(PreviewHeight)}: {PreviewHeight}, {nameof(BarrelFactor)}: {BarrelFactor}, {nameof(BarrelX)}: {BarrelX}, {nameof(BarrelY)}: {BarrelY}, {nameof(ImageMirror)}: {ImageMirror}, {nameof(DisplayController)}: {DisplayController}, {nameof(LightOutputFormula)}: {LightOutputFormula}, {nameof(PlateID)}: {PlateID}, {nameof(LayerID)}: {LayerID}, {nameof(LayerCount)}: {LayerCount}, {nameof(UUID)}: {UUID}, {nameof(DynamicThickness)}: {DynamicThickness}, {nameof(FillColorRGB)}: {FillColorRGB}, {nameof(BlankColorRGB)}: {BlankColorRGB}, {nameof(ExportType)}: {ExportType}, {nameof(OutputPath)}: {OutputPath}, {nameof(Suffix)}: {Suffix}, {nameof(SkipEmpty)}: {SkipEmpty}";
        }
    }

    public sealed class NanoDLPPlateManifest
    {
        public uint PlateID { get; set; }
        public uint ProfileID { get; set; }
        public string? Profile { get; set; }
        public uint CreatedDate { get; set; }
        public string? StopLayers { get; set; } = string.Empty;
        public string? Path { get; set; } = string.Empty;
        public uint LowQualityLayerNumber { get; set; }
        public byte AutoCenter { get; set; }
        public long Updated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        public uint LastPrint { get; set; }
        public uint PrintTime { get; set; }
        public uint PrintEst { get; set; }
        public byte ImageRotate { get; set; }
        public float MaskEffect { get; set; }
        public float XRes { get; set; }
        public float YRes { get; set; }
        public float ZRes { get; set; }
        public string? MultiCure { get; set; } = string.Empty;
        public string? MultiThickness { get; set; } = string.Empty;
        public float[]? CureTimes { get; set; } = [];
        public float[]? DynamicThickness { get; set; } = [];
        public float Offset { get; set; }
        public int[]? OverHangs { get; set; } = [];
        public bool Risky { get; set; }
        public bool IsFaulty { get; set; }
        public bool Repaired { get; set; }
        public int[]? FaultyLayers { get; set; } = [];
        public bool Corrupted { get; set; }
        public float TotalSolidArea { get; set; }
        public string? BlackoutData { get; set; }
        public uint LayersCount { get; set; }
        public bool Feedback { get; set; }
        public bool ReSliceNeeded { get; set; }
        public bool MultiMaterial { get; set; }
        public uint PrintID { get; set; }
        public NanoDLPMCItem MC { get; set; } = new();
        public float XMin { get; set; }
        public float XMax { get; set; }
        public float YMin { get; set; }
        public float YMax { get; set; }
        public float ZMin { get; set; }
        public float ZMax { get; set; }

        public void Update()
        {
            Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        public override string ToString()
        {
            return $"{nameof(PlateID)}: {PlateID}, {nameof(ProfileID)}: {ProfileID}, {nameof(Profile)}: {Profile}, {nameof(CreatedDate)}: {CreatedDate}, {nameof(StopLayers)}: {StopLayers}, {nameof(Path)}: {Path}, {nameof(LowQualityLayerNumber)}: {LowQualityLayerNumber}, {nameof(AutoCenter)}: {AutoCenter}, {nameof(Updated)}: {Updated}, {nameof(LastPrint)}: {LastPrint}, {nameof(PrintTime)}: {PrintTime}, {nameof(PrintEst)}: {PrintEst}, {nameof(ImageRotate)}: {ImageRotate}, {nameof(MaskEffect)}: {MaskEffect}, {nameof(XRes)}: {XRes}, {nameof(YRes)}: {YRes}, {nameof(ZRes)}: {ZRes}, {nameof(MultiCure)}: {MultiCure}, {nameof(MultiThickness)}: {MultiThickness}, {nameof(CureTimes)}: {CureTimes}, {nameof(DynamicThickness)}: {DynamicThickness}, {nameof(Offset)}: {Offset}, {nameof(OverHangs)}: {OverHangs}, {nameof(Risky)}: {Risky}, {nameof(IsFaulty)}: {IsFaulty}, {nameof(Repaired)}: {Repaired}, {nameof(FaultyLayers)}: {FaultyLayers}, {nameof(Corrupted)}: {Corrupted}, {nameof(TotalSolidArea)}: {TotalSolidArea}, {nameof(BlackoutData)}: {BlackoutData}, {nameof(LayersCount)}: {LayersCount}, {nameof(Feedback)}: {Feedback}, {nameof(ReSliceNeeded)}: {ReSliceNeeded}, {nameof(MultiMaterial)}: {MultiMaterial}, {nameof(PrintID)}: {PrintID}, {nameof(MC)}: {MC}, {nameof(XMin)}: {XMin}, {nameof(XMax)}: {XMax}, {nameof(YMin)}: {YMin}, {nameof(YMax)}: {YMax}, {nameof(ZMin)}: {ZMin}, {nameof(ZMax)}: {ZMax}";
        }
    }

    public sealed class NanoDLPProfileManifest
    {
        public uint ResinID { get; set; }
        public uint ProfileID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Desc { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public float ResinPrice { get; set; }
        public float OptimumTemperature { get; set; }
        public float Depth { get; set; } = DefaultLayerHeight;
        public float SupportTopWait { get; set; }
        public float SupportWaitHeight { get; set; }
        public float SupportDepth { get; set; } = DefaultLayerHeight;
        public float SupportWaitBeforePrint { get; set; }
        public float SupportWaitAfterPrint { get; set; }
        public ushort TransitionalLayer { get; set; }
        public long Updated { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public Dictionary<string, string> CustomValues { get; set; } = new()
        {
            {"CdThreshold", "2000"},
            {"DwFlowEndSlope", "10"},
            {"FssEnableCrashdetection", "0"},
            {"FssEnableDynamicwait", "0"},
            {"FssEnablePeeldetection", "0"},
            {"FssEnableResinleveldetection", "0"},
            {"PdPeelEndSlope", "0"},
            {"PdPeelStartSlope", "0"},
            {"ResinPreheatTemperature", "25"},
            {"RlThreshold", "-5"},
            {"UvPwmValue", "1.0"},
        };

        public int Type { get; set; }
        public uint ZStepWait { get; set; } = 5000;
        public float TopWait { get; set; }
        public float WaitHeight { get; set; } = 5;
        public float CureTime { get; set; } = DefaultExposureTime;
        public float WaitBeforePrint { get; set; }
        public float WaitAfterPrint { get; set; }
        public float SupportCureTime { get; set; } = DefaultBottomExposureTime;
        public ushort SupportLayerNumber { get; set; } = DefaultBottomLayerCount;
        public byte AdaptSlicing { get; set; }
        public float AdaptSlicingMin { get; set; }
        public float AdaptSlicingMax { get; set; }
        public float SupportOffset { get; set; }
        public float Offset { get; set; }
        public string FillColor { get; set; } = "#ffffff";
        public string BlankColor { get; set; } = "#000000";
        public float DimAmount { get; set; }
        public int DimWall { get; set; }
        public uint DimSkip { get; set; }
        public byte PixelDiming { get; set; }
        public byte HatchingType { get; set; }
        public byte ElephantMidExposure { get; set; } = 1;
        public byte ElephantType { get; set; }
        public float ElephantAmount { get; set; }
        public uint ElephantWall { get; set; }
        public uint ElephantLayers { get; set; }
        public uint HatchingWall { get; set; } = 20;
        public uint HatchingGap { get; set; } = 20;
        public uint HatchingOuterWall { get; set; } = 20;
        public uint HatchingTopCap { get; set; } = 10;
        public uint HatchingBottomCap { get; set; } = 10;
        public float MultiCureGap { get; set; }
        public byte AntiAlias { get; set; }
        public byte AntiAlias3D { get; set; }
        public float AntiAlias3DDistance { get; set; }
        public byte AntiAlias3DMin { get; set; }
        public float AntiAliasThreshold { get; set; }
        public byte ImageRotate { get; set; }
        public byte IgnoreMask { get; set; } = 1;
        public float XYRes { get; set; }
        public float YRes { get; set; }
        public float ZResPerc { get; set; }
        public string DynamicCureTime { get; set; } = string.Empty;
        public string DynamicSpeed { get; set; } = string.Empty;
        public string ShieldBeforeLayer { get; set; } = "G90\\r\\nSET_PROGRESS_BAR CL=[[LayerNumber]] TL=[[TotalNumberOfLayers]]\\r\\nMOVE_PLATE Z=[[LayerPosition]] F=600\\r\\n[[MoveWait 2]]\\r\\n[[PositionSet [[LayerPosition]]]]\\r\\n[JS]\\r\\nif([[_FssEnableDynamicwait]] == 0)output=\\\"[[DynamicWaitStart]]\\\";\\r\\n[/JS]\\r\\nfss_idle";
        public string ShieldAfterLayer { get; set; } = "[JS]if ([[LayerNumber]]==1||[[LayerNumber]]%3==0) output=\\\"TRIGGER\\\";[/JS]\\r\\n[JS]if ([[LayerNumber]]==1||[[LayerNumber]]%5==0) output=\\\"M105\\\";[/JS]\\r\\n[[MoveCounterSet 0]]\\r\\nUVLED_ON PWM=[[_UvPwmValue]]\\r\\nDWELL P={[[CureTime]]*1000}\\r\\n[[MoveWait 1]]\\r\\nUVLED_OFF \\r\\n[[GPIOHigh 10]]\\r\\n\\r\\n[JS]\\r\\nvar nano = nanodlpContext();\\r\\nvar resinlevel = nano[\\\"Status\\\"][\\\"ResinLevelMm\\\"];\\r\\nif( [[_FssEnablePeeldetection]]==0 \\u0026\\u0026 [[CurrentPosition]] \\u003e resinlevel) output=\\\"[[SeparationDetectionStart]]\\\";\\r\\n[/JS]\\r\\n\\r\\n[[MoveCounterSet 0]]\\r\\nG91\\r\\nMOVE_PLATE_FSS Z=[[ZLiftDistance]] F={120-([[LayerNumber]]\\u003c5)*70}\\r\\n[[PositionChange [[ZLiftDistance]]]]\\r\\n[[MoveWait 1]]\\r\\n[[SeparationDetectionStop]]\\r\\n[JS]if ([[LayerNumber]]==1||[[LayerNumber]]%100==0) output=\\\"G4 P2000\\\";[/JS]\\r\\n[JS]if ([[LayerNumber]]==1||[[LayerNumber]]%100==0) output=\\\"[[PressureWrite 1]]\\\";[/JS]";
        public string ShieldDuringCure { get; set; } = string.Empty;
        public string ShieldStart { get; set; } = string.Empty;
        public string ShieldResume { get; set; } = string.Empty;
        public string ShieldFinish { get; set; } = string.Empty;
        public string LaserCode { get; set; } = string.Empty;
        public string ShutterOpenGcode { get; set; } = string.Empty;
        public string ShutterCloseGcode { get; set; } = string.Empty;
        public string SeparationDetection { get; set; } = string.Empty;
        public string ResinLevelDetection { get; set; } = string.Empty;
        public string CrashDetection { get; set; } = string.Empty;
        public string DynamicWait { get; set; } = string.Empty;
        public float SlowSectionHeight { get; set; } = 7;
        public uint SlowSectionStepWait { get; set; } = 40;
        public uint JumpPerLayer { get; set; }
        public string DynamicWaitAfterLift { get; set; } = string.Empty;
        public string DynamicLift { get; set; } = string.Empty;
        public float JumpHeight { get; set; }
        public float LowQualityCureTime { get; set; }
        public float LowQualitySkipPerLayer { get; set; }
        public float XYResPerc { get; set; }

        public override string ToString()
        {
            return $"{nameof(ResinID)}: {ResinID}, {nameof(ProfileID)}: {ProfileID}, {nameof(Title)}: {Title}, {nameof(Desc)}: {Desc}, {nameof(Color)}: {Color}, {nameof(ResinPrice)}: {ResinPrice}, {nameof(OptimumTemperature)}: {OptimumTemperature}, {nameof(Depth)}: {Depth}, {nameof(SupportTopWait)}: {SupportTopWait}, {nameof(SupportWaitHeight)}: {SupportWaitHeight}, {nameof(SupportDepth)}: {SupportDepth}, {nameof(SupportWaitBeforePrint)}: {SupportWaitBeforePrint}, {nameof(SupportWaitAfterPrint)}: {SupportWaitAfterPrint}, {nameof(TransitionalLayer)}: {TransitionalLayer}, {nameof(Updated)}: {Updated}, {nameof(CustomValues)}: {CustomValues}, {nameof(Type)}: {Type}, {nameof(ZStepWait)}: {ZStepWait}, {nameof(TopWait)}: {TopWait}, {nameof(WaitHeight)}: {WaitHeight}, {nameof(CureTime)}: {CureTime}, {nameof(WaitBeforePrint)}: {WaitBeforePrint}, {nameof(WaitAfterPrint)}: {WaitAfterPrint}, {nameof(SupportCureTime)}: {SupportCureTime}, {nameof(SupportLayerNumber)}: {SupportLayerNumber}, {nameof(AdaptSlicing)}: {AdaptSlicing}, {nameof(AdaptSlicingMin)}: {AdaptSlicingMin}, {nameof(AdaptSlicingMax)}: {AdaptSlicingMax}, {nameof(SupportOffset)}: {SupportOffset}, {nameof(Offset)}: {Offset}, {nameof(FillColor)}: {FillColor}, {nameof(BlankColor)}: {BlankColor}, {nameof(DimAmount)}: {DimAmount}, {nameof(DimWall)}: {DimWall}, {nameof(DimSkip)}: {DimSkip}, {nameof(PixelDiming)}: {PixelDiming}, {nameof(HatchingType)}: {HatchingType}, {nameof(ElephantMidExposure)}: {ElephantMidExposure}, {nameof(ElephantType)}: {ElephantType}, {nameof(ElephantAmount)}: {ElephantAmount}, {nameof(ElephantWall)}: {ElephantWall}, {nameof(ElephantLayers)}: {ElephantLayers}, {nameof(HatchingWall)}: {HatchingWall}, {nameof(HatchingGap)}: {HatchingGap}, {nameof(HatchingOuterWall)}: {HatchingOuterWall}, {nameof(HatchingTopCap)}: {HatchingTopCap}, {nameof(HatchingBottomCap)}: {HatchingBottomCap}, {nameof(MultiCureGap)}: {MultiCureGap}, {nameof(AntiAlias)}: {AntiAlias}, {nameof(AntiAlias3D)}: {AntiAlias3D}, {nameof(AntiAlias3DDistance)}: {AntiAlias3DDistance}, {nameof(AntiAlias3DMin)}: {AntiAlias3DMin}, {nameof(AntiAliasThreshold)}: {AntiAliasThreshold}, {nameof(ImageRotate)}: {ImageRotate}, {nameof(IgnoreMask)}: {IgnoreMask}, {nameof(XYRes)}: {XYRes}, {nameof(YRes)}: {YRes}, {nameof(ZResPerc)}: {ZResPerc}, {nameof(DynamicCureTime)}: {DynamicCureTime}, {nameof(DynamicSpeed)}: {DynamicSpeed}, {nameof(ShieldBeforeLayer)}: {ShieldBeforeLayer}, {nameof(ShieldAfterLayer)}: {ShieldAfterLayer}, {nameof(ShieldDuringCure)}: {ShieldDuringCure}, {nameof(ShieldStart)}: {ShieldStart}, {nameof(ShieldResume)}: {ShieldResume}, {nameof(ShieldFinish)}: {ShieldFinish}, {nameof(LaserCode)}: {LaserCode}, {nameof(ShutterOpenGcode)}: {ShutterOpenGcode}, {nameof(ShutterCloseGcode)}: {ShutterCloseGcode}, {nameof(SeparationDetection)}: {SeparationDetection}, {nameof(ResinLevelDetection)}: {ResinLevelDetection}, {nameof(CrashDetection)}: {CrashDetection}, {nameof(DynamicWait)}: {DynamicWait}, {nameof(SlowSectionHeight)}: {SlowSectionHeight}, {nameof(SlowSectionStepWait)}: {SlowSectionStepWait}, {nameof(JumpPerLayer)}: {JumpPerLayer}, {nameof(DynamicWaitAfterLift)}: {DynamicWaitAfterLift}, {nameof(DynamicLift)}: {DynamicLift}, {nameof(JumpHeight)}: {JumpHeight}, {nameof(LowQualityCureTime)}: {LowQualityCureTime}, {nameof(LowQualitySkipPerLayer)}: {LowQualitySkipPerLayer}, {nameof(XYResPerc)}: {XYResPerc}";
        }

        public void Update()
        {
            Updated = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }
    }

    public sealed class NanoDLPInfoItem
    {
        public float TotalSolidArea { get; set; }

        public float LargestArea { get; set; }

        public float SmallestArea { get; set; }

        public float MinX { get; set; }

        public float MinY { get; set; }

        public float MaxX { get; set; }

        public float MaxY { get; set; }

        public uint AreaCount { get; set; } = 1;

        public override string ToString()
        {
            return $"{nameof(TotalSolidArea)}: {TotalSolidArea}, {nameof(LargestArea)}: {LargestArea}, {nameof(SmallestArea)}: {SmallestArea}, {nameof(MinX)}: {MinX}, {nameof(MinY)}: {MinY}, {nameof(MaxX)}: {MaxX}, {nameof(MaxY)}: {MaxY}, {nameof(AreaCount)}: {AreaCount}";
        }
    }
    #endregion

    #region Members
    private string? _temporaryStlFile;
    private byte[]? _3dMetaBytes;
    #endregion

    #region Properties

    public NanoDLPMetaManifest MetaManifest { get; set; } = new ();
    public NanoDLPSlicerManifest SlicerManifest { get; set; } = new ();
    public NanoDLPPlateManifest PlateManifest { get; set; } = new ();
    public NanoDLPProfileManifest ProfileManifest { get; set; } = new ();
    public NanoDLPProfileManifest? OverrideManifest { get; set; }

    public NanoDLPInfoItem[] InfoManifest { get; set; } = [];

    public override FileFormatType FileType => FileFormatType.Archive;

    public override string ConvertMenuGroup => "NanoDLP";

    public override FileExtension[] FileExtensions { get; } =
    [
        new(typeof(NanoDLPFile), "nanodlp", "NanoDLP (Mono)"),
        new(typeof(NanoDLPFile), "rgb.nanodlp", "NanoDLP (RGB)", false)
    ];

    public override PrintParameterModifier[] PrintParameterModifiers
    {
        get
        {
            if (OverrideManifest is null) return base.PrintParameterModifiers;
            return
            [
                PrintParameterModifier.BottomLayerCount,
                PrintParameterModifier.TransitionLayerCount,

                PrintParameterModifier.BottomWaitTimeBeforeCure,
                PrintParameterModifier.WaitTimeBeforeCure,

                PrintParameterModifier.BottomExposureTime,
                PrintParameterModifier.ExposureTime,

                PrintParameterModifier.BottomWaitTimeAfterCure,
                PrintParameterModifier.WaitTimeAfterCure,

                PrintParameterModifier.BottomLiftHeight,
                PrintParameterModifier.LiftHeight,

                PrintParameterModifier.BottomWaitTimeAfterLift,
                PrintParameterModifier.WaitTimeAfterLift,

                PrintParameterModifier.BottomLightPWM,
                PrintParameterModifier.LightPWM
            ];

        }
    }

    public override Size[] ThumbnailsOriginalSize { get; } =
    [
        new(607, 487)
    ];

    public override uint[] AvailableVersions { get; } = [1, 2];

    public override uint DefaultVersion => 2;

    public override uint Version
    {
        get => (uint)MetaManifest.FormatVersion;
        set
        {
            base.Version = value;
            MetaManifest.FormatVersion = (int)base.Version;
        }
    }

    public override uint ResolutionX
    {
        get => SlicerManifest.PWidth;
        set
        {
            base.ResolutionX = SlicerManifest.PWidth = (ushort)value;
            SlicerManifest.XOffset = value / 2f;
            SlicerManifest.XPixelSize = PixelWidth;
        }
    }

    public override uint ResolutionY
    {
        get => SlicerManifest.PHeight;
        set
        {
            base.ResolutionY = SlicerManifest.PHeight = (ushort)value;
            SlicerManifest.YOffset = value / 2f;
            SlicerManifest.YPixelSize = PixelHeight;
        }
    }

    public override float DisplayWidth
    {
        get => RoundDisplaySize(ResolutionX * SlicerManifest.XPixelSize);
        set
        {
            base.DisplayWidth = RoundDisplaySize(value);
            SlicerManifest.XPixelSize = value > 0 && ResolutionX > 0 ? MathF.Round(value / ResolutionX, 4) : 0;
        }
    }

    public override float DisplayHeight
    {
        get => RoundDisplaySize(ResolutionY * SlicerManifest.YPixelSize);
        set
        {
            base.DisplayHeight = RoundDisplaySize(value);
            SlicerManifest.YPixelSize = value > 0 && ResolutionY > 0 ? MathF.Round(value / ResolutionY, 4) : 0;
        }
    }

    /*public override float MachineZ
    {
        get => MetaManifest.MachineZ > 0 ? MetaManifest.MachineZ : base.MachineZ;
        set
        {
            MetaManifest.MachineZ = value;
            RaisePropertyChanged();
        }
    }*/

    public override FlipDirection DisplayMirror
    {
        get => SlicerManifest.ImageMirror == 0 ? FlipDirection.None : FlipDirection.Horizontally;
        set
        {
            SlicerManifest.ImageMirror = value == FlipDirection.None ? byte.MinValue : (byte)1;
            RaisePropertyChanged();
        }
    }

    public override float LayerHeight
    {
        get => Layer.RoundHeight((SlicerManifest.Thickness > 0 ? SlicerManifest.Thickness : ProfileManifest.Depth) / 1000f);
        set
        {
            SlicerManifest.Thickness = SlicerManifest.SupportDepth = ProfileManifest.Depth = ProfileManifest.SupportDepth = Layer.RoundHeight(value * 1000f);
            base.LayerHeight = value;
        }
    }

    public override byte AntiAliasing
    {
        get => OverrideManifest?.AntiAlias ?? ProfileManifest.AntiAlias;
        set
        {
            if (OverrideManifest is not null)
            {
                OverrideManifest.AntiAlias = Math.Clamp(value, (byte)1, (byte)16);
            }
            base.AntiAliasing = SlicerManifest.AntiAlias = ProfileManifest.AntiAlias = Math.Clamp(value, (byte)1, (byte)16);
        }
    }


    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = SlicerManifest.LayerCount = PlateManifest.LayersCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => OverrideManifest?.SupportLayerNumber ?? ProfileManifest.SupportLayerNumber;
        set
        {
            if (OverrideManifest is not null)
            {
                OverrideManifest.SupportLayerNumber = value;
            }

            SlicerManifest.SupportLayerNumber = ProfileManifest.SupportLayerNumber = value;
            base.BottomLayerCount = value;
        }
    }

    public override TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Firmware;

    public override ushort TransitionLayerCount
    {
        get => OverrideManifest?.TransitionalLayer ?? ProfileManifest.TransitionalLayer;
        set
        {
            if (OverrideManifest is not null)
            {
                base.TransitionLayerCount = OverrideManifest.TransitionalLayer = (ushort)Math.Min(value, MaximumPossibleTransitionLayerCount);
            }
            else
            {
                base.TransitionLayerCount = ProfileManifest.TransitionalLayer = (ushort)Math.Min(value, MaximumPossibleTransitionLayerCount);
            }

        }
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => OverrideManifest?.SupportWaitBeforePrint ?? ProfileManifest.SupportWaitBeforePrint;
        set
        {
            if (OverrideManifest is not null)
            {
                base.BottomWaitTimeBeforeCure = OverrideManifest.SupportWaitBeforePrint = MathF.Round(value, 2);
            }
            else
            {
                base.BottomWaitTimeBeforeCure = ProfileManifest.SupportWaitBeforePrint = MathF.Round(value, 2);
            }
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => OverrideManifest?.WaitBeforePrint ?? ProfileManifest.WaitBeforePrint;
        set
        {
            if (OverrideManifest is not null)
            {
                base.WaitTimeBeforeCure = OverrideManifest.WaitBeforePrint = MathF.Round(value, 2);
            }
            else
            {
                base.WaitTimeBeforeCure = ProfileManifest.WaitBeforePrint = MathF.Round(value, 2);
            }
        }
    }

    public override float BottomExposureTime
    {
        get => OverrideManifest?.SupportCureTime ?? ProfileManifest.SupportCureTime;
        set
        {
            if (OverrideManifest is not null)
            {
                base.BottomExposureTime = OverrideManifest.SupportCureTime = MathF.Round(value, 2);
            }
            else
            {
                base.BottomExposureTime = ProfileManifest.SupportCureTime = MathF.Round(value, 2);
            }
        }
    }

    public override float BottomWaitTimeAfterCure
    {
        get => OverrideManifest?.SupportWaitAfterPrint ?? ProfileManifest.SupportWaitAfterPrint;
        set
        {
            if (OverrideManifest is not null)
            {
                base.BottomWaitTimeAfterCure = OverrideManifest.SupportWaitAfterPrint = MathF.Round(value, 2);
            }
            else
            {
                base.BottomWaitTimeAfterCure = ProfileManifest.SupportWaitAfterPrint = MathF.Round(value, 2);
            }
        }
    }

    public override float WaitTimeAfterCure
    {
        get => OverrideManifest?.WaitAfterPrint ?? ProfileManifest.WaitAfterPrint;
        set
        {
            if (OverrideManifest is not null)
            {
                base.WaitTimeAfterCure = OverrideManifest.WaitAfterPrint = MathF.Round(value, 2);
            }
            else
            {
                base.WaitTimeAfterCure = ProfileManifest.WaitAfterPrint = MathF.Round(value, 2);
            }
        }
    }

    public override float ExposureTime
    {
        get => OverrideManifest?.CureTime ?? ProfileManifest.CureTime;
        set
        {
            if (OverrideManifest is not null)
            {
                base.ExposureTime = OverrideManifest.CureTime = MathF.Round(value, 2);
            }
            else
            {
                base.ExposureTime = ProfileManifest.CureTime = MathF.Round(value, 2);
            }
        }
    }

    public override float BottomLiftHeight
    {
        get => OverrideManifest?.SupportWaitHeight ?? ProfileManifest.SupportWaitHeight;
        set
        {
            if (OverrideManifest is not null)
            {
                base.BottomLiftHeight = OverrideManifest.SupportWaitHeight = MathF.Round(value, 2);
            }
            else
            {
                base.BottomLiftHeight = ProfileManifest.SupportWaitHeight = MathF.Round(value, 2);
            }
        }
    }

    public override float LiftHeight
    {
        get => OverrideManifest?.WaitHeight ?? ProfileManifest.WaitHeight;
        set
        {
            if (OverrideManifest is not null)
            {
                base.LiftHeight = OverrideManifest.WaitHeight = MathF.Round(value, 2);
            }
            else
            {
                base.LiftHeight = ProfileManifest.WaitHeight = MathF.Round(value, 2);
            }
        }
    }

    public override float BottomWaitTimeAfterLift
    {
        get => OverrideManifest?.SupportTopWait ?? ProfileManifest.SupportTopWait;
        set
        {
            if (OverrideManifest is not null)
            {
                base.BottomWaitTimeAfterLift = OverrideManifest.SupportTopWait = MathF.Round(value, 2);
            }
            else
            {
                base.BottomWaitTimeAfterLift = ProfileManifest.SupportTopWait = MathF.Round(value, 2);
            }
        }
    }

    public override float WaitTimeAfterLift
    {
        get => OverrideManifest?.TopWait ?? ProfileManifest.TopWait;
        set
        {
            if (OverrideManifest is not null)
            {
                base.WaitTimeAfterLift = OverrideManifest.TopWait = MathF.Round(value, 2);
            }
            else
            {
                base.WaitTimeAfterLift = ProfileManifest.TopWait = MathF.Round(value, 2);
            }
        }
    }

    public override byte BottomLightPWM
    {
        get => LightPWM;
        set
        {
            LightPWM = value;
            base.BottomLightPWM = value;
        }
    }

    public override byte LightPWM
    {
        get
        {
            if (OverrideManifest is not null && ProfileManifest.CustomValues.TryGetValue("UvPwmValue", out var pwmValueStr) && float.TryParse(pwmValueStr, CultureInfo.InvariantCulture, out var pwmValue)) return (byte)Math.Round(pwmValue * byte.MaxValue, MidpointRounding.AwayFromZero);
            if (ProfileManifest.CustomValues.TryGetValue("UvPwmValue", out var pwmValueStr2) && float.TryParse(pwmValueStr2, CultureInfo.InvariantCulture, out var pwmValue2)) return (byte)Math.Round(pwmValue2 * byte.MaxValue, MidpointRounding.AwayFromZero);
            return base.LightPWM;
        }
        set
        {
            if (OverrideManifest is not null)
            {
                if (OverrideManifest.CustomValues.ContainsKey("UvPwmValue"))
                {
                    OverrideManifest.CustomValues["UvPwmValue"] = Math.Round(value / 255f, 2).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    OverrideManifest.CustomValues.TryAdd("UvPwmValue", Math.Round(value / 255f, 2).ToString(CultureInfo.InvariantCulture));
                }
            }
            else
            {
                if (ProfileManifest.CustomValues.ContainsKey("UvPwmValue"))
                {
                    ProfileManifest.CustomValues["UvPwmValue"] = Math.Round(value / 255f, 2).ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    ProfileManifest.CustomValues.TryAdd("UvPwmValue", Math.Round(value / 255f, 2).ToString(CultureInfo.InvariantCulture));
                }
            }

            base.LightPWM = value;
        }
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            PlateManifest.PrintEst = PlateManifest.PrintTime = (uint)base.PrintTime;
        }
    }

    public override string MachineName
    {
        get => ProfileManifest.Title;
        set => base.MachineName = ProfileManifest.Title = value;
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            PlateManifest.TotalSolidArea = base.MaterialMilliliters;
        }
    }

    public override object[] Configs =>
    [
        MetaManifest,
        SlicerManifest,
        PlateManifest,
        ProfileManifest
    ];

    #endregion

    #region Constructor

    public NanoDLPFile()
    {
        _layerImageFormat = ImageFormat.Png24RgbAA;
    }
    #endregion

    #region Methods

    public override void Clear()
    {
        base.Clear();
        _temporaryStlFile = null;
        _3dMetaBytes = null;
        OverrideManifest = null;
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        PlateManifest.Update();
        ProfileManifest.Update();

        PlateManifest.Path = FilenameNoExt;

        SlicerManifest.Boundary.ZMin = PlateManifest.ZMin = 0;
        SlicerManifest.Boundary.ZMax = PlateManifest.ZMax = PrintHeight;

        if (DisplayWidth > 0 && DisplayHeight > 0)
        {
            var rect = BoundingRectangleMillimeters;
            rect.Offset(DisplayWidth / -2f, DisplayHeight / -2f);

            SlicerManifest.Boundary.XMin = PlateManifest.XMin = MathF.Round(rect.X, 4);
            SlicerManifest.Boundary.XMax = PlateManifest.XMax = MathF.Round(rect.Right, 4);
            SlicerManifest.Boundary.YMin = PlateManifest.YMin = MathF.Round(rect.Y, 4);
            SlicerManifest.Boundary.YMax = PlateManifest.YMax = MathF.Round(rect.Bottom, 4);
        }

        var thumbnail = GetLargestThumbnail();
        if (thumbnail is not null)
        {
            SlicerManifest.PreviewWidth = (uint)thumbnail.Width;
            SlicerManifest.PreviewHeight = (uint)thumbnail.Height;
        }

        if (!isPartialEncode)
        {
            if (FileEndsWith(".rgb.nanodlp"))
            {
                LayerImageFormat = ImageFormat.Png24;
            }
        }
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);

        SlicerManifest.DisplayController = LayerImageFormat is ImageFormat.Png8 or ImageFormat.Png24RgbAA or ImageFormat.Png24RgbAA ? (byte)1 : byte.MinValue;

        outputFile.CreateEntryFromSerializeJson(MetaManifestFileName, MetaManifest, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(SlicerManifestFileName, SlicerManifest, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(PlateManifestFileName, PlateManifest, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(ProfileManifestFileName, ProfileManifest, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);


        progress.Reset("Calculating layers info", LayerCount);
        InfoManifest = new NanoDLPInfoItem[LayerCount];

        var pixelArea = PixelArea;

       Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), i =>
        {
            var layer = this[i];

            var item = new NanoDLPInfoItem
            {
                MinX = layer.BoundingRectangle.X,
                MinY = layer.BoundingRectangle.Y,
                MaxX = layer.BoundingRectangle.Right,
                MaxY = layer.BoundingRectangle.Bottom,
            };

            if (pixelArea > 0)
            {
                var contours = layer.Contours;
                //item.TotalSolidArea =  MathF.Round((float)contours.TotalSolidArea * pixelArea, 4, MidpointRounding.AwayFromZero);
                item.TotalSolidArea = layer.MaterialMilliliters;
                item.SmallestArea = MathF.Round((float)contours.MinSolidArea * pixelArea, 4, MidpointRounding.AwayFromZero);
                item.LargestArea = MathF.Round((float)contours.MaxSolidArea * pixelArea, 4, MidpointRounding.AwayFromZero);
                item.AreaCount = (uint)contours.ExternalContoursCount;
            }

            InfoManifest[i] = item;
            progress.LockAndIncrement();
        });

        outputFile.CreateEntryFromSerializeJson(InfoManifestFileName, InfoManifest, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);

        progress.Reset("Copying original stl file");
        // Preserve the original stl file
        if (!string.IsNullOrWhiteSpace(_temporaryStlFile) && File.Exists(_temporaryStlFile))
        {
            outputFile.CreateEntryFromFile(_temporaryStlFile, StlFileName);
        }

        if (_3dMetaBytes is not null)
        {
            outputFile.CreateEntryFromContent(ThumbnailMetaFileName, _3dMetaBytes, ZipArchiveMode.Create);
        }

        EncodeThumbnailsInZip(outputFile, progress, ThumbnailFileName);
        EncodeLayersInZip(outputFile, IndexStartNumber.One, progress);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(MetaManifestFileName);
        if (entry is null) throw new FileLoadException($"Unable to find the \"{MetaManifestFileName}\" file", FileFullPath);
        try
        {
            using var stream = entry.Open();
            MetaManifest = JsonSerializer.Deserialize<NanoDLPMetaManifest>(stream)!;
        }
        catch (Exception e)
        {
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        entry = inputFile.GetEntry(SlicerManifestFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                SlicerManifest = JsonSerializer.Deserialize<NanoDLPSlicerManifest>(stream)!;
            }
            catch (Exception e)
            {
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        entry = inputFile.GetEntry(PlateManifestFileName);
        if (entry is null) throw new FileLoadException($"Unable to find the \"{PlateManifestFileName}\" file", FileFullPath);
        try
        {
            using var stream = entry.Open();
            PlateManifest = JsonSerializer.Deserialize<NanoDLPPlateManifest>(stream)!;
        }
        catch (Exception e)
        {
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        entry = inputFile.GetEntry(ProfileManifestFileName);
        if (entry is null) throw new FileLoadException($"Unable to find the \"{ProfileManifestFileName}\" file", FileFullPath);
        try
        {
            using var stream = entry.Open();
            ProfileManifest = JsonSerializer.Deserialize<NanoDLPProfileManifest>(stream)!;
        }
        catch (Exception e)
        {
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        entry = inputFile.GetEntry(OverrideManifestFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                OverrideManifest = JsonSerializer.Deserialize<NanoDLPProfileManifest>(stream)!;
            }
            catch (Exception e)
            {
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        entry = inputFile.GetEntry(InfoManifestFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                InfoManifest = JsonSerializer.Deserialize<NanoDLPInfoItem[]>(stream)!;
            }
            catch (Exception e)
            {
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        GCode = new GCodeBuilder();

        GCode.AppendComment(nameof(ProfileManifest.ShieldStart));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldStart)) GCode.AppendLine(ProfileManifest.ShieldStart);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShieldResume));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldResume)) GCode.AppendLine(ProfileManifest.ShieldResume);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShieldFinish));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldFinish)) GCode.AppendLine(ProfileManifest.ShieldFinish);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShieldBeforeLayer));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldBeforeLayer)) GCode.AppendLine(ProfileManifest.ShieldBeforeLayer);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShieldAfterLayer));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldAfterLayer)) GCode.AppendLine(ProfileManifest.ShieldAfterLayer);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShutterOpenGcode));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShutterOpenGcode)) GCode.AppendLine(ProfileManifest.ShutterOpenGcode);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShieldDuringCure));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShieldDuringCure)) GCode.AppendLine(ProfileManifest.ShieldDuringCure);
        GCode.AppendLine();

        GCode.AppendComment(nameof(ProfileManifest.ShutterCloseGcode));
        if (!string.IsNullOrWhiteSpace(ProfileManifest.ShutterCloseGcode)) GCode.AppendLine(ProfileManifest.ShutterCloseGcode);
        GCode.AppendLine();



        if (PlateManifest.LayersCount == 0)
        {
            uint layerCount = 0;

            foreach (var zipEntry in inputFile.Entries)
            {
                if (!zipEntry.Name.EndsWith(".png")) continue;
                var filename = Path.GetFileNameWithoutExtension(zipEntry.Name);
                if (!filename.AsValueEnumerable().All(char.IsDigit)) continue;
                if (!uint.TryParse(filename, out var layerIndex)) continue;
                layerCount = Math.Max(layerCount, layerIndex);
            }

            if (layerCount == 0)
            {
                Clear();
                throw new FileLoadException("Unable to detect layer images in the file", FileFullPath);
            }

            PlateManifest.LayersCount = layerCount;
        }

        // Preserve the original stl file
        entry = inputFile.GetEntry(StlFileName);
        if (entry is not null)
        {
            progress.Reset($"Preserving the {StlFileName}");
            _temporaryStlFile = PathExtensions.GetTemporaryFilePathWithExtension("stl", Path.GetFileNameWithoutExtension(StlFileName));
            entry.ExtractToFile(_temporaryStlFile, true);
        }

        entry = inputFile.GetEntry(ThumbnailMetaFileName);
        if (entry is not null)
        {
            _3dMetaBytes = entry.ToArray();
        }

        DecodeThumbnailsFromZip(inputFile, progress, ThumbnailFileName);
        Init(PlateManifest.LayersCount, DecodeType == FileDecodeType.Partial);

        if (LayerCount <= 0) return;

        // Must discover png depth grayscale or color
        if (DecodeType == FileDecodeType.Full)
        {
            LayerImageFormat = FetchImageFormat(inputFile, ImageFormat.Png24RgbAA);
            DecodeLayersFromZip(inputFile, IndexStartNumber.One, progress);
        }
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);

        //outputFile.CreateEntryFromSerializeJson(MetaManifestFileName, MetaManifest, ZipArchiveMode.Update);
        outputFile.CreateEntryFromSerializeJson(SlicerManifestFileName, SlicerManifest, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(PlateManifestFileName, PlateManifest, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(ProfileManifestFileName, ProfileManifest, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        if (OverrideManifest is not null) outputFile.CreateEntryFromSerializeJson(OverrideManifestFileName, OverrideManifest, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
    }
    #endregion
}