# Changelog

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

* (Add) Able to install only the desired profiles and not the whole lot (Suggested  by: Ingo Strohmenger)
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