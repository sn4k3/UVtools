using System;
using System.Collections.Generic;
using System.Text;

namespace UVtools.Core
{
    public class Enumerations
    {
        public enum FlipDirection : byte
        {
            Horizontally,
            Vertically,
            Both,
        }

        public enum Anchor : byte
        {
            TopLeft, TopCenter, TopRight,
            MiddleLeft, MiddleCenter, MiddleRight,
            BottomLeft, BottomCenter, BottomRight,
            None
        }
    }
}
