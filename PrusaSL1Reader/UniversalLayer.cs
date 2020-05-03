/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class UniversalLayer : List<UniversalLayer.LayerLine>
    {
        /// <summary>
        /// Represents a line, only white pixels
        /// </summary>
        public class LayerLine
        {
            /// <summary>
            /// Gets the x start position
            /// </summary>
            public uint X { get; }

            /// <summary>
            /// Gets the x end position
            /// </summary>
            public uint X2 => X + Length;

            /// <summary>
            /// Gets the y position
            /// </summary>
            public uint Y { get; }

            /// <summary>
            /// Number of pixels to fill
            /// </summary>
            public uint Length { get; }

            public LayerLine(uint x, uint y, uint length)
            {
                X = x;
                Y = y;
                Length = length;
            }
        }

        public List<LayerLine> Lines { get; } = new List<LayerLine>();

        public UniversalLayer(Image<L8> image)
        {
            AddFromImage(image);
        }

        public void AddFromImage(Image<L8> image)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var span = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    if(span[x].PackedValue < 125) continue;
                    int startX = x;
                    while (++x < image.Width)
                    {
                        if (span[x].PackedValue < 125 || x == (image.Width-1))
                        {
                            Add(new LayerLine((uint)startX, (uint)y, (uint)(x-startX)));
                        }
                    }
                }
            }
        }

        public Image<L8> ToImage(int resolutionX, int resolutionY)
        {
            Image <L8> image = new Image<L8>(resolutionX, resolutionY);
            
            foreach (var line in Lines)
            {
                var span = image.GetPixelRowSpan((int)line.Y);
                for (uint i = line.X; i <= line.X2; i++)
                {
                    span[(int)i] = Helpers.L8White;
                }
            }
            return image;
        }
    }
}
