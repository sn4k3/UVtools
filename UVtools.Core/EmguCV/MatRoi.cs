/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


using Emgu.CV;
using System;
using System.Drawing;
using UVtools.Core.Extensions;

namespace UVtools.Core.EmguCV;

/// <summary>
/// A disposable Mat with associated ROI Mat
/// </summary>
public class MatRoi : IDisposable
{
    #region Properties
    public Mat SourceMat { get; }
    public Rectangle Roi { get; }
    public Mat RoiMat { get; }

    public Point RoiLocation => Roi.Location;
    public Size RoiSize => Roi.Size;

    #endregion

    #region Constructor

    public MatRoi(Mat sourceMat, Rectangle roi)
    {
        SourceMat = sourceMat;
        Roi = roi;
        RoiMat = sourceMat.Roi(roi);
    }

    #endregion

    public MatRoi Clone()
    {
        return new MatRoi(SourceMat.Clone(), Roi);
    }

    public void Dispose()
    {
        SourceMat.Dispose();
        RoiMat.Dispose();
    }
}