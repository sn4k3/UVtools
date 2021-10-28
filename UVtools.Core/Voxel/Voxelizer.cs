/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV;
using Emgu.CV.CvEnum;
using KdTree;
using KdTree.Math;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.MeshFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.Voxel
{
    public class Voxelizer
    {

        public class UVFace
        {
            public FaceOrientation Type;
            public uint LayerIndex;
            public Rectangle FaceRect;

            /* Doubly linked list of UVFaces, used during Stage 3, collapsing the faces vertically.
             * instead of modifying properties and having to remove items from lists, we keep all faces 
             * and just link parents and children together.
             * During STL triangle generation, we only draw the 'roots' (faces with no parent) and we count
             * the chain of children for how "high" the face should be. */
            public UVFace Parent = null;
            public UVFace Child = null;

            /* This is used to make a linked list of faces, instead of generating a list which requires resize/reallocation/copies. 
             * Particularly useful when you have a model that consists of 49 million visible faces...*/
            public UVFace FlatListNext = null;
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

        public static FaceOrientation GetOpenFaces(Mat layer, int x, int y, Mat layerBelow = null, Mat layerAbove = null)
        {
            var layerSpan = layer.GetDataByteSpan();

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
                var belowSpan = layerBelow.GetDataByteSpan();
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
                var aboveSpan = layerAbove.GetDataByteSpan();
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

        public static Mat BuildVoxelLayerImage(Mat curLayer, Mat layerAbove = null, Mat layerBelow = null)
        {
            /* The goal of the VoxelLayerImage is to reduce as much as possible, the number of pixels we need to do 6 direction neighbor checking on */

            /* the outer contours of the current layer should always be checked, they by definition should have an exposed face */
            using var contours = curLayer.FindContours(RetrType.Tree);
            var onlyContours = curLayer.NewBlank();
            CvInvoke.DrawContours(onlyContours, contours, -1, EmguExtensions.WhiteColor, 1);

            bool needAboveDispose = layerAbove is null;
            bool needBelowDispose = layerBelow is null;

            layerAbove ??= curLayer.NewBlank();
            layerBelow ??= curLayer.NewBlank();

            /* anything that is in the current layer but is not in the layer above, by definition has an exposed face */
            var upperXor = curLayer.NewBlank();
            CvInvoke.BitwiseXor(curLayer, layerAbove, upperXor);

            /* anything that is in the current layer but is not in the layer below, by definition has an exposed face */
            var lowerXor = curLayer.NewBlank();
            CvInvoke.BitwiseXor(curLayer, layerBelow, lowerXor);

            /* Or all of these together to get the list of pixels that have exposed face(s) */
            var voxelLayer = curLayer.NewBlank();
            CvInvoke.BitwiseOr(onlyContours, voxelLayer, voxelLayer);
            CvInvoke.BitwiseOr(upperXor, voxelLayer, voxelLayer);
            CvInvoke.BitwiseOr(lowerXor, voxelLayer, voxelLayer);

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
        public enum VoxelQuality : byte
        {
            ACCURATE = 1,
            AVERAGE = 2,
            QUICK = 3,

            DIRTY = 6,
            MINECRAFT = 8
        }

        public unsafe void CreateVoxelMesh(Type meshType, FileFormat file, string filePath, OperationProgress progress, VoxelQuality quality = VoxelQuality.ACCURATE, uint layerStart = 0, uint layerStop = 0)
        {
            var layerManager = file.LayerManager;
            /* Voxelization has 4 overall stages
             * 1.) Generate all visible faces, this is for each pixel we determine which of its faces are visible from outside the model
             * 2.) Collapse faces horizontally, this combines faces that are coplanar horizontally into a longer face, this reduces triangles
             * 3.) Collapse faces that are coplanar and the same size vertically leveraging KD Trees for fast lookups, O(logn) vs O(n) for a normal list
             * 4.) Generate triangles for faces and write out to STL
             */

            /* Basic information for the file, how many layers, how big should each voxel be) */

            if (layerStop == 0)
            {
                layerStop = file.LayerCount;
            }

            uint layerCount = layerStop - layerStart;
            float xWidth = (float)(layerManager.SlicerFile.Xppmm > 0 ? 1 / layerManager.SlicerFile.Xppmm : 0.035)  * (int)quality;
            float yWidth = (float)(layerManager.SlicerFile.Yppmm > 0 ? 1 / layerManager.SlicerFile.Yppmm : 0.035) * (int)quality;
            var zHeight = layerManager.SlicerFile.LayerHeight;


            /* For the 1st stage, we maintain up to 3 mats, the current layer, the one below us, and the one above us 
             * (below will be null when current layer is 0, above will be null when currentlayer is layercount-1) */
            /* We init the aboveLayer to the first layer, in the loop coming up we shift above->current->below, so this effectively inits current layer */
            Mat RoiLayer = layerManager[layerStart].LayerMat.Roi(layerManager.SlicerFile.BoundingRectangle);
            Mat aboveLayer = RoiLayer.Clone(); /* clone and then dispose of the ROI mat, not effecient but keeps the GetPixelPos working and clean */
            if ((int)quality > 1)
            {
                Mat resize = new Mat();
                CvInvoke.Resize(aboveLayer, resize, new Size(), 1.0 / (int)quality, 1.0 / (int)quality, Inter.Area);
                aboveLayer.Dispose();
                aboveLayer = resize;
            }
            RoiLayer.Dispose();
            Mat curLayer = null;
            Mat belowLayer = null;

            /* List of faces to process, great for debugging if you are haveing issues with a face of particular orientation. */
            FaceOrientation[] facesToCheck = new FaceOrientation[] { FaceOrientation.Front, FaceOrientation.Back, FaceOrientation.Left, FaceOrientation.Right, FaceOrientation.Top, FaceOrientation.Bottom };

            /* Init of other objects that will be used in subsequent stages */
            UVFace[] rootFaces = new UVFace[layerCount];
            uint[] layerFaceCounts = new uint[layerCount];
            
            KdTree<float, UVFace>[] layerTrees = new KdTree<float, UVFace>[layerCount];

            progress.Reset();
            progress.Title = "Generating faces from layers...";
            progress.ItemCount = layerCount;
            
            /* Begin Stage 1, identifying all faces that are visible from outside the model */
            for (uint layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                UVFace currentFaceItem = null;

                /* Should contain a list of all found faces on this layer, keyed by the face orientation */
                Dictionary<FaceOrientation, List<Point>> foundFaces = new Dictionary<FaceOrientation, List<Point>>();

                /* move current layer to below */
                belowLayer = curLayer;

                /* move above layer to us */
                curLayer = aboveLayer;

                /* bring in a new aboveLayer if we need to */
                if (layerIndex < layerCount - 1)
                {
                    RoiLayer = layerManager[layerIndex + layerStart + 1].LayerMat.Roi(layerManager.SlicerFile.BoundingRectangle);
                    aboveLayer = RoiLayer.Clone();

                    if ((int)quality > 1)
                    {
                        Mat resize = new Mat();
                        CvInvoke.Resize(aboveLayer, resize, new Size(), 1.0 / (int)quality, 1.0 / (int)quality, Inter.Area);
                        aboveLayer.Dispose();
                        aboveLayer = resize;
                    }

                    RoiLayer.Dispose();
                    //CvInvoke.Threshold(aboveLayer, aboveLayer, 1, 255, ThresholdType.Binary);
                }
                else
                {
                    aboveLayer = null;
                }

                /* get image of pixels to do neighbor checks on */
                var voxelLayer = BuildVoxelLayerImage(curLayer, aboveLayer, belowLayer);
                var voxelBytes = voxelLayer.GetBytePointer();

                /* Seems to be faster to parallel on the Y and not the X */
                Parallel.For(0, curLayer.Height, CoreSettings.ParallelOptions, y =>
                {
                    /* Collects all the faces found for this thread, will be combined into the main dictionary later */
                    Dictionary<FaceOrientation, List<Point>> threadDict = new Dictionary<FaceOrientation, List<Point>>();
                    for (var x = 0; x < curLayer.Width; x++)
                    {
                        if (voxelBytes[voxelLayer.GetPixelPos(x, y)] == 0) continue;

                        var faces = GetOpenFaces(curLayer, x, y, belowLayer, aboveLayer);
                        if (faces != FaceOrientation.None)
                        {
                            foreach (var face in facesToCheck)
                            {
                                if (faces.HasFlag(face))
                                {

                                    if (!threadDict.ContainsKey(face))
                                    {

                                        threadDict.Add(face, new());
                                    }

                                    threadDict[face].Add(new Point(x, y));

                                }
                            }
                        }
                    }

                    /* merge all found faces to main foundFaces dictionary */
                    lock (foundFaces)
                    {
                        foreach (var kvp in threadDict)
                        {
                            if (!foundFaces.ContainsKey(kvp.Key))
                            {
                                foundFaces.Add(kvp.Key, new());
                            }
                            lock (foundFaces[kvp.Key])
                            {
                                foundFaces[kvp.Key].AddRange(kvp.Value);
                            }
                        }
                    }
                });

                /* Begin stage 2, horizontal combining of coplanar faces */
                foreach (var faceType in facesToCheck)
                {
                    if (foundFaces.ContainsKey(faceType) == false || foundFaces[faceType].Count == 0) continue;

                    if (faceType == FaceOrientation.Front || faceType == FaceOrientation.Back || faceType == FaceOrientation.Top || faceType == FaceOrientation.Bottom)
                    {
                        /* sort the faces by coordinate */
                        foundFaces[faceType] = foundFaces[faceType].OrderBy(f => f.Y).ThenBy(f => f.X).ToList();

                        var startX = foundFaces[faceType][0].X;
                        var curX = foundFaces[faceType][0].X;
                        var startY = foundFaces[faceType][0].Y;
                        var curY = foundFaces[faceType][0].Y;

                        foreach (var f in foundFaces[faceType].Skip(1))
                        {
                            if (f.Y == curY)
                            {
                                /* same row...*/
                                if (f.X == curX + 1)
                                {
                                    /* this face is adjecent to the previous, just increase the "width" */
                                    curX++;
                                }
                                else
                                {
                                    /* This face is disconnected by at least 1 pixel from the chain we've been building */
                                    /* Create a UVFace for the current chain and reset to this one */
                                    layerFaceCounts[layerIndex]++;
                                    if (currentFaceItem is null)
                                    {
                                        rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                                        currentFaceItem = rootFaces[layerIndex];
                                    }
                                    else
                                    {
                                        currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                                        currentFaceItem = currentFaceItem.FlatListNext;
                                    }
                                    //faceTree.Add(new float[] { (float)faceType, startX, startY, layerIndex }, new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) });
                                    /* disconnected */
                                    startX = f.X;
                                    curX = f.X;

                                }
                            }
                            else
                            {
                                /* this face isn't on the same Y row as previous, therefore it is disconnected. */
                                /* Create a UVFace for the current chain and reset to this one */
                                layerFaceCounts[layerIndex]++;
                                if (currentFaceItem is null)
                                {
                                    rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                                    currentFaceItem = rootFaces[layerIndex];
                                }
                                else
                                {
                                    currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                                    currentFaceItem = currentFaceItem.FlatListNext;
                                }
                                startY = f.Y;
                                curY = f.Y;
                                startX = f.X;
                                curX = f.X;
                            }
                        }
                        /* we've gone through all the faces, add the final chain we've been building */
                        /* Create a UVFace for the final chain */
                        layerFaceCounts[layerIndex]++;
                        if (currentFaceItem is null)
                        {
                            rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                            currentFaceItem = rootFaces[layerIndex];
                        }
                        else
                        {
                            currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1) };
                            currentFaceItem = currentFaceItem.FlatListNext;
                        }
                    }

                    if (faceType == FaceOrientation.Left || faceType == FaceOrientation.Right)
                    {
                        /* sort the faces by coordinate */
                        foundFaces[faceType] = foundFaces[faceType].OrderBy(f => f.X).ThenBy(f => f.Y).ToList();

                        var startX = foundFaces[faceType][0].X;
                        var curX = foundFaces[faceType][0].X;
                        var startY = foundFaces[faceType][0].Y;
                        var curY = foundFaces[faceType][0].Y;
                        foreach (var f in foundFaces[faceType].Skip(1))
                        {
                            if (f.X == curX)
                            {
                                /* same column...*/
                                if (f.Y == curY + 1)
                                {
                                    /* this face is adjecent to the previous, just increase the "width" */
                                    curY++;
                                }
                                else
                                {
                                    /* This face is disconnected by at least 1 pixel from the chain we've been building */
                                    /* Create a UVFace for the current chain and reset to this one */
                                    layerFaceCounts[layerIndex]++;
                                    if (currentFaceItem is null)
                                    {
                                        rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                                        currentFaceItem = rootFaces[layerIndex];
                                    }
                                    else
                                    {
                                        currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                                        currentFaceItem = currentFaceItem.FlatListNext;
                                    }
                                    startY = f.Y;
                                    curY = f.Y;
                                }
                            }
                            else
                            {
                                /* this face is on a different column, cannot be part of the current chain we're building */
                                /* Create a UVFace for the current chain and reset to this one */
                                layerFaceCounts[layerIndex]++;
                                if (currentFaceItem is null)
                                {
                                    rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                                    currentFaceItem = rootFaces[layerIndex];
                                }
                                else
                                {
                                    currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                                    currentFaceItem = currentFaceItem.FlatListNext;
                                }
                                startY = f.Y;
                                curY = f.Y;
                                startX = f.X;
                                curX = f.X;
                            }
                        }
                        layerFaceCounts[layerIndex]++;
                        if (currentFaceItem is null)
                        {
                            rootFaces[layerIndex] = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                            currentFaceItem = rootFaces[layerIndex];
                        }
                        else
                        {
                            currentFaceItem.FlatListNext = new UVFace() { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1) };
                            currentFaceItem = currentFaceItem.FlatListNext;
                        }
                    }
                }

                progress.LockAndIncrement();

                if (progress.Token.IsCancellationRequested)
                {
                    Cleanup();
                    return;
                }

            }

            progress.Reset();
            progress.Title = "Building KD Trees";
            progress.ItemCount = layerCount;

            /* We build out a 3 dimensional KD tree for each layer, having 1 big KD tree is prohibitive when you get to millions and millions of faces. */
            Parallel.For(0, layerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                /* Create the KD tree for the layer, in practice there should never be dups, but just in case, set to skip */
                layerTrees[layerIndex] = new KdTree<float, UVFace>(3, new FloatMath(), AddDuplicateBehavior.Skip);

                /* Walk the linked list of UVFaces, adding them to the tree */
                var currentFaceItem = rootFaces[layerIndex];
                if (currentFaceItem is null) return;
                while (currentFaceItem.FlatListNext is not null)
                {
                    layerTrees[layerIndex].Add(new float[] { (float)currentFaceItem.Type, currentFaceItem.FaceRect.X, currentFaceItem.FaceRect.Y }, currentFaceItem);
                    currentFaceItem = currentFaceItem.FlatListNext;
                }
                layerTrees[layerIndex].Add(new float[] { (float)currentFaceItem.Type, currentFaceItem.FaceRect.X, currentFaceItem.FaceRect.Y }, currentFaceItem);

                progress.LockAndIncrement();
            });

            if (progress.Token.IsCancellationRequested)
            {
                Cleanup();
                return;
            }

            progress.Reset();
            progress.Title = "Collapsing faces...";
            progress.ItemCount = layerCount;
            long collapseCount = 0;

            /* Begin Stage 3: Vertical collapse 
             * Since we don't modify the lists/objects and only connect them via doubly linked list
             * we can process each layer independant of the others.
             */
            Parallel.For(0, layerCount, idx =>
            {
                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                /* if no faces on this layer... skip.... needed for empty layers */
                if (layerTrees[idx] is null) return;

                /* check each point in the current layers tree */
                foreach (var point in layerTrees[idx])
                {
                    /* if this point already has a parent, skip */
                    if (point.Value.Parent is not null) continue;


                    /* deterimine the point below to check. 
                     * For front/back/left/right its the same X/Y point and Z is different, and Z is done basically by looking at the layer tree below us 
                     * For Top/Bottom its a bit different, the Z stays the same (we query our own layer tree) but the Y coordinate is 1 less */

                    float[] pointBelow = null;
                    KdTree<float, UVFace> treeBelow = null;
                    if (point.Value.Type == FaceOrientation.Top || point.Value.Type == FaceOrientation.Bottom)
                    {
                        if (point.Value.Type == FaceOrientation.Top)
                        {
                            pointBelow = new float[] { point.Point[0], point.Point[1], point.Point[2] - 1 };
                        } else
                        {
                            pointBelow = new float[] { point.Point[0], point.Point[1], point.Point[2] - 1 };
                        }
                        treeBelow = layerTrees[idx];
                    }
                    else
                    {
                        pointBelow = new float[] { point.Point[0], point.Point[1], point.Point[2] };
                        if (idx > 0)
                        {
                            treeBelow = layerTrees[idx - 1];
                        }
                    }

                    if (treeBelow == null) continue;
                    var faceBelow = treeBelow.FindValueAt(pointBelow);
                    if (faceBelow is not null)
                    {
                        /* if we find a face below us it has to be the same width too */
                        if (point.Value.FaceRect.Width == faceBelow.FaceRect.Width)
                        {
                            /* same coordinate, same width, safe to merge together. Do so by doubly linking the items */
                            point.Value.Parent = faceBelow;
                            faceBelow.Child = point.Value;
                            collapseCount++;
                        }
                    }
                }
                progress.LockAndIncrement();
            });

            if (progress.Token.IsCancellationRequested)
            {
                Cleanup();
                return;
            }

            progress.Reset();
            progress.Title = "Generating STL...";
            progress.ItemCount = layerCount;

            using var mesh = meshType.CreateInstance<MeshFile>(filePath, FileMode.Create);
            mesh.BeginWrite();

            /* Begin Stage 4, generating triangles and saving to STL */
            foreach (var tree in layerTrees)
            {
                if (tree is null) continue;

                /* only process UVFaces that do not have a parent, these are the "root" faces that couldn't be combined with something above them */
                foreach (var p in tree.Where(p => p.Value.Parent is null))
                {
                    /* generate the triangles */
                    foreach (var f in MakeFacetsForUVFace(p.Value, xWidth, yWidth, zHeight, layerStart))
                    {
                        /* save to STL file */
                        mesh.WriteTriangle(f.p1, f.p2, f.p3, f.normal);
                    }
                }

                /* check for cancellation at every layer, and if so, close the STL file properly */
                if (progress.Token.IsCancellationRequested)
                {
                    Cleanup();
                    return;
                }
                progress.LockAndIncrement();
            }
            
            void Cleanup()
            {
                /* dispose of everything */
                for (var x = 0; x < layerTrees.Length; x++)
                {
                    layerTrees[x] = null;
                }

                layerTrees = null;

                for (var x = 0; x < rootFaces.Length; x++)
                {
                    if (rootFaces[x] is not null)
                    {
                        rootFaces[x].FlatListNext = null;
                    }
                    rootFaces[x] = null;
                }
                rootFaces = null;
                GC.Collect();
            }

            mesh.EndWrite();

        }

        /* NOTE: this took a lot, a lot, a lot, of trial and error, just trust that it generates the correct triangles for a given face ;) */
        public static IEnumerable<(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 normal)> MakeFacetsForUVFace(UVFace face, float xSize, float ySize, float layerHeight, uint layerStart)
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
            var height = 1;
            UVFace child = face;
            while (child.Child is not null)
            {
                height++;
                child = child.Child;
            }
            face.LayerIndex += layerStart;
            if (face.Type == FaceOrientation.Front)
            {
                var lowerLeft = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, face.LayerIndex * layerHeight);
                var lowerRight = new Vector3((face.FaceRect.X + face.FaceRect.Width) * xSize, face.FaceRect.Y * ySize, face.LayerIndex * layerHeight);
                var upperLeft = new Vector3(lowerLeft.X, lowerLeft.Y, lowerLeft.Z + ((height) * layerHeight));
                var upperRight = new Vector3(lowerRight.X, lowerRight.Y, lowerRight.Z + ((height) * layerHeight));
                yield return (lowerLeft, lowerRight, upperRight, FrontNormal);
                yield return (upperRight, upperLeft, lowerLeft, FrontNormal);
            }
            else if (face.Type == FaceOrientation.Back)
            {
                var lowerRight = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize + ySize, face.LayerIndex * layerHeight);
                var lowerLeft = new Vector3((face.FaceRect.X + face.FaceRect.Width) * xSize, face.FaceRect.Y * ySize + ySize, face.LayerIndex * layerHeight);
                var upperLeft = new Vector3(lowerLeft.X, lowerLeft.Y, lowerLeft.Z + ((height) * layerHeight));
                var upperRight = new Vector3(lowerRight.X, lowerRight.Y, lowerRight.Z + ((height) * layerHeight));
                yield return (lowerLeft, lowerRight, upperRight, BackNormal);
                yield return (upperRight, upperLeft, lowerLeft, BackNormal);
            }
            else if (face.Type == FaceOrientation.Left)
            {
                var lowerLeft = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + face.FaceRect.Width) * ySize, face.LayerIndex * layerHeight);
                var lowerRight = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y) * ySize, face.LayerIndex * layerHeight);
                var upperLeft = new Vector3(lowerLeft.X, lowerLeft.Y, lowerLeft.Z + ((height) * layerHeight));
                var upperRight = new Vector3(lowerRight.X, lowerRight.Y, lowerRight.Z + ((height) * layerHeight));
                yield return (lowerLeft, lowerRight, upperRight, LeftNormal);
                yield return (upperRight, upperLeft, lowerLeft, LeftNormal);
            }
            else if (face.Type == FaceOrientation.Right)
            {
                var lowerRight = new Vector3(face.FaceRect.X * xSize + xSize, (face.FaceRect.Y + face.FaceRect.Width) * ySize, face.LayerIndex * layerHeight);
                var lowerLeft = new Vector3(face.FaceRect.X * xSize + xSize, (face.FaceRect.Y) * ySize, face.LayerIndex * layerHeight);
                var upperLeft = new Vector3(lowerLeft.X, lowerLeft.Y, lowerLeft.Z + ((height) * layerHeight));
                var upperRight = new Vector3(lowerRight.X, lowerRight.Y, lowerRight.Z + ((height) * layerHeight));
                yield return (lowerLeft, lowerRight, upperRight, RightNormal);
                yield return (upperRight, upperLeft, lowerLeft, RightNormal);
            }
            else if (face.Type == FaceOrientation.Top)
            {
                var upperLeft = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, face.LayerIndex * layerHeight + layerHeight);
                var upperRight = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + height) * ySize, face.LayerIndex * layerHeight + layerHeight);
                var lowerLeft = new Vector3(upperLeft.X + (face.FaceRect.Width * xSize), upperLeft.Y, face.LayerIndex * layerHeight + layerHeight);
                var lowerRight = new Vector3(upperRight.X + (face.FaceRect.Width * xSize), upperRight.Y, face.LayerIndex * layerHeight + layerHeight);
                yield return (lowerLeft, lowerRight, upperRight, TopNormal);
                yield return (upperRight, upperLeft, lowerLeft, TopNormal);
            }
            else if (face.Type == FaceOrientation.Bottom)
            {
                var upperRight = new Vector3(face.FaceRect.X * xSize, face.FaceRect.Y * ySize, face.LayerIndex * layerHeight);
                var upperLeft = new Vector3(face.FaceRect.X * xSize, (face.FaceRect.Y + height) * ySize, face.LayerIndex * layerHeight);
                var lowerLeft = new Vector3(upperLeft.X + (face.FaceRect.Width * xSize), upperLeft.Y, face.LayerIndex * layerHeight);
                var lowerRight = new Vector3(upperRight.X + (face.FaceRect.Width * xSize), upperRight.Y, face.LayerIndex * layerHeight);
                yield return (lowerLeft, lowerRight, upperRight, BottomNormal);
                yield return (upperRight, upperLeft, lowerLeft, BottomNormal);
            }
            else
            {
                yield break;
            }

        }
    }
}
