/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Converters;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class AnycubicZipFile : FileFormat
{
    #region Sub classes

    public sealed class SettingsManifest
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "3";

        [JsonPropertyName("machine_type")]
        public SettingsMachineType MachineType { get; set; } = new ();

        [JsonPropertyName("machine_extern")]
        public SettingsMachineExtern MachineExtern { get; set; } = new();

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(MachineType)}: {MachineType}, {nameof(MachineExtern)}: {MachineExtern}";
        }
    }

    public sealed class SettingsChildScreen
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("width")]
        public uint Width { get; set; }

        [JsonPropertyName("height")]
        public uint Height { get; set; }

        public SettingsChildScreen()
        {
        }

        public SettingsChildScreen(int x, int y, uint width, uint height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public sealed class SettingsMachineType
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "3";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "Unknown";

        [JsonPropertyName("key_suffix")]
        public string KeySuffix { get; set; } = "pwsz";

        [JsonPropertyName("key_image_format")]
        public string KeyImageFormat { get; set; } = "pwszImg";

        [JsonPropertyName("res_x")]
        public uint ResolutionX { get; set; }

        [JsonPropertyName("res_y")]
        public uint ResolutionY { get; set; }

        [JsonPropertyName("xy_pixel")]
        public float PixelWidthMicrons { get; set; }

        [JsonPropertyName("xy_pixel_y")]
        public float PixelHeightMicrons { get; set; }

        [JsonPropertyName("max_samples")]
        public byte MaxSamples { get; set; } = 16;

        [JsonPropertyName("property")]
        public ushort Properties { get; set; } = 119;

        [JsonPropertyName("print_xsize")]
        public float DisplayWidth { get; set; }

        [JsonPropertyName("print_ysize")]
        public float DisplayHeight { get; set; }

        [JsonPropertyName("print_zsize")]
        public float MachineZ { get; set; }

        [JsonPropertyName("max_file_version")]
        public uint MaxFileVersion { get; set; } = 518;

        [JsonPropertyName("prev_back_color")]  
        public float[] PreviewBackgroundColor { get; set; } = { 0.0f, 0.28f, 0.39f };

        [JsonPropertyName("prev_model_color")]
        public float[] ModelBackgroundColor { get; set; } = { 0.80f, 0.80f, 0.80f };

        [JsonPropertyName("prev_supports_color")]
        public float[] SupportsBackgroundColor { get; set; } = { 0.07f, 0.93f, 0.93f };

        [JsonPropertyName("prev_image_size")]
        public int[] PreviewImageSize { get; set; } = { 224, 168 };

        [JsonPropertyName("child_screen")]
        public SettingsChildScreen[] Screens { get; set; } = Array.Empty<SettingsChildScreen>();

        [JsonPropertyName("prev2_back_color")]
        public float[] Preview2BackgroundColor { get; set; } = { 0.08f, 0.11f, 0.16f };

        [JsonPropertyName("prev2_image_size")]
        public int[] Preview2ImageSize { get; set; } = { 336, 252 };

        [JsonPropertyName("raster_segments_capacity")]
        public uint RasterSegmentsCapacity { get; set; } = 100000;

        [JsonPropertyName("raster_antialiasing")]
        public byte RasterAntialiasing { get; set; } = 8;

        [JsonPropertyName("cloudprev_back_color")]
        public float[] CloudBackgroundColor { get; set; } = { 0.00f, 0.28f, 0.39f };

        [JsonPropertyName("cloudprev_imag_size")]
        public int[] CloudImageSize { get; set; } = { 800, 600 };

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(Name)}: {Name}, {nameof(KeySuffix)}: {KeySuffix}, {nameof(KeyImageFormat)}: {KeyImageFormat}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PixelWidthMicrons)}: {PixelWidthMicrons}, {nameof(PixelHeightMicrons)}: {PixelHeightMicrons}, {nameof(MaxSamples)}: {MaxSamples}, {nameof(Properties)}: {Properties}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(MachineZ)}: {MachineZ}, {nameof(MaxFileVersion)}: {MaxFileVersion}, {nameof(PreviewBackgroundColor)}: {PreviewBackgroundColor}, {nameof(ModelBackgroundColor)}: {ModelBackgroundColor}, {nameof(SupportsBackgroundColor)}: {SupportsBackgroundColor}, {nameof(PreviewImageSize)}: {PreviewImageSize}, {nameof(Preview2BackgroundColor)}: {Preview2BackgroundColor}, {nameof(Preview2ImageSize)}: {Preview2ImageSize}, {nameof(RasterSegmentsCapacity)}: {RasterSegmentsCapacity}, {nameof(RasterAntialiasing)}: {RasterAntialiasing}, {nameof(CloudBackgroundColor)}: {CloudBackgroundColor}, {nameof(CloudImageSize)}: {CloudImageSize}";
        }
    }

    public sealed class SettingsMachineExtern
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "3";

        [JsonPropertyName("alias")]
        public string Alias { get; set; } = "Unknown";

        [JsonPropertyName("picture")]
        public string Picture { get; set; } = "Unknown.png";

        [JsonPropertyName("cloud_property")]
        public uint CloudProperty { get; set; }

        [JsonPropertyName("device_cn_code")]
        public string DeviceCnCode { get; set; } = string.Empty;

        [JsonPropertyName("factory_resins")]
        public object[] FactoryResins { get; set; } = Array.Empty<object>();

        [JsonPropertyName("user_resins")]
        public object[] UserResins { get; set; } = Array.Empty<object>();

        [JsonPropertyName("active_resins")]
        public string[] ActiveResins { get; set; } = { "default_resin" };

        [JsonPropertyName("firmware_calc_print_time")]
        public byte FirmwareCalcPrintTime { get; set; } = 1;

        [JsonPropertyName("firmware_calc_print_time_paras")]
        public object FirmwareCalcPrintParameters { get; set; } = new();

        [JsonPropertyName("firmware_calc_exp_time_paras")]
        public object FirmwareCalcExposureTimeParameters { get; set; } = new();

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(Alias)}: {Alias}, {nameof(Picture)}: {Picture}, {nameof(CloudProperty)}: {CloudProperty}, {nameof(DeviceCnCode)}: {DeviceCnCode}, {nameof(FactoryResins)}: {FactoryResins}, {nameof(UserResins)}: {UserResins}, {nameof(ActiveResins)}: {ActiveResins}, {nameof(FirmwareCalcPrintTime)}: {FirmwareCalcPrintTime}, {nameof(FirmwareCalcPrintParameters)}: {FirmwareCalcPrintParameters}, {nameof(FirmwareCalcExposureTimeParameters)}: {FirmwareCalcExposureTimeParameters}";
        }
    }

    public sealed class LayerManifest
    {
        [JsonPropertyName("exposure_time")]
        public float ExposureTime { get; set; }

        [JsonPropertyName("layer_index")]
        public uint LayerIndex { get; set; }

        [JsonPropertyName("layer_minheight")]
        public float LayerMinHeight { get; set; }

        [JsonPropertyName("layer_thickness")]
        public float LayerThickness { get; set; }

        [JsonPropertyName("zup_height")]
        public float LiftHeight { get; set; }

        [JsonPropertyName("zup_speed")]
        public float LiftSpeed { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(ExposureTime)}: {ExposureTime}, {nameof(LayerIndex)}: {LayerIndex}, {nameof(LayerMinHeight)}: {LayerMinHeight}, {nameof(LayerThickness)}: {LayerThickness}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}";
        }
    }

    public sealed class LayersControllerManifest
    {
        [JsonPropertyName("count")]
        public int Count => Layers.Length;

        [JsonPropertyName("paras")]
        public LayerManifest[] Layers { get; set; } = Array.Empty<LayerManifest>();
    }
    
    public sealed class PrintInfoManifest
    {
        [JsonPropertyName("cost")]
        public float Cost { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "€";

        [JsonPropertyName("print_time")]
        public float PrintTime { get; set; }

        [JsonPropertyName("volume")]
        public float Volume { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Cost)}: {Cost}, {nameof(Currency)}: {Currency}, {nameof(PrintTime)}: {PrintTime}, {nameof(Volume)}: {Volume}";
        }
    }


    public sealed class SoftwareInfoManifest
    {
        [JsonPropertyName("mark")]
        public string Mark { get; set; } = About.Software;

        [JsonPropertyName("opengl")]
        public string OpenGL { get; set; } = "3.3-CoreProfile";

        [JsonPropertyName("os")]
        public string OS { get; set; } = RuntimeInformation.RuntimeIdentifier;

        [JsonPropertyName("Version")]
        public string Version { get; set; } = About.VersionString;

        public override string ToString()
        {
            return
                $"{nameof(Mark)}: {Mark}, {nameof(OpenGL)}: {OpenGL}, {nameof(OS)}: {OS}, {nameof(Version)}: {Version}";
        }

        public void Update()
        {
            Mark = About.Software;
            Version = About.VersionString;
            OS = RuntimeInformation.RuntimeIdentifier;
        }
    }

    public sealed class SceneLayerDef
    {
        [FieldOrder(0)] public float Height { get; set; }

        [FieldOrder(1)] public float Area { get; set; }

        [FieldOrder(2)] public float XStartBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(3)] public float YStartBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(4)] public float XEndBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(5)] public float YEndBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(6)] public uint ObjectCount { get; set; }

        [FieldOrder(7)] public float MaxContourArea { get; set; }

        [FieldOrder(8)] [FieldCount(8)] public uint[] Padding { get; set; } = new uint[8];

        public override string ToString()
        {
            return
                $"{nameof(Height)}: {Height}, {nameof(Area)}: {Area}, {nameof(XStartBoundingRectangleOffsetFromCenter)}: {XStartBoundingRectangleOffsetFromCenter}, {nameof(YStartBoundingRectangleOffsetFromCenter)}: {YStartBoundingRectangleOffsetFromCenter}, {nameof(XEndBoundingRectangleOffsetFromCenter)}: {XEndBoundingRectangleOffsetFromCenter}, {nameof(YEndBoundingRectangleOffsetFromCenter)}: {YEndBoundingRectangleOffsetFromCenter}, {nameof(ObjectCount)}: {ObjectCount}, {nameof(MaxContourArea)}: {MaxContourArea}, {nameof(Padding)}: {Padding}";
        }
    }

    public sealed class SceneManifest
    {
        public const string MAGIC = "ANYCUBIC-PWSZ";

        [FieldOrder(0)]
        [FieldLength(16)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string Magic { get; set; } = MAGIC;

        [FieldOrder(1)]
        [FieldLength(64)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string Software { get; set; } = About.SoftwareWithVersionArch;

        /// <summary>
        /// 1: Pure Binary<br/>
        /// 2: FPGA Debug<br/>
        /// 3: FPGA Release. Firmware set to 3 (Default)
        /// </summary>
        [FieldOrder(2)] public uint BinaryType { get; set; } = 3;

        [FieldOrder(3)] public uint Version { get; set; } = 1;

        [FieldOrder(4)] public uint SliceType { get; set; }

        /// <summary>
        /// 0: mm<br/>
        /// 1: cm<br/>
        /// 2: m
        /// </summary>
        [FieldOrder(5)] public uint ModelUnit { get; set; }

        [FieldOrder(6)] public float PointRatio { get; set; } = 1f;

        [FieldOrder(7)] public uint LayerCount { get; set; }

        [FieldOrder(8)] public float XStartBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(9)] public float YStartBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(10)] public float ZMin { get; set; }

        [FieldOrder(11)] public float XEndBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(12)] public float YEndBoundingRectangleOffsetFromCenter { get; set; }

        [FieldOrder(13)] public float ZMax { get; set; }

        /// <summary>
        /// Some status flags of the scene model
        /// </summary>
        [FieldOrder(14)] public uint ModelStats { get; set; }

        [FieldOrder(15)] [FieldCount(64)] public uint[] Padding { get; set; } = new uint[64];

        [FieldOrder(16)] [FieldLength(4)] public string Separator { get; set; } = "<---";

        [FieldOrder(17)] public uint LayerDefCount { get; set; }

        [FieldOrder(18)][FieldCount(nameof(LayerDefCount))] public SceneLayerDef[] LayersDef { get; set; } = Array.Empty<SceneLayerDef>();

        [FieldOrder(19)] [FieldLength(4)] public string EndMarker { get; set; } = "--->";

        public void Update(FileFormat slicerFile)
        {
            Software = About.SoftwareWithVersionArch;
            var rect = slicerFile.BoundingRectangleMillimeters;
            rect.Offset(slicerFile.DisplayWidth / -2f, slicerFile.DisplayHeight / -2f);
            XStartBoundingRectangleOffsetFromCenter = RoundDisplaySize(rect.X);
            YStartBoundingRectangleOffsetFromCenter = RoundDisplaySize(rect.Y);
            XEndBoundingRectangleOffsetFromCenter = RoundDisplaySize(rect.Right);
            YEndBoundingRectangleOffsetFromCenter = RoundDisplaySize(rect.Bottom);

            ZMin = 0;
            ZMax = slicerFile.PrintHeight;
        }

        public override string ToString()
        {
            return
                $"{nameof(Magic)}: {Magic}, {nameof(Software)}: {Software}, {nameof(BinaryType)}: {BinaryType}, {nameof(Version)}: {Version}, {nameof(SliceType)}: {SliceType}, {nameof(ModelUnit)}: {ModelUnit}, {nameof(PointRatio)}: {PointRatio}, {nameof(LayerCount)}: {LayerCount}, {nameof(XStartBoundingRectangleOffsetFromCenter)}: {XStartBoundingRectangleOffsetFromCenter}, {nameof(YStartBoundingRectangleOffsetFromCenter)}: {YStartBoundingRectangleOffsetFromCenter}, {nameof(ZMin)}: {ZMin}, {nameof(XEndBoundingRectangleOffsetFromCenter)}: {XEndBoundingRectangleOffsetFromCenter}, {nameof(YEndBoundingRectangleOffsetFromCenter)}: {YEndBoundingRectangleOffsetFromCenter}, {nameof(ZMax)}: {ZMax}, {nameof(ModelStats)}: {ModelStats}, {nameof(Padding)}: {Padding}, {nameof(Separator)}: {Separator}, {nameof(LayerDefCount)}: {LayerDefCount}, {nameof(LayersDef)}: {LayersDef}, {nameof(EndMarker)}: {EndMarker}";
        }
    }
    #endregion

    #region Enums
    public enum AnycubicZipRleFormat
    {
        PW0,
        PWSZ
    }
    #endregion

    #region Constants
    private const string SettingsFileName = "anycubic_photon_resins.pwsp";
    private const string LayersFileName = "layers_controller.conf";
    private const string PrintInfoFileName = "print_info.json";
    private const string SoftwareInfoFileName = "software_info.conf";
    private const string SceneFileName = "scene.slice";
    #endregion

    #region Properties
    public SettingsManifest Settings { get; set; } = new ();
    public PrintInfoManifest PrintInfoSettings { get; set; } = new ();
    public LayersControllerManifest LayersSettings { get; set; } = new ();
    public SoftwareInfoManifest SoftwareInfoSettings { get; set; } = new ();
    public SceneManifest SceneSettings { get; set; } = new ();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override SpeedUnit FormatSpeedUnit => SpeedUnit.MillimetersPerSecond;

    public override PrintParameterModifier[] PrintParameterModifiers
    {
        get
        {
            return new[]
            {
                PrintParameterModifier.BottomLayerCount,
                PrintParameterModifier.TransitionLayerCount,

                PrintParameterModifier.BottomExposureTime,
                PrintParameterModifier.ExposureTime,

                PrintParameterModifier.BottomLiftHeight,
                PrintParameterModifier.BottomLiftSpeed,
                PrintParameterModifier.LiftHeight,
                PrintParameterModifier.LiftSpeed,
            };
        }
    }

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
    };

    public override string ConvertMenuGroup => "Anycubic Photon Workshop";

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(AnycubicZipFile), "pm4u", "Photon Mono 4 Ultra (PM4U)"),
        new(typeof(AnycubicZipFile), "pm7", "Photon Mono M7 (PM7)"),
        new(typeof(AnycubicZipFile), "pm7m", "Photon Mono M7 Max (PM7M)"),
        new(typeof(AnycubicZipFile), "pwsz", "Photon Mono M7 Pro (PWSZ)"),
        
    };

    /*public override uint[] AvailableVersions { get; } = { 1 };

    public override uint DefaultVersion => 1;

    public override uint Version
    {
        get => SceneSettings.Version;
        set
        {
            base.Version = value;
            SceneSettings.Version = base.Version;
        }
    }*/

    public override uint ResolutionX
    {
        get => Settings.MachineType.ResolutionX;
        set => base.ResolutionX = Settings.MachineType.ResolutionX = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => Settings.MachineType.ResolutionY;
        set => base.ResolutionY = Settings.MachineType.ResolutionY = (ushort)value;
    }

    public override float DisplayWidth
    {
        get => Settings.MachineType.DisplayWidth;
        set => base.DisplayWidth = Settings.MachineType.DisplayWidth = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => Settings.MachineType.DisplayHeight;
        set => base.DisplayHeight = Settings.MachineType.DisplayHeight = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get => Settings.MachineType.MachineZ;
        set => base.MachineZ = Settings.MachineType.MachineZ = value;
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = SceneSettings.LayerCount = SceneSettings.LayerDefCount = base.LayerCount;
    }

    public override string MachineName
    {
        get => Settings.MachineType.Name;
        set
        {
            Settings.MachineExtern.Picture = $"{value.Replace(" ", string.Empty)}.png";
            base.MachineName = Settings.MachineExtern.Alias = Settings.MachineType.Name = value;
        }
    }

    public override byte AntiAliasing
    {
        get => Settings.MachineType.RasterAntialiasing;
        set => base.AntiAliasing = Settings.MachineType.RasterAntialiasing = Math.Clamp(value, (byte)1, (byte)16);
    }

    public override Size[] ThumbnailsOriginalSize { get; } =
    {
        new(224, 168),
        new(336, 252)
    };


    public override object[] Configs => new object[] { 
        Settings, PrintInfoSettings, SoftwareInfoSettings, SceneSettings
    };

    #endregion

    #region Constructor
    public AnycubicZipFile()
    { }
    #endregion

    #region Methods

    private Mat DecodeLayerRle(AnycubicZipRleFormat format, byte[] encodedRle)
    {
        var mat = CreateMat();

        if (format == AnycubicZipRleFormat.PW0)
        {
            return AnycubicFile.DecodePW0(mat, encodedRle);
        }

        if (format == AnycubicZipRleFormat.PWSZ)
        {
            if (encodedRle.Length < 56) throw new FileLoadException("Invalid RLE data is shorter than 56 bytes.");
            

            if (encodedRle[0] != '{' || encodedRle[1] != '=' || encodedRle[2] != '=' || encodedRle[3] != '\0') 
                throw new FileLoadException($"Invalid RLE file start marker, expecting: {{==\0 got: {System.Text.Encoding.Default.GetString(encodedRle, 0, 4)}.");
            

            int index = 4;
            var area = BitExtensions.ToSingleLittleEndian(encodedRle, index); index += 4;
            var xMin = BitExtensions.ToSingleLittleEndian(encodedRle, index); index += 4;
            var yMin = BitExtensions.ToSingleLittleEndian(encodedRle, index); index += 4;
            var xMax = BitExtensions.ToSingleLittleEndian(encodedRle, index); index += 4;
            var yMax = BitExtensions.ToSingleLittleEndian(encodedRle, index); index += 4;
            var padding1 = BitExtensions.ToUIntLittleEndian(encodedRle, index); index += 4;
            var objectCount = BitExtensions.ToUIntLittleEndian(encodedRle, index); index += 4;

            if (encodedRle[index] != '[' || encodedRle[index+1] != '-' || encodedRle[index+2] != '-' || encodedRle[index+3] != '\0')
                throw new FileLoadException($"Invalid RLE coordinates start marker, expecting: [--\0 got: {System.Text.Encoding.Default.GetString(encodedRle, index, 4)}.");

            index += 4;

            var padding2 = BitExtensions.ToUIntLittleEndian(encodedRle, index); index += 4;
            var lineCount = BitExtensions.ToUIntLittleEndian(encodedRle, index); index += 4;
            var unknown1 = BitExtensions.ToUIntLittleEndian(encodedRle, index); index += 4;

            float halfDisplayX = DisplayWidth / 2f;
            float halfDisplayY = DisplayHeight / 2f;

            if (lineCount > 0)
            {
                for (int i = 0; i < lineCount; i++)
                {
                    var startX = BitExtensions.ToSingleLittleEndian(encodedRle, index) + halfDisplayX; index += 4;
                    var startY = BitExtensions.ToSingleLittleEndian(encodedRle, index) + halfDisplayY; index += 4;
                    var endX = BitExtensions.ToSingleLittleEndian(encodedRle, index) + halfDisplayX; index += 4;
                    var endY = BitExtensions.ToSingleLittleEndian(encodedRle, index) + halfDisplayY; index += 4;
                    var cw = encodedRle[index++];

                    var startPoint = DisplayToPixelPosition(startX, startY);
                    var endPoint = DisplayToPixelPosition(endX, endY);

                    //CvInvoke.Line(mat, startPoint, endPoint, EmguExtensions.WhiteColor);
                    CvInvoke.Line(mat, startPoint, endPoint, new MCvScalar((1+i)*15));
                }

                var boundingRectangle = CvInvoke.BoundingRectangle(mat);
                using var matRoi = new Mat(mat, boundingRectangle);
                using var contours = new EmguContours(matRoi, RetrType.Tree, ChainApproxMethod.ChainApproxSimple, boundingRectangle.Location);

                if (!contours.IsEmpty)
                {
                    using var newContours = new VectorOfVectorOfPoint();
                    foreach (var family in contours.Families)
                    {
                        newContours.Push(family.TraverseTree()
                            .Where(traverseFamily => traverseFamily.IsPositive) // Ignore all non-welcomers
                            .Select(traverseFamily => traverseFamily.Self.Vector).ToArray());
                    }
                    

                    /*for (var i = 0; i < contours.Vector.Size; i++)
                    {
                        var x = Random.Shared.Next(0, contours.Vector[i].Size);
                        CvInvoke.PutText(mat, contours.Hierarchy[i, EmguContour.HierarchyParent].ToString(), contours.Vector[i][0], FontFace.HersheyDuplex, 1, new MCvScalar(127), 1);
                    }*/

                    CvInvoke.DrawContours(mat, newContours, -1, EmguExtensions.WhiteColor, -1);

                    /*foreach (var family in contours.Families)
                    {
                        foreach (var me in family.TraverseTree())
                        {
                            var i = Random.Shared.Next(0, me.Self.Vector.Size);
                            CvInvoke.PutText(mat, me.Depth.ToString(), me.Self.Vector[0], FontFace.HersheyDuplex, 1, new MCvScalar(127), 1);
                        }
                    }*/
                }
            }

            if (encodedRle[index] != '-' || encodedRle[index + 1] != '-' || encodedRle[index + 2] != ']' || encodedRle[index + 3] != '\0')
                throw new FileLoadException($"Invalid RLE coordinates end marker, expecting: --]\0 got: {System.Text.Encoding.Default.GetString(encodedRle, index, 4)}.");
            
            index += 4;

            if (encodedRle[index] != '=' || encodedRle[index + 1] != '=' || encodedRle[index + 2] != '}' || encodedRle[index + 3] != '\0')
                throw new FileLoadException($"Invalid RLE file end marker, expecting: ==}}\0 got: {System.Text.Encoding.Default.GetString(encodedRle, index, 4)}.");

            return mat;
        }

        throw new NotSupportedException($"Unsupported RLE format: {format}");
    }

    private byte[] EncodeLayerRle(AnycubicZipRleFormat format, uint layerIndex)
    {
        if (format == AnycubicZipRleFormat.PW0)
        {
            using var mat = this[layerIndex].LayerMat;
            return AnycubicFile.EncodePW0(mat);
        }

        if (format == AnycubicZipRleFormat.PWSZ)
        {
            var layer = this[layerIndex];
            var rle = new List<byte>();

            rle.AddRange(new byte[]
            {
                (byte)'{', 
                (byte)'=', 
                (byte)'=', 
                0
            });

            var zeroUintArray = new byte[4];

            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].Area));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].XStartBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].YStartBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].XEndBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].YEndBoundingRectangleOffsetFromCenter));
            rle.AddRange(zeroUintArray);
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].ObjectCount));

            rle.AddRange(new byte[]
            {
                (byte)'[',
                (byte)'-',
                (byte)'-',
                0
            });

            rle.AddRange(zeroUintArray);

            float halfDisplayX = DisplayWidth / 2f;
            float halfDisplayY = DisplayHeight / 2f;

            uint lines = 0;

            var linesRle = new List<byte>();
            foreach (var family in layer.Contours.Families)
            {
                foreach (var contour in family.TraverseTreeAsEmguContour())
                {
                    if (contour.Count == 1)
                    {
                        var startPoint = PixelToDisplayPosition(contour[0]);
                        linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.X - halfDisplayX)));
                        linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.Y - halfDisplayY)));
                        linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.X - halfDisplayX)));
                        linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.Y - halfDisplayY)));
                        linesRle.Add(1);
                        lines++;
                    }
                    else
                    {
                        for (int i = 1; i < contour.Count; i++)
                        {
                            var startPoint = PixelToDisplayPosition(contour[i-1]);
                            var endPoint = PixelToDisplayPosition(contour[i]);
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.X - halfDisplayX)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.Y - halfDisplayY)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(endPoint.X - halfDisplayX)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(endPoint.Y - halfDisplayY)));
                            linesRle.Add(1);
                            lines++;
                        }

                        // Closing line
                        if (lines >= 3 && contour.IsClosed)
                        {
                            var startPoint = PixelToDisplayPosition(contour[^1]);
                            var endPoint = PixelToDisplayPosition(contour[0]);
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.X - halfDisplayX)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(startPoint.Y - halfDisplayY)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(endPoint.X - halfDisplayX)));
                            linesRle.AddRange(BitExtensions.ToBytesLittleEndian(RoundDisplaySize(endPoint.Y - halfDisplayY)));
                            linesRle.Add(1);
                            lines++;
                        }
                    }
                }
            }
            
            rle.AddRange(BitExtensions.ToBytesLittleEndian(lines));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(1u)); // Unknown

            rle.AddRange(linesRle);

            rle.AddRange(new byte[]
            {
                (byte)'-',
                (byte)'-',
                (byte)']',
                0
            });

            rle.AddRange(new byte[]
            {
                (byte)'=',
                (byte)'=',
                (byte)'}',
                0
            });

            return rle.ToArray();
        }

        throw new NotSupportedException($"Unsupported RLE format: {format}");
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        Settings.MachineType.PixelWidthMicrons = PixelWidthMicrons;
        Settings.MachineType.PixelHeightMicrons = PixelHeightMicrons;
        Settings.MachineType.PreviewImageSize = [ThumbnailsOriginalSize[0].Width, ThumbnailsOriginalSize[0].Height];
        Settings.MachineType.Preview2ImageSize = [ThumbnailsOriginalSize[1].Width, ThumbnailsOriginalSize[1].Height];

        if (Settings.MachineType.Screens.Length > 0)
        {
            Settings.MachineType.Screens[0].X = 0;
            Settings.MachineType.Screens[0].Y = 0;
            Settings.MachineType.Screens[0].Width = ResolutionX;
            Settings.MachineType.Screens[0].Height = ResolutionY;
        }
        else
        {
            Settings.MachineType.Screens = new[]
            {
                new SettingsChildScreen(0, 0, ResolutionX, ResolutionY)
            };
        }
        
        PrintInfoSettings.Cost = MaterialCost;
        PrintInfoSettings.PrintTime = PrintTime;
        PrintInfoSettings.Volume = Volume;
        SoftwareInfoSettings.Update();

        LayersSettings.Layers = new LayerManifest[LayerCount];
        float minHeight = 0;
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var layer = this[layerIndex];
            var relativeZ = layer.RelativePositionZ;
            LayersSettings.Layers[layerIndex] = new LayerManifest
            {
                ExposureTime = layer.ExposureTime,
                LayerIndex = layer.Index,
                LayerMinHeight = Layer.RoundHeight(minHeight),
                LayerThickness = relativeZ,
                LiftHeight = layer.LiftHeight,
                LiftSpeed = SpeedConverter.Convert(layer.LiftSpeed, CoreSpeedUnit, FormatSpeedUnit)
            };

            minHeight += relativeZ;
        }

        SceneSettings.Update(this);
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);

        EncodeAllThumbnailsInZip(outputFile, "preview_images/preview_{0}.png", progress);
        
        outputFile.CreateEntryFromSerializeJson(SettingsFileName, Settings, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(PrintInfoFileName, PrintInfoSettings, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(LayersFileName, LayersSettings, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(SoftwareInfoFileName, SoftwareInfoSettings, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);

        SceneSettings.LayersDef = new SceneLayerDef[LayerCount];
        var pixelArea = PixelArea;

        var encodeWith = AnycubicZipRleFormat.PW0; // pw0 better than my pmsz implementation
        var encodedRle = new byte[LayerCount][];
        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();

                var layer = this[layerIndex];
                var rect = layer.BoundingRectangleMillimeters;
                rect.Offset(DisplayWidth / -2f, DisplayHeight / -2f);

                SceneSettings.LayersDef[layerIndex] = new SceneLayerDef
                {
                    Height = this[layerIndex].PositionZ,
                    Area = (float)Math.Round(layer.Contours.TotalSolidArea * pixelArea, 4),
                    XStartBoundingRectangleOffsetFromCenter = (float)Math.Round(rect.X, 4),
                    YStartBoundingRectangleOffsetFromCenter = (float)Math.Round(rect.Y, 4),
                    XEndBoundingRectangleOffsetFromCenter = (float)Math.Round(rect.Right, 4),
                    YEndBoundingRectangleOffsetFromCenter = (float)Math.Round(rect.Bottom, 4),
                    ObjectCount = (uint)layer.Contours.ExternalContoursCount,
                    MaxContourArea = (float)Math.Round(layer.Contours.MaxSolidArea * pixelArea, 4)
                };

                encodedRle[layerIndex] = EncodeLayerRle(encodeWith, (uint)layerIndex);

                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                outputFile.CreateEntryFromContent($"layer_images/layer_{layerIndex}.{encodeWith.ToString().ToLowerInvariant()}Img", encodedRle[layerIndex], ZipArchiveMode.Create);
                encodedRle[layerIndex] = null!;
                progress.PauseOrCancelIfRequested();
            }
        }

        using var memoryStream = new MemoryStream();
        Helpers.Serializer.Serialize(memoryStream, SceneSettings);
        outputFile.CreateEntryFromContent(SceneFileName, memoryStream, ZipArchiveMode.Create);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(SettingsFileName);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"Unable to find {SettingsFileName} file", FileFullPath);
        }
        try
        {
            using var stream = entry.Open();
            Settings = JsonSerializer.Deserialize<SettingsManifest>(stream)!;
        }
        catch (Exception e)
        {
            Clear();
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        entry = inputFile.GetEntry(LayersFileName);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"Unable to find {LayersFileName} file", FileFullPath);
        }
        try
        {
            using var stream = entry.Open();
            LayersSettings = JsonSerializer.Deserialize<LayersControllerManifest>(stream)!;
        }
        catch (Exception e)
        {
            Clear();
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        if (LayersSettings.Count == 0)
        {
            Clear();
            throw new FileLoadException("Unable to detect layer images in the file", FileFullPath);
        }

        entry = inputFile.GetEntry(PrintInfoFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                PrintInfoSettings = JsonSerializer.Deserialize<PrintInfoManifest>(stream)!;
            }
            catch (Exception e)
            {
                Clear();
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        entry = inputFile.GetEntry(SoftwareInfoFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                SoftwareInfoSettings = JsonSerializer.Deserialize<SoftwareInfoManifest>(stream)!;
            }
            catch (Exception e)
            {
                Clear();
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        entry = inputFile.GetEntry(SceneFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                SceneSettings = Helpers.Deserialize<SceneManifest>(stream);
            }
            catch (Exception e)
            {
                Clear();
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        DecodeAllThumbnailsFromZip(inputFile, progress, "preview_");

        Init((uint)LayersSettings.Count);
        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

        float positionZ = 0;
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            positionZ += LayersSettings.Layers[layerIndex].LayerThickness;
            this[layerIndex] = new Layer(layerIndex, this)
            {
                Index = layerIndex,
                PositionZ = Layer.RoundHeight(positionZ),
                ExposureTime = (float)Math.Round(LayersSettings.Layers[layerIndex].ExposureTime, 2, MidpointRounding.AwayFromZero),
                LiftHeight = (float)Math.Round(LayersSettings.Layers[layerIndex].LiftHeight, 2, MidpointRounding.AwayFromZero),
                LiftSpeed = SpeedConverter.Convert(LayersSettings.Layers[layerIndex].LiftSpeed, FormatSpeedUnit, CoreSpeedUnit)
            };
        }

        if (DecodeType == FileDecodeType.Full)
        {
            Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                byte[] encodedRle;
                AnycubicZipRleFormat rleFormat;
                lock (Mutex)
                {
                    entry = inputFile.GetEntry($"layer_images/layer_{layerIndex}.pwszImg");
                    if (entry is null)
                    {
                        entry = inputFile.GetEntry($"layer_images/layer_{layerIndex}.pw0Img");
                        if (entry is null)
                        {
                            Clear();
                            throw new FileLoadException($"Layer image {layerIndex} is missing in the file.", FileFullPath);
                        }

                        rleFormat = AnycubicZipRleFormat.PW0;
                    }
                    else
                    {
                        rleFormat = AnycubicZipRleFormat.PWSZ;
                    }

                    using var stream = entry.Open();
                    encodedRle = stream.ToArray();
                }

                this[layerIndex].LayerMat = DecodeLayerRle(rleFormat, encodedRle);
                progress.LockAndIncrement();
            });
        }

        UpdateGlobalPropertiesFromLayers();
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);

        outputFile.CreateEntryFromSerializeJson(SettingsFileName, Settings, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(PrintInfoFileName, PrintInfoSettings, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(LayersFileName, LayersSettings, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
        outputFile.CreateEntryFromSerializeJson(SoftwareInfoFileName, SoftwareInfoSettings, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
    }
    #endregion
}