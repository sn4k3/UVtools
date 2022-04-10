/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using AnimatedGif;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations;

[Serializable]
public sealed class OperationLayerExportGif : Operation
{
    #region Members
    private string _filePath = null!;
    private bool _clipByVolumeBounds;
    private bool _renderLayerCount = true;
    private byte _fps = 30;
    private ushort _repeats;
    private ushort _skip;
    private decimal _scale = 50;
    private RotateDirection _rotateDirection = RotateDirection.None;
    private FlipDirection _flipDirection = FlipDirection.None;

    #endregion

    #region Overrides

    public override string IconClass => "mdi-file-gif-box";
    public override string Title => "Export layers to GIF";

    public override string Description =>
        "Export a layer range to an animated GIF file.\n" +
        "Note: This process is slow, optimize the parameters to output few layers as possible and/or scale them down.";

    public override string ConfirmationText =>
        $"export layers {LayerIndexStart} through {LayerIndexEnd} and pack {TotalLayers} layers?";

    public override string ProgressTitle =>
        $"Exporting layers {LayerIndexStart} through {LayerIndexEnd}";

    public override string ProgressAction => "Exported layers";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (TotalLayers == 0)
        {
            sb.AppendLine("There are no layers to pack, please adjust the configurations.");
        }
        /*else if (TotalLayers > 500)
        {
            sb.AppendLine("Packing more than 500 layers will cause most of the systems and browsers to not play the GIF animation.\n" +
                          "For that reason the pack is limited to 500 maximum layers.\n" +
                          "Use the 'Skip layers' option or adjust the layer range to limit the number of layers in the pack.");
        }*/

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Clip bounds: {_clipByVolumeBounds}]" +
                     $" [Render count: {_renderLayerCount}]" +
                     $" [FPS: {_fps}]" +
                     $" [Repeats: {_repeats}]" +
                     $" [Skip: {_skip}]" +
                     $" [Scale: {_scale}%]" +
                     $" [Rotate: {_rotateDirection}]" +
                     $" [Flip: {_flipDirection}]" +
                     LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerRangeCount))
        {
            RaisePropertyChanged(nameof(TotalLayers));
            RaisePropertyChanged(nameof(GifDurationMilliseconds));
            RaisePropertyChanged(nameof(GifDurationSeconds));
        }
        base.OnPropertyChanged(e);
    }

    #endregion

    #region Properties

    public string FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }

    public bool ClipByVolumeBounds
    {
        get => _clipByVolumeBounds;
        set => RaiseAndSetIfChanged(ref _clipByVolumeBounds, value);
    }

    public bool RenderLayerCount
    {
        get => _renderLayerCount;
        set => RaiseAndSetIfChanged(ref _renderLayerCount, value);
    }

    public byte FPS
    {
        get => _fps;
        set
        {
            if(!RaiseAndSetIfChanged(ref _fps, value)) return;
            RaisePropertyChanged(nameof(FPSToMilliseconds));
            RaisePropertyChanged(nameof(GifDurationMilliseconds));
            RaisePropertyChanged(nameof(GifDurationSeconds));
        }
    }

    public int FPSToMilliseconds => 1000 / _fps;

    public ushort Repeats
    {
        get => _repeats;
        set => RaiseAndSetIfChanged(ref _repeats, value);
    }

    public ushort Skip
    {
        get => _skip;
        set
        {
            if(!RaiseAndSetIfChanged(ref _skip, value)) return;
            RaisePropertyChanged(nameof(TotalLayers));
            RaisePropertyChanged(nameof(GifDurationMilliseconds));
            RaisePropertyChanged(nameof(GifDurationSeconds));
        }
    }

    public uint TotalLayers => (uint)(LayerRangeCount / (float) (_skip + 1));

    public uint GifDurationMilliseconds => (uint)(TotalLayers * FPSToMilliseconds);
    public float GifDurationSeconds => (float)Math.Round(GifDurationMilliseconds / 1000.0, 2);

    public decimal Scale
    {
        get => _scale;
        set => RaiseAndSetIfChanged(ref _scale, Math.Round(value, 2));
    }

    public float ScaleFactor => (float)_scale / 100f;

    public RotateDirection RotateDirection
    {
        get => _rotateDirection;
        set => RaiseAndSetIfChanged(ref _rotateDirection, value);
    }

    public FlipDirection FlipDirection
    {
        get => _flipDirection;
        set => RaiseAndSetIfChanged(ref _flipDirection, value);
    }

    #endregion

    #region Constructor

    public OperationLayerExportGif()
    { }

    public OperationLayerExportGif(FileFormat slicerFile) : base(slicerFile)
    {
        _flipDirection = SlicerFile.DisplayMirror;
        _skip = TotalLayers switch
        {
            > 5000 => 2,
            > 1000 => 1,
            _ => _skip
        };
        /*while (TotalLayers > 500)
        {
            _skip++;
        }*/
    }

    public override void InitWithSlicerFile()
    {
        _filePath = SlicerFile.FileFullPath + ".gif";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var gif = AnimatedGif.AnimatedGif.Create(_filePath, FPSToMilliseconds, _repeats);
        var layerBuffer = new byte[TotalLayers][];
        progress.Reset("Optimized layers", TotalLayers);

        var fontFace = FontFace.HersheyDuplex;
        float fontScale = 1.5f;
        int fontThickness = 2;
        MCvScalar textColor = new(200);

        if (_clipByVolumeBounds)
        {
            ROI = SlicerFile.BoundingRectangle;
        }

        Parallel.For(0, TotalLayers, CoreSettings.GetParallelOptions(progress), i =>
        {
            uint layerIndex = (uint) (LayerIndexStart + i * (_skip + 1));
            var layer = SlicerFile[layerIndex];
            using var mat = layer.LayerMat;
            //using var matOriginal = mat.Clone();
            var matRoi = GetRoiOrDefault(mat);

            if (_scale != 100)
            {
                CvInvoke.Resize(matRoi, matRoi, new Size((int) (matRoi.Width * ScaleFactor), (int)(matRoi.Height * ScaleFactor)));
            }

            if (_flipDirection != FlipDirection.None)
            {
                CvInvoke.Flip(matRoi, matRoi, (FlipType)_flipDirection);
            }

            if (_rotateDirection != RotateDirection.None)
            {
                CvInvoke.Rotate(matRoi, matRoi, (RotateFlags)_rotateDirection);
            }

            if (_renderLayerCount)
            {
                int baseLine = 0;
                var text = $"{layerIndex.ToString().PadLeft(SlicerFile.LayerCount.ToString().Length, '0')}/{SlicerFile.LayerCount-1}";
                var fontSize = CvInvoke.GetTextSize(text, fontFace, fontScale, fontThickness, ref baseLine);

                Point point = new(
                    matRoi.Width / 2 - fontSize.Width / 2,
                    70);
                CvInvoke.PutText(matRoi, text, point, fontFace, fontScale, textColor, fontThickness, LineType.AntiAlias);
            }

            //ApplyMask(matOriginal, matRoi);

            layerBuffer[i] = matRoi.GetPngByes();

            progress.LockAndIncrement();
        });

        progress.ResetNameAndProcessed("Packed layers");
        foreach (var buffer in layerBuffer)
        {
            progress.ThrowIfCancellationRequested();
            using var stream = new MemoryStream(buffer);
            using var img = Image.FromStream(stream);
            gif.AddFrame(img, -1, GifQuality.Bit8);
            progress++;
        }

        if (progress.Token.IsCancellationRequested)
        {
            try
            {
                File.Delete(_filePath);
            }
            catch
            {
                // ignored
            }
        }

        return !progress.Token.IsCancellationRequested;
    }


    #endregion

    #region Equality

    private bool Equals(OperationLayerExportGif other)
    {
        return _clipByVolumeBounds == other._clipByVolumeBounds && _renderLayerCount == other._renderLayerCount && _fps == other._fps && _skip == other._skip && _scale == other._scale && _rotateDirection == other._rotateDirection && _flipDirection == other._flipDirection && _repeats == other._repeats;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportGif other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_clipByVolumeBounds, _renderLayerCount, _fps, _skip, _scale, (int) _rotateDirection, (int) _flipDirection, _repeats);
    }

    #endregion
}