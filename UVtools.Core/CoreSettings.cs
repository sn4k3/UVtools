/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV.Cuda;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Managers;
using UVtools.Core.Operations;

namespace UVtools.Core;

public static class CoreSettings
{
    #region Properties

    public static CultureInfo OptimalCultureInfo
    {
        get
        {
            var cultureInfo = (CultureInfo)CultureInfo.CurrentCulture.Clone();
            cultureInfo.NumberFormat.CurrencyDecimalSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .CurrencyDecimalSeparator;
            cultureInfo.NumberFormat.CurrencyGroupSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .CurrencyGroupSeparator;
            cultureInfo.NumberFormat.NumberDecimalSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .NumberDecimalSeparator;
            cultureInfo.NumberFormat.NumberGroupSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .NumberGroupSeparator;
            cultureInfo.NumberFormat.PercentDecimalSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .PercentDecimalSeparator;
            cultureInfo.NumberFormat.PercentGroupSeparator = CultureInfo
                .InvariantCulture
                .NumberFormat
                .PercentGroupSeparator;
            return cultureInfo;
        }
    }

    /// <summary>
    /// Gets the optimal maximum degree of parallelism
    /// </summary>
    public static int OptimalMaxDegreeOfParallelism
    {
        get
        {
            var processorCount = Environment.ProcessorCount;
            return processorCount switch
            {
                //>= 24 => processorCount - 6,
                >= 20 => processorCount - 5,
                >= 16 => processorCount - 4,
                >= 12 => processorCount - 3,
                >= 8 => processorCount - 2,
                >= 4 => processorCount - 1,
                _ => processorCount,
            };
        }
    }

    /// <summary>
    /// Gets or sets the maximum number of concurrent tasks enabled by this ParallelOptions instance.
    /// Less or equal to 0 will set to auto number
    /// -2 = Auto-optimal number
    /// -1 = .NET scheduler
    /// 0 = <see cref="Environment.ProcessorCount"/>
    /// 1 = Single thread
    /// n = Multi threads
    /// </summary>
    public static int MaxDegreeOfParallelism
    {
        get;
        set =>
            field = value switch
            {
                <= -2 => OptimalMaxDegreeOfParallelism, // Auto-optimal number
                -1 => -1, // .NET scheduler
                0 => Environment.ProcessorCount, // Max threads
                _ => Math.Clamp(value, 1, Environment.ProcessorCount), // Custom number
            };
    } = OptimalMaxDegreeOfParallelism;

    /// <summary>
    /// Gets the ParallelOptions with <see cref="MaxDegreeOfParallelism"/> set
    /// </summary>
    /// <remarks>The degree of parallelism is reduced when the workers would not fit in the available memory,
    /// see <see cref="MemoryGovernor.GetMaxDegreeOfParallelism"/>.</remarks>
    public static ParallelOptions ParallelOptions =>
        new() { MaxDegreeOfParallelism = MemoryGovernor.GetMaxDegreeOfParallelism(MaxDegreeOfParallelism) };

    /// <summary>
    /// Gets the ParallelOptions with <see cref="MaxDegreeOfParallelism"/> set to 1 for debug purposes
    /// </summary>
    public static ParallelOptions ParallelDebugOptions => new() { MaxDegreeOfParallelism = 1 };

    /// <summary>
    /// Gets the ParallelDebugOptions with the <see cref="CancellationToken"/> set
    /// </summary>
    public static ParallelOptions GetParallelDebugOptions(CancellationToken token = default)
    {
        var options = ParallelDebugOptions;
        options.CancellationToken = token;
        return options;
    }

    /// <summary>
    /// Gets the ParallelDebugOptions with the <see cref="CancellationToken"/> set
    /// </summary>
    public static ParallelOptions GetParallelDebugOptions(OperationProgress progress)
    {
        return GetParallelDebugOptions(progress.Token);
    }

    /// <summary>
    /// Gets the ParallelOptions with <see cref="MaxDegreeOfParallelism"/> and the <see cref="CancellationToken"/> set
    /// </summary>
    public static ParallelOptions GetParallelOptions(CancellationToken token = default)
    {
        var options = ParallelOptions;
        options.CancellationToken = token;
        return options;
    }

    /// <summary>
    /// Gets the ParallelOptions with <see cref="MaxDegreeOfParallelism"/> and the <see cref="CancellationToken"/> set
    /// </summary>
    public static ParallelOptions GetParallelOptions(OperationProgress progress)
    {
        return GetParallelOptions(progress.Token);
    }

    /// <summary>
    /// Gets the ParallelOptions with the <see cref="CancellationToken"/> set and the <see cref="MaxDegreeOfParallelism"/>
    /// reduced to the number of workers that fit in the available memory.
    /// </summary>
    /// <param name="progress">The progress holding the cancellation token.</param>
    /// <param name="bytesPerWorker">The estimated bytes each worker holds at a time.</param>
    public static ParallelOptions GetParallelOptions(OperationProgress progress, long bytesPerWorker)
    {
        return new ParallelOptions
        {
            MaxDegreeOfParallelism = MemoryGovernor.GetMaxDegreeOfParallelism(MaxDegreeOfParallelism, bytesPerWorker),
            CancellationToken = progress.Token
        };
    }

    /// <summary>
    /// Gets or sets if the memory is managed adaptively: under pressure caches are trimmed, idle layers and the undo
    /// history move to the disk cache and the parallelism is reduced, instead of letting the system run out of memory.
    /// </summary>
    public static bool AdaptiveMemoryManagement { get; set; } = true;

    /// <summary>
    /// Gets or sets the physical memory in bytes to keep available for the system and other programs, 0 for automatic.
    /// See <see cref="MemoryGovernor.ReserveBytes"/>.
    /// </summary>
    public static long MemoryReserveBytes
    {
        get;
        set => field = Math.Max(0, value);
    }

    /// <summary>
    /// Gets or sets if layers and the undo history can be moved to a disk cache to release memory RAM.
    /// </summary>
    public static bool EnableDiskCache { get; set; } = true;

    /// <summary>
    /// Gets or sets the folder of the disk cache, empty for <see cref="DefaultDiskCacheFolder"/>.
    /// Changes apply on the next start.
    /// </summary>
    public static string? DiskCacheFolder { get; set; }

    /// <summary>
    /// Gets or sets the maximum size of the disk cache in bytes, 0 to be limited by the disk free space only.
    /// </summary>
    public static long DiskCacheMaximumBytes
    {
        get;
        set
        {
            field = Math.Max(0, value);
            var store = LayerBlobStore.SessionIfCreated;
            if (store is not null) store.MaximumBytes = field;
        }
    }

    /// <summary>
    /// Gets the default folder of the disk cache.
    /// On Linux the temporary folder is often a RAM disk (tmpfs), so the user cache folder is used instead.
    /// </summary>
    public static string DefaultDiskCacheFolder
    {
        get
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD())
            {
                var cache = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
                if (string.IsNullOrWhiteSpace(cache))
                {
                    cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
                }

                return Path.Combine(cache, About.Software);
            }

            return Path.Combine(Path.GetTempPath(), About.Software);
        }
    }

    /// <summary>
    /// Gets <see cref="DiskCacheFolder"/> or <see cref="DefaultDiskCacheFolder"/> when not set.
    /// </summary>
    public static string DiskCacheFolderOrDefault =>
        string.IsNullOrWhiteSpace(DiskCacheFolder) ? DefaultDiskCacheFolder : DiskCacheFolder;

    /// <summary>
    /// Gets or sets the maximum number of operations kept in the undo history, 0 for unlimited.
    /// When exceeded, the oldest operations are merged so the original layers are always kept.
    /// </summary>
    public static int MaximumUndoHistory
    {
        get;
        set => field = Math.Max(0, value);
    } = 50;

    /// <summary>
    /// Gets or sets if the layers of the undo history that are no longer in use by the file are moved to the disk cache
    /// after each operation, instead of only under memory pressure.
    /// </summary>
    public static bool StoreUndoHistoryOnDisk { get; set; } = true;

    /// <summary>
    /// Gets or sets if operations run via CUDA when possible
    /// </summary>
    public static bool EnableCuda { get; set; }

    /// <summary>
    /// Gets if we can use cuda on operations
    /// </summary>
    public static bool CanUseCuda => EnableCuda && CudaInvoke.HasCuda;

    /// <summary>
    /// Gets or sets the default compression type for layers
    /// </summary>
    public static LayerCompressionCodec DefaultLayerCompressionCodec
    {
        get;
        set
        {
            field = value;
            MatCompressor.DefaultCompressor = Layer.LayerCompressionCodecToMatCompressor(value);
        }
    } = LayerCompressionCodec.Deflate;

    /// <summary>
    /// Gets or sets the default compression level applied to image layers.
    /// </summary>
    /// <remarks>This property determines the compression level used when no explicit compression setting is
    /// specified for a layer. Changing this value affects subsequent operations that rely on the default compression
    /// behavior.</remarks>
    public static LayerCompressionLevel DefaultLayerCompressionLevel
    {
        get;
        set
        {
            field = value;
            MatCompressor.DefaultCompressionLevel =
                Layer.LayerCompressionLevelToMatCompressionLevel(value);
        }
    } = LayerCompressionLevel.Optimal;

    /// <summary>
    /// <para>The average resin 1000ml bottle cost, to use when bottle cost is not available.</para>
    /// <para>Use 0 to disable.</para>
    /// </summary>
    public static float AverageResin1000MlBottleCost = 60f;

    /// <summary>
    /// Gets or sets the per-layer settings mode.
    /// </summary>
    public static PerLayerSettingsModes PerLayerSettingsMode =
        PerLayerSettingsModes.EnabledButExcludeProblematicFileFormats;

    /// <summary>
    /// Gets the most likely problematic file formats where per-layer settings can cause unexpected printing issues.
    /// </summary>
    public static Type[] PerLayerSettingsProblematicFileFormats { get; } =
    [typeof(AnycubicFile), typeof(AnycubicZipFile), typeof(GooFile), typeof(PHZFile)];

    /// <summary>
    /// Gets the default folder to save the settings
    /// </summary>
    public static string DefaultSettingsFolder
    {
        get
        {
            if (OperatingSystem.IsMacOS())
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Library",
                    "Application Support",
                    About.Software
                );
            }

            var folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrWhiteSpace(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            if (string.IsNullOrWhiteSpace(folder))
                folder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var path = Path.Combine(folder, About.Software);
            return path;
        }
    }

    /// <summary>
    /// Gets the default folder to save the settings
    /// </summary>
    public static string DefaultSettingsFolderAndEnsureCreation
    {
        get
        {
            var path = DefaultSettingsFolder;
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return path;
        }
    }

    #endregion
}
