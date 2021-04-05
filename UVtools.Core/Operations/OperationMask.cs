/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationMask : Operation
    {
        #region Overrides
        public override string Title => "Mask";
        public override string Description =>
            "Mask the intensity of the LCD output using a greyscale input image.\n\n" +
            "Useful to correct LCD light uniformity for a specific printer.\n\n" +
            "NOTE:  This operation should be run only after repairs and other transformations.  The provided" +
            "input mask image must match the output resolution of the target printer.";

        public override string ConfirmationText =>
            $"mask layers from {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressTitle =>
            $"Masking layers from {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Masked layers";

        public override bool CanHaveProfiles => false;

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();
            if (Mask is null)
            {
                sb.AppendLine("The mask can not be empty.");
            }

            return sb.ToString();
        }
        #endregion

        #region Properties
        [XmlIgnore]
        public Mat Mask { get; set; }

        public bool HaveMask => !(Mask is null);
        #endregion

        #region Constructor

        public OperationMask() { }

        public OperationMask(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        public void InvertMask()
        {
            if (!HaveMask) return;
            CvInvoke.BitwiseNot(Mask, Mask);
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat);
                SlicerFile[layerIndex].LayerMat = mat;
                progress.LockAndIncrement();
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            var target = GetRoiOrDefault(mat);
            using var mask = GetMask(mat);
            if (Mask.Size != target.Size) return false;
            CvInvoke.BitwiseAnd(target, Mask, target, mask);
            return true;
        }

        #endregion
    }
}
