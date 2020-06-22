/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using System.Runtime.InteropServices;
using Emgu.CV;
using Emgu.CV.CvEnum;

namespace UVtools.Core.Extensions
{
    public static class EmguExtensions
    {
        public static byte[] GetBytes(this Mat mat)
        {
            byte[] data = new byte[mat.Width * mat.Height * mat.NumberOfChannels];

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            using (Mat m2 = new Mat(mat.Size, DepthType.Cv8U, 3, handle.AddrOfPinnedObject(), mat.Width * mat.NumberOfChannels)) { }
            handle.Free();

            return data;
        }

        public static Mat CloneBlank(this Mat mat)
        {
            return new Mat(new Size(mat.Width, mat.Height), mat.Depth, mat.NumberOfChannels);
        }
    }
}
