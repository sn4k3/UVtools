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
    #region Cosntants

    public const string BottomLayersStage1Key = "bott_0";
    public const string BottomLayersStage2Key = "bott_1";

    public const string NormalLayersStage1Key = "normal_0";
    public const string NormalLayersStage2Key = "normal_1";

    #endregion

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
        public float[] PreviewBackgroundColor { get; set; } = [0.0f, 0.28f, 0.39f];

        [JsonPropertyName("prev_model_color")]
        public float[] ModelBackgroundColor { get; set; } = [0.80f, 0.80f, 0.80f];

        [JsonPropertyName("prev_supports_color")]
        public float[] SupportsBackgroundColor { get; set; } = [0.07f, 0.93f, 0.93f];

        [JsonPropertyName("prev_image_size")]
        public int[] PreviewImageSize { get; set; } = [224, 168];

        [JsonPropertyName("child_screen")]
        public SettingsChildScreen[] Screens { get; set; } = [new()];

        [JsonPropertyName("prev2_back_color")]
        public float[] Preview2BackgroundColor { get; set; } = [0.08f, 0.11f, 0.16f];

        [JsonPropertyName("prev2_image_size")]
        public int[] Preview2ImageSize { get; set; } = [336, 252];

        [JsonPropertyName("raster_segments_capacity")]
        public uint RasterSegmentsCapacity { get; set; }

        [JsonPropertyName("raster_antialiasing")]
        public byte RasterAntialiasing { get; set; } = 8;

        [JsonPropertyName("cloudprev_back_color")]
        public float[] CloudBackgroundColor { get; set; } = [0.00f, 0.28f, 0.39f];

        [JsonPropertyName("cloudprev_imag_size")]
        public int[] CloudImageSize { get; set; } = [800, 600];

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(Name)}: {Name}, {nameof(KeySuffix)}: {KeySuffix}, {nameof(KeyImageFormat)}: {KeyImageFormat}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PixelWidthMicrons)}: {PixelWidthMicrons}, {nameof(PixelHeightMicrons)}: {PixelHeightMicrons}, {nameof(MaxSamples)}: {MaxSamples}, {nameof(Properties)}: {Properties}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(MachineZ)}: {MachineZ}, {nameof(MaxFileVersion)}: {MaxFileVersion}, {nameof(PreviewBackgroundColor)}: {PreviewBackgroundColor}, {nameof(ModelBackgroundColor)}: {ModelBackgroundColor}, {nameof(SupportsBackgroundColor)}: {SupportsBackgroundColor}, {nameof(PreviewImageSize)}: {PreviewImageSize}, {nameof(Preview2BackgroundColor)}: {Preview2BackgroundColor}, {nameof(Preview2ImageSize)}: {Preview2ImageSize}, {nameof(RasterSegmentsCapacity)}: {RasterSegmentsCapacity}, {nameof(RasterAntialiasing)}: {RasterAntialiasing}, {nameof(CloudBackgroundColor)}: {CloudBackgroundColor}, {nameof(CloudImageSize)}: {CloudImageSize}";
        }
    }

    public sealed class SettingsResinProperty
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "3";

        [JsonPropertyName("code")]
        public string Code { get; set; } = "10";

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "€";

        [JsonPropertyName("name")]
        public string Name { get; set; } = "default_resin";

        [JsonPropertyName("price")]
        public float Price { get; set; } = 25;

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Standard resin";

        [JsonPropertyName("volume")]
        public float Volume { get; set; } = 1000;

        [JsonPropertyName("subfunc_code")]
        public int SubfuncCode { get; set; } = 0;

        [JsonPropertyName("density")]
        public float Density { get; set; } = 1.2f;

        [JsonPropertyName("target_temperature")]
        public float TargetTemperature { get; set; } = 25;

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(Code)}: {Code}, {nameof(Currency)}: {Currency}, {nameof(Name)}: {Name}, {nameof(Price)}: {Price}, {nameof(Type)}: {Type}, {nameof(Volume)}: {Volume}, {nameof(SubfuncCode)}: {SubfuncCode}, {nameof(Density)}: {Density}, {nameof(TargetTemperature)}: {TargetTemperature}";
        }
    }

    public sealed class SettingsResinTemperatureCoefficients
    {
        [JsonPropertyName("temperature")]
        public float Temperature { get; set; }

        [JsonPropertyName("x_coefficient")]
        public float XCoefficient { get; set; }

        [JsonPropertyName("y_compensation")]
        public float YCompensation { get; set; }

        public SettingsResinTemperatureCoefficients()
        {
        }

        public SettingsResinTemperatureCoefficients(float temperature, float xCoefficient, float yCompensation)
        {
            Temperature = temperature;
            XCoefficient = xCoefficient;
            YCompensation = yCompensation;
        }

        public override string ToString()
        {
            return
                $"{nameof(Temperature)}: {Temperature}, {nameof(XCoefficient)}: {XCoefficient}, {nameof(YCompensation)}: {YCompensation}";
        }
    }

    public sealed class SettingsResinDepthPenetrationCurve
    {
        [JsonPropertyName("zthick_min")]
        public float ZThickMin { get; set; } = 0.01f;

        [JsonPropertyName("zthick_max")]
        public float ZThickMax { get; set; } = 0.2f;

        [JsonPropertyName("light_intensity")]
        public float LightIntensity { get; set; } = 9000;

        [JsonPropertyName("safety_coefficient")]
        public float SafetyCoefficient { get; set; } = 1.6f;

        [JsonPropertyName("current_tempcurve_selector")]
        public int CurrentTempcurveSelector { get; set; }

        [JsonPropertyName("temperature_coefficients")]
        public List<SettingsResinTemperatureCoefficients> TemperatureCoefficients { get; set; } =
            [
                new(10, 184.27f, 1675.80f),
                new(25, 197.78f, 1803.20f),
                new(35, 161.19f, 1417.30f),
                new(45, 167.31f, 1480.90f),
                new(55, 166.76f, 1474.10f),
            ];

        public override string ToString()
        {
            return
                $"{nameof(ZThickMin)}: {ZThickMin}, {nameof(ZThickMax)}: {ZThickMax}, {nameof(LightIntensity)}: {LightIntensity}, {nameof(SafetyCoefficient)}: {SafetyCoefficient}, {nameof(CurrentTempcurveSelector)}: {CurrentTempcurveSelector}, {nameof(TemperatureCoefficients)}: {TemperatureCoefficients}";
        }
    }

    public sealed class SettingsResinMultiStateParas
    {
        [JsonPropertyName("height")]
        public float LiftHeight { get; set; } = DefaultLiftHeight;

        [JsonPropertyName("up_speed")]
        public float LiftSpeed { get; set; } = 3;

        [JsonPropertyName("down_speed")]
        public float RetractSpeed { get; set; } = 3;

        public SettingsResinMultiStateParas()
        {
        }

        public SettingsResinMultiStateParas(float liftHeight, float liftSpeed, float retractSpeed)
        {
            LiftHeight = liftHeight;
            LiftSpeed = liftSpeed;
            RetractSpeed = retractSpeed;
        }

        public override string ToString()
        {
            return
                $"{nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}";
        }
    }

    public sealed class SettingsResinSliceExtPara
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "3";

        [JsonPropertyName("multi_state_used")]
        public byte MultiStateUsed { get; set; }

        [JsonPropertyName("transition_layercount")]
        public ushort TransitionLayerCount { get; set; }

        [JsonPropertyName("transition_type")]
        public byte TransitionType { get; set; }


        [JsonPropertyName("multi_state_paras")]
        public Dictionary<string, SettingsResinMultiStateParas> MultiStateParas { get; set; } = new()
        {
            { BottomLayersStage1Key, new SettingsResinMultiStateParas(5, 2, 3) },
            { BottomLayersStage2Key, new SettingsResinMultiStateParas(4, 3, 3) },
            { NormalLayersStage1Key, new SettingsResinMultiStateParas(3, 2, 2) },
            { NormalLayersStage2Key, new SettingsResinMultiStateParas(5, 6, 6) },
        };

        [JsonPropertyName("exposure_compensate")]
        public float ExposureCompensate { get; set; }

        [JsonPropertyName("intelli_mode")]
        public byte IntelligentMode { get; set; }

        [JsonPropertyName("max_acceleration")]
        public float MaxAcceleration { get; set; } = 2.0f;

        [JsonPropertyName("separate_support_exposure_delayed")]
        public float SeparateSupportExposureDelayed { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(MultiStateUsed)}: {MultiStateUsed}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(TransitionType)}: {TransitionType}, {nameof(MultiStateParas)}: {MultiStateParas}, {nameof(ExposureCompensate)}: {ExposureCompensate}, {nameof(IntelligentMode)}: {IntelligentMode}, {nameof(MaxAcceleration)}: {MaxAcceleration}, {nameof(SeparateSupportExposureDelayed)}: {SeparateSupportExposureDelayed}";
        }
    }

    public sealed class SettingsResinSlicePara
    {
        [JsonPropertyName("anti_count")]
        public byte AntiCount { get; set; } = 1;

        [JsonPropertyName("blur_level")]
        public byte BlurLevel { get; set; }

        [JsonPropertyName("bott_layers")]
        public ushort BottomLayerCount { get; set; } = DefaultBottomLayerCount;

        [JsonPropertyName("bott_time")]
        public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;

        [JsonPropertyName("exposure_time")]
        public float ExposureTime { get; set; } = DefaultExposureTime;

        [JsonPropertyName("gray_level")]
        public byte GrayLevel { get; set; }

        [JsonPropertyName("off_time")]
        public float WaitTimeBeforeCure { get; set; } = 0.5f;

        [JsonPropertyName("use_indivi_layerpara")]
        public float UseIndividualLayerPara { get; set; }

        [JsonPropertyName("use_random_erode")]
        public byte UseRandomErode { get; set; }

        [JsonPropertyName("zthick")]
        public float LayerHeight { get; set; } = DefaultLayerHeight;

        [JsonPropertyName("zup_height")]
        public float LiftHeight { get; set; } = DefaultLiftHeight;

        [JsonPropertyName("zup_speed")]
        public float LiftSpeed { get; set; } = 6;

        [JsonPropertyName("zdown_speed")]
        public float RetractSpeed { get; set; } = 6;

        public override string ToString()
        {
            return
                $"{nameof(AntiCount)}: {AntiCount}, {nameof(BlurLevel)}: {BlurLevel}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(GrayLevel)}: {GrayLevel}, {nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(UseIndividualLayerPara)}: {UseIndividualLayerPara}, {nameof(UseRandomErode)}: {UseRandomErode}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}";
        }
    }


    public sealed class SettingsResin
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2";

        [JsonPropertyName("property")]
        public SettingsResinProperty Property { get; set; } = new();

        [JsonPropertyName("depth_penetration_curve")]
        public SettingsResinDepthPenetrationCurve DepthPenetrationCurve { get; set; } = new();

        [JsonPropertyName("slice_extpara")]
        public SettingsResinSliceExtPara SliceExtPara { get; set; } = new();

        [JsonPropertyName("slicepara")]
        public SettingsResinSlicePara SlicePara { get; set; } = new();

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(Property)}: {Property}, {nameof(DepthPenetrationCurve)}: {DepthPenetrationCurve}, {nameof(SliceExtPara)}: {SliceExtPara}, {nameof(SlicePara)}: {SlicePara}";
        }
    }

    public sealed class SettingsFirmwareCalcPrintTimeParas
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "2";

        [JsonPropertyName("MACHINE_AXIS_STEPS_PER_UNIT")]
        public float[] MachineAxisStepsPerUnit { get; set; } = [100, 100, 3200, 94];

        [JsonPropertyName("MACHINE_BLOCK_BUFFER_SIZE")]
        public int MachineBlockBufferSize { get; set; } = 32;

        [JsonPropertyName("MACHINE_DEFAULT_ACCELERATION")]
        public float MachineDefaultAcceleration { get; set; } = 1000;

        [JsonPropertyName("MACHINE_DEFAULT_MINSEGMENTTIME")]
        public float MachineDefaultMinSegmentTime { get; set; } = 20000;

        [JsonPropertyName("MACHINE_DEFAULT_XYJERK")]
        public float MachineDefaultXYJerk { get; set; } = 20;

        [JsonPropertyName("MACHINE_DEFAULT_ZJERK")]
        public float MachineDefaultZJerk { get; set; } = 0.2f;

        [JsonPropertyName("MACHINE_GENERATE_FRAME_TIME")]
        public float MachineGenerateFrameTime { get; set; } = 450;

        [JsonPropertyName("MACHINE_MAX_ACCELERATION")]
        public float[] MachineMaxAcceleration { get; set; } = [1000, 1000, 160, 1000];

        [JsonPropertyName("MACHINE_MAX_FEEDRATE")]
        public float[] MachineMaxFeedRate { get; set; } = [200, 200, 20, 45];

        [JsonPropertyName("MACHINE_MAX_STEP_FREQUENCY")]
        public float MachineMaxStepFrequency { get; set; } = 256000;

        [JsonPropertyName("MACHINE_MINIMUM_PLANNER_SPEED")]
        public float MachineMinimumPlannerSpeed { get; set; } = 0.5f;

        [JsonPropertyName("MACHINE_NOR_LAYER_DOWN_HEIGHT_DIV")]
        public float MachineNormalLayerDownHeightDiv { get; set; } = 0.25f;

        [JsonPropertyName("MACHINE_NOR_LAYER_DOWN_SPEED_DIV")]
        public float MachineNormalLayerDownSpeedDiv { get; set; } = 0.5f;

        [JsonPropertyName("MACHINE_NOR_LAYER_UP_HEIGHT_DIV")]
        public float MachineNormalLayerUpHeightDiv { get; set; } = 0.25f;

        [JsonPropertyName("MACHINE_NOR_LAYER_UP_SPEED_DIV")]
        public float MachineNormalLayerUpSpeedDiv { get; set; } = 0.5f;

        [JsonPropertyName("MACHINE_STEP_MUL")]
        public float MachineStepMul { get; set; } = 1;

        [JsonPropertyName("MACHINE_TIME_COMPENSATE")]
        public float MachineTimeCompensate { get; set; }

        [JsonPropertyName("MACHINE_TIM_PRES")]
        public float MachineTimePres { get; set; } = 30;

        [JsonPropertyName("MACHINE_TIM_RCC_CLK")]
        public float MachineTimeRccClk { get; set; } = 60;

        [JsonPropertyName("FUNCTION")]
        public float Function { get; set; } = 1;

        [JsonPropertyName("MACHINE_MODE_ACCELERATION")]
        public float[] MachineModeAcceleration { get; set; } = [0, 0, 0, 0];

        [JsonPropertyName("LAYER_COMPENSATE")]
        public float[] LayerCompensate { get; set; } = [0, 0, 0, 0];

        [JsonPropertyName("HEIGHT_COMPENSATE")]
        public float[] HeightCompensate { get; set; } = [0, 0, 0, 0];

        [JsonPropertyName("TIMES_COMPENSATE")]
        public float[] TimesCompensate { get; set; } = [0, 0, 0, 0];

        public override string ToString()
        {
            return
                $"{nameof(Version)}: {Version}, {nameof(MachineAxisStepsPerUnit)}: {MachineAxisStepsPerUnit}, {nameof(MachineBlockBufferSize)}: {MachineBlockBufferSize}, {nameof(MachineDefaultAcceleration)}: {MachineDefaultAcceleration}, {nameof(MachineDefaultMinSegmentTime)}: {MachineDefaultMinSegmentTime}, {nameof(MachineDefaultXYJerk)}: {MachineDefaultXYJerk}, {nameof(MachineDefaultZJerk)}: {MachineDefaultZJerk}, {nameof(MachineGenerateFrameTime)}: {MachineGenerateFrameTime}, {nameof(MachineMaxAcceleration)}: {MachineMaxAcceleration}, {nameof(MachineMaxFeedRate)}: {MachineMaxFeedRate}, {nameof(MachineMaxStepFrequency)}: {MachineMaxStepFrequency}, {nameof(MachineMinimumPlannerSpeed)}: {MachineMinimumPlannerSpeed}, {nameof(MachineNormalLayerDownHeightDiv)}: {MachineNormalLayerDownHeightDiv}, {nameof(MachineNormalLayerDownSpeedDiv)}: {MachineNormalLayerDownSpeedDiv}, {nameof(MachineNormalLayerUpHeightDiv)}: {MachineNormalLayerUpHeightDiv}, {nameof(MachineNormalLayerUpSpeedDiv)}: {MachineNormalLayerUpSpeedDiv}, {nameof(MachineStepMul)}: {MachineStepMul}, {nameof(MachineTimeCompensate)}: {MachineTimeCompensate}, {nameof(MachineTimePres)}: {MachineTimePres}, {nameof(MachineTimeRccClk)}: {MachineTimeRccClk}, {nameof(Function)}: {Function}, {nameof(MachineModeAcceleration)}: {MachineModeAcceleration}, {nameof(LayerCompensate)}: {LayerCompensate}, {nameof(HeightCompensate)}: {HeightCompensate}, {nameof(TimesCompensate)}: {TimesCompensate}";
        }
    }

    public sealed class SettingsFirmwareCalcExpTimeParas
    {
        [JsonPropertyName("precision_range_branch")]
        public float[] PrecisionRangeBranch { get; set; } = [0, 5, 25];

        [JsonPropertyName("precision_per_volume")]
        public float PrecisionPerVolume { get; set; } = 5;

        [JsonPropertyName("precision_coeff_value")]
        public float[] PrecisionCoeffValue { get; set; } = [0.024f, 0.010f, -0.200f];

        [JsonPropertyName("energy_coeff")]
        public float EnergyCoeff { get; set; } = 0;

        [JsonPropertyName("machine_exposure_ton")]
        public float MachineExposureTon { get; set; } = 0.40f;

        public override string ToString()
        {
            return
                $"{nameof(PrecisionRangeBranch)}: {PrecisionRangeBranch}, {nameof(PrecisionPerVolume)}: {PrecisionPerVolume}, {nameof(PrecisionCoeffValue)}: {PrecisionCoeffValue}, {nameof(EnergyCoeff)}: {EnergyCoeff}, {nameof(MachineExposureTon)}: {MachineExposureTon}";
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
        public List<SettingsResin> FactoryResins { get; set; } = [new()];

        [JsonPropertyName("user_resins")]
        public List<SettingsResin> UserResins { get; set; } = [new()];

        [JsonPropertyName("active_resins")]
        public string[] ActiveResins { get; set; } = ["default_resin"];

        [JsonPropertyName("firmware_calc_print_time")]
        public byte FirmwareCalcPrintTime { get; set; } = 1;

        [JsonPropertyName("firmware_calc_print_time_paras")]
        public SettingsFirmwareCalcPrintTimeParas FirmwareCalcPrintParameters { get; set; } = new();

        [JsonPropertyName("firmware_calc_exp_time_paras")]
        public SettingsFirmwareCalcExpTimeParas FirmwareCalcExposureTimeParameters { get; set; } = new();

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
        public LayerManifest[] Layers { get; set; } = [];
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
        public float MaterialMilliliters { get; set; }

        public override string ToString()
        {
            return
                $"{nameof(Cost)}: {Cost}, {nameof(Currency)}: {Currency}, {nameof(PrintTime)}: {PrintTime}, {nameof(MaterialMilliliters)}: {MaterialMilliliters}";
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

        [FieldOrder(18)][FieldCount(nameof(LayerDefCount))] public SceneLayerDef[] LayersDef { get; set; } = [];

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
            return
            [
                PrintParameterModifier.BottomLayerCount,
                PrintParameterModifier.TransitionLayerCount,

                PrintParameterModifier.WaitTimeBeforeCure,

                PrintParameterModifier.BottomExposureTime,
                PrintParameterModifier.ExposureTime,

                PrintParameterModifier.BottomLiftHeight,
                PrintParameterModifier.BottomLiftSpeed,
                PrintParameterModifier.LiftHeight,
                PrintParameterModifier.LiftSpeed,
                PrintParameterModifier.BottomLiftHeight2,
                PrintParameterModifier.BottomLiftSpeed2,
                PrintParameterModifier.LiftHeight2,
                PrintParameterModifier.LiftSpeed2,

                PrintParameterModifier.BottomRetractSpeed,
                PrintParameterModifier.RetractSpeed,
                PrintParameterModifier.BottomRetractSpeed2,
                PrintParameterModifier.RetractSpeed2
            ];
        }
    }

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } =
    [
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed
    ];

    public override string ConvertMenuGroup => "Anycubic Photon Workshop";

    public override FileExtension[] FileExtensions { get; } =
    [
        new(typeof(AnycubicZipFile), "pm4u", "Photon Mono 4 Ultra (PM4U)"),
        new(typeof(AnycubicZipFile), "pm7", "Photon Mono M7 (PM7)"),
        new(typeof(AnycubicZipFile), "pm7m", "Photon Mono M7 Max (PM7M)"),
        new(typeof(AnycubicZipFile), "pwsz", "Photon Mono M7 Pro (PWSZ)")

    ];

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
        set
        {
            base.ResolutionX = Settings.MachineType.ResolutionX = (ushort)value;
            if (Settings.MachineType.Screens.Length > 0)
            {
                Settings.MachineType.Screens[0].Width = base.ResolutionX;
            }
        }
    }

    public override uint ResolutionY
    {
        get => Settings.MachineType.ResolutionY;
        set
        {
            base.ResolutionY = Settings.MachineType.ResolutionY = (ushort)value;
            if (Settings.MachineType.Screens.Length > 0)
            {
                Settings.MachineType.Screens[0].Height = base.ResolutionY;
            }
        }
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

    public override float LayerHeight
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.LayerHeight
                : resinSettings.SlicePara.LayerHeight;
        }
        set
        {
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.LayerHeight = Layer.RoundHeight(value);
            }
            base.LayerHeight = Layer.RoundHeight(value);
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = SceneSettings.LayerCount = SceneSettings.LayerDefCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLayerCount
                : resinSettings.SlicePara.BottomLayerCount;
        }
        set
        {
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.BottomLayerCount = value;
            }
            base.BottomLayerCount = value;
        }
    }

    public override ushort TransitionLayerCount
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLayerCount
                : resinSettings.SliceExtPara.TransitionLayerCount;
        }
        set
        {
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.TransitionLayerCount = value;
            }
            base.TransitionLayerCount = value;
        }
    }

    public override float BottomLightOffDelay => BottomWaitTimeBeforeCure;

    public override float LightOffDelay => WaitTimeBeforeCure;

    public override float BottomWaitTimeBeforeCure => WaitTimeBeforeCure;

    public override float WaitTimeBeforeCure
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.WaitTimeBeforeCure
                : resinSettings.SlicePara.WaitTimeBeforeCure;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.WaitTimeBeforeCure = value;
            }

            base.WaitTimeBeforeCure = value;
        }
    }

    public override float BottomExposureTime
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomExposureTime
                : resinSettings.SlicePara.BottomExposureTime;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.BottomExposureTime = value;
            }

            base.BottomExposureTime = value;
        }
    }

    public override float ExposureTime
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.ExposureTime
                : resinSettings.SlicePara.ExposureTime;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.IntelligentMode = 0;
                resinSettings.SlicePara.ExposureTime = value;
            }

            base.ExposureTime = value;
        }
    }

    public override float BottomLiftHeight
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLiftHeight
                : resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].LiftHeight;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].LiftHeight = value;
            }

            base.BottomLiftHeight = value;
        }
    }

    public override float BottomLiftSpeed
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLiftSpeed
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].LiftSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].LiftSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.BottomLiftSpeed = value;
        }
    }

    public override float BottomLiftHeight2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLiftHeight2
                : resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].LiftHeight;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].LiftHeight = value;
            }

            base.BottomLiftHeight2 = value;
        }
    }

    public override float BottomLiftSpeed2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomLiftSpeed2
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].LiftSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].LiftSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.BottomLiftSpeed2 = value;
        }
    }

    public override float BottomRetractSpeed
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomRetractSpeed
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].RetractSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage1Key].RetractSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.BottomRetractSpeed = value;
        }
    }

    public override float BottomRetractSpeed2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.BottomRetractSpeed2
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].RetractSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[BottomLayersStage2Key].RetractSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.BottomRetractSpeed2 = value;
        }
    }

    public override float LiftHeight
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.LiftHeight
                : resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftHeight;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftHeight = value;
                resinSettings.SlicePara.LiftHeight = Layer.RoundHeight(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftHeight
                                                                       + resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftHeight);
            }

            base.LiftHeight = value;
        }
    }

    public override float LiftSpeed
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.LiftSpeed
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.LiftSpeed = resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.LiftSpeed = value;
        }
    }

    public override float LiftHeight2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.LiftHeight2
                : resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftHeight;
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftHeight = value;
                resinSettings.SlicePara.LiftHeight = Layer.RoundHeight(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].LiftHeight
                                                                       + resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftHeight);
            }

            base.LiftHeight2 = value;
        }
    }

    public override float LiftSpeed2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.LiftSpeed2
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].LiftSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.LiftSpeed2 = value;
        }
    }

    public override float RetractSpeed
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.RetractSpeed
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].RetractSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SlicePara.RetractSpeed = resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage1Key].RetractSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.RetractSpeed = value;
        }
    }


    public override float RetractSpeed2
    {
        get
        {
            var resinSettings = GetResinSetting();
            return resinSettings is null
                ? base.RetractSpeed2
                : SpeedConverter.Convert(resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].RetractSpeed, FormatSpeedUnit, CoreSpeedUnit);
        }
        set
        {
            value = MathF.Round(value, 2, MidpointRounding.AwayFromZero);
            var resinSettings = GetResinSetting();
            if (resinSettings is not null)
            {
                resinSettings.SliceExtPara.MultiStateParas[NormalLayersStage2Key].RetractSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            }

            base.RetractSpeed2 = value;
        }
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
    [
        new(224, 168),
        new(336, 252)
    ];


    public override object[] Configs =>
    [
        Settings, PrintInfoSettings, SoftwareInfoSettings, SceneSettings
    ];

    #endregion

    #region Constructor
    public AnycubicZipFile()
    { }
    #endregion

    #region Methods

    private List<SettingsResin> GetResinSettings()
    {
        var resins = new List<SettingsResin>();
        if (Settings.MachineExtern.ActiveResins.Length == 0) return resins;

        var activeResin = Settings.MachineExtern.ActiveResins[0];

        resins.AddRange(Settings.MachineExtern.UserResins.Where(resin => resin.Property.Name == activeResin));
        resins.AddRange(Settings.MachineExtern.FactoryResins.Where(resin => resin.Property.Name == activeResin));

        return resins;
    }

    private SettingsResin? GetResinSetting()
    {
        if (Settings.MachineExtern.ActiveResins.Length == 0) return null;
        var activeResin = Settings.MachineExtern.ActiveResins[0];

        var setting = Settings.MachineExtern.UserResins.FirstOrDefault(resin => resin.Property.Name == activeResin);
        setting ??= Settings.MachineExtern.FactoryResins.FirstOrDefault(resin => resin.Property.Name == activeResin);

        return setting;
    }

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

            rle.AddRange("{==\0"u8.ToArray());

            var zeroUintArray = new byte[4];

            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].Area));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].XStartBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].YStartBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].XEndBoundingRectangleOffsetFromCenter));
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].YEndBoundingRectangleOffsetFromCenter));
            rle.AddRange(zeroUintArray);
            rle.AddRange(BitExtensions.ToBytesLittleEndian(SceneSettings.LayersDef[layerIndex].ObjectCount));

            rle.AddRange("[--\0"u8.ToArray());

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

            rle.AddRange("--]\0"u8.ToArray());

            rle.AddRange("==}\0"u8.ToArray());

            return rle.ToArray();
        }

        throw new NotSupportedException($"Unsupported RLE format: {format}");
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        Settings.MachineType.KeySuffix = FileExtension![1..];


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
            Settings.MachineType.Screens =
            [
                new SettingsChildScreen(0, 0, ResolutionX, ResolutionY)
            ];
        }

        PrintInfoSettings.Cost = MaterialCost;
        PrintInfoSettings.PrintTime = PrintTime;
        PrintInfoSettings.MaterialMilliliters = MaterialMilliliters;
        SoftwareInfoSettings.Update();

        var resinSetting = GetResinSetting();
        if (resinSetting is not null)
        {
            resinSetting.SliceExtPara.MultiStateUsed = System.Convert.ToByte(IsUsingTSMC);
            resinSetting.SlicePara.UseIndividualLayerPara = System.Convert.ToByte(UsingPerLayerSettings);
        }

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
                    Area = MathF.Round((float)layer.Contours.TotalSolidArea * pixelArea, 4, MidpointRounding.AwayFromZero),
                    XStartBoundingRectangleOffsetFromCenter = MathF.Round(rect.X, 4, MidpointRounding.AwayFromZero),
                    YStartBoundingRectangleOffsetFromCenter = MathF.Round(rect.Y, 4, MidpointRounding.AwayFromZero),
                    XEndBoundingRectangleOffsetFromCenter = MathF.Round(rect.Right, 4, MidpointRounding.AwayFromZero),
                    YEndBoundingRectangleOffsetFromCenter = MathF.Round(rect.Bottom, 4, MidpointRounding.AwayFromZero),
                    ObjectCount = (uint)layer.Contours.ExternalContoursCount,
                    MaxContourArea = MathF.Round((float)layer.Contours.MaxSolidArea * pixelArea, 4, MidpointRounding.AwayFromZero)
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
                ExposureTime = MathF.Round(LayersSettings.Layers[layerIndex].ExposureTime, 2, MidpointRounding.AwayFromZero),
                LiftHeight = MathF.Round(LayersSettings.Layers[layerIndex].LiftHeight, 2, MidpointRounding.AwayFromZero),
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

        if (GetResinSetting() is null) UpdateGlobalPropertiesFromLayers();
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