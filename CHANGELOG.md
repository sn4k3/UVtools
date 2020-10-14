# Changelog

## ??/??/2020 - v0.9.0.0

* (Add) Multi-OS with Linux and MacOS support
* (Add) Themes support
* (Add) Fullscreen support (F11)
* (Change) GUI was rewritten from Windows Forms to WPF Avalonia, C#
* (Improvement) GUI is now scalable
* (Fix) Some bug found and fixed during convertion

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