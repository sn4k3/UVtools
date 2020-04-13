using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace PrusaSL1Viewer
{
    public partial class ImageBox : UserControl
    {
        private static SolidBrush Brush { get; } = new SolidBrush(System.Drawing.Color.White);
        public Image<Gray8> Image { get; private set; }

        public ImageBox()
        {
            SetStyle(ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            SetStyle(ControlStyles.ResizeRedraw, true);

            UpdateStyles();
            InitializeComponent();
            
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (ReferenceEquals(Image, null)) return;

            var newImage = Image.Clone();

            newImage.Mutate(x => x.Resize(Width, Height));

            for (int y = 0; y < newImage.Height; y++)
            {
                var span = newImage.GetPixelRowSpan(y);
                for (int x = 0; x < newImage.Width; x++)
                {
                    if (span[x].PackedValue > 125)
                        e.Graphics.FillRectangle(Brush, x, y, 1, 1);
                }
            }
        }

        public void SetImage(Image<Gray8> image)
        {
            Image = image;
            
        }
    }
}
