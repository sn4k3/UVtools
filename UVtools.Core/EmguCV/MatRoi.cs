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
/// A disposable Mat with the associated ROI Mat
/// </summary>
public class MatRoi : IDisposable, IEquatable<MatRoi>
{
    #region Members
    private bool _disposed;
    #endregion

    #region Properties
    public Mat SourceMat { get; }
    public Rectangle Roi { get; }
    public Mat RoiMat { get; }

    /// <summary>
    /// True to dispose the <see cref="SourceMat"/> on <see cref="Dispose()"/>
    /// </summary>
    public bool DisposeSourceMat { get; set; }
    
    /// <summary>
    /// Gets the <see cref="Roi"/> location
    /// </summary>
    public Point RoiLocation => Roi.Location;

    /// <summary>
    /// Gets the <see cref="Roi"/> size
    /// </summary>
    public Size RoiSize => Roi.Size;

    /// <summary>
    /// Gets if the <see cref="SourceMat"/> is the same size of the <see cref="RoiSize"/>
    /// </summary>
    public bool IsSourceSameSizeOfRoi => SourceMat.Size == RoiSize;

    #endregion

    #region Constructor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceMat">The source mat to apply the <paramref name="roi"/>.</param>
    /// <param name="roi">The roi rectangle.</param>
    /// <param name="emptyRoiBehaviour">Sets the behavior for an empty roi event.</param>
    /// <param name="disposeSourceMat">True if you want to dispose the <see cref="SourceMat"/> upon the <see cref="Dispose()"/> call.<br/>
    /// Use true when creating a <see cref="Mat"/> directly on this constructor or not handling the <see cref="SourceMat"/> disposal, otherwise false.</param>
    public MatRoi(Mat sourceMat, Rectangle roi, EmptyRoiBehaviour emptyRoiBehaviour = EmptyRoiBehaviour.Continue, bool disposeSourceMat = false)
    {
        SourceMat = sourceMat;
        Roi = roi;
        RoiMat = sourceMat.Roi(roi, emptyRoiBehaviour);

        DisposeSourceMat = disposeSourceMat;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceMat">The source mat to apply the <paramref name="roi"/>.</param>
    /// <param name="roi">The roi rectangle.</param>
    /// <param name="disposeSourceMat">True if you want to dispose the <see cref="SourceMat"/> upon the <see cref="Dispose()"/> call.<br/>
    /// Use true when creating a <see cref="Mat"/> directly on this constructor or not handling the <see cref="SourceMat"/> disposal, otherwise false.</param>
    public MatRoi(Mat sourceMat, Rectangle roi, bool disposeSourceMat) : this(sourceMat, roi, EmptyRoiBehaviour.Continue, disposeSourceMat)
    {
    }

    public void Deconstruct(out Mat sourceMat, out Mat roiMat, out Rectangle roi)
    {
        sourceMat = SourceMat;
        roiMat = RoiMat;
        roi = Roi;
    }

    public void Deconstruct(out Mat sourceMat, out Mat roiMat)
    {
        sourceMat = SourceMat;
        roiMat = RoiMat;
    }

    public void Deconstruct(out Mat roiMat, out Rectangle roi)
    {
        roiMat = RoiMat;
        roi = Roi;
    }

    #endregion

    #region Clone
    public MatRoi Clone()
    {
        return new MatRoi(SourceMat.Clone(), Roi, true);
    }
    #endregion

    #region Equality
    public bool Equals(MatRoi? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Roi.Equals(other.Roi) && SourceMat.Equals(other.SourceMat);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((MatRoi)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SourceMat, Roi);
    }

    public static bool operator ==(MatRoi? left, MatRoi? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MatRoi? left, MatRoi? right)
    {
        return !Equals(left, right);
    }
    #endregion

    #region Dispose
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            if (DisposeSourceMat) SourceMat.Dispose();
            RoiMat.Dispose();
        }

        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion
}