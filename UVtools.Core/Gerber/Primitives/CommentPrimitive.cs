/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// The comment primitive has no effect on the image but adds human-readable comments in an AM command.
/// The comment primitive starts with the ‘0’ code followed by a space and then a single-line text string.
/// The text string follows the syntax for strings in section 3.4.3.
/// </summary>
public class CommentPrimitive : Primitive
{
    #region Constants
    public const byte Code = 0;
    #endregion

    #region Properties
    public override string Name => "Comment";

    /// <summary>
    /// The comment
    /// 1
    /// </summary>
    public string Comment { get; set; } = string.Empty;
    #endregion

    public CommentPrimitive(GerberDocument document) : base(document)
    {
        IsParsed = true;
    }

    public CommentPrimitive(GerberDocument document, string comment) : base(document)
    {
        Comment = comment;
        IsParsed = true;
    }

    
    public override void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected)
    {

    }

    public override void ParseExpressions(params string[] args)
    {
    }
}