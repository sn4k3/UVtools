/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRaiseOnPrintFinish : Operation
    {
        #region Constants
        public const byte DummyPixelBrightness = 128;
        #endregion

        #region Members
        private decimal _positionZ;

        private bool _outputDummyPixel = true;
        #endregion

        #region Overrides
        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Raise platform on print finish";
        public override string Description =>
            "Raise the build platform to a set position after finish the print.\n\n" +
            "NOTE: Only use this tool once and if your printer firmware don't already raise the build platform after finish the print.\n" +
            "This will create a \"empty\" layer on end to simulate a print at a defined height.\n" +
            "Not compatible with all printers, still it won't cause any harm if printer don't support this strategy.";

        public override string ConfirmationText =>
            $"raise the platform on print finish to Z={_positionZ}mm";

        public override string ProgressTitle =>
            $"Inserting dummy layer on end";

        public override string ProgressAction => "Inserted layer";

        public override string ValidateSpawn()
        {
            if(!SlicerFile.CanUseLayerLiftHeight)
            {
                return NotSupportedMessage;
            }

            return null;
        }

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (!ValidateSpawn(out var message))
            {
                sb.AppendLine(message);
            }
            if((float)_positionZ < SlicerFile.PrintHeight)
            {
                sb.AppendLine($"Can't raise to {_positionZ}mm, because it's below the maximum print height of {SlicerFile.PrintHeight}mm.");
            }
            else if ((float)_positionZ == SlicerFile.PrintHeight)
            {
                sb.AppendLine($"Raise to {_positionZ}mm will have no effect because it's the same height as last layer of {SlicerFile.PrintHeight}mm.");
            }
            
            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Z={_positionZ}mm] [Dummy pixel: {_outputDummyPixel}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Properties

        public float MinimumPositionZ => Layer.RoundHeight(SlicerFile.PrintHeight + SlicerFile.LayerHeight);

        /// <summary>
        /// Sets or gets the Z position to raise to
        /// </summary>
        public decimal PositionZ
        {
            get => _positionZ;
            set => RaiseAndSetIfChanged(ref _positionZ, Layer.RoundHeight(value));
        }

        /// <summary>
        /// True to output a dummy pixel on bounding rectangle position to avoid empty layer and blank image, otherwise set to false
        /// </summary>
        public bool OutputDummyPixel 
        {
            get => _outputDummyPixel; 
            set => RaiseAndSetIfChanged(ref _outputDummyPixel, value); 
        }
        #endregion

        #region Constructor

        public OperationRaiseOnPrintFinish() 
        {
            //_outputDummyPixel = !SlicerFile.SupportsGCode;
        }

        public OperationRaiseOnPrintFinish(FileFormat slicerFile) : base(slicerFile) 
        {
            if (_positionZ <= 0) _positionZ = (decimal)SlicerFile.MachineZ;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationRaiseOnPrintFinish other)
        {
            return _positionZ == other._positionZ && _outputDummyPixel == other._outputDummyPixel;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationRaiseOnPrintFinish) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_positionZ, _outputDummyPixel);
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            return Execute(null, null);
            //return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            var layer = SlicerFile.LastLayer.Clone();
            layer.PositionZ = (float)_positionZ;
            layer.ExposureTime = 0.05f; // Very low exposure time
            layer.LightPWM = 0; // Try to disable light if possible
            layer.SetNoDelays();
            using var newMat = EmguExtensions.InitMat(SlicerFile.Resolution);
            if(_outputDummyPixel)
            {
                newMat.SetByte(newMat.GetPixelPos(layer.BoundingRectangle.Center()), DummyPixelBrightness);
            }

            layer.LayerMat = newMat;

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerManager.Append(layer);
                return true;
            });
            
            return true;
        }

        #endregion
    }
}
