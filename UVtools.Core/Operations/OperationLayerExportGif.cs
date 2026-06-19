/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using EmguExtensions;
using SixLabors.ImageSharp.Formats;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using Point = System.Drawing.Point;
using Size = System.Drawing.Size;
using ZLinq;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationLayerExportGif : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _scale = 50;

    #endregion

    #region Overrides

    public override string IconClass => "FileGifBox";
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
        var result = $"[Clip bounds: {ClipByVolumeBounds}]" +
                     $" [Render count: {RenderLayerCount}]" +
                     $" [FPS: {FPS}]" +
                     $" [Repeats: {Repeats}]" +
                     $" [Skip: {Skip}]" +
                     $" [Scale: {_scale}%]" +
                     $" [Rotate: {RotateDirection}]" +
                     $" [Flip: {FlipDirection}]" +
                     LayerRangeString;
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LayerRangeCount))
        {
            OnPropertyChanged(nameof(TotalLayers));
            OnPropertyChanged(nameof(GifDurationMilliseconds));
            OnPropertyChanged(nameof(GifDurationSeconds));
        }
        base.OnPropertyChanged(e);
    }

    #endregion

    #region Properties

    [ObservableProperty]
    public partial string FilePath { get; set; } = null!;

    [ObservableProperty]
    public partial bool ClipByVolumeBounds { get; set; }

    [ObservableProperty]
    public partial bool RenderLayerCount { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FPSToMilliseconds))]
    [NotifyPropertyChangedFor(nameof(GifDurationMilliseconds))]
    [NotifyPropertyChangedFor(nameof(GifDurationSeconds))]
    public partial byte FPS { get; set; } = 30;

    public int FPSToMilliseconds => 1000 / FPS;

    [ObservableProperty]
    public partial ushort Repeats { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalLayers))]
    [NotifyPropertyChangedFor(nameof(GifDurationMilliseconds))]
    [NotifyPropertyChangedFor(nameof(GifDurationSeconds))]
    public partial ushort Skip { get; set; }

    public uint TotalLayers => (uint)(LayerRangeCount / (float) (Skip + 1));

    public uint GifDurationMilliseconds => (uint)(TotalLayers * FPSToMilliseconds);
    public float GifDurationSeconds => MathF.Round(GifDurationMilliseconds / 1000.0f, 2);

    public decimal Scale
    {
        get => _scale;
        set => SetProperty(ref _scale, Math.Round(value, 2));
    }

    public float ScaleFactor => (float)_scale / 100f;

    [ObservableProperty]
    public partial RotateDirection RotateDirection { get; set; } = RotateDirection.None;

    [ObservableProperty]
    public partial FlipDirection FlipDirection { get; set; } = FlipDirection.None;

    #endregion

    #region Constructor

    public OperationLayerExportGif()
    { }

    public OperationLayerExportGif(FileFormat slicerFile) : base(slicerFile)
    {
        FlipDirection = SlicerFile.DisplayMirror;
        Skip = TotalLayers switch
        {
            > 5000 => 2,
            > 1000 => 1,
            _ => Skip
        };
        /*while (TotalLayers > 500)
        {
            Skip++;
        }*/
    }

    public override void InitWithSlicerFile()
    {
        FilePath = SlicerFile.FileFullPathNoExt + ".gif";
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {

        //using var gif = AnimatedGif.AnimatedGif.Create(FilePath, FPSToMilliseconds, Repeats);
        // Set animation loop repeat count to 5.

        progress.Reset("Packed layers", TotalLayers);

        var fontFace = FontFace.HersheyDuplex;
        float fontScale = 1.5f;
        int fontThickness = 2;
        MCvScalar textColor = new(200);

        if (ClipByVolumeBounds)
        {
            ROI = SlicerFile.BoundingRectangle;
        }

        var roiSize = GetRoiSizeOrDefault();
        var imgSize = new Size((int)(roiSize.Width * ScaleFactor), (int)(roiSize.Height * ScaleFactor));
        var finalSize = Size.Empty;

        Image<L8>? gif = null;

        var delay = FPSToMilliseconds / 10;
        var layerBuffer = new byte[TotalLayers][];
        var batches = ValueEnumerable.Range(0, (int)TotalLayers).Chunk(FileFormat.DefaultParallelBatchCount);
        foreach (var batch in batches)
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), i =>
            {
                progress.PauseIfRequested();
                uint layerIndex = (uint)(LayerIndexStart + i * (Skip + 1));
                var layer = SlicerFile[layerIndex];
                using var mat = layer.LayerMat;
                using var matRoi = GetRoiOrDefault(mat);

                if (_scale != 100)
                {
                    CvInvoke.Resize(matRoi, matRoi, imgSize);
                }

                if (FlipDirection != FlipDirection.None)
                {
                    CvInvoke.Flip(matRoi, matRoi, (FlipType)FlipDirection);
                }

                if (RotateDirection != RotateDirection.None)
                {
                    CvInvoke.Rotate(matRoi, matRoi, (RotateFlags)RotateDirection);
                }

                if (RenderLayerCount)
                {
                    int baseLine = 0;
                    var text = string.Format($"{{0:D{SlicerFile.LayerDigits}}}/{{1}}",
                        layerIndex,
                        SlicerFile.LayerCount - 1);
                    var fontSize = CvInvoke.GetTextSize(text, fontFace, fontScale, fontThickness, ref baseLine);

                    Point point = new(
                        matRoi.Width / 2 - fontSize.Width / 2,
                        70);
                    CvInvoke.PutText(matRoi, text, point, fontFace, fontScale, textColor, fontThickness, LineType.AntiAlias);
                }

                //ApplyMask(matOriginal, matRoi);

                if (finalSize == Size.Empty)
                {
                    finalSize = matRoi.Size;
                }

                layerBuffer[i] = matRoi.ToArray();
                progress.LockAndIncrement();
            });

            if (gif is null)
            {
                gif = new Image<L8>(imgSize.Width, imgSize.Height);
                var gifMetaData = gif.Metadata.GetGifMetadata();
                gifMetaData.RepeatCount = Repeats;

                var metadata = gif.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = delay;
            }

            foreach (var i in batch)
            {
                // Create a color image, which will be added to the gif.
                using var image = Image.LoadPixelData<L8>(layerBuffer[i], finalSize.Width, finalSize.Height);
                layerBuffer[i] = null!;

                // Set the delay until the next image is displayed.
                var metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
                metadata.FrameDelay = delay;
                metadata.DisposalMode = FrameDisposalMode.RestoreToBackground;

                // Add the color image to the gif.
                gif.Frames.AddFrame(image.Frames.RootFrame);

                progress.PauseOrCancelIfRequested();
            }
        }

        /*Parallel.For(0, TotalLayers, CoreSettings.GetParallelOptions(progress), i =>
        {
            progress.PauseIfRequested();
            uint layerIndex = (uint) (LayerIndexStart + i * (Skip + 1));
            var layer = SlicerFile[layerIndex];
            using var mat = layer.LayerMat;
            //using var matOriginal = mat.Clone();
            using var matRoi = GetRoiOrDefault(mat);

            if (_scale != 100)
            {
                CvInvoke.Resize(matRoi, matRoi, imgSize);
            }

            if (FlipDirection != FlipDirection.None)
            {
                CvInvoke.Flip(matRoi, matRoi, (FlipType)FlipDirection);
            }

            if (RotateDirection != RotateDirection.None)
            {
                CvInvoke.Rotate(matRoi, matRoi, (RotateFlags)RotateDirection);
            }

            if (RenderLayerCount)
            {
                int baseLine = 0;
                var text = string.Format($"{{0:D{SlicerFile.LayerDigits}}}/{{1}}",
                    layerIndex,
                    SlicerFile.LayerCount - 1);
                var fontSize = CvInvoke.GetTextSize(text, fontFace, fontScale, fontThickness, ref baseLine);

                Point point = new(
                    matRoi.Width / 2 - fontSize.Width / 2,
                    70);
                CvInvoke.PutText(matRoi, text, point, fontFace, fontScale, textColor, fontThickness, LineType.AntiAlias);
            }

            //ApplyMask(matOriginal, matRoi);

            if (finalSize == Size.Empty)
            {
                finalSize = matRoi.Size;
            }

            layerBuffer[i] = matRoi.GetPngBytes();

            progress.LockAndIncrement();
        });



        progress.ResetNameAndProcessed("Packed layers");
        foreach (var buffer in layerBuffer)
        {
            progress.PauseOrCancelIfRequested();
            //using var stream = new MemoryStream(buffer);
            //using var img = Image.FromStream(stream);
            //gif.AddFrame(img, -1, GifQuality.Bit8);

            // Create a color image, which will be added to the gif.
            using var image = Image.Load<L8>(buffer);

            // Set the delay until the next image is displayed.
            metadata = image.Frames.RootFrame.Metadata.GetGifMetadata();
            metadata.FrameDelay = delay;
            metadata.DisposalMethod = GifDisposalMethod.RestoreToBackground;

            // Add the color image to the gif.
            gif.Frames.AddFrame(image.Frames.RootFrame);

            progress++;
        }
        */

        progress.Reset("Saving GIF to file");

        if (!progress.Token.IsCancellationRequested && gif is not null)
        {
            try
            {
                gif.Frames.RemoveFrame(0);
                gif.SaveAsGif(FilePath);
            }
            catch (Exception)
            {
                File.Delete(FilePath);
            }
            finally
            {
                gif.Dispose();
            }

        }

        return !progress.Token.IsCancellationRequested;
    }


    #endregion

    #region Equality

    private bool Equals(OperationLayerExportGif other)
    {
        return ClipByVolumeBounds == other.ClipByVolumeBounds && RenderLayerCount == other.RenderLayerCount && FPS == other.FPS && Skip == other.Skip && _scale == other._scale && RotateDirection == other.RotateDirection && FlipDirection == other.FlipDirection && Repeats == other.Repeats;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationLayerExportGif other && Equals(other);
    }

    #endregion
}