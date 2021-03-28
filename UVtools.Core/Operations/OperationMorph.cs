/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationMorph : Operation
    {
        #region Members
        private MorphOp _morphOperation = MorphOp.Erode;
        private uint _iterationsStart = 1;
        private uint _iterationsEnd = 1;
        private bool _chamfer;
        #endregion
        
        #region Overrides

        public override string Title => "Morph";
        public override string Description =>
            $"Morph Model - " +
            $"Various operations that can be used to change the physical structure of the model or individual layers.";
        public override string ConfirmationText =>
            $"morph model layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Morphing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Morphed layers";

        #endregion

        #region Properties


        public static StringTag[] MorphOperations => new[]
        {
            new StringTag("Erode - Contracts the boundaries within the object", MorphOp.Erode),
            new StringTag("Dilate - Expands the boundaries within the object", MorphOp.Dilate),
            new StringTag("Gap Closing - Closes small holes inside the objects", MorphOp.Close),
            new StringTag("Noise Removal - Removes small isolated pixels", MorphOp.Open),
            new StringTag("Gradient - Removes the interior areas of objects", MorphOp.Gradient),
        };

        public byte MorphOperationIndex
        {
            get
            {
                for (byte i = 0; i < MorphOperations.Length; i++)
                {
                    if ((MorphOp) MorphOperations[i].Tag == MorphOperation) return i;
                }

                return 0;
            }
        }

        public MorphOp MorphOperation
        {
            get => _morphOperation;
            set => RaiseAndSetIfChanged(ref _morphOperation, value);
        }
        
        public uint Iterations
        {
            get => IterationsStart;
            set => IterationsStart = IterationsEnd = value;
        }

        public uint IterationsStart
        {
            get => _iterationsStart;
            set => RaiseAndSetIfChanged(ref _iterationsStart, value);
        }

        public uint IterationsEnd
        {
            get => _iterationsEnd;
            set => RaiseAndSetIfChanged(ref _iterationsEnd, value);
        }

        public bool Chamfer
        {
            get => _chamfer;
            set => RaiseAndSetIfChanged(ref _chamfer, value);
        }

        [XmlIgnore]
        public Kernel Kernel { get; set; } = new();

        public override string ToString()
        {
            var result = $"[{_morphOperation}] [Iterations: {_iterationsStart}/{_iterationsEnd}] [Chamfer: {_chamfer}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Constructor

        public OperationMorph() { }

        public OperationMorph(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Equality

        private bool Equals(OperationMorph other)
        {
            return _morphOperation == other._morphOperation && _iterationsStart == other._iterationsStart && _iterationsEnd == other._iterationsEnd && _chamfer == other._chamfer;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationMorph other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) _morphOperation;
                hashCode = (hashCode * 397) ^ (int) _iterationsStart;
                hashCode = (hashCode * 397) ^ (int) _iterationsEnd;
                hashCode = (hashCode * 397) ^ _chamfer.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var isFade = Chamfer;
            LayerManager.MutateGetVarsIterationChamfer(
                LayerIndexStart,
                LayerIndexEnd,
                (int)IterationsStart,
                (int)IterationsEnd,
                ref isFade,
                out var iterationSteps,
                out var maxIteration
            );

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1,
                //new ParallelOptions {MaxDegreeOfParallelism = 1},
                layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    int iterations = LayerManager.MutateGetIterationVar(isFade, (int)IterationsStart, (int)IterationsEnd, iterationSteps, maxIteration, LayerIndexStart, (uint)layerIndex);

                    using var mat = SlicerFile[layerIndex].LayerMat;
                    Execute(mat, iterations);
                    SlicerFile[layerIndex].LayerMat = mat;

                    progress.LockAndIncrement();
                });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            int iterations = (int) _iterationsStart;
            if (arguments is not null && arguments.Length >= 1)
            {
                iterations = (int) arguments[0];
            }

            using var original = mat.Clone();
            var target = GetRoiOrDefault(mat);
            CvInvoke.MorphologyEx(target, target, MorphOperation, Kernel.Matrix, Kernel.Anchor, iterations, BorderType.Reflect101, default);
            ApplyMask(original, mat);
            return true;
        }

        #endregion
    }
}
