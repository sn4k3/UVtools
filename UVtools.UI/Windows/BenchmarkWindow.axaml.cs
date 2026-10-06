using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using EmguExtensions;
using StageKit.Primitives.System;
using UVtools.Core;
using UVtools.Core.Compressors;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;

namespace UVtools.UI.Windows;

public partial class BenchmarkWindow : GenericWindow
{
    public enum BenchmarkResolution
    {
        Resolution4K,
        Resolution8K,
    }

    private const ushort SingleThreadTests = 200;
    private const ushort MultiThreadTests = 5000;

    public const string RunsAbbreviation = "TDPS";
    public const string StressCPUTestName = "Stress CPU (Run until stop)";

    private readonly Dictionary<BenchmarkResolution, Mat> Mats = new();

    private int _referenceSelectedIndex;
    private int _testSelectedIndex;
    private int _threads = -1;

    //private readonly RNGCryptoServiceProvider _randomProvider = new();

    private CancellationTokenSource _tokenSource = null!;

    public BenchmarkWindow()
    {
        InitializeComponent();

        DataContext = this;

        foreach (var resolution in Enum.GetValues<BenchmarkResolution>())
        {
            Mats.Add(resolution, GetBenchmarkMat(resolution));
        }
    }

    private CancellationToken _token => _tokenSource.Token;

    public static BenchmarkMachine[] BenchmarkMachines =>
        [
            new(
                "Intel® Core™ i9-13900K @ 5.5 GHz",
                "G.Skill Trident Z5 64GB DDR5-6400MHz CL32",
                [
                    /*CBBDLP 4K Encode*/new BenchmarkTestResult(1666.67f, 25000f),
                    /*CBBDLP 8K Encode*/new BenchmarkTestResult(338.98f, 5813.95f),
                    /*CBT 4K Encode*/new BenchmarkTestResult(952.38f, 11904.76f),
                    /*CBT 8K Encode*/new BenchmarkTestResult(246.91f, 3067.48f),
                    /*PW0 4K Encode*/new BenchmarkTestResult(952.38f, 12500f),
                    /*PW0 8K Encode*/new BenchmarkTestResult(238.1f, 3030.3f),
                    /*PNG 4K Compress*/new BenchmarkTestResult(25.58f, 373.69f),
                    /*PNG 8K Compress*/new BenchmarkTestResult(6.37f, 89.73f),
                    /*GZip 4K Compress*/new BenchmarkTestResult(400f, 5882.35f),
                    /*GZip 8K Compress*/new BenchmarkTestResult(101.52f, 1510.57f),
                    /*Deflate 4K Compress*/new BenchmarkTestResult(400f, 5952.38f),
                    /*Deflate 8K Compress*/new BenchmarkTestResult(106.95f, 1572.33f),
                    /*Brotli 4K Compress*/new BenchmarkTestResult(555.56f, 10204.08f),
                    /*Brotli 8K Compress*/new BenchmarkTestResult(181.82f, 3246.75f),
                    /*LZ4 4K Compress*/new BenchmarkTestResult(1111.11f, 17857.14f),
                    /*LZ4 8K Compress*/new BenchmarkTestResult(307.69f, 5208.33f),
                    /*Zstd 4K Compress*/new BenchmarkTestResult(487.8f, 7692.31f),
                    /*Zstd 8K Compress*/new BenchmarkTestResult(151.52f, 2500f),
                    /*GC Memory Copy 4K*/new BenchmarkTestResult(3333.33f, 4385.96f),
                    /*GC Memory Copy 8K*/new BenchmarkTestResult(555.56f, 1655.63f),
                    /*Pooled Memory Copy 4K*/new BenchmarkTestResult(4000, 9259.26f),
                    /*Pooled Memory Copy 8K*/new BenchmarkTestResult(645.16f, 1893.94f),
                    /*Stress CPU test*/new BenchmarkTestResult(0f, 0f),
                ]
            ),
            new(
                "Intel® Core™ i9-9900K @ 5.0 GHz",
                "G.Skill Trident Z 32GB DDR4-3200MHz CL14",
                [
                    /*CBBDLP 4K Encode*/new BenchmarkTestResult(108.70f, 912.41f),
                    /*CBBDLP 8K Encode*/new BenchmarkTestResult(27.47f, 226.76f),
                    /*CBT 4K Encode*/new BenchmarkTestResult(86.96f, 782.47f),
                    /*CBT 8K Encode*/new BenchmarkTestResult(21.86f, 196.15f),
                    /*PW0 4K Encode*/new BenchmarkTestResult(84.03f, 886.53f),
                    /*PW0 8K Encode*/new BenchmarkTestResult(21.05f, 221.63f),
                    /*PNG 4K Compress*/new BenchmarkTestResult(55.25f, 501.00f),
                    /*PNG 8K Compress*/new BenchmarkTestResult(14.28f, 124.10f),
                    /*GZip 4K Compress*/new BenchmarkTestResult(169.49f, 1506.02f),
                    /*GZip 8K Compress*/new BenchmarkTestResult(45.77f, 397.47f),
                    /*Deflate 4K Compress*/new BenchmarkTestResult(170.94f, 1592.36f),
                    /*Deflate 8K Compress*/new BenchmarkTestResult(46.30f, 406.50f),
                    /*Brotli 4K Compress*/new BenchmarkTestResult(0, 0),
                    /*Brotli 8K Compress*/new BenchmarkTestResult(0, 0),
                    /*LZ4 4K Compress*/new BenchmarkTestResult(665.12f, 2762.43f),
                    /*LZ4 8K Compress*/new BenchmarkTestResult(148.15f, 907.44f),
                    /*Zstd 4K Compress*/new BenchmarkTestResult(0, 0),
                    /*Zstd 8K Compress*/new BenchmarkTestResult(0, 0),
                    /*GC Memory Copy 4K*/new BenchmarkTestResult(0, 0),
                    /*GC Memory Copy 8K*/new BenchmarkTestResult(0, 0),
                    /*Pooled Memory Copy 4K*/new BenchmarkTestResult(0, 0),
                    /*Pooled Memory Copy 8K*/new BenchmarkTestResult(0, 0),
                    /*Stress CPU test*/new BenchmarkTestResult(0f, 0f),
                ]
            ),
        ];

    public static BenchmarkTest[] Tests =>
        [
            new("CBBDLP 4K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution4K),
            new("CBBDLP 8K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution8K),
            new("CBT 4K Encode", "TestCBTEncode", BenchmarkResolution.Resolution4K),
            new("CBT 8K Encode", "TestCBTEncode", BenchmarkResolution.Resolution8K),
            new("PW0 4K Encode", "TestPW0Encode", BenchmarkResolution.Resolution4K),
            new("PW0 8K Encode", "TestPW0Encode", BenchmarkResolution.Resolution8K),
            new("PNG 4K Compress", "TestPNGCompress", BenchmarkResolution.Resolution4K),
            new("PNG 8K Compress", "TestPNGCompress", BenchmarkResolution.Resolution8K),
            new("GZip 4K Compress", "TestGZipCompress", BenchmarkResolution.Resolution4K),
            new("GZip 8K Compress", "TestGZipCompress", BenchmarkResolution.Resolution8K),
            new("Deflate 4K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution4K),
            new("Deflate 8K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution8K),
            new("Brotli 4K Compress", "TestBrotliCompress", BenchmarkResolution.Resolution4K),
            new("Brotli 8K Compress", "TestBrotliCompress", BenchmarkResolution.Resolution8K),
            new("LZ4 4K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution4K),
            new("LZ4 8K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution8K),
            new("Zstd 4K Compress", "TestZstdCompress", BenchmarkResolution.Resolution4K),
            new("Zstd 8K Compress", "TestZstdCompress", BenchmarkResolution.Resolution8K),
            new("GC Memory Copy 4K", "TestGCMemoryCopy", BenchmarkResolution.Resolution4K),
            new("GC Memory Copy 8K", "TestGCMemoryCopy", BenchmarkResolution.Resolution8K),
            new("Pooled Memory Copy 4K", "TestPooledMemoryCopy", BenchmarkResolution.Resolution4K),
            new("Pooled Memory Copy 8K", "TestPooledMemoryCopy", BenchmarkResolution.Resolution8K),
            new(StressCPUTestName, "TestCBTEncode", BenchmarkResolution.Resolution4K),
        ];

    public string Description =>
        "Benchmark your machine against pre-defined tests.\n"
        + "This will use all computation power available, CPU will be exhausted.\n"
        + "Run the test while your PC is idle or not in heavy load.\n"
        + "Results are in 'tests done per second' (TDPS) and uses fastest compression mode";

    public static string? ProcessorName => HostSystem.ProcessorName;

    public int ReferenceSelectedIndex
    {
        get => _referenceSelectedIndex;
        set
        {
            if (!RaiseAndSetIfChanged(ref _referenceSelectedIndex, value))
                return;
            UpdateReferenceResults();
            ResetDifferenceValues();
        }
    }

    public int TestSelectedIndex
    {
        get => _testSelectedIndex;
        set
        {
            if (!RaiseAndSetIfChanged(ref _testSelectedIndex, value))
                return;
            UpdateReferenceResults();
            ResetDifferenceValues();
            SingleThreadTDPS = $"0 {RunsAbbreviation}";
            MultiThreadTDPS = $"0 {RunsAbbreviation}";
        }
    }

    public int Threads
    {
        get => _threads;
        set => RaiseAndSetIfChanged(ref _threads, value);
    }

    public string SingleThreadTDPS
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = $"0 {RunsAbbreviation}";

    public string MultiThreadTDPS
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = $"0 {RunsAbbreviation}";

    public string DevSingleThreadTDPS
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } =
        $"{BenchmarkMachines[0][0].SingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / BenchmarkMachines[0][0].SingleThreadResult, 2)}s)";

    public string DevMultiThreadTDPS
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } =
        $"{BenchmarkMachines[0][0].MultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / BenchmarkMachines[0][0].MultiThreadResult, 2)}s)";

    public double SingleThreadDiffValue
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    }

    public double SingleThreadDiffMaxValue
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = 100;

    public double MultiThreadDiffValue
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    }

    public double MultiThreadDiffMaxValue
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = 100;

    public IBrush SingleThreadDiffForeground
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = null!;

    public IBrush MultiThreadDiffForeground
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = null!;

    public string StartStopButtonText
    {
        get;
        set => RaiseAndSetIfChanged(ref field, value);
    } = "Start";

    public bool IsRunning
    {
        get;
        set
        {
            if (!RaiseAndSetIfChanged(ref field, value))
                return;
            StartStopButtonText = field ? "Stop" : "Start";
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        foreach (var mat in Mats)
        {
            mat.Value.Dispose();
        }

        base.OnClosed(e);
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = IsRunning;
        base.OnClosing(e);
    }

    private int GetMaxDegreeOfParallelism()
    {
        if (_threads <= -2)
            return CoreSettings.OptimalMaxDegreeOfParallelism;
        if (_threads == -1)
            return -1;
        if (_threads == 0)
            return Environment.ProcessorCount;
        return _threads;
    }

    public void StartStop()
    {
        if (IsRunning)
        {
            if (!_token.CanBeCanceled || _token.IsCancellationRequested)
                return;
            _tokenSource.Cancel();
        }
        else
        {
            var benchmark = Tests[_testSelectedIndex];
            SingleThreadTDPS = $"Running {SingleThreadTests} tests";
            MultiThreadTDPS = $"Running {MultiThreadTests} tests";

            ResetDifferenceValues();

            _tokenSource = new CancellationTokenSource();
            var theMethod = GetType().GetMethod(benchmark.FunctionName)!;

            Task.Factory.StartNew(
                () =>
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        if (benchmark.Name.Equals(StressCPUTestName))
                        {
                            while (true)
                            {
                                Parallel.For(
                                    0,
                                    MultiThreadTests,
                                    new ParallelOptions
                                    {
                                        MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(),
                                        CancellationToken = _tokenSource.Token,
                                    },
                                    i =>
                                    {
                                        theMethod.Invoke(this, [benchmark.Resolution]);
                                    }
                                );
                            }
                        }

                        for (var i = 0; i < SingleThreadTests; i++)
                        {
                            if (_token.IsCancellationRequested)
                                _token.ThrowIfCancellationRequested();
                            theMethod.Invoke(this, [benchmark.Resolution]);
                        }

                        sw.Stop();
                        var singleMilliseconds = sw.ElapsedMilliseconds;
                        Dispatcher.UIThread.InvokeAsync(() =>
                            UpdateResults(true, singleMilliseconds)
                        );

                        if (_token.IsCancellationRequested)
                            _token.ThrowIfCancellationRequested();

                        sw.Restart();
                        Parallel.For(
                            0,
                            MultiThreadTests,
                            new ParallelOptions
                            {
                                MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(),
                                CancellationToken = _tokenSource.Token,
                            },
                            i =>
                            {
                                theMethod.Invoke(this, [benchmark.Resolution]);
                            }
                        );

                        sw.Stop();

                        var multiMilliseconds = sw.ElapsedMilliseconds;
                        Dispatcher.UIThread.InvokeAsync(() =>
                            UpdateResults(false, multiMilliseconds)
                        );
                    }
                    catch (OperationCanceledException) { }
                    catch (Exception ex)
                    {
                        Dispatcher.UIThread.InvokeAsync(() =>
                            this.MessageBoxError(ex.ToString(), "Error")
                        );
                    }
                    finally
                    {
                        Dispatcher.UIThread.InvokeAsync(() => IsRunning = !IsRunning);
                    }
                },
                _token
            );

            IsRunning = !IsRunning;
        }
    }

    private void ResetDifferenceValues()
    {
        SingleThreadDiffValue = 0;
        MultiThreadDiffValue = 0;
        SingleThreadDiffMaxValue = 100;
        MultiThreadDiffMaxValue = 100;
        SingleThreadDiffForeground = Brushes.CornflowerBlue;
        MultiThreadDiffForeground = Brushes.CornflowerBlue;
    }

    private void UpdateReferenceResults()
    {
        if (_referenceSelectedIndex < 0 || _testSelectedIndex < 0)
            return;
        DevSingleThreadTDPS =
            $"{BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult, 2)}s)";
        DevMultiThreadTDPS =
            $"{BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult, 2)}s)";
    }

    private void UpdateResults(bool isSingleThread, long milliseconds)
    {
        var seconds = Math.Round(milliseconds / 1000m, 2);

        if (isSingleThread)
        {
            var result = (double)Math.Round(SingleThreadTests / seconds, 2);
            SingleThreadTDPS =
                $"{result} {RunsAbbreviation} ({SingleThreadTests} tests / {seconds}s)";
            var diff =
                BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult
                > 0
                    ? result
                        * 100.0
                        / BenchmarkMachines[_referenceSelectedIndex][
                            _testSelectedIndex
                        ].SingleThreadResult
                    : result;

            SingleThreadDiffForeground = diff switch
            {
                < 90 => Brushes.DarkRed,
                > 110 => Brushes.Green,
                _ => Brushes.CornflowerBlue,
            };

            SingleThreadDiffMaxValue = Math.Max(100, diff);
            SingleThreadDiffValue = diff;
        }
        else
        {
            var result = (double)Math.Round(MultiThreadTests / seconds, 2);
            MultiThreadTDPS =
                $"{result} {RunsAbbreviation} ({MultiThreadTests} tests / {seconds}s)";

            var diff =
                BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult > 0
                    ? result
                        * 100.0
                        / BenchmarkMachines[_referenceSelectedIndex][
                            _testSelectedIndex
                        ].MultiThreadResult
                    : result;

            MultiThreadDiffForeground = diff switch
            {
                < 90 => Brushes.DarkRed,
                > 110 => Brushes.Green,
                _ => Brushes.CornflowerBlue,
            };

            MultiThreadDiffMaxValue = Math.Max(100, diff);
            MultiThreadDiffValue = diff;
        }
    }

    #region Tests

    // Uses the same encoders the file formats use
    public byte[] EncodeCbddlpImage(Mat image) =>
        BitPlaneRle.EncodePlane(image.GetReadOnlySpanOfBytes(), 127, 0x7d);

    private byte[] EncodeCbtImage(Mat image) => CtbRleCodec.Encode(image.GetReadOnlySpanOfBytes());

    public byte[] EncodePW0Image(Mat image) => AnycubicFile.EncodePW0(image);

    public static Mat RandomMat(int width, int height)
    {
        Mat mat = new(new Size(width, height), DepthType.Cv8U, 1);
        CvInvoke.Randu(mat, EmguCvExtensions.BlackColor, EmguCvExtensions.WhiteColor);
        return mat;
    }

    public static Mat GetBenchmarkMat(BenchmarkResolution resolution = default)
    {
        using var stream = App.GetAsset("/Assets/benchmark.png");
        var mat4K = new Mat();
        CvInvoke.Imdecode(stream.ToArray(), ImreadModes.Grayscale, mat4K);
        switch (resolution)
        {
            case BenchmarkResolution.Resolution4K:
                return mat4K;
            case BenchmarkResolution.Resolution8K:
                var mat8K = new Mat();
                CvInvoke.Repeat(mat4K, 2, 2, mat8K);
                mat4K.Dispose();
                return mat8K;
            default:
                throw new ArgumentOutOfRangeException(nameof(resolution), resolution, null);
        }
    }

    public void Test4KRandomCBBDLPEncode()
    {
        using var mat = RandomMat(3840, 2160);
        EncodeCbddlpImage(mat);
    }

    public void Test8KRandomCBBDLPEncode()
    {
        using var mat = RandomMat(7680, 4320);
        EncodeCbddlpImage(mat);
    }

    public void TestCBBDLPEncode(BenchmarkResolution resolution)
    {
        EncodeCbddlpImage(Mats[resolution]);
    }

    public void Test4KRandomCBTEncode()
    {
        using var mat = RandomMat(3840, 2160);
        EncodeCbtImage(mat);
    }

    public void Test8KRandomCBTEncode()
    {
        using var mat = RandomMat(7680, 4320);
        EncodeCbtImage(mat);
    }

    public void TestCBTEncode(BenchmarkResolution resolution)
    {
        EncodeCbtImage(Mats[resolution]);
    }

    public void Test4KRandomPW0Encode()
    {
        using var mat = RandomMat(3840, 2160);
        EncodePW0Image(mat);
    }

    public void Test8KRandomPW0Encode()
    {
        using var mat = RandomMat(7680, 4320);
        EncodePW0Image(mat);
    }

    public void TestPW0Encode(BenchmarkResolution resolution)
    {
        EncodePW0Image(Mats[resolution]);
    }

    public void TestPNGCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Png);
        MatCompressorPng.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestPNGDecompress(BenchmarkResolution resolution) { }

    public void TestGZipCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.GZip);
        MatCompressorGZip.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestGZipDecompress(BenchmarkResolution resolution) { }

    public void TestDeflateCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Deflate);
        MatCompressorDeflate.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestDeflateDecompress(BenchmarkResolution resolution) { }

    public void TestBrotliCompress(BenchmarkResolution resolution)
    {
        MatCompressorBrotli.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestLZ4Compress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Lz4);
        MatCompressorLz4.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestLZ4Decompress(BenchmarkResolution resolution) { }

    public void TestZstdCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Lz4);
        MatCompressorZstd.Instance.Compress(Mats[resolution], CompressionLevel.Fastest);
    }

    public void TestZstdDecompress(BenchmarkResolution resolution) { }

    public void TestGCMemoryCopy(BenchmarkResolution resolution)
    {
        var bytes = Mats[resolution].ToArray();
    }

    public void TestPooledMemoryCopy(BenchmarkResolution resolution)
    {
        var spanSrc = Mats[resolution].GetReadOnlySpanOfBytes();
        var bytes = ArrayPool<byte>.Shared.Rent(spanSrc.Length);
        spanSrc.CopyTo(bytes.AsSpan());
        ArrayPool<byte>.Shared.Return(bytes);
    }

    #endregion
}
