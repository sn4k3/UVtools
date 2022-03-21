using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows;

public class BenchmarkWindow : WindowEx
{
    private int _testSelectedIndex;
    private string _singleThreadTdps = $"0 {RunsAbbreviation}";
    private string _multiThreadTdps = $"0 {RunsAbbreviation}";
    private string _devSingleThreadTdps = $"{Tests[0].DevSingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / Tests[0].DevSingleThreadResult, 2)}s)";
    private string _devMultiThreadTdps = $"{Tests[0].DevMultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / Tests[0].DevMultiThreadResult, 2)}s)";
    private bool _isRunning;
    private string _startStopButtonText = "Start";
    private const ushort SingleThreadTests = 200;
    private const ushort MultiThreadTests = 5000;

    public const string RunsAbbreviation = "TDPS";
    public const string StressCPUTestName = "Stress CPU (Run until stop)";

    private readonly Dictionary<BenchmarkResolution, Mat> Mats = new();

    //private readonly RNGCryptoServiceProvider _randomProvider = new();

    private CancellationTokenSource _tokenSource;
    private CancellationToken _token => _tokenSource.Token;

    public enum BenchmarkResolution
    { 
        Resolution4K,
        Resolution8K
    }

    public static BenchmarkTest[] Tests =>
        new[]
        {
            new BenchmarkTest("CBBDLP 4K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution4K, 108.70f, 912.41f),
            new BenchmarkTest("CBBDLP 8K Encode", "TestCBBDLPEncode", BenchmarkResolution.Resolution8K, 27.47f, 226.76f),
            new BenchmarkTest("CBT 4K Encode", "TestCBTEncode", BenchmarkResolution.Resolution4K, 86.96f, 782.47f),
            new BenchmarkTest("CBT 8K Encode", "TestCBTEncode", BenchmarkResolution.Resolution8K, 21.86f, 196.15f),
            new BenchmarkTest("PW0 4K Encode", "TestPW0Encode",BenchmarkResolution.Resolution4K,  84.03f, 886.53f),
            new BenchmarkTest("PW0 8K Encode", "TestPW0Encode", BenchmarkResolution.Resolution8K, 21.05f, 221.63f),
            
            new BenchmarkTest("PNG 4K Compress", "TestPNGCompress", BenchmarkResolution.Resolution4K, 55.25f, 501.00f),
            //new BenchmarkTest("PNG 4K Decompress", "TestPNGDecompress", BenchmarkResolution.Resolution4K, 4.07f, 26.65f),
            new BenchmarkTest("PNG 8K Compress", "TestPNGCompress", BenchmarkResolution.Resolution8K, 14.28f, 124.10f),
            //new BenchmarkTest("PNG 8K Decompress", "TestPNGDecompress", BenchmarkResolution.Resolution8K, 4.07f, 26.65f),

            new BenchmarkTest("GZip 4K Compress", "TestGZipCompress", BenchmarkResolution.Resolution4K, 169.49f, 1506.02f),
            //new BenchmarkTest("GZip 4K Decompress", "TestGZipDecompress", BenchmarkResolution.Resolution4K, 4.07f, 26.65f),
            new BenchmarkTest("GZip 8K Compress", "TestGZipCompress", BenchmarkResolution.Resolution8K, 45.77f, 397.47f),
            //new BenchmarkTest("GZip 8K Decompress", "TestGZipDecompress", BenchmarkResolution.Resolution8K, 4.07f, 26.65f),

            new BenchmarkTest("Deflate 4K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution4K, 170.94f, 1592.36f),
            //new BenchmarkTest("Deflate 4K Decompress", "TestDeflateDecompress", BenchmarkResolution.Resolution4K, 4.07f, 26.65f),
            new BenchmarkTest("Deflate 8K Compress", "TestDeflateCompress", BenchmarkResolution.Resolution8K, 46.30f, 406.50f),
            //new BenchmarkTest("Deflate 8K Decompress", "TestDeflateDecompress", BenchmarkResolution.Resolution8K, 4.07f, 26.65f),

            new BenchmarkTest("LZ4 4K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution4K, 665.12f, 2762.43f),
            //new BenchmarkTest("LZ4 4K Decompress", "TestLZ4Decompress", BenchmarkResolution.Resolution4K, 4.07f, 26.65f),
            new BenchmarkTest("LZ4 8K Compress", "TestLZ4Compress", BenchmarkResolution.Resolution8K, 148.15f, 907.44f),
            //new BenchmarkTest("LZ4 8K Decompress", "TestLZ4Decompress", BenchmarkResolution.Resolution8K, 4.07f, 26.65f),

            new BenchmarkTest(StressCPUTestName, "TestCBTEncode", BenchmarkResolution.Resolution4K, 0, 0),
        };

    public string Description  => "Benchmark your machine against pre-defined tests.\n" +
                                  "This will use all compution power avaliable, CPU will be exhausted.\n" +
                                  "Run the test while your PC is idle or not in heavy load.\n" +
                                  "Results are in 'tests done per second' (TDPS)\n" +
                                  "\n" +
                                  "To your reference you are competing against developer system:\n"+
                                  $"CPU: {BenchmarkTest.DEVCPU}\n" +
                                  $"RAM: {BenchmarkTest.DEVRAM}";

    public int TestSelectedIndex
    {
        get => _testSelectedIndex;
        set
        {
            if(!RaiseAndSetIfChanged(ref _testSelectedIndex, value)) return;
            DevSingleThreadTDPS = $"{Tests[_testSelectedIndex].DevSingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / Tests[_testSelectedIndex].DevSingleThreadResult, 2)}s)";
            DevMultiThreadTDPS = $"{Tests[_testSelectedIndex].DevMultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / Tests[_testSelectedIndex].DevMultiThreadResult, 2)}s)";
        }
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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override bool HandleClosing() => IsRunning;

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
                
            _tokenSource = new CancellationTokenSource();
            var theMethod = GetType().GetMethod(benchmark.FunctionName);

            Task.Factory.StartNew(() =>
            {
                var sw = Stopwatch.StartNew();
                try
                {
                    if (benchmark.Name.Equals(StressCPUTestName))
                    {
                        while (true)
                        {
                            Parallel.For(0, MultiThreadTests, new ParallelOptions{CancellationToken = _tokenSource.Token }, i =>
                            {
                                theMethod.Invoke(this, new object[]{benchmark.Resolution});
                            });
                        }
                    }

                        
                    for (int i = 0; i < SingleThreadTests; i++)
                    {
                        if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();
                        theMethod.Invoke(this, new object[] { benchmark.Resolution });
                    }

                    sw.Stop();
                    var singleMiliseconds = sw.ElapsedMilliseconds;
                    Dispatcher.UIThread.InvokeAsync(() => UpdateResults(true, singleMiliseconds));

                    if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();

                    sw.Restart();
                    Parallel.For(0, MultiThreadTests, new ParallelOptions { CancellationToken = _tokenSource.Token }, i =>
                    {
                        theMethod.Invoke(this, new object[] { benchmark.Resolution });
                    });

                    sw.Stop();

                    var multiMiliseconds = sw.ElapsedMilliseconds;
                    Dispatcher.UIThread.InvokeAsync(() => UpdateResults(false, multiMiliseconds));
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

    private void UpdateResults(bool isSingleThread, long milliseconds)
    {
        decimal seconds = Math.Round(milliseconds / 1000m, 2);
        //var text = (isSingleThread ? "Single" : "Multi") + $" Thread: {Math.Round(SingleThreadTests / seconds, 2)} OPS ({seconds}s)";

        if (isSingleThread)
            SingleThreadTDPS = $"{Math.Round(SingleThreadTests / seconds, 2)} {RunsAbbreviation} ({SingleThreadTests} tests / {seconds}s)";
        else
            MultiThreadTDPS = $"{Math.Round(MultiThreadTests / seconds, 2)} {RunsAbbreviation} ({MultiThreadTests} tests / {seconds}s)";
    }

    #region Tests
    public byte[] EncodeCbddlpImage(Mat image, byte bit = 0)
    {
        List<byte> rawData = new();
        var span = image.GetDataByteSpan();

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
        List<byte> rawData = new();
        byte color = byte.MaxValue >> 1;
        uint stride = 0;
        var span = image.GetDataByteSpan();

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
        List<byte> rawData = new();
        var span = image.GetDataByteSpan();

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
        Layer.CompressMat(Mats[resolution], Layer.LayerCompressionCodec.Png); 
    }

    public void TestPNGDecompress(BenchmarkResolution resolution)
    { }

    public void TestGZipCompress(BenchmarkResolution resolution)
    {
        Layer.CompressMat(Mats[resolution], Layer.LayerCompressionCodec.GZip);
    }

    public void TestGZipDecompress(BenchmarkResolution resolution)
    { }

    public void TestDeflateCompress(BenchmarkResolution resolution)
    {
        Layer.CompressMat(Mats[resolution], Layer.LayerCompressionCodec.Deflate);
    }

    public void TestDeflateDecompress(BenchmarkResolution resolution)
    { }

    public void TestLZ4Compress(BenchmarkResolution resolution)
    {
        Layer.CompressMat(Mats[resolution], Layer.LayerCompressionCodec.Lz4);
    }

    public void TestLZ4Decompress(BenchmarkResolution resolution)
    { }

    #endregion
}