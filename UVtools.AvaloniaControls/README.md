# UVtools AvaloniaUI controls

[![Nuget](https://img.shields.io/nuget/v/UVtools.AvaloniaControls?style=flat-square)](https://www.nuget.org/packages/UVtools.AvaloniaControls)

## Usage

In your `app.axaml` add:

```xml
<Application.Styles>
    <StyleInclude Source="avares://UVtools.AvaloniaControls/Controls.axaml"/>
</Application.Styles>
```

## AdvancedImageBox
  - Port of: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox
  - Demo: https://youtu.be/bIr6P4dDlHc
  - Features:
     - Image modes (Zoom, fit, stretch)
     - Pan & zoom
     - Select regions
     - Pixel grid
     - Cursor images
     - Configurable
     - Fast

## ExtendedNumericUpDown
  Extended the NumericUpDown with more features
  - Features:
    - Initial value with a reset button
    - Value unit label

## IndexSelector
  Allow to choose an index from a collection count and display the selected number
  - Features:
    - SelectedIndex
    - SelectedNumber
    - Count

## GroupBox
Similar to `GroupBox` of WinForms, it contain an `Header` and `Content`

## ScrollGroupBox
Same as `GroupBox` but content is inside a `ScrollViewer`