/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using Stream = System.IO.Stream;

namespace UVtools.Core
{
    /// <summary>
    /// Represent a Layer
    /// </summary>
    public class Layer : BindableBase, IEquatable<Layer>, IEquatable<uint>
    {
        #region Properties

        public object Mutex = new object();

        /// <summary>
        /// Gets the parent layer manager
        /// </summary>
        public LayerManager ParentLayerManager { get; set; }

        public FileFormat SlicerFile => ParentLayerManager?.SlicerFile;

        /// <summary>
        /// Gets the number of non zero pixels on this layer image
        /// </summary>
        public uint NonZeroPixelCount
        {
            get => _nonZeroPixelCount;
            internal set => RaiseAndSetIfChanged(ref _nonZeroPixelCount, value);
        }

        /// <summary>
        /// Gets the bounding rectangle for the image area
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get => _boundingRectangle;
            internal set => RaiseAndSetIfChanged(ref _boundingRectangle, value);
        }

        public bool IsBottomLayer => Index < ParentLayerManager.SlicerFile.BottomLayerCount;
        public bool IsNormalLayer => !IsBottomLayer;

        /// <summary>
        /// Gets the layer index
        /// </summary>
        public uint Index
        {
            get => _index;
            set => RaiseAndSetIfChanged(ref _index, value);
        }

        /// <summary>
        /// Gets or sets the normal layer exposure time in seconds
        /// </summary>
        public float ExposureTime
        {
            get => _exposureTime;
            set => RaiseAndSetIfChanged(ref _exposureTime, value);
        }

        /// <summary>
        /// Gets or sets the layer off time in seconds
        /// </summary>
        public float LayerOffTime
        {
            get => _layerOffTime;
            set => RaiseAndSetIfChanged(ref _layerOffTime, value);
        }

        /// <summary>
        /// Gets or sets the lift height in mm
        /// </summary>
        public float LiftHeight
        {
            get => _liftHeight;
            set => RaiseAndSetIfChanged(ref _liftHeight, value);
        }

        /// <summary>
        /// Gets or sets the speed in mm/min
        /// </summary>
        public float LiftSpeed
        {
            get => _liftSpeed;
            set => RaiseAndSetIfChanged(ref _liftSpeed, value);
        }

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        public float RetractSpeed
        {
            get => _retractSpeed;
            set => RaiseAndSetIfChanged(ref _retractSpeed, value);
        }

        /// <summary>
        /// Gets or sets the pwm value from 0 to 255
        /// </summary>
        public byte LightPWM
        {
            get => _lightPwm;
            set => RaiseAndSetIfChanged(ref _lightPwm, value);
        }

        /// <summary>
        /// Gets or sets the layer position on Z in mm
        /// </summary>
        public float PositionZ
        {
            get => _positionZ;
            set => RaiseAndSetIfChanged(ref _positionZ, value);
        }

        private byte[] _compressedBytes;
        private uint _nonZeroPixelCount;
        private Rectangle _boundingRectangle = Rectangle.Empty;
        private bool _isModified;
        private uint _index;
        private float _positionZ;
        private float _exposureTime;
        private float _layerOffTime = FileFormat.DefaultLightOffDelay;
        private float _liftHeight = FileFormat.DefaultLiftHeight;
        private float _liftSpeed = FileFormat.DefaultLiftSpeed;
        private float _retractSpeed = FileFormat.DefaultRetractSpeed;
        private byte _lightPwm = FileFormat.DefaultLightPWM;

        /// <summary>
        /// Gets or sets layer image compressed data
        /// </summary>
        public byte[] CompressedBytes
        {
            get => _compressedBytes;
            set
            {
                _compressedBytes = value;
                IsModified = true;
                if (!ReferenceEquals(ParentLayerManager, null))
                    ParentLayerManager.BoundingRectangle = Rectangle.Empty;
            }
        }

        /// <summary>
        /// Gets a computed layer filename, padding zeros are equal to layer count digits
        /// </summary>
        public string Filename => FormatFileName("layer");

        /// <summary>
        /// Gets if layer has been modified
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set => RaiseAndSetIfChanged(ref _isModified, value);
        }

        /// <summary>
        /// Gets or sets a new image instance
        /// </summary>
        public Mat LayerMat
        {
            get
            {
                Mat mat = new Mat();
                CvInvoke.Imdecode(CompressedBytes, ImreadModes.Grayscale, mat);
                return mat;
            }
            set
            {
                using (var vector = new VectorOfByte())
                {
                    CvInvoke.Imencode(".png", value, vector);
                    CompressedBytes = vector.ToArray();

                    GetBoundingRectangle(value, true);
                }

                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets a new Brg image instance
        /// </summary>
        public Mat BrgMat
        {
            get
            {
                Mat mat = LayerMat;
                CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
                return mat;
            }
        }

        #endregion

        #region Constructor
        public Layer(uint index, byte[] compressedBytes, LayerManager parentLayerManager)
        {
            ParentLayerManager = parentLayerManager;
            Index = index;
            //Filename = filename ?? $"Layer{index}.png";
            CompressedBytes = compressedBytes;
            IsModified = false;
            /*if (compressedBytes.Length > 0)
            {
                GetBoundingRectangle();
            }*/

            if (!(parentLayerManager is null))
            {
                _positionZ = SlicerFile.GetHeightFromLayer(index);
                _exposureTime = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
                _liftHeight = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
                _liftSpeed = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
                _retractSpeed = SlicerFile.RetractSpeed;
                _lightPwm = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
            }
        }

        public Layer(uint index, Mat layerMat, LayerManager parentLayerManager) : this(index, new byte[0], parentLayerManager)
        {
            LayerMat = layerMat;
            IsModified = false;
        }


        public Layer(uint index, Stream stream, LayerManager parentLayerManager) : this(index, stream.ToArray(), parentLayerManager)
        { }
        #endregion

        #region Equatables

        public static bool operator ==(Layer obj1, Layer obj2)
        {
            return obj1.Equals(obj2);
        }

        public static bool operator !=(Layer obj1, Layer obj2)
        {
            return !obj1.Equals(obj2);
        }

        public static bool operator >(Layer obj1, Layer obj2)
        {
            return obj1.Index > obj2.Index;
        }

        public static bool operator <(Layer obj1, Layer obj2)
        {
            return obj1.Index < obj2.Index;
        }

        public static bool operator >=(Layer obj1, Layer obj2)
        {
            return obj1.Index >= obj2.Index;
        }

        public static bool operator <=(Layer obj1, Layer obj2)
        {
            return obj1.Index <= obj2.Index;
        }

        public bool Equals(uint other)
        {
            return Index == other;
        }

        public bool Equals(Layer other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (_index != other._index) return false;
            if (_compressedBytes.Length != other._compressedBytes.Length) return false;
            return _compressedBytes.AsSpan().SequenceEqual(other._compressedBytes.AsSpan());
            //return Equals(_compressedBytes, other._compressedBytes);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Layer)obj);
        }

        public override int GetHashCode()
        {
            return (_compressedBytes != null ? _compressedBytes.GetHashCode() : 0);
        }

        private sealed class IndexRelationalComparer : IComparer<Layer>
        {
            public int Compare(Layer x, Layer y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return x.Index.CompareTo(y.Index);
            }
        }

        public static IComparer<Layer> IndexComparer { get; } = new IndexRelationalComparer();
        #endregion

        #region Formaters

        public override string ToString()
        {
            return $"{nameof(Index)}: {Index}, {nameof(Filename)}: {Filename}, {nameof(NonZeroPixelCount)}: {NonZeroPixelCount}, {nameof(BoundingRectangle)}: {BoundingRectangle}, {nameof(IsBottomLayer)}: {IsBottomLayer}, {nameof(IsNormalLayer)}: {IsNormalLayer}, {nameof(PositionZ)}: {PositionZ}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(LightPWM)}: {LightPWM}, {nameof(IsModified)}: {IsModified}";
        }
        #endregion

        #region Methods

        public string FormatFileName(string name)
        {
            return $"{name}{Index.ToString().PadLeft(ParentLayerManager.LayerDigits, '0')}.png";
        }

        public Rectangle GetBoundingRectangle(Mat mat = null, bool reCalculate = false)
        {
            if (NonZeroPixelCount > 0 && !reCalculate)
            {
                return BoundingRectangle;
            }
            bool needDispose = false;
            if (mat is null)
            {
                mat = LayerMat;
                needDispose = true;
            }

            using (var nonZeroMat = new Mat())
            {
                CvInvoke.FindNonZero(mat, nonZeroMat);
                NonZeroPixelCount = (uint)nonZeroMat.Height;
                BoundingRectangle = NonZeroPixelCount > 0 ? CvInvoke.BoundingRectangle(nonZeroMat) : Rectangle.Empty;
            }


            if (needDispose) mat.Dispose();

            return BoundingRectangle;
        }

        public Layer PreviousLayer()
        {
            if (ReferenceEquals(ParentLayerManager, null) || Index == 0)
                return null;

            return ParentLayerManager[Index - 1];
        }

        public Layer NextLayer()
        {
            if (ReferenceEquals(ParentLayerManager, null) || Index >= ParentLayerManager.Count - 1)
                return null;

            return ParentLayerManager[Index + 1];
        }

        public bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, decimal value)
        {
            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.ExposureSeconds))
            {
                ExposureTime = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LayerOffTime))
            {
                LayerOffTime = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftHeight))
            {
                LiftHeight = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LiftSpeed))
            {
                LiftSpeed = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.RetractSpeed))
            {
                RetractSpeed = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LightPWM))
            {
                LightPWM = (byte)value;
                return true;
            }

            return false;
        }

        public byte SetValuesFromPrintParametersModifiers(FileFormat.PrintParameterModifier[] modifiers)
        {
            if (modifiers is null) return 0;
            byte changed = 0;
            foreach (var modifier in modifiers)
            {
                if (!modifier.HasChanged) continue;
                modifier.OldValue = modifier.NewValue;
                SetValueFromPrintParameterModifier(modifier, modifier.NewValue);
                changed++;
            }

            return changed;
        }

        /// <summary>
        /// Gets all islands start pixel location for this layer
        /// https://www.geeksforgeeks.org/find-number-of-islands/
        /// </summary>
        /// <returns><see cref="List{T}"/> holding all islands coordinates</returns>
        public List<LayerIssue> GetIssues(uint requiredPixelsToSupportIsland = 5)
        {
            if (requiredPixelsToSupportIsland == 0)
                requiredPixelsToSupportIsland = 1;

            // These arrays are used to 
            // get row and column numbers 
            // of 8 neighbors of a given cell 
            List<LayerIssue> result = new List<LayerIssue>();
            List<Point> pixels = new List<Point>();



            var mat = LayerMat;
            var bytes = mat.GetPixelSpan<byte>();



            var previousLayerImage = PreviousLayer()?.LayerMat;
            var previousBytes = previousLayerImage?.GetBytes();


            /*var nextLayerImage = NextLayer()?.Image;
            byte[] nextBytes = null;
            if (!ReferenceEquals(nextLayerImage, null))
            {
                if (nextLayerImage.TryGetSinglePixelSpan(out var nextPixelSpan))
                {
                    nextBytes = MemoryMarshal.AsBytes(nextPixelSpan).ToArray();
                }
            }*/

            // Make a bool array to
            // mark visited cells. 
            // Initially all cells 
            // are unvisited 
            bool[,] visited = new bool[mat.Width, mat.Height];

            // Initialize count as 0 and 
            // traverse through the all 
            // cells of given matrix 
            //uint count = 0;

            // Island checker
            sbyte[] rowNbr = { -1, -1, -1, 0, 0, 1, 1, 1 };
            sbyte[] colNbr = { -1, 0, 1, -1, 1, -1, 0, 1 };
            const uint minPixel = 10;
            const uint minPixelForSupportIsland = 200;
            int pixelIndex;
            uint islandSupportingPixels;
            if (Index > 0)
            {
                for (int y = 0; y < mat.Height; y++)
                {
                    for (int x = 0; x < mat.Width; x++)
                    {
                        pixelIndex = y * mat.Width + x;

                        /*if (bytes[pixelIndex] == 0 && previousBytes?[pixelIndex] == byte.MaxValue &&
                            nextBytes?[pixelIndex] == byte.MaxValue)
                        {
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.HoleSandwich, new []{new Point(x, y)}));
                        }*/

                        if (bytes[pixelIndex] > minPixel && !visited[x, y])
                        {
                            // If a cell with value 1 is not 
                            // visited yet, then new island 
                            // found, Visit all cells in this 
                            // island and increment island count 
                            pixels.Clear();
                            pixels.Add(new Point(x, y));
                            islandSupportingPixels = previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;

                            int minX = x;
                            int maxX = x;
                            int minY = y;
                            int maxY = y;

                            int x2;
                            int y2;


                            Queue<Point> queue = new Queue<Point>();
                            queue.Enqueue(new Point(x, y));
                            // Mark this cell as visited 
                            visited[x, y] = true;

                            while (queue.Count > 0)
                            {
                                var point = queue.Dequeue();
                                y2 = point.Y;
                                x2 = point.X;
                                for (byte k = 0; k < 8; k++)
                                {
                                    //if (isSafe(y2 + rowNbr[k], x2 + colNbr[k]))
                                    var tempy2 = y2 + rowNbr[k];
                                    var tempx2 = x2 + colNbr[k];
                                    pixelIndex = tempy2 * mat.Width + tempx2;
                                    if (tempy2 >= 0 &&
                                        tempy2 < mat.Height &&
                                        tempx2 >= 0 && tempx2 < mat.Width &&
                                        bytes[pixelIndex] >= minPixel &&
                                        !visited[tempx2, tempy2])
                                    {
                                        visited[tempx2, tempy2] = true;
                                        point = new Point(tempx2, tempy2);
                                        pixels.Add(point);
                                        queue.Enqueue(point);

                                        minX = Math.Min(minX, tempx2);
                                        maxX = Math.Max(maxX, tempx2);
                                        minY = Math.Min(minY, tempy2);
                                        maxY = Math.Max(maxY, tempy2);

                                        islandSupportingPixels += previousBytes[pixelIndex] >= minPixelForSupportIsland ? 1u : 0;
                                    }
                                }
                            }
                            //count++;

                            if (islandSupportingPixels >= requiredPixelsToSupportIsland)
                                continue; // Not a island, bounding is strong
                            if (islandSupportingPixels > 0 && pixels.Count < requiredPixelsToSupportIsland &&
                                islandSupportingPixels >= Math.Max(1, pixels.Count / 2)) continue; // Not a island
                            result.Add(new LayerIssue(this, LayerIssue.IssueType.Island, pixels.ToArray(), new Rectangle(minX, minY, maxX - minX, maxY - minY)));
                        }
                    }
                }
            }

            pixels.Clear();

            // TouchingBounds Checker
            for (int x = 0; x < mat.Width; x++) // Check Top and Bottom bounds
            {
                if (bytes[x] >= 200) // Top
                {
                    pixels.Add(new Point(x, 0));
                }

                if (bytes[mat.Width * mat.Height - mat.Width + x] >= 200) // Bottom
                {
                    pixels.Add(new Point(x, mat.Height - 1));
                }
            }

            for (int y = 0; y < mat.Height; y++) // Check Left and Right bounds
            {
                if (bytes[y * mat.Width] >= 200) // Left
                {
                    pixels.Add(new Point(0, y));
                }

                if (bytes[y * mat.Width + mat.Width - 1] >= 200) // Right
                {
                    pixels.Add(new Point(mat.Width - 1, y));
                }
            }

            if (pixels.Count > 0)
            {
                result.Add(new LayerIssue(this, LayerIssue.IssueType.TouchingBound, pixels.ToArray()));
            }

            pixels.Clear();

            return result;
        }

        public void Move(OperationMove operation)
        {
            using (var mat = LayerMat)
            {
                if (operation.ImageWidth == 0) operation.ImageWidth = (uint)mat.Width;
                if (operation.ImageHeight == 0) operation.ImageHeight = (uint)mat.Height;

                /*layer.Transform(1.0, 1.0, move.MarginLeft - move.MarginRight, move.MarginTop-move.MarginBottom);
                LayerMat = layer;*/
                /*using (var layerRoi = new Mat(layer, operation.SrcRoi))
                using (var dstLayer = layer.CloneBlank())
                using (var dstRoi = new Mat(dstLayer, operation.DstRoi))
                {
                    layerRoi.CopyTo(dstRoi);
                    LayerMat = dstLayer;
                }*/

                using (var srcRoi = new Mat(mat, operation.ROI))
                using (var dstRoi = new Mat(mat, operation.DstRoi))
                {
                    if (operation.IsCutMove)
                    {
                        using (var targetRoi = srcRoi.Clone())
                        {
                            srcRoi.SetTo(new MCvScalar(0));
                            targetRoi.CopyTo(dstRoi);
                        }
                    }
                    else
                    {
                        srcRoi.CopyTo(dstRoi);
                    }

                    LayerMat = mat;
                }
            }
        }


        public void Resize(double xScale, double yScale, OperationResize operation)
        {
            using (var mat = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(mat);
                target.TransformFromCenter(xScale, yScale);
                LayerMat = mat;
            }
        }

        public void Flip(OperationFlip operation)
        {
            using (var mat = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(mat);

                if (operation.MakeCopy)
                {
                    using (Mat dst = new Mat())
                    {

                        CvInvoke.Flip(target, dst, operation.FlipTypeOpenCV);
                        CvInvoke.Add(target, dst, target);
                    }
                }
                else
                {
                    CvInvoke.Flip(target, target, operation.FlipTypeOpenCV);
                }

                LayerMat = mat;
            }
        }

        public void Rotate(OperationRotate operation)
        {
            using (var mat = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(mat);

                var halfWidth = target.Width / 2.0f;
                var halfHeight = target.Height / 2.0f;
                using (var translateTransform = new Matrix<double>(2, 3))
                {
                    CvInvoke.GetRotationMatrix2D(new PointF(halfWidth, halfHeight), (double) -operation.AngleDegrees, 1.0, translateTransform);
                    /*var rect = new RotatedRect(PointF.Empty, mat.Size, (float) angle).MinAreaRect();
                    translateTransform[0, 2] += rect.Width / 2.0 - mat.Cols / 2.0;
                    translateTransform[0, 2] += rect.Height / 2.0 - mat.Rows / 2.0;*/

                    /*   var abs_cos = Math.Abs(translateTransform[0, 0]);
                       var abs_sin = Math.Abs(translateTransform[0, 1]);

                       var bound_w = mat.Height * abs_sin + mat.Width * abs_cos;
                       var bound_h = mat.Height * abs_cos + mat.Width * abs_sin;

                       translateTransform[0, 2] += bound_w / 2 - halfWidth;
                       translateTransform[1, 2] += bound_h / 2 - halfHeight;*/


                    CvInvoke.WarpAffine(target, target, translateTransform, target.Size);
                }

                LayerMat = mat;
            }
        }

        public void Solidify(OperationSolidify operation)
        {
            using (Mat mat = LayerMat)
            {
                using (Mat filteredMat = new Mat())
                {
                    Mat target = operation.GetRoiOrDefault(mat);


                    CvInvoke.Threshold(target, filteredMat, 254, 255, ThresholdType.Binary); // Clean AA

                    using (VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint())
                    {
                        using (Mat hierarchy = new Mat())
                        {
                            CvInvoke.FindContours(filteredMat, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
                            var arr = hierarchy.GetData();
                            for (int i = 0; i < contours.Size; i++)
                            {
                                if ((int)arr.GetValue(0, i, 2) != -1 || (int)arr.GetValue(0, i, 3) == -1) continue;
                                if (operation.MinimumArea > 1)
                                {
                                    var rectangle = CvInvoke.BoundingRectangle(contours[i]);
                                    if (operation.AreaCheckType == OperationSolidify.AreaCheckTypes.More)
                                    {
                                        if (rectangle.GetArea() < operation.MinimumArea) continue;
                                    }
                                    else
                                    {
                                        if (rectangle.GetArea() > operation.MinimumArea) continue;
                                    }
                                    
                                }

                                CvInvoke.DrawContours(target, contours, i, new MCvScalar(255), -1);
                            }
                        }
                    }
                }

                LayerMat = mat;
            }
        }

        public void Mask(OperationMask operation)
        {
            using (var mat = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(mat);
                if(operation.Mask.Size != target.Size) return;
                CvInvoke.BitwiseAnd(target, operation.Mask, target);
                LayerMat = mat;
            }
        }

        /*public void MutatePixelDimming(Matrix<byte> evenPattern = null, Matrix<byte> oddPattern = null, ushort borderSize = 5)
        {
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            if (ReferenceEquals(evenPattern, null))
            {
                evenPattern = new Matrix<byte>(2, 2)
                {
                    [0, 0] = 127,
                    [0, 1] = 255,
                    [1, 0] = 255,
                    [1, 1] = 127,
                };

                if (ReferenceEquals(oddPattern, null))
                {
                    oddPattern = new Matrix<byte>(2, 2)
                    {
                        [0, 0] = 255,
                        [0, 1] = 127,
                        [1, 0] = 127,
                        [1, 1] = 255,
                    };
                }
            }

            using (Mat dst = LayerMat)
            {
                using (Mat erode = new Mat())
                {
                    using (Mat diff = new Mat())
                    {
                        using (Mat mask = dst.CloneBlank())
                        {
                            CvInvoke.Erode(dst, erode, kernel, anchor, borderSize, BorderType.Reflect101, default);
                            CvInvoke.Subtract(dst, erode, diff);

                            if (Index % 2 == 0)
                            {
                                CvInvoke.Repeat(evenPattern, dst.Rows / evenPattern.Rows + 1, dst.Cols / evenPattern.Cols + 1, mask);
                            }
                            else
                            {
                                CvInvoke.Repeat(oddPattern, dst.Rows / oddPattern.Rows + 1, dst.Cols / oddPattern.Cols + 1, mask);
                            }

                            using (var maskReshape = new Mat(mask, new Rectangle(0, 0, dst.Width, dst.Height)))
                            {
                                CvInvoke.BitwiseAnd(erode, maskReshape, dst);
                            }

                            CvInvoke.Add(dst, diff, dst);
                            LayerMat = dst;
                        }
                    }
                }
            }
        }*/

        public void PixelDimming(OperationPixelDimming operation, Mat patternMask, Mat alternatePatternMask = null)
        {
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            
            if (ReferenceEquals(alternatePatternMask, null))
            {
                alternatePatternMask = patternMask;
            }

            using (Mat dst = LayerMat)
            using (Mat erode = new Mat())
            using (Mat diff = new Mat())
            {
                Mat target = operation.GetRoiOrDefault(dst);

                CvInvoke.Erode(target, erode, kernel, anchor, (int) operation.WallThickness, BorderType.Reflect101, default);
                CvInvoke.Subtract(target, erode, diff);

                
                if (operation.WallsOnly)
                {
                    CvInvoke.BitwiseAnd(diff, operation.IsNormalPattern(Index) ? patternMask : alternatePatternMask, target);
                    CvInvoke.Add(erode, target, target);
                }
                else
                {
                    CvInvoke.BitwiseAnd(erode, operation.IsNormalPattern(Index) ? patternMask : alternatePatternMask, target);
                    CvInvoke.Add(target, diff, target);
                }
                
                LayerMat = dst;
            }
        }

        public void Infill(OperationInfill operation)
        {
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            uint layerIndex = Index - operation.LayerIndexStart;
            var infillColor = new MCvScalar(operation.InfillBrightness);

            Mat patternMask = null;
            using (Mat dst = LayerMat)
            using (Mat erode = new Mat())
            using (Mat diff = new Mat())
            {
                Mat target = operation.GetRoiOrDefault(dst);

                /*if (operation.InfillType == OperationInfill.InfillAlgorithm.Rhombus)
                {
                    const double rotationAngle = 55;
                    patternMask = target.CloneBlank();
                    int offsetLayerPos = (int)(layerIndex % operation.InfillSpacing);
                    var pivot = new Point( operation.InfillThickness / 2, operation.InfillThickness / 2);

                    Point[] points1 = {
                        new Point(operation.InfillThickness / 4, operation.InfillThickness / 2), // Left
                        new Point(operation.InfillThickness / 2, 0), // Top
                        new Point((int) (operation.InfillThickness / 1.25), operation.InfillThickness / 2), // Right
                        new Point(operation.InfillThickness / 2, operation.InfillThickness) // Bottom
                    };
                    var vec1 = new VectorOfPoint(points1);
                    
                    
                    Point[] points2 = {
                        points1[0].Rotate(rotationAngle, pivot),
                        points1[1].Rotate(rotationAngle, pivot),
                        points1[2].Rotate(rotationAngle, pivot),
                        points1[3].Rotate(rotationAngle, pivot),
                    };
                    var vec2 = new VectorOfPoint(points2);

                    Point[] points3 = {
                        points1[0].Rotate(-rotationAngle, pivot),
                        points1[1].Rotate(-rotationAngle, pivot),
                        points1[2].Rotate(-rotationAngle, pivot),
                        points1[3].Rotate(-rotationAngle, pivot),
                    };
                    var vec3 = new VectorOfPoint(points3);

                    
                    /*int halfPos = (operation.InfillThickness + offsetPos) / 2;

                    Point[] points = {
                        new Point(0+offsetPos, halfPos), // Left
                        new Point(halfPos, 0+offsetPos), // Top
                        new Point(operation.InfillThickness+offsetPos, halfPos), // Right
                        new Point(halfPos, operation.InfillThickness+offsetPos) // Bottom
                    }; */
           /*         for (int y = 0; y < patternMask.Height; y += operation.InfillSpacing)
                    {
                        for (int x = 0; x < patternMask.Width; x+=operation.InfillSpacing)
                        {
                            CvInvoke.FillPoly(patternMask, vec1, infillColor, LineType.EightConnected, default, new Point(x, y+offsetLayerPos));
                            CvInvoke.FillPoly(patternMask, vec2, infillColor, LineType.EightConnected, default, new Point(x- offsetLayerPos, y+ offsetLayerPos));
                            CvInvoke.FillPoly(patternMask, vec3, infillColor, LineType.EightConnected, default, new Point(x+ offsetLayerPos, y+ offsetLayerPos));
                        }
                    }

                    patternMask.Save("D:\\mask.png");


                }
                else*/ if (operation.InfillType == OperationInfill.InfillAlgorithm.Cubic ||
                           operation.InfillType == OperationInfill.InfillAlgorithm.CubicCenterLink ||
                           operation.InfillType == OperationInfill.InfillAlgorithm.CubicDynamicLink ||
                           operation.InfillType == OperationInfill.InfillAlgorithm.CubicInterlinked)
                {
                    using (var infillPattern = EmguExtensions.InitMat(new Size(operation.InfillSpacing, operation.InfillSpacing)))
                    using (Mat matPattern = dst.CloneBlank())
                    {
                        bool firstPattern = true;
                        uint accumulator = 0;
                        uint step = 0;
                        bool dynamicCenter = false;
                        while (accumulator < layerIndex)
                        {
                            dynamicCenter = !dynamicCenter;
                            firstPattern = true;
                            accumulator += operation.InfillSpacing;

                            if (accumulator >= layerIndex) break;
                            firstPattern = false;
                            accumulator += operation.InfillThickness;
                        }

                        if (firstPattern)
                        {
                            int thickness = operation.InfillThickness / 2;
                            // Top Left
                            CvInvoke.Rectangle(infillPattern,
                                new Rectangle(0, 0, thickness, thickness),
                                infillColor, -1);

                            // Top Right
                            CvInvoke.Rectangle(infillPattern,
                                new Rectangle(infillPattern.Width - thickness, 0, thickness, thickness),
                                infillColor, -1);

                            // Bottom Left
                            CvInvoke.Rectangle(infillPattern,
                                new Rectangle(0, infillPattern.Height - thickness, thickness, thickness),
                                infillColor, -1);

                            // Bottom Right
                            CvInvoke.Rectangle(infillPattern,
                                new Rectangle(infillPattern.Width - thickness, infillPattern.Height - thickness,
                                    thickness, thickness),
                                infillColor, -1);

                            // Center cross
                            int margin = (int)(operation.InfillSpacing - accumulator + layerIndex) - thickness;
                            int marginInv = (int)(accumulator - layerIndex) - thickness;

                            if (operation.InfillType == OperationInfill.InfillAlgorithm.CubicCenterLink ||
                                (operation.InfillType == OperationInfill.InfillAlgorithm.CubicDynamicLink &&
                                 dynamicCenter) ||
                                operation.InfillType == OperationInfill.InfillAlgorithm.CubicInterlinked)
                            {
                                
                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(margin, margin, operation.InfillThickness, operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(marginInv, marginInv, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(margin, marginInv, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(marginInv, margin, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);
                            }


                            if (operation.InfillType == OperationInfill.InfillAlgorithm.CubicInterlinked ||
                                (operation.InfillType == OperationInfill.InfillAlgorithm.CubicDynamicLink && !dynamicCenter))
                            {
                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(margin, -thickness, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(marginInv, -thickness, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(-thickness, margin, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(-thickness, marginInv, operation.InfillThickness,
                                        operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(operation.InfillSpacing - thickness, margin,
                                        operation.InfillThickness, operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(operation.InfillSpacing - thickness, marginInv,
                                        operation.InfillThickness, operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(margin, operation.InfillSpacing - thickness,
                                        operation.InfillThickness, operation.InfillThickness),
                                    infillColor, -1);

                                CvInvoke.Rectangle(infillPattern,
                                    new Rectangle(marginInv, operation.InfillSpacing - thickness,
                                        operation.InfillThickness, operation.InfillThickness),
                                    infillColor, -1);
                            }


                        }
                        else
                        {
                            CvInvoke.Rectangle(infillPattern,
                                new Rectangle(0, 0, operation.InfillSpacing, operation.InfillSpacing),
                                infillColor, operation.InfillThickness);
                        }

                        
                        {
                            CvInvoke.Repeat(infillPattern, target.Rows / infillPattern.Rows + 1,
                                target.Cols / infillPattern.Cols + 1, matPattern);
                            patternMask = new Mat(matPattern, new Rectangle(0, 0, target.Width, target.Height));
                        }
                    }
                }


                CvInvoke.Erode(target, erode, kernel, anchor, operation.WallThickness, BorderType.Reflect101,
                    default);
                CvInvoke.Subtract(target, erode, diff);


                CvInvoke.BitwiseAnd(erode, patternMask, target);
                CvInvoke.Add(target, diff, target);
                patternMask?.Dispose();

                LayerMat = dst;
            }
        }

        public void Morph(OperationMorph operation, int iterations = 1, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (iterations == 0)
                iterations = (int) operation.IterationsStart;

            using (Mat dst = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(dst);
                CvInvoke.MorphologyEx(target, target, operation.MorphOperation, operation.Kernel.Matrix, operation.Kernel.Anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateErode(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.Erode(dst, dst, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateDilate(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.Dilate(dst, dst, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateOpen(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Open, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateClose(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Close, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void MutateGradient(int iterations = 1, IInputArray kernel = null, Point anchor = default, BorderType borderType = BorderType.Default, MCvScalar borderValue = default)
        {
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            if (ReferenceEquals(kernel, null))
            {
                kernel = CvInvoke.GetStructuringElement(ElementShape.Cross, new Size(3, 3), anchor);
            }
            using (Mat dst = LayerMat)
            {
                CvInvoke.MorphologyEx(dst, dst, MorphOp.Gradient, kernel, anchor, iterations, borderType, borderValue);
                LayerMat = dst;
            }
        }

        public void ThresholdPixels(OperationThreshold operation)
        {
            using (Mat dst = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(dst);
                CvInvoke.Threshold(target, target, operation.Threshold, operation.Maximum, operation.Type);
                LayerMat = dst;
            }
        }

        public void Blur(OperationBlur operation)
        {
            Size size = new Size((int) operation.Size, (int) operation.Size);
            Point anchor = operation.Kernel.Anchor;
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            //if (size.IsEmpty) size = new Size(3, 3);
            //if (anchor.IsEmpty) anchor = new Point(-1, -1);
            using (Mat dst = LayerMat)
            {
                Mat target = operation.GetRoiOrDefault(dst);
                switch (operation.BlurOperation)
                {
                    case OperationBlur.BlurAlgorithm.Blur:
                        CvInvoke.Blur(target, target, size, operation.Kernel.Anchor); 
                        break;
                    case OperationBlur.BlurAlgorithm.Pyramid:
                        CvInvoke.PyrDown(target, target);
                        CvInvoke.PyrUp(target, target);
                        break;
                    case OperationBlur.BlurAlgorithm.MedianBlur:
                        CvInvoke.MedianBlur(target, target, (int) operation.Size);
                        break;
                    case OperationBlur.BlurAlgorithm.GaussianBlur:
                        CvInvoke.GaussianBlur(target, target, size, 0);
                        break;
                    case OperationBlur.BlurAlgorithm.Filter2D:
                        CvInvoke.Filter2D(target, target, operation.Kernel.Matrix, anchor);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                LayerMat = dst;
            }
        }

        public void ChangeResolution(OperationChangeResolution operation)
        {
            using (var mat = LayerMat)
            {
                //mat.Transform(1, 1, newResolutionX-mat.Width+roi.Width, newResolutionY-mat.Height - roi.Height/2, new Size((int) newResolutionX, (int) newResolutionY));
                using (var matRoi = new Mat(mat, operation.VolumeBonds))
                {
                    using (var matDst = new Mat(new Size((int)operation.NewResolutionX, (int)operation.NewResolutionY), mat.Depth,
                        mat.NumberOfChannels))
                    {
                        using (var matDstRoi = new Mat(matDst,
                            new Rectangle((int) (operation.NewResolutionX / 2 - operation.VolumeBonds.Width / 2),
                                (int)operation.NewResolutionY / 2 - operation.VolumeBonds.Height / 2, 
                                operation.VolumeBonds.Width, operation.VolumeBonds.Height)))
                        {
                            matRoi.CopyTo(matDstRoi);
                            LayerMat = matDst;
                        }
                    }
                }
            }
        }

        public void Pattern(OperationPattern operation)
        {
            using (var layer = LayerMat)
            using (var layerRoi = new Mat(layer, operation.ROI))
            using (var dstLayer = layer.CloneBlank())
            {
                for (ushort col = 0; col < operation.Cols; col++)
                for (ushort row = 0; row < operation.Rows; row++)
                {
                    using (var dstRoi = new Mat(dstLayer, operation.GetRoi(col, row)))
                    {
                        layerRoi.CopyTo(dstRoi);
                    }
                }

                LayerMat = dstLayer;
            }
        }
        public Layer Clone()
        {
            return new Layer(_index, CompressedBytes.ToArray(), ParentLayerManager)
            {
                PositionZ = _positionZ,
                ExposureTime = _exposureTime,
                LiftHeight = _liftHeight,
                LiftSpeed = _liftSpeed,
                RetractSpeed = _retractSpeed,
                LayerOffTime = _layerOffTime,
                LightPWM = _lightPwm,
                BoundingRectangle = _boundingRectangle,
                NonZeroPixelCount = _nonZeroPixelCount,
                IsModified = _isModified,
            };
        }

        #endregion
    }
}
