/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
// https://stackoverflow.com/questions/10803184/windows-forms-button-with-drop-down-menu
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace UVtools.GUI.Controls
{
    public class SplitButton : Button
    {
        [DefaultValue(null), Browsable(true),
        DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public ContextMenuStrip Menu { get; set; }

        [DefaultValue(20), Browsable(true),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int SplitWidth { get; set; } = 20;

        [DefaultValue(false), Browsable(true),
         DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public bool OpenMenuOnlyOnArrow { get; set; } = false;

        public SplitButton()
        {
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            if (Menu != null && e.Button == MouseButtons.Left)
            {
                if (OpenMenuOnlyOnArrow)
                {
                    var splitRect = new Rectangle(this.Width - this.SplitWidth, 0, this.SplitWidth, this.Height);
                    if (!splitRect.Contains(e.Location))
                    {
                        base.OnMouseDown(e);
                        return;
                    }
                }
                Menu.Show(this, 0, this.Height);    // Shows menu under button
                //Menu.Show(this, mevent.Location); // Shows menu at click location
            }
            else
            {
                base.OnMouseDown(e);
            }
        }

        /*protected override void OnMouseDown(MouseEventArgs mevent)
        {
            var splitRect = new Rectangle(this.Width - this.SplitWidth, 0, this.SplitWidth, this.Height);

            // Figure out if the button click was on the button itself or the menu split
            if (Menu != null &&
                mevent.Button == MouseButtons.Left &&
                splitRect.Contains(mevent.Location))
            {
                Menu.Show(this, 0, this.Height);    // Shows menu under button
                                                    //Menu.Show(this, mevent.Location); // Shows menu at click location
            }
            else
            {
                base.OnMouseDown(mevent);
            }
        }*/

        protected override void OnPaint(PaintEventArgs pevent)
        {
            base.OnPaint(pevent);

            if (this.Menu != null && this.SplitWidth > 0)
            {
                // Draw the arrow glyph on the right side of the button
                int arrowX = ClientRectangle.Width - 14;
                int arrowY = ClientRectangle.Height / 2 - 1;

                var arrowBrush = Enabled ? SystemBrushes.ControlText : SystemBrushes.ButtonShadow;
                var arrows = new[] { new Point(arrowX, arrowY), new Point(arrowX + 7, arrowY), new Point(arrowX + 3, arrowY + 4) };
                pevent.Graphics.FillPolygon(arrowBrush, arrows);

                // Draw a dashed separator on the left of the arrow
                int lineX = ClientRectangle.Width - this.SplitWidth;
                int lineYFrom = arrowY - 4;
                int lineYTo = arrowY + 8;
                using (var separatorPen = new Pen(Brushes.DarkGray) { DashStyle = DashStyle.Dot })
                {
                    pevent.Graphics.DrawLine(separatorPen, lineX, lineYFrom, lineX, lineYTo);
                }
            }
        }
    }
}
