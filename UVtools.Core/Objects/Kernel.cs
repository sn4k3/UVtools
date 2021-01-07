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
using Emgu.CV.CvEnum;

namespace UVtools.Core.Objects
{
    [Serializable]
    public sealed class Kernel
    {
        public Matrix<byte> Matrix { get; set; } 
        public Point Anchor { get; set; } = new(-1, -1);

        public Kernel()
        {
			Matrix = new(3,3);
			for(int y = 0; y < Matrix.Height; y++)
			for(int x = 0; x < Matrix.Width; x++)
			{
				Matrix[x, y] = 1;
			}
        }

        public Kernel(Matrix<byte> matrix, Point anchor)
        {
            Matrix = matrix;
            Anchor = anchor;
        }

        public void SetKernel(ElementShape shape, Size size, Point anchor)
        {
            using var mat = CvInvoke.GetStructuringElement(shape, size, anchor);
            Matrix = new Matrix<byte>(mat.Rows, mat.Cols);
            mat.CopyTo(Matrix.Mat);
        }

        public void SetKernel(ElementShape shape, Size size) => SetKernel(shape, size, new Point(-1, -1));
        public void SetKernel(ElementShape shape) => SetKernel(shape, new Size(3, 3), new Point(-1, -1));
    }
}
