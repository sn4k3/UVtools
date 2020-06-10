using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace PrusaSL1Viewer.Forms
{
    public partial class FrmLoading : Form
    {
        public Stopwatch StopWatch { get; } = new Stopwatch();
        public FrmLoading()
        {
            InitializeComponent();
        }

        public FrmLoading(string description) : this()
        {
            SetDescription(description);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            e.Handled = true;
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            e.Handled = true;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);
            e.Handled = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            StopWatch.Restart();
            timer.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            timer.Stop();
            StopWatch.Stop();
        }

        public void SetDescription(string description)
        {
            Text =
            lbDescription.Text = description;
        }

        public void SetProgress(int value)
        {
            progressBar.Style = ProgressBarStyle.Blocks;
            progressBar.Value = value;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            lbElapsedTime.Text = $"Elapsed Time: {StopWatch.ElapsedMilliseconds}ms";
        }
    }
}
