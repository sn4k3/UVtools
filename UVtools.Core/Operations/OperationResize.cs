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
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationResize : Operation
    {
        #region Members
        private decimal _x = 100;
        private decimal _y = 100;
        private bool _constrainXy;
        private bool _isFade;
        #endregion

        #region Overrides
        public override string Title => "Resize";
        public override string Description =>
            "Resize the model by a percentage in the X/Y plane.\n\n" +
            "NOTE: This operation is applied based on the original orientation of the layers in the model file. " +
            "If the image is rotated 90° in the Layer Preview, you will need to compensate by inverting " +
            "the X and Y values.  The print bounds will also not be validated as part of this operation, so please " +
            "ensure that the scaling factor does not result in the model being clipped.";

        public override string ConfirmationText =>
            $"resize model layers {LayerIndexStart} through {LayerIndexEnd} by " +
            $"X={X}% and Y={Y}%";

        public override string ProgressTitle =>
            $"Resizing from layers {LayerIndexStart} to {LayerIndexEnd} at X:{X}%  Y:{Y}% ";

        public override string ProgressAction => "Resized layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (X == 100m && Y == 100m)
            {
                sb.AppendLine("X and Y can't both be 100%.");
            }

            return new StringTag(sb.ToString());
        }
        #endregion

        #region Properties
        public decimal X
        {
            get => _x;
            set
            {
                RaiseAndSetIfChanged(ref _x, value);
                if (_constrainXy)
                    Y = value;
                RaisePropertyChanged(nameof(XScale));
            }
        }

        public decimal XScale => _x / 100m;
        public decimal YScale => _y / 100m;

        public decimal Y
        {
            get => _y;
            set
            {
                if(!RaiseAndSetIfChanged(ref _y, value)) return;
                RaisePropertyChanged(nameof(YScale));
            }
        }

        public bool ConstrainXY
        {
            get => _constrainXy;
            set
            {
                if (!RaiseAndSetIfChanged(ref _constrainXy, value)) return;
                if (_constrainXy)
                {
                    Y = _x;
                }
            }
        }

        public bool IsFade
        {
            get => _isFade;
            set => RaiseAndSetIfChanged(ref _isFade, value);
        }
        #endregion

        #region Constructor

        public OperationResize() { }

        public OperationResize(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        public override string ToString()
        {
            var result = $"[X: {_x}%, Y: {_y}%] [Fade: {_isFade}] [Constrain: {_constrainXy}]"+ LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #region Equality

        protected bool Equals(OperationResize other)
        {
            return _x == other._x && _y == other._y && _constrainXy == other._constrainXy && _isFade == other._isFade;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((OperationResize) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _x.GetHashCode();
                hashCode = (hashCode * 397) ^ _y.GetHashCode();
                hashCode = (hashCode * 397) ^ _constrainXy.GetHashCode();
                hashCode = (hashCode * 397) ^ _isFade.GetHashCode();
                return hashCode;
            }
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            if (X == 1m && Y == 1m) return false;

            decimal xSteps = Math.Abs(X - 1) / (LayerIndexEnd - LayerIndexStart);
            decimal ySteps = Math.Abs(Y - 1) / (LayerIndexEnd - LayerIndexStart);

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                var newX = X;
                var newY = Y;
                if (IsFade)
                {
                    if (newX != 1m)
                    {

                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newX = newX < 1m
                            ? newX + (layerIndex - LayerIndexStart) * xSteps
                            : newX - (layerIndex - LayerIndexStart) * xSteps;

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }

                    if (newY != 1m)
                    {

                        //maxIteration = Math.Max(iterationsStart, iterationsEnd);

                        newY = (newY < 1m
                            ? newY + (layerIndex - LayerIndexStart) * ySteps
                            : newY - (layerIndex - LayerIndexStart) * ySteps);

                        // constrain
                        //iterations = Math.Min(Math.Max(1, iterations), maxIteration);
                    }
                }

                lock (progress.Mutex)
                {
                    progress++;
                }

                if (newX == 1.0m && newY == 1.0m) return;

                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat, newX  / 100m, newY / 100m);
                SlicerFile[layerIndex].LayerMat = mat;
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            var xScale = XScale;
            var yScale = YScale;
            if (arguments is not null && arguments.Length >= 2)
            {
                xScale = (decimal) arguments[0];
                yScale = (decimal) arguments[1];
            }

            var target = GetRoiOrDefault(mat);
            target.TransformFromCenter((double) xScale, (double) yScale);
            return true;
        }

        #endregion
    }
}
