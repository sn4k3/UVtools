/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;

namespace UVtools.Core.Objects
{
    [Serializable]
    public sealed class Kernel
    {
        public Matrix<byte> Matrix { get; set; }
        public Point Anchor { get; set; }

        public Kernel()
        {
        }

        public Kernel(Matrix<byte> matrix, Point anchor)
        {
            Matrix = matrix;
            Anchor = anchor;
        }
    }
}
