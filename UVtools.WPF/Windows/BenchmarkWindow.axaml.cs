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
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows
{
    public class BenchmarkWindow : WindowEx
    {
        private int _testSelectedIndex;
        private string _singleThreadTdps = $"0 {RunsAbbreviation}";
        private string _multiThreadTdps = $"0 {RunsAbbreviation}";
        private string _devSingleThreadTdps = $"{Tests[0].DevSingleThreadResult} {RunsAbbreviation} ({SingleThreadTests} tests / {Math.Round(SingleThreadTests / Tests[0].DevSingleThreadResult, 2)}s)";
        private string _devMultiThreadTdps = $"{Tests[0].DevMultiThreadResult} {RunsAbbreviation} ({MultiThreadTests} tests / {Math.Round(MultiThreadTests / Tests[0].DevMultiThreadResult, 2)}s)";
        private bool _isRunning;
        private string _startStopButtonText = "Start";
        private const ushort SingleThreadTests = 100;
        private const ushort MultiThreadTests = 1000;

        public const string RunsAbbreviation = "TDPS";
        public const string StressCPUTestName = "Stress CPU (Run until stop)";

        //private readonly RNGCryptoServiceProvider _randomProvider = new();

        private CancellationTokenSource _tokenSource;
        private CancellationToken _token => _tokenSource.Token;

        public static BenchmarkTest[] Tests =>
            new[]
            {
                new BenchmarkTest("4K Random CBBDLP Enconde", "Test4KRandomCBBDLPEncode", 57.14f, 401.61f),
                new BenchmarkTest("8K Random CBBDLP Enconde", "Test8KRandomCBBDLPEncode", 12.03f, 99.80f),
                new BenchmarkTest("4K Random CBT Enconde", "Test4KRandomCBTEncode", 19.05f, 124.38f),
                new BenchmarkTest("8K Random CBT Enconde", "Test8KRandomCBTEncode", 4.03f, 35.64f),
                new BenchmarkTest("4K Random PW0 Enconde", "Test4KRandomPW0Encode", 18.85f, 103.00f),
                new BenchmarkTest("8K Random PW0 Enconde", "Test8KRandomPW0Encode", 4.07f, 26.65f),
                new BenchmarkTest(StressCPUTestName, "Test4KRandomCBTEncode", 0, 0),
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
                                if (_token.IsCancellationRequested) break;
                                Parallel.For(0, MultiThreadTests, i =>
                                {
                                    if (_token.IsCancellationRequested) return;
                                    theMethod.Invoke(this, null);
                                });
                            }

                            return;
                        }

                        
                        for (int i = 0; i < SingleThreadTests; i++)
                        {
                            if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();
                            theMethod.Invoke(this, null);
                        }

                        sw.Stop();
                        var singleMiliseconds = sw.ElapsedMilliseconds;
                        Dispatcher.UIThread.InvokeAsync(() => UpdateResults(true, singleMiliseconds));

                        if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();

                        sw.Restart();
                        Parallel.For(0, MultiThreadTests, i =>
                        {
                            if (_token.IsCancellationRequested) return;
                            theMethod.Invoke(this, null);
                        });

                        sw.Stop();

                        if (_token.IsCancellationRequested) _token.ThrowIfCancellationRequested();
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

        public Mat RandomMat(int width, int height)
        {
            Mat mat = new(new Size(width, height), DepthType.Cv8U, 1);
            CvInvoke.Randu(mat, EmguExtensions.BlackColor, EmguExtensions.WhiteColor);
            return mat;
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

        #endregion
    }
}
