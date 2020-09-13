/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
// https://stackoverflow.com/questions/34335157/show-a-label-with-semi-transparent-backcolor-above-other-controls

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace UVtools.GUI.Controls
{
    public class TransparentLabel : Label
    {
        public TransparentLabel()
        {
            transparentBackColor = Color.Blue;
            _opacity = 50;
            BackColor = Color.Transparent;
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Parent == null) return;
            using (var bmp = new Bitmap(Parent.Width, Parent.Height))
            {
                Parent.Controls.Cast<Control>()
                    .Where(c => Parent.Controls.GetChildIndex(c) > Parent.Controls.GetChildIndex(this))
                    .Where(c => c.Bounds.IntersectsWith(Bounds))
                    .OrderByDescending(c => Parent.Controls.GetChildIndex(c))
                    .ToList()
                    .ForEach(c => c.DrawToBitmap(bmp, c.Bounds));


                e.Graphics.DrawImage(bmp, -Left, -Top);
                using (var b = new SolidBrush(Color.FromArgb(Opacity, TransparentBackColor)))
                {
                    e.Graphics.FillRectangle(b, ClientRectangle);
                }
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
                var rectangle = ClientRectangle;
                rectangle.X += Padding.Left;
                //rectangle.Y += Padding.Top;
                //rectangle.Width -= Padding.Left - Padding.Right;
                //rectangle.Height -= Padding.Top - Padding.Bottom;
                TextRenderer.DrawText(e.Graphics, Text, Font, rectangle, ForeColor, Color.Transparent, TextFormatFlags.VerticalCenter);
            }
        }

        private byte _opacity;
        public byte Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                Invalidate();
            }
        }

        private Color transparentBackColor;
        public Color TransparentBackColor
        {
            get => transparentBackColor;
            set
            {
                transparentBackColor = value;
                Invalidate();
            }
        }

        [Browsable(false)]
        public override Color BackColor
        {
            get => Color.Transparent;
            set => base.BackColor = Color.Transparent;
        }
    }
}
