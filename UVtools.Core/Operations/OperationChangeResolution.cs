using System.Drawing;
using System.Text;

namespace UVtools.Core.Operations
{
    public sealed class OperationChangeResolution : Operation
    {
        public class Resolution
        {
            public uint ResolutionX { get; }
            public uint ResolutionY { get; }
            public string Name { get; }

            public Resolution(uint resolutionX, uint resolutionY, string name = null)
            {
                ResolutionX = resolutionX;
                ResolutionY = resolutionY;
                Name = name;
            }

            public override string ToString()
            {
                var str = $"{ResolutionX} x {ResolutionY}";
                if (!string.IsNullOrEmpty(Name))
                {
                    str += $" ({Name})";
                }
                return str;
            }
        }

        public uint OldResolutionX { get; }
        public uint OldResolutionY { get; }

        public uint NewResolutionX { get; set; }
        public uint NewResolutionY { get; set; }
        public Rectangle VolumeBonds { get; }

        public override string ConfirmationText => "Are you sure you want change file resolution?\n" +
                                                   $"From: {OldResolutionX} x {OldResolutionY}\n" +
                                                   $"To: {NewResolutionX} x {NewResolutionY}";

        public OperationChangeResolution(uint oldResolutionX, uint oldResolutionY, Rectangle volumeBonds)
        {
            OldResolutionX = oldResolutionX;
            OldResolutionY = oldResolutionY;
            VolumeBonds = volumeBonds;
        }


        public static Resolution[] GetResolutions()
        {
            return new [] {
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

        public override string Validate()
        {
            var sb = new StringBuilder();
            if (OldResolutionX == NewResolutionX && OldResolutionY == NewResolutionY)
            {
                sb.AppendLine($"The new resolution must be different from current resolution ({OldResolutionX} x {OldResolutionY}).");
            }

            if (NewResolutionX < VolumeBonds.Width || NewResolutionY < VolumeBonds.Height)
            {
                sb.AppendLine($"The new resolution ({NewResolutionX} x {NewResolutionY}) is not enough to accommodate the object volume ({VolumeBonds.Width} x {VolumeBonds.Height}), continuing operation would cut the object");
                sb.AppendLine("To fix this, try to rotate the object and/or resize to fit on this new resolution.");
            }

            return sb.ToString();
        }
    }
}
