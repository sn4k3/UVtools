using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class UniversalLayer : List<LayerLine>
    {
        public List<LayerLine> Lines { get; } = new List<LayerLine>();

        public UniversalLayer(Image<Gray8> image)
        {
            AddFromImage(image);
        }

        public void AddFromImage(Image<Gray8> image)
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

        public Image<Gray8> ToImage(int resolutionX, int resolutionY)
        {
            Image <Gray8> image = new Image<Gray8>(resolutionX, resolutionY);
            
            foreach (var line in Lines)
            {
                var span = image.GetPixelRowSpan((int)line.Y);
                for (uint i = line.X; i <= line.X2; i++)
                {
                    span[(int)i] = Helpers.Gray8White;
                }
            }
            return image;
        }
    }
}
