/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using KdTree;
using KdTree.Math;
using MoreLinq;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.MeshFormats;
using UVtools.Core.Voxel;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerExportMesh : Operation
    {
        #region MyRegion
        public enum ExportMeshQuality : byte
        {
            Accurate = 1,
            Average = 2,
            Quick = 3,

            Dirty = 6,
            Minecraft = 8
        }


        #endregion


        #region Members
        private string _filePath;
        private ExportMeshQuality _quality = ExportMeshQuality.Accurate;
        private Enumerations.RotateDirection _rotateDirection = Enumerations.RotateDirection.None;
        private Enumerations.FlipDirection _flipDirection = Enumerations.FlipDirection.None;
        #endregion

        #region Overrides

        public override bool CanHaveProfiles => false;
        public override string Title => "Export layers to mesh";

        public override string Description =>
            "Reconstructs and export a layer range to a 3D mesh via voxelization.\n" +
            "Note: Depending on quality and triangle count, this will often render heavy files.\n" +
            "This process will not recover your original 3D model as data was already lost when sliced.";

        public override string ConfirmationText =>
            $"generate a mesh from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Generating a mesh from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Packed layers";
        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (MeshFile.FindFileExtension(_filePath) is null)
            {
                sb.AppendLine("The used file extension is invalid.");
            }
            
            return sb.ToString();
        }

        /*public override string ToString()
        {
            var result = $"[Crop by ROI: {_cropByRoi}]" +
                         LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }*/

        #endregion

        #region Properties

        public string FilePath
        {
            get => _filePath;
            set => RaiseAndSetIfChanged(ref _filePath, value);
        }

        public ExportMeshQuality Quality
        {
            get => _quality;
            set => RaiseAndSetIfChanged(ref _quality, value);
        }

        public Enumerations.RotateDirection RotateDirection
        {
            get => _rotateDirection;
            set => RaiseAndSetIfChanged(ref _rotateDirection, value);
        }

        public Enumerations.FlipDirection FlipDirection
        {
            get => _flipDirection;
            set => RaiseAndSetIfChanged(ref _flipDirection, value);
        }

        #endregion

        #region Constructor

        public OperationLayerExportMesh() { }

        public OperationLayerExportMesh(FileFormat slicerFile) : base(slicerFile)
        {
            _flipDirection = SlicerFile.DisplayMirror;
        }

        public override void InitWithSlicerFile()
        {
            _filePath = Path.Combine(Path.GetDirectoryName(SlicerFile.FileFullPath), $"{SlicerFile.FilenameNoExt}.stl");
        }

        #endregion

        #region Methods

        protected override unsafe bool ExecuteInternally(OperationProgress progress)
        {
            var fileExtension = MeshFile.FindFileExtension(_filePath);
            if (fileExtension is null) return false;

            //using var meshFile = fileExtension.FileFormatType.CreateInstance<MeshFile>(_filePath, FileMode.Create);
            //new Voxelizer().CreateVoxelMesh(fileExtension.FileFormatType, SlicerFile, _filePath, progress);


            /* Voxelization has 4 overall stages
             * 1.) Generate all visible faces, this is for each pixel we determine which of its faces are visible from outside the model
             * 2.) Collapse faces horizontally, this combines faces that are coplanar horizontally into a longer face, this reduces triangles
             * 3.) Collapse faces that are coplanar and the same size vertically leveraging KD Trees for fast lookups, O(logn) vs O(n) for a normal list
             * 4.) Generate triangles for faces and write out to STL
             */

            /* Basic information for the file, how many layers, how big should each voxel be) */
            var pixelSize = SlicerFile.PixelSize;
            float xWidth = (pixelSize.Width > 0 ? pixelSize.Width : 0.035f) * (byte)_quality;
            float yWidth = (pixelSize.Height > 0 ? pixelSize.Height : 0.035f) * (byte)_quality;
            var zHeight = SlicerFile.LayerHeight;

            //var totalLayerCount = SlicerFile.LayerCount;
            var distinctLayers = SlicerFile.Where((_, layerIndex) => layerIndex >= LayerIndexStart && layerIndex <= LayerIndexEnd).DistinctBy(layer => layer.PositionZ).ToArray();

            /* For the 1st stage, we maintain up to 3 mats, the current layer, the one below us, and the one above us 
             * (below will be null when current layer is 0, above will be null when currentlayer is layercount-1) */
            /* We init the aboveLayer to the first layer, in the loop coming up we shift above->current->below, so this effectively inits current layer */
            Mat aboveLayer = null;
            using (var mat = SlicerFile.LayerManager.GetMergedMatForSequentialPositionedLayers(distinctLayers[0].Index))
            {
                var matRoi = mat.Roi(SlicerFile.BoundingRectangle);

                if (_flipDirection != Enumerations.FlipDirection.None)
                {
                    CvInvoke.Flip(matRoi, matRoi, Enumerations.ToOpenCVFlipType(_flipDirection));
                }

                if (_rotateDirection != Enumerations.RotateDirection.None)
                {
                    CvInvoke.Rotate(matRoi, matRoi, Enumerations.ToOpenCVRotateFlags(_rotateDirection));
                }

                if ((byte)_quality > 1)
                {
                    aboveLayer = new Mat();
                    CvInvoke.Resize(matRoi, aboveLayer, Size.Empty, 1.0 / (int)_quality, 1.0 / (int)_quality, Inter.Area);
                }
                else
                {
                    aboveLayer = matRoi.Clone(); /* clone and then dispose of the ROI mat, not efficient but keeps the GetPixelPos working and clean */
                }

            }

            Mat curLayer = null;
            Mat belowLayer = null;

            /* List of faces to process, great for debugging if you are haveing issues with a face of particular orientation. */
            var facesToCheck = new[] { Voxelizer.FaceOrientation.Front, Voxelizer.FaceOrientation.Back, Voxelizer.FaceOrientation.Left, Voxelizer.FaceOrientation.Right, Voxelizer.FaceOrientation.Top, Voxelizer.FaceOrientation.Bottom };

            /* Init of other objects that will be used in subsequent stages */
            var rootFaces = new Voxelizer.UVFace[distinctLayers.Length];
            var layerFaceCounts = new uint[distinctLayers.Length];
            var layerTrees = new KdTree<float, Voxelizer.UVFace>[distinctLayers.Length];

            progress.Reset("layers", (uint)distinctLayers.Length);
            progress.Title = "Stage 1: Generating faces from layers";
            //progress.ItemCount = LayerRangeCount;

            SlicerFile.LayerManager.GetSamePositionedLayers();

            /* Begin Stage 1, identifying all faces that are visible from outside the model */
            for (uint layerIndex = 0; layerIndex < distinctLayers.Length; layerIndex++)
            {
                Voxelizer.UVFace currentFaceItem = null;

                /* Should contain a list of all found faces on this layer, keyed by the face orientation */
                var foundFaces = new Dictionary<Voxelizer.FaceOrientation, List<Point>>();

                /* move current layer to below */
                belowLayer = curLayer;

                /* move above layer to us */
                curLayer = aboveLayer;

                /* bring in a new aboveLayer if we need to */
                if (layerIndex < distinctLayers.Length - 1)
                {
                    using var mat = SlicerFile.LayerManager.GetMergedMatForSequentialPositionedLayers(distinctLayers[(int)layerIndex+1].Index);
                    var matRoi = mat.Roi(SlicerFile.BoundingRectangle);

                    if (_flipDirection != Enumerations.FlipDirection.None)
                    {
                        CvInvoke.Flip(matRoi, matRoi, Enumerations.ToOpenCVFlipType(_flipDirection));
                    }

                    if (_rotateDirection != Enumerations.RotateDirection.None)
                    {
                        CvInvoke.Rotate(matRoi, matRoi, Enumerations.ToOpenCVRotateFlags(_rotateDirection));
                    }

                    if ((byte)_quality > 1)
                    {
                        aboveLayer = new Mat();
                        CvInvoke.Resize(matRoi, aboveLayer, Size.Empty, 1.0 / (int)_quality, 1.0 / (int)_quality, Inter.Area);
                    }
                    else
                    {
                        aboveLayer = matRoi.Clone();
                    }

                    //CvInvoke.Threshold(aboveLayer, aboveLayer, 1, 255, ThresholdType.Binary);
                }
                else
                {
                    aboveLayer = null;
                }

                /* get image of pixels to do neighbor checks on */
                var voxelLayer = Voxelizer.BuildVoxelLayerImage(curLayer, aboveLayer, belowLayer);
                var voxelSpan = voxelLayer.GetBytePointer();

                /* Seems to be faster to parallel on the Y and not the X */
                Parallel.For(0, curLayer.Height, CoreSettings.ParallelOptions, y =>
                {
                    /* Collects all the faces found for this thread, will be combined into the main dictionary later */
                    var threadDict = new Dictionary<Voxelizer.FaceOrientation, List<Point>>();
                    for (var x = 0; x < curLayer.Width; x++)
                    {
                        if (voxelSpan[voxelLayer.GetPixelPos(x, y)] == 0) continue;

                        var faces = Voxelizer.GetOpenFaces(curLayer, x, y, belowLayer, aboveLayer);
                        if (faces == Voxelizer.FaceOrientation.None) continue;
                        foreach (var face in facesToCheck)
                        {
                            if (!faces.HasFlag(face)) continue;
                            if (!threadDict.ContainsKey(face)) threadDict.Add(face, new());
                            threadDict[face].Add(new Point(x, y));
                        }
                    }

                    /* merge all found faces to main foundFaces dictionary */
                    lock (foundFaces)
                    {
                        foreach (var kvp in threadDict)
                        {
                            if (!foundFaces.ContainsKey(kvp.Key)) foundFaces.Add(kvp.Key, new());
                            lock (foundFaces[kvp.Key]) foundFaces[kvp.Key].AddRange(kvp.Value);
                        }
                    }
                });

                /* Begin stage 2, horizontal combining of coplanar faces */
                foreach (var faceType in facesToCheck)
                {
                    if (foundFaces.ContainsKey(faceType) == false || foundFaces[faceType].Count == 0) continue;

                    if (faceType 
                        is Voxelizer.FaceOrientation.Front 
                        or Voxelizer.FaceOrientation.Back 
                        or Voxelizer.FaceOrientation.Top 
                        or Voxelizer.FaceOrientation.Bottom)
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
                                        rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight};
                                        currentFaceItem = rootFaces[layerIndex];
                                    }
                                    else
                                    {
                                        currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
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
                                    rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                                    currentFaceItem = rootFaces[layerIndex];
                                }
                                else
                                {
                                    currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
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
                            rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                            currentFaceItem = rootFaces[layerIndex];
                        }
                        else
                        {
                            currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curX - startX + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                            currentFaceItem = currentFaceItem.FlatListNext;
                        }
                    }

                    if (faceType is Voxelizer.FaceOrientation.Left or Voxelizer.FaceOrientation.Right)
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
                                        rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                                        currentFaceItem = rootFaces[layerIndex];
                                    }
                                    else
                                    {
                                        currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
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
                                    rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                                    currentFaceItem = rootFaces[layerIndex];
                                }
                                else
                                {
                                    currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
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
                            rootFaces[layerIndex] = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                            currentFaceItem = rootFaces[layerIndex];
                        }
                        else
                        {
                            currentFaceItem.FlatListNext = new Voxelizer.UVFace { LayerIndex = layerIndex, Type = faceType, FaceRect = new Rectangle(startX, startY, curY - startY + 1, 1), LayerHeight = distinctLayers[(int)layerIndex].LayerHeight };
                            currentFaceItem = currentFaceItem.FlatListNext;
                        }
                    }
                }

                progress.LockAndIncrement();

                if (progress.Token.IsCancellationRequested)
                {
                    Cleanup();
                    return false;
                }

            }

            progress.Title = "Stage 2: Building KD Trees";
            progress.ProcessedItems = 0;

            /* We build out a 3 dimensional KD tree for each layer, having 1 big KD tree is prohibitive when you get to millions and millions of faces. */
            Parallel.For(0, distinctLayers.Length, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                /* Create the KD tree for the layer, in practice there should never be dups, but just in case, set to skip */
                layerTrees[layerIndex] = new KdTree<float, Voxelizer.UVFace>(3, new FloatMath(), AddDuplicateBehavior.Skip);

                /* Walk the linked list of UVFaces, adding them to the tree */
                var currentFaceItem = rootFaces[layerIndex];
                if (currentFaceItem is null) return;
                while (currentFaceItem.FlatListNext is not null)
                {
                    layerTrees[layerIndex].Add(new[] { (float)currentFaceItem.Type, currentFaceItem.FaceRect.X, currentFaceItem.FaceRect.Y }, currentFaceItem);
                    currentFaceItem = currentFaceItem.FlatListNext;
                }
                layerTrees[layerIndex].Add(new[] { (float)currentFaceItem.Type, currentFaceItem.FaceRect.X, currentFaceItem.FaceRect.Y }, currentFaceItem);

                progress.LockAndIncrement();
            });

            if (progress.Token.IsCancellationRequested)
            {
                Cleanup();
                return false;
            }

            progress.Title = "Stage 3: Collapsing faces";
            progress.ProcessedItems = 0;
            long collapseCount = 0;

            /* Begin Stage 3: Vertical collapse 
             * Since we don't modify the lists/objects and only connect them via doubly linked list
             * we can process each layer independant of the others.
             */
            Parallel.For(0, distinctLayers.Length, i =>
            {
                if (progress.Token.IsCancellationRequested)
                {
                    return;
                }

                /* if no faces on this layer... skip.... needed for empty layers */
                if (layerTrees[i] is null) return;

                /* check each point in the current layers tree */
                foreach (var point in layerTrees[i])
                {
                    /* if this point already has a parent, skip */
                    if (point.Value.Parent is not null) continue;


                    /* deterimine the point below to check. 
                     * For front/back/left/right its the same X/Y point and Z is different, and Z is done basically by looking at the layer tree below us 
                     * For Top/Bottom its a bit different, the Z stays the same (we query our own layer tree) but the Y coordinate is 1 less */

                    float[] pointBelow = null;
                    KdTree<float, Voxelizer.UVFace> treeBelow = null;
                    if (point.Value.Type is Voxelizer.FaceOrientation.Top or Voxelizer.FaceOrientation.Bottom)
                    {
                        if (point.Value.Type == Voxelizer.FaceOrientation.Top)
                        {
                            pointBelow = new[] { point.Point[0], point.Point[1], point.Point[2] - 1 };
                        }
                        else
                        {
                            pointBelow = new[] { point.Point[0], point.Point[1], point.Point[2] - 1 };
                        }
                        treeBelow = layerTrees[i];
                    }
                    else
                    {
                        pointBelow = new[] { point.Point[0], point.Point[1], point.Point[2] };
                        if (i > 0)
                        {
                            treeBelow = layerTrees[i - 1];
                        }
                    }

                    var faceBelow = treeBelow?.FindValueAt(pointBelow);
                    if (faceBelow is null) continue;
                    /* if we find a face below us it has to be the same width too */
                    if (point.Value.FaceRect.Width == faceBelow.FaceRect.Width)
                    {
                        /* same coordinate, same width, safe to merge together. Do so by doubly linking the items */
                        point.Value.Parent = faceBelow;
                        faceBelow.Child = point.Value;
                        collapseCount++;
                    }
                }
                progress.LockAndIncrement();
            });

            if (progress.Token.IsCancellationRequested)
            {
                Cleanup();
                return false;
            }

            progress.Title = "Stage 4: Generating STL";
            progress.ProcessedItems = 0;


            using var mesh = fileExtension.FileFormatType.CreateInstance<MeshFile>(_filePath, FileMode.Create);
            mesh.BeginWrite();

            /* Begin Stage 4, generating triangles and saving to STL */
            for (var treeIndex = 0; treeIndex < layerTrees.Length; treeIndex++) {
                var tree = layerTrees[treeIndex];
                if (tree is null) continue;

                /* only process UVFaces that do not have a parent, these are the "root" faces that couldn't be combined with something above them */
                foreach (var p in tree.Where(p => p.Value.Parent is null))
                {
                    /* generate the triangles */
                    foreach (var f in Voxelizer.MakeFacetsForUVFace(p.Value, xWidth, yWidth,distinctLayers[treeIndex].PositionZ))
                    {
                        /* write to file */
                        mesh.WriteTriangle(f.p1, f.p2, f.p3, f.normal);
                    }
                }

                /* check for cancellation at every layer, and if so, close the STL file properly */
                if (progress.Token.IsCancellationRequested)
                {
                    Cleanup();
                    return false;
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
                    if (rootFaces[x] is not null) rootFaces[x].FlatListNext = null;
                    rootFaces[x] = null;
                }
                rootFaces = null;
                GC.Collect();
            }

            mesh.EndWrite();


            return !progress.Token.IsCancellationRequested;
        }


        #endregion

        #region Equality

        private bool Equals(OperationLayerExportMesh other)
        {
            return _filePath == other._filePath && _rotateDirection == other._rotateDirection && _flipDirection == other._flipDirection;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationLayerExportMesh other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_filePath, (int)_rotateDirection, (int)_flipDirection);
        }

        #endregion
    }
}
