using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.WPF.Controls;

namespace UVtools.WPF
{
    public class MainWindow : Window
    {
        public AdvancedImageBox LayerImage;
        public Button ZoomToFitButton;
        public Button CenterButton;
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            LayerImage = this.FindControl<AdvancedImageBox>("Layer.ImageOld");
            ZoomToFitButton = this.FindControl<Button>("zoomtofit");
            CenterButton = this.FindControl<Button>("center");
            LayerImage.LoadImage(@"D:\Tiago\Desktop\UVtools\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN00000.png");

            ZoomToFitButton.Click += (sender, args) => LayerImage.ZoomToFit();
            CenterButton.Click += (sender, args) => LayerImage.CenterAt(1440/2,2560/2);
            //var layerImage = this.FindControl<AdvancedPictureBox>("Layer.ImageOld");
            //layerImage.LoadImage(@"D:\Tiago\Desktop\UVtools\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN00000.png");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void EndInit()
        {
            base.EndInit();
            
            //layerImage.LoadImage(@"D:\Tiago\Desktop\UVtools\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN\body_Tough0.1mm_SL1_5h16m_HOLLOW_DRAIN00000.png");
        }
    }
}
