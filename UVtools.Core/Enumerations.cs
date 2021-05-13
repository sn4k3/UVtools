/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.ComponentModel;

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
