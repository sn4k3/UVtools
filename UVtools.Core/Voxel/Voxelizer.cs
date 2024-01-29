/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;
using UVtools.Core.Extensions;

namespace UVtools.Core.Voxel;

public class Voxelizer
{
    public sealed class UVFace
    {
        public FaceOrientation Type;
        public uint LayerIndex;
        public Rectangle FaceRect;
        public float LayerHeight;
        /* Doubly linked list of UVFaces, used during Stage 3, collapsing the faces vertically.
         * instead of modifying properties and having to remove items from lists, we keep all faces 
         * and just link parents and children together.
         * During STL triangle generation, we only draw the 'roots' (faces with no parent) and we count
         * the chain of children for how "high" the face should be. */
        public UVFace? Parent = null;
        public UVFace? Child = null;

        /* This is used to make a linked list of faces, instead of generating a list which requires resize/reallocation/copies. 
         * Particularly useful when you have a model that consists of 49 million visible faces...*/
        public UVFace? FlatListNext = null;
    }

    [Flags]
    public enum FaceOrientation : short
    {
        None = 0,
        Top = 1,
        Bottom = 2,
        Left = 4,
        Right = 8,
        Front = 16,
        Back = 32
    }

    public static FaceOrientation GetOpenFaces(Mat layer, int x, int y, Mat? layerBelow = null, Mat? layerAbove = null)
    {
        var layerSpan = layer.GetDataByteReadOnlySpan();

        var foundFaces = FaceOrientation.None;
        var pixelPos = layer.GetPixelPos(x, y);
        if (layerSpan[pixelPos] == 0)
        {
            return foundFaces;
        }

        if (layerBelow is null)
        {
            foundFaces |= FaceOrientation.Bottom;
        }
        else
        {
            var belowSpan = layerBelow.GetDataByteReadOnlySpan();
            if (belowSpan[pixelPos] == 0)
            {
                foundFaces |= FaceOrientation.Bottom;
            }
        }

        if (layerAbove is null)
        {
            foundFaces |= FaceOrientation.Top;
        }
        else
        {
            var aboveSpan = layerAbove.GetDataByteReadOnlySpan();
            if (aboveSpan[pixelPos] == 0)
            {
                foundFaces |= FaceOrientation.Top;
            }
        }

        if (x == 0 || layerSpan[pixelPos-1] == 0)
        {
            foundFaces |= FaceOrientation.Left;
        }

        if (x == layer.Width - 1 || layerSpan[pixelPos+1] == 0)
        {
            foundFaces |= FaceOrientation.Right;
        }

        if (y == 0 || layerSpan[layer.GetPixelPos(x, y - 1)] == 0)
        {
            foundFaces |= FaceOrientation.Front;
        }

        if (y == layer.Height - 1 || layerSpan[layer.GetPixelPos(x, y + 1)] == 0)
        {
            foundFaces |= FaceOrientation.Back;
        }

        return foundFaces;
    }

    public static Mat BuildVoxelLayerImage(Mat curLayer, Mat? layerAbove = null, Mat? layerBelow = null, ChainApproxMethod contourCompressionMethod = ChainApproxMethod.ChainApproxSimple)
    {
        /* The goal of the VoxelLayerImage is to reduce as much as possible, the number of pixels we need to do 6 direction neighbor checking on */

        /* the outer contours of the current layer should always be checked, they by definition should have an exposed face */
        using var contours = curLayer.FindContours(RetrType.Tree, contourCompressionMethod);
        var onlyContours = curLayer.NewBlank();
        CvInvoke.DrawContours(onlyContours, contours, -1, EmguExtensions.WhiteColor, 1);

        bool needAboveDispose = layerAbove is null;
        bool needBelowDispose = layerBelow is null;

        layerAbove ??= curLayer.NewBlank();
        layerBelow ??= curLayer.NewBlank();

        /* anything that is in the current layer but is not in the layer above, by definition has an exposed face */
        var upperSubtract = new Mat();
        CvInvoke.Subtract(curLayer, layerAbove, upperSubtract);

        /* anything that is in the current layer but is not in the layer below, by definition has an exposed face */
        var lowerSubtract = new Mat();
        CvInvoke.Subtract(curLayer, layerBelow, lowerSubtract);

        /* Or all of these together to get the list of pixels that have exposed face(s) */
        var voxelLayer = curLayer.NewBlank();
        CvInvoke.BitwiseOr(onlyContours, voxelLayer, voxelLayer);
        CvInvoke.BitwiseOr(upperSubtract, voxelLayer, voxelLayer);
        CvInvoke.BitwiseOr(lowerSubtract, voxelLayer, voxelLayer);

        /* dispoose of the layerAbove/layerBelow if they were allocated here */
        if (needAboveDispose)
        {
            layerAbove.Dispose();
        }
        if (needBelowDispose)
        {
            layerBelow.Dispose();
        }
        onlyContours.Dispose();

        return voxelLayer;
    }

    /* CreateVoxelMesh is no longer used, see OperationLayerExportMesh for the logic that used to be here */

    /* NOTE: this took a lot, a lot, a lot, of trial and error, just trust that it generates the correct triangles for a given face ;) */
    public static IEnumerable<(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)> MakeFacetsForUVFace(UVFace face, float xSize, float ySize, float positionZ)
    {
        /* triangles need "normal" vectors to show which is the outside of the triangle */
        /* also, triangle points need to be provided in counter clockwise direction...*/

        var LeftNormal = new Vector3(-1, 0, 0);
        var RightNormal = new Vector3(1, 0, 0);
        var TopNormal = new Vector3(0, 0, 1);
        var BottomNormal = new Vector3(0, 0, -1);
        var BackNormal = new Vector3(0, 1, 0);
        var FrontNormal = new Vector3(0, -1, 0);

        /* count the "height" of this face, which is == to itself + number of children in its doubly linked list chain */
        float height = 0;
        var totalFaceCount = 1;
        UVFace child = face;
        while (child.Child is not null)
        {
            height += child.LayerHeight;
            totalFaceCount++;
            child = child.Child;
        }
        height += child.LayerHeight;

        if (face.Type == FaceOrientation.Front)
        {
            var lowerLeft = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, positionZ);
            var lowerRight = new Vector3((face.FaceRect.X + face.FaceRect.Width) * xSize, face.FaceRect.Y * ySize, positionZ);
            var upperLeft = lowerLeft with {Z = lowerLeft.Z + height};
            var upperRight = lowerRight with {Z = lowerRight.Z + height};
            yield return (lowerLeft, lowerRight, upperRight, FrontNormal);
            yield return (upperRight, upperLeft, lowerLeft, FrontNormal);
        }
        else if (face.Type == FaceOrientation.Back)
        {
            var lowerRight = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize + ySize, positionZ);
            var lowerLeft = new Vector3((face.FaceRect.X + face.FaceRect.Width) * xSize, face.FaceRect.Y * ySize + ySize, positionZ);
            var upperLeft = lowerLeft with {Z = lowerLeft.Z + height};
            var upperRight = lowerRight with {Z = lowerRight.Z + height};
            yield return (lowerLeft, lowerRight, upperRight, BackNormal);
            yield return (upperRight, upperLeft, lowerLeft, BackNormal);
        }
        else if (face.Type == FaceOrientation.Left)
        {
            var lowerLeft = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + face.FaceRect.Width) * ySize, positionZ);
            var lowerRight = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y) * ySize, positionZ);
            var upperLeft = lowerLeft with {Z = lowerLeft.Z + height};
            var upperRight = lowerRight with {Z = lowerRight.Z + height};
            yield return (lowerLeft, lowerRight, upperRight, LeftNormal);
            yield return (upperRight, upperLeft, lowerLeft, LeftNormal);
        }
        else if (face.Type == FaceOrientation.Right)
        {
            var lowerRight = new Vector3(face.FaceRect.X * xSize + xSize, (face.FaceRect.Y + face.FaceRect.Width) * ySize, positionZ);
            var lowerLeft = new Vector3(face.FaceRect.X * xSize + xSize, (face.FaceRect.Y) * ySize, positionZ);
            var upperLeft = lowerLeft with {Z = lowerLeft.Z + height};
            var upperRight = lowerRight with {Z = lowerRight.Z + height};
            yield return (lowerLeft, lowerRight, upperRight, RightNormal);
            yield return (upperRight, upperLeft, lowerLeft, RightNormal);
        }
        else if (face.Type == FaceOrientation.Top)
        {
            var upperLeft = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, positionZ + face.LayerHeight);
            var upperRight = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + totalFaceCount) * ySize, positionZ + face.LayerHeight);
            var lowerLeft = new Vector3(upperLeft.X + (face.FaceRect.Width * xSize), upperLeft.Y, positionZ + face.LayerHeight);
            var lowerRight = new Vector3(upperRight.X + (face.FaceRect.Width * xSize), upperRight.Y, positionZ + face.LayerHeight);
            yield return (lowerLeft, lowerRight, upperRight, TopNormal);
            yield return (upperRight, upperLeft, lowerLeft, TopNormal);
        }
        else if (face.Type == FaceOrientation.Bottom)
        {
            var upperRight = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, positionZ);
            var upperLeft = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + totalFaceCount) * ySize, positionZ);
            var lowerLeft = new Vector3(upperLeft.X + (face.FaceRect.Width * xSize), upperLeft.Y, positionZ);
            var lowerRight = new Vector3(upperRight.X + (face.FaceRect.Width * xSize), upperRight.Y, positionZ);
            yield return (lowerLeft, lowerRight, upperRight, BottomNormal);
            yield return (upperRight, upperLeft, lowerLeft, BottomNormal);
        }
        /*else
        {
            yield break;
        }*/

    }
}