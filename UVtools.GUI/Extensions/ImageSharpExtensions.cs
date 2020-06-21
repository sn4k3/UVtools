/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using UVtools.Parser;
using Image = SixLabors.ImageSharp.Image;

namespace UVtools.GUI.Extensions
{
    public static class ImageSharpExtensions
    {
        public static System.Drawing.Bitmap ToBitmap(this Image image)
        {
            using (var memoryStream = new MemoryStream())
            {
                //var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(SixLabors.ImageSharp.Formats.Bmp.BmpFormat.Instance);
                Helpers.BmpEncoder.SupportTransparency = true;
                image.Save(memoryStream, Helpers.BmpEncoder);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return new System.Drawing.Bitmap(memoryStream);
            }
        }

        public static Image<Gray, byte> ToEmguImage(this Image<L8> image)
        {
            return 
                new Image<Gray, byte>(image.Width, image.Height)
                {
                    Bytes = Helpers.ImageL8ToBytes(image)
                };
        }

        public static Mat ToEmguMat(this Image<L8> image)
        {
            var mat = new Mat(new System.Drawing.Size(image.Width, image.Height), DepthType.Cv8U, 1);
            mat.SetTo(Helpers.ImageL8ToBytes(image));
            return mat;
        }

        /*public static Image<TPixel> ToImageSharpImage<TPixel>(this System.Drawing.Bitmap bitmap) where TPixel : struct, IPixel<TPixel>
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);

                memoryStream.Seek(0, SeekOrigin.Begin);

                return Image.Load<TPixel>(memoryStream);
            }
        }*/
    }
}
