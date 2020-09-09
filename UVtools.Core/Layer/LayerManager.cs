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
            Mask,
            PixelDimming,
            Erode,
            Dilate,
            Opening,
            Closing,
            Gradient,
            TopHat,
            BlackHat,
            HitMiss,
            ThresholdPixels,
            Blur
            /*PyrDownUp,
            SmoothMedian,
            SmoothGaussian,*/
        }
        #endregion

        #region Properties
        public FileFormat SlicerFile { get; private set; }

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

        /// <summary>
        /// Rebuild layer properties based on slice settings
        /// </summary>
        public void RebuildLayersProperties()
        {
            var layerHeight = SlicerFile.LayerHeight;
            for (uint layerIndex = 0; layerIndex < Count; layerIndex++)
            {
                var layer = this[layerIndex];
                layer.Index = layerIndex;
                layer.PositionZ = (float) Math.Round(layerHeight * (layerIndex + 1), 2);
                layer.ExposureTime = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.InitialExposureTime, SlicerFile.LayerExposureTime);
            }
        }

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

        public void Solidify(OperationSolidify operation, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Solidifying", operation.LayerRangeCount);
            Parallel.For(operation.LayerIndexStart, operation.LayerIndexEnd + 1, layerIndex =>
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

        public void MutateMask(uint startLayerIndex, uint endLayerIndex, Mat mask, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Masking pixels", endLayerIndex - startLayerIndex + 1);

            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateMask(mask);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutatePixelDimming(uint startLayerIndex, uint endLayerIndex, Matrix<byte> evenPattern = null,
            Matrix<byte> oddPattern = null, ushort borderSize = 5, bool dimOnlyBorders = false, OperationProgress progress = null)
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
                                    this[layerIndex].MutatePixelDimming(evenPatternMask, oddPatternMask, borderSize, dimOnlyBorders);
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

        private void MutateGetVarsIterationFade(uint startLayerIndex, uint endLayerIndex, int iterationsStart, int iterationsEnd, ref bool isFade, out float iterationSteps, out int maxIteration)
        {
            iterationSteps = 0;
            maxIteration = 0;
            isFade = isFade && startLayerIndex != endLayerIndex && iterationsStart != iterationsEnd;
            if (!isFade) return;
            iterationSteps = Math.Abs((iterationsStart - (float)iterationsEnd) / ((float)endLayerIndex - startLayerIndex));
            maxIteration = Math.Max(iterationsStart, iterationsEnd);
        }

        private int MutateGetIterationVar(bool isFade, int iterationsStart, int iterationsEnd, float iterationSteps, int maxIteration, uint startLayerIndex, uint layerIndex)
        {
            if (!isFade) return iterationsStart;
            // calculate iterations based on range
            int iterations = (int)(iterationsStart < iterationsEnd
                ? iterationsStart + (layerIndex - startLayerIndex) * iterationSteps
                : iterationsStart - (layerIndex - startLayerIndex) * iterationSteps);

            // constrain
            return Math.Min(Math.Max(1, iterations), maxIteration);
        }

        public void Morph(OperationMorphModel operation, BorderType borderType = BorderType.Default, MCvScalar borderValue = default, OperationProgress progress = null)
        {
            if (progress is null) progress = new OperationProgress();
            progress.Reset("Morphing model", operation.LayerRangeCount);

            var isFade = operation.FadeInOut;
            MutateGetVarsIterationFade(
                operation.LayerIndexStart,
                operation.LayerIndexEnd,
                (int) operation.IterationsStart,
                (int) operation.IterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Debug.WriteLine($"Steps: {iterationSteps}, Max iteration: {maxIteration}");

            Parallel.For(operation.LayerIndexStart, operation.LayerIndexEnd + 1, 
                new ParallelOptions {MaxDegreeOfParallelism = 1},
                layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                int iterations = MutateGetIterationVar(isFade, (int) operation.IterationsStart, (int) operation.IterationsEnd, iterationSteps, maxIteration, operation.LayerIndexStart, (uint)layerIndex);
                Debug.WriteLine(iterations);
                this[layerIndex].Morph(operation, iterations, borderType, borderValue);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();

        }

        /*public void MutateErode(uint startLayerIndex, uint endLayerIndex, int iterationsStart = 1, int iterationsEnd = 1, bool isFade = false, OperationProgress progress = null,
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
        }*/

        public void MutateThresholdPixels(uint startLayerIndex, uint endLayerIndex, byte threshold, byte maximum, ThresholdType thresholdType, OperationProgress progress)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Thresholding", endLayerIndex - startLayerIndex + 1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateThresholdPixels(threshold, maximum, thresholdType);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutateBlur(uint startLayerIndex, uint endLayerIndex, Size size, Point kernelAnchor = default, BorderType borderType = BorderType.Reflect101, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Box Blur", endLayerIndex - startLayerIndex + 1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateBlur(size, kernelAnchor, borderType);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public void MutatePyrDownUp(uint startLayerIndex, uint endLayerIndex, BorderType borderType = BorderType.Reflect101, OperationProgress progress = null)
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

        public void MutateFilter2D(uint startLayerIndex, uint endLayerIndex, IInputArray kernel = null, Point kernelAnchor = default, BorderType borderType = BorderType.Reflect101, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Filter 2D", endLayerIndex - startLayerIndex + 1);
            Parallel.For(startLayerIndex, endLayerIndex + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                this[layerIndex].MutateFilter2D(kernel, kernelAnchor, borderType);
                lock (progress.Mutex)
                {
                    progress++;
                }
            });
            progress.Token.ThrowIfCancellationRequested();
        }

        public List<LayerIssue> GetAllIssues(
            IslandDetectionConfiguration islandConfig = null, ResinTrapDetectionConfiguration resinTrapConfig = null,
            TouchingBoundDetectionConfiguration touchBoundConfig = null,
            bool emptyLayersConfig = true,
            OperationProgress progress = null)
        {
            if(ReferenceEquals(islandConfig, null)) islandConfig = new IslandDetectionConfiguration();
            if(ReferenceEquals(resinTrapConfig, null)) resinTrapConfig = new ResinTrapDetectionConfiguration();
            if(ReferenceEquals(touchBoundConfig, null)) touchBoundConfig = new TouchingBoundDetectionConfiguration();
            if(ReferenceEquals(progress, null)) progress = new OperationProgress();

            var result = new ConcurrentBag<LayerIssue>();
            var layerHollowAreas = new ConcurrentDictionary<uint, List<LayerHollowArea>>();

            bool islandsFinished = false;

            progress.Reset(OperationProgress.StatusIslands, Count);

            Parallel.Invoke(() =>
            {
                if (!islandConfig.Enabled && !touchBoundConfig.Enabled)
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
                        if (layer.NonZeroPixelCount == 0)
                        {
                            if (emptyLayersConfig)
                            {
                                result.Add(new LayerIssue(layer, LayerIssue.IssueType.EmptyLayer));
                            }

                            lock (progress.Mutex)
                            {
                                progress++;
                            }

                            return;
                        }

                        // Spare a decoding cycle
                        if (!touchBoundConfig.Enabled && 
                            (layer.Index == 0 || 
                             (!ReferenceEquals(islandConfig.WhiteListLayers, null) && !islandConfig.WhiteListLayers.Contains(layer.Index)))
                            )
                        {
                            lock (progress.Mutex)
                            {
                                progress++;
                            }
                            return;
                        }

                        using (var image = layer.LayerMat)
                        {
                            int step = image.Step;
                            var span = image.GetPixelSpan<byte>();

                            if (touchBoundConfig.Enabled)
                            {
                                // TouchingBounds Checker
                                List<Point> pixels = new List<Point>();
                                for (int x = 0; x < image.Width; x++) // Check Top and Bottom bounds
                                {
                                    if (span[x] >= touchBoundConfig.MaximumPixelBrightness) // Top
                                    {
                                        pixels.Add(new Point(x, 0));
                                    }

                                    if (span[step * image.Height - step + x] >=
                                        touchBoundConfig.MaximumPixelBrightness) // Bottom
                                    {
                                        pixels.Add(new Point(x, image.Height - 1));
                                    }
                                }

                                for (int y = 0; y < image.Height; y++) // Check Left and Right bounds
                                {
                                    if (span[y * step] >= touchBoundConfig.MaximumPixelBrightness) // Left
                                    {
                                        pixels.Add(new Point(0, y));
                                    }

                                    if (span[y * step + step - 1] >= touchBoundConfig.MaximumPixelBrightness) // Right
                                    {
                                        pixels.Add(new Point(step - 1, y));
                                    }
                                }

                                if (pixels.Count > 0)
                                {
                                    result.Add(new LayerIssue(layer, LayerIssue.IssueType.TouchingBound,
                                        pixels.ToArray()));
                                    /*result.TryAdd(layer.Index, new List<LayerIssue>
                                        {
                                            new LayerIssue(layer, LayerIssue.IssueType.TouchingBound, pixels.ToArray())
                                        });*/
                                }
                            }

                            if (layer.Index == 0 || !islandConfig.Enabled)
                            {
                                lock (progress.Mutex)
                                {
                                    progress++;
                                }
                                return; // No islands for layer 0
                            }

                            if (!ReferenceEquals(islandConfig.WhiteListLayers, null)) // Check white list
                            {
                                if (!islandConfig.WhiteListLayers.Contains(layer.Index))
                                {
                                    lock (progress.Mutex)
                                    {
                                        progress++;
                                    }
                                    return;
                                }
                            }

                            if (islandConfig.BinaryThreshold > 0)
                            {
                                CvInvoke.Threshold(image, image, islandConfig.BinaryThreshold, 255,
                                    ThresholdType.Binary);
                            }

                            using (Mat labels = new Mat())
                            using (Mat stats = new Mat())
                            using (Mat centroids = new Mat())
                            {
                                var numLabels = CvInvoke.ConnectedComponentsWithStats(image, labels, stats, centroids,
                                    islandConfig.AllowDiagonalBonds ? LineType.EightConnected : LineType.FourConnected);
                                
                                // Get array that contains details of each connected component
                                var ccStats = stats.GetData();
                                //stats[i][0]: Left Edge of Connected Component
                                //stats[i][1]: Top Edge of Connected Component 
                                //stats[i][2]: Width of Connected Component
                                //stats[i][3]: Height of Connected Component
                                //stats[i][4]: Total Area (in pixels) in Connected Component

                                Span<int> labelSpan = labels.GetPixelSpan<int>();
                                Mat previousImage = null;
                                Span<byte> previousSpan = null;

                                for (int i = 1; i < numLabels; i++)
                                {
                                    Rectangle rect = new Rectangle((int)ccStats.GetValue(i, (int)ConnectedComponentsTypes.Left),
                                                                   (int)ccStats.GetValue(i, (int)ConnectedComponentsTypes.Top),
                                                                   (int)ccStats.GetValue(i, (int)ConnectedComponentsTypes.Width),
                                                                   (int)ccStats.GetValue(i, (int)ConnectedComponentsTypes.Height));
                                    
                                    if (rect.GetArea() < islandConfig.RequiredAreaToProcessCheck)
                                        continue;

                                    if (ReferenceEquals(previousImage, null))
                                    {
                                        previousImage = this[layer.Index - 1].LayerMat;
                                        previousSpan = previousImage.GetPixelSpan<byte>();
                                    }

                                    List<Point> points = new List<Point>();
                                    uint pixelsSupportingIsland = 0;

                                    for (int y = rect.Y; y < rect.Bottom; y++)
                                    {
                                        for (int x = rect.X; x < rect.Right; x++)
                                        {
                                            int pixel = step * y + x;
                                            if (
                                                labelSpan[pixel] != i || // Background pixel or a pixel from another component within the bounding rectangle
                                                span[pixel] < islandConfig.RequiredPixelBrightnessToProcessCheck // Low brightness, ignore
                                            ) continue;

                                            points.Add(new Point(x, y));

                                            if (previousSpan[pixel] >= islandConfig.RequiredPixelBrightnessToSupport)
                                            {
                                                pixelsSupportingIsland++;
                                            }
                                        }
                                    }

                                    if (points.Count == 0) continue; // Should never happen
                                    if (pixelsSupportingIsland >= islandConfig.RequiredPixelsToSupport)
                                        continue; // Not a island, bounding is strong, i think...
                                    if (pixelsSupportingIsland > 0 &&
                                        points.Count < islandConfig.RequiredPixelsToSupport &&
                                        pixelsSupportingIsland >= Math.Max(1, points.Count / 2))
                                        continue; // Not a island, but maybe weak bounding...


                                    var issue = new LayerIssue(layer, LayerIssue.IssueType.Island, points.ToArray(),
                                        rect);
                                    result.Add(issue);
                                }


                                previousImage?.Dispose();
                            }
                        }

                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    }); // Parallel end
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

            /*var resultSorted = result.ToList();
            resultSorted.Sort((issue, layerIssue) =>
            {
                int ret = issue.Type.CompareTo(layerIssue.Type);
                return ret != 0 ? ret : issue.LayerIndex.CompareTo(layerIssue.LayerIndex);
            });*/
            if (progress.Token.IsCancellationRequested) return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.PixelsCount).ToList();

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
                    result.Add(issue);
                }
            }

            return result.OrderBy(issue => issue.Type).ThenBy(issue => issue.LayerIndex).ThenBy(issue => issue.PixelsCount).ToList();
        }

        public void RepairLayers(uint layerStart, uint layerEnd, uint closingIterations = 1, uint openingIterations = 0, byte removeIslandsBelowEqualPixels = 4,
            bool repairIslands = true, bool removeEmptyLayers = true, bool repairResinTraps = true, List<LayerIssue> issues = null,
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
                    Mat image = null;

                    void initImage()
                    {
                        if(ReferenceEquals(image, null))
                            image = layer.LayerMat;
                    }

                    if (!ReferenceEquals(issues, null))
                    {
                        if (repairIslands && removeIslandsBelowEqualPixels > 0)
                        {
                            Span<byte> bytes = null;
                            foreach (var issue in issues)
                            {
                                if (
                                    issue.LayerIndex != layerIndex ||
                                    issue.Type != LayerIssue.IssueType.Island ||
                                    issue.Pixels.Length > removeIslandsBelowEqualPixels) continue;

                                initImage();
                                if(bytes == null)
                                    bytes = image.GetPixelSpan<byte>();

                                foreach (var issuePixel in issue.Pixels)
                                {
                                    bytes[image.GetPixelPos(issuePixel)] = 0;
                                }
                            }
                            /*if (issues.TryGetValue((uint)layerIndex, out var issueList))
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
                            }*/
                        }

                        if (repairResinTraps)
                        {
                            foreach (var issue in issues.Where(issue => issue.LayerIndex == layerIndex && issue.Type == LayerIssue.IssueType.ResinTrap))
                            {
                                initImage();
                                using (var vec = new VectorOfVectorOfPoint(new VectorOfPoint(issue.Pixels)))
                                {
                                    CvInvoke.DrawContours(image,
                                        vec,
                                        -1,
                                        new MCvScalar(255),
                                        -1);
                                }
                            }
                        }
                    }

                    if (repairIslands && (closingIterations > 0 || openingIterations > 0))
                    {
                        initImage();
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

                    if (!ReferenceEquals(image, null))
                    {
                        layer.LayerMat = image;
                        image.Dispose();
                    }

                    lock (progress.Mutex)
                    {
                        progress++;
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
                    RemoveLayers(removeLayers);
                }
            }

            progress.Token.ThrowIfCancellationRequested();
        }

        public void Import(OperationLayerImport operation, OperationProgress progress = null)
        {
            if (progress is null) progress = new OperationProgress();
            progress.Reset("Imported layers", (uint)operation.Count);

            var oldLayers = Layers;
            uint newLayerCount = operation.CalculateTotalLayers((uint) Layers.Length);
            uint startIndex = operation.LayerIndexStart;
            Layers = new Layer[newLayerCount];

            // Keep same layers up to InsertAfterLayerIndex
            for (uint i = 0; i <= operation.InsertAfterLayerIndex; i++)
            {
                Layers[i] = oldLayers[i];
            }

            // Keep all old layers if not discarding them
            if (operation.ReplaceSubsequentLayers)
            {
                if (!operation.DiscardRemainingLayers)
                {
                    for (uint i = operation.InsertAfterLayerIndex + 1; i < oldLayers.Length; i++)
                    {
                        Layers[i] = oldLayers[i];
                    }
                }
            }
            else // Push remaining layers to the end of imported layers
            {
                uint oldLayerIndex = operation.InsertAfterLayerIndex;
                for (uint i = operation.LayerIndexEnd + 1; i < newLayerCount; i++)
                {
                    oldLayerIndex++;
                    Layers[i] = oldLayers[oldLayerIndex];
                }
            }

            
            Parallel.For(0, operation.Count, 
                //new ParallelOptions{MaxDegreeOfParallelism = 1},
                i =>
            {
                var mat = CvInvoke.Imread(operation.Files[i], ImreadModes.Grayscale);
                uint layerIndex = (uint) (startIndex + i);
                this[layerIndex] = new Layer(layerIndex, mat);

                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;

            RebuildLayersProperties();

            progress.Token.ThrowIfCancellationRequested();
        }

        public void CloneLayer(OperationLayerClone operation, OperationProgress progress = null)
        {
            var oldLayers = Layers;

            uint totalClones = (operation.LayerIndexEnd - operation.LayerIndexStart + 1) * operation.Clones;
            uint newLayerCount = Count + totalClones;
            Layers = new Layer[newLayerCount];

            progress.Reset("Cloned layers", totalClones);

            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                Layers[newLayerIndex] = oldLayers[layerIndex];
                if (layerIndex >= operation.LayerIndexStart && layerIndex <= operation.LayerIndexEnd)
                {
                    for (uint i = 0; i < operation.Clones; i++)
                    {
                        newLayerIndex++;
                        Layers[newLayerIndex] = oldLayers[layerIndex].Clone();
                        Layers[newLayerIndex].IsModified = true;

                        progress++;
                    }
                }

                newLayerIndex++;
            }

            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;

            RebuildLayersProperties();

            progress.Token.ThrowIfCancellationRequested();
        }

        public void RemoveLayer(uint layerIndex) => RemoveLayers(new OperationLayerRemove
        {
            LayerIndexStart = layerIndex,
            LayerIndexEnd = layerIndex,
        });

        public void RemoveLayers(OperationLayerRemove operation, OperationProgress progress = null)
        {
            if(progress is null)
                progress = new OperationProgress(false);
            var layersRemove = new List<uint>();
            for (uint layerIndex = operation.LayerIndexStart; layerIndex <= operation.LayerIndexEnd; layerIndex++)
            {
                layersRemove.Add(layerIndex);
            }

            RemoveLayers(layersRemove, progress);
        }

        public void RemoveLayers(List<uint> layersRemove, OperationProgress progress = null)
        {
            if (layersRemove.Count == 0) return;

            if (progress is null)
                progress = new OperationProgress(false);

            progress.Reset("Removing layers", (uint) layersRemove.Count);

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
                progress++;
            }

            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;
        }

        public void ReHeight(OperationLayerReHeight operation, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset($"Layer re-height from {SlicerFile.LayerHeight}mm to {operation.Item.LayerHeight}mm");

            var oldLayers = Layers;

            Layers = new Layer[operation.Item.LayerCount];

            uint newLayerIndex = 0;
            for (uint layerIndex = 0; layerIndex < oldLayers.Length; layerIndex++)
            {
                var oldLayer = oldLayers[layerIndex];
                if (operation.Item.IsDivision)
                {
                    for (byte i = 0; i < operation.Item.Modifier; i++)
                    {
                        Layers[newLayerIndex] =
                            new Layer(newLayerIndex, oldLayer.CompressedBytes, null, this)
                            {
                                PositionZ = (float)(operation.Item.LayerHeight * (newLayerIndex + 1)),
                                ExposureTime = oldLayer.ExposureTime,
                                BoundingRectangle = oldLayer.BoundingRectangle,
                                NonZeroPixelCount = oldLayer.NonZeroPixelCount

                            };
                        newLayerIndex++;
                        progress++;
                    }
                }
                else
                {
                    using (var mat = oldLayers[layerIndex++].LayerMat)
                    {
                        for (byte i = 1; i < operation.Item.Modifier; i++)
                        {
                            using (var nextMat = oldLayers[layerIndex++].LayerMat)
                            {
                                CvInvoke.Add(mat, nextMat, mat);
                            }
                        }

                        Layers[newLayerIndex] = new Layer(newLayerIndex, mat, null, this)
                        {
                            PositionZ = (float)(operation.Item.LayerHeight * (newLayerIndex + 1)),
                            ExposureTime = oldLayer.ExposureTime
                        };
                        newLayerIndex++;
                        layerIndex--;
                        progress++;
                    }
                }
            }


            SlicerFile.LayerHeight = (float)operation.Item.LayerHeight;
            SlicerFile.LayerCount = Count;
            BoundingRectangle = Rectangle.Empty;
            SlicerFile.RequireFullEncode = true;
        }

        public void ChangeResolution(OperationChangeResolution operation, OperationProgress progress)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Resolution", Count);

            Parallel.For(0, Count, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                this[layerIndex].ChangeResolution(operation);

                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            SlicerFile.ResolutionX = operation.NewResolutionX;
            SlicerFile.ResolutionY = operation.NewResolutionY;
        }

        public void Pattern(OperationPattern operation, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Pattern", operation.LayerRangeCount);

            Parallel.For(operation.LayerIndexStart, operation.LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                this[layerIndex].Pattern(operation);

                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            _boundingRectangle = Rectangle.Empty;

            progress.Token.ThrowIfCancellationRequested();

            if (operation.Anchor == Anchor.None) return;
            MutateMove(operation.LayerIndexStart, operation.LayerIndexEnd, new OperationMove(BoundingRectangle, 0, 0, operation.Anchor), progress);

        }

        public void DrawModifications(PixelHistory pixelHistory, OperationProgress progress = null)
        {
            if (ReferenceEquals(progress, null)) progress = new OperationProgress();
            progress.Reset("Drawings", (uint) pixelHistory.Count);

            ConcurrentDictionary<uint, Mat> modifiedLayers = new ConcurrentDictionary<uint, Mat>();
            for (var i = 0; i < pixelHistory.Count; i++)
            {
                var operation = pixelHistory[i];
                VectorOfVectorOfPoint layerContours = null;
                Mat layerHierarchy = null;

                if (operation.OperationType == PixelOperation.PixelOperationType.Drawing)
                {
                    var operationDrawing = (PixelDrawing) operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (operationDrawing.BrushSize == 1)
                    {
                        mat.SetByte(operation.Location.X, operation.Location.Y, operationDrawing.Color);
                        continue;
                    }

                    switch (operationDrawing.BrushShape)
                    {
                        case PixelDrawing.BrushShapeType.Rectangle:
                            CvInvoke.Rectangle(mat, operationDrawing.Rectangle, new MCvScalar(operationDrawing.Color), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        case PixelDrawing.BrushShapeType.Circle:
                            CvInvoke.Circle(mat, operation.Location, operationDrawing.BrushSize / 2,
                                new MCvScalar(operationDrawing.Color), operationDrawing.Thickness, operationDrawing.LineType);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Text)
                {
                    var operationText = (PixelText)operation;
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    CvInvoke.PutText(mat, operationText.Text, operationText.Location, operationText.Font, operationText.FontScale, new MCvScalar(operationText.Color), operationText.Thickness, operationText.LineType, operationText.Mirror);
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Eraser)
                {
                    var mat = modifiedLayers.GetOrAdd(operation.LayerIndex, u => this[operation.LayerIndex].LayerMat);

                    if (ReferenceEquals(layerContours, null))
                    {
                        layerContours = new VectorOfVectorOfPoint();
                        layerHierarchy = new Mat();

                        CvInvoke.FindContours(mat, layerContours, layerHierarchy, RetrType.Ccomp,
                            ChainApproxMethod.ChainApproxSimple);
                    }

                    if (mat.GetByte(operation.Location) >= 10)
                    {
                        for (int contourIdx = 0; contourIdx < layerContours.Size; contourIdx++)
                        {
                            if (!(CvInvoke.PointPolygonTest(layerContours[contourIdx], operation.Location, false) >= 0))
                                continue;
                            CvInvoke.DrawContours(mat, layerContours, contourIdx, new MCvScalar(0, 0, 0), -1);
                            break;
                        }
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.Supports)
                {
                    var operationSupport = (PixelSupport)operation;
                    int drawnLayers = 0;
                    for (int operationLayer = (int)operation.LayerIndex-1; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint) operationLayer, u => this[operationLayer].LayerMat);
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

                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(255), -1, operationSupport.LineType);
                        drawnLayers++;
                    }
                }
                else if (operation.OperationType == PixelOperation.PixelOperationType.DrainHole)
                {
                    uint drawnLayers = 0;
                    var operationDrainHole = (PixelDrainHole)operation;
                    for (int operationLayer = (int)operation.LayerIndex; operationLayer >= 0; operationLayer--)
                    {
                        var mat = modifiedLayers.GetOrAdd((uint)operationLayer, u => this[operationLayer].LayerMat);
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
                        
                        CvInvoke.Circle(mat, operation.Location, radius, new MCvScalar(0), -1, operationDrainHole.LineType);
                        drawnLayers++;
                    }
                }

                layerContours?.Dispose();
                layerHierarchy?.Dispose();

                progress++;
            }

            progress.Reset("Saving", (uint) modifiedLayers.Count);
            Parallel.ForEach(modifiedLayers, (modfiedLayer, state) =>
            {
                this[modfiedLayer.Key].LayerMat = modfiedLayer.Value;
                modfiedLayer.Value.Dispose();

                lock (progress)
                {
                    progress++;
                }
            });
            /*foreach (var modfiedLayer in modfiedLayers)
            {
                this[modfiedLayer.Key].LayerMat = modfiedLayer.Value;
                modfiedLayer.Value.Dispose();
                progress++;
            }*/

            //pixelHistory.Clear();
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

        
    }
}
