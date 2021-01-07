# UVtools PowerShell Scripts

UVtools has become a big toolset, but do you know you can do your own scripts not limited 
to what GUI offers you? With PowerShell you can have access to whole UVtools.Core, 
which expose all core access to you to use. 
If you have a workflow and need to speed up, scripts can save time and dismiss the GUI interaction. 
You will get same performance as native calling, plus you can stack all your actions!

## Requirements

* UVtools
* [PowerShell >= 7.1](https://github.com/PowerShell/PowerShell/releases)
* [Visual Studio Code](https://code.visualstudio.com/) (Optional but easier to run and edit scripts)

## How to run scripts

**WARNING:** Running PowerShell scripts as administrator are very powerfull and with wide access on your system.
Never run scripts from untrusted sources! Always inspect the script content before run something new from others. 
Always try to run scripts with non-adminstration privileges.

1. First you need to register the UVtools install directory under a environment variable. This will
allow you to run scripts without have to modify each script to put the UVtools path in order to run them.
   * Open a PowerShell instance as admin
   * Enter: `[System.Environment]::SetEnvironmentVariable('UVTOOLS_PATH','FOLDER/PATH/TO/UVTOOLS', [System.EnvironmentVariableTarget]::User)`
   * Replace 'FOLDER/PATH/TO/UVTOOLS' with your UVtools instalation folder
   * Run command
   * Run: `$Env:UVTOOLS_PATH` to confirm if path is registed
   * Quit terminal
   * Note: You need to repeat this step if you change install directory
   * If your system is unable to register a environment you need to manuall set the path on each script
2. Download and open a script with Visual Studio Code
   * After open a .ps1 file for the first time, visual code will ask if you want to install the PowerShell extension, say 'yes' and wait for instalation
3. Click on the play arrow (Run) or F5
4. Script will run and prompt for inputs
   * The easier way to input a file is drag and drop the file on the terminal

## Documentation

The best way to get function names and variables is exploring **UVtools.Core** source code, most of the functions and variables
have a good readable name that make sense what it does, you can only run public methods and variables. 

On Visual Code, after you have an valid variable on your script, type it name with a ended dot (.) will show you a list of avaliable methods and variables that you can call. 
eg: `$slicerFile.`, if the list don't show up, press CTRL+Space, that should force show the list.

Take **Erode-Bottom.ps1** as bootstrap and minimal script, read each line and start exploring!


* [Core - Source code](https://github.com/sn4k3/UVtools/tree/master/UVtools.Core)
* [IFileFormat.cs - File format functions & variables](https://github.com/sn4k3/UVtools/blob/master/UVtools.Core/FileFormats/IFileFormat.cs)
  * How to load file: 
    ```Powershell
    # Find a file format given a file path, $true = is file path, $true = Create a new instance
    # Returns null if file is invalid
    $slicerFile = [UVtools.Core.FileFormats.FileFormat]::FindByExtension($inputFile, $true, $true)
    if($slicerFile){ return } # Invalid file, exit
    $slicerFile.Decode($inputFile)
    ```
* [Layer.cs - Layer representation and hold it's own data](https://github.com/sn4k3/UVtools/blob/master/UVtools.Core/Layer/Layer.cs)
  * How to access:
      *  `$slicerFile[layerIndex]`
      *  `$slicerFile.LayerManager[layerIndex]` (Alternative)
      *  `$slicerFile.LayerManager.Layers[layerIndex]` (Alternative)
  * Example:
    ```Powershell
    Write-Output $slicerFile[layerIndex].PositionZ
    Write-Output $slicerFile[layerIndex].ExposureTime
    Write-Output $slicerFile[layerIndex].LiftHeight
    Write-Output $slicerFile[layerIndex].LiftSpeed
    Write-Output $slicerFile[layerIndex].RetractSpeed
    Write-Output $slicerFile[layerIndex].LayerOffTime
    Write-Output $slicerFile[layerIndex].LightPWM
    Write-Output $slicerFile[layerIndex].BoundingRectangle
    Write-Output $slicerFile[layerIndex].NonZeroPixelCount
    Write-Output $slicerFile[layerIndex].IsModified
    ```
* [LayerManager.cs - The layer manager that keeps all layers within the file](https://github.com/sn4k3/UVtools/blob/master/UVtools.Core/Layer/LayerManager.cs)
  * How to access: `$slicerFile.LayerManager`
* [Operations - Applies operation over layers, the tools menu on the GUI](https://github.com/sn4k3/UVtools/tree/master/UVtools.Core/Operations)
  * Example:
    ```Powershell
    $morph = New-Object UVtools.Core.Operations.OperationMorph
    $morph.MorphOperation = [Emgu.CV.CvEnum.MorphOp]::Erode
    $morph.IterationsStart = $iterations
    $morph.LayerIndexEnd = $slicerFile.BottomLayerCount - 1
    if(!$morph.Execute($slicerFile, $progress)){ return; }
    ```

## Contribute with your scripts

If you make a usefull script and want to contribute you can share and publish your script under [github - issues](https://github.com/sn4k3/UVtools/issues/new/choose).
After analyzation it will be published on the repository