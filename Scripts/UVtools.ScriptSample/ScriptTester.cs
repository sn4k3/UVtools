/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

/// <summary>
/// Change layer properties to random values
/// </summary>
public class ScriptChangeLayesrPropertiesSample : ScriptGlobals
{
    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Change layer properties";
        Script.Description = "Change layer properties to random values :D";
        Script.Author = "Tiago Conceição";
        Script.Version = new Version(0, 1);
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        return null;
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        var dict = new Dictionary<uint, List<(Point[] points, Rectangle rect)>>();
        Parallel.For(Operation.LayerIndexStart, Operation.LayerIndexEnd + 1, CoreSettings.GetParallelOptions(Progress), layerIndex =>
        {
            Progress.PauseIfRequested();
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var contours = mat.FindContours(out var hierarchy, RetrType.Tree);

            var hollowContours = new List<(Point[] points, Rectangle rect)>();
            dict.Add((uint)layerIndex, hollowContours);

            for (int i = 0; i < contours.Size; i++)
            {
                // Only hollow areas inside model
                if (hierarchy[i, EmguContour.HierarchyParent] == -1) continue;

                hollowContours.Add((contours[i].ToArray(), CvInvoke.BoundingRectangle(contours[i])));
            }
        });

        foreach (var (layerIndex, contours) in dict)
        {
            if (!dict.TryGetValue(layerIndex + 1, out var nextContours)) continue; // No next layer with results

            foreach (var tuple in contours)
            {
                Mat? thisContourMat = null;
                foreach (var nextTuple in nextContours)
                {
                    if (!tuple.rect.IntersectsWith(nextTuple.rect)) continue;
                    if (thisContourMat is null)
                    {
                        thisContourMat = EmguExtensions.InitMat(SlicerFile.Resolution);
                        using var vec = new VectorOfPoint(tuple.points);
                        CvInvoke.DrawContours(thisContourMat, vec, -1, EmguExtensions.WhiteColor, -1);
                    }

                    using var nextContourMat = thisContourMat.NewBlank();
                    using var vecNext = new VectorOfPoint(nextTuple.points);
                    CvInvoke.DrawContours(nextContourMat, vecNext, -1, EmguExtensions.WhiteColor, -1);

                    CvInvoke.BitwiseAnd(thisContourMat, nextContourMat, nextContourMat);
                    if (CvInvoke.CountNonZero(nextContourMat) == 0) continue; // Does not intersect!

                    // Intersecting here!
                }

                thisContourMat?.Dispose();
            }
        }


        // return true if not cancelled by user
        return !Progress.Token.IsCancellationRequested;
    }
}