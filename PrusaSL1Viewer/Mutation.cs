/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Drawing;


namespace PrusaSL1Viewer
{
    public class Mutation
    {
        #region Enum
        public enum Mutates : byte
        {
            Erode,
            Dilate,
            Opening,
            Closing,
            Gradient,
            TopHat,
            BlackHat,
            HitMiss,
            PyrDownUp,
            SmoothMedian,
            SmoothGaussian
        }
        #endregion

        #region Properties
        public Mutates Mutate { get; }

        public string Description { get; }

        public Image Image { get; }
        #endregion

        #region Constructor

        public Mutation(Mutates mutate, string description, Image image = null)
        {
            Mutate = mutate;
            Description = description;
            Image = image;
        }

        #endregion
    }
}
