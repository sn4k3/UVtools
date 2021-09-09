/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;

namespace UVtools.Core.EmguCV
{
    /// <summary>
    /// A contour cache for OpenCV
    /// </summary>
    public class EmguContour : IReadOnlyCollection<Point>, IDisposable
    {
        #region Constants

        public const byte HierarchyNextSameLevel = 0;
        public const byte HierarchyPreviousSameLevel = 1;
        public const byte HierarchyFirstChild = 2;
        public const byte HierarchyParent = 3;

        #endregion

        #region Members

        private VectorOfPoint _points;
        private Rectangle? _bounds;
        private RotatedRect? _boundsBestFit;
        private CircleF? _minEnclosingCircle;
        private bool? _isConvex;
        private double _area = double.NaN;
        private double _perimeter = double.NaN;
        private Moments _moments;
        private Point? _centroid;

        #endregion

        #region Properties
        public int XMin => Bounds.X;

        public int YMin => Bounds.Y;

        public int XMax => Bounds.Right;

        public int YMax => Bounds.Bottom;

        public Rectangle Bounds => _bounds ??= CvInvoke.BoundingRectangle(_points);

        public RotatedRect BoundsBestFit => _boundsBestFit ??= CvInvoke.MinAreaRect(_points);

        public CircleF MinEnclosingCircle => _minEnclosingCircle ??= CvInvoke.MinEnclosingCircle(_points);

        public bool IsConvex => _isConvex ??= CvInvoke.IsContourConvex(_points);

        /// <summary>
        /// Gets the area of the contour
        /// </summary>
        public double Area
        {
            get
            {
                if (double.IsNaN(_area))
                {
                    _area = CvInvoke.ContourArea(_points);
                }
                
                return _area;
            }
        }

        /// <summary>
        /// Gets the perimeter of the contours
        /// </summary>
        public double Perimeter
        {
            get
            {
                if (double.IsNaN(_perimeter))
                {
                    _perimeter = CvInvoke.ArcLength(_points, true);
                }
                return _perimeter;
            }
        }

        public Moments Moments => _moments ??= CvInvoke.Moments(_points);

        /// <summary>
        /// Gets the centroid of the contour
        /// </summary>
        public Point Centroid => _centroid ??= Moments.M00 == 0 ? new Point(-1,-1) : 
            new Point(
            (int)Math.Round(_moments.M10 / _moments.M00),
            (int)Math.Round(_moments.M01 / _moments.M00));

        /// <summary>
        /// Gets or sets the contour <see cref="Point"/>
        /// </summary>
        public VectorOfPoint Points
        {
            get => _points;
            set
            {
                Dispose();
                _points = value ?? throw new ArgumentNullException(nameof(Points));
            }
        }


        /// <summary>
        /// Gets if this contour have any point
        /// </summary>
        public bool IsEmpty => _points.Size == 0;
        #endregion

        #region Constructor
        public EmguContour(VectorOfPoint points) : this(points.ToArray())
        { }

        public EmguContour(Point[] points)
        {
            Points = new VectorOfPoint(points);
        }
        #endregion

        #region Methods

        /// <summary>
        /// Checks if a given <see cref="Point"/> is inside the contour rectangle bounds
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsInsideBounds(Point point) => Bounds.Contains(point);

        /// <summary>
        /// Gets if a given <see cref="Point"/> is inside the contour
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsInside(Point point)
        {
            if (!IsInsideBounds(point)) return false;
            return CvInvoke.PointPolygonTest(_points, point, false) >= 0;
        }

        public double MeasureDist(Point point)
        {
            if (!IsInsideBounds(point)) return -1;
            return CvInvoke.PointPolygonTest(_points, point, true);
        }

        public IOutputArray ContourApproximation(double epsilon = 0.1)
        {
            var mat = new Mat();
            CvInvoke.ApproxPolyDP(_points, mat, epsilon*Perimeter, true);
            return mat;
        }

        /*
        /// <summary>
        /// Calculate the X/Y min/max boundary
        /// </summary>
        private void CalculateMinMax()
        {
            Bounds = Rectangle.Empty;

            if (_contourPoints.Length == 0)
            {
                _xMin = -1;
                _yMin = -1;
                _xMax = -1;
                _yMax = -1;
                return;
            }

            _xMin = int.MaxValue;
            _yMin = int.MaxValue;
            _xMax = int.MinValue;
            _yMax = int.MinValue;

            for (int i = 0; i < _contourPoints.Length; i++)
            {
                _xMin = Math.Min(_xMin, _contourPoints[i].X);
                _yMin = Math.Min(_yMin, _contourPoints[i].Y);

                _xMax = Math.Max(_xMax, _contourPoints[i].X);
                _yMax = Math.Max(_yMax, _contourPoints[i].Y);
            }

            Bounds = new Rectangle(_xMin, _yMin, _xMax - _xMin, _yMax - _yMin);
        }
        */

        public void FitCircle(Mat src, MCvScalar color, int thickness = 1, LineType lineType = LineType.EightConnected, int shift = 0)
        {
            CvInvoke.Circle(src, 
                MinEnclosingCircle.Center.ToPoint(), 
                (int) Math.Round(MinEnclosingCircle.Radius), 
                color, 
                thickness, 
                lineType, 
                shift);
        }

        /*public void FitEllipse(Mat src, MCvScalar color, int thickness = 1, LineType lineType = LineType.EightConnected, int shift = 0)
        {
            var ellipse = CvInvoke.FitEllipse(_points);
            CvInvoke.Ellipse(src, ellipse.Center.ToPoint(), ellipse.Size.ToSize(), ellipse.Angle, 0, 0);
        }*/
        #endregion

        #region Static methods
        public static Point GetCentroid(VectorOfPoint points)
        {
            if (points is null || points.Length == 0) return new Point(-1, -1);
            using var moments = CvInvoke.Moments(points);
            return moments.M00 == 0 ? new Point(-1, -1) :
                new Point(
                    (int)Math.Round(moments.M10 / moments.M00),
                    (int)Math.Round(moments.M01 / moments.M00));
        }
        #endregion

        #region Implementations

        public IEnumerator<Point> GetEnumerator()
        {
            return (IEnumerator<Point>) _points.ToArray().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int Count => _points.Size;

        public Point this[int index] => _points[index];
        public Point this[uint index] => _points[(int) index];
        public Point this[long index] => _points[(int) index];
        public Point this[ulong index] => _points[(int) index];

        public void Dispose()
        {
            _points?.Dispose();
            _moments?.Dispose();
        }
        #endregion
    }
}
