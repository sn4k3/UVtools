/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionModelPosition : Suggestion
{
    #region Enums
    public enum SuggestionModelAnchor : byte
    {
        [Description("⬚ Random")]
        Random,
        [Description("⌜ Top left")]
        TopLeft,
        [Description("⌝ Top right")]
        TopRight,
        [Description("⌟ Bottom right")]
        BottomRight,
        [Description("⌞ Bottom left")]
        BottomLeft
    }
    #endregion

    #region Members

    #endregion

    #region Properties

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;

            if (SlicerFile.BoundingRectangle.Size == SlicerFile.Resolution) return true;

            var topLeft     = new Point(LeftRightMargin, TopBottomMargin);
            var topRight    = new Point((int)SlicerFile.ResolutionX - LeftRightMargin, TopBottomMargin);
            var bottomRight = new Point((int)SlicerFile.ResolutionX - LeftRightMargin, (int)SlicerFile.ResolutionY - TopBottomMargin);
            var bottomLeft  = new Point(LeftRightMargin, (int)SlicerFile.ResolutionY - TopBottomMargin);

            var applyWhen = ApplyWhen;
            if (SlicerFile.ResolutionX - SlicerFile.BoundingRectangle.Size.Width < MinimumLeftRightMargin * 2
                || SlicerFile.ResolutionY - SlicerFile.BoundingRectangle.Size.Height < MinimumTopBottomMargin * 2)
            {
                applyWhen = SuggestionApplyWhen.Different;
            }

            return applyWhen switch
            {
                SuggestionApplyWhen.OutsideLimits => 
                /*TL*/    (SlicerFile.BoundingRectangle.X >= MinimumLeftRightMargin && SlicerFile.BoundingRectangle.X <= MaximumLeftRightMargin && SlicerFile.BoundingRectangle.Y >= MinimumTopBottomMargin && SlicerFile.BoundingRectangle.Y <= MaximumTopBottomMargin)
                /*TR*/ || (SlicerFile.BoundingRectangle.Right <= (int)SlicerFile.ResolutionX - MinimumLeftRightMargin && SlicerFile.BoundingRectangle.Right >= (int)SlicerFile.ResolutionX - MaximumLeftRightMargin && SlicerFile.BoundingRectangle.Y >= MinimumTopBottomMargin && SlicerFile.BoundingRectangle.Y <= MaximumTopBottomMargin)
                /*BR*/ || (SlicerFile.BoundingRectangle.Right <= (int)SlicerFile.ResolutionX - MinimumLeftRightMargin && SlicerFile.BoundingRectangle.Right >= (int)SlicerFile.ResolutionX - MaximumLeftRightMargin && SlicerFile.BoundingRectangle.Bottom <= (int)SlicerFile.ResolutionY - MinimumTopBottomMargin && SlicerFile.BoundingRectangle.Bottom >= (int)SlicerFile.ResolutionY - MaximumTopBottomMargin)
                /*BL*/ || (SlicerFile.BoundingRectangle.X >= MinimumLeftRightMargin && SlicerFile.BoundingRectangle.X <= MaximumLeftRightMargin && SlicerFile.BoundingRectangle.Bottom <= (int)SlicerFile.ResolutionY - MinimumTopBottomMargin && SlicerFile.BoundingRectangle.Bottom >= (int)SlicerFile.ResolutionY - MaximumTopBottomMargin)
                ,
                SuggestionApplyWhen.Different =>
                /*TL*/    (SlicerFile.BoundingRectangle.Location == topLeft)
                /*TR*/ || (SlicerFile.BoundingRectangle.Right == topRight.X && SlicerFile.BoundingRectangle.Y == topRight.Y)
                /*BR*/ || (SlicerFile.BoundingRectangle.Right == bottomRight.X && SlicerFile.BoundingRectangle.Bottom == bottomRight.Y)
                /*BL*/ || (SlicerFile.BoundingRectangle.X == bottomLeft.X && SlicerFile.BoundingRectangle.Bottom == bottomLeft.Y)
                ,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public override string Title => "Model position";

    public override string Description =>
        "Printing on a corner will reduce the FEP stretch forces when detaching from the model during a lift sequence, benefits are: Reduced lift height and faster printing, less stretch, less FEP marks, better FEP lifespan, easier to peel, less prone to failure and use the screen pixels more evenly.\n" +
        "If the model is too large to fit within the margin(s) on the screen, it will attempt to center it on that same axis to avoid touching on screen edge(s) and to give a sane margin from it.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.BoundingRectangle.ToString().Replace(",", ", ")}" 
        : $"{GlobalNotAppliedMessage}: {SlicerFile.BoundingRectangle.Location} is out of the recommended {AnchorPosition}";

    public override string ToolTip => $"The recommended model position must be at a corner and between a top/bottom margin of [{MinimumTopBottomMargin}px to {MaximumTopBottomMargin}px] and left/right margin of [{MinimumLeftRightMargin}px to {MaximumLeftRightMargin}px].\n" +
                                      $"Explanation: {Description}";

    public override string? InformationUrl => "https://ameralabs.com/blog/9-settings-to-change-for-faster-resin-3d-printing";

    public override string? ConfirmationMessage => $"{Title}: {SlicerFile.BoundingRectangle.Location} » {AnchorPosition} ({AnchorType})";

    [ObservableProperty]
    public partial SuggestionModelAnchor AnchorType { get; set; } = SuggestionModelAnchor.Random;

    [ObservableProperty]
    public partial ushort TargetTopBottomMargin { get; set; } = 100;

    public ushort TopBottomMargin
    {
        get
        {
            var margin = Math.Clamp(TargetTopBottomMargin, MinimumTopBottomMargin, MaximumTopBottomMargin);
            return SlicerFile.ResolutionY - SlicerFile.BoundingRectangle.Size.Height < margin * 2
                ? (ushort) Math.Round((SlicerFile.ResolutionY - SlicerFile.BoundingRectangle.Size.Height) / 2f,
                    MidpointRounding.AwayFromZero)
                : margin;
        }
    }


    [ObservableProperty]
    public partial ushort TargetLeftRightMargin { get; set; } = 100;

    public ushort LeftRightMargin
    {
        get
        {
            var margin = Math.Clamp(TargetLeftRightMargin, MinimumLeftRightMargin, MaximumLeftRightMargin);
            return SlicerFile.ResolutionX - SlicerFile.BoundingRectangle.Size.Width < margin * 2
                ? (ushort) Math.Round((SlicerFile.ResolutionX - SlicerFile.BoundingRectangle.Size.Width) / 2f,
                    MidpointRounding.AwayFromZero)
                : margin;
        }
    }

    [ObservableProperty]
    public partial ushort MinimumTopBottomMargin { get; set; } = 50;

    [ObservableProperty]
    public partial ushort MaximumTopBottomMargin { get; set; } = 300;

    [ObservableProperty]
    public partial ushort MinimumLeftRightMargin { get; set; } = 50;

    [ObservableProperty]
    public partial ushort MaximumLeftRightMargin { get; set; } = 300;

    public Anchor ToolMoveAnchor
    {
        get
        {
            switch (AnchorType)
            {
                case SuggestionModelAnchor.TopLeft:
                    return Anchor.TopLeft;
                case SuggestionModelAnchor.TopRight:
                    return Anchor.TopRight;
                case SuggestionModelAnchor.BottomRight:
                    return Anchor.BottomRight;
                case SuggestionModelAnchor.BottomLeft:
                    return Anchor.BottomLeft;
                case SuggestionModelAnchor.Random:
                default:
                    var anchors = new[]
                    {
                        Anchor.TopLeft,
                        Anchor.TopRight,
                        Anchor.BottomRight,
                        Anchor.BottomLeft
                    };

                    return anchors[new Random().Next(anchors.Length)];
            }
        }
    }

    public Point AnchorPosition =>
        ToolMoveAnchor switch
        {
            Anchor.TopLeft     => new Point(LeftRightMargin, TopBottomMargin),
            Anchor.TopRight    => new Point((int) SlicerFile.ResolutionX - LeftRightMargin, TopBottomMargin),
            Anchor.BottomRight => new Point((int) SlicerFile.ResolutionX - LeftRightMargin, (int) SlicerFile.ResolutionY - TopBottomMargin),
            Anchor.BottomLeft  => new Point(LeftRightMargin, (int) SlicerFile.ResolutionY - TopBottomMargin),
            _ => throw new ArgumentOutOfRangeException()
        };

    /*private static Anchor ToolMoveRandomAnchor
    {
        get
        {
            var anchors = new[]
            {
                Anchor.TopLeft,
                Anchor.TopRight,
                Anchor.BottomRight,
                Anchor.BottomLeft
            };

            return anchors[new Random().Next(0, anchors.Length)];
        }
    }*/

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (MinimumTopBottomMargin > MaximumTopBottomMargin)
        {
            sb.AppendLine("Minimum top/bottom margin limit (px) can't be higher than maximum limit (px)");
        }

        if (MinimumLeftRightMargin > MaximumLeftRightMargin)
        {
            sb.AppendLine("Minimum left/right margin limit (px) can't be higher than maximum limit (px)");
        }

        return sb.ToString();
    }

    #endregion

    #region Constructor
    public SuggestionModelPosition()
    {
        ApplyWhen = SuggestionApplyWhen.OutsideLimits;
    }
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var anchor = ToolMoveAnchor;
        var operation = new OperationMove(SlicerFile, anchor);

        switch (anchor)
        {
            case Anchor.TopLeft:
                operation.MarginLeft = LeftRightMargin;
                operation.MarginTop = TopBottomMargin;
                break;
            case Anchor.TopRight:
                operation.MarginRight = LeftRightMargin;
                operation.MarginTop = TopBottomMargin;
                break;
            case Anchor.BottomLeft:
                operation.MarginLeft = LeftRightMargin;
                operation.MarginBottom = TopBottomMargin;
                break;
            case Anchor.BottomRight:
                operation.MarginRight = LeftRightMargin;
                operation.MarginBottom = TopBottomMargin;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        return operation.IsWithinBoundary && operation.Execute(progress);
    }

    #endregion
}
