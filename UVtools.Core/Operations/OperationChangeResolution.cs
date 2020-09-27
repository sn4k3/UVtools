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
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public sealed class OperationChangeResolution : Operation
    {
        private uint _newResolutionX;
        private uint _newResolutionY;

        #region Subclasses
        public class Resolution
        {
            public uint ResolutionX { get; }
            public uint ResolutionY { get; }
            public string Name { get; }
            public bool IsEmpty => ResolutionX == 0 && ResolutionY == 0;

            public Resolution(uint resolutionX, uint resolutionY, string name = null)
            {
                ResolutionX = resolutionX;
                ResolutionY = resolutionY;
                Name = name;
            }

            public override string ToString()
            {
                if(IsEmpty) return string.Empty;
                var str = $"{ResolutionX} x {ResolutionY}";
                if (!string.IsNullOrEmpty(Name))
                {
                    str += $" ({Name})";
                }
                return str;
            }
        }
        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection LayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI { get; set; } = false;
        public override string Title => "Change print resolution";
        public override string Description =>
            "Crops or resizes all layer images to fit an alternate print area\n\n" +
            "Useful to make files printable on a different printer than they were originally sliced for without the need to re-slice.\n\n" +
            "NOTE: Please ensure that the actual model will fit within the new print resolution. The operation will be aborted if it will result in any of the actual model being clipped.";

        public override string ConfirmationText => 
            "change print resolution " +
            $"from {OldResolution.Width}x{OldResolution.Height} " +
            $"to {NewResolutionX}x{NewResolutionY}?";

        public override string ProgressTitle =>
            $"Changing print resolution from ({OldResolution.Width}x{OldResolution.Height}) to ({NewResolutionX}x{NewResolutionY})";

        public override string ProgressAction => "Changed layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (OldResolution.Width == NewResolutionX && OldResolution.Height == NewResolutionY)
            {
                sb.AppendLine($"The new resolution must be different from current resolution ({OldResolution.Width} x {OldResolution.Height}).");
            }

            if (NewResolutionX < VolumeBonds.Width || NewResolutionY < VolumeBonds.Height)
            {
                sb.AppendLine($"The new resolution ({NewResolutionX} x {NewResolutionY}) is not large enough to hold the model volume ({VolumeBonds.Width} x {VolumeBonds.Height}), continuing operation would clip the model");
                sb.AppendLine("To fix this, try to rotate the object and/or resize to fit on this new resolution.");
            }

            return new StringTag(sb.ToString());
        }

        #endregion

        #region Properties
        public Size OldResolution { get; }

        public uint NewResolutionX
        {
            get => _newResolutionX;
            set => SetProperty(ref _newResolutionX, value);
        }

        public uint NewResolutionY
        {
            get => _newResolutionY;
            set => SetProperty(ref _newResolutionY, value);
        }

        public Rectangle VolumeBonds { get; }

        public Size VolumeBondsSize => VolumeBonds.Size;

        #endregion

        #region Constructor

        public OperationChangeResolution()
        {
        }

        public OperationChangeResolution(Size oldResolution, Rectangle volumeBonds)
        {
            OldResolution = oldResolution;
            VolumeBonds = volumeBonds;
            NewResolutionX = (uint) oldResolution.Width;
            NewResolutionY = (uint) oldResolution.Height;
        }
        #endregion

        #region Methods
        public static Resolution[] GetResolutions()
        {
            return new [] {
                //new Resolution(0, 0, string.Empty),
                new Resolution(854, 480, "FWVGA"),
                new Resolution(960, 1708),
                new Resolution(1080, 1920, "FHD"),
                new Resolution(1440, 2560, "QHD"),
                new Resolution(1600, 2560, "WQXGA"),
                new Resolution(1620, 2560, "WQXGA"),
                new Resolution(1920, 1080, "FHD"),
                new Resolution(2160, 3840, "4K UHD"),
                new Resolution(2531, 1410, "QHD"),
                new Resolution(2560, 1440, "QHD"),
                new Resolution(2560, 1600, "WQXGA"),
                new Resolution(2560, 1620, "WQXGA"),
                new Resolution(3840, 2160, "4K UHD"),
                new Resolution(3840, 2400, "WQUXGA"),
            };
        }

        public static Resolution[] Presets => GetResolutions();

        #endregion

    }
}
