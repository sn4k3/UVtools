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
using System.Linq;
using System.Timers;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using Stream = System.IO.Stream;

namespace UVtools.Core
{
    /// <summary>
    /// Represent a Layer
    /// </summary>
    public class Layer : BindableBase, IEquatable<Layer>, IEquatable<uint>
    {
        #region Members
        private byte[] _compressedBytes;
        private uint _nonZeroPixelCount;
        private Rectangle _boundingRectangle = Rectangle.Empty;
        private bool _isModified;
        private uint _index;
        private float _positionZ;
        private float _exposureTime;
        private float _lightOffDelay = FileFormat.DefaultLightOffDelay;
        private float _liftHeight = FileFormat.DefaultLiftHeight;
        private float _liftSpeed = FileFormat.DefaultLiftSpeed;
        private float _retractSpeed = FileFormat.DefaultRetractSpeed;
        private byte _lightPwm = FileFormat.DefaultLightPWM;
        private float _materialMilliliters;
        #endregion

        #region Properties

        public object Mutex = new();

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
            internal set
            {
                if(!RaiseAndSetIfChanged(ref _nonZeroPixelCount, value)) return;
                RaisePropertyChanged(nameof(ExposureMillimeters));
                MaterialMilliliters = 0; // Recalculate
            }
        }

        public float ExposureMillimeters
        {
            get
            {
                if (SlicerFile is null) return 0;
                return (float) Math.Round(SlicerFile.PixelSizeMax * _nonZeroPixelCount, 2);
            }
        }

        /// <summary>
        /// Gets the bounding rectangle for the image area
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get => _boundingRectangle;
            internal set
            {
                RaiseAndSetIfChanged(ref _boundingRectangle, value);
                RaisePropertyChanged(nameof(BoundingRectangleMillimeters));
            }
        }

        /// <summary>
        /// Gets the bounding rectangle for the image area in millimeters
        /// </summary>
        public RectangleF BoundingRectangleMillimeters
        {
            get
            {
                if (SlicerFile is null) return RectangleF.Empty;
                var pixelSize = SlicerFile.PixelSize;
                return new RectangleF(
                    (float) Math.Round(_boundingRectangle.X * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Y * pixelSize.Height, 2),
                    (float)Math.Round(_boundingRectangle.Width * pixelSize.Width, 2),
                    (float)Math.Round(_boundingRectangle.Height * pixelSize.Height, 2));
            }
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
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
                RaiseAndSetIfChanged(ref _exposureTime, value);
            }
        }

        /// <summary>
        /// Gets or sets the layer off time in seconds
        /// </summary>
        public float LightOffDelay
        {
            get => _lightOffDelay;
            set
            {
                if (value < 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
                RaiseAndSetIfChanged(ref _lightOffDelay, value);
            }
        }

        /// <summary>
        /// Gets or sets the lift height in mm
        /// </summary>
        public float LiftHeight
        {
            get => _liftHeight;
            set
            {
                if (value < 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
                RaiseAndSetIfChanged(ref _liftHeight, value);
            }
        }

        /// <summary>
        /// Gets or sets the speed in mm/min
        /// </summary>
        public float LiftSpeed
        {
            get => _liftSpeed;
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
                RaiseAndSetIfChanged(ref _liftSpeed, value);
            }
        }

        /// <summary>
        /// Gets the speed in mm/min for the retracts
        /// </summary>
        public float RetractSpeed
        {
            get => _retractSpeed;
            set
            {
                if (value <= 0) value = SlicerFile.RetractSpeed;
                RaiseAndSetIfChanged(ref _retractSpeed, value);
            }
        }

        /// <summary>
        /// Gets or sets the pwm value from 0 to 255
        /// </summary>
        public byte LightPWM
        {
            get => _lightPwm;
            set
            {
                if (value <= 0) value = SlicerFile.GetInitialLayerValueOrNormal(Index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
                RaiseAndSetIfChanged(ref _lightPwm, value);
            }
        }

        /// <summary>
        /// Gets or sets the layer position on Z in mm
        /// </summary>
        public float PositionZ
        {
            get => _positionZ;
            set
            {
                if(!RaiseAndSetIfChanged(ref _positionZ, value)) return;
                RaisePropertyChanged(nameof(LayerHeight));
            }
        }

        /// <summary>
        /// Gets the layer height in millimeters of this layer
        /// </summary>
        public float LayerHeight
        {
            get
            {
                if (_index == 0) return _positionZ;
                Layer previousLayer = this;

                while ((previousLayer = previousLayer.PreviousLayer()) is not null) // This cycle returns the correct layer height if two or more layers have the same position z
                {
                    var layerHeight = (float)Math.Round(_positionZ - previousLayer.PositionZ, 2);
                    //Debug.WriteLine($"Layer {_index}-{previousLayer.Index}: {_positionZ} - {previousLayer.PositionZ}: {layerHeight}");
                    if (layerHeight == 0f) continue;
                    if (layerHeight < 0f) break;
                    return layerHeight;
                }

                return ParentLayerManager.SlicerFile.LayerHeight;
            }
        }

        /// <summary>
        /// Gets the computed material milliliters spent on this layer
        /// </summary>
        public float MaterialMilliliters
        {
            get => _materialMilliliters;
            private set
            {
                if (ParentLayerManager?.SlicerFile is null) return;
                if (value <= 0)
                {
                    value = (float) Math.Round(ParentLayerManager.SlicerFile.PixelArea * ParentLayerManager.SlicerFile.LayerHeight * NonZeroPixelCount / 1000, 4);
                }

                if(!RaiseAndSetIfChanged(ref _materialMilliliters, value)) return;
                SlicerFile.MaterialMilliliters = 0; // Recalculate global
                RaisePropertyChanged(nameof(MaterialMillilitersPercent));
                //ParentLayerManager.MaterialMillilitersTimer.Stop();
                //if(!ParentLayerManager.MaterialMillilitersTimer.Enabled)
                //    ParentLayerManager.MaterialMillilitersTimer.Start();
            }
        }

        /// <summary>
        /// Gets the computed material milliliters percentage compared to the rest of the model
        /// </summary>
        public float MaterialMillilitersPercent => _materialMilliliters * 100 / SlicerFile.MaterialMilliliters;

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
                if (ParentLayerManager is not null)
                    ParentLayerManager.BoundingRectangle = Rectangle.Empty;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a new image instance
        /// </summary>
        public Mat LayerMat
        {
            get
            {
                Mat mat = new();
                CvInvoke.Imdecode(_compressedBytes, ImreadModes.Grayscale, mat);
                return mat;
            }
            set
            {
                using var vector = new VectorOfByte();
                CvInvoke.Imencode(".png", value, vector);
                CompressedBytes = vector.ToArray();
                GetBoundingRectangle(value, true);
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
                var mat = LayerMat;
                CvInvoke.CvtColor(mat, mat, ColorConversion.Gray2Bgr);
                return mat;
            }
        }

        /// <summary>
        /// Gets a computed layer filename, padding zeros are equal to layer count digits
        /// </summary>
        public string Filename => FormatFileName("layer");

        /// <summary>
        /// Gets if layer image has been modified
        /// </summary>
        public bool IsModified
        {
            get => _isModified;
            set => RaiseAndSetIfChanged(ref _isModified, value);
        }

        /// <summary>
        /// Gets if this layer have same value parameters as global settings
        /// </summary>
        public bool HaveGlobalParameters
        {
            get
            {
                if (SlicerFile is null) return false; // Cant verify
                if (IsBottomLayer)
                {
                    if (ExposureTime != SlicerFile.BottomExposureTime ||
                        LiftHeight != SlicerFile.BottomLiftHeight ||
                        LiftSpeed != SlicerFile.BottomLiftSpeed ||
                        LightOffDelay != SlicerFile.BottomLightOffDelay ||
                        LightPWM != SlicerFile.BottomLightPWM 
                        ) return false;
                }
                else
                {
                    if (ExposureTime != SlicerFile.ExposureTime ||
                        LiftHeight != SlicerFile.LiftHeight ||
                        LiftSpeed != SlicerFile.LiftSpeed ||
                        LightOffDelay != SlicerFile.LightOffDelay ||
                        LightPWM != SlicerFile.LightPWM
                    ) return false;
                }

                if (RetractSpeed != SlicerFile.RetractSpeed) return false;

                return true;
            }
        }

        #endregion

        #region Constructor

        public Layer(uint index, LayerManager parentLayerManager)
        {
            ParentLayerManager = parentLayerManager;
            _index = index;

            if (parentLayerManager is null) return;
            _positionZ = SlicerFile.GetHeightFromLayer(index);
            _exposureTime = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
            _liftHeight = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftHeight, SlicerFile.LiftHeight);
            _liftSpeed = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
            _lightOffDelay = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLightOffDelay, SlicerFile.LightOffDelay);
            _retractSpeed = SlicerFile.RetractSpeed;
            _lightPwm = SlicerFile.GetInitialLayerValueOrNormal(index, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);
        }

        public Layer(uint index, byte[] compressedBytes, LayerManager parentLayerManager) : this(index, parentLayerManager)
        {
            CompressedBytes = compressedBytes;
            _isModified = false;
            /*if (compressedBytes.Length > 0)
            {
                GetBoundingRectangle();
            }*/
        }

        public Layer(uint index, byte[] compressedBytes, FileFormat slicerFile) : this(index, compressedBytes, slicerFile.LayerManager)
        {}

        public Layer(uint index, Mat layerMat, LayerManager parentLayerManager) : this(index, parentLayerManager)
        {
            LayerMat = layerMat;
            _isModified = false;
        }

        public Layer(uint index, Mat layerMat, FileFormat slicerFile) : this(index, layerMat, slicerFile.LayerManager) { }

        public Layer(uint index, Stream stream, LayerManager parentLayerManager) : this(index, stream.ToArray(), parentLayerManager) { }
        public Layer(uint index, Stream stream, FileFormat slicerFile) : this(index, stream.ToArray(), slicerFile.LayerManager) { }
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
            return $"{nameof(Index)}: {Index}, {nameof(Filename)}: {Filename}, {nameof(NonZeroPixelCount)}: {NonZeroPixelCount}, {nameof(BoundingRectangle)}: {BoundingRectangle}, {nameof(IsBottomLayer)}: {IsBottomLayer}, {nameof(IsNormalLayer)}: {IsNormalLayer}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(PositionZ)}: {PositionZ}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(LightPWM)}: {LightPWM}, {nameof(IsModified)}: {IsModified}, {nameof(HaveGlobalParameters)}: {HaveGlobalParameters}";
        }
        #endregion

        #region Methods

        public string FormatFileName(string name)
        {
            return $"{name}{Index.ToString().PadLeft(ParentLayerManager.LayerDigits, '0')}.png";
        }

        public Rectangle GetBoundingRectangle(Mat mat = null, bool reCalculate = false)
        {
            if (_nonZeroPixelCount > 0 && !reCalculate)
            {
                return BoundingRectangle;
            }
            bool needDispose = false;
            if (mat is null)
            {
                if (_compressedBytes is null || _compressedBytes.Length == 0) return Rectangle.Empty;
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
            if (ParentLayerManager is null || _index == 0)
                return null;

            return ParentLayerManager[_index - 1];
        }

        public Layer NextLayer()
        {
            if (ParentLayerManager is null || _index >= ParentLayerManager.Count - 1)
                return null;

            return ParentLayerManager[_index + 1];
        }

        public bool SetValueFromPrintParameterModifier(FileFormat.PrintParameterModifier modifier, decimal value)
        {
            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.ExposureSeconds))
            {
                ExposureTime = (float)value;
                return true;
            }

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.LightOffDelay))
            {
                LightOffDelay = (float)value;
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

        public Layer Clone()
        {
            return new(_index, CompressedBytes.ToArray(), ParentLayerManager)
            {
                PositionZ = _positionZ,
                ExposureTime = _exposureTime,
                LiftHeight = _liftHeight,
                LiftSpeed = _liftSpeed,
                RetractSpeed = _retractSpeed,
                LightOffDelay = _lightOffDelay,
                LightPWM = _lightPwm,
                BoundingRectangle = _boundingRectangle,
                NonZeroPixelCount = _nonZeroPixelCount,
                IsModified = _isModified,
                MaterialMilliliters = _materialMilliliters
            };
        }


        #endregion
    }
}
