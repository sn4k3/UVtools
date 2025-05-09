/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Emgu.CV.Util;
using ZLinq;

namespace UVtools.Core.EmguCV;

public class EmguContourFamily : List<EmguContourFamily>
{
    /// <summary>
    /// Gets the index of the contour
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Gets the self contour
    /// </summary>
    public EmguContour Self { get; }

    /// <summary>
    /// Gets the contour depth level
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// Gets the root contour which is the topmost/external contour
    /// </summary>
    public EmguContourFamily Root
    {
        get
        {
            var current = this;

            while (!current.IsExternal)
            {
                current = current.Parent;
            }

            return current;
        }
    }

    /// <summary>
    /// Gets the parent contour
    /// </summary>
    public EmguContourFamily? Parent { get; }

    /// <summary>
    /// Gets the parent index
    /// </summary>
    public int ParentIndex => Parent?.Index ?? -1;

    /// <summary>
    /// Gets whether the contour is a root/external contour
    /// </summary>
    [MemberNotNullWhen(false, nameof(Parent))]
    public bool IsExternal => Depth == 0;

    /// <summary>
    /// Gets whether the contour is a positive contour
    /// </summary>
    public bool IsPositive => Depth % 2 == 0;

    /// <summary>
    /// Gets whether the contour is a negative contour
    /// </summary>
    public bool IsNegative => !IsPositive;

    /// <summary>
    /// Gets whether this contour has children contours
    /// </summary>
    public bool HaveChildren => Count > 0;


    private double _totalSolidArea = double.NaN;
    /// <summary>
    /// Gets the total solid area enclosed in this contour, including children contours, which is the sum of all positive areas minus the sum of all negative areas
    /// </summary>
    /// <remarks>Call only this method on external contours / root</remarks>
    public double TotalSolidArea
    {
        get
        {
            if (double.IsNaN(_totalSolidArea))
            {
                _totalSolidArea = 0;

                foreach (var family in TraverseTree())
                {
                    if (family.IsPositive) _totalSolidArea += family.Self.Area;
                    else _totalSolidArea -= family.Self.Area;
                }
            }

            return _totalSolidArea;
        }
    }


    public EmguContourFamily(int index, int depth, EmguContour self, EmguContourFamily? parent)
    {
        Index = index;
        Depth = depth;
        Self = self;
        Parent = parent;
    }

    /// <summary>
    /// Traverses the contour tree in a breadth-first manner
    /// </summary>
    /// <returns></returns>
    public IEnumerable<EmguContourFamily> TraverseTree()
    {
        var queue = new Queue<EmguContourFamily>();
        queue.Enqueue(this);

        while (queue.Count > 0)
        {
            var currentFamily = queue.Dequeue();
            yield return currentFamily;
            foreach (var child in currentFamily)
            {
                //yield return child;
                queue.Enqueue(child);
            }
        }
    }

    public IEnumerable<EmguContour> TraverseTreeAsEmguContour()
    {
        return TraverseTree().Select(family => family.Self);
    }

    /// <summary>
    /// Gets the contours of this family and its children of children down to last level
    /// </summary>
    /// <remarks>Always dispose the returned <see cref="VectorOfVectorOfPoint"/>.</remarks>
    /// <returns>A new instance of <see cref="VectorOfVectorOfPoint"/> holding all <see cref="VectorOfPoint"/>.</returns>
    public VectorOfVectorOfPoint ToVectorOfVectorOfPoint()
    {
        var contours = new VectorOfVectorOfPoint();

        foreach (var family in TraverseTree())
        {
            contours.Push(family.Self.Vector);
        }

        return contours;
    }

    /// <summary>
    /// Gets the contours of this family and its children of children down to last level
    /// </summary>
    /// <returns></returns>
    public EmguContour[] ToEmguContourArray()
    {
        return TraverseTreeAsEmguContour().AsValueEnumerable().ToArray();
    }


}