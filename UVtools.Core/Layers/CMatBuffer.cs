/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Threading;
using Emgu.CV.CvEnum;
using EmguExtensions;

namespace UVtools.Core.Layers;

/// <summary>
/// Buffer level operations over <see cref="CMat"/> that its public API does not expose yet:
/// sharing the compressed buffer between clones and detaching/attaching it to move the bytes to the disk cache.
/// </summary>
/// <remarks>
/// <see cref="CMat"/> never mutates its compressed array in place, it always replaces it, so clones can share it safely.
/// The private state is reached with <see cref="UnsafeAccessorAttribute"/>, when the fields are not found (a different
/// EmguExtensions version) <see cref="IsSupported"/> is false and callers fall back to the public API (deep copies, no
/// disk cache). Remove this class when EmguExtensions exposes equivalent members.
/// </remarks>
internal static class CMatBuffer
{
    #region Accessors

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_rwLock")]
    private static extern ref ReaderWriterLockSlim RwLock(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_hash")]
    private static extern ref ulong? HashField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_compressedBytes")]
    private static extern ref byte[] BytesField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_isInitialized")]
    private static extern ref bool IsInitializedField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_isCompressed")]
    private static extern ref bool IsCompressedField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_thresholdToCompress")]
    private static extern ref int ThresholdToCompressField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_width")]
    private static extern ref int WidthField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_height")]
    private static extern ref int HeightField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_depth")]
    private static extern ref DepthType DepthField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_channels")]
    private static extern ref int ChannelsField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_roi")]
    private static extern ref Rectangle RoiField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_compressionLevel")]
    private static extern ref CompressionLevel CompressionLevelField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_compressor")]
    private static extern ref MatCompressor CompressorField(CMat mat);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_decompressor")]
    private static extern ref MatCompressor DecompressorField(CMat mat);

    #endregion

    /// <summary>
    /// Gets if the running EmguExtensions version has the fields required by this class.
    /// </summary>
    public static bool IsSupported { get; } = Probe();

    private static bool Probe()
    {
        try
        {
            var mat = new CMat(2, 2);
            _ = RwLock(mat);
            _ = HashField(mat);
            _ = BytesField(mat);
            _ = IsInitializedField(mat);
            _ = IsCompressedField(mat);
            _ = ThresholdToCompressField(mat);
            _ = WidthField(mat);
            _ = HeightField(mat);
            _ = DepthField(mat);
            _ = ChannelsField(mat);
            _ = RoiField(mat);
            _ = CompressionLevelField(mat);
            _ = CompressorField(mat);
            _ = DecompressorField(mat);
            return true;
        }
        catch (Exception e) when (e is MissingFieldException or MissingMemberException or TypeLoadException)
        {
            Debug.WriteLine($"{nameof(CMatBuffer)}: unsupported EmguExtensions version, falling back to deep copies. {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates a clone of <paramref name="source"/> that shares its compressed array instead of copying it.
    /// </summary>
    public static CMat Clone(CMat source)
    {
        if (!IsSupported) return source.Clone();
        var clone = new CMat();
        CopyTo(source, clone);
        return clone;
    }

    /// <summary>
    /// Copies <paramref name="source"/> to <paramref name="destination"/> sharing the compressed array instead of copying it.
    /// </summary>
    public static void CopyTo(CMat source, CMat destination)
    {
        if (!IsSupported)
        {
            source.CopyTo(destination);
            return;
        }

        if (ReferenceEquals(source, destination)) return;

        byte[] bytes;
        ulong? hash;
        bool isInitialized, isCompressed;
        int thresholdToCompress, width, height, channels;
        DepthType depth;
        Rectangle roi;
        CompressionLevel compressionLevel;
        MatCompressor compressor, decompressor;

        var sourceLock = RwLock(source);
        sourceLock.EnterReadLock();
        try
        {
            bytes = BytesField(source);
            hash = HashField(source);
            isInitialized = IsInitializedField(source);
            isCompressed = IsCompressedField(source);
            thresholdToCompress = ThresholdToCompressField(source);
            width = WidthField(source);
            height = HeightField(source);
            depth = DepthField(source);
            channels = ChannelsField(source);
            roi = RoiField(source);
            compressionLevel = CompressionLevelField(source);
            compressor = CompressorField(source);
            decompressor = DecompressorField(source);
        }
        finally
        {
            sourceLock.ExitReadLock();
        }

        var destinationLock = RwLock(destination);
        destinationLock.EnterWriteLock();
        try
        {
            BytesField(destination) = bytes;
            HashField(destination) = hash;
            IsInitializedField(destination) = isInitialized;
            IsCompressedField(destination) = isCompressed;
            ThresholdToCompressField(destination) = thresholdToCompress;
            WidthField(destination) = width;
            HeightField(destination) = height;
            DepthField(destination) = depth;
            ChannelsField(destination) = channels;
            RoiField(destination) = roi;
            CompressionLevelField(destination) = compressionLevel;
            CompressorField(destination) = compressor;
            DecompressorField(destination) = decompressor;
        }
        finally
        {
            destinationLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets the compressed array of <paramref name="mat"/> without copying it, the array must be treated as read-only.
    /// </summary>
    public static byte[] GetBytes(CMat mat) => mat.CompressedBytes;

    /// <summary>
    /// Detaches the compressed array from <paramref name="mat"/>, leaving an empty array in its place while keeping every
    /// other state (size, ROI, hash, compressors). The hash is computed first so equality keeps working while detached.
    /// </summary>
    /// <param name="mat">The mat to detach the array from.</param>
    /// <param name="expected">The array that is expected to be attached, the detach is aborted when it was replaced meanwhile.</param>
    /// <returns>True when detached.</returns>
    public static bool TryDetach(CMat mat, byte[] expected)
    {
        if (!IsSupported) return false;
        _ = mat.Hash; // Cache the hash of the current bytes

        var rwLock = RwLock(mat);
        rwLock.EnterWriteLock();
        try
        {
            if (!ReferenceEquals(BytesField(mat), expected) || HashField(mat) is null) return false;
            BytesField(mat) = [];
            return true;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Attaches back a compressed array previously detached with <see cref="TryDetach"/>, without changing any other state.
    /// </summary>
    public static void Attach(CMat mat, byte[] bytes)
    {
        var rwLock = RwLock(mat);
        rwLock.EnterWriteLock();
        try
        {
            BytesField(mat) = bytes;
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }
}
