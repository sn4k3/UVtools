using Avalonia.Markup.Xaml;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolCalculatorControl : ToolControl
    {
        public OperationCalculator Operation { get; }

        public FileFormat SlicerFile => App.SlicerFile;

        public ToolCalculatorControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationCalculator
            {
                CalcMillimetersToPixels = new OperationCalculator.MillimetersToPixels(SlicerFile.Resolution, SlicerFile.Display),
                CalcLightOffDelay = new OperationCalculator.LightOffDelayC(
                    (decimal)SlicerFile.LiftHeight, (decimal)SlicerFile.BottomLiftHeight,
                    (decimal)SlicerFile.LiftSpeed, (decimal)SlicerFile.BottomLiftSpeed,
                    (decimal)SlicerFile.RetractSpeed, (decimal)SlicerFile.RetractSpeed)
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void LightOffDelaySetParameters(byte side)
        {
            if (side == 0) // Bottom layers
            {
                SlicerFile.BottomLiftHeight = (float)Operation.CalcLightOffDelay.BottomLiftHeight;
                SlicerFile.BottomLiftSpeed = (float)Operation.CalcLightOffDelay.BottomLiftSpeed;
                SlicerFile.RetractSpeed = (float)Operation.CalcLightOffDelay.RetractSpeed;
                SlicerFile.BottomLayerOffTime = (float)Operation.CalcLightOffDelay.BottomLightOffDelay;
            }
            else // Normal layers
            {
                SlicerFile.LiftHeight = (float)Operation.CalcLightOffDelay.LiftHeight;
                SlicerFile.LiftSpeed = (float)Operation.CalcLightOffDelay.LiftSpeed;
                SlicerFile.RetractSpeed = (float)Operation.CalcLightOffDelay.RetractSpeed;
                SlicerFile.LayerOffTime = (float)Operation.CalcLightOffDelay.LightOffDelay;
            }

            App.MainWindow.CanSave = true;
        }
    }
}
