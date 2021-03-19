/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationSolidify : Operation
    {
        #region Enums
        public enum AreaCheckTypes
        {
            More,
            Less
        }
        #endregion

        #region Members
        private uint _minimumArea = 1;
        private AreaCheckTypes _areaCheckType = AreaCheckTypes.More;
        #endregion

        #region Overrides

        public override string Title => "Solidify";

        public override string Description =>
            "Solidifies the selected layers, closing all interior holes.\n\n" +
            "NOTE: All open areas of the layer that are completely surrounded by pixels will be filled. Please ensure that none of the holes in the layer are required before proceeding.";

        public override string ConfirmationText =>
            $"solidify layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Solidifying layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Solidified layers";

        /// <summary>
        /// Gets the minimum required area to solidify it
        /// </summary>
        public uint MinimumArea
        {
            get => _minimumArea;
            set => RaiseAndSetIfChanged(ref _minimumArea, Math.Max(1, value));
        }

        public AreaCheckTypes AreaCheckType
        {
            get => _areaCheckType;
            set => RaiseAndSetIfChanged(ref _areaCheckType, value);
        }

        public static Array AreaCheckTypeItems => Enum.GetValues(typeof(AreaCheckTypes));

        public override string ToString()
        {
            var result = $"[Area: ={_areaCheckType} than {_minimumArea}px²]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Constructor

        public OperationSolidify() { }

        public OperationSolidify(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat);
                SlicerFile[layerIndex].LayerMat = mat;
                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            using Mat filteredMat = new();
            using VectorOfVectorOfPoint contours = new();
            using Mat hierarchy = new();
            using var original = mat.Clone();
            var target = GetRoiOrDefault(mat);

            CvInvoke.Threshold(target, filteredMat, 127, 255, ThresholdType.Binary); // Clean AA
            CvInvoke.FindContours(filteredMat, contours, hierarchy, RetrType.Ccomp, ChainApproxMethod.ChainApproxSimple);
            var arr = hierarchy.GetData();
            for (int i = 0; i < contours.Size; i++)
            {
                if ((int)arr.GetValue(0, i, 2) != -1 || (int)arr.GetValue(0, i, 3) == -1) continue;
                if (MinimumArea >= 1)
                {
                    var rectangle = CvInvoke.BoundingRectangle(contours[i]);
                    if (AreaCheckType == AreaCheckTypes.More)
                    {
                        if (rectangle.Area() < MinimumArea) continue;
                    }
                    else
                    {
                        if (rectangle.Area() > MinimumArea) continue;
                    }

                }

                CvInvoke.DrawContours(target, contours, i, EmguExtensions.WhiteByte, -1);
            }

            ApplyMask(original, mat);

            return true;
        }

        #endregion
    }
}
