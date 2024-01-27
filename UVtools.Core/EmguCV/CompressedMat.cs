using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;

namespace UVtools.Core.EmguCV;

public class CompressedMat : IEquatable<CompressedMat>
{
    /// <summary>
    /// Gets the compressed bytes that have been compressed with <see cref="Decompressor"/>.
    /// </summary>
    private byte[] _compressedBytes = Array.Empty<byte>();

    /// <summary>
    /// Gets the compressed bytes that have been compressed with <see cref="Decompressor"/>.
    /// </summary>
    public byte[] CompressedBytes
    {
        get => _compressedBytes;
        private set => _compressedBytes = value;
    }

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
    public Rectangle Roi { get; private set; } = default;

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
    public bool IsEmpty => CompressedBytes.Length == 0;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CompressedBytes"/> are compressed or raw bytes.
    /// </summary>
    public bool IsCompressed { get; private set; }

    public Mat Mat
    {
        get => Decompress();
        set => Compress(value);
    }
    
    public CompressedMat(int width = 0, int height = 0, DepthType depth = DepthType.Cv8U, int channels = 1)
    {
        Width = width;
        Height = height;
        Depth = depth;
        Channels = channels;
    }

    public CompressedMat(Mat mat)
    {
        Compress(mat);
    }

    public CompressedMat(MatRoi matRoi)
    {
        Compress(matRoi);
    }

    public CompressedMat(MatCompressor compressor, int width = 0, int height = 0, DepthType depth = DepthType.Cv8U, int channels = 1) : this(width, height, depth, channels)
    {
        Compressor = compressor;
        Decompressor = compressor;
    }

    public CompressedMat(MatCompressor compressor, Mat mat)
    {
        Compressor = compressor;
        Decompressor = compressor;
        Compress(mat);
    }

    public CompressedMat(MatCompressor compressor, MatRoi matRoi)
    {
        Compressor = compressor;
        Decompressor = compressor;
        Compress(matRoi);
    }

    /// <summary>
    /// Creates a new <see cref="Mat"/> with the same size, depth, and channels as the <see cref="CompressedMat"/>.
    /// </summary>
    /// <returns></returns>
    public Mat CreateMat()
    {
        if (Width <= 0 || Height <= 0) return new Mat();
        var mat = new Mat(Height, Width, Depth, Channels);
        return mat;
    }

    /// <summary>
    /// Create a new <see cref="Mat"/> with the same size, depth, and channels as the <see cref="CompressedMat"/> but with all bytes set to 0.
    /// </summary>
    /// <returns></returns>
    public Mat CreateMatZeros()
    {
        return EmguExtensions.InitMat(Size, Channels, Depth);
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array. 
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    public void Compress(Mat src, object? tag = null)
    {
        Width = src.Width;
        Height = src.Height;
        Depth = src.Depth;
        Channels = src.NumberOfChannels;
        Roi = Rectangle.Empty;

        if (src.IsEmpty)
        {
            _compressedBytes = Array.Empty<byte>();
            return;
        }

        try
        {
            _compressedBytes = Compressor.Compress(src, tag);
            if (_compressedBytes.Length >= src.GetLength())
            {
                Decompressor = Compressor;
                _compressedBytes = src.GetBytes();
                IsCompressed = false;
            }
            else
            {
                IsCompressed = true;
            }
        }
        catch (Exception)
        {
            _compressedBytes = src.GetBytes();
            IsCompressed = false;
        }
        
    }

    /// <summary>
    /// Compresses the <see cref="MatRoi"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    public void Compress(MatRoi src, object? tag = null)
    {
        if (src.IsSourceSameSizeOfRoi)
        {
            Compress(src.SourceMat, tag);
        }
        else
        {
            Compress(src.RoiMat, tag);
            Width = src.SourceMat.Width;
            Height = src.SourceMat.Height;
            Roi = src.Roi;
        }
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CompressAsync(Mat src, object? tag = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Compress(src, tag), cancellationToken);
    }

    /// <summary>
    /// Compresses the <see cref="Mat"/> into a byte array.
    /// </summary>
    /// <param name="src"></param>
    /// <param name="tag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task CompressAsync(MatRoi src, object? tag = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Compress(src, tag), cancellationToken);
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/>.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public Mat Decompress(object? tag = null)
    {
        if (IsEmpty) return new Mat();

        var mat = Roi.IsEmpty ? CreateMat() : new Mat(Roi.Size, Depth, Channels);

        if (!IsCompressed)
        {
            mat.SetBytes(_compressedBytes);
        }
        else
        {
            Decompressor.Decompress(_compressedBytes, mat, tag);
        }

        if (Roi.IsEmpty) return mat;

        var fullMat = CreateMatZeros();
        using var roi = new Mat(fullMat, Roi);
        mat.CopyTo(roi);
        mat.Dispose();
        return fullMat;
    }

    /// <summary>
    /// Decompresses the <see cref="CompressedBytes"/> into a new <see cref="Mat"/>.
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Mat> DecompressAsync(object? tag = null, CancellationToken cancellationToken = default)
    {
        return Task.Run(() => Decompress(tag), cancellationToken);
    }

    
    /// <summary>
    /// Creates a clone of the <see cref="CompressedMat"/> with the same <see cref="CompressedBytes"/>.
    /// </summary>
    /// <returns></returns>
    public CompressedMat Clone()
    {
        var clone = (CompressedMat)MemberwiseClone();
        clone._compressedBytes = _compressedBytes.ToArray();
        return clone;
    }

    public bool Equals(CompressedMat? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Width == other.Width && Height == other.Height && Depth == other.Depth && Channels == other.Channels && Roi == other.Roi && IsCompressed == other.IsCompressed && _compressedBytes.SequenceEqual(other._compressedBytes);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((CompressedMat)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_compressedBytes, Width, Height, (int)Depth, Channels, Roi, IsCompressed);
    }

    public static bool operator ==(CompressedMat? left, CompressedMat? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CompressedMat? left, CompressedMat? right)
    {
        return !Equals(left, right);
    }
}