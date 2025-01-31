/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.Core.EmguCV;

/// <summary>
/// Represents a compressed <see cref="Mat"/> that can be compressed and decompressed using multiple <see cref="MatCompressor"/>s.<br/>
/// This allows to have a high count of <see cref="CMat"/>s in memory without using too much memory.
/// </summary>
#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class CMat : IEquatable<CMat>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    #region Properties
    /// <summary>
    /// Gets the compressed bytes that have been compressed with <see cref="Decompressor"/>.
    /// </summary>
    private byte[] _compressedBytes = Array.Empty<byte>();
    private string? _hash;

    /// <summary>
    /// Gets the compressed bytes that have been compressed with <see cref="Decompressor"/>.
    /// </summary>
    public byte[] CompressedBytes
    {
        get => _compressedBytes;
        private set
        {
            _compressedBytes = value;
            _hash = null;
            IsInitialized = true;
            IsCompressed = !IsEmpty;
        }
    }

    /// <summary>
    /// Gets the MD5 hash of the <see cref="CompressedBytes"/>.
    /// </summary>
    public string Hash => _hash ?? CryptExtensions.ComputeSHA1Hash(_compressedBytes);

    /// <summary>
    /// Gets a value indicating whether the <see cref="CompressedBytes"/> have ever been set.
    /// </summary>
    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the <see cref="CompressedBytes"/> are compressed or raw bytes.
    /// </summary>
    public bool IsCompressed { get; private set; }

    /// <summary>
    /// Gets or sets the threshold in bytes to compress the data. Mat's equal to or less than this size will not be compressed.
    /// </summary>
    public int ThresholdToCompress { get; set; } = 64;

    /// <summary>
    /// Gets the cached width of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Gets the cached height of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Gets the cached size of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public Size Size
    {
        get => new(Width, Height);
        private set
        {
            Width = value.Width;
            Height = value.Height;
        }
    }

    /// <summary>
    /// Gets the cached depth of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public DepthType Depth { get; private set; } = DepthType.Cv8U;

    /// <summary>
    /// Gets the cached number of channels of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public int Channels { get; private set; } = 1;

    /// <summary>
    /// Gets the cached ROI of the <see cref="Mat"/> that was compressed.
    /// </summary>
    public Rectangle Roi { get; private set; }

    /// <summary>
    /// Gets or sets the <see cref="MatCompressor"/> that will be used to compress and decompress the <see cref="Mat"/>.
    /// </summary>
    public MatCompressor Compressor { get; set; } = MatCompressorLz4.Instance;

    /// <summary>
    /// Gets the <see cref="MatCompressor"/> that will be used to decompress the <see cref="Mat"/>.
    /// </summary>
    public MatCompressor Decompressor { get; private set; } = MatCompressorLz4.Instance;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CompressedBytes"/> are empty.
    /// </summary>
    public bool IsEmpty => _compressedBytes.Length == 0;

    /// <summary>
    /// Gets the length of the <see cref="CompressedBytes"/>.
    /// </summary>
    public int Length => _compressedBytes.Length;

    /// <summary>
    /// Gets the uncompressed length of the <see cref="Mat"/> aka bitmap size.
    /// </summary>
    public int UncompressedLength => (Roi.Size.IsEmpty ? Width * Height : Roi.Width * Roi.Height) * Channels;

    /// <summary>
    /// Gets the compression ratio of the <see cref="CompressedBytes"/> to the <see cref="UncompressedLength"/>.
    /// </summary>
    public float CompressionRatio
    {
        get
        {
            var uncompressedLength = UncompressedLength;
            if (uncompressedLength == 0 || Length == uncompressedLength) return 1;
            if (Length == 0) return uncompressedLength;
            return MathF.Round((float)uncompressedLength / Length, 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Gets the compression percentage of the <see cref="CompressedBytes"/> to the <see cref="UncompressedLength"/>.
    /// </summary>
    public float CompressionPercentage
    {
        get
        {
            var uncompressedLength = UncompressedLength;
            if (uncompressedLength == 0 || Length == uncompressedLength) return 0;
            if (Length == 0) return 100f;
            return MathF.Round(100 - (Length * 100f / uncompressedLength), 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Gets the compression efficiency percentage of the <see cref="CompressedBytes"/> to the <see cref="UncompressedLength"/>.
    /// </summary>
    public float CompressionEfficiency
    {
        get
        {
            var uncompressedLength = UncompressedLength;
            if (uncompressedLength == 0) return 0;
            if (Length == 0) return uncompressedLength;
            return MathF.Round(uncompressedLength * 100f / Length, 2, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Gets the number of bytes saved by compressing the <see cref="Mat"/>.
    /// </summary>
    public int SavedBytes => UncompressedLength - Length;

    /// <summary>
    /// Gets or sets the <see cref="Mat"/> that will be compressed and decompressed.<br/>
    /// Every time the <see cref="Mat"/> is accessed, it will be de/compressed.
    /// </summary>
    public Mat Mat
    {
        get => Decompress();
        set => Compress(value);
    }
    #endregion

    #region Constructors

    public CMat(int width = 0, int height = 0, DepthType depth = DepthType.Cv8U, int channels = 1)
    {
        Width = width;
        Height = height;
        Depth = depth;
        Channels = channels;
    }

    public CMat(Size size, DepthType depth = DepthType.Cv8U, int channels = 1) : this(size.Width, size.Height, depth, channels)
    {
    }

    public CMat(MatCompressor compressor, int width = 0, int height = 0, DepthType depth = DepthType.Cv8U, int channels = 1) : this(width, height, depth, channels)
    {
        Compressor = compressor;
        Decompressor = compressor;
    }

    public CMat(MatCompressor compressor, Size size, DepthType depth = DepthType.Cv8U, int channels = 1) : this(compressor, size.Width, size.Height, depth, channels)
    {
    }

    public CMat(Mat mat)
    {
        Compress(mat);
    }

    public CMat(MatRoi matRoi)
    {
        Compress(matRoi);
    }


    public CMat(MatCompressor compressor, Mat mat)
    {
        Compressor = compressor;
        Decompressor = compressor;
        Compress(mat);
    }

    public CMat(MatCompressor compressor, MatRoi matRoi)
    {
        Compressor = compressor;
        Decompressor = compressor;
        Compress(matRoi);
    }
    #endregion

    #region Compress/Decompress
    /// <summary>
    /// Changes the <see cref="Compressor"/> and optionally re-encodes the <see cref="Mat"/> with the new <paramref name="compressor"/> if the <see cref="Decompressor"/> is different from the set <paramref name="compressor"/>.
    /// </summary>
    /// <param name="compressor">New compressor</param>
    /// <param name="reEncodeWithNewCompressor">True to re-encodes the <see cref="Mat"/> with the new <see cref="Compressor"/>, otherwise false.</param>
    /// <param name="argument"></param>
    /// <returns>True if compressor has been changed, otherwise false.</returns>
    public bool ChangeCompressor(MatCompressor compressor, bool reEncodeWithNewCompressor = false, object? argument = null)
    {
        bool willReEncode = reEncodeWithNewCompressor && !IsEmpty && !ReferenceEquals(Decompressor, compressor);
        if (ReferenceEquals(Compressor, compressor) && !willReEncode) return false; // Nothing to change
        Compressor = compressor;

        if (willReEncode)
        {
            using var mat = RawDecompress(argument);
            var lastRoi = Roi;
            Compress(mat);
            Roi = lastRoi;
        }

        return true;
    }

    /// <summary>
    /// Changes the <see cref="Compressor"/> and optionally re-encodes the <see cref="Mat"/> with the new <paramref name="compressor"/> if the <see cref="Decompressor"/> is different from the set <paramref name="compressor"/>.
    /// </summary>
    /// <param name="compressor">New compressor</param>
    /// <param name="reEncodeWithNewCompressor">True to re-encodes the <see cref="Mat"/> with the new <see cref="Compressor"/>, otherwise false.</param>
    /// <param name="argument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>True if compressor has been changed, otherwise false.</returns>
    public Task<bool> ChangeCompressorAsync(MatCompressor compressor, bool reEncodeWithNewCompressor = false, object? argument = null, CancellationToken cancellationToken = default)
    {
        bool willReEncode = reEncodeWithNewCompressor && !IsEmpty && !ReferenceEquals(Decompressor, compressor);
        if (ReferenceEquals(Compressor, compressor) && !willReEncode) return Task.FromResult(false); // Nothing to change
        Compressor = compressor;

        if (!willReEncode) return Task.FromResult(true);

        return Task.Run(() =>
        {
            using var mat = RawDecompress(argument);
            var lastRoi = Roi;
            Compress(mat);
            Roi = lastRoi;
            return true;
        }, cancellationToken);
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> to an empty byte array and sets <see cref="IsCompressed"/> to false.
    /// </summary>
    public void SetEmptyCompressedBytes()
    {
        if (_compressedBytes.Length == 0) return; // Already empty
        _compressedBytes = Array.Empty<byte>();
        _hash = null;
        IsCompressed = false;
        Roi = Rectangle.Empty;
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> to an empty byte array and sets <see cref="IsCompressed"/> to false.
    /// </summary>
    /// <param name="isInitialized">Sets the <see cref="IsInitialized"/> to a known state.</param>
    public void SetEmptyCompressedBytes(bool isInitialized)
    {
        SetEmptyCompressedBytes();
        IsInitialized = isInitialized;
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> to an empty byte array, sets <see cref="IsCompressed"/> to false and extract size, depth and channels from a <see cref="Mat"/>.
    /// </summary>
    /// <param name="src">Source Mat to extract Size, Depth and Channels</param>
    public void SetEmptyCompressedBytes(Mat src)
    {
        SetEmptyCompressedBytes();
        Width = src.Width;
        Height = src.Height;
        Depth = src.Depth;
        Channels = src.NumberOfChannels;
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> to an empty byte array, sets <see cref="IsCompressed"/> to false and extract size, depth and channels from a <see cref="Mat"/>.
    /// </summary>
    /// <param name="src">Source Mat to extract Size, Depth and Channels</param>
    /// <param name="isInitialized">Sets the <see cref="IsInitialized"/> to a known state.</param>
    public void SetEmptyCompressedBytes(Mat src, bool isInitialized)
    {
        SetEmptyCompressedBytes(src);
        IsInitialized = isInitialized;
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> and <see cref="Compressor"/> and <see cref="Decompressor"/>.
    /// </summary>
    /// <param name="compressedBytes"></param>
    /// <param name="decompressor"></param>
    /// <param name="setCompressor"></param>
    public void SetCompressedBytes(byte[] compressedBytes, MatCompressor decompressor, bool setCompressor = true)
    {
        CompressedBytes = compressedBytes;
        if (setCompressor) Compressor = decompressor;
        Decompressor = decompressor;
    }

    /// <summary>
    /// Sets the <see cref="CompressedBytes"/> to uncompressed bitmap data.
    /// </summary>
    /// <param name="src"></param>
    private void SetUncompressed(Mat src)
    {
        CompressedBytes = src.GetBytes();
        IsCompressed = false;
        Decompressor = MatCompressorNone.Instance;
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array. 
    /// </summary>
    /// <param name="src"></param>
    /// <param name="argument"></param>
    public void Compress(Mat src, object? argument = null)
    {
        Width = src.Width;
        Height = src.Height;
        Depth = src.Depth;
        Channels = src.NumberOfChannels;
        Roi = Rectangle.Empty;

        if (src.IsEmpty)
        {
            CompressedBytes = Array.Empty<byte>();
            return;
        }

        var srcLength = src.GetLength();
        if (srcLength <= ThresholdToCompress) // Do not compress if the size is smaller or equal to the threshold
        {
            SetUncompressed(src);
            return;
        }

        try
        {
            CompressedBytes = Compressor.Compress(src, argument);
            if (_compressedBytes.Length < srcLength) // Compressed ok
            {
                Decompressor = Compressor;
            }
            else if (ReferenceEquals(Compressor, MatCompressorNone.Instance)) // Special case for uncompressed bitmap
            {
                IsCompressed = false;
                Decompressor = Compressor;
            }
            else // Compressed size is larger than uncompressed size
            {
                SetUncompressed(src);
            }
        }
        catch (Exception) // Cannot compress due some error
        {
            SetUncompressed(src);
        }
    }

    /// <summary>
    /// Compresses the <see cref="MatRoi"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="argument"></param>
    public void Compress(MatRoi src, object? argument = null)
    {
        if (src.RoiSize.IsEmpty)
        {
            Width = src.SourceMat.Width;
            Height = src.SourceMat.Height;
            Depth = src.SourceMat.Depth;
            Channels = src.SourceMat.NumberOfChannels;
            Roi = Rectangle.Empty;
            CompressedBytes = Array.Empty<byte>();
            return;
        }

        if (src.IsSourceSameSizeOfRoi)
        {
            Compress(src.SourceMat, argument);
        }
        else
        {
            Compress(src.RoiMat, argument);
            Width = src.SourceMat.Width;
            Height = src.SourceMat.Height;
            Roi = src.Roi;
        }
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="argument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CompressAsync(Mat src, object? argument = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Compress(src, argument), cancellationToken);
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="argument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CompressAsync(MatRoi src, object? argument = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Compress(src, argument), cancellationToken);
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/> without expanding into the original <see cref="Mat"/> if there is a <see cref="Roi"/>.
    /// </summary>
    /// <param name="argument"></param>
    /// <returns>Returns a <see cref="Mat"/> with size of <see cref="Roi"/> if is not empty, otherwise returns the original <see cref="Size"/></returns>
    public Mat RawDecompress(object? argument = null)
    {
        if (IsEmpty) return Roi.Size.IsEmpty ? CreateMatZeros() : EmguExtensions.InitMat(Roi.Size, Channels, Depth);

        var mat = Roi.Size.IsEmpty ? CreateMat() : new Mat(Roi.Size, Depth, Channels);

        if (IsCompressed)
        {
            Decompressor.Decompress(_compressedBytes, mat, argument);
        }
        else
        {
            mat.SetBytes(_compressedBytes);
        }

        return mat;
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/> without expanding into the original <see cref="Mat"/> if there is a <see cref="Roi"/>.
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Returns a <see cref="Mat"/> with size of <see cref="Roi"/> if is not empty, otherwise returns the original <see cref="Size"/></returns>
    public Task<Mat> RawDecompressAsync(object? argument = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => RawDecompress(argument), cancellationToken);
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/>.
    /// </summary>
    /// <param name="argument"></param>
    /// <returns></returns>
    public Mat Decompress(object? argument = null)
    {
        if (IsEmpty) return CreateMatZeros();

        var mat = Roi.Size.IsEmpty ? CreateMat() : new Mat(Roi.Size, Depth, Channels);

        if (IsCompressed)
        {
            Decompressor.Decompress(_compressedBytes, mat, argument);
        }
        else
        {
            mat.SetBytes(_compressedBytes);
        }

        if (Roi.Size.IsEmpty) return mat;

        var fullMat = CreateMatZeros();
        using var roi = new Mat(fullMat, Roi);
        mat.CopyTo(roi);
        mat.Dispose();
        return fullMat;
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/>.
    /// </summary>
    /// <param name="argument"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Mat> DecompressAsync(object? argument = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Decompress(argument), cancellationToken);
    }
    #endregion

    #region Utilities

    /// <summary>
    /// Creates a new <see cref="Mat"/> with the same size, depth, and channels as the <see cref="CMat"/>.
    /// </summary>
    /// <returns></returns>
    public Mat CreateMat()
    {
        if (Width <= 0 || Height <= 0) return new Mat();
        var mat = new Mat(Size, Depth, Channels);
        return mat;
    }

    /// <summary>
    /// Create a new <see cref="Mat"/> with the same size, depth, and channels as the <see cref="CMat"/> but with all bytes set to 0.
    /// </summary>
    /// <returns></returns>
    public Mat CreateMatZeros()
    {
        return EmguExtensions.InitMat(Size, Channels, Depth);
    }

    #endregion

    #region Copy and Clone
    /// <summary>
    /// Copies the <see cref="CMat"/> to the <paramref name="dst"/>.
    /// </summary>
    /// <param name="dst"></param>
    public void CopyTo(CMat dst)
    {
        dst._compressedBytes = _compressedBytes.ToArray();
        dst._hash = _hash;
        dst.IsInitialized = IsInitialized;
        dst.IsCompressed = IsCompressed;
        dst.ThresholdToCompress = ThresholdToCompress;
        dst.Compressor = Compressor;
        dst.Decompressor = Decompressor;
        dst.Width = Width;
        dst.Height = Height;
        dst.Depth = Depth;
        dst.Channels = Channels;
        dst.Roi = Roi;
    }
    
    /// <summary>
    /// Creates a clone of the <see cref="CMat"/> with the same <see cref="CompressedBytes"/>.
    /// </summary>
    /// <returns></returns>
    public CMat Clone()
    {
        var clone = (CMat)MemberwiseClone();
        clone._compressedBytes = _compressedBytes.ToArray();
        return clone;
    }
    #endregion

    #region Formaters
    public override string ToString()
    {
        return $"{nameof(Decompressor)}: {Decompressor}, {nameof(Size)}: {Size}, {nameof(UncompressedLength)}: {UncompressedLength}, {nameof(Length)}: {Length}, {nameof(IsCompressed)}: {IsCompressed}, {nameof(CompressionRatio)}: {CompressionRatio}x, {nameof(CompressionPercentage)}: {CompressionPercentage}%";
    }
    #endregion

    #region Equality

    public bool Equals(CMat? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return IsInitialized == other.IsInitialized 
               && IsCompressed == other.IsCompressed 
               //&& ThresholdToCompress == other.ThresholdToCompress 
               && Width == other.Width 
               && Height == other.Height 
               && Depth == other.Depth 
               && Channels == other.Channels 
               && Roi.Equals(other.Roi)
               && _compressedBytes.Length == other._compressedBytes.Length
               && _compressedBytes.SequenceEqual(other._compressedBytes);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CMat)obj);
    }

    public static bool operator ==(CMat? left, CMat? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CMat? left, CMat? right)
    {
        return !Equals(left, right);
    }

    #endregion
}