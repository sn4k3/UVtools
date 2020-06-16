/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.Structure;
using UVtools.Parser;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace UVtools.GUI
{
    public static class EmguExtensions
    {
        public static Image<L8> ToImageSharpL8(this Image<Gray, byte> image)
        {
            return Image.LoadPixelData<L8>(image.Bytes, image.Width, image.Height);
        }
    }
}
