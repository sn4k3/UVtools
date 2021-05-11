using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace UVtools.Core
{
    public class Enumerations
    {
        public enum LayerRangeSelection : byte
        {
            None,
            All,
            Current,
            Bottom,
            Normal,
            First,
            Last
        }

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

        public enum LightOffDelaySetMode : byte
        {
            [Description("Set the light-off with an extra delay")]
            UpdateWithExtraDelay,

            [Description("Set the light-off without an extra delay")]
            UpdateWithoutExtraDelay,

            [Description("Set the light-off to zero")]
            SetToZero,

            [Description("Disabled")]
            NoAction
        }
    }
}
