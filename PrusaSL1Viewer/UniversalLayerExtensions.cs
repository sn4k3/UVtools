using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PrusaSL1Reader;
using SixLabors.ImageSharp;

namespace PrusaSL1Viewer
{
    public static class UniversalLayerExtensions
    {
        public static Bitmap ToBitmap(this UniversalLayer layer, int width, int height)
        {
            Bitmap buffer = new Bitmap(width, height);//set the size of the image
            Graphics gfx = Graphics.FromImage(buffer);//set the graphics to draw on the image

            foreach (var line in layer)
            {
                gfx.DrawRectangle(Pens.White, line.X, line.Y, line.Length, 1);
            }

            return buffer;
        }
    }
}
