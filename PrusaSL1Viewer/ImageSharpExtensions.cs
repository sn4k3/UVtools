/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using PrusaSL1Reader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Viewer
{
    public static class ImageSharpExtensions
    {
        public static System.Drawing.Bitmap ToBitmap(this Image image)
        {
            using (var memoryStream = new MemoryStream())
            {
                //var imageEncoder = image.GetConfiguration().ImageFormatsManager.FindEncoder(SixLabors.ImageSharp.Formats.Bmp.BmpFormat.Instance);
                image.Save(memoryStream, Helpers.BmpEncoder);

                //memoryStream.Seek(0, SeekOrigin.Begin);

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
