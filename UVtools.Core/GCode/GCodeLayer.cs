/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.GCode
{
    public class GCodeLayer
    {
        private float? _waitTimeBeforeCure;
        private float? _exposureTime;
        private float? _waitTimeAfterCure;
        private float? _liftHeight;
        private float? _liftSpeed;
        private float? _waitTimeAfterLift;
        private float? _retractSpeed;

        public enum GCodeLastParsedLine : byte
        {
            LayerIndex,
        }

        public bool IsValid => LayerIndex.HasValue;

        public FileFormat SlicerFile { get; }
        public uint? LayerIndex { get; set; }
        public float? PositionZ { get; set; }

        public float? WaitTimeBeforeCure
        {
            get => _waitTimeBeforeCure;
            set => _waitTimeBeforeCure = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? ExposureTime
        {
            get => _exposureTime;
            set => _exposureTime = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? WaitTimeAfterCure
        {
            get => _waitTimeAfterCure;
            set => _waitTimeAfterCure = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? LiftHeight
        {
            get => _liftHeight;
            set => _liftHeight = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? LiftSpeed
        {
            get => _liftSpeed;
            set => _liftSpeed = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? WaitTimeAfterLift
        {
            get => _waitTimeAfterLift;
            set => _waitTimeAfterLift = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public float? RetractSpeed
        {
            get => _retractSpeed;
            set => _retractSpeed = value is null ? null : (float)Math.Round(value.Value, 2);
        }

        public byte? LightPWM { get; set; }

        public bool IsExposing => LightPWM.HasValue && !IsAfterLightOff;

        public bool IsAfterLightOff { get; set; }

        public GCodeLayer(FileFormat slicerFile)
        {
            SlicerFile = slicerFile;
        }

        public void Init()
        {
            LayerIndex = null;
            PositionZ = null;
            WaitTimeBeforeCure = null;
            ExposureTime = null;
            WaitTimeAfterCure = null;
            LiftHeight = null;
            LiftSpeed = null;
            WaitTimeAfterLift = null;
            RetractSpeed = null;
            LightPWM = null;
            IsAfterLightOff = false;
        }

        /// <summary>
        /// Set gathered data to the layer
        /// </summary>
        public void SetLayer()
        {
            if (!IsValid) return;
            uint layerIndex = LayerIndex.Value;
            var layer = SlicerFile[layerIndex];

            if(PositionZ.HasValue) layer.PositionZ = PositionZ.Value;
            layer.WaitTimeBeforeCure = WaitTimeBeforeCure ?? 0;
            layer.ExposureTime = ExposureTime ?? SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomExposureTime, SlicerFile.ExposureTime);
            layer.WaitTimeAfterCure = WaitTimeAfterCure ?? 0;
            layer.LiftHeight = LiftHeight ?? 0;
            layer.LiftSpeed = LiftSpeed ?? SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
            layer.WaitTimeAfterLift = WaitTimeAfterLift ?? 0;
            layer.RetractSpeed = RetractSpeed ?? SlicerFile.RetractSpeed;
            layer.LightPWM = LightPWM ?? 0;//SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);

            if (SlicerFile.GCode.SyncMovementsWithDelay) // Dirty fix of the value
            {
                var syncTime = OperationCalculator.LightOffDelayC.CalculateSeconds(layer.LiftHeight, layer.LiftSpeed, layer.RetractSpeed, 1.5f);
                if (syncTime < layer.WaitTimeBeforeCure)
                {
                    layer.WaitTimeBeforeCure = (float) Math.Round(layer.WaitTimeBeforeCure - syncTime, 2);
                }
            }
        }
    }
}
