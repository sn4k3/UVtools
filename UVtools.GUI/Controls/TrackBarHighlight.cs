/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UVtools.Core.Extensions;

namespace UVtools.GUI.Controls
{
    public class TrackBarHighlight : TrackBar
    {
        private uint[] _highlightValues = null;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, uint msg, int wParam, int lParam);

        private const int TBM_GETNUMTICS = 0x0410;
        private const int TBM_GETTICPOS = 0x40F;

        private const byte TrackerMargin = 12;

        //public event PaintEventHandler PaintOver;

        public Pen HighlightPen { get; set; } = new Pen(Color.FromArgb(50, Color.Red), 2);
        public byte LineLength { get; set; } = 10;

        public uint[] HighlightValues
        {
            get => _highlightValues;
            set { _highlightValues = value; Invalidate(); }
        }

        public TrackBarHighlight()
        {
            //SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        /*protected override void OnInvalidated(InvalidateEventArgs e)
        {
            base.OnInvalidated(e);
            Debug.WriteLine("Invalidated");
            DrawHighlights();
        }*/

        /*protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            
            // WM_PAINT
            if (m.Msg == 0x0F)
            {
                using (Graphics lgGraphics = Graphics.FromHwndInternal(m.HWnd))
                    OnPaintOver(new PaintEventArgs(lgGraphics, ClientRectangle));
            }
        }*/

        protected virtual void OnPaintOver(PaintEventArgs e)
        {
            //PaintOver?.Invoke(this, e);
            //Debug.WriteLine("Paint");
            DrawHighlights(e.Graphics);
        }

        public int GetTickPos(int index) => SendMessage(Handle, TBM_GETTICPOS, index, 0);
        public int NumTicks => SendMessage(Handle, TBM_GETNUMTICS, 0, 0);

        public void DrawHighlights(Graphics gfx = null)
        {
            if (HighlightValues is null) return;
            if (gfx is null) gfx = CreateGraphics();

            gfx.SmoothingMode = SmoothingMode.HighSpeed;

            int startX = Width / 2 - LineLength / 2;
            int endX = startX + LineLength;
            foreach (var value in HighlightValues)
            {
                var tickPos = GetTickPos((int)value);
                if (tickPos == -1) continue;
                //int y = (int)(Height - TrackerStartMargin - (Height / Maximum * value));
                int y = (Height - tickPos).Clamp(1, Height);
                gfx.DrawLine(HighlightPen, startX, y, endX, y);
            }
        }
    }
}
