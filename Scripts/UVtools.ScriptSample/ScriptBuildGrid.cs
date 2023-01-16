using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Drawing;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample;

public class ScriptBuildGrid : ScriptGlobals
{
    readonly ScriptNumericalInput<ushort> GridSpacing = new()
    {
        Label = "Size of the initial grains",
        Unit = "px",
        Minimum = 1,
        Maximum = 10000,
        Increment = 1,
        Value = 200,
    };

    readonly ScriptNumericalInput<ushort> GridWidth = new()
    {
        Label = "Width of line",
        Unit = "px",
        Minimum = 1,
        Maximum = 500,
        Increment = 1,
        Value = 1,
    };

    readonly ScriptNumericalInput<float> ExposureTime = new()
    {
        Label = "Exposure time",
        Unit = "s",
        Minimum = 0.1f,
        Maximum = 1000f,
        Increment = 0.5f,
        DecimalPlates = 2,
        Value = 1,
    };

    /// <summary>
    /// Set configurations here, this function trigger just after load a script
    /// </summary>
    public void ScriptInit()
    {
        Script.Name = "Build grid pattern";
        Script.Description = "Useful for measuring backlight quality";
        Script.Author = "Jan Mrázek";
        Script.Version = new Version(0, 1);
        Script.UserInputs.Add(GridSpacing);
        Script.UserInputs.Add(GridWidth);

        Script.UserInputs.Add(ExposureTime);
        ExposureTime.Value = SlicerFile.BottomExposureTime;
    }

    /// <summary>
    /// Validate user inputs here, this function trigger when user click on execute
    /// </summary>
    /// <returns>A error message, empty or null if validation passes.</returns>
    public string? ScriptValidate()
    {
        return null;
    }

    private Mat GenerateGridPattern() 
    {
        var pattern = SlicerFile.CreateMat();

        for (int x = pattern.Size.Width / 2; x < pattern.Size.Width; x += GridSpacing.Value) {
            CvInvoke.Line(pattern,
                new Point(x, 0),
                new Point(x, pattern.Size.Height),
                EmguExtensions.WhiteColor, GridWidth.Value, LineType.FourConnected);
            CvInvoke.Line(pattern,
                new Point(pattern.Size.Width - x, 0),
                new Point(pattern.Size.Width - x, pattern.Size.Height),
                EmguExtensions.WhiteColor, GridWidth.Value, LineType.FourConnected);
        }

        for (int y = pattern.Size.Height / 2; y < pattern.Size.Height; y += GridSpacing.Value) {
            CvInvoke.Line(pattern,
                new Point(0, y),
                new Point(pattern.Size.Width, y),
                EmguExtensions.WhiteColor, GridWidth.Value, LineType.FourConnected);
            CvInvoke.Line(pattern,
                new Point(0, pattern.Size.Height - y),
                new Point(pattern.Size.Width, pattern.Size.Height - y),
                EmguExtensions.WhiteColor, GridWidth.Value, LineType.FourConnected);
        }

        return pattern;
    }

    /// <summary>
    /// Execute the script, this function trigger when when user click on execute and validation passes
    /// </summary>
    /// <returns>True if executes successfully to the end, otherwise false.</returns>
    public bool ScriptExecute()
    {
        Progress.Reset("Changing layers", 2); // Sets the progress name and number of items to process

        SlicerFile.Reallocate(2);

        using var pattern = GenerateGridPattern();
        SlicerFile[0].LayerMat = pattern;
        Progress++;
        SlicerFile[0].CopyImageTo(SlicerFile[1]);
        Progress++;

        SlicerFile.BottomLayerCount = 2; // Sanitize bottom layer count
        SlicerFile.BottomExposureTime = ExposureTime.Value;
        SlicerFile.ExposureTime = ExposureTime.Value;

        return !Progress.Token.IsCancellationRequested;
    }
}
