/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Collections.Generic;
using System.Linq;

namespace UVtools.Core.Slicer
{
    public class Slice : List<SliceLine>
    {
        public bool ValidateShapeIntegrity()
        {
            // A shape has to have at least three lines to close
            if (Count < 3)
                return false;

            var allPoints = this.ToList();
            var pointFreq = new Dictionary<SliceLine, int>();

            // There can't be any hanging lines. Verify every point connects to at least one other line.
            foreach (var p in allPoints)
            {
                if (!pointFreq.ContainsKey(p))
                    pointFreq[p] = 1;
                else
                    pointFreq[p]++;
            }

            foreach (var p in pointFreq.Keys)
            {
                if (pointFreq[p] < 2)
                    return false;
            }

            return true;
        }
    }
}
