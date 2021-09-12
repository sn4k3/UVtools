/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Util;

namespace UVtools.Core.EmguCV
{
    public static class EmguContours
    {
        /// <summary>
        /// Gets contours inside a point
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="hierarchy"></param>
        /// <param name="location"></param>
        /// <param name="includeLimitingArea">If true it will include all limiting area, otherwise only outer contour will be returned</param>
        /// <returns></returns>
        public static VectorOfVectorOfPoint GetContoursInside(VectorOfVectorOfPoint contours, int[,] hierarchy, Point location, bool includeLimitingArea = true)
        {
            var vector = new VectorOfVectorOfPoint();
            var vectorSize = contours.Size;
            for (var i = vectorSize - 1; i >= 0; i--)
            {
                if (CvInvoke.PointPolygonTest(contours[i], location, false) < 0) continue;
                vector.Push(contours[i]);
                if (!includeLimitingArea) break;
                for (int n = i + 1; n < vectorSize; n++)
                {
                    if (hierarchy[n, EmguContour.HierarchyParent] != i) continue;
                    vector.Push(contours[n]);
                }
                break;
            }

            return vector;
        }

        /// <summary>
        /// Gets a contour given a location.
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="location"></param>
        /// <param name="index">Contour index, -1 if not exists</param>
        /// <returns>null if not exists</returns>
        public static VectorOfPoint GetContourInside(VectorOfVectorOfPoint contours, Point location, out int index)
        {
            index = -1;
            var vectorSize = contours.Size;
            for (int i = vectorSize - 1; i >= 0; i--)
            {
                if (CvInvoke.PointPolygonTest(contours[i], location, false) < 0) continue;
                index = i;
                return contours[i];
            }

            return null;
        }

        /// <summary>
        /// Gets only the outer most external contours
        /// Only compatible with Tree type of contour detection
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        public static VectorOfVectorOfPoint GetExternalContours(VectorOfVectorOfPoint contours, int[,] hierarchy)
        {
            var result = new VectorOfVectorOfPoint();
            var vectorSize = contours.Size;
            for (var i = 0; i < vectorSize; i++)
            {
                if (hierarchy[i, EmguContour.HierarchyParent] != -1) continue;
                result.Push(contours[i]);
            }

            return result;
        }

        /// <summary>
        /// Gets contours inside contours that are black pixels
        /// </summary>
        /// <param name="contours"></param>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        public static VectorOfVectorOfPoint GetNegativeContours(VectorOfVectorOfPoint contours, int[,] hierarchy)
        {
            var result = new VectorOfVectorOfPoint();
            var vectorSize = contours.Size;
            for (var i = 0; i < vectorSize; i++)
            {
                if (hierarchy[i, EmguContour.HierarchyParent] == -1) continue;
                result.Push(contours[i]);
            }

            return result;
        }

        /// <summary>
        /// Gets contours inside contours that are black pixels and group them by areas
        /// Only compatible with Tree type of contour detection
        /// </summary>
        /// <returns></returns>
        public static List<VectorOfVectorOfPoint> GetNegativeContoursInGroups(VectorOfVectorOfPoint contours, int[,] hierarchy)
        {
            var result = new List<VectorOfVectorOfPoint>();
            var vectorSize = contours.Size;
            var processedContours = new bool[vectorSize];
            for (int i = 0; i < vectorSize; i++)
            {
                if (processedContours[i]) continue;
                processedContours[i] = true;
                if (hierarchy[i, EmguContour.HierarchyParent] == -1) continue;
                result.Add(new VectorOfVectorOfPoint(contours[i]));
                for (int n = i + 1; n < vectorSize; n++)
                {
                    if (processedContours[n] || hierarchy[n, EmguContour.HierarchyParent] != i) continue;
                    processedContours[n] = true;
                    result[^1].Push(contours[n]);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets contours real area for a limited area
        /// </summary>
        /// <param name="contours"></param>
        /// <returns></returns>
        public static double GetContoursArea(VectorOfVectorOfPoint contours)
        {
            var vectorSize = contours.Size;
            if (vectorSize == 0) return 0;

            double result = CvInvoke.ContourArea(contours[0]);
            for (var i = 1; i < vectorSize; i++)
            {
                result -= CvInvoke.ContourArea(contours[i]);
            }
            return result;
        }

        /// <summary>
        /// Gets contours real area for a group of contours
        /// </summary>
        /// <param name="contours">Grouped contours</param>
        /// <param name="useParallel">True to run in parallel</param>
        /// <returns>Array with same size with contours area</returns>
        public static double[] GetContoursArea(List<VectorOfVectorOfPoint> contours, bool useParallel = false)
        {
            var result = new double[contours.Count];

            if (useParallel)
            {
                Parallel.For(0, contours.Count, CoreSettings.ParallelOptions, i =>
                {
                    result[i] = GetContoursArea(contours[i]);
                });
            }
            else
            {
                for (var i = 0; i < contours.Count; i++)
                {
                    result[i] = GetContoursArea(contours[i]);
                }
            }
            
            return result;
        }
    }
}
