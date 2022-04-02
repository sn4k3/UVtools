# Changelog

## 02/04/2022 - v3.2.1

- **AnyCubic file format:**
   - (Fix) Lift height and speed are not being correctly set on old version, keeping a constant value (#441)
   - (Fix) Retract speed getter was not return value from TSMC if using version 516
- **Tool - Infill:**
   - (Add) Waves infill type
   - (Add) Concentric infill type
   - (Add) Gyroid infill type (#443)
   - (Change) Increase the default spacing from 200px to 300px
   - (Improvement) Fastter infill processing by use the model bounds
- (Add) `FormatSpeedUnit` property to file formats to get the speed unit which the file use internally
- (Fix) UI: ROI rectangle can overlap scroll bars while selecting

## 26/03/2022 - v3.2.0

- **Core:**
   - (Add) Machine presets and able to load machine collection from PrusaSlicer
   - (Improvement) Core: Reference EmguCV runtimes into core instead of the UI project
- **File formats:**
   - **CXDLP:**
      - (Add) Detection support for Halot One Pro
      - (Add) Detection support for Halot One Plus
      - (Add) Detection support for Halot Sky Plus
      - (Add) Detection support for Halot Lite
      - (Improvement) Better handling and detection of printer model when converting
      - (Improvement) Discovered more fields meanings on format
      - (Fix) Exposure time in format is `round(time * 10, 1)`
      - (Fix) Speeds in format are in mm/s, was using mm/min before
   - (Add) JXS format for Uniformation GKone [Zip+GCode]
   - (Improvement) Saving and converting files now handle the file backup on Core instead on the UI, which prevents scripts and other projects lose the original file in case of error while saving
   - (Improvement) When saving files the .tmp extension is no longer shown on `FileFullPath`, which now `TemporaryOutputFileFullPath` is who holds the file.tmp
   - (Fix) After load files they was flagged as requiring a full encode, preventing fast save a fresh file 
- **UVtoolsCmd:**
   - Bring back the commandline project
   - Consult README to see the available commands and syntax
   - Old terminal commands on UVtools still works for now, but consider switch to UVtoolsCmd or redirect the command using `UVtools --cmd "commands"`
- **Tools:**
   - **Change print resolution:**
      - (Add) Allow to change the display size to match the new printer
      - (Add) Machine presets to help set both resolution and display size to a correct printer and auto set fix pixel ratio
      - (Improvement) Real pixel pitch fixer due new display size information, this allow full transfers between different printers "without" invalidating the model size
      - (Improvement) Better arrangement of  the layout 
   - (Add) Infill: Option "Reinforce infill if possible", it was always on before, now default is off and configurable
   - (Improvement) Always allow to export settings from tools
- **GCode:**
   - (Improvement) After print the last layer, do one lift with the same layer settings before attempt a fast move to top
   - (Improvement) Use the highest defined speed to send the build plate to top after finish print
   - (Improvement) Append a wait sync command in the end of gcode if needed
   - (Fix) When lift without a retract it still output the motor sync delay for the retract time and the wait time after retract
- **PrusaSlicer:**
   - (Add) Printer: Creality Halot One Pro CL-70
   - (Add) Printer: Creality Halot One Plus CL-79
   - (Add) Printer: Creality Halot Sky Plus CL-92
   - (Add) Printer: Creality Halot Lite CL-89L
   - (Add) Printer: Creality Halot Lite CL-89L
   - (Add) Printer: Creality CT133 Pro 
   - (Add) Printer: Creality CT-005 Pro
   - (Add) Printer: Uniformation GKone
   - (Add) Printer: FlashForge Foto 8.9S
   - (Add) Printer: Elegoo Mars 2
   - (Improvement) Rename all Creality printers
   - (Fix) Creality model in print notes

## 21/03/2022 - v3.1.1

- (Add) Raft relief: Tabs type - Creates tabs around the raft to easily insert a tool under it and detach the raft from build plate
- (Add) Linux AppImage binaries (You won't get them with auto-update, please download AppImage once before can use auto-update feature in the future)
- (Change) Rename "layer compression method" to "layer compression codec", please redefine the codec setting if you changed before
- (Improvement) Linux and macOS releases are now compiled, published and packed under Linux (WSL). Windows release still and must be published under windows.
- (Fix) Windows auto-upgrade was downloading `.zip` instead of `.msi` (Bug was introduced on v3.1.0).  
        You still need to download v3.1.1 manually in order to get this fix on future releases if you come from v3.1.0.

## 17/03/2022 - v3.1.0

- **Benchmark:**
   - (Add) PNG, GZip, Deflate and LZ4 compress tests
   - (Change) Test against a known image instead of random noise
   - (Change) Single-thread tests from 100 to 200 and multi-thread tests from 1000 to 5000
   - (Improvement) Same image instance is shared between tests instead of create new per test
   - (Fix) Encode typo
- **Core:**
   - (Add) Layer compression method: Allow to choose the compression method for layer image
      - **PNG:** Compression=High Speed=Slow (Use with low RAM)
      - **GZip:** Compression=Medium Speed=Medium (Optimal)
      - **Deflate:** Compression=Medium Speed=Medium (Optimal)
      - **LZ4:** Compression=Low Speed=Fast (Use with high RAM)
   - (Improvement) Better handling on cancel operations and more immediate response
   - (Fix) Extract: Zip Slip Vulnerability (CWE-22)
- **File formats:**
   - (Improvement) Better handling of encode/decoding layers from zip files
   - (Fix) ZCode: Canceling the file load can trigger an error
   - (Fix) VDA: Unable to open vda zip files
- **Tools:**
   - (Improvement) Allow operations to be aware of ROI and Masks before execution (#436)
   - (Improvement) Scripting: Allow save and load profiles (#436)
   - (Fix) Adjust layer height: When using the Offset type the last layer in the range was not taken in account (#435)
- **UI:**
   - (Improvement) Allow layer zoom levels of 0.1x and 64x but constrain minimum zoom to the level of image fit
   - (Improvement) Update change log now shows with markdown style and more readable
   - (Fix) Windows MSI upgrade to this version (#432)
   - (Fix) Auto-updater for Mac ARM, was downloading x64 instead

## 12/03/2022 - v3.0.0

- **(Add) Suggestions:**
   - A new module that detect bad or parameters out of a defined range and suggest a change on the file, those can be auto applied if configured to do so
   - **Avaliable suggestions:**
      - **Bottom layer count:** Bottom layers should be kept to a minimum, usually from 2 to 3, it function is to provide a good adhesion to the first layer on the build plate, using a high count have disadvantages.  
      - **Wait time before cure:** Rest some time before cure the layer is crucial to let the resin settle after the lift sequence and allow some time for the arm settle at the correct Z position as the resin will offer some resistance and push the structure.  
                                   This lead to better quality with more successful prints, less lamination problems, better first layers with more success of stick to the build plate and less elephant foot effect.
      - **Wait time after cure:** Rest some time after cure the layer and before the lift sequence can be important to allow the layer to cooldown a bit and detach better from the FEP.
      - **Layer height:** Using the right layer height is important to get successful prints:  
                          Thin layers may cause problems on adhesion, lamination, will print much slower and have no real visual benefits.  
                          Thick layers may not fully cure no matter the exposure time you use, causing lamination and other hazards. Read your resin dtasheet to know the limits.  
                          Using layer height with too many decimal digits may produce a wrong positioning due stepper step loss and/or Z axis quality.
- **Core:**
   - Convert the project to Nullable aware and "null-safe"
- **File Formats:**
   - (Add) `Volume` property to get the total model volume
   - (Add) `SanitizeLayers` method to reassign indexes and force attribute parent file
   - (Improvement) Merge `LayerManager` into `FileFormat` and cleanup: This affects the whole project and external scripts.
   If using scripts please update them, search for `.LayerManager.` and replace by `.`
   - (Change) Chitubox encrypted format can now be saved as normal
   - (Fix) Converted files layers was pointing to the source file and related to it
- **Layers:**
   - (Add) Methods: `ResetParameters`, `CopyParametersTo`, `CopyExposureTo`, `CopyWaitTimesTo`
   - (Improvement) `IsBottomLayer` property will also return true when the index is inside bottom layer count
- **Scripting:**
   - (Add) Configuration variable: `MinimumVersionToRun` - Sets the minimum version able to run the script
   - (Improvement) Allow run scripts written in C# 10 with the new namespace; style as well as nullables methods
   - (Improvement) Convert scripts to use Nullable code
- **UI:**
   - (Add) Fluent Dark theme
   - (Add) Default Light theme
   - (Add) Default Dark theme
   - (Change) Use fontawesome and material design to render the icons instead of static png images
   - (Change) Some icons
   - (Change) Move log tab to clipboard tab
   - (Change) Tooltip overlay default color
   - (Improvement) Windows position for tool windows, sometimes framework can return negative values affecting positions, now limits to 0 (#426)
   - (Fix) Center image icon for layer action button
   - (Fix) Center image icon for save layer image button
- **Tools:**
   - (Add) Layer re-height: Offset mode, change layers position by a defined offset (#423)
   - (Improvement) Rotate: Unable to use an angle of 0
   - (Improvement) Remove layers: Will not recalcualte and reset properties of layers anymore, allowing removing layers on dynamic layer height models and others
   - (Improvement) Clone layers: Will not recalcualte and reset properties of layers anymore, allowing cloning layers on dynamic layer height models and others
   - (Fix) Exposure time finder: Very small printers may not print the stock object as it is configured, lead to a unknown error while generating the test. It will now show a better error message and advice a solution (#426)
- **Terminal:**
   - (Add) More default namespaces
   - (Improvement) Set a MinHeight for the rows to prevent spliter from eat the elements
   - (Change) Set working space to the MainWindow instead of TerminalWindow
- **(Upgrade) .NET from 5.0.14 to 6.0.3**
   - This brings big performance improvements, better JIT, faster I/O operations and others
   - Read more: https://devblogs.microsoft.com/dotnet/performance-improvements-in-net-6
   - Due this macOS requirement starts at 10.15 (Catalina)
   - Read more: https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md
- (Add) Native support for MacOS ARM64 architecture (Mac M1 and upcomming Mac's) (#187)
- (Exchange) Dependency Newtonsoft Json by System.Text.Json to parse the json documents
- (Remove) Dependency: MoreLinq
- (Remove) "Automations - Light-off delay" in favor of new suggestion "wait time before cure" module
- (Fix) File - Send to: Winrar or 7zip have a wrong extension on the list (uvt) when should be (uvj)
- (Upgrade) AvaloniaUI from 0.10.12 to 0.10.13

## 21/02/2022 - v2.29.0

- **File formats:**
   - (Add) Transition layer count to the supported files and auto compute transition time on corresponding layers in software mode
   - (Add) `HaveTransitionLayers`, `TransitionLayersType`, `BottomLayers`, `NormalLayers`, `TransitionLayers`, `TsmcLayers` properties
   - (Add) Layer: `IsTransitionLayer` property
   - (Add) SL1: Keyword `TransitionLayerCount_xxx` - Sets the number of transition layers
   - (Improvement) CTB, PHZ, FDG: Implement the `ModifiedTimestampMinutes` field, it was the MysteriousId before as an unknown field
   - (Fix) CWS: Open in partial mode will cause an exception and prevent file from load
- **GCode:**
   - (Add) Allow inverse lifts to work as an retract
   - (Fix) Parsing of WaitTimeAfterLift was incorrect when lacking a lift sequence
   - (Fix) Layers lacking an exposure time was defaulting to global time, now defaults to 0
   - (Fix) Layers without a LED ON (M106) was setting `LightPWM` to the max value (255), now defaults to 0
- **Tools:**
   - **Timelapse:**
      - (Add) Information: Raise Layer count equivalence 
      - (Add) Information: Additional lifts to be generated
      - (Add) Option: Ensure the last layer - If enabled, it will generate an obligatory layer to cover the last layer
      - (Improvement) Optimize lift for virtual layer mode, allowing set a slow and fast lift / retract by using another virtual layer to emulate a lift
      - (Improvement) Allow to define slow and fast speed for virtual layer mode even if TSMC isn't supported
   - (Add) Fade exposure time: Setting 'Disable firmware transition layers' - Attempt to disable firmware strict transition layers in favor of this tool
   - (Add) Calibration tests: Attempt to auto disable the firmware transifiton layers
   - (Change) Edit print parameters: Allow set `BottomLiftHeight` and `LiftHeight` to 0mm
- **UI:**
   - (Improvement) Disallow drop files into UI when is processing data / disabled and prevent crashing from that action
   - (Improvement) Information tab visibility and MinHeight for data grids
   - (Improvement) Hide/show GCode tab when necessary (dependent on file format)
   - (Improvement) The 'save as' will show the new file into 'Open recent' files
- **PrusaSlicer printers:**
   - (Add) Elegoo Jupiter
   - (Add) EPAX X1 4KS
   - (Add) EPAX DX1 Pro
   - (Add) EPAX DX10 Pro 5K
   - (Add) EPAX DX10 Pro 8K
   - (Add) EPAX E10 8K
   - (Add) EPAX X133 6K

## 15/02/2022 - v2.28.1

- (Add) File - Terminal: Inject C# code into UVtools with an interactive terminal
- (Improvement) Modifiers: Better increment values for the spin up/down buttons
- (Improvement) Tool - Timelapse: Better lift and feeds for virtual layer, also allow to set custom lift speed for that mode
- (Fix) Tool - Edit print parameters: Disallow to change PositionZ in layers with an active alternating pattern
- (Fix) GCode: When generating layers higher than the next, it will not return to the correct position z

## 13/02/2022 - v2.28.0

- **Core:**
   - (Add) Utilities methods (`MinimumSpeed`, `MaximumSpeed`, `CreateMat`, `CreateMatWithDummyPixel`, `FileFormat.CopyParameters`)
   - (Improvement) Issues - Print Height: Group all layers outside the valid print height
   - (Fix) `Rectangle.Center` return a wrong center
- **FileFormats:**
   - (Add) Generic / Phrozen ZIP format
   - (Add) Information/modifier to file formats to tell whatever is possible to use custom PositionZ per layer
   - (Add) Safe checks in order to run some tools, related to the previous "PositionZ" point
   - (Improvement) if blank, allow the previous layer to have a higher Z position than the successor layer
   - (Improvement) SL1: Implement the missing keys from new features of PrusaSlicer 2.4.0
   - (Fix) Calling a partial save action without a progress instance would cause a crash
   - (Fix) GCode: Unable to parse the "Wait time after lift" when a second lift (TSMC) was present, leading to a sum on "Wait time before cure" 
- **Tools:**
   - (Add) Timelapse: Raise the build platform to a set position every odd-even height to be able to take a photo and create a time-lapse video of the print
   - (Add) Scripting: ScriptToggleSwitchInput
   - **Raise on print finish:**
      - (Add) Reapply check and prevent run the tool in that case
      - (Fix) It was incorrectly marked to be able to run in partial mode
      - (Fix) The dummy pixel was beeing set to a wrong location
- **Layers:**:
   - (Change) When set Wait times to a negative value, it will set the global wait time value accordingly
   - (Change) Allow ExposureTime to be 0
- **Commandline arguments**
   - (Add) --run-operation \<input_file\> \<operation_file.uvtop\>
   - (Add) --run-script \<input_file\> \<script_file.cs\>
   - (Add) --copy-parameters \<from_file\> \<to_file\>
- **UI:**
   - (Add) When open a file with missing crucial information, prompt the user to fill in that information (Optional)
   - (Add) Warn user about misfunction when open a file with invalid layer height of 0mm
   - (Improvement) Layer information: Only show the "Exposure time" when its availiable on the file format
   - (Improvement) When a file is about to get auto-converted once loaded and the output file already exists, prompt user for overwrite action
- (Improvement) Set default culture info at top most of the program to avoid strange problems with commandline arguments
- (Upgrade) .NET from 5.0.13 to 5.0.14
- (Downgrade) OpenCV from 4.5.5 to 4.5.4 due the crash while detecting islands (Linux and MacOS) (#411, #415, #416)

## 27/01/2022 - v2.27.7

- **Pixel Arithmetic:**
   - (Add) Corrode: Number of passes
   - (Change) Corrode: Noise area default from 3px² to 1px²
   - (Change) Fuzzy skin preset: Wall thickness from 6px to 4px
   - (Change) Fuzzy skin preset: Max noise offset: 64
- (Add) Core: More helper functions: Area, Volume, LastBottomLayer, BottomLayersHeight, SetNoDelays, SetWaitTimeBeforeCureOrLightOffDelay
- (Change) Display the current layer volume instead of area
- (Fix) Having a first empty layer will miscalculate the model rectangle bounds
- (Fix) Tool - Calculator - Model tilt: Change formula to use arctan instead of tanh
- (Upgrade) OpenCV from 4.5.4 to 4.5.5
- (Upgrade) AvaloniaUI from 0.10.11 to 0.10.12 (#378)

## 07/01/2022 - v2.27.6

- **PrusaSlicer:**
   - (Fix) Printer: AnyCubic Photon Mono 4K - bed origin (#369)
   - (Fix) Printer: AnyCubic Photon Mono X 6K - bed origin (#369)
- (Fix) Anycubic files: Soft-save on versions below 516 can corrupt the file (#396)
- (Fix) Pixel editor: Place a line on a mirrored virtual layer was previewing the line in the wrong orientation (#399)

## 05/01/2022 - v2.27.5

- **Pixel Arithmetic:**
  - (Add) Corode: Noise pixel area, defaulting to 3px²
  - (Change) Corode: Cryptonumeric random to normal random to speed up calculation
  - (Change) Fuzzy skin preset: Set a ignore threshold area of 5000px2
  - (Improvement) Masking performance and auto crop the layer to speed up the processing when using an "Apply to" other than "All"
  - (Fix) Some "Apply to" methods was creating a wrong mask with some operators
- **CXDLP V3:**
   - (Fix) Checksum (CRC32) (#389)
   - (Fix) Software name and material name serialization

## 26/12/2021 - v2.27.4

- **UI:**
   - (Add) When converting file formats with multiple available versions, it will prompt to select the version to use
   - (Add) Remove CTBv4 from convert menu (Will require readjust settings for default extension if you are using an below extension) (#286)
   - (Change) Rename CTBv3 to CTB on the convert menu
- (Fix) CXDLP: Set version 3 was setting wrong information

## 24/12/2021 - v2.27.3

- **Encrypted CTB:**
   - (Add) Allow convert files to this format in the UI
   - (Fix) Converting files with a null machine name would cause an exception
   - (Fix) Converting files was generating malformed files
-  **UI:**
   - (Change) CTB: Extension display order (Will require readjust settings for default extension if you are using any of them)
   - (Fix) 'File - Open recent file' would not enable first item when closing the file

## 23/12/2021 - v2.27.2

- **(Core) Layer:**
  - (Add) Property: IsFirstLayer -  Gets if is the first layer
  - (Add) Property: IsIntermediateLayer - Gets if layer is between first and last layer, aka, not first nor last layer
  - (Add) Property: IsLastLayer - Gets if is the last layer
  - (Rename) Property: RelativeLayerHeight to RelativePositionZ
- **VDT:**
   - (Add) Keyword 'FILEFORMAT_xxx' to allow set the file format to auto convertion on machine notes
   - (Add) Keyword 'FILEVERSION_n' to allow set the file format version/revision on machine notes

## 22/12/2021 - v2.27.1

- **PrusaSlicer:**
   - (Add) Keyword 'FILEVERSION_n' to allow set the file format version/revision
   - (Change) Printer: AnyCubic Photon Mono 4K and Mono X 6K, to use version 516
- **Anycubic file format:**
   - (Fix) Encoding or converting a new file with version 516 was setting wrong length for the new tables
   - (Fix) Setting bottom lift height or speed was not working and keep the same value
- (Add) FileFormats: Abstract `Version` property to set or get the version on file formats
- (Add) CXDLP: Compability with new file v2 revsion (#376)
- (Upgrade) AvaloniaUI from 0.10.8 to 0.10.11
- 
## 18/12/2021 - v2.27.0

- **Tool - Morph:**
   - (Add) Operator: White tophat - Removes small isolated pixels and only return its affected pixels (Image - Noise removal)
   - (Add) Operator: Black tophat - Closes small holes inside the objects and only return its affected pixels (Gap closing - Image)
   - (Add) Operator: Hit or miss - Finds pixels in a given kernel pattern
   - (Remove) Operator: 'Isolate features' as that is the same as the 'White tophat' and is already inbuilt into OpenCV
- **Kernels:**
   - (Add) Option: Use dynamic kernel to enhancement the quality of the borders (#367)
   - (Add) Kernels are now saved with the operation profile
- **PrusaSlicer:**
   - (Add) Support to slice files to be converted for encrypted CTB format
   - (Add) Printer: Elegoo Mars 3 (#370)
   - (Add) Printer: EPAX E10 5K
   - (Add) Printer: EPAX X10 5K
   - (Add) Printer: Phrozen Sonic Mini 8K
   - (Add) Printer: Phrozen Sonic Mega 8K
   - (Fix) Printer: AnyCubic Photon Mono 4K - display size (#369)
   - (Fix) Printer: AnyCubic Photon Mono X 6K - display size (#369)
- (Add) Tool - Double exposure: Kernel configuration
- (Add) Tool - Pixel arithmetic: Kernel configuration
- (Add) Calibration - Elephant foot: Use dynamic kernel to enhancement the quality of the borders (#367)
- (Upgrade) .NET from 5.0.12 to 5.0.13
- (Fix) Calibrate - Elephant foot: Redo (Ctrl + Z) the operation was crashing the application
- (Fix) CTB, PHZ, FDG: Converting files with a null machine name would cause a exception
- (Fix) Anycubic files: Bottom lift and speed were showing default values instead of real used value

## 06/12/2021 - v2.26.0

- **File formats - Photon Workshop: (#360)**
   - (Add) Allow to globally edit bottom lift height and bottom lift speed
   - (Add) Support for file version 515, 516 and TSMC
   - (Add) AnyCubic Photon Ultra (DLP)
   - (Add) AnyCubic Photon Mono SQ (PMSQ)
   - (Add) AnyCubic Photon Mono 4K (PMSA)
   - (Add) AnyCubic Photon Mono X 6K (PMSB)
- **PrusaSlicer printers:**
   - (Add) AnyCubic Photon Mono 4K
   - (Add) Photon Mono X 6K
- (Add) PrusaSlicer profiles manager: Allow to install profiles into SuperSlicer (#355)
- (Fix) CTBv4: Setting the LiftHeight2 was defining the base.LiftHeight2 to BottomLiftHeight2
- (Fix) CWS: Typo on the "resolution must be multiple of 3" error

## 02/12/2021 - v2.25.3

- **File - Send to:**
   - (Add) Icons to distinguish each send type
   - (Add) Allow to configure processes to open the file with (#352)
- (Improvement) CXDLP: When encoding the file, only attempt to change the machine name if not starts with 'CL-' (#351)
- (Improvement) UI: Remove the "0/?" and just show the title on the progress bar
- (Change) CTBv4: Remove the following validation to be compatible with lychee and CTB SDK: "Malformed file, PrintParametersV4 found invalid validation values, expected (4, 4) got (x, y)" (#354)

## 25/11/2021 - v2.25.2

- **About box:**
   - (Add) Processor name
   - (Add) Memory RAM (Used / Total GB)
   - (Add) OpenCV and Assemblies tab
   - (Add) "Copy information" button to copy the whole dialog information, usefull for bug reports
   - (Improvement) Enumerate the loaded assemblies
   - (Improvement) Rearrange the elements and put them inside scroll viewers to not strech the window
   - (Improvement) Allow to resize and maximize the window
- (Fix) Auto updater: From this version forward, the linux packages are correctly identified (linux, arch, rhel) and will download the same package as installed. Were downloading the linux-x64 no matter what

## 23/11/2021 - v2.25.1

- **Change resolution:**
   - (Add) Presets: 5K UHD, 6K and 8K UHD
   - (Add) Resulting pixel ratio information
   - (Add) Fix the pixel ratio by resize the layers images with the proposed ratio to match the new resolution
   - (Fix) New images could have noise when processed on linux and macos
- (Add) Layer slider debounce time to render the image [Configurable] (#343)
- (Fix) CTB v1: Incorrect getter for the LightOffDelay 
- (Fix) Reallocating layers were not notifying nor updating the layer collection about the changes, leading to wrong layer count
- (Fix) Undo and redo now also reverts the file resolution when changed

## 18/11/2021 - v2.25.0

- **File formats:**
   - (Add) Allow to partial open the files for read and/or change properties, the layer images won't be read nor cached (Fast)
   - (Add) More abstraction on partial save
- **Scripting:**
   - (Add) ScriptOpenFolderDialogInput - Selects a folder path
   - (Add) ScriptOpenFileDialogInput - Selects a file to open
   - (Add) ScriptSaveFileDialogInput - Selects a file to save
- (Add) [UNSAVED] tag to the title bar when there are unsaved changes on the current session
- (Improvement) Better handling of empty images on the UI

## 14/11/2021 - v2.24.4

- **File - Send to - Device**
   - (Add) Progress with the transfered megabyte(s) and allow to cancel the transfer
   - (Add) It will prompt for drive ejection [Configurable - On by default] [Windows only] (#340)
- (Fix) PhotonS: Some slicers will not fill the pixel RLE to the end when the remaining pixels are trailing black, this was triggering error on read because data checksum was incomplete, ignoring checksum now (#344)

## 12/11/2021 - v2.24.3

- (Upgrade) .NET from 5.0.11 to 5.0.12
- (Fix) Anycubic formats: LightOffDelay is WaitTimeBeforeCure (#308)

## 05/11/2021 - v2.24.2

- **Export layers to mesh:**
   - (Add) Mesh format: 3MF - 3D Manufacturing Format
   - (Add) Mesh format: AMF - Additive Manufacturing Format
   - (Add) Mesh format: WRL - Virtual Reality Modeling Language
   - (Add) Mesh format: PLY - Polygon File Format / Stanford Triangle Format
   - (Add) Mesh format: OFF - Object File Format
   - (Add) File format (Binary or ASCII) option selection on the UI
   - (Improvement) ASCII file types strict use \n instead of \r\n
- **Exposure time finder:**
   - (Add) Option to enable/disable bottom text on 'pattern model' mode
   - (Fix) Importing a profile with 'Pattern loaded model' enabled, will trigger an error and prevent the import
- **About box:**
   - (Add) AvaloniaUI version information
   - (Add) Copy loaded assemblies to clipboard
- (Downgrade) AvaloniaUI from 0.10.10 to 0.10.8 to fix macOS menus

## 03/11/2021 - v2.24.1

- (Fix) Export layers to mesh: Wrong orientation when output from a horizontal mirrored file

## 03/11/2021 - v2.24.0

- (Add) File formats properties:
   - `DisplayDiagonal`: Display diagonal size in millimeters
   - `DisplayDiagonalInches`: Display diagonal size in inches
   - `DisplayAspectRatio`: Display aspect ratio
- (Add) Action - Export layers to mesh: Reconstructs and export a layer range to a 3D mesh via voxelization (#329)
- (Add) Detect incorrect image ratio upon file load and warn user about it
- (Add) Export layers to heatmap: Mirror, rotate and merge layers on same position options
- (Improvement) Calibration - Elephant foot: For dimming method adds a warning text for files with emulated antialiasing and prevent run the test when AntiAliasing is less than 2
- (Improvement) Export layers to image, GIF and heatmap: Auto select the flip method based on sliced file mirror information, it will output images in thier original form/orientation
- (Upgrade) AvaloniaUI from 0.10.8 to 0.10.10
- (Upgrade) EmguCV from 4.5.3 to 4.5.4
- (Fix) Corrected incorrect label for default scripting directory (#322)
- (Fix) Recursively remove islands wasn't removing above islands (#333)
- (Fix) Dynamic layer heights: Incorrect progress bar when selecting a starting layer range bigger than 0
- (Fix) PrusaSlicer printers: Set vertical mirror to NovaMaker printers

## 12/10/2021 - v2.23.6

- **(Improvement) SL1:** (#314)
   - Complete the SL1 file format with some defaults for convertions
   - Change some data types from bool to byte as in recent prusaslicer changes
- (Upgrade) .NET from 5.0.10 to 5.0.11
- (Upgrade) AvaloniaUI from 0.10.7 to 0.10.8
- (Fix) PWS, PW0, PWM, PWMX, PWMO, PWMS: Incorrect set of layer height for the layer definition when using same positioned layers

## 07/10/2021 - v2.23.5

- (Fix) Odd crash when detecting for resin trap issues which was duplicating contours (#311, #312)

## 04/10/2021 - v2.23.4

- (Fix) RGB images was preventing file open/save/convertion

## 04/10/2021 - v2.23.3

- **Pixel arithmetic**
   - (Add) Apply to - Model surface: Apply only to model surface/visible pixels
   - (Add) Apply to - Model surface & inset: Apply only to model surface/visible pixels and within a inset from walls
   - (Add) Preset: Fuzy skin
   - (Improvement) Speed up the Corrode method
   - (Change) Heal anti-aliasing threshold from 169 to 119
- **Calibration - Grayscale:**
   - (Add) Enable or disable text
   - (Fix) Calibration - Grayscale: Crash program when redo (Ctrl+Z)
   - (Change) Some defaults to better values
- (Fix) Layer arithmetic: Crash program when redo (Ctrl+Z)

## 02/10/2021 - v2.23.2

- **Pixel arithmetic**
   - (Add) Corrode: Random dithering, use for eliminating visual prominence of layer lines. Can also be used to add a microtexture to enhance paint bonding (#307)
   - (Add) Ignore areas smaller or larger than an threshold
   - (Add) Preset - Heal anti-aliasing: Discard uncured faded pixels and turn them into solid black (0)
- (Fix) Resin traps: Discard traps with drain holes directly on first layer / build plate
- (Fix) The setting 'Auto flip layer on file load' is not respected when off

## 23/09/2021 - v2.23.1

- **Issues:**
   - (Add) Suction cups: Additional setting to specify the required minimum height to be flagged as a issue (#302)
   - (Change) Allow touching bounds to have a bounding rectangle and zoom into the issue
   - (Change) Disable the ability to copy issues row text from list as this is crashing the program
   - (Change) Decrease cache count of the layers from x10 to x5 to free RAM
   - (Fix) Touching bounds are reporting areas of 0
   - (Fix) Draw crosshair for issues are called multiple times
   - (Fix) Detection error when resin traps are enabled on some cases (#303)
   - (Fix) Resin trap false-positives (#305)
   - (Fix) When removing multiple islands at once only the first is cleared from the issue list
- **UI:**
   - (Change) Tool - Resize icon
   - (Change) Move "Crosshairs" button inside "Issues" button
- (Add) Tool - Morph - Offset crop: Like erode but discards the outer pixels
- (Fix) Corrected bottom lift unit label in light-off delay calculator (#304)

## 21/09/2021 - v2.23.0

- **Issues:**
   - **Suction cups:**
      - Add a auto repair feature for this issues by drill a vertical vent hole (#296)
      - Add a manual repair feature for this issues by drill a vertical vent hole when click on remove (#301)
   - (Add) Allow to group issues by type and/or layer on display list (Configurable)
   - (Add) Linked issues (Resin traps and suction traps) are now grouped into a single main issue (#300)
   - (Add) Tracker bar: Colorize the issue tracker map with it own colors (Configurable)
   - (Improvement) Order issues by area in descending order
   - (Improvement) Always bring the selected issue into UI view on the list
   - (Fix) When manually removing a issue from the list, it will no longer reselect other and make the user loss track of the remove issue view
   - (Fix) Allow to ignore all issue type
- Tool - Repair layers and issues:
   - (Improvement) Allow to have profiles in the dialog
   - (Improvement) Ignored issues are not repaired
- (Fix) Material ml calculation was calculating a bad value for PhotonWorkshop files
- (Fix) Settings were not being saved on systems that lacks special folder information, ie Rosetta. Now it will use up to 4 possible special folders as fallback if the prior doesn't exists (#299)

## 16/09/2021 - v2.22.0

- **UI:**
   - (Add) Context menu for ROI button at status bar with the following:
      - Mask: Select layer positive areas
      - Mask: Select layer hollow areas
      - ROI: Select model volume
      - ROI: Select layer volume
      - Clear Mask
      - Clear ROI
   - (Add) Allow to choose custom locations for "Send to"
   - (Add) Network remote printers: Send files remotely directly to printer
   - (Add) Layer image shortcut: Right-click + ALT + CTRL on a specific object to select all it enclosing areas as a Mask
   - (Improvement) Redesign numerical input box with value labels into the box
   - (Improvement) Layer tracker highlight issues: Scale line stroke to make sequential layers with issues shows all togther as a group
   - (Fix) Outline - Hollow areas: Not outlining the second closing contour for contours with child
   - (Fix) Pixel editor - Eraser: It was selecting the whole blob even if have inner parents
   - (Fix) Setting window: When open it will change tabs quickly to fix the windows height/scroll problem
   - (Fix) Minor problems with autosizes on some input boxes
- **Tools:**
   - **Import layers:**
      - (Add) 'MergeMax' to import type (#289)
      - (Add) 'AbsDiff' to import type
      - (Add) Description of operations on the combo box
   - **Dynamic Lifts:**
      - (Add) Set methods: Traditional and FullRange (#291)
      - (Improvement) Rearrange the layout
      - (Fix) Normal pixels were being used to calculate bottom lift speed
   - (Improvement) Solidify: Area threshold are now calculated by the real area instead of rectangle area
   - (Improvement) Dynamic layer height: Perform the maximum of layer pixels instead of sum to improve files with AA and prevent glare
- **Resin traps & Suction cups:**
   - **By Timothy Slater [tslater2006] (#292):**
   - (Add) Suction cups detection: Air trapped inside hollow areas that create a suction force. Calculated during resin trap algorithm.  
           This issues will not be auto fixed and require a vent hole as proper fix
   - (Improvement) New and improved algorithm for resin trap detection
- **Settings:**
   - (Add) Option: Loads the last recent file on startup if no file was specified
   - (Change) Resin trap hightlight default color
   - (Fix) Unable to set resin trap threshold to 0 (disabled)
- (Improvement) Better random generation for benchmark
- (Improvement) Allow to cancel the new version download
- (Improvement) Better version checker and file download methods
- (Fix) Disable Centroids by default on settings
- (Fix) Settings: Automations were not being cloned when required
- (Upgrade) .NET from 5.0.9 to 5.0.10

## 06/09/2021 - v2.21.1

- **(Add) Layer outline:**
   - Blob outline: Outline all separate blobs
   - Centroids: Draw a dot at the gemoetric center of a blob
- (Add) Adjust layer height: Allow to change exposure time on the dialog and inform that different layer thickness require different exposure times
- (Add) Resin trap detection: Allow to choose the starting layer index for resin trap detection which will also be considered a drain layer.  
        Use this setting to bypass complicated rafts by selected the model first real layer (#221)
- (Improvement) Disable mirroed preview when loading a file that is not mirroed

## 03/09/2021 - v2.21.0

- **UI:**
   - **Menu:**
      - (Add) File - Open recent: Open any recent open file from a list   
          Shift + Click: Open file in a new window   
          Shift + Ctrl + Click: Remove file from recent list
          Ctrl + Click: Purge non-existing files
      - (Add) File - Send to: Copy the file directly to a removable drive (Windows only)
   - **(Add) Layer navigation buttons:**
      - SB: Navigate to the smallest bottom layer in mass
      - LB: Navigate to the largest bottom layer in mass
      - SN: Navigate to the smallest normal layer in mass
      - LN: Navigate to the largest normal layer in mass
   - (Add) Layer outline - Distance detection: Calculates the distance to the closest zero pixel for each pixel
- **Tools:**
   - **Dynamic Lifts:**
      - (Improvement) Select normal layers by default
      - (Improvement) Hide light-off delay fields when the file format don't support them
      - (Fix) Light-off delay fields was not hidding when set a mode that dont require the extra time fields
   - **Exposure time finder:**
      - (Fix) Fix the 'light-off delay' field not being show on files that support wait time before cure
      - (Change) Field name 'Light-off delay' to 'Wait time before cure'
   - (Add) Fade exposure time: The double exposure method clones the selected layer range and print the same layer twice with different exposure times and strategies
   - (Add) Double exposure: The double exposure method clones the selected layer range and print the same layer twice with different exposure times and strategies
   - (Add) Clone layers: Option to keep the same z position for the cloned layers instead of rebuild model height
   - (Improvement) The layer range selector for normal and bottom layers now selects the correct range based on IsBottom property rather than layer index
   - (Fix) The layer range selector was setting a very high last layer index when bottom layer count is 0
   - (Fix) Pixel arithmetic: Threshold types "Otsu" and "Triangle" are flags to combine with other types, it will auto append the "Binnary" type
- (Add) Support for Encrypted CTB (read-only)

## 31/08/2021 - v2.20.5

- (Add) Setting - Max degree of parallelism: Sets the maximum number of concurrent tasks/threads/operations enabled to run by parallel method calls.   
   If your computer lags and freeze during operations you can reduce this number to reduce the workload and keep some cores available to other tasks as well.  
   <= 0: Will utilize however many threads the underlying scheduler provides, mostly this is the processor count.  
   1: Single thread. (#279)

## 29/08/2021 - v2.20.4

- (Fix) On some tools, calibration tests and even files when recalculating the Z layer position for the whole set, it will use the bottom setting for all layers

## 28/08/2021 - v2.20.3

- **Tool - Dynamic Layer Height:**
   - (Add) Option to strip anti-aliasing: Use this option if you get flashy layers or if you want to enhancement the results
   - (Add) Option to reconstruct anti-aliasing: Use this option with "Strip anti-aliasing" to reconstruct the layer anti-aliasing via an gaussian blur
   - (Add) Maximum wide difference: The maximum number of pixels wide difference to be able to stack layers, where one pixel difference is a whole perimeter of the object to be eroded.  
      0 = Stack only equal layers  
      n = Stack equal layers or with a n perimeter of difference between the sum of the stack (#274) 
   - (Add) Allow to change the base exposure times for the auto generation (#274)
   - (Add) Option to switch between: "Set the same base time for all bottoms" or "Calculate and iterate bottom exposures"
   - (Add) Button to: Copy automatic table data into manual table
   - (Improvement) Auto fill all layer height exposures times on manual entry
   - (Fix) When "Exposure set type = Multiplier" bottom exposure is being used for normal exposure (#274)
   - (Fix) Do not sum equal layers on the stack
- (Fix) Recalculate the material milliliters per layer when replacing a layer collection (#273)

## 27/08/2021 - v2.20.2

- **(Fix) Layers:**
   - Round properties before comparing to avoid the precision error
   - Prevent 'Wait time' properties from having negative values
   - The `RetractSpeed` and `RetractSpeed2` property wasn't setting the bottom speed for bottom layers, instead the normal retract speed was always used
   - Setting the `RetractHeight2` and `RetractSpeed2` property was not notifying the timer to update the print time
   - Propagate the whole global settings to layers now identfies the bottom layers per height instead of the layer index
- (Add) UVJ: Support TSMC for the file format
- (Fix) UVJ: Soft save was not updating the layer settings
- (Fix) CTB: TSMC not working properly due incorrect layer `LiftHeight` value calculation

## 26/08/2021 - v2.20.1

- **UI:**
   - (Add) Pixel position on lcd millimeters to the pixel picker information
   - (Add) Pixel size information when availiable below zoom on status bar
   - (Add) Click on Zoom button will zoom to 100% and shift click will set to the user defined value
- **CTB:**
   - (Add) Allow to change wait time for bottoms and normal layers separately
   - (Change) Software version field to 1.9.0
   - (Fix) Bottom layer count field was not being set in one of the tables
- (Fix) CXDLP: Force the 'Wait time before cure' to be 1 as minimum, or else 0 is preventing the print

## 24/08/2021 - v2.20.0

- **File formats:**
   - (Add) FlashForge SVGX format of FlashDLPrint
   - (Improvement) Change `DisplayMirror` from `bool` to a `FlipDirection` enumeration, to be able to identify the exact mirror direction
- **(Add) PrusaSlicer Printers:**
   - FlashForge Explorer MAX
   - FlashForge Focus 8.9
   - FlashForge Focus 13.3
   - FlashForge Foto 6.0
   - FlashForge Foto 8.9
   - FlashForge Foto 13.3
   - AnyCubic Photon Mono SQ
   - AnyCubic Photon Ultra
- (Add) Pixel arithmetic: Preset "Elephant foot compensation"
<!--
- **FileFormat PhotonWorkshop:**
   - (Add) Compability with version 515
   - (Add) Support for Photon Mono SQ (PMSQ)
   - (Add) Support for Photon Ultra (DLP)
   
!-->

## 22/08/2021 - v2.19.5

- **(Fix) CTB:**
   - Converting a file to version 4 won't port the TMSC values (#271)
   - Force version 3 when on version 4 and converting to photon or cbddlp
- **(Improvement) Export to SVG image:**
   - Group \<g\> all layer objects
   - Intersect all childs on same \<path\>
- **PrusaSlicer:**
   - (Rename) Printer keywords notes:
      - BottomWaitBeforeCure -> BottomWaitTimeBeforeCure
      - WaitBeforeCure -> WaitTimeBeforeCure
      - BottomWaitAfterCure -> BottomWaitTimeAfterCure
      - WaitAfterCure -> WaitTimeAfterCure
      - BottomWaitAfterLift -> BottomWaitTimeAfterLift
      - WaitAfterLift -> WaitTimeAfterLift
   - (Change) PrusaSlicer gcode printers to reflect the previous changes
   - (Change) PrusaSlicer Creality Hallot printers with better values by default
   - (Fix) PrusaSlicer printers with TMSC values was not being ported to the file format
- (Fix) CXDLP: The light-off delay is not present on the format, instead the wait time before cure is used, this was leading to high wait and print times
- (Fix) Converting from files that aren't both TSMC compatible won't set the bottom and lift height
- (Fix) Error when changing the layer collection with another with higher layer count than the previous

## 22/08/2021 - v2.19.4

- (Fix) CTBv2: Corrupt and unprintable file when saving

## 22/08/2021 - v2.19.3

- (Fix) CTB: Corrupt and unprintable file when saving (#270)

## 21/08/2021 - v2.19.2

- **File Formats:**
   - (Fix) Setting a global property that haven't a bottom counter-part will notify and set that value to bottom layers too
   - (Fix) TSMC: Update lift or retract height for the first time will set the `RetractHeight2` property to 0
   - (Fix) TSMC: `RetractHeight` is the first fast sequence paired with `RetractSpeed`
   - (Fix) TSMC: `RetractHeight2` paired with `RetractSpeed2` is performed after the `RetractHeight` and controls the height to retract to the next layer position (The slow stage)
   - (Improvement) When converting from a TSMC enabled to a TSMC unable file, the slowest retract speed will be enforced
- **Layer:**
   - (Add) `Number` property to get the layer number, 1 started
   - (Fix) `HaveGlobalParameters` property was not comparing  the `PositionZ` resulting in `true` when different heights are used but keeping all other settings the same
   - (Fix) `HaveGlobalParameters` property was not comparing the `WaitTimeAfterCure` for bottom layers
   - (Fix) `MaterialMilliliters` calculation with the real layer height instead of global information and recalculate when height changes (#266)
- **CTB:**
   - (Improvement) Discovered more unknown fields and set them accordingly
   - (Improvement) When all layers share same settings as globals it will set to follow global table instead of per layer settings
- (Fix) VDT: Wrong binding to json 'retract_distance2' key
- (Fix) XYZ Accuracy: 'Total height' text showing a F3 and no decimals

## 19/08/2021 - v2.19.1

- (Add) Setting - Allow to resize the tool windows: Check this option if you have problems with content being cut on some windows, down-size the height by a bit and then expand to fix the content.
- (Fix) File formats: When converting from a TSMC-able file to an TSMC-unable file, the LiftHeight will be set to the total lift (1+2) as fail-safe guard
- (Fix) Pixel Arithmetic: Keep pattern visible by default to prevent content from being cut when made visible
- **(Fix) CTBv4:**
   - LiftHeight and LiftHeight2 properties when using TSMC, LiftHeight on CTB is the total of lifts 1+2
   - Soft-save is corrupting the file

## 17/08/2021 - v2.19.0

- **File formats:** 
   - Add and remove some image types that can be open
   - (Add) `CanProcess` method to know if a file can be read under a format and to allow diferent formats with same extension
   - (Fix) `LiftHeightTotal` and `RetractHeight` was rounding to no decimals and returning wrong values
   - (Improvement) Round all float setters on `Layer` class
   - (Improvement) Decode/encode RAM usage and performance by processing in batch groups
- **Pixel Dimming:** (#262)
   - (Add) Option "Lightening pixels" to add brightness/lightening instead of dimming/subtract pixels
   - (Fix) "Dim walls only" would reset body brightness by increase pixel brightness two times it value
- **Pixel Arithmetic:**
   - (Change) Transpose "Pixel Dimming" to "Pixel Arithmetic"
   - (Improvement) New options and manipulations
- **(Fix) Exposure time finder:**
  - Generate top staircase based on selected measure (px or mm)
  - Zebra bars when used in mm measures, it was using X density instead Y to calculate the thickness
  - Move 'Unit of measure' to 'Object configuration'
  - Custom text with wrong Y position when using out of portion resolutions/LCDs
- **CTBv4:**
  - (Fix) More Unknown fields discovered and implemented
  - (Fix) Reserved table is 384 bytes instead of 420
  - (Fix) When full encoding it was forcing to change to version 3. This also affected convertions. (#263)
  - (Fix) `BottomRetractHeight2` was being set to `BottomRetractSpeed2`
  - (Fix) `RetractHeight2` was being set to `RetracSpeed2`
  - (Fix) The PrintParametersV4 table address
  - (Fix) Generates invalid files to open with Chitubox and printers (#263)
  - (Fix) Better progress report
- **(Add) PrusaSlicer printer notes variables:**
  - BottomLiftHeight2
  - BottomLiftSpeed2
  - LiftHeight2
  - LiftSpeed2
  - BottomRetractSpeed
  - BottomRetractSpeed2
  - BottomRetractHeight2
  - BottomRetractSpeed2
  - RetractHeight2
  - RetractSpeed2
- **UI:**
  - (Add) File - Open current file folder (Ctrl+Shift+L): Locate and open the folder that contain the current loaded file
  - (Improvement) Hide some virtual extensions from file open dialog filters
  - (Improvement) UI: Refresh active thumbnail when changed
  - (Change) Icon for File - Open and Open in a new file 
  - (Change) Rename File - Extract to: Extract file contents
- (Upgrade) AvaloniaUI from 0.10.6 to 0.10.7
- (Fix) PhotonS: Allow to use different resolution than printer default
- (Fix) PW0, PWM, PWMX, PWMO, PWMS: Unable to decode some files with AntiAliasing (#143)

## 12/08/2021 - v2.18.0

- **Command line arguments:**
   - (Add) Convert files: UVtools.exe -c \<inputfile\> \<outputfile1/ext1\> [outputfile2/ext2]
   - (Add) Extract files: UVtools.exe -e \<inputfile\> [output_folder]
   - https://github.com/sn4k3/UVtools#command-line-arguments
- **File formats:** 
   - (Add) Implement TSMC (Two Stage Motor Control) for the supported formats
   - (Add) Implement 'Bottom retract speed' for the supported formats
   - (Add) LGS: Support for lgs120 and lg4k (#218)
   - (Add) CTB: Special/virtual file extensions .v2.ctb, .v3.ctb, .v4.ctb to force a convertion to the set version (2 to 4). The .ctb is Version 3 by default when creating/converting files
   - (Improvement) Better performance for file formats that decode images in sequential pixels groups
- **GCode:**
  - (Improvement) Better parsing of the movements / lifts
  - (Improvement) Better handling of lifts performed after cure the layer
  - (Improvement) More fail-safe checks and sanitize of gcode while parsing
- (Improvement) CTBv3: Enable per layer settings if disabled when fast save without reencode
- (Upgrade) .NET from 5.0.8 to 5.0.9
- (Fix) PrusaSlicer printer: Longer Orange 4k with correct resolution and display size
- (Fix) Odd error when changing properties too fast in multi-thread

## 08/08/2021 - v2.17.0

- **Windows MSI:**
   - (Fix) Use the folder programs x64 instead of x86 for new installation path (#254)
   - (Improvement) Mark program and all files as x64
   - (Improvement) Add UVtools logo to side panel and top banner
   - (Improvement) Add open-source logo to side panel
   - (Improvement) License text aligment and bold title
- (Add) File format: OSLA / ODLP / OMSLA - Universal binary file format
- (Add) Calibration - Lift height: Generates test models with various strategies and increments to measure the optimal lift height or peel forces for layers given the printed area
- (Add) Layer Actions - Export layers to images (PNG, JPG/JPEG, JP2, TIF/TIFF, BMP, PBM, PGM, SR/RAS and SVG)
- (Add) About box: License with link
- (Add) Include a copy of the LICENSE on the packages
- (Improvement) File formats: Implement `Wait time before cure` properties on file formats with light-off delay, when used it will calculate the right light-off delay with that extra time and set to `LightOffDelay` property
- (Improvement) Change all date times to Utc instead of local
- (Fix) Tool - Flip: 'Both' were not working correctly
- (Fix) Linux: File 'UVtools.sh' with incorrect line break type, changed to \n (#258)

## 01/08/2021 - v2.16.0

- **(Add) PrusaSlicer printers:**
   - Creality HALOT-MAX CL-133
   - Nova3D Elfin2
   - Nova3D Elfin2 Mono SE
   - Nova3D Elfin3 Mini
   - Nova3D Bene4
   - Nova3D Bene5
   - Nova3D Whale
   - Nova3D Whale2
- **About box:**
  - (Add) Runtime information
  - (Fix) Limit panels width and height to not overgrow
- **(Fix) macOS:**
   - Version auto-upgrade (Will only work on future releases and if running v2.16.0 or greater)
   - Demo file not loading
   - Auto disable windows scaling factor when on Monjave or greater on new instalations
- (Add) Tool: Raise platform on print finish (#247)
- (Add) CXDLP: Support for Halot MAX CL-133
- (Improvement) Tools: Better handling/validation of tools that are unable to run with abstraction
- (Improvement) CWS: Simplify filenames inside the archive
- (Upgrade) EmguCV from 4.5.2 to 4.5.3
- (Change) Allow to set layer `LightPWM` to 0
- (Fix) Arrange dropdown arrow from layer image save icon to be at center

## 24/07/2021 - v2.15.1

- **(Improvement) CWS:**
   - Remove light-off delay from the format
   - Sync movements with a delay time
   - Auto convert the light-off delay time to wait before cure time when required
- **(Improvement) CTB:**
   - When positively set the 'Wait time before cure' property on a CTBv3 or lower, it will compute the right light-off delay with that extra time into consideration
   - When positively set any of the light-off delays on a CTBv4 it will auto zero the 'Wait times' properties and vice-versa
   - Automation to set light-off delay on file load, will no longer do when any of 'Wait times' are defined for a CTBv4
- (Improvement) PrusaSlicer printers that use .cws format, implement the wait times on printer notes
- (Upgrade) .NET from 5.0.7 to 5.0.8
- (Fix) GCode parser: Commented commands were being parsed
- (Fix) Exposure time information on bottom status bar was inverted, showing normal/bottom time instead of bottom/normal
- (Fix) macOS: Installing libusb is no longer a requirement

## 16/07/2021 - v2.15.0

- **File formats:**
   - (Add) Wait time before cure: The time to rest/wait in seconds before cure a new layer
   - (Add) Wait time after cure: The time to rest/wait in seconds after cure a new layer
   - (Add) Wait time after lift: The time to rest/wait in seconds after a lift/peel move
   - (Change) All gcode file formats dropped light-off delay field in favor of new 'Wait time before cure' field, setting light-off delay still valid but it redirects to the new field
   - (Change) Reorder 'Light-off delay' before 'Exposure time'
   - (Improvement) Recalculate the print time when a related property changes
   - (Fix) Generic time estimation calculation was ignoring exposure times
   - (Fix) Unable to load files with uppercase extensions
   - (Fix) ZIP: Use G1 at end of gcode instead of G0
   - (Fix) CWS: Use G1 for movements
   - **(Fix) CXDLP:** (#240)
      - Layer area calculation
      - Validation checksum 
   - **(Fix) ZCODE:**
      - Use G1 at end of gcode instead of G0 to prevent crash to top bug on firmware
      - Put back the M18 motors off at end of gcode
- **GCode Builder/Parser:**
   - (Add) Allow to choose between G0 and G1 for layer movements and end gcode
   - (Fix) Safe guard: If the total print height is larger than set machine Z, do not raise/lower print on completeness
   - (Fix) Light-off delay is the real delay time and not the calculated movement of the lifts plus the extra time
   - (Improvement) Parse gcode line by line instead searching on a group of layers to allow a better control and identification
- **Tools:**
   - **Change print parameters:**
      - (Add) Tooltips to labels
      - (Add) Sun UTF-8 to the Light PWM value unit to describe intensity
   - (Improvement) Dynamic lifts: Round lift height and speed to 1 decimal
   - (Fix) Exposure time finder: Time estimation when using 'Use different settings for layers with same Z positioning'
- **Prusa Slicer:**
  - (Add) Note keyword: BottomWaitBeforeCure_xxx
  - (Add) Note keyword: WaitBeforeCure_xxx
  - (Add) Note keyword: BottomWaitAfterCure_xxx
  - (Add) Note keyword: WaitAfterCure_xxx
  - (Add) Note keyword: BottomWaitAfterLift_xxx
  - (Add) Note keyword: WaitAfterLift_xxx
  - (Change) Uniz IBEE: Remove light-off delay and implement wait time keywords
- **macOS:** (#236)
  - (Remove) osx legacy packages from build and downloads
  - (Fix) macOS: Simplify the libcvextern.dylib and remove libtesseract dependency
  - (Fix) macOS: Include libusb-1.0.0.dylib
  - Note: `brew install libusb` still required
- **UI:**
  - (Add) Shorcuts: Arrow up and down to navigate layers while layer image is on focus
  - (Fix) Refresh gcode does not update text on UI for ZIP, CWS, ZCODEX files
  - (Fix) Operations: Import a .uvtop file by drag and drop into the UI would not load the layer range
  - (Change) When convert a file, the result dialog will have Yes, No and Cancel actions, where No will open the converted file on current window, while Cancel will not perform any action (The old No behaviour)

## 12/07/2021 - v2.14.3

- (Add) Exposure time finder: Base layers print modes, a option to speed up the print process and merge all base layers in the same height
- (Add) GCode tab: Allow to temporarily edit and use custom gcode
- (Change) Zcode: Omit M18 at end of the gcode to prevent carrier goes up and crash to the limit at a end of a print

## 11/07/2021 - v2.14.2

- **Exposure time finder:**
   - (Add) [ME] Option: 'Use different settings for layers with same Z positioning'
   - (Add) [ME] Option: 'Lift height' for same Z positioned layers
   - (Add) [ME] Option: 'Light-off delay' for same Z positioned layers
   - (Improvement) Auto-detect and optimize the 'multiple exposures' test to decrease the print time, by set a minimal lift to almost none
   - (Improvement) Better information on the thumbnail
   - (Fix) Importing a profile would crash the application
   - (Fix) Error with 'Pattern loaded model' fails when generating more models than build plate can afford (#239)
- **GCode:**
   - (Fix) When the last layer have no lifts and a move to top command is set on end, that value were being set incorrectly as last layer position
   - (Fix) Layer parsing from mm/s to mm/m bad convertion
- (Add) File formats: Setter `SuppressRebuildGCode` to disable or enable the gcode auto rebuild when needed, set this to false to manually write your own gcode
- (Add) UVtools version to error logs
- (Fix) ZCode: Some test files come with layer height of 0mm on a property, in that case lookup layer height on the second property as fallback

## 07/07/2021 - v2.14.1

- (Upgrade) EmguCV from 4.5.1 to 4.5.2
- **File formats:**
   - (Add) Getter `IsAntiAliasingEmulated`: Gets whatever a file format uses real or emulated AntiAliasing
   - (Add) Getter `IsDisplayPortrait`: Gets if the display is in portrait mode
   - (Add) Getter `IsDisplayLandscape`: Gets if the display is in landscape mode
- **Tool - Resize:** (#235)
   - (Fix) Division by 0 when start layer is equal to end layer
   - (Fix) Calculations when using the option "Increase or decrease towards 100%"
- (Add) About window: OpenCV build number and a button to copy build information to clipboard
- (Improvement) Exposure time finder: Improve the **Multiple brightness** section to auto fill with correct values for file formats that use time fractions to emulate AntiAliasing, this can be used to replace the **Multiple exposures** section
- (Fix) UVJ: Error when using a null or empty layer array on manifest `config.json` file (#232)
- (Fix) GCode parser: When only a G4/wait command is present on a layer it was setting the global exposure time and discard the this value per layer

## 03/07/2021 - v2.14.0

- **File Formats:**
   - (Add) SL1S: Prusa SL1S Speed
   - (Add) CTB v4 support (318570758)
   - (Improvement) PHOTON, CBDDLP, CTB v2, PHZ: Disallow per layer settings, beside the format support, printers never use it
   - (Improvement) Longer Orange format with new found keys
   - (Improvement) CXDLP: Fix the resolution from CL-60 and trigger an error when a invalid resolution was set and unable to detect the printer model
- **Prusa Slicer:**
   - (Add) UVtools Prusa SL1S SPEED
   - (Add) Longer Orange 120
   - (Fix) Creality HALOT-ONE CL-60: Flip resolution & display and remove mirror
   - (Fix) Creality HALOT-SKY CL-89: Remove mirror
   - (Improvement) Longer Orange printers with better default settings
- (Add) Button on `Help - Sponsor`: Open github sponsor webpage

## 25/06/2021 - v2.13.4

- (Fix) ZCode: lcd.gcode was blank / not generating when converting from any file format
- (Fix) Zcodex: Change MaterialId from `uint` to `string` (#223, #226)
- (Fix) CXDLP: Set the default printer name to `CL-89` when creating new instance, was `null` before
- (Fix) Some tools were unable to pull certain settings from profiles and imported settings:
   - Elephant foot
   - Exposure finder
   - Grayscale
   - Stress tower
   - Tolerance
   - XYZ Accuracy
   - Change resolution
   - Dynamic lifts
- (Change) `Layer repair` icon at Issues tab and `Outline` icon on preview toolbar (#227)
- (Developers) Created `UVtools.AvaloniaControls` project with `AdvancedImageBox` control for AvaloniaUI

## 12/06/2021 - v2.13.3

- **File formats:**
   - (Add) CXDLP v2 (#214)
   - (Improvement) GR1, MDLP and CXDLP decode/encode performance and memory optimization
   - (Remove) CXDLP v1 from available formats
- (Add) Pixel editor - Drawing: New brushes of shapes/polygons with rotation
- (Add) Island repair: Attempt to attach the islands down to n layers when found a safe mass below to support it.
- (Upgrade) .NET from 5.0.6 to 5.0.7
- (Fix) When there are issues on the list, executing any operation will navigate to the last layer
- (Fix) PrusaSlicer printer: Rename "Creality HALOT-SKY CL-60" to "Creality HALOT-ONE CL-60"

## 06/06/2021 - v2.13.2

- (Upgrade) AvaloniaUI from 0.10.5 to 0.10.6
- (Add) Pixel editor - Text: Allow multi-line text and line alignment modes

## 29/05/2021 - v2.13.1

- (Add) Layer preview - Outline: Skeletonize
- (Add) Actions - Export layers to skeleton: Export a layer range to a skeletonized image that is the sum of each layer skeleton
- (Add) Pixel editor - Text: Allow to rotate text placement by any angle (#206)
- (Add) Calibrate - XYZ Accuracy: Drain hole diameter (#205)

## 23/05/2021 - v2.13.0

- (Add) Tool - Light bleed compensation: Compensate the over-curing and light bleed from clear resins by dimming the sequential pixels
- (Add) Infill: Honeycomb infill type
- (Upgrade) MessageBox from 1.2.0 to 1.3.1 to fix the small size messages 

## 20/05/2021 - v2.12.2

- (Add) Layer action - Export layers to heat map: Export a layer range to a grayscale heat map image that represents the median of the mass in the Z depth/perception. The pixel brightness/intensity shows where the most mass are concentrated.

## 19/05/2021 - v2.12.1

- (Upgrade) AvaloniaUI from 0.10.4 to 0.10.5
- (Improvement) VDT: Implement a new key for better auto convert files between with latest version of Tango
- (Change) Exposure time finder: Bar fence offset from 2px to 4px

## 17/05/2021 - v2.12.0

- **Layer arithmetic:**
   - (Add) Allow to use ':' to define a layer range to set, eg, 0:20 to select from 0 to 20 layers
   - (Improvement) Modifications with set ROI and/or Mask(s) are only applied to target layer on that same regions
   - (Improvement) Disallow set one layer to the same layer without any modification
   - (Improvement) Clear and sanitize non-existing layers indexes
   - (Improvement) Disable the layer range selector from dialog
   - (Fix) Prevent error when using non-existing layers indexes
   - (Fix) Allow use only a mask for operations
   - (Fix) Implement the progress bar
- **File formats:**
   - (Add) VDA.ZIP (Voxeldance Additive)
   - (Improvement) Add a check to global `LightPWM` if 0 it will force to 255
   - (Improvement) Add a check to layer `LightPWM` if 0 it will force to 255
- (Add) Allow to save the selected region (ROI) to a image file
- (Update) .NET 5.0.5 to 5.0.6
- (Fix) Getting the transposed rectangle in fliped images are offseting the position by -1
- (Fix) Tools: Hide ROI Region text when empty/not selected

## 13/05/2021 - v2.11.2

- (Improvement) Applied some refactorings on code
- (Fix) MDLP, GR1 and CXDLP: Bad enconde of display width, display height and layer height properties

## 11/05/2021 - v2.11.1

- **Shortcuts:**
   - (Add) (Ctrl + Shift + R) to turn on and cycle the Rotate modes
   - (Add) (Ctrl + Shift + F) to turn on and cycle the Flip modes
   - (Add) (Ctrl + Shift + B) to select the build volume as ROI
- **GUI:**
   - (Add) Allow to drag and drop '.uvtop' files into UVtools to sequential show and load operations from files
   - (Change) Rotate icon on layer preview
   - (Upgrade) AvaloniaUI from 0.10.3 to 0.10.4
- **Tools:**
   - (Add) 'Reset to defaults' button on every dialog
   - (Improvement) Window size and position handling
   - (Improvement) Constrain profile box width to not stretch the window
   - (Improvement) ROI section design
- **Dynamic lift:**
   - (Add) View buttons to show the largest/smallest layers
   - (Add) Light-off mode: Set the light-off with an extra delay
   - (Add) Light-off mode: Set the light-off without an extra delay
   - (Add) Light-off mode: Set the light-off to zero
   - (Improvement) Disable bottom and/or normal layer fields when the selected range is outside
- (Add) Settings - Automations: Light-off delay set modes
- (Fix) Exposure time finder: Add staircase, bullseye and counter triangles to feature count at thumbnail

## 08/05/2021 - v2.11.0

- **Tools:**
   - (Add) Pixel Arithmetic
   - (Add) Layer arithmetic: Operator $ to perform a absolute difference
   - (Add) Allow to save and auto restore operation settings per session (#195)
   - (Add) Allow to auto select the print volume ROI
   - (Add) Allow to export and import operation settings from files
   - (Improvement) Calculator - LightOff delay: Hide the bottom properties or the tab if the file format don't support them (#193)
   - (Change) 'Arithmetic' to 'Layer arithmetic'   
   - (Remove) 'Threshold pixels'
   - (Fix) Solidfy was unable to save profiles
   - (Fix) A redo operation (Ctrl + Shift + Z) wasn't restoring the settings when a default profile is set
- **Operations:**
   - (Fix) Passing a roi mat to `ApplyMask` would cause unwanted results
   - (Improvement) Allow pass a full/original size mask to `ApplyMask`
- **Scripting:**
   - (Add) an script to create an printable file to clean the VAT (#170)
   - (Improvement) Allow to change user input properties outside the initialization
   - (Improvement) Auto format numerical input box with the fixed decimal cases
- (Add) Settings: Section 'Tools'
- (Improvement) GUI: The 'Lift, Retract and Light-off' at status bar now only shows for the supported formats
- (Fix) Print time estimation calculation was wrong since v2.9.3 due a lacking of parentheses on the logic

## 07/05/2021 - v2.10.0

- **Exposure time finder:**
   - Add a enable option for each feature
   - Add a staircase: Creates an incremental stair at top from left to right that goes up to the top layer
   - Add a section dedicated to the bullseye and revamp the design
   - Add a section for counter triangles (this will take the space of the old bullseye)
   - Allow negative fence offset for zebra bars
   - Allow to preview the exposure time information
   - Changed some defaults
- (Add) Layer actions - Export layers to animated GIF
   - **Note:** Non Windows users must install 'libgdiplus' dependency in order to use this tool
- (Add) Tools - Dynamic lifts: Generate dynamic lift height and speeds for each layer given it mass
- (Improvement) File formats using json files are now saved with human readable indentation
- (Fix) GCode builder: Raise to top on completion command was not being sent when feedrate units are in mm/min 
- (Fix) Tools - Layer Range Selector: Fix the 'to layer' minimum to not allow negative values and limit to 0

## 04/05/2021 - v2.9.3

- (Upgrade) AvaloniaUI from 0.10.2 to 0.10.3
- (Change) PrusaSlicer printers: Set 'Light-Off Delay' and 'Bottom Light-Off Delay' to 0 as default to allow UVtools auto-compute the right value once open the file in
- **Exposure time finder:**
   - Optimize layers by merge same position/exposure layers
   - Add a fence option to the zebra bars
   - Add a option to pattern the loaded model and generate multiple exposures on that
   - Allow to generate tests without the holes feature
   - Prevent from using more chamfers than the base height
   - Removed two of the largest holes
   - Hide not so often used 'Multiple brightness' and 'Multiple layer height' by default
   - Disable Anti-Aliasing by default
   - Change 'Part margin' from 1.5mm to 2.0mm
   - Change 'Multiple exposures - Bottom exposure step' default to 0
   - Fix a error when generating tests with multiple exposures
   - Fix some typos
- **XYZ accuracy test:**
   - Change 'Wall thickess' default from 2.5mm to 3.0mm
- (Improvement) Allow compute print time without a lift sequence

## 30/04/2021 - v2.9.2

- (Upgrade) AvaloniaUI from 0.10 to 0.10.2
- (Remove) Unused assemblies
- **Issues:**
   - Improve the performance when loading big lists of issues into the DataGrid
   - Auto refresh issues on the vertical highlight tracker once cath a modification on the Issues list
- **Layer preview - Difference:**
   - Layer difference will now only check the pixels inside the union of previous, current and next layer bounding rectangle, increasing the performance and speed
   - Previous and next layer pixels if both exists was not showing with the configured color and using the next layer color instead
   - Respect Anti-Aliasing pixels and fade colors accordingly
   - Unlock the possiblity of using the layer difference on first and last layer
   - Add a option to show similar pixels instead of the difference
   - Change previous default color from (255, 0, 255) to (81, 131, 82) for better depth preception
   - Change next default color from (0, 255, 255) to (81, 249, 252) for better depth preception
   - Change previous & next default color from (255, 0, 0) to (246, 240, 216) for better depth preception
- **(Fix) Pixel editor:**
   - Modification was append instead of prepend on the list 
   - Modification was not updating the index number on the list
- (Fix) PrusaSlicer printer: Bene4 Mono screen, bed and height size

## 18/04/2021 - v2.9.1

* **File formats:**
   * PhotonS: Implement the write/encode method to allow to use this format and fix the thumbnail
   * VDT: Allow to auto convert the .vdt to the target printer format using the Machine - Notes, using a flag: FILEFORMAT_YourPrinterExtension, for example: FILEFORMAT_CTB
   * (Fix) Unable to convert files with no thumbnails to other file format that requires thumbnails
* **Tools:**
   * (Add) Re-height: Option to Anti-Aliasing layers
   * (Fix) Morph and Blur: The combobox was not setting to the selected item when preform a redo operation (Ctrl+Shift+Z)
* **GUI:**
   * (Change) Progress window to be a grid element inside MainWindow, this allow to reuse the graphics and its elements without the need of spawning a Window instance everytime a progress is shown, resulting in better performance and more fluid transaction
   * (Improvement) Clear issues when generating calibration tests
   * (Fix) In some cases the process remains alive after quit the program

## 14/04/2021 - v2.9.0

* **File formats:**
   * Add Voxeldance Tango (VDT)
   * Add Makerbase MKS-DLP (MDLPv1)
   * Add GR1 Workshop (GR1)
   * Add Creality CXDLP (CXDLP)
   * When decoding a file and have a empty resolution (Width: 0 or Height: 0) it will auto fix it by get and set the first layer image resolution to the file
   * Fix when decoding a file it was already set to require a full encode, preventing fast saves on print parameters edits
* **GUI:**
   * When file resolution dismatch from layer resolution, it is now possible to auto fix it by set the layer resolution to the file
   * When loading a file with auto scan for issues disabled it will force auto scan for issues types that are instant to check (print height and empty layers), if any exists it will auto select issues tab
* **(Add) PrusaSlicer printers:**
   * Creality HALOT-SKY CL-89
   * Creality HALOT-SKY CL-60
* (Improvement) Tool - Adjust layer height: Improve the performance when multiplying layers / go with higher layer height
* (Fix) PrusaSlicer Printer - Wanhao D7: Change the auto convertion format from .zip to .xml.cws

## 08/04/2021 - v2.8.4

* (Improvement) Layers: "IsBottomLayer" property will now computing the value taking the height into consideration instead of it index, this allow to identify the real bottom layers when using multiple layers with same heights
* (Fix) GCode Builder: Finish print lift to top was setting the incorrect feedrate if the file format is not in mm/m speed units
* **Operations:**
   * **Exposure time finder:**
      * Add option to "Also set light-off delay to zero" when "Do not perform the lifting sequence for layers with same Z positioning" is enabled
      * Layers heights with more than 3 decimals was limiting the layer generation to 2 decimals leading to wrong the layer thickness
      * Allow set layer heights with 3 decimals
   * **Elephant foot:**
      * Bottom and normal layers count was showing with decimals
      * Allow set layer heights with 3 decimals
   * XYZ Accuracy: Allow set layer heights with 3 decimals
   * Tolerance XYZ: Allow set layer heights with 3 decimals
   * Grayscale: Allow set layer heights with 3 decimals
   * Stress tower: Allow set layer heights with 3 decimals
   * Calculator - Optimal model tilt: Allow layer heights with 3 decimals

## 07/04/2021 - v2.8.3

* File formats: Sanitize and check layers on encoding/saving file, will thrown a error and prevent the save if found any
* GCode Parser: Do not sanitize the lack of lift height on a file to allow read files back with no lift's on the layers
* CWS: Zips containing files without numbers was interrupting the decode method on first cath (#180)
* Tool - Change resolution: Only manipulate the layer image if the new resolution is different from the image resolution

## 05/04/2021 - v2.8.2

* **Operations:**
   * Force validate all operations on execute if they haven't been before, if validation fails it will show the message error
   * Add some utility functions to ease layer and mat allocation in the file
   * Layer remove: Disallow to select and remove all layers
   * Tool - Edit print parameters: Retract speed wasn't propagating the value to normal layers 
* File formats: Better control, set and checking for null/empty thumbnails

## 04/04/2021 - v2.8.1

* Clipboard Manager: As now full backups are made when removing or adding layers, the undo and redo no longer rebuilds layer properties nor Z heights
* Resin traps: Improved the detection to group areas with cross paths and process them as one whole area, this also increase the detection speed and performance (#179, #13)
* Action - Layer import: Fix a bug preventing to import layers of any kind

## 28/03/2021 - v2.8.0

* **Scripting:**
   * Add scripting capability, this allow to run external scripts inside the GUI and take advantage of visual layout to display user input fields
   * Scripts run under the "Roslyn Scripting API" and can make use of the whole C# language, this mean a huge boost compared to PowerShell scripts
   * Scripts are written in the same way UVtools is, by learning and programing scripts you are learning the UVtools core
   * For more information see the script sample: https://github.com/sn4k3/UVtools/tree/master/UVtools.ScriptSample
   * To run scripts go to: Tools - Scripting
* **File formats:**
   * Add a check and warning when opening an file that have a diferent layer and file resolution
* **Issues:**
   * Add "Print height" as new type of issue detection, all layers that goes beyond maximum printer Z height will be flagged as PrintHeight issue
   * Print height issues will not be automatical fixed, however user can fix it by remove some layers to counter the problem, still is recommended to resize object on slicer
   * Fix unable to compute issues when only islands or overhangs are selected to be detected alone (#177)
* **Settings:**
   * Add default directory for scripts on "General - File dialogs"
   * Add checkbox on "Issues - Compute - Print height" to enable or disable this type of detection
   * Add numerical on "Issues - Print height - Offset" to define a custom offset from Z top
   * Fix default directories input width to not grow with text, it was overflowing on large strings
* **Menu - Help:**
   * Add web link to "Wiki & tutorials"
   * Add web link to "Facebook group"
   * Add web link to "Report a issue"
   * Add web link to "Ask a question"
   * Add web link to "Suggest an improvement or new features"

## 28/03/2021 - v2.7.2

* **Core:**
   * Fix some improper locks for progress counter and change to Interlocked instead
   * Fix a bug when chaging layer count by remove or add layers it will malform the file after save and crash the program with some tools and/or clipboard
   * Fix when a operation fails by other reason different from cancelation it was not restoring the backup
   * When manually delete/fix issues it will also backup the layers
* **LayerManager:** 
   * LayerManager is now readonly and no longer used to transpose layers, each FileFormat have now a unique `LayerManager` instance which is set on the generic constructor
   * Implemented `List<Layer>` methods to easy modify the layers array
   * Changing the `Layers` instance will now recompute some properties, call the properties rebuild and forced sanitize of the structure
   * Better reallocation methods
* **Clipboard Manager:**
   * Add the hability to do full backups, they will be marked with an asterisk (*) at clipboard items list
   * When a partial backup is made and it backups all the layers it will be converted to full backup
   * Clipboard can now restore a snapshot directly with `RestoreSnapshot`
   * Prevent restore the initial backup upon file load and when clearing the clipboard
   * Clip's that change the layer count will perform a full backup and also do a fail-safe backup behind if previous clip is not a full backup
* **Pixel dimming:**
   * Allow to load an image file as a pattern (Do not use very large files or it will take much time to dump the data into the textbox)
   * Empty lines on patterns will be discarded and not trigger validation error

## 24/03/2021 - v2.7.1

* **File formats:**
   * Add a layer height check on file load to prevent load files with more decimal digits than supported to avoid precision errors and bugs
   * Fix a wrong cast causing seconds to miliseconds convertion to be caped to the wrong value
   * Internally if a layer colection was replaced, all new layers will be marked as modified to avoid forgeting and ease the code
* **Tools:**
   * Pixel dimming: Better render quality, it now respects AA better and produce better walls (#172)
   * Elephant foot: It now respects AA better and produce better walls for wall dimming 
   * Layer Import: Cancelling the operation while importing layers was permanent supresseing layer properties update when changing a base property
* (Change) PrusaSlicer print profiles: Improved raft height and bottom layer count for better print success, less delamination, shorter time and reduce wear of FEP
* (Scripts): Add operation "Validate" pattern to docs and examples (#172)

## 19/03/2021 - v2.7.0

* **Core:**
   * Write "Unhandled Exceptions" to an "errors.log" file at user settings directory
   * Increase layer height max precision from 2 to 3 decimals
* **Settings - Layer Preview:**
   * Allow to set hollow line thickness to -1 to fill the area
   * Add tooltip for "Auto rotate on load": Auto rotate the layer preview on file load for a landscape viewport
   * Add option to masks outline color and thickness
   * Add option to clear ROI when adding masks
   * Add option "Auto flip on load": Auto flip the layer preview on file load if the file is marked to print mirrored on the printer LCD
* **Layer preview:**
   * Add selectable rotation directions 90º (CW and CCW)
   * Add preview flip (CTRL+F) horizontally and/or vertically
   * Add maskable regions to process on a layer (SHIFT + Alt + Right-Click) on a area 
   * ROI: Shortcut "Shift + left click" now also selects hollow black areas inside a white perimeter
   * ROI: Shortcut "ESC + Shift" to clear only the ROI and leave masks in
   * Fix a crash when using the pixel picker tool outside image bounds
* **Pixel editor:**
   * Change drawings brush diameter limit from 255 to 4000 maximum
   * When using layers below go lower than layer 0 it no longer apply the modifications
* **File formats:**
   * Add an internal GCodeBuilder to generate identical gcode within formats with auto convertion, managing features and parsing information 
   * Internally rearrange formats properties and pass values to the base class
   * Fix "Save As" filename when using formats with dual extensions
   * CBDDLP and CTB: "LightPWM" was setting "BottomLightPWM" instead
   * CWS: Fix problem with filenames with dots (.) and ending with numbers (#171)
   * CWS: Improved the enconding and decoding performance
   * CWS: Implement all available print paramenters and per layer, missing values are got from gcode interpretation
   * CWS: Use set "light off delay" layer value instead of calculating it 
   * CWS: Get light off delay per layer parsed from gcode
   * CWS - RGB flavour (Bene4 Mono): Warn about wrong resolution if width not multiples of 3 (#171)
   * ZCode: Allow to set Bottom and Light intensity (LightPWM) on paramters and per layer
   * ZCode: Allow to change bottom light pwm independent from normal light pwm
   * LGS: Light off and bottom light off was setting the value on the wrong units
   * UVJ: Unable to set per layer parameters
* **Issues:**
   * When computing islands and resin traps together, they will not compute in parallel anymore to prevent CPU and tasks exaustion, it will calculate islands first and then resin traps, this should also speed up the process on weaker machines
   * Gather resin trap areas together when computing for other issues to spare a decoding cycle latter
   * When using a threshold for islands detection it was also appling it to the overhangs
   * Fix the spare decoding conditional cycle for partial scans
   * Change resin trap search from parallel to sync to prevent fake detections and missing joints at cost of speed (#13)
* **Tools:**
   * Add layer selector: 'From first to current layer' and 'From current to last layer'
   * I printed this file: Multiplier - Number of time(s) the file has been printed. Half numbers can be used to consume from a failed print. Example: 0.5x if a print canceled at 50% progress
   * Pixel dimming: Increase wall thickness default from 5px to 10px
   * Import layers: Importing layers was not marking layers as modified, then the save file won't save the new images in, to prevent other similar bugs, all layers that got replaced will be auto marked as modified

## 05/03/2021 - v2.6.2

* (Add) Edit print paramenters: Option to enable or disable the 'Propagate modifications to layers' when working with global parameters
* (Change) PrusaSlicer printer: Auto convert format from ctb to photon for Anycubic Photon
* **(Fix) Change resolution:**
   * It was placing the object on random layers on the wrong position, shifting the object
   * Add informaton on the tool description to warn about diferent pixel pitch for target printer
   * Add current pixel pitch in microns if available
* (Fix) Exposure time finder: Multiple exposures was getting bottom and normal time from base file instead of commum properties fields

## 03/03/2021 - v2.6.1

* (Add) Setting: Auto repair layers and issues on file load
* (Improvement) When ComputeIssuesOnLoad is enabled and issues are detected it will switch the view to the Issues tab
* **(Improvement) Raft Relief:**
   * Add parameter "Mask layer index": Defines the mask layer to use and ignore the white blobs on the raft
   * Increase the automatic support detection allowance on more odd shapes
   * Prevent layer 0 to be used as a mask, if so it will quit
   * If the ignored layer number are set and equal or larger than mask layer index it will quit
   * Fix progress count when using ignored layers
   * Change supports dilate kernel from Elipse to Rectangle for a better coverage of the pad
   * Change the default values for a more conservative values
* (Fix) When both ComputeIssuesOnLoad and ComputeIssuesOnClickTab are enabled, clicking the issues tab will rescan for issues
* (Fix) Tools: Set a default profile insn't working
* (Fix) Redoing the last operation was not recovering the custom ROI on the operation

## 02/03/2021 - v2.6.0

* (Add) File format: Zcode (Uniz IBEE)
* (Add) PrusaSlicer Printer: Uniz IBEE
* (Add) Extract: Output an "Layer.ini" file containing per layer data and include the Configuration.ini for Zip file formats
* (Improvement) Zip: Increase the GCode parsing performance
* (Fix) File formats: Wasn't set bottom / light off delay per individual layer on generic formats, defaulting every layer to 0
* (Fix) Edit print parameters: When changing bottom layer count, layers didnt update thier properties

## 25/02/2021 - v2.5.1

* (Add) Calibration - Exposure time finder: Option to "Do not perform the lifting sequence for layers with same Z positioning" 
   The lift height will be set to 0 for sequential layers that share same z position.
   Some printers may not support this and always require a lift after each layer.
* (Fix) Hide MaterialMilliliters from layer data if unable to calculate the value (NaN)
* (Fix) CWS: A missing line break wasn't lifting printer on finish
* (Fix) Layers: Allow to set LiftHeight and LightOffDelay to 0 per layer

## 21/02/2021 - v2.5.0

* (Add) Help - Material manager (F10): Allow to manage material stock and costs with statistic over time
* (Add) File - I printed this file (CTRL + P): Allow to select a material and consume resin from stock and print time from the loaded file

## 19/02/2021 - v2.4.9

* **(Fix) PhotonWorkshop files: (#149)**
   * CurrencySymbol to show the correct symbol and don't convert unknown values to prevent hacking
   * Set PerLayerOverride to 1 only if any layer contains modified parameters that are not shared with the globals
* **(Fix) Clipboard:**
   * Initing the clipboard for the first time was calling Undo and reseting parameters from layers with base settings
   * Undo and redo make layer parameters to reset and recalculate position z, making invalid files when using with advanced tools
* (Fix) Tool - Edit print parameters: When editing per a layer range and reopen the tool it will show the previous set values

## 18/02/2021 - v2.4.8

* (Improvement) Cache per layer and global used material for faster calculations
* (Improvement) Better internal PrintTime management
* **(Improvement) GUI:**
   * Show per layer used material percentage compared to the rest model
   * Show total of millimeters cured per layer if available
   * Show bounds and ROI in millimeters if available
   * Show display width and height below resolution if available
   * Don't split (Actions / Refresh / Save) region when resize window and keep those fixed
* **(Improvement) Calibrate - Grayscale:**
   * Add a option to convert brightness to exposure time on divisions text
   * Adjust text position to be better centered and near from the center within divisions
* (Fix) Calculate the used material with global layer height instead of calculate height from layer difference which lead to wrong values in parallel computation
* (Fix) Converting files were not setting the new file as parent for the layer manager, this affected auto convertions from SL1 and lead to crashes and bad calculations if file were not reloaded from the disk (#150, #151)
* (Fix) PositionZ rounding error when removing layers

## 17/02/2021 - v2.4.7

* (Add) Computed used material milliliters for each layer, it will dynamic change if pixels are added or subtracted
* (Add) Computed used material milliliters for whole model, it will dynamic change if pixels are added or subtracted
* (Improvement) Round cost, material ml and grams from 2 to 3 decimals
* (Improvement) Operation profiles: Allow to save and get a custom layer range instead of pre-defined ranges
* **(Improvement)** PhotonWorkshop files: (#149)
   * Fill in the display width, height and MaxZ values for the printers
   * Fill in the xy pixel size values for the printers
   * Change ResinType to PriceCurrencyDec and Add PriceCurrencySymbol
   * Change Offset1 on header to PrintTime
   * Change Offset1 on layer table as NonZeroPixelCount, the number of white pixels on the layer 
   * Fix LayerPositionZ to calculate the correct value based on each layer height and fix internal layer layer height which was been set to position z
   * Force PerLayerOverride to be always 1 after save the file
* (Fix) Actions - Remove and clone layers was selecting all layer range instead of the current layer
* (Fix) Redo last action was not getting back the layer range on some cases

## 15/02/2021 - v2.4.6

* **(Improvement) Calibration - Elephant Foot:** (#145)
   * Remove text from bottom layers to prevent islands from not adhering to plate 
   * Add a option to extrude text up to a height
* **(Improvement) Calibration - Exposure time finder:** (#144)
   * Increase the left and right margin to 10mm
   * Allow to iterate over pixel brightness and generate dimmed objects to test multiple times at once
* **(Fix) File format PWS:**
   * Some files would produce black layers if pixels are not full whites, Antialiasing level was not inherit from source
   * Antialiasing level was forced 1 and not read the value from file properties
   * Antialiasing threshold pixel math was producing the wrong pixel value
* **(Fix) Raw images (jpg, png, etc):** (#146)
   * Set layer height to be 0.01mm by default to allow the use of some tools
   * When add layers by clone or other tool it don't update layers height, positions, indexes, leading to crashes
* **(Fix) Actions - Import Layers:** (#146, #147)
   * ROI calculation error leading to not process images that can potential fit inside the volumes
   * Out-of-bounds calculation for Stack type
   * Replace type was calculating out-of-bounds calculation like Stack type when is not required to and can lead to skip images
   * Better image ROI colection for Insert and Replace types instead of capture the center most 
* (Fix) Settings window: Force a redraw on open to fix auto sizes

## 12/02/2021 - v2.4.5

* (Add) Setting: Expand and show tool descriptions by default
* (Improvement) Drag and drop a file on Main Window while hold SHIFT key will open the file under a new instance
* (Improvement) PrusaSlicer & SL1 files: Allow to set custom variables on "Material - Notes" per resin to override the "Printer - Notes" variables
    This will allow custom settings per resin, for example, if you want a higher 'lift height, lift speed, etc' on more viscous resins. (#141)
* (Change) Setting: Windows vertical margin to 60px
* (Fix) Export file was getting a "Parameter count mismatch" on some file formats (#140)
* (Fix) photon and cbddlp file formats with version 3 to never hash images
* (Fix) Windows was not geting the screen bounds from the active monitor
* (Fix) Tool windows height, vertical margin and position
* **(Fix) Exposure time finder:**
  * Text label
  * Set vertical splitter to not show decimals, int value 
  * Set vertical splitter default to 0
  * Allow vertical splitter to accept negative values
  * Optimized the default values
  * Removed similar letters from text
  * Add some symbols to text to validate overexposure
  * Decrease Features height minimum value to 0.5mm

## 09/02/2021 - v2.4.4

* (Improvement) Exposure time finder: Invert circles loop into quadrants

## 08/02/2021 - v2.4.3

* **(Add) Exposure time finder:**
  * Configurable zebra bars
  * Configurable text
  * Tune defaults values to fill the space
  * Add incremental loop circles to fill space on exposure text space
* (Change) Default vertical windows margin from 250 to 400px

## 08/02/2021 - v2.4.2

* **(Improvement) Exposure time finder:**
  * Bring back shapes
  * Diameters now represent square pixels, eg: 3 = 3x3 pixels = 9 pixels total
  * Optimized default diameters

## 07/02/2021 - v2.4.1

* **(Fix) Exposure time finder:**
  * Use a spiral square instead of configurable shapes to match the exact precise set pixels
  * Set pixels as default values
  * Optimized the pixel values to always produce a closed shape
  * Rename cylinder to hole
  * Crash when diameters are empty
  * Profiles was not saving


## 06/02/2021 - v2.4.0

* (Upgrade) EmguCV/OpenCV to v4.5.1
* (Upgrade) AvaloniaUI to 1.0
* (Improvement) GUI re-touched
* (Improvement) Make pixel editor tab to disappear when pixel editor is disabled
* (Improvement) Simplify the output filename from PrusaSlicer profiles
* (Improvement) All operations require a slicer file at constructor rather than on execute, this allow exposure the open file to the operation before run it
* (Improvement) Calibrations: Auto set "Mirror Output" if open file have MirrorDisplay set
* (Change) Tool - Redraw model/supports icon
* (Change) photon and cbddlp to use version 3 by default
* (Add) Tool - Dynamic layer height: Analyze and optimize the model with dynamic layer heights, larger angles will slice at lower layer height
        while more straight angles will slice larger layer height. (#131)
* (Add) Calibration - Exposure time finder: Generates test models with various strategies and increments to verify the best exposure time for a given layer height
* (Add) File load checks, trigger error when a file have critical errors and attempt to fix non-critical errors
  * Layers must have an valid image, otherwise trigger an error
  * Layers must have a incremental or equal position Z than it previous, otherwise trigger an error
  * If layer 0 starts at 0mm it will auto fix all layers, it will add Layer Height to the current z at every layer
* (Add) Tool - Edit print parameters: Allow set parameters to each x layers and skip n layers inside the given range.
        This allow the use of optimizations in a layer pattern, for example, to set 3s for a layer but 2.5s for the next.
* (Add) Layer height property to "Layer Data" table: Shows layer height for the slice
* (Fix) When automations applied and file is saved, it will not warn user about file overwrite for the first time save
* (Fix) Tool - Redraw model/supports: Disable apply button when no file selected
* (Fix) Tool - Infill: Lack of equality member to test if same infill profile already exists
* (Fix) Auto converted files from SL1 where clipping filename at first dot (.), now it only strips known extensions
* (Fix) SL1 encoded files wasn't generating the right information and lead to printer crash
* (Fix) PrusaSlicer printer "Anycubic Photon S" LiftSpeed was missing and contains a typo (#135)
* (Fix) PrusaSlicer profile manager wasnt marking missing profiles to be installed (#135)
* (Fix) PrusaSlicer folder search on linux to also look at %HOME%/.config/PrusaSlicer (#135, #136)
* (Fix) Operations were revised and some bug fixed, most about can't cancel the progress
* (Fix) Some typos on tooltips
* (Fix) Prevent PhotonS from enconding, it will trigger error now as this format is read-only
* **(Fix) Ctrl + Shift + Z to redo the last operation:**
  * The layer range is reseted instead of pull the used values
  * Tool - Arithmetic always disabled
  * Action - Layer import didn't generate info and always disabled

## 22/01/2021 - v2.3.2

* (Add) Settings - Automations: Change only light-off delay if value is zero (Enabled by default)
* (Fix) Calibrators: Some file formats will crash when calibration test output more layers than the dummy file
* (Fix) Undo/redo don't unlock the save function

## 19/01/2021 - v2.3.1

* (Add) Calibrator - Stress Tower: Generates a stress tower to test the printer capabilities
* (Add) PrusaSlicer printer: UVtools Prusa SL1, for SL1 owners must use this profile to be UVtools compatible when using advanced tools
* (Fix) Tool - Calculator - Optimal model tilt: Layer height wasn't get pulled from loaded file and fixed to 0.05mm
* **(Fix) FileFormats:**
  * When change a global print paramenter, it will only set this same parameter on every layer, keeping other parameters intact, it was reseting every parameter per layer to globals
  * SL1: Verify if files are malformed and if there's missing configuration files (#126)
  * CTBv3: Set EncryptionMode to 0x2000000F, this allow the use of per layer settings

## 13/01/2021 - v2.3.0

* **PrusaSlicer:**
   * **In this release is recommended to discard your printer and refresh it with uvtools updated printer or replace notes over**
   * (Add) FILEFORMAT_XXX variable to auto-convert to that file format once open in UVtools
   * (Update) Print profiles fields with new PrusaSlicer version
   * (Remove) LayerOffDelay from printer notes and use only the LightOffDelay variable instead, both were being used, to avoid redundacy LayerOffDelay was dropped. Please update your printer accordingly!
   * (Remove) FLIP_XY compability from printers
   * (Remove) AntiAlias variable from printers
* **(Add) Settings - Automations:**
   * Auto save the file after apply any automation(s)
   * Auto convert SL1 files to the target format when possible and load it back
   * Auto set the extra 'light-off delay' based on lift height and speed.
* **FileFormats:**
    * (Add) Allow all and future formats to convert between them without knowing each other (Abstraction)
    * (Add) MirrorDisplay property: If images need to be mirrored on lcd to print on the correct orientation (If available)
    * (Add) MaxPrintHeight property: The maximum Z build volume of the printer (If available)
    * (Add) XYResolution and XYResolutionUm property
    * (Change) Round all setters floats to 2 decimals
    * (Change) LightOffTime variables to LayerOffDelay
    * (Fix) Files with upper case extensions doesn't load in
* (Add) Calculator - Optimal model tilt: Calculates the optimal model tilt angle for printing and to minimize the visual layer effect
* (Add) Bottom layer count to the status bar
* (Add) ZCodex: Print paramenter light-off delay"
* (Change) Island Repair: "Remove Islands Below Equal Pixels" limit from 255 to 65535 (#124)
* **(Fix) SL1:**
    * Prevent error when bottle volume is 0
    * bool values were incorrectly parsed
    * Implement missing keys: host_type, physical_printer_settings_id and support_small_pillar_diameter_percent
* **(Fix) ZIP:**
    * Material volume was set to grams
    * Bed Y was not being set

## 10/01/2021 - v2.2.0

* (Add) FDG file format for Voxelab Printers (ezrec/uv3dp#129)
* (Add) PrusaSlicer printer: Voxelab Ceres 8.9
* (Change) Print time display to hours and minutes: 00h00m

## 07/01/2021 - v2.1.3

* (Add) PrusaSlicer printers:
   * Peopoly Phenom XXL
   * QIDI 3D ibox mono
   * Wanhao CGR Mini Mono
   * Wanhao CGR Mono
* (Add) PrusaSlicer light supports profiles
* (Add) Calibration - Elephant Foot: Mirror output
* (Add) Calibration - XYZ Accuracy: Mirror output
* (Add) Calibration - Tolerance: Mirror output
* (Add) Calibration - Grayscale: Mirror output
* (Add) Scripts on github
* (Change) Save 'Display Width' and 'Height' to calibration profiles and load them back only if file format aware from these properties
* (Fix) Tool - Morph: Set a rectangular 3x3 kernel by default
* (Fix) Tool - Blur: Set a rectangular 3x3 kernel by default
* (Fix) Calibration - Elephant Foot: Include part scale on profile text
* (Fix) MSI dont store instalation path (#121)

## 03/01/2021 - v2.1.2

* (Add) Pixel editor - Text: Preview of text operation (#120)
* (Add) Calibration - Elephant Foot: 'Part scale' factor to scale up test parts
* (Change) Allow tools text descriptions to be selectable and copied
* (Fix) Pixel editor - Text: Round font scale to avoid precision error

## 03/01/2021 - v2.1.1

* (Add) About box: Primary screen identifier and open on screen identifier
* (Add) Calibrator - External tests
* (Change) Rewrite 'Action - Import Layer(s)' to support file formats and add the followig importation types:
  * **Insert:** Insert layers. (Requires images with bounds equal or less than file resolution)
  * **Replace:** Replace layers. (Requires images with bounds equal or less than file resolution)
  * **Stack:** Stack layers content. (Requires images with bounds equal or less than file resolution)
  * **Merge:** Merge/Sum layers content. (Requires images with same resolution)
  * **Subtract:** Subtract layers content. (Requires images with same resolution)
  * **BitwiseAnd:** Perform a 'bitwise and' operation over layer pixels. (Requires images with same resolution)
  * **BitwiseOr:** Perform a 'bitwise or' operation over layer pixels. (Requires images with same resolution)
  * **BitwiseXOr:** Perform a 'bitwise xor' operation over layer pixels. (Requires images with same resolution)
* (Change) Icon for Tool - Raft Relief
* (Change) Windows and dialogs max size are now calculated to where window is opened instead of use the primary or first screen all the time

## 29/12/2020 - v2.1.0

* (Add) Tool - Redraw model/supports: Redraw the model or supports with a set brightness. This requires an extra sliced file from same object but without any supports and raft, straight to the build plate.
* (Add) Tool - Raft Relief:
    * Allow ignore a number of layer(s) to start only after that number, detault is 0
    * Allow set a pixel brightness for the operation, detault is 0
    * New "dimming" type, works like relief but instead of drill raft it set to a brightness level
* (Add) Arch-x64 package (#104)
* (Fix) A OS dependent startup crash when there's no primary screen set (#115)
* (Fix) Tool - Re height: Able to cancel the job
* (Fix) Unable to save "Calibration - Tolerance" profiles
* (Change) Core: Move all operations code from LayerManager and Layer to it own Operation* class within a Execute method (Abstraction)
* (Change) sh UVtools.sh to run independent UVtools instance first, if not found it will fallback to dotnet UVtools.dll
* (Change) Compile and zip project with WSL to keep the +x (execute) attribute for linux and unix systems
* (Change) MacOS builds are now packed as an application bundle (Auto-updater disabled for now)
* (Remove) Universal package from builds/releases

## 25/12/2020 - v2.0.0

This release bump the major version due the introduction of .NET 5.0, the discontinuation old UVtools GUI project and the new calibration wizards.
* (Upgrade) From .NET Core 3.1 to .NET 5.0
* (Upgrade) From C# 8.0 to C# 9.0
* (Upgrade) From Avalonia preview6 to rc1
    * Bug: The per layer data gets hidden and not auto height on this rc1
* (Add) Setting - General - Windows / dialogs: 
  * **Take into account the screen scale factor to limit the dialogs windows maximum size**: Due wrong information UVtools can clamp the windows maximum size when you have plenty more avaliable or when use in a secondary monitor. If is the case disable this option
  * **Horizontal limiting margin:** Limits windows and dialogs maximum width to the screen resolution less this margin
  * **Vertical limiting margin:** Limits windows and dialogs maximum height to the screen resolution less this margin
* (Add) Ctrl + Shift + Z to undo and edit the last operation (If contain a valid operation)
* (Add) Allow to deselect the current selected profile
* (Add) Allow to set a default profile to load in when open a tool
* (Add) ENTER and ESC hotkeys to message box
* (Add) Pixel dimming: Brightness percent equivalent value
* (Add) Raft relief: Allow to define supports margin independent from wall margin for the "Relief" type
* (Add) Pixel editor: Allow to adjust the remove and add pixel brightness values
* (Add) Calibration Menu:
    * **Elephant foot:** Generates test models with various strategies and increments to verify the best method/values to remove the elephant foot.
    * **XYZ Accuracy:** Generates test models with various strategies and increments to verify the XYZ accuracy.
    * **Tolerance:** Generates test models with various strategies and increments to verify the part tolerances.
    * **Grayscale:** Generates test models with various strategies and increments to verify the LED power against the grayscale levels.
* (Change) PW0, PWS, PWMX, PWMO, PWMS, PWX file formats to ignore preview validation and allow variations on the file format (#111)
* (Change) Tool - Edit print parameters: Increments from 0.01 to 0.5
* (Change) Tool - Resize: Increments from 0.01 to 0.1
* (Change) Tool - Rotate: Increments from 0.01 to 1
* (Change) Tool - Calculator: Increments from 0.01 to 0.5 and 1
* (Fix) PW0, PWS, PWMX, PWMO, PWMS, PWX file formats to replicate missing bottom properties cloned from normal properties
* (Fix) Drain holes to build plate were considered as traps, changed to be drains as when removing object resin will flow outwards
* (Fix) When unable to save the file it will change extension and not delete the temporary file
* (Fix) Pixel dimming wasn't saving all the fields on profiles
* (Fix) Prevent a rare startup crash when using demo file
* (Fix) Tool - Solifiy: Increase AA clean up threshold range, previous value wasn't solidifing when model has darker tones
* (Fix) Sanitize per layer settings, due some slicers are setting 0 at some properties that can cause problems with UVtools calculations, those values are now sanitized and set to the general value if 0
* (Fix) Update partial islands:
    * Was leaving visible issues when the result returns an empty list of new issues
    * Was jumping some modified sequential layers
    * Was not updating the issue tracker map
* (Fix) Edit print parameters was not updating the layer data table information

## 08/11/2020 - v1.4.0

* (Add) Tool - Raft relief: Relief raft by adding holes in between to reduce FEP suction, save resin and easier to remove the prints.

## 04/11/2020 - v1.3.5

* (Add) Pixel Dimming: Chamfer - Allow the number of walls pixels to be gradually varied as the operation progresses from the starting layer to the ending layer (#106)
* (Add) PrusaSlicer print profiles: 0.01, 0.02, 0.03, 0.04, 0.15, 0.2
* (Change) Morph: "Fade" to "Chamfer" naming, created profiles need redo
* (Change) Pixel Dimming: Allow start with 0px walls when using "Walls Only"
* (Change) PrusaSlicer print profiles names, reduced bottom layers and raft height
* (Remove) PrusaSlicer print profiles with 3 digit z precision (0.025 and 0.035)
* (Fix) PW0, PWS, PWMX, PWMO, PWMS, PWX file formats, where 4 offsets (16 bytes) were missing on preview image, leading to wrong table size. Previous converted files with UVtools wont open from now on, you need to reconvert them. (ezrec/uv3dp#124)
* (Fix) Unable to run Re-Height tool due a rounding problem on some cases (#101)
* (Fix) Layer preview end with exception when no per layer settings are available (SL1 case)

## 26/11/2020 - v1.3.4

* (Add) Infill: CubicDynamicLink - Alternates centers with lateral links, consume same resin as center linked and make model/infill stronger.
* (Add) Update estimate print time when modify dependent parameters (#103)
* (Add) Tool - Calculator: Old and new print time estimation (#103)
* (Fix) Print time calculation was using normal layers with bottom layer off time
* (Fix) Calculate print time based on each layer setting instead of global settings

## 25/11/2020 - v1.3.3

* (Add) Improved island detection: Combines the island and overhang detections for a better more realistic detection and to discard false-positives. (Slower)
   If enabled, and when a island is found, it will check for overhangs on that same island, if no overhang found then the island will be discarded and considered safe, otherwise it will flag as an island issue.
   Note: Overhangs settings will be used to configure the detection. Enabling Overhangs is not required for this procedure to work.
   Enabled by default.
* (Add) More information on the About box: Operative system and architecture, framework, processor count and screens
* (Fix) Overhangs: Include islands when detecting overhangs were not skip when found a island
* (Fix) Decode CWS from Wanhao Workshop fails on number of slices (#102)

## 19/11/2020 - v1.3.2

* (Add) Tools: Warn where layer preview is critical for use the tool, must disable layer rotation first (#100)
* (Add) CWS: Bottom lift speed property
* (Add) CWS: Support Wanhao Workshop CWX and Wanhao Creation Workshop file types (#98)
* (Add) CWS: Split format into virtual extensions (.cws, .rgb.cws, .xml.cws) to support diferent file formats and diferent printers under same main .cws extensions. That will affect file converts only to let UVtools know what type of encoding to use. Load and save a xxx.cws file will always auto decode/encode the file for the correct target format no matter the extension.
* (Improvement) CWS: It no longer search for a specific filename in the zip file, instead it look for extension to get the files to ensure it always found them no matter the file name system
* (Fix) CWS: When "Save as" the file were generating sub files with .cws extension, eg: filename0001.cws.png
* (Change) Allow read empty layers without error from Anycubic files (PWS, PW0, PWxx) due a bug on slicer software under macOS

## 16/11/2020 - v1.3.1

* (Add) File format: PWX (AnyCubic Photon X) (#93)
* (Add) File format: PWMO (AnyCubic Photon Mono) (#93)
* (Add) File format: PWMS (AnyCubic Photon Mono SE) (#93)
* (Add) PrusaSlicer printer: AnyCubic Photon X
* (Add) PrusaSlicer printer: AnyCubic Photon Mono
* (Add) PrusaSlicer printer: AnyCubic Photon Mono SE
* (Add) PrusaSlicer printer: AnyCubic Photon Mono X
* (Change) "Save as" file filter dialog with better file extension description
* (Fix) Tool - Infill: Allow save profiles
* (Fix) Material cost was showing as ml instead of currency

## 14/11/2020 - v1.3.0

* (Add) Changelog description to the new version update dialog
* (Add) Tool - Infill: Proper configurable infills
* (Add) Pixel area as "px²" to the layer bounds and ROI at layer bottom information bar
* (Add) Pixel dimming: Alternate pattern every x layers
* (Add) Pixel dimming: Lattice infill
* (Add) Solidify: Required minimum/maximum area to solidify found areas (Default values will produce the old behaviour)
* (Add) Issues: Allow to hide and ignore selected issues
* (Add) Issue - Touch boundary: Allow to configure Left, Top, Right, Bottom margins in pixels, defaults to 5px (#94)
* (Add) UVJ: Allow convert to another formats (#96)
* (Add) Setters to some internal Core properties for more abstraction
* (Improvement) Issue - Touch boundary: Only check boundary pixels if layer bounds overlap the set margins, otherwise, it will not waste cycles on check individual rows of pixels when not need to
* (Change) Place .ctb extension show first than .cbddlp due more popular this days
* (Change) Pixel dimming: Text "Borders" to "Walls"
* (Change) Issues: Remove "Remove" text from button, keep only the icon to free up space
* (Change) Ungroup extensions on "convert to" menu (#97)
* (Fix) Issues: Detect button has a incorrect "save" icon
* (Fix) SL1: Increase NumSlow property limit
* (Fix) UVJ: not decoding nor showing preview images
* (Fix) "Convert to" menu shows same options than previous loaded file when current file dont support convertions (#96)
* (Fix) Hides "Convert to" menu when unable to convert to another format (#96)
* (Fix) Program crash when demo file is disabled and tries to load a file in
* (Fix) Rare crash on startup when mouse dont move in startup period and user types a key in meanwhile
* (Fix) On a slow startup on progress window it will show "Decoded layers" as default text, changed to "Initializing"

## 08/11/2020 - v1.2.1

* (Add) IsModified property to current layer information, indicates if layer have unsaved changes
* (Add) Splitter between preview image and properties to resize the vertical space between that two controls
* (Fix) Unable to save file with made modifications, layer IsModified property were lost when entering on clipboard
* (Fix) After made a modification clipboard tries to restores that same modification (Redundant)
* (Fix) Current layer data doesn't refresh when refreshing current layer, made changes not to show in
* (Fix) Hides not supported properties from current layer data given the file format

## 07/11/2020 - v1.2.0

* (Add) RAM usage on title bar
* (Add) Clipboard manager: Undo (Ctrl + Z) and Redo (Ctrl + Y) modifications (Memory optimized)
* (Add) Current layer properties on information tab
* (Fix) Long windows with system zoom bigger than 100% were being hidden and overflow (#90)
* (Fix) Do not recompute issues nor properties nor reshow layer if operation is cancelled or failed

## 05/11/2020 - v1.1.3

* (Add) Auto-updater: When a new version is detected UVtools still show the same green button at top, 
on click, it will prompt for auto or manual update.
On Linux and Mac the script will kill all UVtools instances and auto-upgrade.
On Windows the user must close all instances and continue with the shown MSI installation
* (Add) Tool profiles: Create and remove named presets for some tools
* (Add) Event handler for handling non-UI thread exceptions
* (Fix) Mac: File - Open in a new window was not working
* (Fix) Tool - Rotate: Allow negative angles
* (Fix) Tool - Rotate: The operation was inverting the angle
* (Fix) Tools: Select normal layers can crash the program with small files with low layer count, eg: 3 layers total

## 02/11/2020 - v1.1.2

* (Add) Program start elapsed seconds on Log
* (Add) Lift heights @ speeds, retract speed, light-off information to status bar
* (Fix) Per layer settings are being lost when doing operations via tools that changes the layer count
* (Fix) Current layer height mm was being calculated instead of showing the stored position Z value (For hacked files)
* (Fix) Zip: By using hacked gcodes were possible to do a lift sequence without returning back to Z layer position
* (Fix) ZCodex: Read per layer lift height/speed, retract speed and pwm from GCode
* (Fix) Status bar, layer top and bottom bar: Break content down for the next line if window size overlaps the controls
* (Fix) Status bar: Make right buttons same height as left buttons
* (Improvement) CWS: Better gcode parser for decoding
* (Change) GCodes: Cure commands (Light-on/Cure time/Light-off) are only exposed when exposure time and pwm are present and greater than 0 [Safe guard]
* (Change) Zip: If only one G0 command found per layer, it will be associated to the cure z position (No lift height)
* (Change) Merged bottom/normal exposure times on status bar
* (Change) Tabs: Change controls spacing from 5 to 2 for better looking
* (Change) Deploy UVtools self-contained per platform specific: (#89)
  * Platform optimized 
  * Reduced the package size
  * Includes .NET Core assemblies and dont require the installation of .NET Core
  * Can execute UVtools by double click on "UVtools" file or via "./UVtools" on terminal
  * **Naming:** UVtools_[os]-[architecture]_v[version].zip
  * **"universal"** zip file that includes the portable version, os and architecture independent but requires dotnet to run, these build were used in all previous versions

## 01/11/2020 - v1.1.1

* (Fix) PHZ, PWS, LGS, SL1 and ZCodex per layer settings and implement missing properties on decode
* (Fix) LGS and PHZ Zip wasn't setting the position z per layer
* (Fix) Add missing ctb v3 per layer settings on edit parameters window
* (Fix) PWS per layer settings internal LiftSpeed was calculating in mm/min, changed to mm/sec

## 01/11/2020 - v1.1.0

* (Add) photons file format (Read-only)
* (Add) Allow mouse scroll wheel on layer slider and issue tracker to change layers (#81)
* (Add) Menu - Help - Open settings folder: To open user settings folder
* (Add) When a file doesn't have a print time field or it's 0, UVtools calculate the approximate time based on parameters
* (Add) Per layer settings override on UVtools layer core
* (Add) Tool - Edit print parameters: Allow change per layer settings on a layer range
* (Add) Tool Window - Layer range synchronization and lock for single layer navigation (Checkbox)
* (Add) Tool Window - Change the start layer index on range will also change the layer image on background
* (Improvement) Adapt every file format to accept per layer settings where possible
* (Improvement) Better gcode checks and per layer settings parses
* (Change) When converting to CTB, version 3 of the file will be used instead of version 2
* (Change) When converting to photon or cbddlp, version 2 of the file will be used
* (Change) New logo, thanks to (Vinicius Silva @photonsters)
* (Fix) MSI installer was creating multiple entries/uninstallers on windows Apps and Features (#79)
* (Fix) Release builder script (CreateRelease.WPF.ps1): Replace backslash with shash for zip releases (#82)
* (Fix) CWS file reader when come from Chitubox (#84)
* (Fix) CWS was introducing a big delay after each layer, LiftHeight was being used 2 times instead of LiftSpeed (#85)
* (Fix) CWS fix Build Direction property name, was lacking a whitespace
* (Fix) Layer bounds was being show for empty layers on 0x0 position with 1px wide
* (Fix) Empty layers caused miscalculation of print volume bounds
* (Fix) Recalculate GCode didn't unlock save button
* (Fix) Tool - Calculator - Light-Off Delay: Wasn't calculating bottom layers
* (Change) Drop a digit from program version for simplicity, now: MAJOR.MINOR.PATCH 
  * **Major:** new UI, lots of new features, conceptual change, incompatible API changes, etc.
  * **Minor:** add functionality in a backwards-compatible manner
  * **Patch:** backwards-compatible bug fixes
* (Upgrade) Avalonia framework to preview6

## 23/10/2020 - v1.0.0.2

* (Fix) ROI selection button on bottom was always disabled even when a region is selected
* (Fix) Settings - Issues- "Pixel intensity threshold" defaults to 0, but can't be set back to 0 after change (minimum is 1). (#78)
* (Fix) Settings - Issues - "Supporting safe pixels..." is present twice (#78)
* (Fix) Settings - Layer repair - Empty layers / Resin traps texts are swapped in the settings window (#78)

## 23/10/2020 - v1.0.0.1

* (Change) Checked and click buttons highlight color for better distinguish
* (Fix) Move user settings to LocalUser folder to allow save without run as admin
* (Fix) Save button for print parameters were invisible

## 22/10/2020 - v1.0.0.0

* (Add) Multi-OS with Linux and MacOS support
* (Add) Themes support
* (Add) Fullscreen support (F11)
* (Change) GUI was rewritten from Windows Forms to WPF Avalonia, C#
* (Improvement) GUI is now scalable
* (Fix) Some bug found and fixed during convertion

## 14/10/2020 - v0.8.6.0

* (Change) Island detection system:
  * **Before**: A island is consider safe by just have a static amount of pixels, this mean it's possible to have a mass with 100000px supported by only 10px (If safe pixels are configured to this value), so there's no relation with island size and it supporting size. This leads to a big problem and not detecting some potential/unsafe islands.
  * **Now:** Instead of a static number of safe pixels, now there's a multiplier value, which will multiply the island total pixels per the multiplier, the supporting pixels count must be higher than the result of the multiplication.
    *  **Formula:** Supporting pixels >= Island pixels * multiplier
    *  **Example:** Multiplier of 0.25, an island with 1000px * 0.25 = 250px, so this island will not be considered if below exists at least 250px to support it, otherwise will be flagged as an island.
    *  **Notes:** This is a much more fair system but still not optimal, bridges and big planes with micro supports can trigger false islands. While this is a improvement over old system it's not perfect and you probably will have islands which you must ignore. Renember that you not have to clear out the issue list! Simply step over and ignore the issues you think are false-positives.

## 14/10/2020 - v0.8.5.0

* (Add) Tool - Calculator: Convert millimeters to pixels
* (Add) Tool - Calculator: Find the optimal "Ligth-Off Delay"
* (Add) Internal abstraction of display size to all file formats
* (Add) Default demo file that loads on startup when no file is specified (this can be disable/enabled on settings)

## 13/10/2020 - v0.8.4.3

* (Add) Tool - Layer repair: Allow remove islands recursively (#74)
* (Fix) Pixel Editor: Cursor dimentions when using brushes with thickness > 1 (#73)

## 10/10/2020 - v0.8.4.2

* (Fix) pws and pw0: Error when try to save or copy to clipboard the slicer information / properties
* (Fix) photon, ctb, cbbdlp, phz, pws, pw0: Rare cases were decoding image generate noise and malformed image
* (Fix) Rare cases where manipulation of images generate areas with noise

## 10/10/2020 - v0.8.4.1

* (Add) Tool - Modify print parameters: Value unit to confirmation text
* (Change) Tool - Modify print parameters: Maximum allowed exposure times from 255s to 1000s (#69)
* (Change) On operations, instead of partial backup a whole backup is made, this allow cancel operations which changes layer count and other structure changes
* (Improvement) PrusaSlicer profile manager: Files are now checked against checksum instead write time to prevent trigger an false update when using MSI installer
* (Fix) Tool - Layer Import: Allow cancel operation
* (Fix) Tool - Layer Import: When importing layers that increase the total layer count of the file program stays forever on progress
* (Fix) Tool - Layer Clone: Layer information was the same as heights, fixed to show the result of operation in layers
* (Fix) Tool - Pattern: Unable to use an anchor

## 01/10/2020 - v0.8.4.0

* (Add) Tool: Arithmetic operations
* (Add) Allow convert chitubox zip to cbddlp, ctb, photon, phz, pws, pw0, cws, zcodex
* (Add) When using filenames containing "bene4_mono" and when converting to cws it will use the GRAY2RGB encoding (#67)
* (Add) Hint on how to use layer re-height tool when it fails to launch
* (Add) PrusaSlicer Printer: Creality LD-006
* (Add) PrusaSlicer Printer: EPAX E6 Mono
* (Add) PrusaSlicer Printer: EPAX E10 Mono
* (Add) PrusaSlicer Printer: EPAX X1K 2K Mono
* (Add) PrusaSlicer Printer: Elegoo Mars C
* (Add) PrusaSlicer Printer: Longer 3D Orange4K
* (Add) PrusaSlicer Printer: Phrozen Shuffle XL Lite
* (Add) PrusaSlicer Printer: Phrozen Shuffle 16
* (Add) PrusaSlicer Printer: Phrozen Sonic 4K
* (Add) PrusaSlicer Printer: Phrozen Sonic Mighty 4K
* (Add) PrusaSlicer Printer: Voxelab Proxima
* (Add) PrusaSlicer Printer: QIDI S-Box
* (Fix) PrusaSlicer Printer: Elegoo Saturn - name and resolution
* (Fix) PrusaSlicer Printer: AnyCubic Photon S - display width/height
* (Fix) PrusaSlicer Printer: Epax X10 4K Mono - Y Resolution
* (Fix) PrusaSlicer Printer: EPAX X133 4K Mono - display width/height
* (Fix) PrusaSlicer Printer: Phrozen Shuffle Lite - display width/height
* (Fix) All PrusaSlicer Printers were reviewed and some bugs were fixed
* (Fix) Chitubox 3D preview when use files converted with UVtools (#68)
* (Fix) Overhangs: False-positives when previous or current layer has draker pixels, it now threshold pixels before proceed (#64)
* (Change) Tools: Placed "Threshold" menu item after "Morph"

## 30/09/2020 - v0.8.3.0

* (Add) Issue: Overhangs - Detects potential overhangs on layers (#64)
* (Add) PrusaSlicer Printer: Phrozen Sonic Mini 4K
* (Improvement) CWS: Allow read files with "slice*" filenames as content (#67)
* (Improvement) Allow convert chitubox files to CWS Bene4 Mono printer, must configure a printer containing "Bene4 Mono" name on Chitubox (#67)
* (Improvement) Edit print parameters: Show changes on confirm dialog
* (Improvement) Edit print parameters: Dedicated reset button hides when value is unchanged
* (Improvement) More detailed descriptions on error messages
* (Fix) Some islands wont remove from list when many selected and click remove
* (Fix) Extract: Use trail zeros to layer filenames
* (Fix) MSI installer not creating shortcuts (#66)

## 22/09/2020 - v0.8.2.4

* (Add) Layer Importer: Option to merge images
* (Improvement) Layer difference computation time, faster render

## 19/09/2020 - v0.8.2.3

* (Add) Tooltip for next and previous layer buttons with associated shortcut (#61)
* (Add) Pixel Editor: Erase drawing edits while hold Control (#63)
* (Add) Pixel Editor: When using diameters larger than 1px and when possible the cursor will show the associated drawing preview (#63)
* (Fix) Pixel Editor: Area px<sup>2</sup> to Diameter px (#63)
* (Fix) LGS: Some plugins and slicers use XY resolution information, while others are swapped, a auto swap will be performed when required (#59)
* (Fix) Global hotkeys prevent user from typing that key on controls (#62)

## 16/09/2020 - v0.8.2.2

* (Add) Support for PHZ zip files when renamed to .zip
* (Fix) Tools - Move and Pattern: When not selecting a ROI will draw black layers
* (Fix) Tool - Move: When making a cut move and move to a overlap zone it will blackout the source rectangle
* (Fix) ZIP: Allow to cancel on gather layers stage
* (Fix) ZIP: Thumbnails not showing nor saving

## 14/09/2020 - v0.8.2.1

* (Improvement) When unable to convert a format from SL1 to other, advice users to check used printer on PrusaSlicer (#60)
* (Improvement) Information on "Install profiles on PrusaSlicer" (#60)
* (Fix) LGS: Change resolution tool was defining wrong Y
* (Fix) ctb and pws: Renders a bad file after save, this was introduced with cancelled saves feature
* (Fix) When cancel a file convertion, it now deletes the target file

## 13/09/2020 - v0.8.2.0

* (Add) Layer status bar: Button with ROI - Click to zoom in region | Click + shift to clear roi
* (Add) Setting: Allow the layer overlay tooltips for select issues, ROI, and edit pixel mode to be hidden (#51)
* (Add) Setting: Allow change layer tooltip overlay color and opacity
* (Add) Global print properties on formats for more internal abstraction
* (Improvement) Print properties performance internal code with abstraction
* (Change) Layer status bar: Bounds text to button - Click to zoom in region
* (Change) Layer status bar: Pixel picker text to button - Click to center in point
* (Change) Layer status bar: Resolution text to button - Click to zoom to fit
* (Change) Customized cursor for Pixel Edit mode (#51)
* (Change) Layer overlay tooltip is now semi-transparent
* (Change) File - Save As is always available (#56)
* (Fix) File - Save when cancelled no longer keep a invalid file, old restored (#54)
* (Fix) File - Save As when cancelled no longer keep a invalid file, that will be deleted (#54, #55)
* (Fix) When a operation is cancelled affected layers will revert to the original form (#57)
* (Fix) Misc. text cleanup (#52, #53, #58)

## 12/09/2020 - v0.8.1.0

* (Add) Tools can now run inside a ROI (#49)
* (Add) Layer preview: Hold-Shift + Left-drag to select an ROI (Region of interest) on image, that region will be used instead of whole image when running some tools
* (Add) Layer preview: Hold-Shift + Hold-Alt + Left-drag to select and auto adjust the ROI to the contained objects, that region will be used instead of whole image when running some tools
* (Add) Layer preview: Hold-Shift + Right-click on a object to select its bounding area, that region will be used instead of whole image when running some tools
* (Add) Layer preview: ESC key to clear ROI
* (Add) Layer preview: Overlay text with hints for current action
* (Add) Tool - Move: Now possible to do a copy move instead of a cut move
* (Add) Arrow wait cursor to progress loadings
* (Change) Layer preview: Hold-Shift key to select issues and pick pixel position/brightness changed to Hold-Control key
* (Change) Layer preview: Shift+click combination to zoom-in changed to Alt+click
* (Fix) CTB v3: Bad file when re-encoding

## 11/09/2020 - v0.8.0.0

* (Add) LGS and LGS30 file format for Longer Orange 10 and 30 (ezrec/uv3dp#105)
* (Add) CWS: Support the GRAY2RGB and RBG2GRAY encoding for Bene Mono
* (Add) PrusaSlicer Printer: Nova Bene4 Mono
* (Add) PrusaSlicer Printer: Longer Orange 10
* (Add) PrusaSlicer Printer: Longer Orange 30
* (Add) Layer importer tool (#37)
* (Add) Settings & Issues: Enable or disable Empty Layers
* (Add) Layer issue Z map paired with layer navigation tracker bar
* (Add) Setting: Pixel editor can be configured to exit after each apply operation (#45)
* (Add) More abstraction on GUI and operations
* (Add) Verbose log - More a developer feature to cath bugs
* (Improvement) Redesign tools and mutator windows
* (Improvement) Erode, dilate, gap closing and noise removal converted into one window (Morph model)
* (Improvement) Convert add edit parameters into one tool window, edit all at once now
* (Improvement) Some edit parameters will trigger an error if outside the min/max limit
* (Improvement) Change some edit parameters to have decimals
* (Improvement) Kernel option on some mutators is now hidden by default
* (Improvement) When zoom into issue or drawing now it checks bounds of zoom rectangle and only performs ZoomToFit is it will be larger then the viewPort after zoom. Otherwise, it will zoom to the fixed zoom level (Auto zoom to region setting dropped as merged into this) (#42)
* (Improvement) Layer and Issues Repair: Detailed description and warning text in this dialog has been moved from main form into tooltips. It's useful information for new users, but not needed to be visible each time repair is run.
* (Improvement) Tool - Flip: Better performance on "make copy"
* (Improvement) Tool - Rotate: Disallow operation when selecting an angle of -360, 0 and 360
* (Improvement) Shortcuts: + and - to go up and down on layers were change to W and S keys. Reason: + and - are bound to zoom and can lead to problems
Less frequently used settings for gap and noise removal iterations have been moved to an advanced settings group that is hidden by default, and can be shown if changes in those settings is desired. For many users, those advanced settings can be left on default and never adjusted. (#43)
* (Change) Tool - Rotate - icon
* (Upgrade) OpenCV from 4.2 to 4.3
* (Upgrade) BinarySerializer from 8.5.2 to 8.5.3
* (Remove) Menu - Tools - Layer Removal and Layer clone for redudancy they now home at layer preview toolbar under "Actions" dropdown button
* (Fix) CWS: Add missing Platform X,Y,Z size when converting from SL1
* (Fix) CWS: Invert XY resolution when converting from SL1
* (Fix) Layer Preview: When selecting issues using SHIFT in the layer preview, the selected issue doesn't update in the issue list until after shift is released and slow operation
* (Fix) PrusaSlicer Printer: Kelant S400 Y Resolution from 1440 to 1600 and default slice settings, FLIP_XY removed, portait mode to landscape
* (Fix) Layer Clone window title was set to Pattern
* (Fix) CTB: Add support for CTB v3 (ezrec/uv3dp#97, #36)
* (Fix) SL1: Bottle volume doesn't accept decimal numbers
* (Fix) Tool - Change resolution: Confirmation text was set to remove layers
* (Fix) Fade iteration now working as expected
* (Fix) Pattern: When select big margins and cols/rows it triggers an error because value hits the maximum variable size
* (Fix) Mask: A crash when check "Invert" when mask is not loaded
* (Fix) Some text and phrases

## 04/09/2020 - v0.7.0.0

* (Add) "Rebuild GCode" button
* (Add) Issues: Touching Bounds and Empty Layers to the detect button
* (Add) Mutator - Pixel Dimming: Dims only the borders (Suggested by Marco Borzacconi)
* (Add) Mutator - Pixel Dimming: "Solid" button to set brightness only
* (Add) Issue Highlighting
  * Issues selected from the issue List View are now painted in an alternate configurable highlight color to distinguish them from non-selected issues.
  * Issues are now made active as soon as they are selected in the issue list, so single-click or arrow keys can now be used to select and issue. Double-click is no longer required.
  * Multi-select is supported. All selected issues on the currently visible layer will be highlighted with the appropriate highlight color.
  * When an issue is selected, if it is already visible in the layer preview, it will be highlighted, but not moved. If an issue is not visible when selected, it's layer will be made active (in necessary) and it will be centered in the layer preview to make it visible.
  * Issues can be selected directly from layer preview by double clicking or SHIFT+Left click on it (Hand mouse icon), also will be highlighted on issue list (This will not work while on pixel editor mode)
* (Add) Edit Pixel Operation Highlighting
  * Similar to issue highlighting, pending operations in the pixel edit view will be highlighted in an alternate configurable color when they are selected from the operations List View, including multi-select support.
  * Unlike issue highlighting, when an operation is selected from the List View, it will always be centered in the layer preview window, even if it is already visible on screen. A future update could be smarter about this and handle operations similar to issues (determining bounds of operations is a bit more involved than determining bounds of an issue).
* (Add) Crosshair Support
  * Cross-hairs can now be displayed to identify the exact location of each selected issue in the layer preview window. This is particularly beneficial at lower zoom levels to identify where issues are located within the overall layer.
  * Multi-select is supported, so selecting multiple issues will render multiple cross-hairs, one per issue.
  * Cross-hairs can be enabled/disabled on-demand using a tool strip button next to the issues button.
  * Cross-hairs can be configured to automatically fade at a specific zoom level, so that they are visible when zoomed-out, but disappear when zoomed in and issue highlighting is more obvious. The Zoom-level at which the fade occurs is configurable in settings.
  * Cross-hairs are visible in Pixel Edit mode, but they are linked to selected issues in the issues tab, not selected pixel operations in the pixel edit tab. Cross-hairs will automatically fade when an add/remove operation is initiated (via SHIFT key).
* (Add) Configurable auto-zoom level support
  * The zoom level used for auto-zoom operations is now configurable. It can be changed at any time by zooming to the desired level in the layer preview and double-clicking or CTRL-clicking the middle mouse button.
  * The currently selected auto-zoom level is indicated by a "lock" icon that appears next to the current zoom level indicator in the tool strip.
  * The default auto-zoom level (used on startup) can be configured in settings.
* (Add) Mouse-Based Navigation updates for the issue list, layer preview and pixel edit mode.
  * Issue List
     * Single Left or Right click now selects an issue from the issues list. If auto-zoom is enabled, the issue will also be centered and zoomed. Holding ALT will invert the configured behavior in your settings. With these navigation updates, leaving auto-zoom disabled in settings is recommended (and is now the new default).
     * Double-Left click or CTRL-Left-click on an issue in the issue list will zoom in on that specific issue.
     * Double-Right click or CTRL-Right-Click on any issue will zoom to fit either the build plate or the print bounds, depending on your settings. Holding ALT during the click operation will perform the inverse zoom action from what is configured in your settings(zoom plate vs zoom print bounds).
     * The Prev/Next buttons at the top of the Layer Preview will now auto-repeat if held down (similar to the layer scroll bar).
  * Layer Preview
     * Clicking in the Layer Preview window will allow you to grab and pan the image (unchanged behavior)
     * Double-Left clicking or CTRL-click on any point within the Layer Preview window will zoom in on that specific point using the locked auto-zoom level.
     * Double-Right click or CTRL-click in the layer preview will zoom-to-fit. Same behavior as double-left-click on an issue in the issue list.
     * Hold middle mouse button for 1 second will set the auto-zoom-level to the current zoom level.
     * Mouse wheel scroll behavior is unchanged (wheel scrolls in/out)
  * Pixel Edit Mode
     * Single click left or right in the pixel operation list view will now select an operation. Double click does the same (advanced zoom operations described for issue list are not currently supported from the operation list).
     * When Pixel Edit Mode is active, mouse operations in the Layer Preview area generally behave the same as described in the Layer Preview section above, including pan and double-click zoom in/out.
     * Pressing the SHIFT key in layer edit mode activates the ability to perform add/remove operations, while shift is pressed the cursor icon changes to a cross-hair, and add/remove operations can be performed. In this mode, pan and double-click zoom operations are disabled. Releasing the shift key will end add/remove mode and restore pan/zoom functions.
     * Shift-Left-Click will perform an add operations (add pixel, text, etc).
     * Shift-Right-Click will perform a remove operation (remove pixel, etc).
* (Change) Mouse coordinates on status bar now only change when SHIFT key is press, this allow to lock a position for debug
* (Remove) Confirmation for detect issues as they can now be cancelled
* (Fix) When next layer or previous layer button got disabled while pressing it get stuck
* (Fix) Partial island detection wasn't checking next layer as it should
* (Fix) chitubox: Keep some original values when read from chitubox sliced files
* (Fix) chitubox: Preview thumbnails to respect order and size names
* (Fix) Settings: Reset settings triggers a upgrade from previous version when relaunch UVtools and bring that same values
* (Fix) Issues: Touching bounds only calculate when resin traps are active
* Notes: This release is the combination of the following pull requests: #26, #27, #28, #29, #30, #31, #32, #33 (Thanks to Bryce Yancey)

## 27/08/2020 - v0.6.7.1

* (Add) Menu - Help - Benchmark: Run benchmark test to measure system performance 
* (Fix) Properties listview trigger an error when there are no groups to show
* (Fix) Elfin: "(Number of Slices = x)" to ";Number of Slices = x" (#24)

## 21/08/2020 - v0.6.7.0

* (Add) Tool: Layer Clone
* (Add) Mutator: Mask
* (Add) Mutator - Pixel Dimming: "Strips" pattern
* (Remove) Bottom progress bar

## 17/08/2020 - v0.6.6.1

* (Add) Elapsed time to the Log list
* (Add) Setting - Issues - Islands: Allow diagonal bonds with default to false (#22, #23)
* (Change) Tool - Repair Layers: Allow set both iterations to 0 to skip closing and opening operations and allow remove islands independently
* (Change) Title - file open time from miliseconds to seconds
* (Improvement) Tool - Repair Layers: Layer image will only read/save if required and if current layer got modified
* (Fix) Setting - Issues - Islands: "Pixels below this value will turn black, otherwise white" (Threshold) was not using the set value and was forcing 1
* (Fix) Remove duplicated log for repair layers and issues

## 11/08/2020 - v0.6.6.0

* (Add) Pixel Editor: Eraser - Right click over a white pixel to remove it whole linked area (Fill with black) (#7)
* (Add) Pixel Editor: Parallel layer image save when apply modifications 
* (Add) GCode: Save to clipboard
* (Change) Issues Repair: Default noise removal iterations to 0
* (Fix) Edit: Remove decimal plates for integer properties
* (Fix) cws: Exposure time was in seconds, changed to ms (#17)
* (Fix) cws: Calculate blanking time (#17)
* (Fix) cws: Edit LiftHeight and Exposure Time was enforcing integer number
* (Fix) cws: GCode extra space between slices
* (Fix) cws and zcodex: Precision errors on retract height

## 08/08/2020 - v0.6.5.0

* (Add) Mutators: Custom kernels, auto kernels and anchor where applicable
* (Add) Mutator - Blur: Box Blur
* (Add) Mutator - Blur: Filter2D
* (Improvement) Mutator: Group all blurs into one window
* (Fix) Mutators: Sample images was gone
* (Fix) Mutator - Solidify: Remove the disabled input box
* (Fix) Mutator - Pixel Dimming: Disable word wrap on pattern text box

## 06/08/2020 - v0.6.4.3

* (Add) Pixel Editor - Supports and Drain holes: AntiAliasing
* (Add) Pixel Editor - Drawing: Line type and defaults to AntiAliasing
* (Add) Pixel Editor - Drawing: Line thickness to allow hollow shapes
* (Add) Pixel Editor - Drawing: Layer depth, to add pixels at multiple layers at once
* (Add) Pixel Editor: Text writing (#7)

## 05/08/2020 - v0.6.4.2

* (Add) Hold "ALT" key when double clicking over items to invert AutoZoom setting, prevent or do zoom in issues or pixels, this will behave as !AutoZoom as long key is held
* (Improvement) Partial island update speed, huge boost performance over large files

## 04/08/2020 - v0.6.4.1

* (Add) Partial update islands from current working layer and next layer when using pixel editor or island remove
* (Add) Setting: To enable or disable partial update islands
* (Change) Properties, Issues, Pixel Editor: ListView upgraded to a FastObjectListView, resulting in faster renders, sorting capabilities, column order, groups with counter, selection, hot tracking, filtering and empty list message
* (Change) Log: ObjectListView upgraded to a FastObjectListView
* (Change) Bunch of icons

## 30/07/2020 - v0.6.4.0

* (Add) Tool: Change resolution
* (Add) Log: Track every action you do on the program

## 28/07/2020 - v0.6.3.4

* (Add) Mutator: Threshold pixels
* (Change) Mutator: PyrDownUp - Name to "Big Blur" and add better description of the effect
* (Change) Mutator: SmoothMedian - Better description
* (Change) Mutator: SmoothGaussian - Better description
* (Fix) Tool: Layer Re-Height - When go lower heights the pixels count per layer statistics are lost
* (Fix) "Pixel Edit" has the old tooltip text (#14)
* (Fix) Readme: Text fixes (#14)

## 26/07/2020 - v0.6.3.3

* (Add) Allow to save properties to clipboard
* (Add) Tool: Layer Repair - Allow remove islands below or equal to a pixel count (Suggested by: Nicholas Taylor)
* (Add) Issues: Allow sort columns by click them (Suggested by: Nicholas Taylor)
* (Improvement) Tool: Pattern - Prevent open this tool when unable to pattern due lack of space
* (Fix) Tool: Layer Repair - When issues are not caculated before, they are computed but user settings are ignored

## 24/07/2020 - v0.6.3.2

* (Add) Tool: Layer Re-Height - Allow change layer height
* (Add) Setting: Gap closing default iterations
* (Add) Setting: Noise removal default iterations
* (Add) Setting: Repair layers and islands by default
* (Add) Setting: Remove empty layers by default
* (Add) Setting: Repair resin traps by default
* (Change) Setting: "Reset to Defaults" changed to "Reset All Settings"
* (Fix) CWS: Lack of ';' on GCode was preventing printer from printing

## 20/07/2020 - v0.6.3.1

* (Add) Preview: Allow import images from disk and replace preview image
* (Add) Setting: Auto zoom to issues and drawings portrait area (best fit)
* (Add) Issue and Pixel Editor ListView can now reorder columns
* (Add) Pixel Editor: Button "Clear" remove all the modifications
* (Add) Pixel Editor: Button "Apply All" to apply the modifications
* (Add) Pixel Editor: Double click items will track and position over the draw
* (Fix) Pixel Editor: Label "Operations" was not reset to 0 after apply the modifications
* (Fix) Pixel Editor: Button "Remove" tooltip
* (Fix) Pixel Editor: Drawing - Bursh Area - px to px²

## 19/07/2020 - v0.6.3.0

* (Add) Layer remove button
* (Add) Tool: Layer removal
* (Add) Layer Repair tool: Remove empty layers
* (Add) Issues: Remove a empty layer will effectively remove the layer
* (Fix) SL1: When converting to other format in some cases the parameters on Printer Notes were not respected nor exported (#12)
* (Fix) Pixel Editor: Draw pixels was painting on wrong positions after apply, when refreshing layer some pixels disappear (Spotted by Nicholas Taylor)

## 17/07/2020 - v0.6.2.3

* (Add) Issue: EmptyLayer - Detects empty layers were image is all black with 0 pixels to cure
* (Add) Toolbar and pushed layer information to bottom
* (Add) Information: Cure pixel count per layer and percentage against total lcd pixels
* (Add) Information: Bounds per layer
* (Add) Zip: Compability with Formware zip files

## 14/07/2020 - v0.6.2.2

* (Add) cbddlp, photon and ctb version 3 compability (Chitubox >= 1.6.5)

## 14/07/2020 - v0.6.2.1

* (Fix) Mutator: Erode was doing pixel dimming

## 14/07/2020 - v0.6.2.0

* (Add) PrusaSlicer Printer: Elegoo Mars 2 Pro
* (Add) PrusaSlicer Printer: Creality LD-002H
* (Add) PrusaSlicer Printer: Voxelab Polaris
* (Add) File Format: UVJ (#8)
* (Add) Mutataor: Pixel Dimming
* (Add) Pixel Editor tab with new drawing functions
* (Add) Pixel Editor: Bursh area and shape
* (Add) Pixel Editor: Supports
* (Add) Pixel Editor: Drain holes
* (Add) Settings for pixel editor
* (Add) Setting: File open default directory
* (Add) Setting: File save default directory
* (Add) Setting: File extract default directory
* (Add) Setting: File convert default directory
* (Add) Setting: File save prompt for overwrite (#10)
* (Add) Setting: File save preffix and suffix name
* (Add) Setting: UVtools version to the title bar
* (Improvement) Force same directory as input file on dialogs
* (Improvement) Pattern: Better positioning when not using an anchor, now it's more center friendly
* (Change) Setting: Start maximized defaults to true
* (Fix) Pattern: Calculated volume was appending one margin width/height more
* (Fix) When cancel a file load, some shortcuts can crash the program as it assume file is loaded
* (Fix) pws: Encode using the same count-of-threshold method as CBDDLP (ezrec/uv3dp#79)

## 02/07/2020 - v0.6.1.1

* (Add) Allow chitubox, phz, pws, pw0 files convert to cws
* (Add) Allow convert between cbddlp, ctb and photon
* (Add) Allow convert between pws and pw0
* (Improvement) Layers can now have modified heights and independent parameters (#9)
* (Improvement) UVtools now generate better gcode and detect the lack of Lift and same z position and optimize the commands
* (Fix) zcodex: Wasn't reporting layer decoding progress

## 02/07/2020 - v0.6.1.0

* (Add) Thumbnail image can now saved to clipboard
* (Add) Setting to allow choose default file extension at load file dialog
* (Add) Double click middle mouse to zoom to fit to image
* (Add) Move mutator to move print volume around the plate
* (Add) Pattern tool
* (Change) Setting window now have tabs to compact the window height
* (Fix) Progress for mutators always show layer count instead of selected range

## 01/07/2020 - v0.6.0.2

* (Add) PrusaSlicer Printer "EPAX X10 4K Mono"
* (Improvement) Better progress window with real progress and cancel button
* (Improvement) Mutators text and name
* (Fix) sl1: After save file gets decoded again
* (Fix) photon, cbddlp, ctb, phz, pws, pw0: Unable to save file, not closed from the decode session
* (Fix) zcodex: Unable to convert file
* (Fix) images: Wasn't opening
* (Fix) images: Wasn't saving
* (Fix) When click on button "New version is available" sometimes it crash the program
* (Fix) Force 1 layer scroll when using Mouse Wheel to scroll the tracker bar
* (Fix) PrusaSlicer printers: Mirror vertically instead to produce equal orientation compared with chitubox

## 29/06/2020 - v0.6.0.1

* (Improvement) Pixel edit now spare a memory cycle per pixel
* (Fix) Resin trap detection was considering layer 0 black pixels as always a drain and skip potential traps on layer 0
* (Fix) Resin trap was crashing when reach -1 layer index and pass the layer count
* (Fix) Pixel edit was crashing the program

## 29/06/2020 - v0.6.0.0

* (Add) UVtools now notify when a new version available is detected
* (Add) Mutator "Flip"
* (Add) Mutator "Rotate"
* (Add) User Settings - Many parameters can now be customized to needs
* (Add) File load elapsed time into Title bar
* (Add) Outline - Print Volume bounds
* (Add) Outline - Layer bounds
* (Add) Outline - Hollow areas
* (Add) Double click layer picture to Zoom To Fit
* (Improvement) Huge performance boost in layer reparing and in every mutator
* (Improvement) Layer preview is now faster
* (Improvement) Islands detection is now better and don't skip any pixel, more islands will show or the region will be bigger
* (Improvement) Islands search are now faster, it will jump from island to island instead of search in every pixel by pixel
* (Improvement) ResinTrap detection and corrected some cases where it can't detect a drain
* (Improvement) Better memory optimization by dispose all objects on operations
* (Improvement) Image engine changed to use only OpenCV Mat instead of two and avoid converting from one to another, as result there's a huge performance gain in some operations (#6)
* (Improvement) UVtools now rely on UVtools.Core, and drop the UVtools.Parser. The Core now perform all operations and transformations inplace of the GUI
* (Improvement) If error occur during save it will show a message with the error
* (Improvement) When rotate layer it will zoom to fit
* (Improvement) Allow zoom to fit to print volume area instead of whole build volume
* (Removed) ImageSharp dependency
* (Removed) UVtools.Parser project
* (Fix) Nova3D Elfin printer values changed to Display Width : 131mm / Height : 73mm & Screen X: 2531 / Y: 1410 (#5)
* (Fix) Fade resizes make image offset a pixel from layer to layer because of integer placement, now it matain the correct position
* (Fix) sl1: AbsoluteCorrection, GammaCorrection, MinExposureTime, MaxExposureTime, FastTiltTime, SlowTiltTime and AreaFill was byte and float values prevents the file from open (#4)
* (Fix) zcodex: XCorrection and YCorrection was byte and float values prevents the file from open (#4)
* (Fix) cws: XCorrection and YCorrection was byte and float values prevents the file from open (#4)
* (Fix) cws: Wrong # char on .gcode file prevent from printing (#4)

## 21/06/2020 - v0.5.2.2

* (Fix) phz: Files with encryption or sliced by chitubox produced black images after save due not setting the image address nor size (Spotted by Burak Cezairli)

## 20/06/2020 - v0.5.2.1

* (Add) cws: Allow change layer PWM value
* (Update) Dependency ImageSharp from 1.0.0-rc0002 to 1.0.0-rc0003 (It fix a error on resize function)
* (Fix) cws: GCode 0 before G29
* (Fix) Phrozen Sonic Mini: Display Height from 66.04 to 68.04
* (Fix) Zortrax Inkspire: Display and Volume to 74.67x132.88
* (Fix) Layer repair tool allow operation when every repair checkbox is deselected

## 19/06/2020 - v0.5.2

* (Add) Resin Trap issue validator and repairer - Experimental Feature (#3)
* (Add) Layer Repair tool can now fix Resin Traps when selected
* (Add) "Remove" issues button fix selected Resin traps, the operation now run under a thread and in a parallel way, preventing the GUI from freeze
* (Change) "Repair Layers" button renamed to "Repair Layers and Issues"
* (Fix) When do a "repair layers" before open the Issue tab, when open next it will recompute issues without the need

## 18/06/2020 - v0.5.1.3

* (Add) Button save layer image to Clipboard 
* (Change) Go to issue now zoom at bounding area instead of first pixels
* (Change) Layer navigation panel width increased in 20 pixels, in some cases it was overlaping the slider
* (Change) Actual layer information now have a depth border
* (Change) Increased main GUI size to X: 1800 and Y: 850 pixels
* (Change) If the GUI window is bigger than current screen resolution, it will start maximized istead
* (Fix) cbddlp: AntiAlias is number of _greys_, not number of significant bits (ezrec/uv3dp#75)
* (Fix) Outline not working as before, due a forget to remove test code

## 17/06/2020 - v0.5.1.2

* (Add) Able to install only the desired profiles and not the whole lot (Suggested by: Ingo Strohmenger)
* (Add) Update manager for PrusaSlicer profiles
* (Add) If PrusaSlicer not installed on system it prompt for installation (By open the official website)
* (Fix) Prevent profiles instalation when PrusaSlicer is not installed on system
* (Fix) The "Issues" computation sometimes fails triggering an error due the use of non concurrent dictionary
* (Fix) Print profiles won't install into PrusaSlicer

## 16/06/2020 - v0.5.1.1

* (Add) photon, cbddlp, ctb and phz can be converted to Zip
* (Fix) ctb: When AntiAliasing is on it saves a bad file

## 16/06/2020 - v0.5.1

* (Add) Zip file format compatible with chitubox zip
* (Add) PrusaSlicer Printer "Kelant S400"
* (Add) PrusaSlicer Printer "Wanhao D7"
* (Add) PrusaSlicer Printer "Wanhao D8"
* (Add) PrusaSlicer Printer "Creality LD-002R"
* (Add) Shortcut "CTRL+C" under Issues listview to copy all selected item text to clipboard
* (Add) Shortcut "ESC" under Properties listview to deselect all items
* (Add) Shortcut "CTRL+A" under Properties listview to select all items
* (Add) Shortcut "*" under Properties listview to invert selection
* (Add) Shortcut "CTRL+C" under Properties listview to copy all selected item text to clipboard
* (Add) Resize function can now fade towards 100% (Chamfers)
* (Add) Solidify mutator, solidifies the selected layers, closes all inner holes
* (Change) Renamed the project: UVtools
* (Change) On title bar show loaded filename first and program version after
* (Improvement) Increased Pixel column width on Issues tab listview
* (Fix) Resize function can't make use of decimal numbers
* (Fix) CWS gcode was setting M106 SO instead of M106 S0
* (Fix) CWS disable motors before raise Z after finish print

## 13/06/2020 - v0.5

* (Add) PWS and PW0 file formats (Thanks to Jason McMullan)
* (Add) PrusaSlicer Printer "AnyCubic Photon S"
* (Add) PrusaSlicer Printer "AnyCubic Photon Zero"
* (Add) PrusaSlicer Universal Profiles optimized for non SL1 printers (Import them)
* (Add) Open image files as single layer and transform them in grayscale (jpg, jpeg, png, bmp, gif, tga)
* (Add) Resize mutator
* (Add) Shortcut "F5" to reload current layer preview
* (Add) Shortcut "Home" and button go to first layer
* (Add) Shortcut "End" and button go to last layer
* (Add) Shortcut "+" and button go to next layer
* (Add) Shortcut "-" and button go to previous layer
* (Add) Shortcut "CTRL+Left" go to previous issue if available
* (Add) Shortcut "CTRL+Right" go to next issue if available
* (Add) Shortcut "Delete" to remove selected issues
* (Add) Button to jump to a layer number
* (Add) Show current layer and height near tracker position
* (Add) Auto compute issues when click "Issues" tab for the first time for the open file
* (Add) "AntiAliasing_x" note under PrusaSlicer printer to enable AntiAliasing on supported formats, printers lacking this note are not supported
* (Add) AntiAliasing capable convertions
* (Add) Touching Bounds detection under issues
* (Change) Scroll bar to track bar
* (Change) Keyword "LiftingSpeed" to "LiftSpeed" under PrusaSlicer notes (Please update printers notes or import them again)
* (Change) Keywords For Nova3D Elfin printer under PrusaSlicer notes (Please update printers notes or import them again)
* (Change) Keywords For Zortrax Inkspire printer under PrusaSlicer notes (Please update printers notes or import them again)
* (Change) Islands tab to Issues ab
* (Improvement) Much faster photon, cbddlp, cbt and phz file encoding/convert and saves
* (Improvement) Much faster layer scroll display
* (Improvement) Hide empty items for status bar, ie: if printer don't have them to display
* (Improvement) Smooth mutators descriptions
* (Improvement) Disallow invalid iteration numbers for smooth mutators
* (Improvement) File reload now reshow current layer after reload
* (Improvement) Some dependecies were updated and ZedGraph removed
* (Fix) AntiAlias decodes for photon and cbddlp
* (Fix) AntiAlias encodes and decodes for cbt
* (Fix) Save the preview thumbnail image trigger an error
* (Fix) Implement missing "InheritsCummulative" key to SL1 files
* (Fix) Install print profiles button, two typos and Cancel button doesn't really cancel the operation

## 05/06/2020 - v0.4.2.2 - Beta

* (Add) Shortcut "ESC" under Islands listview to deselect all items
* (Add) Shortcut "CTRL+A" under Islands listview to select all items
* (Add) Shortcut "*" under Islands listview to invert selection
* (Add) Shortcut "CTRL+F" to go to a layer number
* (Change) Layer image is now a RGB image for better manipulation and draws
* (Change) Layer difference now shows previous and next layers (only pixels not present on current layer) were previous are pink and next are cyan, if a pixel are present in both layers a red pixel will be painted.
* (Fix) Save modified layers on .cbddlp and .cbt corrupts the file to print when Anti-Aliasing is used (> 1)
* (Fix) cbdlp layer encoding

## 04/06/2020 - v0.4.2.1 - Beta

* (Add) PrusaSlicer Printer "AnyCubic Photon"
* (Add) PrusaSlicer Printer "Elegoo Mars Saturn"
* (Add) PrusaSlicer Printer "Elegoo Mars"
* (Add) PrusaSlicer Printer "EPAX X10"
* (Add) PrusaSlicer Printer "EPAX X133 4K Mono"
* (Add) PrusaSlicer Printer "EPAX X156 4K Color"
* (Add) PrusaSlicer Printer "Peopoly Phenom L"
* (Add) PrusaSlicer Printer "Peopoly Phenom Noir"
* (Add) PrusaSlicer Printer "Peopoly Phenom"
* (Add) PrusaSlicer Printer "Phrozen Shuffle 4K"
* (Add) PrusaSlicer Printer "Phrozen Shuffle Lite"
* (Add) PrusaSlicer Printer "Phrozen Shuffle XL"
* (Add) PrusaSlicer Printer "Phrozen Shuffle"
* (Add) PrusaSlicer Printer "Phrozen Sonic"
* (Add) PrusaSlicer Printer "Phrozen Transform"
* (Add) PrusaSlicer Printer "QIDI Shadow5.5"
* (Add) PrusaSlicer Printer "QIDI Shadow6.0 Pro"
* (Add) "Detect" text to compute layers button
* (Add) "Repair" islands button on Islands tab
* (Add) "Highlight islands" button on layer toolbar
* (Add) Possible error cath on island computation
* (Add) After load new file layer is rotated or not based on it width, landscape will not rotate while portrait will
* (Improvement) Highlighted islands now also show AA pixels as a darker yellow
* (Improvement) Island detection now need a certain number of touching pixels to consider a island or not, ie: it can't lay on only one pixel
* (Fix) Island detection now don't consider dark fadded AA pixels as safe land
* (Fix) Epax X1 printer properties

## 03/06/2020 - v0.4.2 - Beta

* (Add) Zoom times information
* (Add) Island checker, navigation and removal
* (Add) Layer repair with island repair
* (Add) Show mouse coordinates over layer image
* (Fix) Pixel edit cant remove faded AA pixels
* (Fix) Pixel edit cant add white pixels over faded AA pixels
* (Change) Nova3D Elfin printer build volume from 130x70 to 132x74

## 01/06/2020 - v0.4.1 - Beta

* (Add) Opening, Closing and Gradient Mutators
* (Add) Choose layer range when appling a mutator #1
* (Add) Choose iterations range/fading when appling a mutator (Thanks to Renos Makrosellis)
* (Add) Global and unhandled exceptions are now logged to be easier to report a bug
* (Change) Current layer and layer count text was reduced by 1 to match indexes on mutators
* (Improvement) Better mutator dialogs and explanation
* (Improvement) Compressed GUI images size
* (Fix) SlicerHeader was with wrong data size and affecting .photon, .cbddlp and .cbt (Thanks to Renos Makrosellis)


## 27/05/2020 - v0.4 - Beta

* (Add) CWS file format
* (Add) Nova3D Elfin printer
* (Add) Zoom and pan functions to layer image
* (Add) Pixel editor to add or remove pixels
* (Add) Outline layer showing only borders
* (Add) Image mutators, Erode, Dilate, PyrDownUp, Smooth
* (Add) Task to save operation
* (Add) Printers can be installed from GUI Menu -> About -> Install printers into PrusaSlicer
* (Improvement) Layer Management
* (Improvement) Faster Save and Save As operation
* (Fix) Bad layer image when converting SL1 to PHZ
* (Fix) Corrected EncryptionMode for PHZ files
* (Fix) Save As can change file extension
* (Fix) Save As no longer reload file
* (Fix) SL1 files not accepting float numbers for exposures
* (Fix) SL1 files was calculating the wrong layer count when using slow layer settings
* (Fix) Modifiers can't accept float values
* (Fix) Sonic Mini prints mirroed
* (Fix) Layer resolution shows wrong values

## 21/05/2020 - v0.3.3.1 - Beta

* (Fix) Unable to convert Chitubox or PHZ files when enconter repeated layer images

## 19/05/2020 - v0.3.3 - Beta

* (Add) PHZ file format
* (Add) "Phrozen Sonic Mini" printer
* (Add) Convert Chitubox files to PHZ files and otherwise
* (Add) Convert Chitubox and PHZ files to ZCodex
* (Add) Elapsed seconds to convertion and extract dialog
* (Improvement) "Convert To" menu now only show available formats to convert to, if none menu is disabled
* (Fix) Enforce cbt encryption
* (Fix) Not implemented convertions stay processing forever


## 11/05/2020 - v0.3.2 - Beta

* (Add) Show layer differences where daker pixels were also present on previous layer and the white pixels the difference between previous and current layer.
* (Add) Layer preview process time in milliseconds
* (Add) Long operations no longer freeze the GUI and a progress message will shown on those cases
* (Improvement) Cache layers were possible for faster operation
* (Improvement) As layer data is now cached, input file is closed after read, this way file wouldn't be locked for other programs
* (Improvement) Speed up extraction with parallelism
* (Improvement) Extract output folder dialog now open by default on from same folder as input file
* (Improvement) Extract now create a folder with same file name to dump the content
* (Fix) Extract to folder was wiping the target folder, this is now disabled to prevent acidental data lost, target files will be overwritten

## 30/04/2020 - v0.3.1 - Beta

* (Add) Thumbnails to converted photon and cbddlp files
* (Add) ctb file format
* (Add) Show possible extensions/files under "Convert To" menu
* (Add) Open new file in a new window without lose current work
* (Improvement) Rename and complete some Chitubox properties
* (Improvement) More completion of cbddlp file
* (Improvement) Optimized layer read from cbddlp file
* (Improvement) Add layer hash code to encoded Chitubox layers in order to optimize file size in case of repeated layer images
* (Improvement) GUI thumbnail preview now auto scale splitter height to a max of 400px when change thumbnail
* (Improvement) After convertion program prompt for open the result file in a new window
* (Change) Move layer rotate from view menu to layer menu
* (Change) Cbbdlp convertion name to Chitubox
* (Change) On convert, thumbnails are now resized to match exactly the target thumbnail size
* (Change) GUI will now show thumbnails from smaller to larger
* (Fix) RetractFeedrate was incorrectly used instead of LiftFeedrate on Zcodex gcode

## 27/04/2020 - v0.3.0 - Beta

* (Add) zcodex file format
* (Add) Zortrax Inkspire Printer
* (Add) Properties menu -- Shows total keys and allow save information to a file
* (Add) "GCode" viewer Tab -- Only for formats that incldue gcode into file (ie: zcodex)
* (Add) Save gcode to a text file
* (Add) Allow to vertical arrange height between thumbnails and properties
* (Improvement) Thumbnail section is now hidden if no thumbnails avaliable
* (Improvement) Thumbnail section now vertical auto scales to the image height on file load
* (Improvement) On "modify properties" window, ENTER key can now be used to accept and submit the form
* (Fixed) Current model height doesn't calculate when viewing cbddlp files
* (Change) Round values up to two decimals
* (Change) Move actual model height near total height, now it shows (actual/total mm)
* (Change) Increase font size
* (Change) Rearrange code

## 22/04/2020 - v0.2.2 - Beta

* (Add) File -> Reload
* (Add) File -> Save
* (Add) File -> Save As
* (Add) Can now ajust some print properties
* (Add) 'Initial Layer Count' to status bar
* (Add) Allow cbbdlp format to extract 'File -> Extract'
* (Add) Thumbnail resolution label
* (Add) Layer resolution label
* (Add) Allow save current layer image
* (Change) Rearrange menu edit items to file
* (Change) Edit some shortcuts
* (Change) Strict use dot (.) for real numbers instead of comma (,)

## 15/04/2020 - v0.2.1 - Beta

* (Add) Allow open other file formats as well on viewer
* (Add) All thumbnails can now be seen and saved
* (Add) Rotate layer image
* (Add) Close file
* (Change) more abstraction
* (Change) from PNG to BMP compression to speed up bitmap coversion
* (Change) Faster layer preview

## 12/04/2020 - v0.2 - Beta

* (Add) cbddlp file format
* (Add) "convert to" function, allow convert sl1 file to another
* (Add) EPAX X1 printer
* (Change) Code with abstraction of file formats

## 06/04/2020 - V0.1 - Beta

* First release for testing