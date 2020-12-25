/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRaftRelief : Operation
    {
        private RaftReliefTypes _reliefType = RaftReliefTypes.Relief;
        private byte _dilateIterations = 10;
        private byte _wallMargin = 20;
        private byte _holeDiameter = 50;
        private byte _holeSpacing = 20;
        public override string Title => "Raft relief";
        public override string Description =>
            "Relief raft by adding holes in between to reduce FEP suction, save resin and easier to remove the prints.";

        public override string ConfirmationText =>
            $"relief the raft";

        public override string ProgressTitle =>
            $"Relieving raft";

        public override string ProgressAction => "Relieved layers";

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection =>
            Enumerations.LayerRangeSelection.None;


        public OperationRaftRelief()
        {
        }

        public override string ToString()
        {
            var result = $"[{_reliefType}] [Dilate: {_dilateIterations}] [Wall margin: {_wallMargin}] [Hole diameter: {_holeDiameter}] [Hole spacing: {_holeSpacing}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        public enum RaftReliefTypes : byte
        {
            Relief,
            Decimate
        }

        public static Array RaftReliefItems => Enum.GetValues(typeof(RaftReliefTypes));

        public RaftReliefTypes ReliefType
        {
            get => _reliefType;
            set => RaiseAndSetIfChanged(ref _reliefType, value);
        }

        public byte DilateIterations
        {
            get => _dilateIterations;
            set => RaiseAndSetIfChanged(ref _dilateIterations, value);
        }

        public byte WallMargin
        {
            get => _wallMargin;
            set => RaiseAndSetIfChanged(ref _wallMargin, value);
        }

        public byte HoleDiameter
        {
            get => _holeDiameter;
            set => RaiseAndSetIfChanged(ref _holeDiameter, value);
        }

        public byte HoleSpacing
        {
            get => _holeSpacing;
            set => RaiseAndSetIfChanged(ref _holeSpacing, value);
        }

        #region Equality

        protected bool Equals(OperationRaftRelief other)
        {
            return _reliefType == other._reliefType && _dilateIterations == other._dilateIterations && _holeDiameter == other._holeDiameter && _holeSpacing == other._holeSpacing && _wallMargin == other._wallMargin;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationRaftRelief) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) _reliefType;
                hashCode = (hashCode * 397) ^ _dilateIterations.GetHashCode();
                hashCode = (hashCode * 397) ^ _holeDiameter.GetHashCode();
                hashCode = (hashCode * 397) ^ _holeSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ _wallMargin.GetHashCode();
                return hashCode;
            }
        }

        #endregion
    }
}
