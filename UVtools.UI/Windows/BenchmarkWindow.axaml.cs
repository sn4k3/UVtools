using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using UVtools.Core;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.UI.Controls;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;
using UVtools.Core.SystemOS;

namespace UVtools.UI.Windows;

public partial class BenchmarkWindow : WindowEx
{
    private int _referenceSelectedIndex;
    private int _testSelectedIndex;
    private string _singleThreadTdps = $"0 {RunsAbbreviation}";
    private string _multiThreadTdps = $"0 {RunsAbbreviation}";
    private string _devSingleThreadTdps = $"{BenchmarkMachines[0][0].SingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / BenchmarkMachines[0][0].SingleThreadResult, 2)}s)";
    private string _devMultiThreadTdps = $"{BenchmarkMachines[0][0].MultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / BenchmarkMachines[0][0].MultiThreadResult, 2)}s)";
    private double _singleThreadDiffValue;
    private double _singleThreadDiffMaxValue = 100;
    private double _multiThreadDiffValue;
    private double _multiThreadDiffMaxValue = 100;
    private bool _isRunning;
    private string _startStopButtonText = "Start";
    private const ushort SingleThreadTests = 200;
    private const ushort MultiThreadTests = 5000;

    public const string RunsAbbreviation = "TDPS";
    public const string StressCPUTestName = "Stress CPU (Run until stop)";

    private readonly Dictionary<BenchmarkResolution, Mat> Mats = new();

    //private readonly RNGCryptoServiceProvider _randomProvider = new();

    private CancellationTokenSource _tokenSource = null!;
    private IBrush _singleThreadDiffForeground = null!;
    private IBrush _multiThreadDiffForeground = null!;
    private int _threads = -1;

    private CancellationToken _token => _tokenSource.Token;

    public enum BenchmarkResolution
    {
        Resolution4K,
        Resolution8K
    }

    public static BenchmarkMachine[] BenchmarkMachines =>
    [
        new BenchmarkMachine("Intel® Core™ i9-13900K @ 5.5 GHz", "G.Skill Trident Z5 64GB DDR5-6400MHz CL32", [
            /*CBBDLP 4K Encode*/    new BenchmarkTestResult(200.0f, 3355.70f),
                /*CBBDLP 8K Encode*/    new BenchmarkTestResult(49.38f, 847.46f),
                /*CBT 4K Encode*/       new BenchmarkTestResult(162.60f, 2463.05f),
                /*CBT 8K Encode*/       new BenchmarkTestResult(39.45f, 617.28f),
                /*PW0 4K Encode*/       new BenchmarkTestResult(196.08f, 2824.86f),
                /*PW0 8K Encode*/       new BenchmarkTestResult(47.06f, 697.35f),
                /*PNG 4K Compress*/     new BenchmarkTestResult(88.5f, 1479.29f),
                /*PNG 8K Compress*/     new BenchmarkTestResult(22.68f, 369.28f),
                /*GZip 4K Compress*/    new BenchmarkTestResult(250f, 4237.29f),
                /*GZip 8K Compress*/    new BenchmarkTestResult(65.57f, 1091.7f),
                /*Deflate 4K Compress*/ new BenchmarkTestResult(256.41f, 4273.5f),
                /*Deflate 8K Compress*/ new BenchmarkTestResult(66.67f, 1103.75f),
                /*Brotli 4K Compress*/  new BenchmarkTestResult(666.67f, 13157.89f),
                /*Brotli 8K Compress*/  new BenchmarkTestResult(198.02f, 3968.25f),
                /*LZ4 4K Compress*/     new BenchmarkTestResult(1250f, 20833.33f),
                /*LZ4 8K Compress*/     new BenchmarkTestResult(344.83f, 6250f),
                /*GC Memory Copy 4K*/   new BenchmarkTestResult(952.38f, 2304.15f),
                /*GC Memory Copy 8K*/   new BenchmarkTestResult(266.67f, 758.73f),
                /*Pooled Memory Copy 4K*/new BenchmarkTestResult(5000, 9433.96f),
                /*Pooled Memory Copy 8K*/new BenchmarkTestResult(769.23f, 2074.69f),
                /*Stress CPU test*/     new BenchmarkTestResult(0f, 0f)
        ]),
            new BenchmarkMachine("Intel® Core™ i9-9900K @ 5.0 GHz", "G.Skill Trident Z 32GB DDR4-3200MHz CL14", [
            /*CBBDLP 4K Encode*/    new BenchmarkTestResult(108.70f, 912.41f),
                /*CBBDLP 8K Encode*/    new BenchmarkTestResult(27.47f, 226.76f),
                /*CBT 4K Encode*/       new BenchmarkTestResult(86.96f, 782.47f),
                /*CBT 8K Encode*/       new BenchmarkTestResult(21.86f, 196.15f),
                /*PW0 4K Encode*/       new BenchmarkTestResult(84.03f, 886.53f),
                /*PW0 8K Encode*/       new BenchmarkTestResult(21.05f, 221.63f),
                /*PNG 4K Compress*/     new BenchmarkTestResult(55.25f, 501.00f),
                /*PNG 8K Compress*/     new BenchmarkTestResult(14.28f, 124.10f),
                /*GZip 4K Compress*/    new BenchmarkTestResult(169.49f, 1506.02f),
                /*GZip 8K Compress*/    new BenchmarkTestResult(45.77f, 397.47f),
                /*Deflate 4K Compress*/ new BenchmarkTestResult(170.94f, 1592.36f),
                /*Deflate 8K Compress*/ new BenchmarkTestResult(46.30f, 406.50f),
                /*Brotli 4K Compress*/  new BenchmarkTestResult(0, 0),
                /*Brotli 8K Compress*/  new BenchmarkTestResult(0, 0),
                /*LZ4 4K Compress*/     new BenchmarkTestResult(665.12f, 2762.43f),
                /*LZ4 8K Compress*/     new BenchmarkTestResult(148.15f, 907.44f),
                /*GC Memory Copy 4K*/   new BenchmarkTestResult(0, 0),
                /*GC Memory Copy 8K*/   new BenchmarkTestResult(0, 0),
                /*Pooled Memory Copy 4K*/new BenchmarkTestResult(0, 0),
                /*Pooled Memory Copy 8K*/new BenchmarkTestResult(0, 0),
                /*Stress CPU test*/     new BenchmarkTestResult(0f, 0f)
            ])
    ];

    public static BenchmarkTest[] Tests =>
    [
        new BenchmarkTest("CBBDLP 4K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("CBBDLP 8K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution8K),
            new BenchmarkTest("CBT 4K Encode", "TestCBTEncode", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("CBT 8K Encode", "TestCBTEncode", BenchmarkResolution.Resolution8K),
            new BenchmarkTest("PW0 4K Encode", "TestPW0Encode", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("PW0 8K Encode", "TestPW0Encode", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("PNG 4K Compress", "TestPNGCompress", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("PNG 8K Compress", "TestPNGCompress", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("GZip 4K Compress", "TestGZipCompress", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("GZip 8K Compress", "TestGZipCompress", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("Deflate 4K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("Deflate 8K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("Brotli 4K Compress", "TestBrotliCompress", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("Brotli 8K Compress", "TestBrotliCompress", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("LZ4 4K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("LZ4 8K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("GC Memory Copy 4K", "TestGCMemoryCopy", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("GC Memory Copy 8K", "TestGCMemoryCopy", BenchmarkResolution.Resolution8K),

            new BenchmarkTest("Pooled Memory Copy 4K", "TestPooledMemoryCopy", BenchmarkResolution.Resolution4K),
            new BenchmarkTest("Pooled Memory Copy 8K", "TestPooledMemoryCopy", BenchmarkResolution.Resolution8K),

            new BenchmarkTest(StressCPUTestName, "TestCBTEncode", BenchmarkResolution.Resolution4K)
    ];

    public string Description => "Benchmark your machine against pre-defined tests.\n" +
                                 "This will use all computation power available, CPU will be exhausted.\n" +
                                 "Run the test while your PC is idle or not in heavy load.\n" +
                                 "Results are in 'tests done per second' (TDPS)";

    public static string? ProcessorName => SystemAware.GetProcessorName();

    public int ReferenceSelectedIndex
    {
        get => _referenceSelectedIndex;
        set
        {
            if(!RaiseAndSetIfChanged(ref _referenceSelectedIndex, value)) return;
            UpdateReferenceResults();
            ResetDifferenceValues();
        }
    }

    public int TestSelectedIndex
    {
        get => _testSelectedIndex;
        set
        {
            if(!RaiseAndSetIfChanged(ref _testSelectedIndex, value)) return;
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
        get => _singleThreadTdps;
        set => RaiseAndSetIfChanged(ref _singleThreadTdps, value);
    }

    public string MultiThreadTDPS
    {
        get => _multiThreadTdps;
        set => RaiseAndSetIfChanged(ref _multiThreadTdps, value);
    }

    public string DevSingleThreadTDPS
    {
        get => _devSingleThreadTdps;
        set => RaiseAndSetIfChanged(ref _devSingleThreadTdps, value);
    }

    public string DevMultiThreadTDPS
    {
        get => _devMultiThreadTdps;
        set => RaiseAndSetIfChanged(ref _devMultiThreadTdps, value);
    }

    public double SingleThreadDiffValue
    {
        get => _singleThreadDiffValue;
        set => RaiseAndSetIfChanged(ref _singleThreadDiffValue, value);
    }

    public double SingleThreadDiffMaxValue
    {
        get => _singleThreadDiffMaxValue;
        set => RaiseAndSetIfChanged(ref _singleThreadDiffMaxValue, value);
    }

    public double MultiThreadDiffValue
    {
        get => _multiThreadDiffValue;
        set => RaiseAndSetIfChanged(ref _multiThreadDiffValue, value);
    }

    public double MultiThreadDiffMaxValue
    {
        get => _multiThreadDiffMaxValue;
        set => RaiseAndSetIfChanged(ref _multiThreadDiffMaxValue, value);
    }

    public IBrush SingleThreadDiffForeground
    {
        get => _singleThreadDiffForeground;
        set => RaiseAndSetIfChanged(ref _singleThreadDiffForeground, value);
    }

    public IBrush MultiThreadDiffForeground
    {
        get => _multiThreadDiffForeground;
        set => RaiseAndSetIfChanged(ref _multiThreadDiffForeground, value);
    }

    public string StartStopButtonText
    {
        get => _startStopButtonText;
        set => RaiseAndSetIfChanged(ref _startStopButtonText, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if(!RaiseAndSetIfChanged(ref _isRunning, value)) return;
            StartStopButtonText = _isRunning ? "Stop" : "Start";
        }
    }

    public BenchmarkWindow()
    {
        InitializeComponent();

        DataContext = this;


        foreach (var resolution in Enum.GetValues<BenchmarkResolution>())
        {
            Mats.Add(resolution, GetBenchmarkMat(resolution));
        }
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        e.Cancel = IsRunning;
        base.OnClosing(e);
    }

    private int GetMaxDegreeOfParallelism()
    {
        if (_threads <= -2) return CoreSettings.OptimalMaxDegreeOfParallelism;
        if (_threads == -1) return -1;
        if (_threads == 0) return Environment.ProcessorCount;
        return _threads;
    }

    public void StartStop()
    {
        if (IsRunning)
        {
            if (!_token.CanBeCanceled || _token.IsCancellationRequested) return;
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

            Task.Factory.StartNew(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    if (benchmark.Name.Equals(StressCPUTestName))
                    {
                        while (true)
                        {
                            Parallel.For(0, MultiThreadTests, new ParallelOptions{ MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(), CancellationToken = _tokenSource.Token }, i =>
                            {
                                theMethod.Invoke(this, [benchmark.Resolution]);
                            });
                        }
                    }


                    for (int i = 0; i < SingleThreadTests; i++)
                    {
                        if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();
                        theMethod.Invoke(this, [benchmark.Resolution]);
                    }

                    sw.Stop();
                    var singleMilliseconds = sw.ElapsedMilliseconds;
                    Dispatcher.UIThread.InvokeAsync(() => UpdateResults(true, singleMilliseconds));

                    if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();

                    sw.Restart();
                    Parallel.For(0, MultiThreadTests, new ParallelOptions { MaxDegreeOfParallelism = GetMaxDegreeOfParallelism(), CancellationToken = _tokenSource.Token }, i =>
                    {
                        theMethod.Invoke(this, [benchmark.Resolution]);
                    });

                    sw.Stop();

                    var multiMilliseconds = sw.ElapsedMilliseconds;
                    Dispatcher.UIThread.InvokeAsync(() => UpdateResults(false, multiMilliseconds));
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.InvokeAsync(() => this.MessageBoxError(ex.ToString(), "Error"));

                }
                finally
                {
                    Dispatcher.UIThread.InvokeAsync(() => IsRunning = !IsRunning);
                }
            }, _token);

            IsRunning = !IsRunning;
        }
    }

    private void ResetDifferenceValues()
    {
        SingleThreadDiffValue = 0;
        MultiThreadDiffValue = 0;
        SingleThreadDiffMaxValue = 100;
        MultiThreadDiffMaxValue = 100;
        SingleThreadDiffForeground = Avalonia.Media.Brushes.CornflowerBlue;
        MultiThreadDiffForeground = Avalonia.Media.Brushes.CornflowerBlue;
    }

    private void UpdateReferenceResults()
    {
        if (_referenceSelectedIndex < 0 || _testSelectedIndex < 0) return;
        DevSingleThreadTDPS = $"{BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult, 2)}s)";
        DevMultiThreadTDPS = $"{BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult, 2)}s)";
    }

    private void UpdateResults(bool isSingleThread, long milliseconds)
    {
        decimal seconds = Math.Round(milliseconds / 1000m, 2);

        if (isSingleThread)
        {
            var result = (double)Math.Round(SingleThreadTests / seconds, 2);
            SingleThreadTDPS = $"{result} {RunsAbbreviation} ({SingleThreadTests} tests / {seconds}s)";
            var diff = BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult > 0
            ? result * 100.0 / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].SingleThreadResult
            : result;

            SingleThreadDiffForeground = diff switch
            {
                < 90 => Avalonia.Media.Brushes.DarkRed,
                > 110 => Avalonia.Media.Brushes.Green,
                _ => Avalonia.Media.Brushes.CornflowerBlue
            };

            SingleThreadDiffMaxValue = Math.Max(100, diff);
            SingleThreadDiffValue = diff;
        }
        else
        {
            var result = (double)Math.Round(MultiThreadTests / seconds, 2);
            MultiThreadTDPS = $"{result} {RunsAbbreviation} ({MultiThreadTests} tests / {seconds}s)";

            var diff = BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult > 0
                ? result * 100.0 / BenchmarkMachines[_referenceSelectedIndex][_testSelectedIndex].MultiThreadResult
                : result;

            MultiThreadDiffForeground = diff switch
            {
                < 90 => Avalonia.Media.Brushes.DarkRed,
                > 110 => Avalonia.Media.Brushes.Green,
                _ => Avalonia.Media.Brushes.CornflowerBlue
            };

            MultiThreadDiffMaxValue = Math.Max(100, diff);
            MultiThreadDiffValue = diff;
        }
    }

    #region Tests
    public byte[] EncodeCbddlpImage(Mat image, byte bit = 0)
    {
        List<byte> rawData = [];
        var span = image.GetDataByteReadOnlySpan();

        bool obit = false;
        int rep = 0;

        //ngrey:= uint16(r | g | b)
        // thresholds:
        // aa 1:  127
        // aa 2:  255 127
        // aa 4:  255 191 127 63
        // aa 8:  255 223 191 159 127 95 63 31
        byte threshold = (byte)(256 / 1 * bit - 1);

        void AddRep()
        {
            if (rep <= 0) return;

            byte by = (byte)rep;

            if (obit)
            {
                by |= 0x80;
                //bitsOn += uint(rep)
            }

            rawData.Add(by);
        }

        for (int pixel = 0; pixel < span.Length; pixel++)
        {
            var nbit = span[pixel] >= threshold;

            if (nbit == obit)
            {
                rep++;

                if (rep == 0x7d)
                {
                    AddRep();
                    rep = 0;
                }
            }
            else
            {
                AddRep();
                obit = nbit;
                rep = 1;
            }
        }

        // Collect stragglers
        AddRep();


        return rawData.ToArray();
    }

    private byte[] EncodeCbtImage(Mat image)
    {
        List<byte> rawData = [];
        byte color = byte.MaxValue >> 1;
        uint stride = 0;
        var span = image.GetDataByteReadOnlySpan();

        void AddRep()
        {
            if (stride == 0)
            {
                return;
            }

            if (stride > 1)
            {
                color |= 0x80;
            }
            rawData.Add(color);

            if (stride <= 1)
            {
                // no run needed
                return;
            }

            if (stride <= 0x7f)
            {
                rawData.Add((byte)stride);
                return;
            }

            if (stride <= 0x3fff)
            {
                rawData.Add((byte)((stride >> 8) | 0x80));
                rawData.Add((byte)stride);
                return;
            }

            if (stride <= 0x1fffff)
            {
                rawData.Add((byte)((stride >> 16) | 0xc0));
                rawData.Add((byte)(stride >> 8));
                rawData.Add((byte)stride);
                return;
            }

            if (stride <= 0xfffffff)
            {
                rawData.Add((byte)((stride >> 24) | 0xe0));
                rawData.Add((byte)(stride >> 16));
                rawData.Add((byte)(stride >> 8));
                rawData.Add((byte)stride);
            }

        }


        for (int pixel = 0; pixel < span.Length; pixel++)
        {
            var grey7 = (byte)(span[pixel] >> 1);

            if (grey7 == color)
            {
                stride++;
            }
            else
            {
                AddRep();
                color = grey7;
                stride = 1;
            }
        }

        AddRep();


        return rawData.ToArray();
    }

    public byte[] EncodePW0Image(Mat image)
    {
        List<byte> rawData = [];
        var span = image.GetDataByteReadOnlySpan();

        int lastColor = -1;
        int reps = 0;

        void PutReps()
        {
            while (reps > 0)
            {
                int done = reps;

                if (lastColor == 0 || lastColor == 0xf)
                {
                    if (done > 0xfff)
                    {
                        done = 0xfff;
                    }
                    //more:= []byte{ 0, 0}
                    //binary.BigEndian.PutUint16(more, uint16(done | (color << 12)))

                    //rle = append(rle, more...)

                    ushort more = (ushort)(done | (lastColor << 12));
                    rawData.Add((byte)(more >> 8));
                    rawData.Add((byte)more);
                }
                else
                {
                    if (done > 0xf)
                    {
                        done = 0xf;
                    }
                    rawData.Add((byte)(done | lastColor << 4));
                }

                reps -= done;
            }
        }

        for (int i = 0; i < span.Length; i++)
        {
            int color = span[i] >> 4;

            if (color == lastColor)
            {
                reps++;
            }
            else
            {
                PutReps();
                lastColor = color;
                reps = 1;
            }
        }

        PutReps();

        return rawData.ToArray();
    }

    public static Mat RandomMat(int width, int height)
    {
        Mat mat = new(new Size(width, height), DepthType.Cv8U, 1);
        CvInvoke.Randu(mat, EmguExtensions.BlackColor, EmguExtensions.WhiteColor);
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
        MatCompressorPngGreyScale.Instance.Compress(Mats[resolution]);
    }

    public void TestPNGDecompress(BenchmarkResolution resolution)
    { }

    public void TestGZipCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.GZip);
        MatCompressorGZip.Instance.Compress(Mats[resolution]);
    }

    public void TestGZipDecompress(BenchmarkResolution resolution)
    { }

    public void TestDeflateCompress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Deflate);
        MatCompressorDeflate.Instance.Compress(Mats[resolution]);
    }

    public void TestDeflateDecompress(BenchmarkResolution resolution)
    { }

    public void TestBrotliCompress(BenchmarkResolution resolution)
    {
        MatCompressorBrotli.Instance.Compress(Mats[resolution]);
    }

    public void TestLZ4Compress(BenchmarkResolution resolution)
    {
        //Layer.CompressMat(Mats[resolution], LayerCompressionCodec.Lz4);
        MatCompressorLz4.Instance.Compress(Mats[resolution]);
    }

    public void TestLZ4Decompress(BenchmarkResolution resolution)
    { }

    public void TestGCMemoryCopy(BenchmarkResolution resolution)
    {
        var bytes = Mats[resolution].GetBytes();
    }

    public void TestPooledMemoryCopy(BenchmarkResolution resolution)
    {
        var spanSrc = Mats[resolution].GetDataByteReadOnlySpan();
        var bytes = ArrayPool<byte>.Shared.Rent(spanSrc.Length);
        spanSrc.CopyTo(bytes.AsSpan());
        ArrayPool<byte>.Shared.Return(bytes);
    }

    #endregion
}