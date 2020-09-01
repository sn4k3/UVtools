/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.GUI.Controls;

namespace UVtools.GUI.Forms
{
    public partial class FrmBenchmark : Form
    {
        private CancellationTokenSource TokenSource { get; set; }
        private CancellationToken Token => TokenSource.Token;

        private RNGCryptoServiceProvider RandomProvider { get; }

        private const ushort SingleThreadTests = 100;
        private const ushort MultiThreadTests = 1000;

        public const string RunsAbbreviation = "TDPS";
        public const string StressCPUTestName = "Stress CPU (Run until stop)";

        public FrmBenchmark()
        {
            InitializeComponent();
            lbDescription.Text += $"CPU: {BenchmarkTest.DEVCPU}\nRAM: {BenchmarkTest.DEVRAM}";
            RandomProvider = new RNGCryptoServiceProvider();

            cbTest.Items.AddRange(
                new object[]
                {
                    new BenchmarkTest("4K Random CBBDLP Enconde", "Test4KRandomCBBDLPEncode", 40.30f, 246.30f), 
                    new BenchmarkTest("8K Random CBBDLP Enconde", "Test8KRandomCBBDLPEncode", 9.70f, 64.10f),
                    new BenchmarkTest("4K Random CBT Enconde", "Test4KRandomCBTEncode", 13.50f, 113.30f),
                    new BenchmarkTest("8K Random CBT Enconde", "Test8KRandomCBTEncode", 3.40f, 28.20f),
                    new BenchmarkTest("4K Random PW0 Enconde", "Test4KRandomPW0Encode", 14.10f, 89.00f),
                    new BenchmarkTest("8K Random PW0 Enconde", "Test8KRandomPW0Encode", 3.50f, 22.40f),
                    new BenchmarkTest(StressCPUTestName, "Test4KRandomCBTEncode", 0, 0),
                }
            );
            cbTest.SelectedIndex = 0;
        }

        private void FrmBenchmark_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!btnStart.Enabled)
                e.Cancel = true;
        }

        private void EventClicked(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnStart))
            {
                StartBenchmark();
                return;
            }
            if (ReferenceEquals(sender, btnStop))
            {
                TokenSource.Cancel();
                return;
            }
        }

        private void EventSelectedIndexChanged(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, cbTest))
            {
                BenchmarkTest benchmark = cbTest.SelectedItem as BenchmarkTest;
                lbSingleThreadDevResults.Text = $"Single Thread: {benchmark.DevSingleThreadResult} {RunsAbbreviation}";
                lbMultiThreadDevResults.Text = $"Multi Thread: {benchmark.DevMultiThreadResult} {RunsAbbreviation}";
                return;
            }
        }

        private void StartBenchmark()
        {
            if (!btnStart.Enabled) return;
            cbTest.Enabled =
            btnStart.Enabled = false;
            btnStop.Enabled = true;

            TokenSource = new CancellationTokenSource();

            progressBar.Style = ProgressBarStyle.Marquee;

            BenchmarkTest benchmark = cbTest.SelectedItem as BenchmarkTest;
            MethodInfo theMethod = GetType().GetMethod(benchmark.FunctionName);

            if (!benchmark.Name.Equals(StressCPUTestName))
            {
                lbSingleThreadResults.Text = $"Single Thread: Running {SingleThreadTests} tests";
                lbMultiThreadResults.Text = $"Multi Thread: Running {MultiThreadTests} tests";
            }
                
            
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (benchmark.Name.Equals(StressCPUTestName))
                    {
                        while (true)
                        {
                            if (Token.IsCancellationRequested) break;
                            Parallel.For(0, MultiThreadTests, i =>
                            {
                                if (Token.IsCancellationRequested) return;
                                theMethod.Invoke(this, null);
                            });
                        }
                        
                        return;
                    }

                    Stopwatch sw = Stopwatch.StartNew();
                    for (int i = 0; i < SingleThreadTests; i++)
                    {
                        if(Token.IsCancellationRequested) Token.ThrowIfCancellationRequested();
                        theMethod.Invoke(this, null);
                    }

                    sw.Stop();
                    Invoke((MethodInvoker) delegate
                    {
                        // Running on the UI thread
                        UpdateResults(true, sw.ElapsedMilliseconds);
                    });

                    if (Token.IsCancellationRequested) Token.ThrowIfCancellationRequested();

                    sw.Restart();
                    Parallel.For(0, MultiThreadTests, i =>
                        {
                            if (Token.IsCancellationRequested) return;
                            theMethod.Invoke(this, null);
                        });

                    sw.Stop();

                    if (Token.IsCancellationRequested) Token.ThrowIfCancellationRequested();

                    Invoke((MethodInvoker)delegate
                    {
                        // Running on the UI thread
                        UpdateResults(false, sw.ElapsedMilliseconds);
                    });
                }
                catch (OperationCanceledException)
                {

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally
                {
                    Invoke((MethodInvoker)StopBenchmark);
                }
            }, Token);
        }

        private void StopBenchmark()
        {
            btnStart.Enabled = true;
            btnStop.Enabled = false;
            cbTest.Enabled = true;
            progressBar.Style = ProgressBarStyle.Blocks;
        }

        public byte[] EncodeCbddlpImage(Mat image, byte bit = 0)
        {
            List<byte> rawData = new List<byte>();
            var span = image.GetPixelSpan<byte>();

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
            List<byte> rawData = new List<byte>();
            byte color = byte.MaxValue >> 1;
            uint stride = 0;
            var span = image.GetPixelSpan<byte>();

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
            List<byte> rawData = new List<byte>();
            var span = image.GetPixelSpan<byte>();

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

        private void UpdateResults(bool isSingleThread, long milliseconds)
        {
            decimal seconds = Math.Round(milliseconds / 1000m, 2);
            //var text = (isSingleThread ? "Single" : "Multi") + $" Thread: {Math.Round(SingleThreadTests / seconds, 2)} OPS ({seconds}s)";

            if(isSingleThread)
                lbSingleThreadResults.Text = $"Single Thread: {Math.Round(SingleThreadTests / seconds, 2)} {RunsAbbreviation} ({SingleThreadTests} tests / {seconds}s)";
            else
                lbMultiThreadResults.Text = $"Multi Thread: {Math.Round(MultiThreadTests / seconds, 2)} {RunsAbbreviation} ({MultiThreadTests} tests / {seconds}s)"; ;
        }

        #region Tests

        public Mat RandomMat(int width, int height)
        {
            var bytes = new byte[width * height];
            RandomProvider.GetBytes(bytes);
            Mat mat = new Mat(new Size(width, height), DepthType.Cv8U, 1);
            mat.SetBytes(bytes);
            return mat;
        }

        public void Test4KRandomCBBDLPEncode()
        {
            using (var mat = RandomMat(3840, 2160))
                EncodeCbddlpImage(mat);
        }

        public void Test8KRandomCBBDLPEncode()
        {
            using (var mat = RandomMat(7680, 4320))
                EncodeCbddlpImage(mat);
        }

        public void Test4KRandomCBTEncode()
        {
            using (var mat = RandomMat(3840, 2160))
                EncodeCbtImage(mat);
        }

        public void Test8KRandomCBTEncode()
        {
            using (var mat = RandomMat(7680, 4320))
                EncodeCbtImage(mat);
        }

        public void Test4KRandomPW0Encode()
        {
            using (var mat = RandomMat(3840, 2160))
                EncodePW0Image(mat);
        }

        public void Test8KRandomPW0Encode()
        {
            using (var mat = RandomMat(7680, 4320))
                EncodePW0Image(mat);
        }

        #endregion


    }
}
