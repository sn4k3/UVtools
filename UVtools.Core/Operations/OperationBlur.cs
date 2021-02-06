/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationBlur : Operation
    {
        private BlurAlgorithm _blurOperation = BlurAlgorithm.Blur;
        private uint _size = 1;

        #region Overrides

        public override string Title => "Blur";
        public override string Description =>
            $"Blur layer images by applying a low pass filter\n\n" +
            "NOTE: Target printer must support AntiAliasing in order to use this function.\n\n" +
            "See https://docs.opencv.org/master/d4/d13/tutorial_py_filtering.html";

        public override string ConfirmationText =>
            $"blur model with {BlurOperation} from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Bluring model with {BlurOperation} from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Blured layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (BlurOperation == BlurAlgorithm.GaussianBlur ||
                BlurOperation == BlurAlgorithm.MedianBlur)
            {
                if (Size % 2 != 1)
                {
                    sb.AppendLine("Size must be a odd number.");
                }
            }

            if (BlurOperation == BlurAlgorithm.Filter2D)
            {
                if (Kernel is null)
                {
                    sb.AppendLine("Kernel can not be empty.");
                }
            }

            return new StringTag(sb.ToString());
        }

        #endregion

        #region Enums
        public enum BlurAlgorithm
        {
            Blur,
            Pyramid,
            MedianBlur,
            GaussianBlur,
            Filter2D
        }
        #endregion

        #region Properties

        public static StringTag[] BlurTypes => new[]
        {
            new StringTag("Blur: Normalized box filter", BlurAlgorithm.Blur),
            new StringTag("Pyramid: Down/up-sampling step of Gaussian pyramid decomposition", BlurAlgorithm.Pyramid),
            new StringTag("Median Blur: Each pixel becomes the median of its surrounding pixels", BlurAlgorithm.MedianBlur),
            new StringTag("Gaussian Blur: Each pixel is a sum of fractions of each pixel in its neighborhood", BlurAlgorithm.GaussianBlur),
            new StringTag("Filter 2D: Applies an arbitrary linear filter to an image", BlurAlgorithm.Filter2D),
        };

        public byte BlurTypeIndex
        {
            get
            {
                for (byte i = 0; i < BlurTypes.Length; i++)
                {
                    if ((BlurAlgorithm)BlurTypes[i].Tag == BlurOperation) return i;
                }

                return 0;
            }
        }

        public BlurAlgorithm BlurOperation
        {
            get => _blurOperation;
            set => RaiseAndSetIfChanged(ref _blurOperation, value);
        }

        public uint Size
        {
            get => _size;
            set => RaiseAndSetIfChanged(ref _size, value);
        }

        [XmlIgnore]
        public Kernel Kernel { get; set; } = new Kernel();

        public override string ToString()
        {
            var result = $"[{_blurOperation}] [Size: {_size}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Constructor

        public OperationBlur() { }

        public OperationBlur(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        { 
            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using (var mat = SlicerFile[layerIndex].LayerMat)
                {
                    Execute(mat);
                    SlicerFile[layerIndex].LayerMat = mat;
                }
                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            Size size = new Size((int)Size, (int)Size);
            Point anchor = Kernel.Anchor;
            if (anchor.IsEmpty) anchor = new Point(-1, -1);
            //if (size.IsEmpty) size = new Size(3, 3);
            //if (anchor.IsEmpty) anchor = new Point(-1, -1);
            Mat target = GetRoiOrDefault(mat);
            switch (BlurOperation)
            {
                case BlurAlgorithm.Blur:
                    CvInvoke.Blur(target, target, size, Kernel.Anchor);
                    break;
                case BlurAlgorithm.Pyramid:
                    CvInvoke.PyrDown(target, target);
                    CvInvoke.PyrUp(target, target);
                    break;
                case BlurAlgorithm.MedianBlur:
                    CvInvoke.MedianBlur(target, target, (int)Size);
                    break;
                case BlurAlgorithm.GaussianBlur:
                    CvInvoke.GaussianBlur(target, target, size, 0);
                    break;
                case BlurAlgorithm.Filter2D:
                    CvInvoke.Filter2D(target, target, Kernel.Matrix, anchor);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return true;
        }


        #endregion

        #region Equality
        private bool Equals(OperationBlur other)
        {
            return _blurOperation == other._blurOperation && _size == other._size;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationBlur other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int) _blurOperation * 397) ^ (int) _size;
            }
        }
        #endregion
    }
}
