/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.Core.PixelEditor;

namespace UVtools.Core
{
    public class LayerManager : IEnumerable<Layer>
    {
        #region Enums
        public enum Mutate : byte
        {
            Move,
            Resize,
            Flip,
            Rotate,
            Solidify,
            PixelDimming,
            //LayerSmash,
            Erode,
            Dilate,
            Opening,
            Closing,
            Gradient,
            TopHat,
            BlackHat,
            HitMiss,
            PyrDownUp,
            SmoothMedian,
            SmoothGaussian,
        }
        #endregion

        #region Properties
        public FileFormats.FileFormat SlicerFile { get; private set; }

        /// <summary>
        /// Layers List
        /// </summary>
        public Layer[] Layers { get; private set; }

        private Rectangle _boundingRectangle = Rectangle.Empty;
        public Rectangle BoundingRectangle
        {
            get => GetBoundingRectangle();
            set => _boundingRectangle = value;
        }

        /// <summary>
        /// Gets the layers count
        /// </summary>
        public uint Count => (uint) Layers.Length;

        /// <summary>
        /// Gets if any layer got modified, otherwise false
        /// </summary>
        public bool IsModified
        {
            get
            {
                for (uint i = 0; i < Count; i++)
                {
                    if (Layers[i].IsModified) return true;
                }
                return false;
            }
        }

        public float LayerHeight => Layers[0].PositionZ;


        #endregion

        #region Constructors
        public LayerManager(uint layerCount, FileFormat slicerFile)
        {
            SlicerFile = slicerFile;
            Layers = new Layer[layerCount];
        }
        #endregion

        #region Indexers
        public Layer this[uint index]
        {
            get => Layers[index];
            set => AddLayer(index, value);
        }

        public Layer this[int index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        public Layer this[long index]
        {
            get => Layers[index];
            set => AddLayer((uint) index, value);
        }

        #endregion

        #region Numerators
        public IEnumerator<Layer> GetEnumerator()
        {
            return ((IEnumerable<Layer>)Layers).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Compress a layer from a <see cref="Stream"/>
        /// </summary>
        /// <param name="input"><see cref="Stream"/> to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(Stream input)
        {
            return CompressLayer(input.ToArray());
        }

        /// <summary>
        /// Compress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream output = new MemoryStream())
            {
                using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                {
                    dstream.Write(input, 0, input.Length);
                }
                return output.ToArray();
            }*/
        }

        /// <summary>
        /// Decompress a layer from a byte array
        /// </summary>
        /// <param name="input">byte array to decompress</param>
        /// <returns>Decompressed byte array</returns>
        public static byte[] DecompressLayer(byte[] input)
        {
            return input;
            /*using (MemoryStream ms = new MemoryStream(input))
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(ms, CompressionMode.Decompress))
                    {
                        dstream.CopyTo(output);
                    }
                    return output.ToArray();
                }
            }*/
        }
        #endregion

        #region Methods

        public Rectangle GetBoundingRectangle(OperationProgress progress = null)
        {
            if (!_boundingRectangle.IsEmpty) return _boundingRectangle;
            _boundingRectangle = this[0].BoundingRectangle;
            if (_boundingRectangle.IsEmpty) // Safe checking
            {
                progress?.Reset(OperationProgress.StatusOptimizingBounds, Count);
                Parallel.For(0, Count, layerIndex =>
                {
                    if (!ReferenceEquals(progress, null) && progress.Token.IsCancellationRequested)
                    {
                        return;
                    }
                    
                    this[layerIndex].GetBoundingRectangle();

                    if (ReferenceEquals(progress, null)) return;
                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });
                _boundingRectangle = this[0].BoundingRectangle;

                if (!ReferenceEquals(progress, null) && progress.Token.IsCancellationRequested)
                {
                    _boundingRectangle = Rectangle.Empty;
                    progress.Token.ThrowIfCancellationRequested();
                }
                
            }

            progress?.Reset(OperationProgress.StatusCalculatingBounds, Count);
            for (int i = 1; i < Count; i++)
            {
                _boundingRectangle = Rectangle.Union(_boundingRectangle, this[i].BoundingRectangle);
                if (ReferenceEquals(progress, null)) continue;
                progress++;
            }

            return _boundingRectangle;
        }

        /// <summary>
        /// Add a layer
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <param name="layer">Layer to add</param>
        public void AddLayer(uint index, Layer layer)
        {
            //layer.Index = index;
            Layers[index] = layer;
            layer.ParentLayerManager = this;
        }

        /// <summary>
        /// Get layer given index
        /// </summary>
        /// <param name="index">Layer index</param>
        /// <returns></returns>
        public Layer GetLayer(uint index)
        {
            return Layers[index];
        }

        public void MutateMove(uint startLayerIndex, uint endLayerIndex, OperationMove move, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Moving", endLayerIndex - startLayerIndex + 1);

            if (move.SrcRoi == Rectangle.Empty) move.SrcRoi = GetBoundingRectangle(progress);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                this[layerIndex].MutateMove(move);

                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            _boundingRectangle = Rectangle.Empty;

            progress.Token.ThrowIfCancellationRequested();


        }

        /// <summary>
        /// Resizes layer images in x and y factor, starting at 1 = 100%
        /// </summary>
        /// <param name="startLayerIndex">Layer index to start</param>
        /// <param name="endLayerIndex">Layer index to end</param>
        /// <param name="x">X factor, starts at 1</param>
        /// <param name="y">Y factor, starts at 1</param>
        /// <param name="isFade">Fade X/Y towards 100%</param>
        public void MutateResize(uint startLayerIndex, uint endLayerIndex, double x, double y, bool isFade, OperationProgress progress = null)
        {
            if (x == 1.0 && y == 1.0) return;

            if(ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Resizing", endLayerIndex - startLayerIndex + 1);

            double xSteps = Math.Abs(x - 1.0) / (endLayerIndex - startLayerIndex);
            double ySteps = Math.Abs(y - 1.0) / (endLayerIndex - startLayerIndex);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                var newX = x;
                var newY = y;
                if (isFade)
                {
                    if (newX != 1.0)
                    {
                        
                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newX = (float)(newX < 1.0
                            ? newX + (layerIndex - startLayerIndex) * xSteps
                            : newX - (layerIndex - startLayerIndex) * xSteps);

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }

                    if (y != 1.0)
                    {
                        
                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newY = (float)(newY < 1.0
                            ? newY + (layerIndex - startLayerIndex) * ySteps
                            : newY - (layerIndex - startLayerIndex) * ySteps);

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }
                }

                lock (progress.Mutex)
                {
                    progress++;
                }

                if (newX == 1.0 && newY == 1.0) return;

                this[layerIndex].MutateResize(newX, newY);
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateFlip(uint startLayerIndex, uint endLayerIndex, FlipType flipType, bool makeCopy = false, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Fliping", endLayerIndex - startLayerIndex + 1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateFlip(flipType, makeCopy);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateRotate(uint startLayerIndex, uint endLayerIndex, double angle, Inter interpolation = Inter.Linear, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Rotating", endLayerIndex - startLayerIndex+1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateRotate(angle, interpolation);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateSolidify(uint startLayerIndex, uint endLayerIndex, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Solidifing", endLayerIndex - startLayerIndex+1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateSolidify();
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutatePixelDimming(uint startLayerIndex, uint endLayerIndex, Matrix<byte> evenPattern = null, Matrix<byte> oddPattern = null, ushort borderSize = 5, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Dimming pixels", endLayerIndex - startLayerIndex + 1);

            if (ReferenceEquals(evenPattern, null))
            {
                evenPattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127, [0, 1] = 255,
                    [1, 0] = 255, [1, 1] = 127,
                };

                if (ReferenceEquals(oddPattern, null))
                {
                    oddPattern = new Matrix<byte>(2, 2)
                    {
                        [0, 0] = 255, [0, 1] = 127,
                        [1, 0] = 127, [1, 1] = 255,
                    };
                }
            }

            if (ReferenceEquals(oddPattern, null))
            {
                oddPattern = evenPattern;
            }

            using (Mat mat = this[0].LayerMat)
            {
                using (var matEven = mat.CloneBlank())
                {
                    using (Mat matOdd = mat.CloneBlank())
                    {
                        CvInvoke.Repeat(evenPattern, mat.Rows / evenPattern.Rows + 1, mat.Cols / evenPattern.Cols + 1, matEven);
                        CvInvoke.Repeat(oddPattern, mat.Rows / oddPattern.Rows + 1, mat.Cols / oddPattern.Cols + 1, matOdd);

                        using (var evenPatternMask = new Mat(matEven, new Rectangle(0, 0, mat.Width, mat.Height)))
                        {
                            using (var oddPatternMask = new Mat(matOdd, new Rectangle(0, 0, mat.Width, mat.Height)))
                            {
                                Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
                                {
                                    if (progress.Token.IsCancellationRequested) return;
                                    this[layerIndex].MutatePixelDimming(evenPatternMask, oddPatternMask, borderSize);
                                    lock (progress.Mutex)
                                    {
                                        progress++;
                                    }
                                });
                            }
                        }
                    }
                }
            }

            progress.Token.ThrowIfCancellationRequested();
        }

        private void MutateGetVarsIterationFade(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out int iterationSteps, out int maxIteration)
        {
            iterationSteps = 0;
            maxIteration = 0;
            isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
            if (!isFade) return;
            iterationSteps = (int)Math.Abs((double)(iterationsStart - iterationsEnd) / (endLayerIndex - startLayerIndex));
            maxIteration = Math.Max(iterationsStart, iterationsEnd);
        }

        private int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, int iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
        {
            if(!isFade) return iterationsStart;
            // calculate iterations based on range
            int iterations = (int)(iterationsStart < iterationsEnd
                ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
                : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

            // constrain
            return Math.Min(Math.Max(1, iterations), maxIteration);
        }

        public void MutateErode(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex, 
                endLayerIndex, 
                iterationsStart,
                iterationsEnd, 
                ref isFade,
                out var iterationSteps, 
                out var maxIteration
                );

            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Eroding", endLayerIndex - startLayerIndex+1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint) layerIndex);
                this[layerIndex].MutateErode(iterations, kernel, anchor, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateDilate(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Dilating", endLayerIndex - startLayerIndex+1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateDilate(iterations, kernel, anchor, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateOpen(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Removing Noise", endLayerIndex - startLayerIndex+1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateOpen(iterations, kernel, anchor, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateClose(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Gap Closing", endLayerIndex - startLayerIndex+1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateClose(iterations, kernel, anchor, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateGradient(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
            IInputArray kernel = null, Point anchor = default,
            BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            MutateGetVarsIterationFade(
                startLayerIndex,
                endLayerIndex,
                iterationsStart,
                iterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Gradient", endLayerIndex - startLayerIndex+1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, iterationsStart, iterationsEnd, iterationSteps, maxIteration, startLayerIndex, (uint)layerIndex);
                this[layerIndex].MutateGradient(iterations, kernel, anchor, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutatePyrDownUp(uint startLayerIndex, uint endLayerIndex, BorderType borderType = BorderType.Default, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("PryDownUp", endLayerIndex - startLayerIndex+1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutatePyrDownUp(borderType);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateMedianBlur(uint startLayerIndex, uint endLayerIndex, int aperture = 1, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Bluring", endLayerIndex - startLayerIndex+1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateMedianBlur(aperture);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateGaussianBlur(uint startLayerIndex, uint endLayerIndex, Size size = default, int sigmaX = 0, int sigmaY = 0, BorderType borderType = BorderType.Reflect101, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Bluring", endLayerIndex - startLayerIndex+1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateGaussianBlur(size, sigmaX, sigmaY, borderType);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public ConcurrentDictionary<uint, List<LayerIssue>> GetAllIssues(
            IslandDetectionConfiguration islandConfig = null, ResinTrapDetectionConfiguration resinTrapConfig = null,
            OperationProgress progress = null)
        {
            if(ReferenceEquals(islandConfig, null)) islandConfig = new IslandDetectionConfiguration();
            if(ReferenceEquals(resinTrapConfig, null)) resinTrapConfig = new ResinTrapDetectionConfiguration();
            if(ReferenceEquals(progress, null)) progress = new OperationProgress();

            const byte minTouchingBondsPixelColor = 200;

            var result = new ConcurrentDictionary<uint, List<LayerIssue>>();
            var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();

            bool islandsFinished = false;

            progress.Reset(OperationProgress.StatusIslands, Count);

            Parallel.Invoke(() =>
            {
                if (!islandConfig.Enabled)
                {
                    islandsFinished = true;
                    return;
                }

                // Detect contours
                Parallel.ForEach(this,
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layer =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        if (layer.NonZeroPixelCount > 0)
                        {
                            using (var image = layer.LayerMat)
                            {
                                int step = image.Step;
                                var span = image.GetPixelSpan<byte>();

                                // TouchingBounds Checker
                                List<Point> pixels = new List<Point>();
                                for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                                {
                                    if (span[x] >= minTouchingBondsPixelColor) // Top
                                    {
                                        pixels.Add(new Point(x, 0));
                                    }

                                    if (span[step * image.Height - step + x] >= minTouchingBondsPixelColor) // Bottom
                                    {
                                        pixels.Add(new Point(x, image.Height - 1));
                                    }
                                }

                                for (int y = 0; y < image.Height; y++) // Check Left and Right bounds
                                {
                                    if (span[y * step] >= minTouchingBondsPixelColor) // Left
                                    {
                                        pixels.Add(new Point(0, y));
                                    }

                                    if (span[y * step + step - 1] >= minTouchingBondsPixelColor) // Right
                                    {
                                        pixels.Add(new Point(step - 1, y));
                                    }
                                }

                                if (pixels.Count > 0)
                                {
                                    result.TryAdd(layer.Index, new List<LayerIssue>
                                    {
                                        new LayerIssue(layer, LayerIssue.IssueType.TouchingBound, pixels.ToArray())
                                    });
                                }

                                if (layer.Index == 0) return; // No islands for layer 0

                                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                                Mat hierarchy = new Mat();

                                if (islandConfig.BinaryThreshold > 0)
                                {
                                    using (var thresholdImage = new Mat())
                                    {
                                        CvInvoke.Threshold(image, thresholdImage, 1, 255, ThresholdType.Binary);
                                        CvInvoke.FindContours(thresholdImage, contours, hierarchy, RetrType.Ccomp,
                                            ChainApproxMethod.ChainApproxSimple);
                                    }
                                }
                                else
                                {
                                    CvInvoke.FindContours(image, contours, hierarchy, RetrType.Ccomp,
                                        ChainApproxMethod.ChainApproxSimple);
                                }

                                var arr = hierarchy.GetData();
                                //
                                //hierarchy[i][0]: the index of the next contour of the same level
                                //hierarchy[i][1]: the index of the previous contour of the same level
                                //hierarchy[i][2]: the index of the first child
                                //hierarchy[i][3]: the index of the parent
                                //

                                Mat previousImage = null;
                                Span<byte> previousSpan = null;
                                for (int i = 0; i < contours.Size; i++)
                                {

                                    if ((int) arr.GetValue(0, i, 2) == -1 && (int) arr.GetValue(0, i, 3) != -1)
                                        continue;
                                    var rect = CvInvoke.BoundingRectangle(contours[i]);
                                    if (rect.GetArea() < islandConfig.RequiredAreaToProcessCheck)
                                        continue;

                                    if (ReferenceEquals(previousImage, null))
                                    {
                                        previousImage = this[layer.Index - 1].LayerMat;
                                        previousSpan = previousImage.GetPixelSpan<byte>();
                                    }

                                    List<Point> points = new List<Point>();
                                    uint pixelsSupportingIsland = 0;

                                    using (Mat contourImage = image.CloneBlank())
                                    {
                                        CvInvoke.DrawContours(contourImage, contours, i, new MCvScalar(255), -1);
                                        var contourImageSpan = contourImage.GetPixelSpan<byte>();

                                        for (int y = rect.Y; y < rect.Bottom; y++)
                                        {
                                            for (int x = rect.X; x < rect.Right; x++)
                                            {
                                                int pixel = step * y + x;
                                                if (span[pixel] < islandConfig.RequiredPixelBrightnessToProcessCheck)
                                                    continue; // Low brightness, ignore
                                                if (contourImageSpan[pixel] != 255)
                                                    continue; // Not inside contour, ignore

                                                //if (CvInvoke.PointPolygonTest(contours[i], new PointF(x, y), false) < 0) continue; // Out of contour SLOW!
                                                //Debug.WriteLine($"Layer: {layer.Index}, Coutour: {i},  X:{x} Y:{y}");
                                                points.Add(new Point(x, y));

                                                if (previousSpan[pixel] >=
                                                    islandConfig.RequiredPixelBrightnessToSupport)
                                                {
                                                    pixelsSupportingIsland++;
                                                }
                                            }
                                        }
                                    }


                                    if (points.Count == 0) continue;
                                    if (pixelsSupportingIsland >= islandConfig.RequiredPixelsToSupport)
                                        continue; // Not a island, bounding is strong, i think...
                                    if (pixelsSupportingIsland > 0 &&
                                        points.Count < islandConfig.RequiredPixelsToSupport &&
                                        pixelsSupportingIsland >= Math.Max(1, points.Count / 2))
                                        continue; // Not a island, but maybe weak bounding...


                                    var issue = new LayerIssue(layer, LayerIssue.IssueType.Island, points.ToArray(),
                                        rect);
                                    result.AddOrUpdate(layer.Index, new List<LayerIssue> {issue},
                                        (layerIndex, list) =>
                                        {
                                            list.Add(issue);
                                            return list;
                                        });
                                }

                                contours.Dispose();
                                hierarchy.Dispose();
                                previousImage?.Dispose();
                            }
                        }
                        else
                        {
                            result.TryAdd(layer.Index, new List<LayerIssue>
                            {
                                new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer)
                            });
                        }

                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    });
                islandsFinished = true;
            }, () =>
            {
                if (!resinTrapConfig.Enabled) return;
                // Detect contours
                Parallel.ForEach(this,
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layer =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        using (var image = layer.LayerMat)
                        {
                            if (resinTrapConfig.BinaryThreshold > 0)
                            {
                                CvInvoke.Threshold(image, image, resinTrapConfig.BinaryThreshold, 255, ThresholdType.Binary);
                            }

                            var listHollowArea = new List<LayerHollowArea>();

                            using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                            {
                                using (Mat hierarchy = new Mat())
                                {

                                    CvInvoke.FindContours(image, contours, hierarchy, RetrType.Ccomp,
                                        ChainApproxMethod.ChainApproxSimple);

                                    var arr = hierarchy.GetData();
                                    //
                                    //hierarchy[i][0]: the index of the next contour of the same level
                                    //hierarchy[i][1]: the index of the previous contour of the same level
                                    //hierarchy[i][2]: the index of the first child
                                    //hierarchy[i][3]: the index of the parent
                                    //

                                    for (int i = 0; i < contours.Size; i++)
                                    {
                                        if ((int) arr.GetValue(0, i, 2) != -1 || (int) arr.GetValue(0, i, 3) == -1)
                                            continue;
                                        var rect = CvInvoke.BoundingRectangle(contours[i]);
                                        if(rect.GetArea() < resinTrapConfig.RequiredAreaToProcessCheck) continue;

                                        listHollowArea.Add(new LayerHollowArea(contours[i].ToArray(),
                                            rect,
                                            layer.Index == Count - 1
                                                ? LayerHollowArea.AreaType.Drain
                                                : LayerHollowArea.AreaType.Unknown));

                                        if (listHollowArea.Count > 0)
                                            layerHollowAreas.TryAdd(layer.Index, listHollowArea);
                                    }
                                }
                            }
                        }
                    });


                for (uint layerIndex = 0; layerIndex < Count - 1; layerIndex++) // Last layers, always drains
                {
                    if (progress.Token.IsCancellationRequested) break;
                    if (!layerHollowAreas.TryGetValue(layerIndex, out var areas))
                        continue; // No hollow areas in this layer, ignore

                    byte areaCount = 0;
                    //foreach (var area in areas)
                    
                    Parallel.ForEach(from t in areas where t.Type == LayerHollowArea.AreaType.Unknown select t, area =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        if (area.Type != LayerHollowArea.AreaType.Unknown) return; // processed, ignore
                        area.Type = LayerHollowArea.AreaType.Trap;

                        areaCount++;

                        List<LayerHollowArea> linkedAreas = new List<LayerHollowArea>();

                        for (sbyte dir = 1; dir >= -1 && area.Type != LayerHollowArea.AreaType.Drain; dir -= 2) 
                        //Parallel.ForEach(new sbyte[] {1, -1}, new ParallelOptions {MaxDegreeOfParallelism = 2}, dir =>
                        {
                            Queue<LayerHollowArea> queue = new Queue<LayerHollowArea>();
                            queue.Enqueue(area);
                            area.Processed = false;
                            int nextLayerIndex = (int) layerIndex;
                            while (queue.Count > 0 && area.Type != LayerHollowArea.AreaType.Drain)
                            {
                                if (progress.Token.IsCancellationRequested) return;

                                LayerHollowArea checkArea = queue.Dequeue();
                                if (checkArea.Processed) continue;
                                checkArea.Processed = true;
                                nextLayerIndex += dir;
                                
                                if (nextLayerIndex < 0 || nextLayerIndex >= Count)
                                    break; // Exhausted layers
                                bool haveNextAreas = layerHollowAreas.TryGetValue((uint) nextLayerIndex, out var nextAreas);
                                Dictionary<int, LayerHollowArea> intersectingAreas = new Dictionary<int, LayerHollowArea>();

                                if (islandsFinished)
                                {
                                    progress.Reset(OperationProgress.StatusResinTraps, Count, (uint) nextLayerIndex);
                                }

                                using (var image = this[nextLayerIndex].LayerMat)
                                {
                                    var span = image.GetPixelSpan<byte>();
                                    using (var emguImage = image.CloneBlank())
                                    {
                                        using(var vec = new VectorOfVectorOfPoint(new VectorOfPoint(checkArea.Contour)))
                                        {
                                            CvInvoke.DrawContours(emguImage, vec, -1, new MCvScalar(255), -1);
                                        }

                                        using (var intersectingAreasMat = image.CloneBlank())
                                        {
                                            if (haveNextAreas)
                                            {
                                                foreach (var nextArea in nextAreas)
                                                {
                                                    if (!checkArea.BoundingRectangle.IntersectsWith(
                                                        nextArea.BoundingRectangle)) continue;
                                                    intersectingAreas.Add(intersectingAreas.Count + 1, nextArea);
                                                    using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(nextArea.Contour)))
                                                    {
                                                        CvInvoke.DrawContours(intersectingAreasMat, vec, -1,
                                                            new MCvScalar(intersectingAreas.Count), -1);
                                                    }
                                                }
                                            }

                                            //Debug.WriteLine($"Area Count: {areaCount} | Next Areas: {intersectingAreas.Count} | Layer: {layerIndex} | Next Layer: {nextLayerIndex} | Dir: {dir}");

                                            bool exitPixelLoop = false;
                                            uint blackCount = 0;

                                            var spanContour = emguImage.GetPixelSpan<byte>();
                                            var spanIntersect = intersectingAreasMat.GetPixelSpan<byte>();
                                            for (int y = checkArea.BoundingRectangle.Y;
                                                y < checkArea.BoundingRectangle.Bottom &&
                                                area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                y++)
                                            {
                                                int pixelPos = image.GetPixelPos(checkArea.BoundingRectangle.X, y) - 1;
                                                for (int x = checkArea.BoundingRectangle.X;
                                                    x < checkArea.BoundingRectangle.Right &&
                                                    area.Type != LayerHollowArea.AreaType.Drain && !exitPixelLoop;
                                                    x++)
                                                {
                                                    pixelPos++;

                                                    if (spanContour[pixelPos] != 255) continue; // No contour
                                                    if (span[pixelPos] > resinTrapConfig.MaximumPixelBrightnessToDrain) continue; // Threshold to ignore white area
                                                    blackCount++;

                                                    if (intersectingAreas.Count > 0) // Have areas, can be on same area path or not
                                                    {
                                                        byte i = spanIntersect[pixelPos];
                                                        if (i == 0 || !intersectingAreas.ContainsKey(i)) // Black pixels
                                                            continue;

                                                        //Debug.WriteLine($"BlackCount: {blackCount}, pixel color: {i}, layerindex: {layerIndex}");

                                                        if (intersectingAreas[i].Type == LayerHollowArea.AreaType.Drain) // Found a drain, stop query
                                                        {
                                                            area.Type = LayerHollowArea.AreaType.Drain;
                                                            exitPixelLoop = true;
                                                        }
                                                        else
                                                        {
                                                            queue.Enqueue(intersectingAreas[i]);
                                                        }

                                                        linkedAreas.Add(intersectingAreas[i]);
                                                        intersectingAreas.Remove(i);
                                                        if (intersectingAreas.Count == 0) // Intersection areas sweep end, quit this path
                                                        {
                                                            exitPixelLoop = true;
                                                            break;
                                                        }

                                                        //break;

                                                        // Old Way

                                                        /*foreach (var nextAreaCheck in intersectingAreas)
                                                        {
                                                            using (var vec = new VectorOfPoint(nextAreaCheck.Value.Contour))
                                                            {
                                                                //Debug.WriteLine(CvInvoke.PointPolygonTest(vec, new PointF(x, y), false));
                                                                if (CvInvoke.PointPolygonTest(vec, new PointF(x, y), false) < 0) continue;
                                                            }
    
                                                            if (nextAreaCheck.Value.Type == LayerHollowArea.AreaType.Drain) // Found a drain, stop query
                                                            {
                                                                area.Type = LayerHollowArea.AreaType.Drain;
                                                                exitPixelLoop = true;
                                                            }
                                                            else
                                                            {
                                                                queue.Enqueue(nextAreaCheck.Value);
                                                            }
    
                                                            linkedAreas.Add(nextAreaCheck.Value);
                                                            intersectingAreas.Remove(nextAreaCheck.Key);
                                                            if (intersectingAreas.Count == 0)
                                                            {
                                                                haveNextAreas = false;
                                                                exitPixelLoop = true;
                                                            }
                                                            //exitPixelLoop = true;
                                                            break;
    
                                                        }*/
                                                    }
                                                    else if (blackCount > Math.Min(checkArea.Contour.Length / 2, resinTrapConfig.RequiredBlackPixelsToDrain)) // Black pixel without next areas = Drain
                                                    {
                                                        area.Type = LayerHollowArea.AreaType.Drain;
                                                        exitPixelLoop = true;
                                                        break;
                                                    }
                                                } // X loop
                                            } // Y loop

                                            if (queue.Count == 0 && blackCount > Math.Min(checkArea.Contour.Length / 2, resinTrapConfig.RequiredBlackPixelsToDrain))
                                            {
                                                area.Type = LayerHollowArea.AreaType.Drain;
                                            }

                                        } // Dispose intersecting image
                                    } // Dispose emgu image
                                } // Dispose image
                            } // Areas loop
                        } // Dir layer loop

                        foreach (var linkedArea in linkedAreas) // Update linked areas
                        {
                            linkedArea.Type = area.Type;
                        }
                    });
                }
            });

            if (progress.Token.IsCancellationRequested) return result;

            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                if (!layerHollowAreas.TryGetValue(layerIndex, out var list)) continue;
                if (list.Count == 0) continue;
                foreach (var issue in 
                        from area 
                        in list 
                        where area.Type == LayerHollowArea.AreaType.Trap 
                        select new LayerIssue(this[layerIndex], LayerIssue.IssueType.ResinTrap, area.Contour, area.BoundingRectangle))
                {
                    result.AddOrUpdate(layerIndex, new List<LayerIssue> {issue}, (u, listIssues) =>
                    {
                        listIssues.Add(issue);
                        return listIssues;
                    });
                }
            }

            return result;
        }

        public void RepairLayers(uint layerStart, uint layerEnd, uint closingIterations = 1, uint openingIterations = 1, byte removeIslandsBelowEqualPixels = 4,
            bool repairIslands = true, bool removeEmptyLayers = true, bool repairResinTraps = true, Dictionary<uint, List<LayerIssue>> issues = null,
            OperationProgress progress = null)
        {
            if(ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset(OperationProgress.StatusRepairLayers, layerEnd - layerStart + 1);

            if (repairIslands || repairResinTraps)
            {
                Parallel.For(layerStart, layerEnd + 1, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    Layer layer = this[layerIndex];
                    using (var image = layer.LayerMat)
                    {
                        if (!ReferenceEquals(issues, null))
                        {
                            if (repairIslands && removeIslandsBelowEqualPixels > 0)
                            {
                                if (issues.TryGetValue((uint)layerIndex, out var issueList))
                                {
                                    var bytes = image.GetPixelSpan<byte>();
                                    foreach (var issue in issueList.Where(issue =>
                                        issue.Type == LayerIssue.IssueType.Island && issue.Pixels.Length <= removeIslandsBelowEqualPixels))
                                    {
                                        foreach (var issuePixel in issue.Pixels)
                                        {
                                            bytes[image.GetPixelPos(issuePixel)] = 0;
                                        }
                                    }
                                }
                            }

                            if (repairResinTraps)
                            {
                                if (issues.TryGetValue((uint) layerIndex, out var issueList))
                                {
                                    foreach (var issue in issueList.Where(issue =>
                                        issue.Type == LayerIssue.IssueType.ResinTrap))
                                    {
                                        using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                                        {
                                            CvInvoke.DrawContours(image,
                                                vec,
                                                -1,
                                                new MCvScalar(255),
                                                -1);
                                        }

                                        /*CvInvoke.DrawContours(image,
                                            new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)),
                                            -1,
                                            new MCvScalar(255),
                                            2);*/
                                    }
                                }
                            }
                        }

                        if (repairIslands)
                        {
                            using (Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3),
                                new Point(-1, -1)))
                            {
                                if (closingIterations > 0)
                                {
                                    CvInvoke.MorphologyEx(image, image, MorphOp.Close, kernel, new Point(-1, -1),
                                        (int) closingIterations, BorderType.Default, new MCvScalar());
                                }

                                if (openingIterations > 0)
                                {
                                    CvInvoke.MorphologyEx(image, image, MorphOp.Open, kernel, new Point(-1, -1),
                                        (int) closingIterations, BorderType.Default, new MCvScalar());
                                }
                            }
                        }

                        layer.LayerMat = image;
                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    }
                });
            }

            if (removeEmptyLayers)
            {
                List<uint> removeLayers = new List<uint>();
                for (uint layerIndex = layerStart; layerIndex <= layerEnd; layerIndex++)
                {
                    if (this[layerIndex].NonZeroPixelCount == 0)
                    {
                        removeLayers.Add(layerIndex);
                    }
                }

                if (removeLayers.Count > 0)
                {
                    RemoveLayer(removeLayers);
                }
            }

            progress.Token.ThrowIfCancellationRequested();
        }

        public void ToolPattern(uint startLayerIndex, uint endLayerIndex, OperationPattern settings, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Pattern", endLayerIndex - startLayerIndex + 1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                this[layerIndex].ToolPattern(settings);

                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            _boundingRectangle = Rectangle.Empty;

            progress.Token.ThrowIfCancellationRequested();

            if (settings.Anchor == Anchor.None) return;
            MutateMove(startLayerIndex, endLayerIndex, new OperationMove(BoundingRectangle, 0, 0, settings.Anchor), progress);

        }

        public void DrawModifications(PixelHistory pixelHistory, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Drawings", (uint) pixelHistory.Count);

            ConcurrentDictionary<uint, Mat> modfiedLayers = new ConcurrentDictionary<uint, Mat>();
            for (var layerIndex = 0; layerIndex < pixelHistory.Count; layerIndex++)
            {
                var operation = pixelHistory[layerIndex];
                if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                {
                    var operationDrawing = (PixelDrawing) operation;
                    var mat = modfiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (operationDrawing.BrushSize == 1)
                    {
                        mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Color);
                        continue;
                    }

                    switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Rectangle:
                            CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Color), -1);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                                new MCvScalar(operationDrawing.Color), -1);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                {
                    var operationSupport = (PixelSupport)operation;
                    int drawnLayers = 0;
                    for (int operationLayer = (int)operation.LayerIndex-1; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modfiedLayers.GetOrAdd((uint) operationLayer, u => this[operationLayer].LayerMat);
                        int radius = (operationLayer > 10 ? Math.Min(operationSupport.TipDiameter + drawnLayers, operationSupport.PillarDiameter) : operationSupport.BaseDiameter) / 2;
                        uint whitePixels;

                        int yStart = Math.Max(0, operation.Location.Y - operationSupport.TipDiameter / 2);
                        int xStart = Math.Max(0, operation.Location.X - operationSupport.TipDiameter / 2);

                        using (Mat matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationSupport.TipDiameter, operationSupport.TipDiameter)))
                        {
                            using (Mat matCircleMask = matCircleRoi.CloneBlank())
                            {
                                CvInvoke.Circle(matCircleMask, new Point(operationSupport.TipDiameter / 2, operationSupport.TipDiameter / 2),
                                    operationSupport.TipDiameter / 2, new MCvScalar(255), -1);
                                CvInvoke.BitwiseAnd(matCircleRoi, matCircleMask, matCircleMask);
                                whitePixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                            }
                        }

                        if (whitePixels >= Math.Pow(operationSupport.TipDiameter, 2) / 3)
                        {
                            //CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                            if (drawnLayers == 0) continue; // Supports nonexistent, keep digging
                            break; // White area end supporting
                        }

                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1);
                        drawnLayers++;
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                {
                    uint drawnLayers = 0;
                    var operationDrainHole = (PixelDrainHole)operation;
                    for (int operationLayer = (int)operation.LayerIndex; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modfiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
                        int radius =  operationDrainHole.Diameter / 2;
                        uint blackPixels;

                        int yStart = Math.Max(0, operation.Location.Y - radius);
                        int xStart = Math.Max(0, operation.Location.X - radius);

                        using (Mat matCircleRoi = new Mat(mat, new Rectangle(xStart, yStart, operationDrainHole.Diameter, operationDrainHole.Diameter)))
                        {
                            using (Mat matCircleRoiInv = new Mat())
                            {
                                CvInvoke.Threshold(matCircleRoi, matCircleRoiInv, 100, 255, ThresholdType.BinaryInv);
                                using (Mat matCircleMask = matCircleRoi.CloneBlank())
                                {
                                    CvInvoke.Circle(matCircleMask, new Point(radius, radius), radius, new MCvScalar(255), -1);
                                    CvInvoke.BitwiseAnd(matCircleRoiInv, matCircleMask, matCircleMask);
                                    blackPixels = (uint) CvInvoke.CountNonZero(matCircleMask);
                                }
                            }
                        }

                        if (blackPixels >= Math.Pow(operationDrainHole.Diameter, 2) / 3) // Enough area to drain?
                        {
                            if (drawnLayers == 0) continue; // Drill not found a target yet, keep digging
                            break; // Stop drill drain found!
                        }
                        
                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(0), -1);
                        drawnLayers++;
                    }
                }

                progress++;
            }

            progress.Reset("Saving", (uint) modfiedLayers.Count);
            foreach (var modfiedLayer in modfiedLayers)
            {
                this[modfiedLayer.Key].LayerMat = modfiedLayer.Value;
                modfiedLayer.Value.Dispose();
                progress++;
            }

            pixelHistory.Clear();
        }

        /// <summary>
        /// Desmodify all layers
        /// </summary>
        public void Desmodify()
        {
            for (uint i = 0; i < Count; i++)
            {
                Layers[i].IsModified = false;
            }
        }

        /// <summary>
        /// Clone this object
        /// </summary>
        /// <returns></returns>
        public LayerManager Clone()
        {
            LayerManager layerManager = new LayerManager(Count, SlicerFile);
            foreach (var layer in this)
            {
                layerManager[layer.Index] = layer.Clone();
            }

            return layerManager;
        }


        #endregion

        public void RemoveLayer(uint layerIndex) => RemoveLayer(layerIndex, layerIndex);

        public void RemoveLayer(uint layerIndexStart, uint layerIndexEnd)
        {
            var layersRemove = new List<uint>();
            for (uint layerIndex = layerIndexStart; layerIndex <= layerIndexEnd; layerIndex++)
            {
                layersRemove.Add(layerIndex);
            }

            RemoveLayer(layersRemove);
        }

        public void RemoveLayer(List<uint> layersRemove)
        {
            if (layersRemove.Count == 0) return;

            var oldLayers = Layers;
            float layerHeight = SlicerFile.LayerHeight;

            Layers = new Layer[Count - layersRemove.Count];

            // Re-set
            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                if (layersRemove.Contains(layerIndex)) continue;
                Layers[newLayerIndex] = oldLayers[layerIndex];
                Layers[newLayerIndex].Index = newLayerIndex;

                // Re-Z
                float posZ = layerHeight;
                if (newLayerIndex > 0)
                {
                    if (oldLayers[layerIndex - 1].PositionZ == oldLayers[layerIndex].PositionZ)
                    {
                        posZ = Layers[newLayerIndex - 1].PositionZ;
                    }
                    else
                    {
                        posZ = (float)Math.Round(Layers[newLayerIndex - 1].PositionZ + layerHeight, 2);
                    }
                }

                Layers[newLayerIndex].PositionZ = posZ;
                Layers[newLayerIndex].IsModified = true;

                newLayerIndex++;
            }

            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;
        }

        public void ReHeight(OperationLayerReHeight operation, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Re-Height", operation.LayerCount);

            var oldLayers = Layers;

            Layers = new Layer[operation.LayerCount];

            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                var oldLayer = oldLayers[layerIndex];
                if (operation.IsDivision)
                {
                    for (byte i = 0; i < operation.Modifier; i++)
                    {
                        Layers[newLayerIndex] =
                            new Layer(newLayerIndex, oldLayer.CompressedBytes, null, this)
                            {
                                PositionZ = (float) (operation.LayerHeight * (newLayerIndex + 1)),
                                ExposureTime = oldLayer.ExposureTime,
                                BoundingRectangle = oldLayer.BoundingRectangle,
                            };
                        newLayerIndex++;
                        progress++;
                    }
                }
                else
                {
                    using (var mat = oldLayers[layerIndex++].LayerMat)
                    {
                        for (byte i = 1; i < operation.Modifier; i++)
                        {
                            using (var nextMat = oldLayers[layerIndex++].LayerMat)
                            {
                                CvInvoke.Add(mat, nextMat, mat);
                            }
                        }

                        Layers[newLayerIndex] = new Layer(newLayerIndex, mat, null, this)
                        {
                            PositionZ = (float)(operation.LayerHeight * (newLayerIndex + 1)),
                            ExposureTime = oldLayer.ExposureTime
                        };
                        newLayerIndex++;
                        layerIndex--;
                        progress++;
                    }
                }
            }


            SlicerFile.LayerHeight = (float) operation.LayerHeight;
            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;
        }
    }
}
